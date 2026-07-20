using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 50 — Sc.3 — Replis fidèles, sans fantôme (@back)
//   Étant donné une fenêtre chargée mêlant des jours aux configurations variées
//   Quand je compose le digest « immédiat » et « à venir »
//   Alors un jour SANS responsable résolu affiche « personne assignée » (aucun nom fantôme)
//   Et un responsable ORPHELIN (id absent du référentiel) retombe en NEUTRE sans nom (R6 / Resolvable s13)
//   Et un jour SANS transfert n'apparaît PAS dans la liste des transferts (ni ligne vide)
//   Et un jour SANS slot est restitué SANS lieu (aucun lieu fantôme)
//   Et aucun de ces replis ne déclenche d'écriture ni de mutation d'état
//
// Filet de CARACTÉRISATION : les replis sont DÉLÉGUÉS à GrilleAgendaQuery (Resolvable / retombée neutre déjà
// livrée et testée) et surfacés tels quels par la composition — le digest n'en réimplémente aucun.
public class Scenario50_S3_DigestReplisFideles
{
    private const string ParentA = "parent-a";
    private const string ParentB = "parent-b";
    private const string Fantome = "parent-supprime"; // absent du référentiel d'acteurs (orphelin)
    private const string LeaId = "enfant-lea";

    private static readonly DateOnly Lundi_06_07_2026 = new(2026, 7, 6);    // neutre : ni période ni cycle
    private static readonly DateOnly Mardi_07_07_2026 = new(2026, 7, 7);    // période orpheline
    private static readonly DateOnly Jeudi_09_07_2026 = new(2026, 7, 9);    // à venir, transfert saisi
    private static readonly DateOnly Vendredi_10_07_2026 = new(2026, 7, 10); // à venir, SANS transfert

    // AUCUN cycle de fond : hors surcharge, chaque jour est NEUTRE (personne assignée).
    private static DigestImmediatQuery Query(IPeriodeRepository periodes, ITransfertRepository transferts)
        => new(new GrilleAgendaQuery(
            new FakeSlotRepository(), periodes,
            new FakePaletteCouleurs(new Dictionary<string, string> { [ParentA] = "bleu", [ParentB] = "orange" }),
            new FakeReferentielResponsables(new Dictionary<string, string> { [ParentA] = "Alice", [ParentB] = "Bruno" }),
            null,
            new FakeEnumerationActeursFoyer(ParentA, ParentB), // Fantome NON énuméré → orphelin
            null,
            transferts));

    // ---------- Acceptation (frontière Application) ----------
    [Fact]
    public void Acceptation_Should_Rendre_chaque_repli_fidelement_sans_fantome()
    {
        // Given — période orpheline le 07/07, un transfert saisi le 09/07 (aucun le 10/07)
        var periodes = new FakePeriodeRepository();
        periodes.Enregistrer(PeriodeDeGarde.Affecter(Fantome,
            new DateTime(2026, 7, 7), new DateTime(2026, 7, 7)).Valeur!);
        var transferts = new FakeTransfertRepository();
        transferts.Enregistrer(Transfert.Definir(ParentA, ParentB, "ecole",
            new TimeSpan(8, 30, 0), Jeudi_09_07_2026.ToDateTime(TimeOnly.MinValue), LeaId).Valeur!);
        var query = Query(periodes, transferts);

        // Then — jour NEUTRE (06/07) : personne assignée, aucun nom fantôme, aucun lieu
        var neutre = query.Composer(Lundi_06_07_2026, Lundi_06_07_2026, LeaId).Immediat!;
        Assert.False(neutre.Responsable.EstAssigne);
        Assert.Equal("", neutre.Responsable.Nom);
        Assert.Null(neutre.Responsable.ActeurId);
        Assert.Empty(neutre.Slots); // sans slot → sans lieu

        // Then — responsable ORPHELIN (07/07) : retombe en neutre sans nom (R6 / Resolvable)
        var orphelin = query.Composer(Mardi_07_07_2026, Mardi_07_07_2026, LeaId).Immediat!;
        Assert.False(orphelin.Responsable.EstAssigne);
        Assert.Equal("", orphelin.Responsable.Nom);
        Assert.Null(orphelin.Responsable.ActeurId);

        // Then — section « à venir » : seul le jour PORTANT un transfert (09/07) figure, pas le 10/07
        var avenir = query.Composer(Lundi_06_07_2026, Lundi_06_07_2026, LeaId).AVenir;
        Assert.Contains(avenir, j => j.Date == Jeudi_09_07_2026);
        Assert.DoesNotContain(avenir, j => j.Date == Vendredi_10_07_2026);
        // et le jour à venir retenu, sans slot, est restitué SANS lieu (aucun lieu fantôme)
        Assert.Empty(avenir.Single(j => j.Date == Jeudi_09_07_2026).Slots);
    }

    // ---------- Boucle interne : invariant zéro-mutation (les replis ne déclenchent aucune écriture) ----------
    [Fact]
    public void Should_Ne_declencher_aucune_ecriture_de_surcharge_lors_de_la_composition_des_replis()
    {
        var periodes = new FakePeriodeRepository();
        periodes.Enregistrer(PeriodeDeGarde.Affecter(Fantome,
            new DateTime(2026, 7, 7), new DateTime(2026, 7, 7)).Valeur!);
        var avant = periodes.AllSnapshots().Count;

        Query(periodes, new FakeTransfertRepository()).Composer(Lundi_06_07_2026, Lundi_06_07_2026, LeaId);

        Assert.Equal(avant, periodes.AllSnapshots().Count); // store des surcharges STRICTEMENT intact
    }
}
