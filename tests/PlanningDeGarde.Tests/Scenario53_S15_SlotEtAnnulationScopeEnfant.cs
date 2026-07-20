using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 53 — Sc.15 — Slot « où » posé pour A + reprise (annuler délégation) sur A : isolés de B (@back)
//   - Un SLOT de localisation posé pour "Léa" n'apparaît QUE dans la grille de "Léa", jamais chez "Tom".
//   - ANNULER la délégation d'un jour pour "Léa" ne retire JAMAIS la surcharge de "Tom" (pas de suppression
//     inter-enfants) ; les segments restants d'une plage reprise conservent l'EnfantId.
//
// Audit exhaustif gate G3 (3e passage) : chemins d'écriture slot (PoserSlot) et reprise (AnnulerDelegation).
public class Scenario53_S15_SlotEtAnnulationScopeEnfant
{
    private const string LeaId = "enfant-lea";
    private const string TomId = "enfant-tom";
    private const string PA = "p-a", PB = "p-b";
    private static readonly DateOnly J = new(2026, 7, 8);

    private static GrilleAgendaQuery Grille(ISlotRepository slots, IPeriodeRepository periodes)
        => new(
            slots, periodes,
            new FakePaletteCouleurs(new Dictionary<string, string> { [PA] = "bleu", [PB] = "orange", ["ecole"] = "vert" }),
            new FakeReferentielResponsables(new Dictionary<string, string> { [PA] = "Alice", [PB] = "Bob" }),
            new FakeReferentielCycleDeFond(),
            new FakeEnumerationActeursFoyer(PA, PB));

    private static JourCase Case(GrilleAgendaQuery grille, string enfantId)
        => grille.Projeter(J, VuePlanning.Semaine, enfantId).Jours.Single(j => j.Date == J);

    [Fact]
    public void Acceptation_Slot_pose_pour_Lea_visible_de_Lea_seul()
    {
        var slots = new FakeSlotRepository();
        slots.Enregistrer(SlotDeLocalisation.Poser(
            LeaId, "ecole", J.ToDateTime(new TimeOnly(8, 30)), J.ToDateTime(new TimeOnly(16, 30))).Valeur!);
        var grille = Grille(slots, new FakePeriodeRepository());

        Assert.NotEmpty(Case(grille, LeaId).Slots);   // le slot « où » de Léa apparaît chez Léa
        Assert.Empty(Case(grille, TomId).Slots);       // ... jamais chez Tom (plus de fuite)
    }

    [Fact]
    public void Acceptation_Annuler_delegation_de_Lea_laisse_la_surcharge_de_Tom_intacte()
    {
        var periodes = new FakePeriodeRepository();
        periodes.Enregistrer(PeriodeDeGarde.Affecter(PA, J.ToDateTime(TimeOnly.MinValue), J.ToDateTime(TimeOnly.MinValue), LeaId).Valeur!);
        periodes.Enregistrer(PeriodeDeGarde.Affecter(PB, J.ToDateTime(TimeOnly.MinValue), J.ToDateTime(TimeOnly.MinValue), TomId).Valeur!);

        // When — reprendre (annuler la délégation) le jour J pour Léa SEULEMENT.
        var resultat = new AnnulerDelegationHandler(periodes).Handle(new AnnulerDelegationCommand(J, LeaId));
        Assert.True(resultat.EstSucces);

        // Then — la surcharge de Léa est retirée ; celle de Tom SURVIT intacte (pas de suppression inter-enfants).
        var restantes = periodes.AllSnapshots();
        Assert.DoesNotContain(restantes, p => p.EnfantId == LeaId);
        Assert.Equal(PB, Assert.Single(restantes, p => p.EnfantId == TomId).ResponsableId);
        Assert.Equal(PB, Case(Grille(new FakeSlotRepository(), periodes), TomId).ResponsableId);
    }
}
