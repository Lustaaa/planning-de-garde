using System;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using PlanningDeGarde.Web.Components.Layout;
using PlanningDeGarde.Web.Components.Pages;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Acceptation de NIVEAU RUNTIME du Sc.5 (🖥️ scénario IHM, <c>@limite</c> — palier 7 « écriture en
/// contexte », retrait du <b>dernier</b> écran de saisie dédié, referme l'épic É12). Le défaut vit dans
/// le câblage Web : tant que le <b>lien-barre</b> de <see cref="PlanningPartage"/>, l'entrée du
/// <see cref="NavMenu"/> ou la <b>route</b> <c>@page "/planning/definir-transfert"</c> subsistent, la
/// saisie du transfert reste atteignable par un écran dédié — contredit « plus aucun écran dédié ne
/// subsiste ». On rend la <b>vraie</b> grille (front WASM) câblée à l'<b>API distante réelle</b>
/// (<see cref="ApiDistanteFactory"/>) et on inspecte les <b>routes réelles</b> de l'assembly Web.
///
/// Ordre impératif : ce retrait s'exécute APRÈS l'acceptation runtime du Sc.1 (vert, commit b674252)
/// qui prouve que la dialog clic-case couvre intégralement l'écran supprimé (borne Risque P1). Le
/// <b>seul</b> chemin de saisie restant est la dialog ouverte depuis une case (Sc.1).
///
/// Anti « vert qui ment » : un bUnit à doublure de composant ne verrait ni la route réellement
/// enregistrée dans l'assembly Web, ni le lien-barre réellement rendu sur l'app câblée.
/// </summary>
public sealed class FrontWasmTransfertAucunEcranDedieTests : TestContext
{
    private const string RouteDediee = "/planning/definir-transfert";

    [Fact]
    public void Should_Ne_proposer_aucun_lien_ni_route_de_saisie_dediee_de_transfert_et_ne_laisser_que_la_dialog_depuis_une_case_When_le_planning_est_affiche_pour_un_parent()
    {
        // Given — la grille réellement câblée à l'API distante, affichée pour un Parent (store réel vierge).
        using var api = new ApiDistanteFactory();
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, GrilleRuntimeHarness.Lundi_29_06_2026);

        // Then (1) — aucun lien « Définir un transfert » vers un écran de saisie dédié n'est présent dans
        // la barre du planning (le seul déclencheur de transfert restant est la 3ᵉ entrée du menu clic-case).
        Assert.Empty(grille.FindAll("a[href*='definir-transfert']"));

        // Then (2) — ni dans le NavMenu (entrée de nav dédiée retirée).
        var navMenu = RenderComponent<NavMenu>();
        Assert.Empty(navMenu.FindAll("a[href*='definir-transfert']"));

        // Then (3) — ouvrir directement la route "/planning/definir-transfert" n'aboutit plus : aucun
        // composant routable de l'assembly Web ne déclare cette route (la page dédiée n'existe plus).
        var routesDediees = typeof(PlanningPartage).Assembly
            .GetTypes()
            .SelectMany(t => t.GetCustomAttributes<RouteAttribute>())
            .Select(r => r.Template)
            .Where(template => string.Equals(template, RouteDediee, StringComparison.OrdinalIgnoreCase))
            .ToList();
        Assert.Empty(routesDediees);
    }
}
