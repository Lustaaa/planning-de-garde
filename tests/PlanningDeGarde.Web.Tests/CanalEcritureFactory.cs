using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Fabrique d'hôte Web réel pour les tests d'intégration du canal d'écriture. Force
/// l'environnement « Testing » → l'amorçage des données de démo est désactivé (Program.cs),
/// le store réel singleton démarre vierge : la projection observée ne reflète que ce que les
/// tests écrivent via le canal (aucune pollution par les périodes/slots de démonstration).
/// Un hôte neuf par test (store frais) garantit l'isolation.
/// </summary>
public sealed class CanalEcritureFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
        => builder.UseEnvironment("Testing");
}
