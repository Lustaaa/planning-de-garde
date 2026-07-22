using System;
using System.Net.Http;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 28 — S6 (@ihm, acceptation de NIVEAU RUNTIME, bout-à-bout). L'écran « redéfinir par jeton »
/// réellement câblé (<see cref="ReinitialiserMotDePasse"/>, API distante RÉELLE <see cref="ApiDistanteFactory"/>,
/// endpoint <c>POST /api/comptes/reinitialisation</c> + <c>RedefinirMotDePasseHandler</c> + hacheur
/// PBKDF2 + stores réels) : le jeton valide est porté par l'URL, la saisie d'un nouveau mot de passe le
/// redéfinit (haché), le jeton est CONSOMMÉ (usage unique), et l'utilisateur peut se connecter avec
/// « email + nouveau mot de passe » (l'ancien ne vérifie plus). Une SECONDE redéfinition avec le même
/// jeton échoue. Chemin non doublé (stores + handlers réels résolus de l'hôte d'API).
/// </summary>
public sealed class FrontWasmReinitialiserMotDePasseRuntimeTests : TestContext
{
    private const string Email = "reset@foyer.fr";
    private const string CompteId = "compte-reset";
    private const string AncienMotDePasse = "ancien-secret";
    private const string NouveauMotDePasse = "nouveau-secret";

    [Fact]
    public void Should_redefinir_le_mot_de_passe_consommer_le_jeton_et_permettre_la_connexion_avec_le_nouveau_When_l_utilisateur_valide_sur_l_ecran_de_reinitialisation()
    {
        // Given — l'API distante RÉELLE ; un compte Actif portant l'ANCIEN mot de passe (haché réel) ; un
        // jeton de réinitialisation VALIDE (non expiré) enregistré au store réel, porté par l'URL de l'écran.
        using var api = new ApiDistanteFactory();
        var hacheur = api.Services.GetRequiredService<IHacheurMotDePasse>();
        api.Services.GetRequiredService<IEditeurComptes>()
            .Creer(CompteId, Email, StatutCompte.Actif, "acteur-reset", hacheur.Hacher(AncienMotDePasse));
        var horloge = api.Services.GetRequiredService<IDateTimeProvider>();
        var jetonValeur = $"reset-{Guid.NewGuid():N}";
        api.Services.GetRequiredService<IReferentielJetonsReset>()
            .Enregistrer(new JetonReset(jetonValeur, CompteId, horloge.Maintenant.AddMinutes(60), Consomme: false));

        Services.AddSingleton(new HttpClient(api.Server.CreateHandler()) { BaseAddress = api.Server.BaseAddress });

        // Le jeton arrive par l'URL (lien reçu par mail) : on positionne l'URI de la navigation avant le rendu.
        var nav = Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo($"reinitialiser-mot-de-passe?jeton={jetonValeur}");

        var ecran = RenderComponent<ReinitialiserMotDePasse>();

        // When — l'utilisateur saisit un nouveau mot de passe et valide (POST /api/canal/redefinir réel).
        this.SurDispatcher(() => ecran.Find("[data-testid='champ-nouveau-mot-de-passe']").Change(NouveauMotDePasse));
        this.SurDispatcher(() => ecran.Find("[data-testid='bouton-redefinir']").Click());

        // Then — message de succès affiché, ET (chemin runtime réel non doublé) le jeton est consommé, le
        // mot de passe est redéfini de sorte que la connexion réussit avec le NOUVEAU et échoue avec l'ANCIEN.
        ecran.WaitForAssertion(
            () =>
            {
                Assert.Contains("redéfini", ecran.Markup, StringComparison.OrdinalIgnoreCase);

                var jetonRelu = api.Services.GetRequiredService<IReferentielJetonsReset>().Trouver(jetonValeur);
                Assert.NotNull(jetonRelu);
                Assert.True(jetonRelu!.Consomme); // usage unique : jeton consommé

                using var scope = api.Services.CreateScope();
                var connexion = scope.ServiceProvider.GetRequiredService<SeConnecterHandler>();
                Assert.True(connexion.Handle(new SeConnecterCommand(Email, NouveauMotDePasse)).EstSucces);   // nouveau MDP actif
                Assert.False(connexion.Handle(new SeConnecterCommand(Email, AncienMotDePasse)).EstSucces);   // ancien MDP révoqué
            },
            TimeSpan.FromSeconds(10));

        // Et — une SECONDE redéfinition avec le MÊME jeton échoue (usage unique) : le motif est surfacé.
        this.SurDispatcher(() => ecran.Find("[data-testid='bouton-redefinir']").Click());
        ecran.WaitForAssertion(
            () => Assert.NotNull(ecran.Find("[data-testid='message-reinit-erreur']")),
            TimeSpan.FromSeconds(10));
    }
}
