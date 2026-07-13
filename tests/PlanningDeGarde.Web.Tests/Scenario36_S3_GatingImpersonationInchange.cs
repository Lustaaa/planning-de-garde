using System.Collections.Generic;
using PlanningDeGarde.Application;
using PlanningDeGarde.Web.State;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 36 — Sc.3 — Non-régression du GATING d'écriture impersonation R8/R9 (@back).
///
/// La bascule s36 (éligibilité du lien enfant↔parent → <see cref="TypeActeur.Parent"/>) touche EXCLUSIVEMENT
/// <c>LierEnfantParentHandler</c>. Le <b>droit d'écriture</b> (règle 9) reste dérivé — inchangé — de
/// <see cref="SessionPlanning.IdentiteEffective"/>.<see cref="IdentiteActeur.Type"/> (règle 8) : c'est une
/// <b>surface INDÉPENDANTE</b> de l'éligibilité du lien. Ce garde consolidé verrouille la table de vérité
/// R8/R9 : une identité effective non-Parent ne gagne AUCUN droit d'écriture du fait de la bascule.
///
/// <b>Non-régression / early-green attendu</b> : ce garde caractérise l'invariant EXISTANT ; il n'existe que
/// pour échouer si un futur couplage réintroduisait une dérivation du gating depuis le critère du lien.
/// L'acceptation runtime de l'impersonation (s14, retour identité réelle / repli sur suppression) reste
/// couverte par les tests <c>FrontWasmImpersonation…</c> / <c>SessionPlanningIncarnation…</c>, tous verts.
/// </summary>
public sealed class Scenario36_S3_GatingImpersonationInchange
{
    private static SessionPlanning SessionIncarnant(string id, string nom, TypeActeur type)
    {
        var session = new SessionPlanning
        {
            ActeursIncarnables = new List<IdentiteActeur> { new(id, nom, type) },
        };
        session.Incarner(id);
        return session;
    }

    [Theory]
    [InlineData(TypeActeur.Parent, true)]  // Parent effectif → écriture autorisée
    [InlineData(TypeActeur.Admin, true)]   // Admin effectif → écriture autorisée
    [InlineData(TypeActeur.Autre, false)]  // Autre effectif → écriture MASQUÉE (aucun droit gagné par la bascule)
    public void Le_droit_d_ecriture_reste_derive_du_type_de_l_identite_effective_R8_R9(TypeActeur type, bool ecritureAttendue)
    {
        var session = SessionIncarnant("acteur", "Acteur", type);

        // Le gating dérive du TYPE de l'identité effective — jamais du critère d'éligibilité du lien (s36).
        Assert.Equal(ecritureAttendue, session.EstParent);
    }

    [Fact]
    public void Un_Invite_reste_en_consultation_seule_meme_en_incarnant_un_Parent_composition_inchangee()
    {
        var session = SessionIncarnant("parent-b", "Bruno", TypeActeur.Parent);
        session.Role = RoleAuteur.Invite;

        // La composition avec le rôle démo (Invité = lecture seule) reste honorée après la bascule s36.
        Assert.False(session.EstParent);
    }
}
