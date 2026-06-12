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

var app = builder.Build();

// Ensure DB exists and seed party members on first run
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DndDbContext>();
    db.Database.EnsureCreated();

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
