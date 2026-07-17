using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Tests.Builders;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 49 — Sc.2 — @back FILET (⚠️ early-green ATTENDU, pas driver). Garantit que la sélection
// d'UN seul jour (drag sans déplacement → Sc.4) retombe sur le comportement PONCTUEL connu :
// affecter une période sur l'intervalle [J..J] écrit EXACTEMENT le jour J (surcharge résolue ce
// jour-là), J-1 et J+1 restant sur le fond, sans écriture doublonnée (un seul enregistrement dans
// le store — last-write-wins R11). Aucune mécanique d'écriture neuve : chemin s06 réemployé.
//
// Données : cycle N=2, mapping {0→parent-b, 1→parent-a}. ISO 28 (06–12/07/2026) PAIRE → index 0 →
// fond Parent B (Bruno/orange). Surcharge Parent A (Alice/bleu) sur le SEUL 08/07 ([J..J]).
public class Scenario49_S2_AffecterPeriodeUnSeulJour
{
    private const string ParentA = "parent-a"; // Alice — surcharge affectée
    private const string ParentB = "parent-b"; // Bruno — fond du cycle
    private const string Alice = "Alice";
    private const string Bruno = "Bruno";
    private const string Bleu = "bleu";
    private const string Orange = "orange";

    private static readonly DateOnly Lundi_29_06_2026 = new(2026, 6, 29); // ancre fenêtre
    private static readonly DateTime J = new(2026, 7, 8);   // Mercredi — ISO 28 paire, jour unique surchargé
    private static readonly DateOnly Jd = new(2026, 7, 8);
    private static readonly DateOnly Veille_07_07 = new(2026, 7, 7);   // J-1 — sur le fond
    private static readonly DateOnly Lendemain_09_07 = new(2026, 7, 9); // J+1 — sur le fond

    [Fact]
    public void Should_ecrire_exactement_le_jour_J_sans_doublon_et_laisser_les_voisins_sur_le_fond_When_une_periode_est_ecrite_sur_l_intervalle_J_a_J()
    {
        // Given — foyer avec cycle de fond (fond Parent B) et l'acteur Alice.
        var periodes = new FakePeriodeRepository();
        var handler = new AffecterPeriodeHandler(
            periodes,
            new FakeResponsableRepository().AvecResponsable(ParentA).AvecResponsable(ParentB));

        // When — une période affectant Alice est écrite sur l'intervalle d'un seul jour [J..J].
        var resultat = handler.Handle(new PeriodeBuilder().PourResponsable(ParentA).Du(J).Au(J).Build());
        Assert.True(resultat.EstSucces);

        // Then — aucune écriture doublonnée : un SEUL enregistrement dans le store (R11).
        var enregistree = Assert.Single(periodes.AllSnapshots());
        Assert.Equal(ParentA, enregistree.ResponsableId);
        Assert.Equal(J, enregistree.Debut);
        Assert.Equal(J, enregistree.Fin);

        // And — seul J porte la surcharge (Alice/bleu) ; J-1 et J+1 restent sur le fond (Bruno/orange).
        var grille = QueryAvec(periodes).Projeter(Lundi_29_06_2026);
        var caseJ = grille.Jours.Single(j => j.Date == Jd);
        Assert.Equal(Alice, caseJ.NomResponsable);
        Assert.Equal(Bleu, caseJ.CouleurResponsable);
        foreach (var voisin in new[] { Veille_07_07, Lendemain_09_07 })
        {
            var caseFond = grille.Jours.Single(j => j.Date == voisin);
            Assert.Equal(Bruno, caseFond.NomResponsable);
            Assert.Equal(Orange, caseFond.CouleurResponsable);
        }
    }

    private static GrilleAgendaQuery QueryAvec(IPeriodeRepository periodes)
        => new(new FakeSlotRepository(), periodes,
            new FakePaletteCouleurs(new Dictionary<string, string> { [ParentA] = Bleu, [ParentB] = Orange }),
            new FakeReferentielResponsables(new Dictionary<string, string> { [ParentA] = Alice, [ParentB] = Bruno }),
            new FakeReferentielCycleDeFond(new CycleDeFond(2, new Dictionary<int, string> { [0] = ParentB, [1] = ParentA })));
}
