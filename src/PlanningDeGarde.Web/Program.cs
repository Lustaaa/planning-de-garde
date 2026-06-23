using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSignalR();

// Application + Infrastructure (persistance en mémoire, SignalR réel, use cases).
builder.Services.AjouterPlanningDeGarde();

// État de session de consultation (rôle, enfant affiché) — par circuit Blazor.
builder.Services.AddScoped<PlanningDeGarde.Web.State.SessionPlanning>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapHub<PlanningHub>("/hubs/planning");

app.Run();
