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
/// Acceptation de NIVEAU RUNTIME du Sc.3 (🖥️ IHM, <c>@limite</c>) — 100 % runtime, 0 backend : la
/// <b>troncature + survol</b> (composant livré s07 Sc.6) s'applique à un nom <b>édité</b> long. Depuis
/// l'écran de configuration réellement câblé (<see cref="ConfigurationFoyer"/>), parent-c (de libellé
/// court « Marie » qui tient dans la case) est renommé en « Marie-Hélène Grand-Dubois » (25 caractères).
/// <b>Sans rechargement</b>, la grille réelle (<see cref="PlanningPartage"/>, API distante + store réel +
/// diffusion SignalR) tronque visuellement la case (classe <c>nom-tronque</c>), restitue le nom complet au
/// <b>survol</b> (attribut natif <c>title</c>) et affiche le nom <b>complet</b> en légende — la donnée
/// reste intègre (textContent complet, la troncature est purement de présentation/CSS).
///
/// Caractérisation au runtime : la troncature/survol est déjà verte depuis s07 ; la valeur ajoutée du
/// sprint 08 est que la source du nom long est désormais une <b>édition</b> (store mutable bindé sur
/// <c>IReferentielResponsables</c> depuis Sc.1), pas le seed figé. Le baseline court « Marie » est posé
/// via le <b>canal d'écriture réel</b> (Given) pour que le renommage vers le nom long soit réellement
/// observable : si l'édition ne propageait pas, la case resterait « Marie » → rouge. Pas un bUnit à
/// doublure (il ne prouve ni le rendu CSS de troncature, ni l'attribut natif, ni la chaîne d'édition réelle).
/// </summary>
public sealed class FrontWasmConfigNomLongEditeTempsReelTests : TestContext
{
    private const string NomCourt = "Marie";
    private const string NomLong = "Marie-Hélène Grand-Dubois"; // 25 caractères

    [Fact]
    public async Task Should_Tronquer_Marie_Helene_Grand_Dubois_dans_la_case_du_16_07_2026_avec_le_nom_complet_au_survol_et_en_legende_When_l_acteur_parent_c_est_renomme_de_Marie_en_un_nom_long_de_25_caracteres()
    {
        // Given — l'API distante réelle porte une période affectée à parent-c le jeudi 16/07/2026, et
        // parent-c a pour libellé court « Marie » (posé via le canal d'écriture réel, baseline qui tient
        // dans la case) — établi AVANT le rendu pour que la grille s'ouvre sur le nom court.
        using var api = new ApiDistanteFactory();
        GrilleRuntimeHarness.SemerPeriode(api, "parent-c", new DateTime(2026, 7, 16), new DateTime(2026, 7, 16));

        using var clientSetup = GrilleRuntimeHarness.ClientVers(api);
        var reponseBaseline = await clientSetup.PostAsJsonAsync("api/canal/editer-acteur", new
        {
            ActeurId = "parent-c",
            Nom = NomCourt,
        });
        Assert.True(reponseBaseline.IsSuccessStatusCode);

        var lundi_13_07_2026 = new DateTime(2026, 7, 13);
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, lundi_13_07_2026);

        // … état initial : la case du 16/07 porte le nom court « Marie » (le renommage long sera donc
        // réellement observable — pas un faux vert sur un nom déjà long).
        var caseInitiale = GrilleRuntimeHarness.CaseDuJour(grille, "16/07");
        var nomInitial = caseInitiale.QuerySelector("[data-testid='nom-responsable']")!;
        Assert.Equal(NomCourt, nomInitial.TextContent.Trim());
        Assert.Equal(NomCourt, nomInitial.GetAttribute("title"));

        // When — depuis l'écran de configuration réellement câblé, je renomme parent-c en le nom long
        // de 25 caractères et j'enregistre (émission via le canal d'écriture HTTP réel).
        var config = RenderComponent<ConfigurationFoyer>();

        // … l'écran énumère ses acteurs depuis le store (GET HTTP réel asynchrone) : on attend que ce
        // chargement ait fini de re-rendre l'écran avant d'interagir avec le select, sinon un re-render
        // intercalé invaliderait le handler d'événement du select (UnknownEventHandlerId) — garde
        // déterministe déjà appliqué par les tests config frères (renommage grille, convergence).
        config.WaitForState(
            () => config.FindAll("[data-testid='acteur-foyer']").Count > 0,
            TimeSpan.FromSeconds(10));

        this.SurDispatcher(() => config.Find("select.form-select").Change("parent-c"));
        this.SurDispatcher(() => config.Find("[data-testid='champ-nom']").Change(NomLong));
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
            // Then — sans rechargement, la case du 16/07 tronque visuellement (classe nom-tronque),
            // restitue le nom complet au survol (title natif) et conserve la donnée intègre (textContent
            // complet) ; la légende affiche le nom complet (en bleu, couleur de parent-c inchangée).
            grille.WaitForAssertion(
                () =>
                {
                    var caseLong = GrilleRuntimeHarness.CaseDuJour(grille, "16/07");
                    var nom = caseLong.QuerySelector("[data-testid='nom-responsable']")!;
                    Assert.Equal(NomLong, nom.GetAttribute("title"));     // survol = nom complet
                    Assert.Contains("nom-tronque", nom.ClassList);        // troncature visuelle (CSS)
                    Assert.Equal(NomLong, nom.TextContent.Trim());        // donnée intègre

                    var entree = Assert.Single(grille.FindAll("[data-testid='legende-entree']"));
                    Assert.Equal(NomLong, entree.QuerySelector(".legende-nom")!.TextContent.Trim()); // légende complète
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
