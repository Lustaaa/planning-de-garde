using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 30 — S5 — Le port d'énumération liste les enfants du foyer (@back)
//   Étant donné un référentiel d'enfants contenant "Léa" et "Tom"
//   Quand on énumère les enfants du foyer via le port de droite
//   Alors la liste porte "Léa" et "Tom" avec leurs identifiants stables et prénoms
//   Et la liste est dédoublonnée par identifiant (jamais par libellé)
//
// Le contrat d'énumération (IEnumerationEnfants) a été livré et piloté en S1 (l'acceptation S1 relit
// l'enfant ajouté par ce port). S5 CARACTÉRISE / verrouille la propriété distinctive du port réalisé
// par le store réel : l'unicité de sortie est portée par l'IDENTIFIANT STABLE (clé), jamais par le
// prénom — deux enfants de MÊME prénom mais d'id distincts restent DEUX entrées. Aucune construction
// de production neuve n'est requise (dictionnaire id→prénom du store, S1) : test de non-régression du contrat.
public class Scenario30_S5_EnumererEnfants
{
    [Fact]
    public void Should_Lister_Lea_et_Tom_avec_leurs_ids_stables_et_prenoms_When_on_enumere_le_referentiel()
    {
        var referentiel = new ReferentielEnfantsEnMemoire();
        referentiel.Ajouter("enfant-lea", "Léa");
        referentiel.Ajouter("enfant-tom", "Tom");

        var enfants = referentiel.EnumererEnfants();

        Assert.Equal(2, enfants.Count);
        Assert.Equal("Léa", enfants.Single(e => e.Id == "enfant-lea").Prenom);
        Assert.Equal("Tom", enfants.Single(e => e.Id == "enfant-tom").Prenom);
    }

    [Fact]
    public void Should_Dedoublonner_par_identifiant_jamais_par_prenom_When_deux_enfants_partagent_le_meme_prenom()
    {
        var referentiel = new ReferentielEnfantsEnMemoire();
        // Deux enfants distincts de MÊME prénom "Léa" (id stables différents) : le foyer peut avoir
        // deux enfants homonymes — le port ne les fusionne PAS (dédoublonnage par id, pas par prénom).
        referentiel.Ajouter("enfant-lea-1", "Léa");
        referentiel.Ajouter("enfant-lea-2", "Léa");

        var enfants = referentiel.EnumererEnfants();

        Assert.Equal(2, enfants.Count);
        Assert.Contains(enfants, e => e.Id == "enfant-lea-1");
        Assert.Contains(enfants, e => e.Id == "enfant-lea-2");
    }
}
