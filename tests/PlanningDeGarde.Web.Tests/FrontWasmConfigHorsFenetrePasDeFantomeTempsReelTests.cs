using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Acceptation de NIVEAU RUNTIME du Sc.6 (🖥️ IHM, <c>@limite</c>) — éditer un acteur <b>absent de la
/// fenêtre affichée</b> : l'édition est <b>confirmée à l'écran de configuration</b>, mais la grille de la
/// fenêtre courante reste <b>inchangée</b> et la légende n'introduit <b>aucune entrée fantôme</b> (la
/// légende = présents dans la fenêtre, s07 Sc.3). La grille affiche la fenêtre de 5 semaines à partir du
/// lundi 13/07/2026 avec un <b>témoin</b> présent (parent-a « Alice » le 14/07) ; parent-c n'a aucune
/// période dans cette fenêtre. Depuis l'écran de config réellement câblé (<see cref="ConfigurationFoyer"/>),
/// parent-c est renommé « Mathilde ». <b>Sans rechargement</b> : confirmation à l'écran, légende toujours
/// limitée à « Alice » (aucune entrée « Mathilde »/parent-c), case témoin inchangée.
///
/// Compose le chemin existant (confirmation d'édition d'un id connu, Sc.1 ; légende-présents s07 Sc.3) :
/// aucun code de production neuf. Le témoin parent-a rend l'absence d'entrée fantôme <b>non triviale</b>
/// (la légende contient parent-a, pas parent-c). Pas un bUnit à doublure (il ne prouve ni la confirmation
/// réelle à l'écran, ni le rendu réel de la légende, ni la chaîne d'édition réelle).
/// </summary>
[Collection("SignalRTempsReel")]
public sealed class FrontWasmConfigHorsFenetrePasDeFantomeTempsReelTests : TestContext
{
    [Fact]
    public async Task Should_Confirmer_le_renommage_de_parent_c_en_Mathilde_a_l_ecran_de_configuration_sans_modifier_la_grille_ni_introduire_d_entree_de_legende_When_parent_c_n_a_aucune_periode_dans_la_fenetre_de_cinq_semaines()
    {
        // Given — la grille réelle affiche la fenêtre de 5 semaines à partir du lundi 13/07/2026 avec un
        // témoin présent (parent-a « Alice » le 14/07) ; parent-c n'a AUCUNE période dans cette fenêtre.
        using var api = new ApiDistanteFactory();
        GrilleRuntimeHarness.SemerPeriode(api, "parent-a", new DateTime(2026, 7, 14), new DateTime(2026, 7, 14));

        var lundi_13_07_2026 = new DateTime(2026, 7, 13);
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, lundi_13_07_2026);

        // … état initial : la légende liste exactement le témoin « Alice » (parent-c absent).
        var entreeInitiale = Assert.Single(grille.FindAll("[data-testid='legende-entree']"));
        Assert.Equal("Alice", entreeInitiale.QuerySelector(".legende-nom")!.TextContent.Trim());

        // When — depuis l'écran de configuration réellement câblé, je renomme parent-c en « Mathilde »
        // (acteur sans période dans la fenêtre) et j'enregistre (émission via le canal d'écriture réel).
        var config = RenderComponent<ConfigurationFoyer>();

        // … garde déterministe : attendre la fin de l'énumération asynchrone des acteurs (GET HTTP réel)
        // avant d'interagir avec le select, sinon un re-render intercalé invalide le handler d'événement
        // (UnknownEventHandlerId) — standard anti-flake *TempsReel* déjà appliqué par les tests config frères.
        config.WaitForState(
            () => config.FindAll("[data-testid='acteur-foyer']").Count > 0,
            TimeSpan.FromSeconds(10));

        // Refonte s32 : l'édition passe par la MODAL ouverte au crayon (plus de sélecteur d'acteur inline).
        ConfigActeursModalHarness.OuvrirEdition(this, config, "parent-c");
        this.SurDispatcher(() => config.Find("[data-testid='champ-nom']").Change("Mathilde"));
        this.SurDispatcher(() => config.Find("#form-edition").Submit());

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
            // Then (1) — l'édition a abouti côté config (refonte s32) : la modal se ferme et la table relue
            // reflète « Mathilde » sur l'identifiant parent-c (id connu + nom non vide, indépendamment de la
            // présence en fenêtre).
            config.WaitForAssertion(
                () =>
                {
                    Assert.Empty(config.FindAll("[data-testid='dialog-acteur']"));
                    Assert.Equal("Mathilde", config.FindAll("[data-testid='acteur-foyer']")
                        .Single(li => li.GetAttribute("data-acteur-id") == "parent-c")
                        .QuerySelector(".acteur-nom")!.TextContent.Trim());
                },
                TimeSpan.FromSeconds(15));

            // Then (2) — après diffusion, la grille de la fenêtre courante reste inchangée et la légende ne
            // fait apparaître AUCUNE entrée fantôme pour parent-c : elle reste limitée au témoin « Alice »,
            // sans « Mathilde » (pas d'entrée tant qu'aucune période ne porte parent-c dans la fenêtre).
            grille.WaitForAssertion(
                () =>
                {
                    var noms = grille.FindAll("[data-testid='legende-entree']")
                        .Select(e => e.QuerySelector(".legende-nom")!.TextContent.Trim())
                        .ToList();
                    var entree = Assert.Single(noms);
                    Assert.Equal("Alice", entree);
                    Assert.DoesNotContain("Mathilde", noms);

                    // … et la case témoin du 14/07 reste « Alice » (grille inchangée).
                    Assert.Equal("Alice", GrilleRuntimeHarness.CaseDuJour(grille, "14/07")
                        .QuerySelector("[data-testid='nom-responsable']")!.TextContent.Trim());
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
