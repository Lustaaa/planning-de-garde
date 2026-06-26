using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Scénario 3 — La case-jour prend la couleur du parent responsable de la période (@nominal)
//   Given Le set de couleurs associe Parent A = bleu, Parent B = orange, et une période
//         confie Léa à Parent A du lundi 22/06 au dimanche 28/06/2026
//   When  Un Parent consulte la grille le 24/06/2026
//   Then  Chaque case-jour du 22/06 au 28/06 porte la couleur de Parent A (bleu), distincte
//         de celle de Parent B (orange)
//
// Projection backend GrilleAgendaQuery — testée sans Blazor. Date de référence injectée ;
// FakePeriodeRepository peuplé via l'agrégat PeriodeDeGarde ; set de couleurs injecté via le
// port IPaletteCouleurs (doublé à la main). La couleur est une donnée du read model, pas un
// style rendu : on assert sur JourCase.CouleurResponsable.
public class Scenario_CouleurResponsableCaseJour
{
    private const string ParentA = "parent-a";
    private const string ParentB = "parent-b";
    private const string Bleu = "bleu";
    private const string Orange = "orange";

    private static readonly DateOnly Date_24_06_2026 = new(2026, 6, 24);
    private static readonly DateOnly Lundi_22_06_2026 = new(2026, 6, 22);
    private static readonly DateOnly Dimanche_28_06_2026 = new(2026, 6, 28);

    private static IPaletteCouleurs PaletteParentABleuParentBOrange()
        => new FakePaletteCouleurs(new Dictionary<string, string>
        {
            [ParentA] = Bleu,
            [ParentB] = Orange,
        });

    private static FakePeriodeRepository PeriodeConfiantLeaAParentADu_22_au_28_06()
    {
        var periodes = new FakePeriodeRepository();
        var periode = PeriodeDeGarde
            .Affecter(ParentA, new DateTime(2026, 6, 22), new DateTime(2026, 6, 28))
            .Valeur!;
        periodes.Enregistrer(periode);
        return periodes;
    }

    private static GrilleAgendaQuery Query(FakePeriodeRepository periodes, IPaletteCouleurs palette)
        => new(new FakeSlotRepository(), periodes, palette);

    // ---------- Test d'acceptation (boucle externe, BDD) ----------

    [Fact]
    public void Should_Colorer_les_cases_du_lundi_22_06_au_dimanche_28_06_2026_de_la_couleur_de_Parent_A_distincte_de_celle_de_Parent_B_When_une_periode_confie_Lea_a_Parent_A_sur_cet_intervalle()
    {
        // Given — Parent A = bleu, Parent B = orange ; une période confie Léa à Parent A du 22 au 28/06
        var query = Query(PeriodeConfiantLeaAParentADu_22_au_28_06(), PaletteParentABleuParentBOrange());

        // When — un Parent consulte la grille le 24/06/2026
        var grille = query.Projeter(Date_24_06_2026);

        // Then — chaque case du 22/06 au 28/06 porte la couleur de Parent A (bleu)...
        var casesCouvertes = grille.Jours
            .Where(j => j.Date >= Lundi_22_06_2026 && j.Date <= Dimanche_28_06_2026)
            .ToList();
        Assert.Equal(7, casesCouvertes.Count);
        Assert.All(casesCouvertes, j => Assert.Equal(Bleu, j.CouleurResponsable));

        // And — bleu (Parent A) est distinct de la couleur de Parent B (orange)
        Assert.NotEqual(Orange, Bleu);
    }

    // ---------- Tests unitaires (boucle interne, TDD) ----------

    // Test #1 — la projection lit IPeriodeRepository, calcule les jours couverts par la période
    // et attribue à chaque case couverte la couleur du responsable (mapping via la palette).
    // (TPP constante neutre → valeur dérivée.) Driver : les cases des Sc.1/Sc.2 portent la
    // couleur neutre (aucune lecture des périodes) ; ce test force la lecture + le mapping.
    [Fact]
    public void Should_Attribuer_la_couleur_du_parent_responsable_aux_cases_couvertes_par_sa_periode_When_une_periode_confie_Lea_a_Parent_A_sur_un_intervalle_interne()
    {
        var query = Query(PeriodeConfiantLeaAParentADu_22_au_28_06(), PaletteParentABleuParentBOrange());

        var grille = query.Projeter(Date_24_06_2026);

        var casesCouvertes = grille.Jours
            .Where(j => j.Date >= Lundi_22_06_2026 && j.Date <= Dimanche_28_06_2026)
            .ToList();
        Assert.All(casesCouvertes, j => Assert.Equal(Bleu, j.CouleurResponsable));
    }

    // Test #2 (ex-#3) — présence + absence couplées : les jours non couverts par une période
    // conservent la couleur neutre, tandis que les jours couverts restent bleus, dans la MÊME
    // grille. Driver : une implémentation qui colorerait toute la grille de la couleur du
    // responsable (au lieu des seuls jours couverts) échoue. Prépare le Sc.6 (intersection partielle).
    [Fact]
    public void Should_Conserver_la_couleur_neutre_sur_les_cases_hors_periode_When_aucune_periode_ne_couvre_ces_jours()
    {
        var query = Query(PeriodeConfiantLeaAParentADu_22_au_28_06(), PaletteParentABleuParentBOrange());

        var grille = query.Projeter(Date_24_06_2026);

        // Les jours couverts (22/06 → 28/06) sont bleus...
        Assert.All(
            grille.Jours.Where(j => j.Date >= Lundi_22_06_2026 && j.Date <= Dimanche_28_06_2026),
            j => Assert.Equal(Bleu, j.CouleurResponsable));

        // ...et tous les autres jours de la grille conservent la couleur neutre (gris).
        Assert.All(
            grille.Jours.Where(j => j.Date < Lundi_22_06_2026 || j.Date > Dimanche_28_06_2026),
            j => Assert.Equal(FakePaletteCouleurs.Neutre, j.CouleurResponsable));
    }
}
