using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Web.Components.Pages;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 20 — Sc.3 (🖥️ @ihm — acceptation de NIVEAU RUNTIME, iso-fonctionnel) : sous l'onglet
/// « Acteurs » (actif par défaut) de l'écran de configuration réellement câblé
/// (<see cref="ConfigurationFoyer"/>, API distante réelle, store réel, diffusion SignalR réelle), le
/// CRUD acteurs existant — <b>éditer</b> (renommer + recolorier), <b>ajouter</b>, <b>supprimer</b> —
/// aboutit <b>exactement comme avant la refonte</b> (aucun handler neuf) et la <b>liste relue</b> + la
/// <b>grille / légende</b> suivent immédiatement, sans rechargement. Rempart de non-régression du
/// réagencement en onglets (Sc.2) : si le panneau « Acteurs » n'avait pas correctement recâblé les
/// formulaires d'édition / d'ajout / de suppression, l'une de ces écritures échouerait → rouge.
///
/// « Grille et légende relues immédiatement » est prouvé ici pour le chemin édition (renommer +
/// recolorier propagé à la grille par SignalR) ; les chemins ajout / suppression sont observés sur la
/// liste relue de l'écran (les volets grille dédiés restent couverts par leurs tests frères, qui
/// opèrent sur le même onglet Acteurs actif par défaut).
/// </summary>
public sealed class FrontWasmConfigOngletActeursCrudIsoFonctionnelTempsReelTests : TestContext
{
    private static string? NomLigne(AngleSharp.Dom.IElement li)
        => li.QuerySelector(".acteur-nom")?.TextContent.Trim();

    [Fact]
    public async Task Should_Editer_ajouter_et_supprimer_un_acteur_iso_fonctionnellement_avec_liste_et_grille_relues_When_je_fais_le_CRUD_depuis_l_onglet_Acteurs_actif_par_defaut()
    {
        // Given — parent-a (« Alice », bleu) garde le 14/07 : la grille réellement câblée à l'API distante
        // affiche « Alice » (bleu) dans la case du 14/07 et en légende.
        using var api = new ApiDistanteFactory();
        GrilleRuntimeHarness.SemerPeriode(api, "parent-a", new DateTime(2026, 7, 14), new DateTime(2026, 7, 14));

        var grille = GrilleRuntimeHarness.RendreGrille(this, api, new DateTime(2026, 7, 13));
        Assert.Equal("Alice", GrilleRuntimeHarness.CaseDuJour(grille, "14/07")
            .QuerySelector("[data-testid='nom-responsable']")!.TextContent.Trim());

        // … l'écran de configuration réellement câblé énumère ses acteurs DEPUIS LE STORE (GET HTTP réel).
        var config = RenderComponent<ConfigurationFoyer>();
        config.WaitForState(
            () => config.FindAll("[data-testid='acteur-foyer']").Count > 0,
            TimeSpan.FromSeconds(10));

        // … l'onglet « Acteurs » est actif par défaut : tout le CRUD s'y exerce sans changer d'onglet.
        Assert.Equal("true", config.Find("[data-testid='onglet-acteurs']").GetAttribute("aria-selected"));

        // Diffusion temps réel poussée en boucle de fond (idempotente) pour que les push SignalR tombent
        // après l'établissement de la connexion (long polling), sans dépendre du timing (anti-flake *TempsReel*).
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
            // When (édition) — je renomme parent-a en « Alicia » ET le recolorie en vert, puis j'enregistre
            // (canal d'écriture HTTP réel : POST /api/canal/editer-acteur).
            config.Find("[data-testid='selecteur-acteur-edition']").Change("parent-a");
            config.Find("[data-testid='champ-nom']").Change("Alicia");
            config.Find("[data-testid='champ-couleur']").Change("vert");
            config.Find("form").Submit(); // le formulaire d'édition est le premier <form> du panneau Acteurs

            // Then (édition) — la grille suit immédiatement, sans rechargement : la case du 14/07 passe à
            // « Alicia » en vert (l'identifiant parent-a est inchangé — nom ET couleur résolus sur l'id stable).
            grille.WaitForAssertion(
                () =>
                {
                    var caseAlicia = GrilleRuntimeHarness.CaseDuJour(grille, "14/07");
                    Assert.Equal("Alicia", caseAlicia.QuerySelector("[data-testid='nom-responsable']")!.TextContent.Trim());
                    Assert.Equal("vert", caseAlicia.GetAttribute("data-couleur"));
                    var entree = Assert.Single(grille.FindAll("[data-testid='legende-entree']"));
                    Assert.Equal("Alicia", entree.QuerySelector(".legende-nom")!.TextContent.Trim());
                    Assert.Equal("vert", entree.GetAttribute("data-couleur"));
                },
                TimeSpan.FromSeconds(15));

            // NB (iso-fonctionnel) : l'édition rafraîchit la grille partagée par diffusion (SignalR) mais ne
            // ré-énumère pas la liste locale de l'écran (comportement d'avant la refonte : seuls l'ajout et la
            // suppression appellent RechargerActeurs). On n'asserte donc pas la liste locale sur l'édition.

            // When (ajout) — j'ajoute « Carla » en rose (canal d'écriture réel : POST /api/canal/ajouter-acteur).
            config.Find("[data-testid='champ-nom-ajout']").Change("Carla");
            config.Find("[data-testid='champ-couleur-ajout']").Change("rose");
            config.Find("#form-ajout").Submit();

            // Then (ajout) — sans rechargement, la liste relue de l'écran contient désormais « Carla ».
            config.WaitForAssertion(
                () => Assert.Contains(config.FindAll("[data-testid='acteur-foyer']"), li => NomLigne(li) == "Carla"),
                TimeSpan.FromSeconds(10));

            // When (suppression) — je supprime « Carla » via son bouton supprimer (POST /api/canal/supprimer-acteur).
            config.FindAll("[data-testid='acteur-foyer']")
                .Single(li => NomLigne(li) == "Carla")
                .QuerySelector("[data-testid='bouton-supprimer']")!
                .Click();

            // Then (suppression) — sans rechargement, « Carla » quitte la liste relue, et un accusé s'affiche.
            config.WaitForAssertion(
                () => Assert.DoesNotContain(config.FindAll("[data-testid='acteur-foyer']"), li => NomLigne(li) == "Carla"),
                TimeSpan.FromSeconds(10));
            Assert.Contains("Acteur supprimé", config.Find("[data-testid='accuse-suppression']").TextContent);
        }
        finally
        {
            diffusionContinue.Cancel();
            await pousseurDeDiffusion;
        }
    }
}
