extern alias api;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Fabrique de l'hôte d'API <b>détaché</b> réel (<c>ApiProgram</c>) jouant l'<b>API distante</b>
/// que le front WASM consomme (Sc.2). Force l'environnement « Testing » (store réel singleton
/// vierge, parité avec l'hôte Web). Un hôte neuf par test garantit l'isolation. C'est la cible
/// distante réelle : l'écriture du front doit y transiter et son store y être observé.
/// </summary>
public sealed class ApiDistanteFactory : WebApplicationFactory<api::ApiProgram>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
        => builder.UseEnvironment("Testing");
}
