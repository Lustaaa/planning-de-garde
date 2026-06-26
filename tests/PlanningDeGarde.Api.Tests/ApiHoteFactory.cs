using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace PlanningDeGarde.Api.Tests;

/// <summary>
/// Fabrique de l'hôte d'API détaché réel (<c>ApiProgram</c>) pour les tests d'intégration du
/// canal d'écriture. Force l'environnement « Testing » (isolation, parité avec l'hôte Web) ;
/// l'hôte API n'amorce aucune donnée de démo, le store réel singleton démarre donc vierge : la
/// projection observée ne reflète que ce que les tests écrivent via le canal. Un hôte neuf par
/// test (store frais) garantit l'isolation.
/// </summary>
public sealed class ApiHoteFactory : WebApplicationFactory<ApiProgram>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
        => builder.UseEnvironment("Testing");
}
