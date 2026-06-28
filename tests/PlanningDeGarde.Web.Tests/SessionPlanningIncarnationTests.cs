using System.Collections.Generic;
using PlanningDeGarde.Application;
using PlanningDeGarde.Web.State;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Inner-loop du Sc.1 (boucle rapide POCO, logique pure) — l'identité réelle vs effective de
/// <see cref="SessionPlanning"/> (impersonation bornée, sprint 14). <b>L'acceptation reste RUNTIME</b>
/// (cf. <see cref="FrontWasmIncarnerRefleteRoleTempsReelTests"/>) : ces tests cadrent la logique de
/// dérivation du rôle depuis le type de l'identité effective, ils ne prouvent pas le câblage IHM/DI.
/// </summary>
public sealed class SessionPlanningIncarnationTests
{
    private static SessionPlanning AvecCatalogue()
    {
        var session = new SessionPlanning
        {
            ActeursIncarnables = new List<IdentiteActeur>
            {
                new("parent-b", "Bruno", TypeActeur.Parent),
                new("nounou", "Nina la nounou", TypeActeur.Autre),
                new("parent-c", "Carla", TypeActeur.Admin),
            },
        };
        return session;
    }

    // #1 — caractérisation de départ : hors incarnation, l'identité effective EST l'identité réelle
    // (Parent configurateur, actions autorisées), aucun bandeau.
    [Fact]
    public void Should_PresenterLeConfigurateurSousSonIdentiteReelleParent_When_AucuneIncarnationNEstActive()
    {
        var session = AvecCatalogue();

        Assert.False(session.IncarnationActive);
        Assert.Null(session.LibelleBandeau);
        Assert.Equal(session.IdentiteReelle, session.IdentiteEffective);
        Assert.True(session.EstParent);
    }

    // #2 — driver : incarner un acteur de type Parent force l'identité effective ≠ réelle et le libellé
    // du bandeau « Vous incarnez Bruno » ; les actions d'écriture restent autorisées.
    [Fact]
    public void Should_AfficherLeBandeauEtRefleterLActeurIncarne_When_OnIncarneUnActeurDeclareDeTypeParent()
    {
        var session = AvecCatalogue();

        session.Incarner("parent-b");

        Assert.True(session.IncarnationActive);
        Assert.Equal("Vous incarnez Bruno", session.LibelleBandeau);
        Assert.Equal("parent-b", session.IdentiteEffective.Id);
        Assert.True(session.EstParent);
    }

    // #3 — driver (règle 8) : incarner un acteur de type Autre masque les actions d'écriture — EstParent
    // dérive du type EFFECTIF, pas d'un droit inconditionnel.
    [Fact]
    public void Should_MasquerLesActionsDEcriture_When_OnIncarneUnActeurDeTypeAutre()
    {
        var session = AvecCatalogue();

        session.Incarner("nounou");

        Assert.True(session.IncarnationActive);
        Assert.Equal("Vous incarnez Nina la nounou", session.LibelleBandeau);
        Assert.False(session.EstParent);
    }

    // #4 — caractérisation (même branche que #2, type ∈ {Parent, Admin}) : incarner un Admin conserve
    // les actions d'écriture.
    [Fact]
    public void Should_ConserverLesActionsDEcriture_When_OnIncarneUnActeurDeTypeAdmin()
    {
        var session = AvecCatalogue();

        session.Incarner("parent-c");

        Assert.True(session.IncarnationActive);
        Assert.True(session.EstParent);
    }

    // Refus silencieux (socle Sc.3) : incarner un identifiant absent du catalogue conserve l'identité réelle.
    [Fact]
    public void Should_ConserverLIdentiteReelle_When_OnIncarneUnIdentifiantInconnu()
    {
        var session = AvecCatalogue();

        session.Incarner("acteur-inexistant");

        Assert.False(session.IncarnationActive);
        Assert.Equal(session.IdentiteReelle, session.IdentiteEffective);
    }

    // Retour à l'identité réelle (socle Sc.2) : après incarnation, le retour restaure l'état.
    [Fact]
    public void Should_RestaurerLIdentiteReelle_When_OnRevientApresUneIncarnation()
    {
        var session = AvecCatalogue();
        session.Incarner("nounou");

        session.RevenirIdentiteReelle();

        Assert.False(session.IncarnationActive);
        Assert.Null(session.LibelleBandeau);
        Assert.True(session.EstParent);
    }

    // Composition avec le rôle démo : un Invité reste en consultation seule même en incarnant un Parent.
    [Fact]
    public void Should_ResterEnConsultationSeule_When_LeRoleDemoEstInviteMemeEnIncarnantUnParent()
    {
        var session = AvecCatalogue();
        session.Role = RoleAuteur.Invite;

        session.Incarner("parent-b");

        Assert.False(session.EstParent);
    }
}
