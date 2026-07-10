using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 31 — Sc.13 (D1, 2e cœur) — Non-régression : slot NON conditionné (défaut s29 inchangé) (@back)
//   Étant donné un slot récurrent NON conditionné (toggle inactif, comportement par défaut)
//   Quand la grille projette les occurrences
//   Alors le slot est projeté sur TOUS ses jours de récurrence (comportement s29 strictement inchangé)
//   Et la résolution de responsabilité n'intervient PAS dans sa projection
//
// Frontière Application (GrilleAgendaQuery). DISCRIMINANT anti-fuite du 2e cœur : la fenêtre porte des
// jours à responsabilité RÉSOLUE HÉTÉROGÈNE (un lundi "autre" en surcharge, les autres neutres) — si la
// résolution intervenait (comme pour un slot conditionné), au moins un lundi serait masqué. Le slot par
// défaut apparaît sur TOUS ses lundis : la projection ignore totalement la responsabilité (invariant s29).
public class Scenario31_S13_SlotNonConditionneDefautInchange
{
    private static readonly DateOnly Reference_24_06_2026 = new(2026, 6, 24);
    private static readonly DateOnly[] TousLesLundisDeLaFenetre =
    {
        new(2026, 6, 22), new(2026, 6, 29), new(2026, 7, 6), new(2026, 7, 13),
    };
    private static readonly DateOnly LundiAutreResponsable_29_06 = new(2026, 6, 29);

    // Slot récurrent au comportement PAR DÉFAUT (conditionneGarde omis → false, aucun poseur).
    private static FakeSlotRecurrentRepository SlotNonConditionneLeLundi()
    {
        var repo = new FakeSlotRecurrentRepository();
        repo.Enregistrer(SlotRecurrent.Poser(
            "lea", "ecole", DayOfWeek.Monday, new TimeSpan(8, 0, 0), new TimeSpan(9, 0, 0)).Valeur!);
        return repo;
    }

    // Un seul lundi porte un responsable RÉSOLU ("autre" en surcharge) ; les autres lundis sont neutres.
    // La responsabilité de la fenêtre est donc hétérogène — de quoi démasquer toute fuite du filtre D1.
    private static FakePeriodeRepository AutreResponsableLe29()
    {
        var repo = new FakePeriodeRepository();
        repo.Enregistrer(PeriodeDeGarde.Affecter(
            "autre", LundiAutreResponsable_29_06.ToDateTime(TimeOnly.MinValue),
            LundiAutreResponsable_29_06.ToDateTime(new TimeOnly(23, 59))).Valeur!);
        return repo;
    }

    private static GrilleAgendaQuery Query()
        => new(
            new FakeSlotRepository(),
            AutreResponsableLe29(),
            new FakePaletteCouleurs(new Dictionary<string, string> { ["autre"] = "rose" }),
            new FakeReferentielResponsables(new Dictionary<string, string> { ["autre"] = "Autre" }),
            slotsRecurrents: SlotNonConditionneLeLundi());

    // ---------- Test d'acceptation (boucle externe, BDD) ----------

    [Fact]
    public void Should_projeter_l_occurrence_sur_tous_les_jours_de_recurrence_When_le_slot_n_est_pas_conditionne()
    {
        var grille = Query().Projeter(Reference_24_06_2026);

        // Then — l'occurrence "ecole" apparaît sur CHAQUE lundi de la fenêtre, quel que soit le responsable
        // résolu ce jour-là (aucun masquage) : comportement s29 strictement inchangé.
        foreach (var lundi in TousLesLundisDeLaFenetre)
        {
            var caseLundi = grille.Jours.Single(j => j.Date == lundi);
            Assert.Contains(caseLundi.Slots, s => s.Libelle == "ecole");
        }

        // … y compris le lundi où "Autre" est résolu responsable : la résolution n'intervient PAS
        // dans la projection d'un slot non conditionné (elle masquerait, s'il était conditionné).
        var caseAutre = grille.Jours.Single(j => j.Date == LundiAutreResponsable_29_06);
        Assert.Equal("Autre", caseAutre.NomResponsable);
        Assert.Contains(caseAutre.Slots, s => s.Libelle == "ecole");

        // … soit exactement QUATRE occurrences (une par lundi de la fenêtre), aucune de plus, aucune de moins.
        var occurrences = grille.Jours.SelectMany(j => j.Slots).Where(s => s.Libelle == "ecole").ToList();
        Assert.Equal(TousLesLundisDeLaFenetre.Length, occurrences.Count);
    }
}
