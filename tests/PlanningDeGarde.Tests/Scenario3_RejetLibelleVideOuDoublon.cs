using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 21 — Sc.3 — Rejet : libellé vide ou doublon (@back)
//   Tranche BACKEND (frontière Application) : création ET renommage rejettent un libellé vide (motif
//   « libellé requis ») ou un doublon de libellé (motif « libellé déjà défini »), SANS muter le
//   référentiel (aucun rôle vide ni doublon persisté). Les motifs sont clairs et observables sur le
//   Result. On NE teste PAS ici de rendu Blazor.
public class Scenario3_RejetLibelleVideOuDoublon
{
    private const string GrandParent = "Grand-parent";

    // ---------- Acceptation (boucle externe, frontière Application) ----------
    // Traduit le scénario Gherkin : un référentiel contient déjà « Grand-parent » ; une création de
    // libellé VIDE échoue (« libellé requis »), une création d'un SECOND « Grand-parent » échoue
    // (« libellé déjà défini »), et le référentiel reste inchangé (un seul rôle, aucun rôle vide).
    [Fact]
    public void Acceptation_Should_Rejeter_libelle_vide_et_doublon_avec_motif_clair_en_laissant_le_referentiel_inchange_When_le_parent_tente_les_deux_creations_fautives()
    {
        var referentiel = new ReferentielRolesEnMemoire();
        new CreerRoleHandler(referentiel, referentiel).Handle(new CreerRoleCommand(GrandParent));
        var handler = new CreerRoleHandler(referentiel, referentiel);

        var vide = handler.Handle(new CreerRoleCommand("   "));
        var doublon = handler.Handle(new CreerRoleCommand(GrandParent));

        Assert.False(vide.EstSucces);
        Assert.Equal("libellé requis", vide.Motif);            // motif clair : libellé requis
        Assert.False(doublon.EstSucces);
        Assert.Equal("libellé déjà défini", doublon.Motif);    // motif clair : libellé déjà défini
        var roles = referentiel.EnumererRoles();
        Assert.Single(roles);                                  // référentiel inchangé : un seul rôle
        Assert.Equal(GrandParent, roles.Single().Libelle);     // ... « Grand-parent », aucun rôle vide ni doublon
    }

    // ---------- Test #1 — Driver : création d'un libellé vide refusée, aucune écriture ----------
    // Contradiction : le handler écrit inconditionnellement. Force une garde « libellé requis » AVANT
    // toute écriture — le référentiel reste vide.
    [Fact]
    public void Should_Refuser_la_creation_d_un_libelle_vide_sans_ecrire_dans_le_referentiel_When_le_libelle_est_vide()
    {
        var referentiel = new FakeReferentielRoles();
        var handler = new CreerRoleHandler(referentiel, referentiel);

        var resultat = handler.Handle(new CreerRoleCommand(""));

        Assert.False(resultat.EstSucces);
        Assert.Equal("libellé requis", resultat.Motif);
        Assert.Empty(referentiel.EnumererRoles()); // aucune écriture : référentiel inchangé
    }

    // ---------- Test #2 — Driver : création d'un doublon de libellé refusée, aucun doublon écrit ----------
    // Contradiction : le handler écrit même si le libellé existe déjà. Force une garde « libellé déjà
    // défini » lue sur le référentiel courant — un seul rôle reste persisté.
    [Fact]
    public void Should_Refuser_la_creation_d_un_doublon_de_libelle_sans_ecrire_de_second_role_When_le_libelle_existe_deja()
    {
        var referentiel = new FakeReferentielRoles();
        referentiel.Creer("role-gp", GrandParent);
        var handler = new CreerRoleHandler(referentiel, referentiel);

        var resultat = handler.Handle(new CreerRoleCommand(GrandParent));

        Assert.False(resultat.EstSucces);
        Assert.Equal("libellé déjà défini", resultat.Motif);
        Assert.Single(referentiel.EnumererRoles()); // aucun doublon écrit
    }

    // ---------- Test #3 — Driver : renommage vers un libellé vide refusé, ancien libellé conservé ----------
    // Contradiction : le renommage mute inconditionnellement. Force une garde « libellé requis » — le
    // rôle conserve son libellé d'origine.
    [Fact]
    public void Should_Refuser_le_renommage_vers_un_libelle_vide_en_conservant_l_ancien_libelle_When_le_nouveau_libelle_est_vide()
    {
        var referentiel = new FakeReferentielRoles();
        referentiel.Creer("role-gp", GrandParent);
        var handler = new RenommerRoleHandler(referentiel, referentiel);

        var resultat = handler.Handle(new RenommerRoleCommand("role-gp", "  "));

        Assert.False(resultat.EstSucces);
        Assert.Equal("libellé requis", resultat.Motif);
        Assert.Equal(GrandParent, referentiel.EnumererRoles().Single(r => r.Id == "role-gp").Libelle); // inchangé
    }

    // ---------- Test #4 — Driver : renommage vers un libellé déjà porté par un AUTRE rôle refusé ----------
    // Contradiction : le renommage n'observe pas les autres rôles. Force une garde « libellé déjà
    // défini » excluant le rôle renommé lui-même — le rôle conserve son libellé, aucun doublon.
    [Fact]
    public void Should_Refuser_le_renommage_vers_un_libelle_deja_porte_par_un_autre_role_en_conservant_l_ancien_libelle_When_le_nouveau_libelle_est_un_doublon()
    {
        var referentiel = new FakeReferentielRoles();
        referentiel.Creer("role-gp", GrandParent);
        referentiel.Creer("role-nounou", "Nounou");
        var handler = new RenommerRoleHandler(referentiel, referentiel);

        var resultat = handler.Handle(new RenommerRoleCommand("role-nounou", GrandParent));

        Assert.False(resultat.EstSucces);
        Assert.Equal("libellé déjà défini", resultat.Motif);
        Assert.Equal("Nounou", referentiel.EnumererRoles().Single(r => r.Id == "role-nounou").Libelle); // inchangé
    }
}
