using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 50 — Sc.1 — Digest « qui récupère aujourd'hui / ce soir » composé depuis GrilleAgendaQuery (@back)
//   Étant donné un foyer configuré (cycle de fond + acteurs) et un enfant sélectionné
//   Et le jour courant portant un responsable résolu par la priorité surcharge > fond > neutre
//   Et, ce jour-là, un slot de localisation (s29) et un transfert saisi OU auto-dérivé (s31)
//   Quand je compose le digest « immédiat » pour cet enfant et ce jour
//   Alors le digest expose QUI récupère (id stable), OÙ (slot s29) et le transfert éventuel
//   Et la composition RÉEMPLOIE GrilleAgendaQuery (aucune résolution/dérivation réimplémentée)
//   Et AUCUN store neuf, AUCUNE mutation ; comportement identique InMemory ET Mongo durable.
//
// Frontière Application (query PURE de composition). L'acceptation runtime sur les DEUX adaptateurs est
// portée par l'acceptation InMemory RÉELLE ci-dessous + le test Mongo (Api.Tests).
public class Scenario50_S1_DigestImmediatResponsable
{
    private const string ParentA = "parent-a";
    private const string ParentB = "parent-b";
    private const string Alice = "Alice";
    private const string Bruno = "Bruno";
    private const string Bleu = "bleu";
    private const string Orange = "orange";
    private const string LeaId = "enfant-lea";

    private static readonly DateOnly Mardi_07_07_2026 = new(2026, 7, 7);    // ISO 28 paire → fond Parent A
    private static readonly DateOnly Mercredi_08_07_2026 = new(2026, 7, 8); // surcharge explicite Parent B

    private static Dictionary<int, string> MappingPairAImpairB() => new() { [0] = ParentA, [1] = ParentB };

    private static DigestImmediatQuery Query(
        IPeriodeRepository periodes, ISlotRepository? slots = null, ITransfertRepository? transferts = null)
        => new(new GrilleAgendaQuery(
            slots ?? new FakeSlotRepository(), periodes,
            new FakePaletteCouleurs(new Dictionary<string, string> { [ParentA] = Bleu, [ParentB] = Orange, ["ecole"] = "vert" }),
            new FakeReferentielResponsables(new Dictionary<string, string> { [ParentA] = Alice, [ParentB] = Bruno }),
            new FakeReferentielCycleDeFond(new CycleDeFond(2, MappingPairAImpairB())),
            new FakeEnumerationActeursFoyer(ParentA, ParentB),
            null,
            transferts));

    private static FakePeriodeRepository SurchargeParentBLe08()
    {
        var periodes = new FakePeriodeRepository();
        periodes.Enregistrer(PeriodeDeGarde.Affecter(ParentB,
            new DateTime(2026, 7, 8), new DateTime(2026, 7, 8)).Valeur!);
        return periodes;
    }

    // ---------- Acceptation (boucle externe, frontière Application) ----------
    [Fact]
    public void Acceptation_Should_Composer_le_digest_immediat_qui_ou_transfert_par_reemploi_de_la_grille()
    {
        // Given — surcharge Parent B le 08/07, un slot 'école' de Léa ce jour-là, un transfert saisi ce jour
        var slots = new FakeSlotRepository();
        slots.Enregistrer(SlotDeLocalisation
            .Poser(LeaId, "ecole", new DateTime(2026, 7, 8, 8, 30, 0), new DateTime(2026, 7, 8, 16, 30, 0)).Valeur!);
        var transferts = new FakeTransfertRepository();
        transferts.Enregistrer(Transfert.Definir(ParentA, ParentB, "ecole",
            new TimeSpan(8, 30, 0), Mercredi_08_07_2026.ToDateTime(TimeOnly.MinValue)).Valeur!);

        var query = Query(SurchargeParentBLe08(), slots, transferts);

        // When — je compose le digest « immédiat » pour Léa, jour courant = 08/07
        var digest = query.Composer(Mercredi_08_07_2026, Mercredi_08_07_2026, LeaId);

        // Then — QUI récupère : Parent B résolu (id stable + nom + couleur), assigné
        Assert.NotNull(digest.Immediat);
        var immediat = digest.Immediat!;
        Assert.True(immediat.Responsable.EstAssigne);
        Assert.Equal(ParentB, immediat.Responsable.ActeurId);
        Assert.Equal(Bruno, immediat.Responsable.Nom);
        Assert.Equal(Orange, immediat.Responsable.Couleur);

        // And — OÙ : le slot 'école' de Léa
        var slot = Assert.Single(immediat.Slots);
        Assert.Equal("ecole", slot.Libelle);

        // And — le transfert éventuel (cédant → recevant) résolu
        Assert.NotNull(immediat.Transfert);
        Assert.Equal(Alice, immediat.Transfert!.CedantNom);
        Assert.Equal(Bruno, immediat.Transfert.RecevantNom);
    }

    // ---------- Boucle interne (TDD) ----------

    // Test #1 — le digest compose la MÊME résolution que la grille (source unique) : nom/couleur identiques.
    [Fact]
    public void Should_Composer_la_meme_resolution_que_la_grille_pour_le_jour_courant()
    {
        var grille = new GrilleAgendaQuery(
            new FakeSlotRepository(), SurchargeParentBLe08(),
            new FakePaletteCouleurs(new Dictionary<string, string> { [ParentA] = Bleu, [ParentB] = Orange }),
            new FakeReferentielResponsables(new Dictionary<string, string> { [ParentA] = Alice, [ParentB] = Bruno }),
            new FakeReferentielCycleDeFond(new CycleDeFond(2, MappingPairAImpairB())),
            new FakeEnumerationActeursFoyer(ParentA, ParentB));
        var caseGrille = grille.Projeter(Mercredi_08_07_2026, VuePlanning.QuatreSemaines)
            .Jours.Single(j => j.Date == Mercredi_08_07_2026);

        var immediat = new DigestImmediatQuery(grille).Composer(Mercredi_08_07_2026, Mercredi_08_07_2026, LeaId).Immediat!;

        Assert.Equal(caseGrille.NomResponsable, immediat.Responsable.Nom);
        Assert.Equal(caseGrille.CouleurResponsable, immediat.Responsable.Couleur);
    }

    // Test #2 — jour de FOND (aucune période) : responsable résolu par le cycle (composition, pas de réimpl.).
    [Fact]
    public void Should_Composer_le_responsable_de_fond_du_cycle_pour_un_jour_sans_surcharge()
    {
        var immediat = Query(new FakePeriodeRepository())
            .Composer(Mardi_07_07_2026, Mardi_07_07_2026, LeaId).Immediat!;

        Assert.True(immediat.Responsable.EstAssigne);
        Assert.Equal(ParentA, immediat.Responsable.ActeurId);
        Assert.Equal(Alice, immediat.Responsable.Nom);
    }

    // ---------- Acceptation runtime — adaptateur InMemory RÉEL (1er des deux adaptateurs, Sc.1) ----------
    [Fact]
    public void Acceptation_InMemory_Should_Composer_le_digest_sur_les_adaptateurs_reels()
    {
        // Given — config foyer InMemory réelle (noms + couleurs + existence) + période InMemory réelle
        var config = new ConfigurationFoyerEnMemoire();
        var aliceId = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Alice")).Valeur!.ActeurId;
        var periodes = new InMemoryPeriodeRepository();
        periodes.Enregistrer(PeriodeDeGarde.Affecter(aliceId,
            new DateTime(2026, 7, 8), new DateTime(2026, 7, 8)).Valeur!);

        var grille = new GrilleAgendaQuery(
            new InMemorySlotRepository(), periodes, config, config,
            new CycleDeFondEnMemoire(), config, new InMemorySlotRecurrentRepository(), new InMemoryTransfertRepository());

        // When — le digest câblé sur les adaptateurs RÉELS
        var immediat = new DigestImmediatQuery(grille).Composer(Mercredi_08_07_2026, Mercredi_08_07_2026, LeaId).Immediat!;

        // Then — Alice résolue depuis le store réel (id stable + nom), assignée
        Assert.True(immediat.Responsable.EstAssigne);
        Assert.Equal(aliceId, immediat.Responsable.ActeurId);
        Assert.Equal("Alice", immediat.Responsable.Nom);
    }
}
