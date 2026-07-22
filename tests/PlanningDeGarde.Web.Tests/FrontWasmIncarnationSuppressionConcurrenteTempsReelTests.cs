using System;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Web.State;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Acceptation de NIVEAU RUNTIME / G3 du Sc.5 (🖥️ IHM, <c>@limite</c>, concurrence — driver) : un autre
/// écran <b>supprime l'acteur incarné</b> PENDANT l'incarnation, la suppression <b>se propage en temps
/// réel</b> (SignalR), et l'écran qui incarne <b>revient AUTOMATIQUEMENT à l'identité réelle</b> (D2,
/// extension de la neutralisation par repli — règles 6/18/19). Deux écrans câblés à la <b>MÊME API
/// distante réelle</b> (<see cref="ApiDistanteFactory"/> unique → store singleton partagé, hub SignalR réel
/// commun) : l'écran 2 est la grille (<see cref="PlanningPartage"/>) où le configurateur incarne Nina la
/// nounou ; l'écran 1 est l'écran de configuration (Parent) qui la supprime.
///
/// <para>Then (sur l'écran qui incarne, sans rechargement) : le bandeau « Vous incarnez Nina la nounou »
/// n'est plus affiché, le configurateur est revenu à son identité réelle (menu d'écriture visible) et
/// <b>aucun nom fantôme</b> de « Nina la nounou » ne subsiste dans la vue (invariant sans nom fantôme).</para>
///
/// <para>Convention anti-flake <c>*TempsReel*</c> : établissement DÉTERMINISTE de la connexion via une
/// <b>pompe de diffusion idempotente</b> (le store est déjà muté, <c>NotifierMiseAJour</c> re-poussé en
/// boucle jusqu'à ce qu'un push tombe APRÈS l'établissement du long polling), JAMAIS un délai fixe ;
/// assertion finale sous <see cref="BunitRenderedComponentExtensions.WaitForAssertion"/>. Isolation : un
/// <see cref="TestContext"/> / store / hub propres au test (écran 2 dans un contexte distinct). Anti « vert
/// qui ment » : le bandeau d'incarnation est asserté présent AVANT la suppression ; un bUnit à doublure ne
/// prouverait ni le store partagé, ni le second client SignalR, ni le repli runtime sur diffusion réelle.</para>
/// </summary>
[Collection("SignalRTempsReel")]
public sealed class FrontWasmIncarnationSuppressionConcurrenteTempsReelTests : TestContext
{
    private static readonly DateTime Mardi_16_06_2026 = new(2026, 6, 16);

    [Fact]
    public async Task Should_RevenirAutomatiquementALIdentiteReelle_When_LActeurIncarneEstSupprimeEnTempsReel()
    {
        // Given — UNE seule API distante réelle (store singleton partagé, hub SignalR commun).
        using var api = new ApiDistanteFactory();

        // Écran 2 = la grille réellement câblée, dans un TestContext distinct (DI / client SignalR propres).
        using var ecran2 = new TestContext();
        var grille2 = GrilleRuntimeHarness.RendreGrille(ecran2, api, Mardi_16_06_2026);

        // … le configurateur incarne Nina la nounou (Autre) depuis le sélecteur réel : le bandeau s'affiche.
        grille2.WaitForState(
            () => grille2.FindAll("[data-testid='selecteur-incarnation'] option[value='nounou']").Count == 1,
            TimeSpan.FromSeconds(10));
        grille2.SurDispatcher(() => grille2.Find("[data-testid='selecteur-incarnation']").Change("nounou"));
        grille2.WaitForAssertion(
            () => Assert.Contains("Vous incarnez Nina la nounou",
                grille2.Find("[data-testid='bandeau-incarnation']").TextContent),
            TimeSpan.FromSeconds(10));

        // When — écran 1 = l'écran de configuration (Parent) câblé à la MÊME API : on supprime Nina la nounou
        // (émission via le canal d'écriture HTTP réel → diffusion temps réel sur succès).
        Services.AddSingleton(GrilleRuntimeHarness.ClientVers(api));
        Services.AddSingleton(new SessionPlanning());
        var config = RenderComponent<ConfigurationFoyer>();
        config.WaitForState(
            () => config.FindAll("[data-testid='acteur-foyer'][data-acteur-id='nounou']").Count == 1,
            TimeSpan.FromSeconds(10));
        // Refonte s32 : la suppression se fait dans la MODAL ouverte au crayon de Nina la nounou.
        ConfigActeursModalHarness.OuvrirEdition(this, config, "nounou");
        this.SurDispatcher(() => config.Find("[data-testid='bouton-supprimer']").Click());

        // … re-diffusion de fond idempotente (le store est déjà muté) pour que le push SignalR tombe forcément
        // APRÈS l'établissement de la connexion long polling de l'écran 2, sans dépendre du timing.
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
            // Then — sans rechargement, l'écran qui incarnait revient automatiquement à l'identité réelle :
            // le bandeau « Vous incarnez Nina la nounou » disparaît et aucun nom fantôme « Nina la nounou »
            // ne subsiste dans la vue (assertions de LECTURE seules sous la pompe — pas de clic réentrant).
            grille2.WaitForAssertion(
                () =>
                {
                    Assert.Empty(grille2.FindAll("[data-testid='bandeau-incarnation']"));
                    Assert.DoesNotContain("Nina la nounou", grille2.Markup);
                },
                TimeSpan.FromSeconds(15));
        }
        finally
        {
            diffusionContinue.Cancel();
            await pousseurDeDiffusion;
        }

        // … et la vue est bien restaurée à l'identité réelle (Parent) : le menu d'écriture est de nouveau
        // visible au clic. Vérifié APRÈS l'arrêt de la pompe de diffusion (pas de re-render concurrent →
        // clic non réentrant), sous WaitForAssertion idempotent.
        grille2.WaitForAssertion(
            () =>
            {
                grille2.SurDispatcher(() => GrilleRuntimeHarness.CaseDuJour(grille2, "16/06").Click());
                Assert.NotEmpty(grille2.FindAll("[data-testid='menu-actions-case']"));
            },
            TimeSpan.FromSeconds(10));
    }
}
