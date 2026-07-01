using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 22 — Sc.5 — Deux parents peuvent être admins (@back)
//   L'invariant admin=parent borne le TYPE, pas l'unicité : quand les deux parents sont utilisateurs,
//   désigner l'un PUIS l'autre réussit, et le foyer reconnaît DEUX admins, tous deux Parent. Aucun
//   acteur non-Parent ne peut rejoindre l'ensemble des admins (invariant tenu, cardinal libre).
public class Scenario5_DeuxParentsAdmins
{
    private const string ParentA = "acteur-parent-a";
    private const string ParentB = "acteur-parent-b";
    private const string Nounou = "acteur-nounou";

    private static IEnumerationActeursFoyer Acteurs() => new FakeActeursTypes(new Dictionary<string, TypeActeur>
    {
        [ParentA] = TypeActeur.Parent,
        [ParentB] = TypeActeur.Parent,
        [Nounou] = TypeActeur.Autre,
    });

    // ---------- Test #1 — Domain : DEUX parents distincts coexistent dans l'ensemble des admins ----------
    // Propriété distincte du Sc.4 (qui n'a prouvé qu'UN admin) : l'invariant ne borne pas le cardinal.
    // Force que désigner deux parents distincts les conserve TOUS DEUX.
    [Fact]
    public void Domain_Should_Reconnaitre_deux_admins_When_deux_parents_distincts_sont_designes()
    {
        var administration = AdministrationFoyer.FromSnapshot(new string[0]);

        Assert.True(administration.DesignerAdmin(ParentA, acteurEstParent: true).EstSucces);
        Assert.True(administration.DesignerAdmin(ParentB, acteurEstParent: true).EstSucces);

        Assert.Contains(ParentA, administration.Admins);
        Assert.Contains(ParentB, administration.Admins);
        Assert.Equal(2, administration.Admins.Count); // le cardinal n'est pas borné à 1
    }

    // ---------- Acceptation — désigner l'un puis l'autre parent réussit et persiste les deux ----------
    [Fact]
    public void Acceptation_Should_Reconnaitre_deux_parents_admins_When_on_les_designe_l_un_puis_l_autre()
    {
        var admins = new AdminsFoyerEnMemoire();
        var handler = new DesignerAdminHandler(admins, admins, Acteurs());

        Assert.True(handler.Handle(new DesignerAdminCommand(ParentA)).EstSucces);
        Assert.True(handler.Handle(new DesignerAdminCommand(ParentB)).EstSucces);

        var ensemble = admins.EnumererAdmins();
        Assert.Contains(ParentA, ensemble);
        Assert.Contains(ParentB, ensemble);
        Assert.Equal(2, ensemble.Count); // deux admins reconnus, l'unicité n'est pas bornée
    }

    // ---------- Acceptation — un non-Parent ne rejoint jamais l'ensemble, même à côté de deux parents admins ----------
    [Fact]
    public void Acceptation_Should_Refuser_le_non_parent_et_conserver_seulement_les_deux_parents_When_on_tente_d_ajouter_une_nounou_aux_admins()
    {
        var admins = new AdminsFoyerEnMemoire();
        var handler = new DesignerAdminHandler(admins, admins, Acteurs());
        Assert.True(handler.Handle(new DesignerAdminCommand(ParentA)).EstSucces);
        Assert.True(handler.Handle(new DesignerAdminCommand(ParentB)).EstSucces);

        var tentative = handler.Handle(new DesignerAdminCommand(Nounou));

        Assert.False(tentative.EstSucces);
        var ensemble = admins.EnumererAdmins();
        Assert.DoesNotContain(Nounou, ensemble);          // invariant tenu malgré le cardinal libre
        Assert.Equal(2, ensemble.Count);                  // seuls les deux parents restent admins
    }
}
