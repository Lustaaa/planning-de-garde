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
/// Acceptation de NIVEAU RUNTIME du Sc.1 (🖥️ IHM, <c>@nominal</c>) — VOLET RUNTIME : depuis
/// l'<b>écran de configuration réellement câblé</b> (<see cref="ConfigurationFoyer"/>), un parent
/// renomme « parent-a » de « Alice » en « Alicia » et enregistre. <b>Sans rechargement</b>, la grille
/// partagée (<see cref="PlanningPartage"/>) réellement câblée à l'<b>API distante réelle</b> (store
/// réel mutable <c>ConfigurationFoyerEnMemoire</c>, projection <c>GrilleAgendaQuery</c> inchangée,
/// diffusion SignalR réelle) restitue « Alicia » dans la <b>case du 14/07/2026</b> ET dans la
/// <b>légende</b>, en conservant la couleur bleue — preuve que l'identifiant « parent-a » est inchangé
/// (la résolution reste sur l'id stable, règle 18).
///
/// Chemin réel exercé : l'écran émet la commande via le <b>canal d'écriture HTTP réel</b>
/// (<c>POST /api/canal/editer-acteur</c>, règle 27, aucune vue n'écrit le domaine en direct) → le
/// handler mute le store réel et déclenche la diffusion → le front, connecté au hub réel (redirigé vers
/// le TestServer), <b>re-projette sans second render</b>. Anti « vert qui ment » : si le binding lecture
/// pointait encore le dictionnaire statique, ou si l'endpoint d'écriture / la diffusion manquaient, la
/// case resterait « Alice » → rouge. Un bUnit à doublure ne prouverait ni la DI réelle, ni le chemin
/// HTTP d'écriture, ni la diffusion temps réel.
/// </summary>
[Collection("SignalRTempsReel")]
public sealed class FrontWasmConfigRenommerActeurTempsReelTests : TestContext
{
    [Fact]
    public async Task Should_Afficher_Alicia_dans_la_case_du_14_07_2026_et_dans_l_entree_de_legende_sans_recharger_la_page_et_conserver_l_identifiant_parent_a_When_l_acteur_parent_a_est_renomme_de_Alice_en_Alicia_depuis_l_ecran_de_configuration()
    {
        // Given — la grille réellement câblée affiche, à la semaine du lundi 13/07/2026, une période
        // affectée à parent-a (« Alice », bleu) : la case du mardi 14/07 porte « Alice ».
        using var api = new ApiDistanteFactory();
        GrilleRuntimeHarness.SemerPeriode(api, "parent-a", new DateTime(2026, 7, 14), new DateTime(2026, 7, 14));

        var lundi_13_07_2026 = new DateTime(2026, 7, 13);
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, lundi_13_07_2026);

        // … état initial : la case du 14/07 et la légende portent « Alice » (en bleu).
        Assert.Equal("Alice", GrilleRuntimeHarness.CaseDuJour(grille, "14/07")
            .QuerySelector("[data-testid='nom-responsable']")!.TextContent.Trim());
        var entreeInitiale = Assert.Single(grille.FindAll("[data-testid='legende-entree']"));
        Assert.Equal("Alice", entreeInitiale.QuerySelector(".legende-nom")!.TextContent.Trim());

        // When — depuis l'écran de configuration réellement câblé, je renomme parent-a en « Alicia »
        // et j'enregistre (émission via le canal d'écriture HTTP réel de l'API distante).
        var config = RenderComponent<ConfigurationFoyer>();

        // … garde déterministe : attendre la fin de l'énumération asynchrone des acteurs (GET HTTP réel)
        // avant d'interagir avec le select, sinon un re-render intercalé invalide le handler d'événement
        // (UnknownEventHandlerId) — standard anti-flake *TempsReel* déjà appliqué par les tests config frères.
        config.WaitForState(
            () => config.FindAll("[data-testid='acteur-foyer']").Count > 0,
            TimeSpan.FromSeconds(10));

        // Refonte s32 : l'édition passe par la MODAL ouverte au crayon (plus de sélecteur d'acteur inline).
        ConfigActeursModalHarness.OuvrirEdition(this, config, "parent-a");
        this.SurDispatcher(() => config.Find("[data-testid='champ-nom']").Change("Alicia"));
        this.SurDispatcher(() => config.Find("#form-edition").Submit());

        // … la connexion SignalR du front (long polling vers le TestServer) s'établit de façon
        // asynchrone : on ré-émet la diffusion en boucle de fond (idempotente — le store est déjà muté)
        // pour qu'un push tombe forcément APRÈS l'établissement de la connexion, sans dépendre du timing.
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
            // Then — sans rechargement (même instance rendue, aucun second render de la grille), la case
            // du 14/07 affiche « Alicia » et la légende affiche « Alicia », toujours en bleu (l'identifiant
            // parent-a est inchangé : la couleur reste résolue sur l'id stable).
            grille.WaitForAssertion(
                () =>
                {
                    var caseAlicia = GrilleRuntimeHarness.CaseDuJour(grille, "14/07");
                    Assert.Equal("Alicia", caseAlicia.QuerySelector("[data-testid='nom-responsable']")!.TextContent.Trim());
                    Assert.Equal("bleu", caseAlicia.GetAttribute("data-couleur"));

                    var entree = Assert.Single(grille.FindAll("[data-testid='legende-entree']"));
                    Assert.Equal("Alicia", entree.QuerySelector(".legende-nom")!.TextContent.Trim());
                    Assert.Equal("bleu", entree.GetAttribute("data-couleur"));
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
