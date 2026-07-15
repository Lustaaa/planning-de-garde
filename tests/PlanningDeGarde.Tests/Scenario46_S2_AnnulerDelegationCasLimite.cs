using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 46 — Sc.2 — Cas LIMITE : no-op idempotent + store intact + aucun jour tiers touché (@back)
//   - Jour J sans AUCUNE surcharge (fond seul) → AnnulerDelegation est un SUCCÈS no-op, store INTACT
//   - Ré-exécuter AnnulerDelegation(J) une seconde fois est de nouveau un no-op idempotent
//   - Reprendre le jour J ne touche PAS la surcharge d'un jour TIERS (aucune suppression collatérale, R11)
//
// Frontière Application (AnnulerDelegationHandler). Le no-op distingue AvaitDelegation=false du nominal (Sc.1).
public class Scenario46_S2_AnnulerDelegationCasLimite
{
    private const string ParentA = "parent-a";
    private const string ParentB = "parent-b";
    private const string ParentC = "parent-c";
    private const string LeaId = "enfant-lea";

    private static readonly DateOnly Mercredi_08_07_2026 = new(2026, 7, 8);   // ISO 28 paire → fond Parent A
    private static readonly DateOnly Vendredi_10_07_2026 = new(2026, 7, 10);  // jour TIERS

    private static bool Couvre(PeriodeSnapshot p, DateOnly j)
        => DateOnly.FromDateTime(p.Debut) <= j && DateOnly.FromDateTime(p.Fin) >= j;

    [Fact]
    public void Should_Etre_un_no_op_success_When_le_jour_ne_porte_aucune_surcharge()
    {
        var periodes = new FakePeriodeRepository();

        var resultat = new AnnulerDelegationHandler(periodes)
            .Handle(new AnnulerDelegationCommand(Mercredi_08_07_2026, LeaId));

        Assert.True(resultat.EstSucces);
        Assert.False(resultat.Valeur!.AvaitDelegation); // rien à reprendre
        Assert.Empty(periodes.AllSnapshots());          // store intact (aucune écriture, aucune suppression)
    }

    [Fact]
    public void Should_Rester_no_op_When_reexecute_une_seconde_fois()
    {
        var periodes = new FakePeriodeRepository();
        var handler = new AnnulerDelegationHandler(periodes);

        Assert.True(handler.Handle(new AnnulerDelegationCommand(Mercredi_08_07_2026, LeaId)).EstSucces);
        var deuxieme = handler.Handle(new AnnulerDelegationCommand(Mercredi_08_07_2026, LeaId));

        Assert.True(deuxieme.EstSucces);
        Assert.False(deuxieme.Valeur!.AvaitDelegation);
        Assert.Empty(periodes.AllSnapshots());
    }

    [Fact]
    public void Should_Ne_pas_toucher_la_surcharge_dun_jour_tiers()
    {
        var periodes = new FakePeriodeRepository();
        // Surcharge sur un jour TIERS (vendredi 10) — sans rapport avec le jour repris (mercredi 08).
        var debutTiers = Vendredi_10_07_2026.ToDateTime(TimeOnly.MinValue);
        periodes.Enregistrer(PeriodeDeGarde.Affecter(ParentC, debutTiers, debutTiers).Valeur!);

        var resultat = new AnnulerDelegationHandler(periodes)
            .Handle(new AnnulerDelegationCommand(Mercredi_08_07_2026, LeaId));

        Assert.True(resultat.EstSucces);
        Assert.False(resultat.Valeur!.AvaitDelegation);
        // La surcharge du jour tiers est INTACTE (aucune suppression collatérale).
        var restante = Assert.Single(periodes.AllSnapshots());
        Assert.Equal(ParentC, restante.ResponsableId);
        Assert.True(Couvre(restante, Vendredi_10_07_2026));
    }
}
