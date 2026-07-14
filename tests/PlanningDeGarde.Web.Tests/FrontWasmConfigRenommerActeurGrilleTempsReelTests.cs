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
/// Acceptation de NIVEAU RUNTIME du Sc.2 (🖥️ IHM, <c>@nominal</c>) — CARACTÉRISATION runtime : depuis
/// l'<b>écran de configuration réellement câblé</b> (<see cref="ConfigurationFoyer"/>), un parent renomme
/// « parent-a » de « Alice » en « Alicia » et enregistre. <b>Sans rechargement</b>, la grille partagée
/// (<see cref="PlanningPartage"/>) câblée à l'<b>API distante réelle</b> (store réel mutable
/// <c>ConfigurationFoyerEnMemoire</c> — désormais réalisé par l'adaptateur durable, port inchangé —,
/// projection <c>GrilleAgendaQuery</c> inchangée, diffusion SignalR réelle) restitue « Alicia » dans
/// <b>les cinq cases du 1er au 5 juin 2026</b> ET dans la <b>légende</b>, en conservant la couleur bleue
/// — preuve que l'identifiant « parent-a » est inchangé (la résolution reste sur l'id stable, règle 18).
///
/// Caractérisation (early green ATTENDU, routé tdd-analyse) : le geste « renommer » est livré @vert au
/// s08 (handler <c>EditerActeur</c> + diffusion) et la re-projection case+légende par s07
/// (<c>GrilleAgendaQuery</c> inchangé). Aucun code neuf n'est forcé ici ; ce test est un FILET de
/// non-régression runtime <b>plus fort</b> que le s08 single-day : il prouve que <b>les cinq cases</b>
/// d'une période multi-jours (1er→5 juin) suivent le renommage en une seule re-projection, sur l'app
/// réellement câblée. La nouveauté de durabilité du sprint 09 est portée par le pivot Sc.3.
///
/// Anti « vert qui ment » : si le binding de lecture pointait encore un dictionnaire statique, ou si
/// l'endpoint d'écriture / la diffusion manquaient, les cases resteraient « Alice » → rouge. Un bUnit à
/// doublure ne prouverait ni la DI réelle, ni le chemin HTTP d'écriture, ni la diffusion temps réel.
/// </summary>
[Collection("SignalRTempsReel")]
public sealed class FrontWasmConfigRenommerActeurGrilleTempsReelTests : TestContext
{
    private static readonly string[] JoursDu1erAu5Juin = { "01/06", "02/06", "03/06", "04/06", "05/06" };

    [Fact]
    public async Task Should_Afficher_Alicia_en_bleu_dans_les_cases_du_1er_au_5_juin_et_dans_la_legende_sans_recharger_la_page_When_un_parent_renomme_Alice_en_Alicia_depuis_l_ecran_de_configuration()
    {
        // Given — la grille réellement câblée affiche la semaine du lundi 1er juin 2026 ; parent-a
        // (« Alice », bleu) garde Léa du 1er au 5 juin (période multi-jours) : les cinq cases portent « Alice ».
        using var api = new ApiDistanteFactory();
        GrilleRuntimeHarness.SemerPeriode(api, "parent-a", new DateTime(2026, 6, 1), new DateTime(2026, 6, 5));

        var lundi_01_06_2026 = new DateTime(2026, 6, 1);
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, lundi_01_06_2026);

        // … état initial : les cinq cases du 1er au 5 juin portent « Alice » (bleu), et la légende affiche
        // une unique entrée « Alice » (bleu), dédoublonnée par identifiant.
        foreach (var jour in JoursDu1erAu5Juin)
        {
            var caseAlice = GrilleRuntimeHarness.CaseDuJour(grille, jour);
            Assert.Equal("Alice", caseAlice.QuerySelector("[data-testid='nom-responsable']")!.TextContent.Trim());
        }
        var entreeInitiale = Assert.Single(grille.FindAll("[data-testid='legende-entree']"));
        Assert.Equal("Alice", entreeInitiale.QuerySelector(".legende-nom")!.TextContent.Trim());

        // When — depuis l'écran de configuration réellement câblé, je renomme parent-a en « Alicia »
        // et j'enregistre (émission via le canal d'écriture HTTP réel de l'API distante).
        var config = RenderComponent<ConfigurationFoyer>();

        // … l'écran de configuration énumère ses acteurs depuis le store (GET HTTP réel asynchrone) ; on
        // attend que ce chargement ait fini de re-rendre l'écran avant d'interagir, sinon un re-render
        // intercalé invaliderait le handler du select (état réaliste : l'écran a fini de charger).
        config.WaitForState(
            () => config.FindAll("[data-testid='acteur-foyer']").Count > 0,
            TimeSpan.FromSeconds(10));

        // Refonte s32 : l'édition passe par la MODAL ouverte au crayon (plus de sélecteur d'acteur inline).
        ConfigActeursModalHarness.OuvrirEdition(this, config, "parent-a");
        this.SurDispatcher(() => config.Find("[data-testid='champ-nom']").Change("Alicia"));
        this.SurDispatcher(() => config.Find("#form-edition").Submit());

        // … la connexion SignalR du front (long polling vers le TestServer) s'établit de façon asynchrone :
        // on ré-émet la diffusion en boucle de fond (idempotente — le store est déjà muté) pour qu'un push
        // tombe forcément APRÈS l'établissement de la connexion, sans dépendre du timing.
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
            // Then — sans rechargement (même instance rendue, aucun second render manuel de la grille), les
            // cinq cases du 1er au 5 juin affichent « Alicia » et la légende affiche « Alicia », toujours en
            // bleu (l'identifiant parent-a est inchangé : la couleur reste résolue sur l'id stable).
            grille.WaitForAssertion(
                () =>
                {
                    foreach (var jour in JoursDu1erAu5Juin)
                    {
                        var caseAlicia = GrilleRuntimeHarness.CaseDuJour(grille, jour);
                        Assert.Equal("Alicia", caseAlicia.QuerySelector("[data-testid='nom-responsable']")!.TextContent.Trim());
                        Assert.Equal("bleu", caseAlicia.GetAttribute("data-couleur"));
                    }

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
