using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;
using Xunit;

namespace PlanningDeGarde.Tests;

// Retour PO s54 (passe architecte) — « les adresses des lieux ne sont pas persistées » :
//   trou backend à la CRÉATION — la commande d'ajout ne portait pas l'adresse, donc l'adresse saisie au
//   formulaire de création était silencieusement perdue (seul le libellé était écrit). Le formulaire
//   d'édition, lui, persistait déjà l'adresse (Scenario35_S2). On borne ici le comportement du handler
//   d'ajout : une adresse fournie à la création est persistée dans la foulée sur la même clé stable.
public class Retour55_AdresseCreationActivite
{
    private static AjouterActiviteHandler NouveauHandler(ReferentielActivitesEnMemoire referentiel)
        => new(referentiel, referentiel);

    // Étant donné un référentiel de lieux, quand un parent crée un lieu AVEC une adresse, alors
    // l'énumération relit l'adresse (plus de perte silencieuse à la création).
    [Fact]
    public void Should_Persister_l_adresse_fournie_a_la_creation()
    {
        var referentiel = new ReferentielActivitesEnMemoire();

        var resultat = NouveauHandler(referentiel).Handle(new AjouterActiviteCommand("piscine", "3 allée du Bassin"));

        Assert.True(resultat.EstSucces);
        var activite = referentiel.EnumererActivites().Single(a => a.Id == "piscine");
        Assert.Equal("piscine", activite.Libelle);
        Assert.Equal("3 allée du Bassin", activite.Adresse); // adresse relue (persistée à la création)
    }

    // Adresse non fournie (null) à la création : le lieu existe, adresse « vide » à la lecture (champ optionnel).
    [Fact]
    public void Should_Creer_sans_adresse_When_aucune_adresse_fournie()
    {
        var referentiel = new ReferentielActivitesEnMemoire();

        var resultat = NouveauHandler(referentiel).Handle(new AjouterActiviteCommand("piscine"));

        Assert.True(resultat.EstSucces);
        var activite = referentiel.EnumererActivites().Single(a => a.Id == "piscine");
        Assert.Equal("", activite.Adresse); // aucune adresse écrite (optionnelle)
    }

    // Une adresse tout-espaces fournie n'introduit pas de bruit : le lieu est créé, adresse vide à la lecture.
    [Fact]
    public void Should_Ignorer_une_adresse_tout_espaces_a_la_creation()
    {
        var referentiel = new ReferentielActivitesEnMemoire();

        NouveauHandler(referentiel).Handle(new AjouterActiviteCommand("piscine", "   "));

        var activite = referentiel.EnumererActivites().Single(a => a.Id == "piscine");
        Assert.Equal("", activite.Adresse);
    }
}
