using System;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Web.Components.Pages;
using PlanningDeGarde.Web.State;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 28 — S8 (@ihm, acceptation de NIVEAU RUNTIME). La page de connexion réellement câblée
/// (<see cref="Connexion"/>, API distante RÉELLE <see cref="ApiDistanteFactory"/>, endpoint
/// <c>POST /api/canal/se-connecter</c> + <c>SeConnecterHandler</c> + store réels) présente un champ
/// <b>mot de passe</b> et émet la connexion avec le COUPLE (email + mot de passe) : sur le bon couple la
/// session s'ouvre et l'on est redirigé vers « /planning » ; sur le mauvais couple un motif NEUTRE est
/// affiché, on reste sur « /connexion », aucune session. Chemin non doublé (transport réel vers l'hôte).
/// </summary>
public sealed class FrontWasmConnexionMotDePasseRuntimeTests : TestContext
{
    private const string Email = "maman@foyer.fr";
    private const string ActeurNounou = "nounou"; // acteur résolu par la config réelle (type Autre)
    private const string BonMotDePasse = "bon-secret-maman";
    private const string MauvaisMotDePasse = "mauvais-essai";

    [Fact]
    public void Should_ouvrir_la_session_et_rediriger_vers_le_planning_When_l_utilisateur_saisit_email_et_bon_mot_de_passe()
    {
        using var api = new ApiDistanteFactory();
        var hacheur = api.Services.GetRequiredService<IHacheurMotDePasse>();
        api.Services.GetRequiredService<IEditeurComptes>()
            .Creer("compte-maman", Email, StatutCompte.Actif, ActeurNounou, hacheur.Hacher(BonMotDePasse));
        Services.AddSingleton(GrilleRuntimeHarness.ClientVers(api));
        Services.AddSingleton<IPersistanceSession>(new PersistanceSessionInerte());
        var session = new SessionPlanning();
        Services.AddSingleton(session);

        var connexion = RenderComponent<Connexion>();

        // When — email + BON mot de passe (POST /api/canal/se-connecter avec le couple, réel).
        this.SurDispatcher(() => connexion.Find("[data-testid='champ-email-connexion']").Change(Email));
        this.SurDispatcher(() => connexion.Find("[data-testid='champ-mot-de-passe-connexion']").Change(BonMotDePasse));
        this.SurDispatcher(() => connexion.Find("[data-testid='bouton-se-connecter']").Click());

        // Then — la session s'ouvre (identité réelle = l'acteur du compte) et l'on est redirigé vers /planning.
        connexion.WaitForAssertion(
            () =>
            {
                Assert.True(session.EstConnecte);
                Assert.Equal(ActeurNounou, session.IdentiteReelle.Id);
                Assert.EndsWith("planning", Services.GetRequiredService<NavigationManager>().Uri);
            },
            TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void Should_afficher_un_motif_neutre_et_rester_sur_connexion_sans_session_When_le_mot_de_passe_est_mauvais()
    {
        using var api = new ApiDistanteFactory();
        var hacheur = api.Services.GetRequiredService<IHacheurMotDePasse>();
        api.Services.GetRequiredService<IEditeurComptes>()
            .Creer("compte-maman", Email, StatutCompte.Actif, ActeurNounou, hacheur.Hacher(BonMotDePasse));
        Services.AddSingleton(GrilleRuntimeHarness.ClientVers(api));
        Services.AddSingleton<IPersistanceSession>(new PersistanceSessionInerte());
        var session = new SessionPlanning();
        Services.AddSingleton(session);

        var connexion = RenderComponent<Connexion>();

        // When — email + MAUVAIS mot de passe.
        this.SurDispatcher(() => connexion.Find("[data-testid='champ-email-connexion']").Change(Email));
        this.SurDispatcher(() => connexion.Find("[data-testid='champ-mot-de-passe-connexion']").Change(MauvaisMotDePasse));
        this.SurDispatcher(() => connexion.Find("[data-testid='bouton-se-connecter']").Click());

        // Then — motif NEUTRE affiché, aucune session, on reste sur /connexion (pas de redirection planning).
        connexion.WaitForAssertion(
            () =>
            {
                Assert.NotNull(connexion.Find("[data-testid='motif-connexion']"));
                Assert.False(session.EstConnecte);
                Assert.DoesNotContain("planning", Services.GetRequiredService<NavigationManager>().Uri);
            },
            TimeSpan.FromSeconds(10));
    }
}
