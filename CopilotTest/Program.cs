using CopilotTest.Services;
using CopilotTest.Components;

var builder = WebApplication.CreateBuilder(args);

// Add Blazor Server
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Monster data service — fully offline, no network required
builder.Services.AddScoped<DndApiService>();

// Combat engine is scoped per circuit (per-user session)
builder.Services.AddScoped<CombatEngineService>();
builder.Services.AddScoped<PdfImportService>();

var app = builder.Build();

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
