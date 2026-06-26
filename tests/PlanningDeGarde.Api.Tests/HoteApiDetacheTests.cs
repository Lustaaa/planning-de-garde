using System.Xml.Linq;

namespace PlanningDeGarde.Api.Tests;

/// <summary>
/// Test d'architecture (driver structurel du Sc.1) : l'hôte d'API détaché démarre seul, sans
/// référencer le projet front. L'assertion inspecte les <c>ProjectReference</c> du
/// <c>.csproj</c> de l'hôte API (et non les assemblies compilées : une référence de projet
/// inutilisée est élaguée des métadonnées d'assembly, ce qui rendrait le test tautologique).
/// L'assertion = <c>PlanningDeGarde.Web</c> absent des références de projet. C'est le garde-fou
/// anti-régression du découplage « démarre seul ».
/// </summary>
public sealed class HoteApiDetacheTests
{
    [Fact]
    public void Should_Demarrer_l_hote_d_API_sans_charger_le_projet_front_When_on_inspecte_les_dependances_de_l_hote_d_API()
    {
        var csprojApi = LocaliserCsprojApi();
        var doc = XDocument.Load(csprojApi);

        var projetsReferences = doc.Descendants("ProjectReference")
            .Select(e => (e.Attribute("Include")?.Value ?? string.Empty))
            .Select(chemin => Path.GetFileNameWithoutExtension(chemin))
            .ToList();

        Assert.DoesNotContain("PlanningDeGarde.Web", projetsReferences);
    }

    // Remonte depuis le dossier de sortie des tests jusqu'à la racine du dépôt (présence du
    // .slnx), puis localise le .csproj de l'hôte API.
    private static string LocaliserCsprojApi()
    {
        var dossier = new DirectoryInfo(AppContext.BaseDirectory);
        while (dossier is not null && !dossier.GetFiles("*.slnx").Any())
            dossier = dossier.Parent;

        Assert.NotNull(dossier);
        var csproj = Path.Combine(dossier!.FullName, "src", "PlanningDeGarde.Api", "PlanningDeGarde.Api.csproj");
        Assert.True(File.Exists(csproj), $"introuvable : {csproj}");
        return csproj;
    }
}
