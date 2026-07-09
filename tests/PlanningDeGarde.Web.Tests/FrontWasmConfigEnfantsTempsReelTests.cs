using System;
using System.Linq;
using Bunit;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Web.Components.Pages;
using PlanningDeGarde.Web.State;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 30 — S9 (🖥️ @ihm — acceptation de NIVEAU RUNTIME) : l'onglet « Enfants » de la Configuration du
/// foyer, câblé à une <b>API distante réelle</b> (<see cref="ApiDistanteFactory"/>, store réel, canal
/// d'écriture/lecture HTTP réel), permet de <b>lister / ajouter / éditer</b> un enfant (miroir strict de
/// l'onglet Lieux s27). L'enfant historique « Léa » (seed InMemory du composition root) est listé ; ajouter
/// « Tom » le fait apparaître sans rechargement ; éditer un prénom le reflète ; un prénom vide ou en doublon
/// laisse un message d'erreur SANS rien enregistrer.
///
/// Rempart anti « vert qui ment » : le chemin observé n'est jamais doublé — la liste vient du store réel via
/// GET /api/foyer/enfants, l'ajout/l'édition transitent par POST /api/canal/{ajouter,editer}-enfant réels.
/// Tant que l'onglet Enfants n'existe pas (ou n'est pas câblé au store), la liste reste vide / l'ajout n'atteint
/// pas le store → rouge.
/// </summary>
public sealed class FrontWasmConfigEnfantsTempsReelTests : TestContext
{
    private static void ConfigurerEcranConfig(Bunit.TestContext ctx, ApiDistanteFactory api)
    {
        ctx.Services.AddSingleton(GrilleRuntimeHarness.ClientVers(api));
        ctx.Services.AddSingleton(new SessionPlanning());
        ctx.Services.AddSingleton(new OptionsConnexionHub
        {
            Configurer = options =>
            {
                options.HttpMessageHandlerFactory = _ => api.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
            },
        });
    }

    private static System.Collections.Generic.IReadOnlyList<string> PrenomsListes(IRenderedComponent<ConfigurationFoyer> config)
        => config.FindAll("[data-testid='enfant-foyer'] .role-libelle")
            .Select(e => e.TextContent.Trim())
            .ToList();

    [Fact]
    public void L_onglet_Enfants_liste_ajoute_et_edite_un_enfant_et_refuse_prenom_vide_ou_doublon_sans_enregistrer()
    {
        // Given — l'écran de config câblé à l'API distante réelle (store réel, seed InMemory de l'enfant « Léa »).
        using var api = new ApiDistanteFactory();
        ConfigurerEcranConfig(this, api);
        var config = RenderComponent<ConfigurationFoyer>();

        // … l'enfant historique « Léa » est listé (énuméré depuis le store vivant).
        config.WaitForAssertion(
            () => Assert.Contains("Léa", PrenomsListes(config)),
            TimeSpan.FromSeconds(10));

        // On ouvre l'onglet « Enfants » (présentation — le panneau est déjà dans le DOM).
        config.SurDispatcher(() => config.Find("[data-testid='onglet-enfants']").Click());

        // When — le parent ajoute un enfant « Tom » valide et valide.
        config.SurDispatcher(() => config.Find("[data-testid='champ-prenom-enfant']").Change("Tom"));
        config.SurDispatcher(() => config.Find("#form-ajouter-enfant").Submit());

        // Then — « Tom » apparaît dans la liste sans rechargement (relecture du store vivant).
        config.WaitForAssertion(
            () => Assert.Contains("Tom", PrenomsListes(config)),
            TimeSpan.FromSeconds(10));

        // When — un prénom vide est soumis.
        config.SurDispatcher(() => config.Find("[data-testid='champ-prenom-enfant']").Change("   "));
        config.SurDispatcher(() => config.Find("#form-ajouter-enfant").Submit());

        // Then — un message d'erreur s'affiche et aucun enfant n'est enregistré (toujours Léa + Tom).
        config.WaitForAssertion(
            () =>
            {
                Assert.NotEmpty(config.FindAll("[data-testid='motif-echec-enfant']"));
                Assert.Equal(2, PrenomsListes(config).Count);
            },
            TimeSpan.FromSeconds(10));

        // When — un prénom en doublon (« Léa ») est soumis.
        config.SurDispatcher(() => config.Find("[data-testid='champ-prenom-enfant']").Change("Léa"));
        config.SurDispatcher(() => config.Find("#form-ajouter-enfant").Submit());

        // Then — message d'erreur, aucun second « Léa » enregistré (toujours 2 enfants).
        config.WaitForAssertion(
            () =>
            {
                Assert.NotEmpty(config.FindAll("[data-testid='motif-echec-enfant']"));
                Assert.Equal(2, PrenomsListes(config).Count);
                Assert.Single(PrenomsListes(config), p => p == "Léa");
            },
            TimeSpan.FromSeconds(10));

        // When — le parent édite le prénom de « Tom » en « Tomas » et valide (clé = identifiant stable).
        var ligneTom = config.FindAll("[data-testid='enfant-foyer']")
            .Single(li => li.QuerySelector(".role-libelle")!.TextContent.Trim() == "Tom");
        var idTom = ligneTom.GetAttribute("data-enfant-id");
        config.SurDispatcher(() => ligneTom.QuerySelector("[data-testid='champ-editer-enfant']")!.Change("Tomas"));
        config.SurDispatcher(() => config.FindAll("[data-testid='enfant-foyer']")
            .Single(li => li.GetAttribute("data-enfant-id") == idTom)
            .QuerySelector("[data-testid='bouton-editer-enfant']")!.Click());

        // Then — la liste reflète « Tomas » (même identifiant stable), sans rechargement.
        config.WaitForAssertion(
            () =>
            {
                Assert.Contains("Tomas", PrenomsListes(config));
                Assert.DoesNotContain("Tom", PrenomsListes(config).Where(p => p != "Tomas"));
            },
            TimeSpan.FromSeconds(10));
    }
}
