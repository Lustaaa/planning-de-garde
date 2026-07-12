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
    public void L_onglet_Enfants_liste_ajoute_et_edite_un_enfant_via_la_modal_et_refuse_prenom_vide_ou_doublon_sans_enregistrer()
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

        // When — le parent ajoute un enfant « Tom » valide via la MODAL d'ajout (patron s34).
        config.SurDispatcher(() => config.Find("[data-testid='bouton-ajouter-enfant']").Click());
        config.WaitForElement("[data-testid='dialog-enfant']", TimeSpan.FromSeconds(10));
        config.SurDispatcher(() => config.Find("[data-testid='champ-prenom-enfant']").Change("Tom"));
        config.SurDispatcher(() => config.Find("#form-enfant").Submit());

        // Then — « Tom » apparaît dans la table sans rechargement (relecture du store vivant), modal fermée.
        config.WaitForAssertion(
            () => Assert.Contains("Tom", PrenomsListes(config)),
            TimeSpan.FromSeconds(10));

        // When — un prénom vide est soumis via la modal d'ajout.
        config.SurDispatcher(() => config.Find("[data-testid='bouton-ajouter-enfant']").Click());
        config.SurDispatcher(() => config.Find("[data-testid='champ-prenom-enfant']").Change("   "));
        config.SurDispatcher(() => config.Find("#form-enfant").Submit());

        // Then — le motif s'affiche DANS la modal restée ouverte et aucun enfant n'est enregistré (toujours Léa + Tom).
        config.WaitForAssertion(
            () =>
            {
                Assert.NotEmpty(config.FindAll("[data-testid='motif-echec-enfant']"));
                Assert.Equal(2, PrenomsListes(config).Count);
            },
            TimeSpan.FromSeconds(10));

        // When — un prénom en doublon (« Léa ») est soumis via la modal restée ouverte.
        config.SurDispatcher(() => config.Find("[data-testid='champ-prenom-enfant']").Change("Léa"));
        config.SurDispatcher(() => config.Find("#form-enfant").Submit());

        // Then — motif d'erreur, aucun second « Léa » enregistré (toujours 2 enfants).
        config.WaitForAssertion(
            () =>
            {
                Assert.NotEmpty(config.FindAll("[data-testid='motif-echec-enfant']"));
                Assert.Equal(2, PrenomsListes(config).Count);
                Assert.Single(PrenomsListes(config), p => p == "Léa");
            },
            TimeSpan.FromSeconds(10));

        // On ferme la modal d'ajout en refus (Annuler) avant d'éditer.
        config.SurDispatcher(() => config.Find("[data-testid='dialog-enfant-annuler']").Click());

        // When — le parent édite le prénom de « Tom » en « Tomas » via le crayon → modal (clé = identifiant stable).
        var ligneTom = config.FindAll("[data-testid='enfant-foyer']")
            .Single(li => li.QuerySelector(".role-libelle")!.TextContent.Trim() == "Tom");
        config.SurDispatcher(() => ligneTom.QuerySelector("[data-testid='crayon-enfant']")!.Click());
        config.WaitForElement("[data-testid='dialog-enfant']", TimeSpan.FromSeconds(10));
        config.SurDispatcher(() => config.Find("[data-testid='champ-prenom-enfant']").Change("Tomas"));
        config.SurDispatcher(() => config.Find("#form-enfant").Submit());

        // Then — la table reflète « Tomas » (même identifiant stable), sans rechargement.
        config.WaitForAssertion(
            () =>
            {
                Assert.Contains("Tomas", PrenomsListes(config));
                Assert.DoesNotContain("Tom", PrenomsListes(config).Where(p => p != "Tomas"));
            },
            TimeSpan.FromSeconds(10));
    }
}
