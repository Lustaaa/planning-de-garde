using System;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Web.Components.Pages;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Acceptation de NIVEAU RUNTIME du Sc.5 (🖥️ IHM, <c>@limite</c>) — éditer un acteur <b>hors set de
/// couleurs</b> : le nom suit, la <b>teinte neutre est conservée</b> (renommer ne crée pas de couleur).
/// La grille réelle affiche l'acteur hors set « grand-pere » (libellé « grand-père », teinte neutre grise
/// assumée) le 17/07. Depuis l'écran de configuration réellement câblé (<see cref="ConfigurationFoyer"/>),
/// grand-pere est renommé « Papy Jo » <b>sans lui attribuer de couleur du set</b>. <b>Sans rechargement</b>,
/// la case du 17/07 et l'entrée de légende affichent « Papy Jo », et la teinte reste <b>neutre (grise)</b>
/// car l'acteur n'a pas de couleur dans le set — le renommage ne crée pas de couleur.
///
/// Compose le chemin existant (renommage Sc.1 + repli neutre s07 Sc.5) : aucun code de production neuf.
/// Le baseline « grand-père » + gris est asserté avant le renommage pour que la transition de nom soit
/// réellement observée et que la persistance du neutre soit prouvée (pas un faux-vert). Pas un bUnit à
/// doublure (il ne prouve ni le rendu réel, ni la chaîne d'édition réelle, ni la diffusion temps réel).
/// </summary>
public sealed class FrontWasmConfigHorsSetNeutreTempsReelTests : TestContext
{
    private const string NomInitial = "grand-père";
    private const string NomEdite = "Papy Jo";

    [Fact]
    public async Task Should_Afficher_Papy_Jo_dans_la_case_du_17_07_2026_et_en_legende_en_conservant_la_teinte_neutre_grise_When_l_acteur_hors_set_grand_pere_est_renomme_sans_couleur_attribuee()
    {
        // Given — l'API distante réelle porte une période affectée à l'acteur hors set « grand-pere » le
        // vendredi 17/07/2026 (baseline : libellé « grand-père », teinte neutre grise par repli — il est
        // absent du set de couleurs).
        using var api = new ApiDistanteFactory();
        GrilleRuntimeHarness.SemerPeriode(api, "grand-pere", new DateTime(2026, 7, 17), new DateTime(2026, 7, 17));

        var lundi_13_07_2026 = new DateTime(2026, 7, 13);
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, lundi_13_07_2026);

        // … état initial : la case du 17/07 porte « grand-père » sur la teinte neutre grise.
        var caseInitiale = GrilleRuntimeHarness.CaseDuJour(grille, "17/07");
        Assert.Equal(NomInitial, caseInitiale.QuerySelector("[data-testid='nom-responsable']")!.TextContent.Trim());
        Assert.Equal("gris", caseInitiale.GetAttribute("data-couleur"));

        // When — depuis l'écran de configuration réellement câblé, je renomme grand-pere en « Papy Jo »
        // SANS lui attribuer de couleur (couleur laissée vide → non appliquée) et j'enregistre.
        var config = RenderComponent<ConfigurationFoyer>();

        // … garde déterministe : attendre la fin de l'énumération asynchrone des acteurs (GET HTTP réel)
        // avant d'interagir avec le select, sinon un re-render intercalé invalide le handler d'événement
        // (UnknownEventHandlerId) — standard anti-flake *TempsReel* déjà appliqué par les tests config frères.
        config.WaitForState(
            () => config.FindAll("[data-testid='acteur-foyer']").Count > 0,
            TimeSpan.FromSeconds(10));

        this.SurDispatcher(() => config.Find("select.form-select").Change("grand-pere"));
        this.SurDispatcher(() => config.Find("[data-testid='champ-nom']").Change(NomEdite));
        this.SurDispatcher(() => config.Find("form").Submit());

        // … re-diffusion de fond idempotente (le store est déjà muté) pour que le push SignalR tombe
        // après l'établissement de la connexion long polling vers le TestServer, sans dépendre du timing.
        var notificateur = api.Services.GetRequiredService<INotificateurPlanning>();
        using var diffusionContinue = new CancellationTokenSource();
        var pousseurDeDiffusion = Task.Run(async () =>
        {
            while (!diffusionContinue.IsCancellationRequested)
            {
                notificateur.NotifierMiseAJour();
                try { await Task.Delay(150, diffusionContinue.Token); }
                catch (TaskCanceledException) { break; }
            }
        });

        try
        {
            // Then — sans rechargement, la case du 17/07 et l'entrée de légende affichent « Papy Jo », et
            // la teinte reste NEUTRE (grise) : le renommage n'a pas créé de couleur dans le set.
            grille.WaitForAssertion(
                () =>
                {
                    var caseEditee = GrilleRuntimeHarness.CaseDuJour(grille, "17/07");
                    Assert.Equal(NomEdite, caseEditee.QuerySelector("[data-testid='nom-responsable']")!.TextContent.Trim());
                    Assert.Equal("gris", caseEditee.GetAttribute("data-couleur")); // teinte neutre conservée

                    var entree = Assert.Single(grille.FindAll("[data-testid='legende-entree']"));
                    Assert.Equal(NomEdite, entree.QuerySelector(".legende-nom")!.TextContent.Trim());
                    Assert.Equal("gris", entree.GetAttribute("data-couleur"));
                },
                TimeSpan.FromSeconds(15));
        }
        finally
        {
            diffusionContinue.Cancel();
            await pousseurDeDiffusion;
        }
    }
}
