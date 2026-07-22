using System;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Web.State;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 24 — Sc.8 (🖥️ @ihm — acceptation de NIVEAU RUNTIME) : la page de connexion dédiée est la
/// <b>landing par défaut</b>. App ouverte non connecté → l'atterrissage (<see cref="Home"/>, route « / »)
/// redirige vers la page de connexion dédiée (« connexion »), PAS vers le planning. Sur la page de
/// connexion réellement câblée (<see cref="Connexion"/>, API distante réelle <see cref="ApiDistanteFactory"/>,
/// store réel, DI réelle), saisir l'email d'un compte <b>Actif</b> puis « Se connecter » ouvre la session via
/// le canal HTTP réel (POST /api/session, s23), <b>redirige vers le planning</b>, et pré-positionne
/// le sélecteur d'acteur sur l'acteur du compte connecté (non-régression s23 Sc.8, via l'incarnation bornée s14).
/// </summary>
public sealed class FrontWasmPageConnexionLandingRuntimeTests : TestContext
{
    private const string ActeurAlice = "parent-a"; // seed Testing : parent-a = « Alice » (Parent)

    private static void SemerCompteActifAlice(ApiDistanteFactory api)
        => api.Services.GetRequiredService<IEditeurComptes>()
            .Creer("compte-alice-s24", "alice@foyer.fr", StatutCompte.Actif, ActeurAlice);

    [Fact]
    public void Should_atterrir_sur_la_page_de_connexion_dediee_pas_le_planning_When_l_app_est_ouverte_non_connecte()
    {
        // Given — un utilisateur non connecté ouvre l'app (route « / » = Home).
        Services.AddSingleton(new SessionPlanning());
        var nav = Services.GetRequiredService<NavigationManager>();

        // When — la landing est rendue.
        RenderComponent<Home>();

        // Then — l'app atterrit sur la page de connexion dédiée, PAS sur le planning.
        Assert.EndsWith("connexion", nav.Uri, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("planning", nav.Uri, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Should_se_connecter_rediriger_vers_le_planning_et_pre_positionner_l_acteur_When_l_email_d_un_compte_actif_est_saisi()
    {
        // Given — la page de connexion dédiée réellement câblée à l'API distante réelle ; un compte ACTIF
        // « alice@foyer.fr » lié à Alice. Le catalogue d'acteurs incarnables est celui du référentiel réel.
        using var api = new ApiDistanteFactory();
        SemerCompteActifAlice(api);
        Services.AddSingleton(GrilleRuntimeHarness.ClientVers(api));
        Services.AddSingleton<IPersistanceSession>(new PersistanceSessionInerte());
        var session = new SessionPlanning();
        Services.AddSingleton(session);
        var nav = Services.GetRequiredService<NavigationManager>();

        var connexion = RenderComponent<Connexion>();

        // When — je saisis l'email d'un compte Actif et clique « Se connecter » (POST réel se-connecter).
        this.SurDispatcher(() => connexion.Find("[data-testid='champ-email-connexion']").Change("alice@foyer.fr"));
        this.SurDispatcher(() => connexion.Find("[data-testid='bouton-se-connecter']").Click());

        // Then — la connexion réussit : redirection vers le planning, et le sélecteur d'acteur est
        // pré-positionné sur l'acteur du compte connecté (identité effective = Alice, s23 Sc.8).
        connexion.WaitForAssertion(
            () =>
            {
                Assert.EndsWith("planning", nav.Uri, StringComparison.OrdinalIgnoreCase);
                Assert.Equal(ActeurAlice, session.IdentiteEffective.Id);
            },
            TimeSpan.FromSeconds(10));
    }
}
