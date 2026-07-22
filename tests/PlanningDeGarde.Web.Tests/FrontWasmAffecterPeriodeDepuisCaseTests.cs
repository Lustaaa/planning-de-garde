using System;
using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Acceptation de NIVEAU RUNTIME du Sc.2 (🖥️ scénario IHM, <c>@nominal</c> — palier 7 « écriture en
/// contexte ») — le comportement neuf vit dans le <c>.razor</c> : cliquer une case ouvre le <b>menu
/// d'actions</b> de la case (décision CP : un seul déclencheur, deux entrées), dont « Affecter une
/// période » ouvre la dialog pré-remplie sur la date de la case ; l'affectation validée <b>colore et
/// nomme</b> la case et <b>agrège la légende</b>. On rend la <b>vraie</b> grille
/// <see cref="Web.Components.Planning.PlanningPartage"/> (front WASM) câblée à une <b>API distante réelle</b>
/// (<see cref="ApiDistanteFactory"/>, store réel, projection réelle <see cref="GrilleAgendaQuery"/>,
/// palette + référentiel <b>réels</b> du foyer : <c>parent-a</c> → « Alice » / « bleu »). Aucun handler ni
/// règle backend neuf : réutilisation de la commande <c>AffecterPeriode</c> et de l'endpoint
/// <c>POST /api/periodes</c>.
///
/// Anti « vert qui ment » : si le menu n'ouvre pas la dialog d'affectation, si l'affectation ne transite
/// pas jusqu'au store distant, ou si la projection ne résout pas nom/couleur/légende, l'observable reste
/// vide → rouge. Un bUnit à doublure ne verrait ni le câblage distant ni la résolution réelle du foyer.
/// </summary>
public sealed class FrontWasmAffecterPeriodeDepuisCaseTests : TestContext
{
    // Mercredi 17/06/2026 : la case cliquée. Référence « aujourd'hui » au 17/06 → fenêtre démarrant au
    // lundi 15/06, qui couvre le mercredi 17/06.
    private static readonly DateTime Mercredi_17_06_2026 = new(2026, 6, 17);

    [Fact]
    public void Should_Colorer_et_nommer_la_case_du_mercredi_17_06_2026_au_responsable_Alice_When_un_parent_affecte_une_periode_via_la_dialog_ouverte_depuis_cette_case()
    {
        // Given — la grille réellement câblée à l'API distante, affichée pour un Parent (store réel vierge),
        // fenêtre couvrant le mercredi 17/06/2026.
        using var api = new ApiDistanteFactory();
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Mercredi_17_06_2026);

        // … la case du mercredi 17/06 n'affiche aucun responsable, et la légende est vide.
        Assert.Null(GrilleRuntimeHarness.CaseDuJour(grille, "17/06").QuerySelector("[data-testid='nom-responsable']"));
        Assert.Empty(grille.FindAll("[data-testid='legende-entree']"));

        // When — un Parent clique la case du mercredi 17/06 → le menu d'actions s'ouvre → il choisit
        // « Affecter une période » → la dialog s'ouvre. Ouverture idempotente sous WaitForAssertion :
        // robuste aux re-renders async (connexion du hub SignalR du harnais) sous charge parallèle.
        grille.WaitForAssertion(
            () =>
            {
                this.SurDispatcher(() => GrilleRuntimeHarness.CaseDuJour(grille, "17/06").Click());
                this.SurDispatcher(() => grille.Find("[data-testid='action-affecter-periode']").Click());
                Assert.NotEmpty(grille.FindAll("[data-testid='dialog-affecter-periode']"));
            },
            TimeSpan.FromSeconds(10));

        // … il choisit « Alice » (id stable parent-a, pré-rempli sur la date de la case) et valide.
        this.SurDispatcher(() => grille.Find("[data-testid='champ-responsable']").Change("parent-a"));
        this.SurDispatcher(() => grille.Find("[data-testid='dialog-affecter-periode'] form").Submit());

        // Then — la dialog se ferme ET la case du 17/06 affiche « Alice », prend sa couleur propre (bleu),
        // et la légende agrège « Alice » avec sa couleur — relu depuis le store réel de l'API distante.
        grille.WaitForAssertion(
            () =>
            {
                Assert.Empty(grille.FindAll("[data-testid='dialog-affecter-periode']"));
                var caseMercredi = GrilleRuntimeHarness.CaseDuJour(grille, "17/06");
                Assert.Equal("Alice", caseMercredi.QuerySelector("[data-testid='nom-responsable']")!.TextContent.Trim());
                Assert.Equal("bleu", caseMercredi.GetAttribute("data-couleur"));

                var entree = grille.FindAll("[data-testid='legende-entree']").Single();
                Assert.Equal("Alice", entree.QuerySelector(".legende-nom")!.TextContent.Trim());
                Assert.Equal("bleu", entree.GetAttribute("data-couleur"));
            },
            TimeSpan.FromSeconds(10));

        // … et l'affectation a réellement transité jusqu'au store de l'API distante (rempart anti
        // vert-qui-ment) : observée via la projection réelle, à la semaine du lundi 15/06/2026.
        using var scope = api.Services.CreateScope();
        var projection = scope.ServiceProvider.GetRequiredService<GrilleAgendaQuery>();
        var grilleStore = projection.Projeter(new DateOnly(2026, 6, 15));
        var caseStore = grilleStore.Jours.Single(j => j.Date == new DateOnly(2026, 6, 17));
        Assert.Equal("Alice", caseStore.NomResponsable);
        Assert.Equal("bleu", caseStore.CouleurResponsable);
    }
}
