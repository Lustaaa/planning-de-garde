using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 35 — Sc.2 — Champ « adresse » sur l'agrégat Activité (@back, frontière Application)
//   Miroir strict de l'adresse acteur s33 : l'adresse est une surface OPTIONNELLE indépendante du
//   libellé — la changer ne touche aucun autre champ (aucune écriture partielle), vide accepté. Un refus
//   (libellé vidé en même temps) laisse le store INCHANGÉ (l'adresse n'est PAS écrite partiellement).
//   La durabilité Mongo réelle est prouvée à part (Scenario35_S2_ActiviteAdresseMongoDurabiliteTests).
public class Scenario35_S2_ActiviteAdresse
{
    private const string Ecole = "école";

    // ---------- Acceptation (boucle externe, frontière Application) ----------
    // Étant donné une activité du référentiel, quand un parent change son adresse, alors l'énumération
    // relit l'adresse — l'id stable ET le libellé restent inchangés (surface indépendante).
    [Fact]
    public void Acceptation_Should_Relire_l_adresse_sans_toucher_id_ni_libelle_When_le_parent_change_l_adresse_d_une_activite()
    {
        var referentiel = new ReferentielActivitesEnMemoire();
        var editer = new EditerActiviteHandler(referentiel);

        var resultat = editer.Handle(new EditerActiviteCommand(Ecole, Adresse: "12 rue des Lilas"));

        Assert.True(resultat.EstSucces);
        var activite = referentiel.EnumererActivites().Single(a => a.Id == Ecole);
        Assert.Equal("12 rue des Lilas", activite.Adresse); // adresse relue par la query
        Assert.Equal(Ecole, activite.Id);                   // id stable inchangé
        Assert.Equal(Ecole, activite.Libelle);              // libellé inchangé (surface indépendante)
    }

    // ---------- Une adresse vide est acceptée (champ optionnel, comme l'adresse acteur s33) ----------
    [Fact]
    public void Should_Accepter_une_adresse_vide_When_le_parent_vide_l_adresse()
    {
        var referentiel = new ReferentielActivitesEnMemoire();
        var editer = new EditerActiviteHandler(referentiel);

        var resultat = editer.Handle(new EditerActiviteCommand(Ecole, Adresse: ""));

        Assert.True(resultat.EstSucces); // vide licite (≠ libellé, jamais vide)
        var activite = referentiel.EnumererActivites().Single(a => a.Id == Ecole);
        Assert.Equal("", activite.Adresse);
        Assert.Equal(Ecole, activite.Libelle); // libellé toujours intact
    }

    // ---------- Éditer le libellé ne touche pas l'adresse (surfaces indépendantes) ----------
    [Fact]
    public void Should_Preserver_l_adresse_When_seul_le_libelle_est_edite()
    {
        var referentiel = new FakeReferentielActivites().AvecActivite(Ecole);
        var editer = new EditerActiviteHandler(referentiel);
        editer.Handle(new EditerActiviteCommand(Ecole, Adresse: "12 rue des Lilas"));

        editer.Handle(new EditerActiviteCommand(Ecole, Libelle: "école primaire"));

        var activite = referentiel.EnumererActivites().Single(a => a.Id == Ecole);
        Assert.Equal("école primaire", activite.Libelle);      // libellé mis à jour
        Assert.Equal("12 rue des Lilas", activite.Adresse);    // adresse préservée (non touchée)
    }

    // ---------- Refus : libellé vidé EN MÊME TEMPS → store INCHANGÉ (pas d'écriture partielle) ----------
    // Le crux de Sc.2 : une édition qui fournit un libellé VIDE est refusée AVANT toute écriture — ni le
    // libellé (conservé) ni l'adresse fournie ne sont écrits (aucune écriture partielle de l'adresse).
    [Fact]
    public void Should_Laisser_le_store_inchange_When_le_libelle_est_vide_meme_avec_une_adresse_fournie()
    {
        var referentiel = new FakeReferentielActivites().AvecActivite(Ecole);
        var editer = new EditerActiviteHandler(referentiel);

        var resultat = editer.Handle(new EditerActiviteCommand(Ecole, Libelle: "   ", Adresse: "12 rue des Lilas"));

        Assert.False(resultat.EstSucces);         // refus métier (libellé requis)
        var activite = referentiel.EnumererActivites().Single(a => a.Id == Ecole);
        Assert.Equal(Ecole, activite.Libelle);    // libellé conservé (jamais vidé)
        Assert.Equal("", activite.Adresse);       // adresse NON écrite partiellement (store inchangé)
    }
}
