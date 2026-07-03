using System;
using System.Collections.Generic;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Web.Components.Pages;
using PlanningDeGarde.Web.State;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 25 — Sc.6 (@back — acceptation de NIVEAU RUNTIME) : non-régression de l'impersonation bornée
/// (lecture seule, s14) AU-DESSUS de la connexion corrigée (Sc.5). Un compte Actif de type Parent
/// (« parent-a » = Alice) est connecté via la page réellement câblée (<see cref="Connexion"/>, API
/// distante réelle <see cref="ApiDistanteFactory"/>, handler + référentiel + store réels) : l'identité
/// réelle de la session est désormais l'acteur du compte (Sc.5), PLUS le configurateur en dur. On incarne
/// alors un autre acteur déclaré : la vue suit l'identité effective incarnée (gating règle 9 piloté par
/// l'incarné) ; le retour à l'identité réelle ramène à l'acteur du COMPTE connecté (Alice), et non au
/// configurateur ; et la suppression concurrente de l'acteur incarné replie sur l'identité réelle du
/// compte (D2, SignalR s14). Preuve sur câblage réel, jamais une doublure.
/// </summary>
public sealed class FrontWasmImpersonationAuDessusDeLaConnexionRuntimeTests : TestContext
{
    private const string ActeurAlice = "parent-a"; // seed : Alice, TypeActeur.Parent (compte connecté)
    private const string ActeurNounou = "nounou";  // seed : Nina la nounou, TypeActeur.Autre (incarné)

    [Fact]
    public void Should_suivre_l_incarne_puis_revenir_a_l_acteur_du_compte_et_replier_sur_lui_When_l_incarne_est_supprime()
    {
        // Given — un compte ACTIF Parent « alice@foyer.fr » lié à Alice, connecté via la page réellement
        // câblée à l'API distante réelle. L'identité réelle de la session est ancrée sur Alice (Sc.5).
        using var api = new ApiDistanteFactory();
        api.Services.GetRequiredService<IEditeurComptes>()
            .Creer("compte-alice-s25", "alice@foyer.fr", StatutCompte.Actif, ActeurAlice);
        Services.AddSingleton(GrilleRuntimeHarness.ClientVers(api));
        var session = new SessionPlanning();
        Services.AddSingleton(session);

        var connexion = RenderComponent<Connexion>();
        this.SurDispatcher(() => connexion.Find("[data-testid='champ-email-connexion']").Change("alice@foyer.fr"));
        this.SurDispatcher(() => connexion.Find("[data-testid='bouton-se-connecter']").Click());
        connexion.WaitForAssertion(
            () =>
            {
                Assert.Equal(ActeurAlice, session.IdentiteReelle.Id);
                Assert.True(session.EstParent); // Alice est Parent : droits d'écriture
            },
            TimeSpan.FromSeconds(10));

        // When (incarnation) — j'incarne un autre acteur déclaré (nounou, type Autre) — impersonation
        // bornée lecture s14. Le catalogue incarnable est celui du référentiel réel chargé par la page.
        session.Incarner(ActeurNounou);

        // Then — la vue suit l'identité effective incarnée : gating règle 9 piloté par l'incarné (Autre →
        // pas les droits Parent), bandeau « Vous incarnez … ».
        Assert.True(session.IncarnationActive);
        Assert.Equal(ActeurNounou, session.IdentiteEffective.Id);
        Assert.False(session.EstParent);

        // When (retour) — je reviens à mon identité réelle.
        session.RevenirIdentiteReelle();

        // Then — le retour ramène à l'acteur du COMPTE connecté (Alice), et NON au configurateur en dur.
        Assert.False(session.IncarnationActive);
        Assert.Equal(ActeurAlice, session.IdentiteEffective.Id);
        Assert.Equal(ActeurAlice, session.IdentiteReelle.Id);
        Assert.True(session.EstParent);

        // When (suppression concurrente) — j'incarne de nouveau nounou, puis nounou est supprimée
        // concurremment (le catalogue rafraîchi via SignalR ne la contient plus, D2 s14).
        session.Incarner(ActeurNounou);
        Assert.True(session.IncarnationActive);
        session.ActeursIncarnables = new List<IdentiteActeur>
        {
            new(ActeurAlice, "Alice", TypeActeur.Parent),
            new("parent-b", "Bruno", TypeActeur.Parent),
        };

        var aReplie = session.ReplierSiActeurIncarneAbsent();

        // Then — le repli automatique ramène sur l'identité réelle DU COMPTE (Alice), sans nom fantôme,
        // et non au configurateur en dur.
        Assert.True(aReplie);
        Assert.False(session.IncarnationActive);
        Assert.Equal(ActeurAlice, session.IdentiteEffective.Id);
        Assert.True(session.EstParent);
    }
}
