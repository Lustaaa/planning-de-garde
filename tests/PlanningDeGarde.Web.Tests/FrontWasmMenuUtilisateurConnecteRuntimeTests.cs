using System;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Web.Components.Shared.Layout;
using PlanningDeGarde.Web.State;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 24 — Sc.11 (🖥️ @ihm — acceptation de NIVEAU RUNTIME) : le menu utilisateur connecté. Sur la page de
/// connexion réellement câblée (<see cref="Connexion"/>, API distante réelle <see cref="ApiDistanteFactory"/>,
/// store réel, DI réelle), on se connecte avec un compte Actif ; le <see cref="MenuUtilisateur"/> (câblé à la
/// MÊME session scoped) affiche alors le <b>nom / acteur</b> (résolu serveur s23), un <b>accès à la config
/// foyer</b> et un bouton <b>« Se déconnecter »</b>. Le clic « Se déconnecter » détruit la session (logout s23)
/// et ramène à la page de connexion dédiée. Hors connexion, le menu n'expose aucune de ces affordances.
/// </summary>
public sealed class FrontWasmMenuUtilisateurConnecteRuntimeTests : TestContext
{
    private const string ActeurAlice = "parent-a"; // seed Testing : parent-a = « Alice »

    [Fact]
    public void Should_afficher_le_menu_utilisateur_connecte_puis_revenir_a_la_page_login_a_la_deconnexion()
    {
        // Given — page de connexion et menu utilisateur câblés à la MÊME session scoped + API distante réelle ;
        // un compte ACTIF « alice@foyer.fr » lié à Alice.
        using var api = new ApiDistanteFactory();
        api.Services.GetRequiredService<IEditeurComptes>()
            .Creer("compte-alice-s24", "alice@foyer.fr", StatutCompte.Actif, ActeurAlice);
        Services.AddSingleton(GrilleRuntimeHarness.ClientVers(api));
        Services.AddSingleton<IPersistanceSession>(new PersistanceSessionInerte());
        var session = new SessionPlanning();
        Services.AddSingleton(session);
        var nav = Services.GetRequiredService<NavigationManager>();

        var connexion = RenderComponent<Connexion>();
        var menu = RenderComponent<MenuUtilisateur>();

        // Baseline (hors connexion) — le menu n'expose ni identité, ni déconnexion.
        Assert.Empty(menu.FindAll("[data-testid='menu-utilisateur']"));

        // When (connexion) — je saisis l'email d'un compte Actif et clique « Se connecter » (POST réel).
        this.SurDispatcher(() => connexion.Find("[data-testid='champ-email-connexion']").Change("alice@foyer.fr"));
        this.SurDispatcher(() => connexion.Find("[data-testid='bouton-se-connecter']").Click());

        // Then — une fois connecté, le menu utilisateur affiche le nom (résolu serveur), un accès config foyer
        // et un bouton « Se déconnecter ».
        menu.WaitForAssertion(
            () =>
            {
                var bandeau = menu.Find("[data-testid='menu-utilisateur']");
                Assert.Contains("Alice", bandeau.TextContent);
                Assert.NotNull(menu.Find("[data-testid='menu-lien-config']"));
                Assert.NotNull(menu.Find("[data-testid='menu-se-deconnecter']"));
            },
            TimeSpan.FromSeconds(10));

        // When (déconnexion) — je clique « Se déconnecter ».
        this.SurDispatcher(() => menu.Find("[data-testid='menu-se-deconnecter']").Click());

        // Then — la session est détruite (le menu n'affiche plus l'identité) et on revient à la page de
        // connexion dédiée.
        menu.WaitForAssertion(
            () => Assert.Empty(menu.FindAll("[data-testid='menu-utilisateur']")),
            TimeSpan.FromSeconds(10));
        Assert.False(session.EstConnecte);
        Assert.EndsWith("connexion", nav.Uri, StringComparison.OrdinalIgnoreCase);
    }
}
