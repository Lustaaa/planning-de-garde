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
/// Acceptation de NIVEAU RUNTIME du Sc.2 (🖥️ IHM, <c>@nominal</c>) — VOLET RUNTIME : depuis
/// l'<b>écran de configuration réellement câblé</b> (<see cref="ConfigurationFoyer"/>), un parent
/// recolorie « parent-b » de orange en <b>violet</b> et enregistre. <b>Sans rechargement</b>, la grille
/// partagée (<see cref="PlanningPartage"/>) réellement câblée à l'<b>API distante réelle</b> (store réel
/// mutable <c>ConfigurationFoyerEnMemoire</c> réalisant <c>IPaletteCouleurs</c>, projection
/// <c>GrilleAgendaQuery</c> inchangée, diffusion SignalR réelle) restitue la <b>case du 15/07/2026</b>
/// en violet <b>en conservant le libellé « Bruno »</b> ET l'entrée de légende « Bruno » passe au violet —
/// l'identifiant « parent-b » est inchangé (la couleur reste résolue sur l'id stable, et le nom n'est
/// pas touché par un recoloriage seul).
///
/// Chemin réel exercé : l'écran émet la commande via le <b>canal d'écriture HTTP réel</b>
/// (<c>POST /api/canal/editer-acteur</c> avec la couleur seule, règle 27) → le handler recolorie le store
/// réel et déclenche la diffusion → le front, connecté au hub réel (redirigé vers le TestServer),
/// <b>re-projette sans second render</b>. Anti « vert qui ment » : si <c>IPaletteCouleurs</c> pointait
/// encore le set statique, la case resterait orange → rouge. Un bUnit à doublure ne prouverait ni la DI
/// réelle, ni le chemin HTTP d'écriture, ni la diffusion temps réel.
/// </summary>
public sealed class FrontWasmConfigRecolorierActeurTempsReelTests : TestContext
{
    [Fact]
    public async Task Should_Rendre_la_case_du_15_07_2026_en_violet_et_passer_l_entree_de_legende_au_violet_sans_recharger_en_conservant_le_libelle_Bruno_et_l_identifiant_parent_b_When_l_acteur_parent_b_est_recolorie_de_orange_en_violet()
    {
        // Given — la grille réellement câblée affiche, à la semaine du lundi 13/07/2026, une période
        // affectée à parent-b (« Bruno », orange) : la case du mercredi 15/07 est orange et porte « Bruno ».
        using var api = new ApiDistanteFactory();
        GrilleRuntimeHarness.SemerPeriode(api, "parent-b", new DateTime(2026, 7, 15), new DateTime(2026, 7, 15));

        var lundi_13_07_2026 = new DateTime(2026, 7, 13);
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, lundi_13_07_2026);

        // … état initial : la case du 15/07 et la légende portent « Bruno » en orange.
        var caseInitiale = GrilleRuntimeHarness.CaseDuJour(grille, "15/07");
        Assert.Equal("Bruno", caseInitiale.QuerySelector("[data-testid='nom-responsable']")!.TextContent.Trim());
        Assert.Equal("orange", caseInitiale.GetAttribute("data-couleur"));
        var entreeInitiale = Assert.Single(grille.FindAll("[data-testid='legende-entree']"));
        Assert.Equal("orange", entreeInitiale.GetAttribute("data-couleur"));

        // When — depuis l'écran de configuration réellement câblé, je recolorie parent-b en « violet »
        // (couleur seule, nom laissé vide) et j'enregistre (émission via le canal d'écriture HTTP réel).
        var config = RenderComponent<ConfigurationFoyer>();
        config.Find("select.form-select").Change("parent-b");
        config.Find("[data-testid='champ-couleur']").Change("violet");
        config.Find("form").Submit();

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
            // du 15/07 devient violet en conservant le libellé « Bruno », et la légende « Bruno » passe au
            // violet (l'identifiant parent-b est inchangé : la couleur reste résolue sur l'id, le nom intact).
            grille.WaitForAssertion(
                () =>
                {
                    var caseViolet = GrilleRuntimeHarness.CaseDuJour(grille, "15/07");
                    Assert.Equal("violet", caseViolet.GetAttribute("data-couleur"));
                    Assert.Equal("Bruno", caseViolet.QuerySelector("[data-testid='nom-responsable']")!.TextContent.Trim());

                    var entree = Assert.Single(grille.FindAll("[data-testid='legende-entree']"));
                    Assert.Equal("violet", entree.GetAttribute("data-couleur"));
                    Assert.Equal("Bruno", entree.QuerySelector(".legende-nom")!.TextContent.Trim());
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
