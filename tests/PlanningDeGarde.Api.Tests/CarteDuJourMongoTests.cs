using System;
using System.Collections.Generic;
using MongoDB.Driver;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Infrastructure;

namespace PlanningDeGarde.Api.Tests;

/// <summary>
/// Sprint 42 — Sc.1 — Acceptation d'<b>intégration sur Mongo RÉEL</b> (conteneur Docker, jamais une
/// doublure : le 2ᵉ des deux adaptateurs, InMemory étant prouvé côté PlanningDeGarde.Tests). La carte
/// <see cref="CarteDuJourQuery"/> est câblée sur les stores durables réels (config foyer + périodes +
/// cycle Mongo) : elle restitue le RESPONSABLE RÉSOLU du jour (id stable + nom + couleur) en COMPOSANT la
/// résolution existante (surcharge &gt; fond &gt; neutre), sur un profil de données réaliste — un jour de
/// <b>surcharge</b>, un jour de <b>cycle de fond</b>, un jour <b>neutre</b>. <b>Lecture PURE</b>.
///
/// <b>Skip propre</b> (<see cref="MongoRequisFactAttribute"/>) si Docker / Mongo est indisponible.
/// </summary>
public sealed class CarteDuJourMongoTests : IDisposable
{
    private const string ConnectionString = "mongodb://localhost:27017";
    private readonly string _baseDeTest = $"planning_test_{Guid.NewGuid():N}";

    private static readonly DateOnly Mardi_07_07_2026 = new(2026, 7, 7);    // ISO 28 paire → fond Parent A
    private static readonly DateOnly Mercredi_08_07_2026 = new(2026, 7, 8); // surcharge explicite Parent B
    private static readonly DateOnly Dimanche_04_01_2026 = new(2026, 1, 4); // hors période, ISO 01 impaire → fond Parent B

    [MongoRequisFact]
    public void Acceptation_Should_Restituer_le_responsable_resolu_sur_Mongo_reel_surcharge_fond_neutre()
    {
        // --- Given : deux acteurs durables (Parent A, Parent B), un cycle N=2, une surcharge le 08/07 ---
        var config = new ConfigurationFoyerMongo(ConnectionString, _baseDeTest);
        var parentA = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Alice")).Valeur!.ActeurId;
        var parentB = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Bruno")).Valeur!.ActeurId;

        var cycle = new CycleDeFondMongo(ConnectionString, _baseDeTest);
        cycle.DefinirCycle(new CycleDeFond(2, new Dictionary<int, string> { [0] = parentA, [1] = parentB }));

        var periodes = new MongoPeriodeRepository(ConnectionString, _baseDeTest);
        periodes.Enregistrer(PeriodeDeGarde.Affecter(parentB,
            new DateTime(2026, 7, 8), new DateTime(2026, 7, 8)).Valeur!);

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
        var carte = new CarteDuJourQuery(grille);

        // --- Then : jour de SURCHARGE → Parent B résolu (id + nom Bruno) ---
        var surcharge = carte.Lire(Mercredi_08_07_2026, "enfant-lea").Responsable;
        Assert.True(surcharge.EstAssigne);
        Assert.Equal(parentB, surcharge.ActeurId);
        Assert.Equal("Bruno", surcharge.Nom);

        // --- Then : jour de CYCLE DE FOND (ISO 28 paire, aucune période) → Parent A résolu ---
        var fond = carte.Lire(Mardi_07_07_2026, "enfant-lea").Responsable;
        Assert.True(fond.EstAssigne);
        Assert.Equal(parentA, fond.ActeurId);
        Assert.Equal("Alice", fond.Nom);
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
