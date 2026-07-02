using Microsoft.EntityFrameworkCore;
using CopilotTest.Services;
using CopilotTest.Components;
using CopilotTest.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var dbPath = Path.Combine(builder.Environment.ContentRootPath, "dnd-party.db");
// Factory (not a scoped DbContext): each operation creates a fresh, short-lived
// context — avoids one long-lived context per Blazor circuit accumulating stale state.
builder.Services.AddDbContextFactory<DndDbContext>(opt =>
    opt.UseSqlite($"Data Source={dbPath}")
       .AddInterceptors(new SqlitePragmaInterceptor()));

builder.Services.AddScoped<DndApiService>();
// CharacterService is stateless (holds only the singleton DbContext factory), so it
// can be a singleton — required because the singleton CombatEngineService depends on it.
builder.Services.AddSingleton<CharacterService>();
// Combat is now SHARED across all players' circuits so the DM and players see one
// live encounter. Singleton + internal locking (see CombatEngineService._gate).
builder.Services.AddSingleton<CombatEngineService>();
// The live table's initiative tracker — bookkeeping only, no rules automation.
builder.Services.AddSingleton<LiveEncounterService>();
builder.Services.AddScoped<PdfImportService>();
builder.Services.AddScoped<ContentService>();

// Shared table-seat registry (who is DM / which character is claimed).
builder.Services.AddSingleton<SeatRegistry>();
// This browser's chosen seat (per circuit).
builder.Services.AddScoped<PlayerSeat>();

// Play-mode services (dice roller, per-session roll log, live play state)
builder.Services.AddScoped<DiceService>();
builder.Services.AddScoped<RollLogService>();
builder.Services.AddScoped<PlayState>();

var app = builder.Build();

// Ensure DB exists and seed party members on first run
using (var scope = app.Services.CreateScope())
{
    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<DndDbContext>>();
    using var db = dbFactory.CreateDbContext();
    db.Database.EnsureCreated();

    // Switch the database to Write-Ahead Logging so readers don't block the
    // writer (and vice versa) — important once the whole party is connected at
    // once. WAL is a persistent property of the file, so setting it once sticks.
    db.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");

    // EnsureCreated never alters existing databases, so add tables introduced
    // after first release by hand (schema must match the EF model exactly).
    db.Database.ExecuteSqlRaw("""
        CREATE TABLE IF NOT EXISTS "CharacterFeatures" (
            "Id" TEXT NOT NULL CONSTRAINT "PK_CharacterFeatures" PRIMARY KEY,
            "CharacterId" TEXT NOT NULL,
            "Name" TEXT NOT NULL,
            "Description" TEXT NOT NULL,
            "Source" TEXT NOT NULL,
            "LevelGained" INTEGER NOT NULL,
            CONSTRAINT "FK_CharacterFeatures_Characters_CharacterId"
                FOREIGN KEY ("CharacterId") REFERENCES "Characters" ("Id") ON DELETE CASCADE
        );
        CREATE INDEX IF NOT EXISTS "IX_CharacterFeatures_CharacterId" ON "CharacterFeatures" ("CharacterId");

        CREATE TABLE IF NOT EXISTS "AbilityGrants" (
            "Id" TEXT NOT NULL CONSTRAINT "PK_AbilityGrants" PRIMARY KEY,
            "CharacterId" TEXT NOT NULL,
            "Ability" TEXT NOT NULL,
            "Amount" INTEGER NOT NULL,
            "Source" TEXT NOT NULL,
            CONSTRAINT "FK_AbilityGrants_Characters_CharacterId"
                FOREIGN KEY ("CharacterId") REFERENCES "Characters" ("Id") ON DELETE CASCADE
        );
        CREATE INDEX IF NOT EXISTS "IX_AbilityGrants_CharacterId" ON "AbilityGrants" ("CharacterId");

        CREATE TABLE IF NOT EXISTS "AppMeta" (
            "Key" TEXT NOT NULL CONSTRAINT "PK_AppMeta" PRIMARY KEY,
            "Value" TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS "CombatSnapshots" (
            "Id" INTEGER NOT NULL CONSTRAINT "PK_CombatSnapshots" PRIMARY KEY,
            "Json" TEXT NOT NULL
        );
        """);

    // Add columns introduced after first release (SQLite has no ADD COLUMN IF NOT
    // EXISTS, so check the table schema first).
    AddColumnIfMissing(db, "Characters", "SpeciesKey", "TEXT NOT NULL DEFAULT ''");
    AddColumnIfMissing(db, "Characters", "BackgroundKey", "TEXT NOT NULL DEFAULT ''");
    AddColumnIfMissing(db, "Characters", "SubclassKey", "TEXT NOT NULL DEFAULT ''");
    AddColumnIfMissing(db, "Spells", "Source", "TEXT NOT NULL DEFAULT ''");
    AddColumnIfMissing(db, "CombatActions", "RequiresConcentration", "INTEGER NOT NULL DEFAULT 0");

    var chars = scope.ServiceProvider.GetRequiredService<CharacterService>();
    chars.SeedIfEmpty();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// ── Shared-password gate ──────────────────────────────────────────────────
// Replaces Caddy's HTTP basic_auth, whose browser popup fired multiple times
// (parallel first-load requests) and sometimes failed to authenticate Blazor's
// WebSocket, leaving a "logged in" but dead page. A cookie rides along on every
// request INCLUDING the WebSocket, so one login per device (~6 months) just works.
// Enabled only when QUAIL_GATE_PASSWORD is set (production); local dev is open.
var gatePassword = Environment.GetEnvironmentVariable("QUAIL_GATE_PASSWORD");
if (!string.IsNullOrEmpty(gatePassword))
{
    // Deterministic token: survives app restarts, and rotating the password
    // invalidates every device's cookie at once.
    var gateToken = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
        System.Text.Encoding.UTF8.GetBytes("quail-gate-v1:" + gatePassword)));

    app.Use(async (ctx, next) =>
    {
        if (ctx.Request.Path.StartsWithSegments("/gate"))
        {
            if (HttpMethods.IsPost(ctx.Request.Method))
            {
                var form = await ctx.Request.ReadFormAsync();
                if (form["password"].ToString() == gatePassword)
                {
                    ctx.Response.Cookies.Append("quail-gate", gateToken, new CookieOptions
                    {
                        HttpOnly = true,
                        SameSite = SameSiteMode.Lax,
                        MaxAge = TimeSpan.FromDays(180),
                        Path = "/",
                    });
                    var from = form["from"].ToString();
                    ctx.Response.Redirect(from.StartsWith('/') && !from.StartsWith("//") ? from : "/");
                    return;
                }
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await WriteGatePage(ctx, wrongPassword: true);
                return;
            }
            await WriteGatePage(ctx, wrongPassword: false);
            return;
        }

        if (ctx.Request.Cookies["quail-gate"] == gateToken)
        {
            await next();
            return;
        }

        // Probes (e.g. the auto-reload script's HEAD poll) get a plain 401;
        // everything else is sent to the login page, remembering where it was.
        if (HttpMethods.IsHead(ctx.Request.Method))
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }
        ctx.Response.Redirect("/gate?from=" +
            Uri.EscapeDataString(ctx.Request.Path + ctx.Request.QueryString));
    });
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

// The shared-password login page (self-contained: no CSS/JS dependencies, since
// static files are behind the gate too).
static async Task WriteGatePage(HttpContext ctx, bool wrongPassword)
{
    var from = HttpMethods.IsPost(ctx.Request.Method)
        ? (await ctx.Request.ReadFormAsync())["from"].ToString()
        : ctx.Request.Query["from"].ToString();
    var fromAttr = System.Net.WebUtility.HtmlEncode(from);
    var error = wrongPassword ? "<p style='color:#e66'>That's not it — try again!</p>" : "";
    ctx.Response.ContentType = "text/html; charset=utf-8";
    await ctx.Response.WriteAsync($$"""
        <!DOCTYPE html>
        <html lang="en"><head>
        <meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1.0">
        <title>Quail Party — Tavern Door</title>
        <style>
            body { margin:0; min-height:100vh; display:flex; align-items:center; justify-content:center;
                   background:#101a1e; color:#f0eeea; font-family:system-ui,sans-serif; }
            .door { background:#16232a; border:1px solid #FFA300; border-radius:12px;
                    padding:32px 36px; text-align:center; max-width:320px; }
            h1 { color:#FFA300; font-size:1.4rem; margin:0 0 6px; }
            p { color:#b9bdc0; font-size:0.9rem; margin:0 0 18px; }
            input { width:100%; box-sizing:border-box; padding:10px 12px; font-size:1rem;
                    background:#101a1e; color:#f0eeea; border:1px solid #3a4a52; border-radius:8px; }
            button { margin-top:12px; width:100%; padding:10px; font-size:1rem; border:0; border-radius:8px;
                     background:#FFA300; color:#101a1e; font-weight:700; cursor:pointer; }
        </style></head>
        <body><div class="door">
            <h1>🎲 Quail Party</h1>
            <p>What's the tavern password?</p>
            {{error}}
            <form method="post" action="/gate">
                <input type="password" name="password" placeholder="Tavern password"
                       autocomplete="current-password" autofocus required>
                <input type="hidden" name="from" value="{{fromAttr}}">
                <button type="submit">Enter the Tavern</button>
            </form>
        </div></body></html>
        """);
}

// Adds a column to a SQLite table only if it isn't already present.
static void AddColumnIfMissing(DndDbContext db, string table, string column, string columnDef)
{
    using var cmd = db.Database.GetDbConnection().CreateCommand();
    if (cmd.Connection!.State != System.Data.ConnectionState.Open) cmd.Connection.Open();
    cmd.CommandText = $"SELECT COUNT(*) FROM pragma_table_info('{table}') WHERE name = '{column}';";
    var exists = Convert.ToInt64(cmd.ExecuteScalar()) > 0;
    if (!exists)
        // Inputs are compile-time constants from the calls above, never user data.
#pragma warning disable EF1002
        db.Database.ExecuteSqlRaw($"ALTER TABLE \"{table}\" ADD COLUMN \"{column}\" {columnDef};");
#pragma warning restore EF1002
}
