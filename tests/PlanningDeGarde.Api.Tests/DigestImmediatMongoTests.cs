using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Infrastructure;

namespace PlanningDeGarde.Api.Tests;

/// <summary>
/// Sprint 50 — Acceptation d'<b>intégration sur Mongo RÉEL</b> (conteneur Docker, jamais une doublure : le
/// 2ᵉ des deux adaptateurs, InMemory étant prouvé côté PlanningDeGarde.Tests). Le digest cloche
/// <see cref="DigestImmediatQuery"/> est câblé sur les stores durables réels (config foyer + périodes +
/// cycle + slots + transferts Mongo) : il COMPOSE la résolution existante (surcharge &gt; fond &gt; neutre,
/// slots s29, transferts saisis/dérivés s31) sur un profil de données réaliste. <b>Lecture PURE</b>.
///
/// <b>Skip propre</b> (<see cref="MongoRequisFactAttribute"/>) si Docker / Mongo est indisponible.
/// </summary>
public sealed class DigestImmediatMongoTests : IDisposable
{
    private const string ConnectionString = "mongodb://localhost:27017";
    private readonly string _baseDeTest = $"planning_test_{Guid.NewGuid():N}";

    private static readonly DateOnly Mardi_07_07_2026 = new(2026, 7, 7);    // ISO 28 paire → fond Parent A
    private static readonly DateOnly Mercredi_08_07_2026 = new(2026, 7, 8); // surcharge explicite Parent B

    private DigestImmediatQuery DigestSurBaseReelle()
    {
        var config = new ConfigurationFoyerMongo(ConnectionString, _baseDeTest);
        var grille = new GrilleAgendaQuery(
            new MongoSlotRepository(ConnectionString, _baseDeTest),
            new MongoPeriodeRepository(ConnectionString, _baseDeTest),
            config, config,
            new CycleDeFondMongo(ConnectionString, _baseDeTest),
            config,
            new MongoSlotRecurrentRepository(ConnectionString, _baseDeTest),
            new MongoTransfertRepository(ConnectionString, _baseDeTest));
        return new DigestImmediatQuery(grille);
    }

    // --- Sc.1 : le digest « immédiat » restitue le responsable résolu (surcharge > fond) + où + transfert ---
    [MongoRequisFact]
    public void Acceptation_Should_Composer_le_digest_immediat_resolu_sur_Mongo_reel()
    {
        // --- Given : deux acteurs durables, un cycle N=2, une surcharge le 08/07, slots Léa/Tom, transfert ---
        var config = new ConfigurationFoyerMongo(ConnectionString, _baseDeTest);
        var parentA = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Alice")).Valeur!.ActeurId;
        var parentB = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Bruno")).Valeur!.ActeurId;

        var cycle = new CycleDeFondMongo(ConnectionString, _baseDeTest);
        cycle.DefinirCycle(new CycleDeFond(2, new Dictionary<int, string> { [0] = parentA, [1] = parentB }));

        var periodes = new MongoPeriodeRepository(ConnectionString, _baseDeTest);
        periodes.Enregistrer(PeriodeDeGarde.Affecter(parentB,
            new DateTime(2026, 7, 8), new DateTime(2026, 7, 8), "enfant-lea").Valeur!);

        var slots = new MongoSlotRepository(ConnectionString, _baseDeTest);
        slots.Enregistrer(SlotDeLocalisation.Poser("enfant-lea", "ecole",
            new DateTime(2026, 7, 8, 8, 30, 0), new DateTime(2026, 7, 8, 16, 30, 0)).Valeur!);
        slots.Enregistrer(SlotDeLocalisation.Poser("enfant-tom", "ecole",
            new DateTime(2026, 7, 8, 9, 0, 0), new DateTime(2026, 7, 8, 17, 0, 0)).Valeur!);

        var transferts = new MongoTransfertRepository(ConnectionString, _baseDeTest);
        transferts.Enregistrer(Transfert.Definir(parentA, parentB, "ecole",
            new TimeSpan(8, 30, 0), Mercredi_08_07_2026.ToDateTime(TimeOnly.MinValue), "enfant-lea").Valeur!);

        // --- When : le digest câblé sur les stores durables réels (nouvelles instances) ---
        var digest = DigestSurBaseReelle().Composer(Mercredi_08_07_2026, Mercredi_08_07_2026, "enfant-lea");

        // --- Then : jour de SURCHARGE → Parent B résolu (id + nom), le « où » de Léa seul, transfert résolu ---
        Assert.NotNull(digest.Immediat);
        var immediat = digest.Immediat!;
        Assert.True(immediat.Responsable.EstAssigne);
        Assert.Equal(parentB, immediat.Responsable.ActeurId);
        Assert.Equal("Bruno", immediat.Responsable.Nom);
        var slot = Assert.Single(immediat.Slots);
        Assert.Equal("ecole", slot.Libelle);
        Assert.NotNull(immediat.Transfert);
        Assert.Equal("Alice", immediat.Transfert!.CedantNom);
        Assert.Equal("Bruno", immediat.Transfert.RecevantNom);

        // --- Then : jour de CYCLE DE FOND (ISO 28 paire, aucune période) → Parent A résolu ---
        var fond = DigestSurBaseReelle().Composer(Mardi_07_07_2026, Mardi_07_07_2026, "enfant-lea").Immediat!;
        Assert.True(fond.Responsable.EstAssigne);
        Assert.Equal(parentA, fond.Responsable.ActeurId);
        Assert.Equal("Alice", fond.Responsable.Nom);
    }

    // --- Sc.2 : la section « à venir » liste les jours à venir PORTANT un transfert, chrono croissant ---
    [MongoRequisFact]
    public void Acceptation_Should_Composer_les_transferts_a_venir_chrono_croissant_sur_Mongo_reel()
    {
        // --- Given : deux acteurs durables, un cycle N=1 (ParentA chaque jour), deux transferts à venir ---
        var config = new ConfigurationFoyerMongo(ConnectionString, _baseDeTest);
        var parentA = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Alice")).Valeur!.ActeurId;
        var parentB = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Bruno")).Valeur!.ActeurId;

        var cycle = new CycleDeFondMongo(ConnectionString, _baseDeTest);
        cycle.DefinirCycle(new CycleDeFond(1, new Dictionary<int, string> { [0] = parentA }));

        var transferts = new MongoTransfertRepository(ConnectionString, _baseDeTest);
        transferts.Enregistrer(Transfert.Definir(parentA, parentB, "ecole",
            new TimeSpan(8, 30, 0), new DateOnly(2026, 7, 10).ToDateTime(TimeOnly.MinValue), "enfant-lea").Valeur!);
        transferts.Enregistrer(Transfert.Definir(parentA, parentB, "ecole",
            new TimeSpan(8, 30, 0), new DateOnly(2026, 7, 9).ToDateTime(TimeOnly.MinValue), "enfant-lea").Valeur!);

        // --- When : le digest câblé sur les stores durables réels, jour courant = 08/07 ---
        var avenir = DigestSurBaseReelle().Composer(Mercredi_08_07_2026, Mercredi_08_07_2026, "enfant-lea").AVenir;

        // --- Then : exactement les deux jours à venir portant un transfert, chrono CROISSANT, résolus ---
        Assert.Equal(2, avenir.Count);
        Assert.Equal(new DateOnly(2026, 7, 9), avenir[0].Date);
        Assert.Equal(new DateOnly(2026, 7, 10), avenir[1].Date);
        Assert.True(avenir[0].Responsable.EstAssigne);
        Assert.Equal(parentA, avenir[0].Responsable.ActeurId);
        Assert.NotNull(avenir[0].Transfert);
    }

    // --- Sc.4 : fenêtre sans à-venir + jour courant hors-fenêtre = digest vide neutre, store Mongo intact ---
    [MongoRequisFact]
    public void Acceptation_Should_Rendre_un_digest_vide_neutre_et_laisser_le_store_Mongo_intact()
    {
        // --- Given : un acteur + une surcharge durable en juillet, AUCUN cycle → aucune bascule dérivée ---
        var config = new ConfigurationFoyerMongo(ConnectionString, _baseDeTest);
        var parentA = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Alice")).Valeur!.ActeurId;

        var periodes = new MongoPeriodeRepository(ConnectionString, _baseDeTest);
        periodes.Enregistrer(PeriodeDeGarde.Affecter(parentA,
            new DateTime(2026, 7, 20), new DateTime(2026, 7, 22)).Valeur!);
        var avant = new MongoPeriodeRepository(ConnectionString, _baseDeTest).AllSnapshots().Count;

        // --- When : fenêtre SEPTEMBRE (ne contient NI le 08/07 NI aucun transfert), jour courant = 08/07 ---
        var digest = DigestSurBaseReelle().Composer(new DateOnly(2026, 9, 1), Mercredi_08_07_2026, "enfant-lea");

        // --- Then : sections vides neutres, aucun crash, store des surcharges STRICTEMENT intact ---
        Assert.Null(digest.Immediat);
        Assert.Empty(digest.AVenir);
        Assert.Equal(avant, new MongoPeriodeRepository(ConnectionString, _baseDeTest).AllSnapshots().Count);
    }

    public void Dispose()
    {
        try
        {
            new MongoClient(ConnectionString).DropDatabase(_baseDeTest);
        }
        catch
        {
            // Best effort : si Mongo est injoignable au teardown, rien à nettoyer.
        }
    }
}
