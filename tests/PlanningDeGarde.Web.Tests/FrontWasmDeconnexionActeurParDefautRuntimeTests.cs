using System;
using System.Linq;
using AngleSharp.Html.Dom;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Web.Components.Pages;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 23 — Sc.8 (🖥️ @ihm — acceptation de NIVEAU RUNTIME) : déconnexion + acteur par défaut
/// pré-positionné, sur la vue planning réellement câblée (<see cref="PlanningPartage"/>, API distante réelle
/// <see cref="ApiDistanteFactory"/>, store réel, DI réelle, hub SignalR réel). Une fois connecté en tant que
/// compte « alice@foyer.fr » (lié à l'acteur « Alice »), le <b>sélecteur d'acteur</b> (source unique
/// <c>ActeursIncarnables</c>, s20) est pré-positionné sur « Alice » = l'acteur du compte connecté. « Se
/// déconnecter » repasse « non connecté » et le sélecteur retombe sur le défaut non connecté (plus de
/// pré-positionnement sur « Alice »). Chemin non doublé (transport + handler + store réels).
/// </summary>
public sealed class FrontWasmDeconnexionActeurParDefautRuntimeTests : TestContext
{
    private const string ActeurAlice = "parent-a"; // seed s22 : parent-a = « Alice »

    private static void SemerCompte(ApiDistanteFactory api, string compteId, string email, StatutCompte statut, string acteurId)
        => api.Services.GetRequiredService<IEditeurComptes>().Creer(compteId, email, statut, acteurId);

    private IRenderedComponent<PlanningPartage> RendrePlanning(ApiDistanteFactory api)
        => GrilleRuntimeHarness.RendreGrille(this, api, GrilleRuntimeHarness.Lundi_29_06_2026);

    private static string ValeurSelecteurActeur(IRenderedComponent<PlanningPartage> planning)
    {
        var select = (IHtmlSelectElement)planning.Find("[data-testid='selecteur-incarnation']");
        return select.Value;
    }

    private static void SeConnecter(IRenderedComponent<PlanningPartage> planning, string email)
    {
        planning.Find("[data-testid='champ-email-connexion']").Change(email);
        planning.Find("[data-testid='bouton-se-connecter']").Click();
    }

    [Fact]
    public void Should_pre_positionner_le_selecteur_sur_l_acteur_du_compte_connecte_When_on_se_connecte()
    {
        // Given — vue câblée réelle ; compte ACTIF « alice@foyer.fr » lié à Alice.
        using var api = new ApiDistanteFactory();
        SemerCompte(api, "compte-alice", "alice@foyer.fr", StatutCompte.Actif, ActeurAlice);
        var planning = RendrePlanning(api);
        Assert.Equal("", ValeurSelecteurActeur(planning)); // pré-condition : non connecté → défaut « Moi »

        // When — je me connecte en tant qu'Alice.
        SeConnecter(planning, "alice@foyer.fr");

        // Then — le sélecteur d'acteur est pré-positionné sur l'acteur du compte connecté (Alice).
        planning.WaitForAssertion(
            () => Assert.Equal(ActeurAlice, ValeurSelecteurActeur(planning)),
            TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void Should_repasser_non_connecte_et_retomber_le_selecteur_sur_le_defaut_When_on_se_deconnecte()
    {
        // Given — connecté en tant qu'Alice, sélecteur pré-positionné sur Alice.
        using var api = new ApiDistanteFactory();
        SemerCompte(api, "compte-alice", "alice@foyer.fr", StatutCompte.Actif, ActeurAlice);
        var planning = RendrePlanning(api);
        SeConnecter(planning, "alice@foyer.fr");
        planning.WaitForAssertion(
            () => Assert.Equal(ActeurAlice, ValeurSelecteurActeur(planning)),
            TimeSpan.FromSeconds(10));

        // When — je clique « Se déconnecter ».
        planning.Find("[data-testid='bouton-se-deconnecter']").Click();

        // Then — le bandeau repasse « non connecté » (le bouton « Se connecter » revient, l'état connecté
        // disparaît) ET le sélecteur d'acteur retombe sur le défaut non connecté (plus de « Alice »).
        planning.WaitForAssertion(
            () =>
            {
                Assert.Empty(planning.FindAll("[data-testid='etat-connexion']"));
                Assert.NotNull(planning.Find("[data-testid='bouton-se-connecter']"));
                Assert.Equal("", ValeurSelecteurActeur(planning));
            },
            TimeSpan.FromSeconds(10));
    }
}
