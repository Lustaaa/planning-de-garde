using System;
using System.IO;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Web.Components.Pages;
using PlanningDeGarde.Web.State;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 26 — Sc.9 (🖥️ @ihm, config foyer) — l'écran de configuration (acteurs + formulaires, en onglets)
/// est habillé « Cocon élevé ». Test de NIVEAU RUNTIME (vraie page câblée à l'API distante réelle) : les
/// onglets portent un habillage tokenisé dédié, la liste d'acteurs est présente. Une garde d'asset vérifie
/// que le style de l'écran est tokenisé (encre/surface via <c>--pdg-*</c>, plus de couleur de texte codée
/// en dur) — condition du rendu correct clair ET sombre. Les data-testid, la pastille de couleur inline
/// des acteurs et tous les flux CRUD restent intacts (Sc.14 le prouve).
/// </summary>
public sealed class FrontWasmConfigFoyerHabilleTests : TestContext
{
    [Fact]
    public void Les_sections_de_config_portent_un_habillage_tokenise_dedie()
    {
        // Given — la vraie page de configuration câblée à l'API distante réelle (store réel).
        using var api = new ApiDistanteFactory();
        Services.AddSingleton(GrilleRuntimeHarness.ClientVers(api));
        Services.AddSingleton(new SessionPlanning());

        // When — l'écran s'affiche.
        var config = RenderComponent<ConfigurationFoyer>();
        config.WaitForElement("[data-testid='panneau-acteurs']", TimeSpan.FromSeconds(10));

        // Then — fusion des sections (hors-sprint) : chaque section porte la classe carte tokenisée dédiée.
        var section = config.Find("[data-testid='panneau-acteurs']");
        Assert.Contains("config-section", section.GetAttribute("class"));
    }

    [Fact]
    public void Le_style_de_l_ecran_est_tokenise_sans_couleur_de_texte_codee_en_dur()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "PlanningDeGarde.slnx")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        var razor = File.ReadAllText(Path.Combine(
            dir!.FullName, "src", "PlanningDeGarde.Web", "Components", "Pages", "ConfigurationFoyer.razor"));

        Assert.Contains("var(--pdg-ink)", razor, StringComparison.Ordinal);
        Assert.Contains("config-section", razor, StringComparison.Ordinal);
        // La couleur de texte codée en dur cassait le thème sombre.
        Assert.DoesNotContain("#333", razor, StringComparison.OrdinalIgnoreCase);
    }
}
