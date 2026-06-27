using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Scénario 5 — Date figée hors fenêtre fait disparaître la saisie (@erreur)
//   Given la date de référence est le 26/06/2026 ; un slot est posé au 15/07/2025 (date par
//         défaut figée, non corrigée)
//   When  la grille du 26/06/2026 est projetée
//   Then  aucune case de la fenêtre ne porte ce slot ; et la saisie SEMBLE avoir disparu alors
//         qu'elle est bien ENREGISTRÉE hors fenêtre (le store contient toujours le slot)
//
// CARACTÉRISATION / DIAGNOSTIC (early green attendu) : ce scénario ne corrige rien — il
// DOCUMENTE la cause du faux bug « les saisies n'apparaissent pas ». La fenêtre démarre au
// lundi 22/06/2026 (35 jours) ; un slot au 15/07/2025 n'a aucune case d'accueil. L'invariant
// d'exclusion hors fenêtre est déjà vert (Scenario_SlotHorsFenetreExclu). La pointe
// DISCRIMINANTE : asserter que le slot est BIEN PRÉSENT dans le store (le repository le rend)
// tout en étant ABSENT de toute case — c'est un défaut de DATE FIGÉE, pas une non-persistance.
// La correction vit dans les formulaires (Sc.1/Sc.2/Sc.3, IDateTimeProvider).
public class Scenario_DateFigeeHorsFenetre
{
    private static readonly DateOnly Date_26_06_2026 = new(2026, 6, 26);
    private static readonly DateOnly DateFigee_15_07_2025 = new(2025, 7, 15);

    // Un slot posé à la DATE FIGÉE par défaut (15/07/2025), non corrigée par le parent. La
    // fenêtre projetée (lundi 22/06/2026 → 26/07/2026) est ~11 mois plus tard : le slot est
    // enregistré mais hors de toute case.
    private static FakeSlotRepository SlotPoseALaDateFigee_15_07_2025()
    {
        var slots = new FakeSlotRepository();
        var slot = SlotDeLocalisation
            .Poser("lea", "ecole", new DateTime(2025, 7, 15, 8, 0, 0), new DateTime(2025, 7, 15, 17, 0, 0))
            .Valeur!;
        slots.Enregistrer(slot);
        return slots;
    }

    // ---------- Test d'acceptation (boucle externe, BDD) ----------

    [Fact]
    public void Should_N_inclure_dans_aucune_case_de_la_fenetre_le_slot_pose_au_15_07_2025_While_il_reste_enregistre_When_la_grille_du_26_06_2026_est_projetee()
    {
        // Given — un slot posé au 15/07/2025 (date figée), bien enregistré dans le store
        var slots = SlotPoseALaDateFigee_15_07_2025();
        var query = new GrilleAgendaQuery(
            slots,
            new FakePeriodeRepository(),
            new FakePaletteCouleurs(new Dictionary<string, string>()),
            new FakeReferentielResponsables(new Dictionary<string, string>()));

        // When — la grille du 26/06/2026 est projetée
        var grille = query.Projeter(Date_26_06_2026);

        // Then — aucune case de la fenêtre ne porte ce slot
        Assert.Equal(0, grille.Jours.Sum(j => j.Slots.Count));
        Assert.DoesNotContain(grille.Jours, j => j.Date == DateFigee_15_07_2025);

        // And — la saisie SEMBLE disparue mais reste ENREGISTRÉE hors fenêtre (le store la rend)
        var snapshot = Assert.Single(slots.AllSnapshots());
        Assert.Equal(DateFigee_15_07_2025, DateOnly.FromDateTime(snapshot.Debut));
    }

    // ---------- Test unitaire (boucle interne, TDD) ----------

    // Test #1 — discriminance « enregistré ≠ affiché » : le slot du 15/07/2025 demeure dans le
    // store (repository.AllSnapshots() le rend) tout en étant absent de toute case de la grille.
    // Couple présence dans le store + absence dans la grille : une grille vide qui passerait sans
    // que le slot soit réellement persisté ne prouverait pas le diagnostic « date figée ».
    // Early green ANTICIPÉ (caractérisation) : l'exclusion stricte hors fenêtre est déjà codée.
    [Fact]
    public void Should_N_afficher_le_slot_du_15_07_2025_dans_aucune_case_While_il_demeure_dans_le_store_When_la_grille_du_26_06_2026_est_projetee()
    {
        var slots = SlotPoseALaDateFigee_15_07_2025();
        var query = new GrilleAgendaQuery(
            slots,
            new FakePeriodeRepository(),
            new FakePaletteCouleurs(new Dictionary<string, string>()),
            new FakeReferentielResponsables(new Dictionary<string, string>()));

        var grille = query.Projeter(Date_26_06_2026);

        // présence dans le store : le slot est bien enregistré (non perdu)
        Assert.Single(slots.AllSnapshots());
        // absence dans la grille : aucune case ne le porte (date hors fenêtre)
        Assert.Equal(0, grille.Jours.Sum(j => j.Slots.Count));
    }
}
