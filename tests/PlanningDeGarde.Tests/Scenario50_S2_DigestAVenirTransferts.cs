using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 50 — Sc.2 — « Transferts à venir » sur la fenêtre chargée (@back)
//   Étant donné une fenêtre de grille chargée couvrant les N prochains jours
//   Et plusieurs jours à venir portant chacun un transfert (saisi OU auto-dérivé s31)
//   Quand je compose la section « à venir » du digest pour l'enfant sélectionné
//   Alors elle restitue, en ordre chronologique CROISSANT, pour chaque jour concerné :
//     le jour, qui récupère (résolu surcharge > fond > neutre), le transfert et le lieu éventuel
//   Et cette section est un MIROIR STRICT de la logique s43 (AVenirQuery) itérée sur les jours à venir
//   Et elle ne compose que la fenêtre chargée — aucun GET dédié, aucune mutation, aucun store neuf
//
// Frontière Application (query PURE de composition). L'acceptation Mongo est portée par Api.Tests.
public class Scenario50_S2_DigestAVenirTransferts
{
    private const string ParentA = "parent-a";
    private const string ParentB = "parent-b";
    private const string Alice = "Alice";
    private const string Bruno = "Bruno";
    private const string LeaId = "enfant-lea";

    private static readonly DateOnly Mercredi_08_07_2026 = new(2026, 7, 8); // aujourd'hui
    private static readonly DateOnly Jeudi_09_07_2026 = new(2026, 7, 9);
    private static readonly DateOnly Vendredi_10_07_2026 = new(2026, 7, 10);
    private static readonly DateOnly Samedi_11_07_2026 = new(2026, 7, 11);  // sans transfert

    // Cycle N=1 : ParentA résolu chaque jour → aucune bascule dérivée parasite (les transferts sont ceux SAISIS).
    private static DigestImmediatQuery Query(ISlotRepository slots, ITransfertRepository transferts)
        => new(new GrilleAgendaQuery(
            slots, new FakePeriodeRepository(),
            new FakePaletteCouleurs(new Dictionary<string, string> { [ParentA] = "bleu", [ParentB] = "orange", ["ecole"] = "vert" }),
            new FakeReferentielResponsables(new Dictionary<string, string> { [ParentA] = Alice, [ParentB] = Bruno }),
            new FakeReferentielCycleDeFond(new CycleDeFond(1, new Dictionary<int, string> { [0] = ParentA })),
            new FakeEnumerationActeursFoyer(ParentA, ParentB),
            null,
            transferts));

    private static FakeTransfertRepository TransfertsLe10Puis09()
    {
        var transferts = new FakeTransfertRepository();
        // Ordre d'insertion volontairement DÉCROISSANT pour prouver le tri chrono CROISSANT en sortie.
        transferts.Enregistrer(Transfert.Definir(ParentA, ParentB, "ecole",
            new TimeSpan(8, 30, 0), Vendredi_10_07_2026.ToDateTime(TimeOnly.MinValue)).Valeur!);
        transferts.Enregistrer(Transfert.Definir(ParentA, ParentB, "ecole",
            new TimeSpan(8, 30, 0), Jeudi_09_07_2026.ToDateTime(TimeOnly.MinValue)).Valeur!);
        return transferts;
    }

    // ---------- Acceptation (boucle externe, frontière Application) ----------
    [Fact]
    public void Acceptation_Should_Lister_les_jours_a_venir_portant_un_transfert_en_ordre_chrono_croissant()
    {
        // Given — deux transferts à venir (09/07 et 10/07), un slot 'école' de Léa le 09/07
        var slots = new FakeSlotRepository();
        slots.Enregistrer(SlotDeLocalisation
            .Poser(LeaId, "ecole", new DateTime(2026, 7, 9, 8, 30, 0), new DateTime(2026, 7, 9, 16, 30, 0)).Valeur!);
        var query = Query(slots, TransfertsLe10Puis09());

        // When — je compose le digest pour Léa, jour courant = 08/07
        var avenir = query.Composer(Mercredi_08_07_2026, Mercredi_08_07_2026, LeaId).AVenir;

        // Then — exactement les deux jours PORTANT un transfert, en ordre chrono CROISSANT
        Assert.Equal(2, avenir.Count);
        Assert.Equal(Jeudi_09_07_2026, avenir[0].Date);
        Assert.Equal(Vendredi_10_07_2026, avenir[1].Date);

        // And — pour chaque jour : qui récupère (résolu ParentA), le transfert, et le lieu (slot) éventuel
        Assert.True(avenir[0].Responsable.EstAssigne);
        Assert.Equal(ParentA, avenir[0].Responsable.ActeurId);
        Assert.NotNull(avenir[0].Transfert);
        Assert.Equal(Bruno, avenir[0].Transfert!.RecevantNom);
        Assert.Equal("ecole", Assert.Single(avenir[0].Slots).Libelle);

        // And — le jour à venir SANS transfert (11/07) n'apparaît pas dans la liste
        Assert.DoesNotContain(avenir, j => j.Date == Samedi_11_07_2026);
    }

    // ---------- Boucle interne (TDD) ----------

    // Test #1 — le jour COURANT (08/07) n'est jamais dans la section « à venir » (strictement après aujourd'hui).
    [Fact]
    public void Should_Exclure_le_jour_courant_de_la_section_a_venir()
    {
        var transferts = new FakeTransfertRepository();
        transferts.Enregistrer(Transfert.Definir(ParentA, ParentB, "ecole",
            new TimeSpan(8, 30, 0), Mercredi_08_07_2026.ToDateTime(TimeOnly.MinValue)).Valeur!);

        var avenir = Query(new FakeSlotRepository(), transferts)
            .Composer(Mercredi_08_07_2026, Mercredi_08_07_2026, LeaId).AVenir;

        Assert.DoesNotContain(avenir, j => j.Date == Mercredi_08_07_2026);
    }

    // Test #2 — un transfert AUTO-dérivé s31 (succession de périodes) alimente aussi la section « à venir ».
    [Fact]
    public void Should_Inclure_un_jour_a_venir_dont_le_transfert_est_auto_derive_s31()
    {
        // Périodes en succession : ParentA finit le 09/07, ParentB débute le 10/07 → transfert dérivé le 10/07.
        var periodes = new FakePeriodeRepository();
        periodes.Enregistrer(PeriodeDeGarde.Affecter(ParentA,
            new DateTime(2026, 7, 6), new DateTime(2026, 7, 9), LeaId).Valeur!);
        periodes.Enregistrer(PeriodeDeGarde.Affecter(ParentB,
            new DateTime(2026, 7, 10), new DateTime(2026, 7, 13), LeaId).Valeur!);

        var query = new DigestImmediatQuery(new GrilleAgendaQuery(
            new FakeSlotRepository(), periodes,
            new FakePaletteCouleurs(new Dictionary<string, string> { [ParentA] = "bleu", [ParentB] = "orange" }),
            new FakeReferentielResponsables(new Dictionary<string, string> { [ParentA] = Alice, [ParentB] = Bruno }),
            new FakeReferentielCycleDeFond(new CycleDeFond(1, new Dictionary<int, string> { [0] = ParentA })),
            new FakeEnumerationActeursFoyer(ParentA, ParentB),
            null,
            new FakeTransfertRepository()));

        var avenir = query.Composer(Mercredi_08_07_2026, Mercredi_08_07_2026, LeaId).AVenir;

        var jour = Assert.Single(avenir, j => j.Date == Vendredi_10_07_2026);
        Assert.NotNull(jour.Transfert);
    }
}
