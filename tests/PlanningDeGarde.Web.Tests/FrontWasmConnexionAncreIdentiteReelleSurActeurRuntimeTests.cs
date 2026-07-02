using System;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Web.Components.Pages;
using PlanningDeGarde.Web.State;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 25 — Sc.5 (@back — acceptation de NIVEAU RUNTIME) : correction du bug rôle ≠ acteur du compte
/// connecté. La page de connexion réellement câblée (<see cref="Connexion"/>, API distante réelle
/// <see cref="ApiDistanteFactory"/>, handler <c>SeConnecterHandler</c> + référentiel réels, store réel)
/// se connecte avec un compte Actif lié 1-1 à un acteur de type <b>Autre</b> (« nounou » = Nina, seed).
/// Après connexion : l'identité RÉELLE de la session est ancrée sur CET acteur (id stable), l'identité
/// effective la reflète (aucune incarnation résiduelle — la connexion n'est PAS une impersonation), et
/// le gating d'écriture suit le type RÉEL de l'acteur (Autre → pas les droits Parent), et non un rôle
/// Parent hérité du configurateur en dur. Preuve sur câblage réel, jamais une doublure.
/// </summary>
public sealed class FrontWasmConnexionAncreIdentiteReelleSurActeurRuntimeTests : TestContext
{
    private const string ActeurNounou = "nounou"; // seed : Nina la nounou, TypeActeur.Autre

    [Fact]
    public void Should_ancrer_l_identite_reelle_sur_l_acteur_du_compte_et_refleter_son_type_When_je_me_connecte_avec_un_compte_lie_a_un_acteur_Autre()
    {
        // Given — la page de connexion réellement câblée à l'API distante réelle ; un compte ACTIF
        // « mamie@foyer.fr » lié 1-1 à l'acteur « nounou » (type Autre au seed).
        using var api = new ApiDistanteFactory();
        api.Services.GetRequiredService<IEditeurComptes>()
            .Creer("compte-mamie-s25", "mamie@foyer.fr", StatutCompte.Actif, ActeurNounou);
        Services.AddSingleton(GrilleRuntimeHarness.ClientVers(api));
        var session = new SessionPlanning();
        Services.AddSingleton(session);

        var connexion = RenderComponent<Connexion>();

        // When — je me connecte avec cet email (POST /api/canal/se-connecter réel).
        connexion.Find("[data-testid='champ-email-connexion']").Change("mamie@foyer.fr");
        connexion.Find("[data-testid='bouton-se-connecter']").Click();

        // Then — l'identité RÉELLE de la session est l'acteur du compte (id stable « nounou »), l'identité
        // effective la reflète, AUCUNE incarnation n'est active (la connexion n'est pas une impersonation)…
        connexion.WaitForAssertion(
            () =>
            {
                Assert.Equal(ActeurNounou, session.IdentiteReelle.Id);
                Assert.Equal(ActeurNounou, session.IdentiteEffective.Id);
                Assert.False(session.IncarnationActive);
                Assert.Null(session.LibelleBandeau);
                // … et le gating d'écriture suit le type RÉEL de l'acteur (Autre → pas les droits Parent),
                // et non un rôle Parent hérité du configurateur en dur.
                Assert.Equal(TypeActeur.Autre, session.IdentiteReelle.Type);
                Assert.False(session.EstParent);
            },
            TimeSpan.FromSeconds(10));
    }
}
