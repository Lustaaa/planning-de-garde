using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 07 — Sc.5 — Acteur hors set (gris assumé) : nom conservé, teinte neutre (@limite)
//   Given un acteur « grand-père » à l'identifiant stable valide mais ABSENT du set de couleurs
//   And une période lui confie la garde le samedi 04/07/2026
//   When la grille est projetée
//   Then la case du 04/07 porte le nom « grand-père » sur couleur NEUTRE (gris assumé)
//   And la légende contient une entrée « grand-père » (gris)
//
// CARACTÉRISATION backend (early green ATTENDU, filet — PAS un driver). Invariant clé : le NOM se
// résout via le référentiel INDÉPENDAMMENT de la couleur ; même quand la couleur retombe au neutre
// (acteur hors set couleur), le nom reste intact. Le repli neutre de la couleur est déjà garanti
// par le contrat IPaletteCouleurs.CouleurDe (note s06, gris ASSUMÉ — pas le gris-bug du s06 Sc.8).
// Le rendu (case grise + nom + entrée légende grise) relève d'ihm-builder.
public class Scenario_ActeurHorsSetNomConserve
{
    private const string GrandPere = "grand-pere";
    private const string NomGrandPere = "grand-père";
    private const string ParentA = "parent-a";
    private const string Bleu = "bleu";

    private static readonly DateOnly Lundi_29_06_2026 = new(2026, 6, 29);
    private static readonly DateOnly Samedi_04_07_2026 = new(2026, 7, 4);

    // Set de couleurs SANS grand-pere (mais avec parent-a, discriminance) → grand-pere retombe au neutre.
    private static IPaletteCouleurs PaletteSansGrandPere()
        => new FakePaletteCouleurs(new Dictionary<string, string> { [ParentA] = Bleu });

    // Référentiel qui NOMME grand-pere (id stable valide, présent au référentiel des noms).
    private static IReferentielResponsables ReferentielAvecGrandPere()
        => new FakeReferentielResponsables(new Dictionary<string, string> { [GrandPere] = NomGrandPere });

    private static FakePeriodeRepository PeriodeGrandPereLe_04_07()
    {
        var periodes = new FakePeriodeRepository();
        periodes.Enregistrer(PeriodeDeGarde.Affecter(GrandPere, new DateTime(2026, 7, 4), new DateTime(2026, 7, 4)).Valeur!);
        return periodes;
    }

    private static GrilleAgendaQuery Query()
        => new(new FakeSlotRepository(), PeriodeGrandPereLe_04_07(), PaletteSansGrandPere(), ReferentielAvecGrandPere());

    // Test #1 — Caractérisation : nom conservé MALGRÉ la couleur neutre (résolution indépendante).
    [Fact]
    public void Should_Conserver_le_nom_de_l_acteur_hors_set_avec_une_couleur_neutre_et_une_entree_de_legende_grise_When_un_acteur_au_identifiant_stable_valide_non_colorie_est_responsable()
    {
        var grille = Query().Projeter(Lundi_29_06_2026);

        // la case du 04/07 porte le NOM (référentiel) ET la couleur NEUTRE (palette, repli) — indépendants
        var caseGrandPere = grille.Jours.Single(j => j.Date == Samedi_04_07_2026);
        Assert.Equal(NomGrandPere, caseGrandPere.NomResponsable);
        Assert.Equal(FakePaletteCouleurs.Neutre, caseGrandPere.CouleurResponsable);

        // la légende contient une entrée grand-père neutre : nom conservé, couleur neutre
        var entree = Assert.Single(grille.Légende);
        Assert.Equal(GrandPere, entree.IdentifiantStable);
        Assert.Equal(NomGrandPere, entree.Nom);
        Assert.Equal(FakePaletteCouleurs.Neutre, entree.Couleur);
    }
}
