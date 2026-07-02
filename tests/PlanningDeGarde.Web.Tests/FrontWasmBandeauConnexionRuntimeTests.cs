using System;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Web.Components.Pages;
using PlanningDeGarde.Web.State;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 23 — Sc.7 (🖥️ @ihm — acceptation de NIVEAU RUNTIME) : le <b>bandeau de connexion custom</b> de la
/// vue planning réellement câblée (<see cref="PlanningPartage"/>, API distante réelle
/// <see cref="ApiDistanteFactory"/>, store réel, DI réelle, hub SignalR réel) : saisir l'email d'un compte
/// <b>Actif</b> puis « Se connecter » ouvre la session via le canal HTTP réel (POST /api/canal/se-connecter)
/// et le bandeau affiche « Connecté : Alice ». Un email inconnu ou un compte Inactif est refusé par le
/// handler réel : le bandeau reste « non connecté » et affiche un motif clair. Chemin non doublé (transport +
/// handler + store réels) — rempart anti « vert qui ment » (un bUnit à transport doublé ne le prouverait pas).
/// </summary>
public sealed class FrontWasmBandeauConnexionRuntimeTests : TestContext
{
    // Seed s22 InMemory (Testing) : parent-a = « Alice » (Parent), parent-b = « Bruno ».
    private const string ActeurAlice = "parent-a";
    private const string ActeurBruno = "parent-b";

    private static void SemerCompte(ApiDistanteFactory api, string compteId, string email, StatutCompte statut, string acteurId)
        => api.Services.GetRequiredService<IEditeurComptes>().Creer(compteId, email, statut, acteurId);

    private IRenderedComponent<PlanningPartage> RendrePlanning(ApiDistanteFactory api)
        => GrilleRuntimeHarness.RendreGrille(this, api, GrilleRuntimeHarness.Lundi_29_06_2026);

    [Fact]
    public void Should_afficher_Connecte_Alice_When_on_se_connecte_avec_l_email_d_un_compte_actif()
    {
        // Given — vue planning câblée à l'API distante réelle ; un compte ACTIF « alice@foyer.fr » lié à Alice.
        using var api = new ApiDistanteFactory();
        SemerCompte(api, "compte-alice", "alice@foyer.fr", StatutCompte.Actif, ActeurAlice);
        var planning = RendrePlanning(api);

        // When — je saisis l'email dans le bandeau et clique « Se connecter » (POST réel se-connecter).
        planning.Find("[data-testid='champ-email-connexion']").Change("alice@foyer.fr");
        planning.Find("[data-testid='bouton-se-connecter']").Click();

        // Then — sans rechargement, le bandeau affiche l'état « Connecté : Alice » (nom résolu côté serveur).
        planning.WaitForAssertion(
            () =>
            {
                var etat = planning.Find("[data-testid='etat-connexion']");
                Assert.Contains("Connecté", etat.TextContent);
                Assert.Contains("Alice", etat.TextContent);
            },
            TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void Should_rester_non_connecte_avec_motif_clair_When_l_email_est_inconnu()
    {
        // Given — vue câblée réelle ; aucun compte « inconnu@foyer.fr ».
        using var api = new ApiDistanteFactory();
        var planning = RendrePlanning(api);

        // When — je tente de me connecter avec un email inconnu (le handler réel refuse « email inconnu »).
        planning.Find("[data-testid='champ-email-connexion']").Change("inconnu@foyer.fr");
        planning.Find("[data-testid='bouton-se-connecter']").Click();

        // Then — le bandeau reste « non connecté » (bouton « Se connecter » présent, pas d'état connecté)
        // et affiche un motif clair.
        planning.WaitForAssertion(
            () =>
            {
                var motif = planning.Find("[data-testid='motif-connexion']");
                Assert.False(string.IsNullOrWhiteSpace(motif.TextContent));
                Assert.Empty(planning.FindAll("[data-testid='etat-connexion']"));
            },
            TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void Should_rester_non_connecte_avec_motif_clair_When_le_compte_est_inactif()
    {
        // Given — vue câblée réelle ; un compte INACTIF « bob@foyer.fr » lié à Bruno (statut défaut s22).
        using var api = new ApiDistanteFactory();
        SemerCompte(api, "compte-bob", "bob@foyer.fr", StatutCompte.Inactif, ActeurBruno);
        var planning = RendrePlanning(api);

        // When — je tente de me connecter (le handler réel refuse « compte non activé »).
        planning.Find("[data-testid='champ-email-connexion']").Change("bob@foyer.fr");
        planning.Find("[data-testid='bouton-se-connecter']").Click();

        // Then — le bandeau reste « non connecté » et affiche un motif clair.
        planning.WaitForAssertion(
            () =>
            {
                var motif = planning.Find("[data-testid='motif-connexion']");
                Assert.False(string.IsNullOrWhiteSpace(motif.TextContent));
                Assert.Empty(planning.FindAll("[data-testid='etat-connexion']"));
            },
            TimeSpan.FromSeconds(10));
    }
}
