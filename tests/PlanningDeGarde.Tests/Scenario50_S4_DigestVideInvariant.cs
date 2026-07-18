using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 50 — Sc.4 — Fenêtre sans à-venir / jour courant hors-fenêtre = digest vide neutre, store intact (@back)
//   Étant donné une fenêtre chargée ne contenant NI jour courant NI aucun transfert à venir
//   Quand je compose le digest « immédiat » et « à venir »
//   Alors la section « immédiat » est vide neutre (message neutre, pas de crash)
//   Et la section « à venir » est une liste vide (message neutre)
//   Et AUCUNE surcharge n'est écrite — le store des surcharges reste STRICTEMENT intact
//   Et la résolution d'aucune case n'est altérée (la query est de LECTURE pure)
//   Et le comportement est prouvé identique sur les deux adaptateurs (InMemory ET Mongo durable)
//
// Frontière Application. Le « jour courant hors-fenêtre » = navigation client vers une semaine ne
// contenant pas aujourd'hui (ancre ≠ semaine du jour courant) → section « immédiat » null (vide neutre côté IHM).
public class Scenario50_S4_DigestVideInvariant
{
    private const string ParentA = "parent-a";
    private const string LeaId = "enfant-lea";

    private static readonly DateOnly Aujourdhui_08_07_2026 = new(2026, 7, 8);
    private static readonly DateOnly AncreSeptembre = new(2026, 9, 1); // fenêtre ne contenant PAS le 08/07

    // AUCUN cycle de fond → aucune bascule dérivée ; AUCUN transfert saisi → fenêtre sans à-venir.
    private static DigestImmediatQuery Query(IPeriodeRepository periodes)
        => new(new GrilleAgendaQuery(
            new FakeSlotRepository(), periodes,
            new FakePaletteCouleurs(new Dictionary<string, string> { [ParentA] = "bleu" }),
            new FakeReferentielResponsables(new Dictionary<string, string> { [ParentA] = "Alice" }),
            null,
            new FakeEnumerationActeursFoyer(ParentA),
            null,
            new FakeTransfertRepository()));

    // ---------- Acceptation (frontière Application) ----------
    [Fact]
    public void Acceptation_Should_Rendre_un_digest_vide_neutre_sans_crash_quand_jour_courant_hors_fenetre_et_sans_a_venir()
    {
        var periodes = new FakePeriodeRepository();

        // When — je compose une fenêtre (septembre) ne contenant NI le 08/07 NI aucun transfert
        var digest = Query(periodes).Composer(AncreSeptembre, Aujourdhui_08_07_2026, LeaId);

        // Then — section « immédiat » vide neutre (null = message neutre côté IHM), section « à venir » vide
        Assert.Null(digest.Immediat);
        Assert.Empty(digest.AVenir);
    }

    // ---------- Boucle interne : invariant zéro-mutation (store des surcharges STRICTEMENT intact) ----------
    [Fact]
    public void Should_Laisser_le_store_des_surcharges_strictement_intact_et_la_resolution_inchangee()
    {
        var periodes = new FakePeriodeRepository();
        periodes.Enregistrer(PeriodeDeGarde.Affecter(ParentA,
            new DateTime(2026, 7, 20), new DateTime(2026, 7, 22)).Valeur!);
        var query = Query(periodes);

        var avant = query.Composer(AncreSeptembre, Aujourdhui_08_07_2026, LeaId);
        var snapshotAvant = periodes.AllSnapshots();

        var apres = query.Composer(AncreSeptembre, Aujourdhui_08_07_2026, LeaId);
        var snapshotApres = periodes.AllSnapshots();

        // le store des surcharges est intact (même cardinalité) et la composition est reproductible (lecture pure)
        Assert.Equal(snapshotAvant.Count, snapshotApres.Count);
        Assert.Null(avant.Immediat);
        Assert.Null(apres.Immediat);
        Assert.Empty(apres.AVenir);
    }

    // ---------- Acceptation runtime — adaptateur InMemory RÉEL (1er des deux adaptateurs) ----------
    [Fact]
    public void Acceptation_InMemory_Should_Rendre_un_digest_vide_neutre_et_laisser_le_store_intact()
    {
        var config = new ConfigurationFoyerEnMemoire();
        var aliceId = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Alice")).Valeur!.ActeurId;
        var periodes = new InMemoryPeriodeRepository();
        periodes.Enregistrer(PeriodeDeGarde.Affecter(aliceId,
            new DateTime(2026, 7, 20), new DateTime(2026, 7, 22)).Valeur!);
        var avant = periodes.AllSnapshots().Count;

        var grille = new GrilleAgendaQuery(
            new InMemorySlotRepository(), periodes, config, config,
            new CycleDeFondEnMemoire(), config, new InMemorySlotRecurrentRepository(), new InMemoryTransfertRepository());

        var digest = new DigestImmediatQuery(grille).Composer(AncreSeptembre, Aujourdhui_08_07_2026, LeaId);

        Assert.Null(digest.Immediat);
        Assert.Empty(digest.AVenir);
        Assert.Equal(avant, periodes.AllSnapshots().Count); // store des surcharges STRICTEMENT intact
    }
}
