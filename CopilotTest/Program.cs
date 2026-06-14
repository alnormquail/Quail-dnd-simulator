using Microsoft.EntityFrameworkCore;
using CopilotTest.Services;
using CopilotTest.Components;
using CopilotTest.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var dbPath = Path.Combine(builder.Environment.ContentRootPath, "dnd-party.db");
builder.Services.AddDbContext<DndDbContext>(opt =>
    opt.UseSqlite($"Data Source={dbPath}"));

builder.Services.AddScoped<DndApiService>();
builder.Services.AddScoped<CharacterService>();
builder.Services.AddScoped<CombatEngineService>();
builder.Services.AddScoped<PdfImportService>();
builder.Services.AddScoped<ContentService>();

// Play-mode services (dice roller, per-session roll log, live play state)
builder.Services.AddScoped<DiceService>();
builder.Services.AddScoped<RollLogService>();
builder.Services.AddScoped<PlayState>();

var app = builder.Build();

// Ensure DB exists and seed party members on first run
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DndDbContext>();
    db.Database.EnsureCreated();

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
        """);

    // Add columns introduced after first release (SQLite has no ADD COLUMN IF NOT
    // EXISTS, so check the table schema first).
    AddColumnIfMissing(db, "Characters", "SpeciesKey", "TEXT NOT NULL DEFAULT ''");
    AddColumnIfMissing(db, "Characters", "BackgroundKey", "TEXT NOT NULL DEFAULT ''");
    AddColumnIfMissing(db, "Characters", "SubclassKey", "TEXT NOT NULL DEFAULT ''");
    AddColumnIfMissing(db, "Spells", "Source", "TEXT NOT NULL DEFAULT ''");

    var chars = scope.ServiceProvider.GetRequiredService<CharacterService>();
    chars.SeedIfEmpty();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

// Adds a column to a SQLite table only if it isn't already present.
static void AddColumnIfMissing(DndDbContext db, string table, string column, string columnDef)
{
    using var cmd = db.Database.GetDbConnection().CreateCommand();
    if (cmd.Connection!.State != System.Data.ConnectionState.Open) cmd.Connection.Open();
    cmd.CommandText = $"SELECT COUNT(*) FROM pragma_table_info('{table}') WHERE name = '{column}';";
    var exists = Convert.ToInt64(cmd.ExecuteScalar()) > 0;
    if (!exists)
        db.Database.ExecuteSqlRaw($"ALTER TABLE \"{table}\" ADD COLUMN \"{column}\" {columnDef};");
}
