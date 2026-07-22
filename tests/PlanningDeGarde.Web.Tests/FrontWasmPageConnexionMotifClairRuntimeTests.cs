using System;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Web.State;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 24 — Sc.9 (🖥️ @ihm — acceptation de NIVEAU RUNTIME) : motif clair sur la page de connexion dédiée
/// réellement câblée (<see cref="Connexion"/>, API distante réelle <see cref="ApiDistanteFactory"/>, store
/// réel, DI réelle). Saisir un email <b>inconnu</b> — ou l'email d'un compte <b>Inactif</b> — puis « Se
/// connecter » : le handler réel (<see cref="SeConnecterCommand"/>, s23) refuse (email inconnu / compte non
/// activé) ; la page affiche un <b>motif clair</b>, <b>reste sur la page de connexion</b> (aucune redirection),
/// et n'ouvre <b>aucune session</b> (identité effective inchangée = pas d'incarnation du compte).
/// </summary>
public sealed class FrontWasmPageConnexionMotifClairRuntimeTests : TestContext
{
    private const string ActeurBruno = "parent-b"; // seed Testing : parent-b = « Bruno »

    private IRenderedComponent<Connexion> RendreConnexion(ApiDistanteFactory api, out SessionPlanning session, out NavigationManager nav)
    {
        Services.AddSingleton(GrilleRuntimeHarness.ClientVers(api));
        Services.AddSingleton<IPersistanceSession>(new PersistanceSessionInerte());
        session = new SessionPlanning();
        Services.AddSingleton(session);
        nav = Services.GetRequiredService<NavigationManager>();
        return RenderComponent<Connexion>();
    }

    [Fact]
    public void Should_afficher_un_motif_clair_rester_sur_la_page_et_n_ouvrir_aucune_session_When_l_email_est_inconnu()
    {
        // Given — page de connexion câblée réelle ; aucun compte « inconnu@foyer.fr ».
        using var api = new ApiDistanteFactory();
        var connexion = RendreConnexion(api, out var session, out var nav);
        var uriAvant = nav.Uri;

        // When — je tente de me connecter avec un email inconnu (le handler réel refuse « email inconnu »).
        this.SurDispatcher(() => connexion.Find("[data-testid='champ-email-connexion']").Change("inconnu@foyer.fr"));
        this.SurDispatcher(() => connexion.Find("[data-testid='bouton-se-connecter']").Click());

        // Then — un motif clair s'affiche sur la page, on reste sur la page de connexion (aucune
        // redirection), et aucune session n'est ouverte (identité effective = réelle, pas incarnée).
        connexion.WaitForAssertion(
            () =>
            {
                var motif = connexion.Find("[data-testid='motif-connexion']");
                Assert.False(string.IsNullOrWhiteSpace(motif.TextContent));
            },
            TimeSpan.FromSeconds(10));
        Assert.Equal(uriAvant, nav.Uri);                                   // aucune redirection
        Assert.DoesNotContain("planning", nav.Uri, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(session.IdentiteReelle.Id, session.IdentiteEffective.Id); // aucune session ouverte
    }

    [Fact]
    public void Should_afficher_un_motif_clair_rester_sur_la_page_et_n_ouvrir_aucune_session_When_le_compte_est_inactif()
    {
        // Given — page de connexion câblée réelle ; un compte INACTIF « bob@foyer.fr » lié à Bruno.
        using var api = new ApiDistanteFactory();
        api.Services.GetRequiredService<IEditeurComptes>()
            .Creer("compte-bob-s24", "bob@foyer.fr", StatutCompte.Inactif, ActeurBruno);
        var connexion = RendreConnexion(api, out var session, out var nav);
        var uriAvant = nav.Uri;

        // When — je tente de me connecter (le handler réel refuse « compte non activé »).
        this.SurDispatcher(() => connexion.Find("[data-testid='champ-email-connexion']").Change("bob@foyer.fr"));
        this.SurDispatcher(() => connexion.Find("[data-testid='bouton-se-connecter']").Click());

        // Then — motif clair, on reste sur la page, aucune session ouverte.
        connexion.WaitForAssertion(
            () =>
            {
                var motif = connexion.Find("[data-testid='motif-connexion']");
                Assert.False(string.IsNullOrWhiteSpace(motif.TextContent));
            },
            TimeSpan.FromSeconds(10));
        Assert.Equal(uriAvant, nav.Uri);
        Assert.DoesNotContain("planning", nav.Uri, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(session.IdentiteReelle.Id, session.IdentiteEffective.Id);
    }
}
