using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Infrastructure;

namespace PlanningDeGarde.Api.Tests;

/// <summary>
/// Sprint 43 — Acceptation d'<b>intégration sur Mongo RÉEL</b> (conteneur Docker, jamais une doublure : le
/// 2ᵉ des deux adaptateurs, InMemory étant prouvé côté PlanningDeGarde.Tests). La liste
/// <see cref="AVenirQuery"/> est câblée sur les stores durables réels (config foyer + périodes + slots +
/// transferts + cycle Mongo) : elle restitue la LISTE ORDONNÉE des jours à venir en COMPOSANT la résolution
/// existante (surcharge &gt; fond &gt; neutre) sur un profil réaliste — un jour de <b>surcharge</b>, des jours
/// de <b>cycle de fond</b>, un jour <b>avec slot + transfert</b>. <b>Lecture PURE</b>.
///
/// <b>Skip propre</b> (<see cref="MongoRequisFactAttribute"/>) si Docker / Mongo est indisponible.
/// </summary>
public sealed class AVenirMongoTests : IDisposable
{
    private const string ConnectionString = "mongodb://localhost:27017";
    private readonly string _baseDeTest = $"planning_test_{Guid.NewGuid():N}";

    private static readonly DateOnly Mardi_07_07_2026 = new(2026, 7, 7);     // « aujourd'hui » (ancre)
    private static readonly DateOnly Mercredi_08_07_2026 = new(2026, 7, 8);  // surcharge Parent B + slot + transfert

    [MongoRequisFact]
    public void Acceptation_Should_Restituer_la_liste_a_venir_sur_Mongo_reel_surcharge_fond_slot_transfert()
    {
        // --- Given : deux acteurs durables, un cycle N=2, une surcharge le 08/07, un slot Léa + un transfert ---
        var config = new ConfigurationFoyerMongo(ConnectionString, _baseDeTest);
        var parentA = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Alice")).Valeur!.ActeurId;
        var parentB = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Bruno")).Valeur!.ActeurId;

        var cycle = new CycleDeFondMongo(ConnectionString, _baseDeTest);
        cycle.DefinirCycle(new CycleDeFond(2, new Dictionary<int, string> { [0] = parentA, [1] = parentB }));

        var periodes = new MongoPeriodeRepository(ConnectionString, _baseDeTest);
        periodes.Enregistrer(PeriodeDeGarde.Affecter(parentB,
            new DateTime(2026, 7, 8), new DateTime(2026, 7, 8)).Valeur!);

        var slots = new MongoSlotRepository(ConnectionString, _baseDeTest);
        slots.Enregistrer(SlotDeLocalisation.Poser("enfant-lea", "ecole",
            new DateTime(2026, 7, 8, 8, 30, 0), new DateTime(2026, 7, 8, 16, 30, 0)).Valeur!);
        slots.Enregistrer(SlotDeLocalisation.Poser("enfant-tom", "ecole",
            new DateTime(2026, 7, 8, 9, 0, 0), new DateTime(2026, 7, 8, 17, 0, 0)).Valeur!);

        var transferts = new MongoTransfertRepository(ConnectionString, _baseDeTest);
        transferts.Enregistrer(Transfert.Definir(parentA, parentB, "ecole",
            TimeSpan.FromHours(8.5), Mercredi_08_07_2026.ToDateTime(TimeOnly.MinValue)).Valeur!);

        // --- Redémarrage : NOUVELLES instances de stores sur la MÊME base persistée ---
        var configApres = new ConfigurationFoyerMongo(ConnectionString, _baseDeTest);
        var grille = new GrilleAgendaQuery(
            new MongoSlotRepository(ConnectionString, _baseDeTest),
            new MongoPeriodeRepository(ConnectionString, _baseDeTest),
            configApres, configApres,
            new CycleDeFondMongo(ConnectionString, _baseDeTest),
            configApres,
            new MongoSlotRecurrentRepository(ConnectionString, _baseDeTest),
            new MongoTransfertRepository(ConnectionString, _baseDeTest));

        var aVenir = new AVenirQuery(grille).Lire(Mardi_07_07_2026, "enfant-lea", VuePlanning.Semaine);

        // --- Then : jours strictement après aujourd'hui, ordonnés, non vides ---
        Assert.NotEmpty(aVenir);
        Assert.All(aVenir, j => Assert.True(j.Date > Mardi_07_07_2026));
        Assert.Equal(aVenir.Select(j => j.Date).OrderBy(d => d), aVenir.Select(j => j.Date));

        // --- Then : le 08/07 → Parent B résolu (surcharge) + le « où » de Léa seul + transfert bicolore ---
        var jour = aVenir.Single(j => j.Date == Mercredi_08_07_2026);
        Assert.True(jour.Responsable.EstAssigne);
        Assert.Equal(parentB, jour.Responsable.ActeurId);
        Assert.Equal("Bruno", jour.Responsable.Nom);
        var slot = Assert.Single(jour.Slots);
        Assert.Equal("ecole", slot.Libelle);
        Assert.NotNull(jour.Transfert);
        Assert.Equal("Alice", jour.Transfert!.CedantNom);
        Assert.Equal("Bruno", jour.Transfert.RecevantNom);
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
