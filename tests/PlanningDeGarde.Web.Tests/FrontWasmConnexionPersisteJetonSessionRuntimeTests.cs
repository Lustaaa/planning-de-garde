using System;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Web;
using PlanningDeGarde.Web.Components.Pages;
using PlanningDeGarde.Web.State;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 31 — Sc.1 (🖥️ @ihm — acceptation de NIVEAU RUNTIME) : moitié « persisté au login » de la survie
/// au F5. La <b>vraie</b> page de connexion (<see cref="Connexion"/>, API distante RÉELLE
/// <see cref="ApiDistanteFactory"/>, endpoint <c>POST /api/canal/se-connecter</c> + <c>SeConnecterHandler</c> +
/// store réels) : sur une connexion réussie (bon couple email + mot de passe), elle <b>persiste un jeton de
/// session</b> via le port <see cref="IPersistanceSession"/> (seul doublé — un espion ; l'effet localStorage
/// réel est couvert par la garde d'asset <see cref="AmorceSessionAssetTests"/>). C'est ce jeton que le
/// démarrage suivant relira pour restaurer la session après un F5. Le jeton porte l'identité RÉELLE ancrée
/// (acteur du compte + nom + type, résolus serveur), jamais un secret. Sans cette persistance, la
/// restauration au démarrage n'aurait jamais rien à relire (feature inerte).
/// </summary>
public sealed class FrontWasmConnexionPersisteJetonSessionRuntimeTests : TestContext
{
    private const string Email = "papa@foyer.fr";
    private const string ActeurPapa = "nounou"; // acteur résolu par la config réelle
    private const string BonMotDePasse = "bon-secret-papa";

    [Fact]
    public void Should_persister_le_jeton_de_session_When_la_connexion_reussit()
    {
        using var api = new ApiDistanteFactory();
        var hacheur = api.Services.GetRequiredService<IHacheurMotDePasse>();
        api.Services.GetRequiredService<IEditeurComptes>()
            .Creer("compte-papa", Email, StatutCompte.Actif, ActeurPapa, hacheur.Hacher(BonMotDePasse));
        Services.AddSingleton(GrilleRuntimeHarness.ClientVers(api));
        var session = new SessionPlanning();
        Services.AddSingleton(session);
        var persistance = new SpyPersistanceSession();
        Services.AddSingleton<IPersistanceSession>(persistance);

        var connexion = RenderComponent<Connexion>();

        // When — email + BON mot de passe (POST réel vers l'API distante).
        this.SurDispatcher(() => connexion.Find("[data-testid='champ-email-connexion']").Change(Email));
        this.SurDispatcher(() => connexion.Find("[data-testid='champ-mot-de-passe-connexion']").Change(BonMotDePasse));
        this.SurDispatcher(() => connexion.Find("[data-testid='bouton-se-connecter']").Click());

        // Then — la session s'ouvre ET un jeton est persisté, portant l'identité réelle ancrée (acteur du
        // compte + nom + type). C'est ce jeton que la restauration au démarrage relira après un F5 (Sc.1).
        connexion.WaitForAssertion(
            () =>
            {
                Assert.True(session.EstConnecte);
                var jeton = persistance.DernierJetonPersiste;
                Assert.NotNull(jeton);
                Assert.Equal(ActeurPapa, jeton!.ActeurId);
                Assert.Equal(session.CompteConnecteNom, jeton.Nom);
                Assert.Equal(session.IdentiteReelle.Type, jeton.Type);
            },
            TimeSpan.FromSeconds(10));
    }
}
