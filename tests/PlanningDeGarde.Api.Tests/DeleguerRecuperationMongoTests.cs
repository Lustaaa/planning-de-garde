using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Infrastructure;

namespace PlanningDeGarde.Api.Tests;

/// <summary>
/// Sprint 44 — Sc.1 — Acceptation d'<b>intégration sur Mongo RÉEL</b> (conteneur Docker) : le 2ᵉ des deux
/// adaptateurs (InMemory étant prouvé côté PlanningDeGarde.Tests). Déléguer la récupération d'un jour
/// RÉSOLU PAR LE FOND (Parent A) vers Parent B écrit une SURCHARGE d'UN jour durable via le chemin s06 ;
/// après redémarrage (nouvelles instances de stores), la carte résout B pour ce jour ET matérialise le
/// transfert dérivé A → B (s31). Écriture réelle, jamais une doublure.
///
/// <b>Skip propre</b> (<see cref="MongoRequisFactAttribute"/>) si Docker / Mongo est indisponible.
/// </summary>
public sealed class DeleguerRecuperationMongoTests : IDisposable
{
    private const string ConnectionString = "mongodb://localhost:27017";
    private readonly string _baseDeTest = $"planning_test_{Guid.NewGuid():N}";

    private static readonly DateOnly Mercredi_08_07_2026 = new(2026, 7, 8); // ISO 28 paire → fond Parent A, aucune surcharge

    private GrilleAgendaQuery GrilleNeuve()
    {
        var config = new ConfigurationFoyerMongo(ConnectionString, _baseDeTest);
        return new GrilleAgendaQuery(
            new MongoSlotRepository(ConnectionString, _baseDeTest),
            new MongoPeriodeRepository(ConnectionString, _baseDeTest),
            config, config,
            new CycleDeFondMongo(ConnectionString, _baseDeTest),
            config,
            new MongoSlotRecurrentRepository(ConnectionString, _baseDeTest),
            new MongoTransfertRepository(ConnectionString, _baseDeTest));
    }

    [MongoRequisFact]
    public void Acceptation_Should_Deleguer_un_jour_de_fond_ecrit_une_surcharge_durable_avec_transfert_derive()
    {
        // --- Given : deux acteurs durables, un cycle N=2 (index 0 pair → Parent A) ---
        var config = new ConfigurationFoyerMongo(ConnectionString, _baseDeTest);
        var parentA = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Alice")).Valeur!.ActeurId;
        var parentB = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Bruno")).Valeur!.ActeurId;

        var cycle = new CycleDeFondMongo(ConnectionString, _baseDeTest);
        cycle.DefinirCycle(new CycleDeFond(2, new Dictionary<int, string> { [0] = parentA, [1] = parentB }));

        // Précondition : le jour J est résolu par le fond (Parent A).
        Assert.Equal(parentA, new CarteDuJourQuery(GrilleNeuve()).Lire(Mercredi_08_07_2026, "enfant-lea").Responsable.ActeurId);

        // --- When : je délègue la récupération du jour J à Parent B via le use case câblé sur Mongo réel ---
        var grille = GrilleNeuve();
        var periodesEcriture = new MongoPeriodeRepository(ConnectionString, _baseDeTest);
        var configExistence = new ConfigurationFoyerMongo(ConnectionString, _baseDeTest);
        var resultat = new DeleguerRecuperationHandler(grille, periodesEcriture, configExistence)
            .Handle(new DeleguerRecuperationCommand(Mercredi_08_07_2026, "enfant-lea", parentB));
        Assert.True(resultat.EstSucces);

        // --- Redémarrage : NOUVELLES instances de stores sur la MÊME base persistée ---
        var periodesRelues = new MongoPeriodeRepository(ConnectionString, _baseDeTest).AllSnapshots();
        var surcharge = Assert.Single(periodesRelues);
        Assert.Equal(parentB, surcharge.ResponsableId);
        Assert.Equal(Mercredi_08_07_2026, DateOnly.FromDateTime(surcharge.Debut));
        Assert.Equal(Mercredi_08_07_2026, DateOnly.FromDateTime(surcharge.Fin));

        // --- Then : la carte (grille neuve) résout B pour J + transfert dérivé A → B ---
        var carte = new CarteDuJourQuery(GrilleNeuve()).Lire(Mercredi_08_07_2026, "enfant-lea");
        Assert.Equal(parentB, carte.Responsable.ActeurId);
        Assert.Equal("Bruno", carte.Responsable.Nom);
        Assert.NotNull(carte.Transfert);
        Assert.Equal("Alice", carte.Transfert!.CedantNom);
        Assert.Equal("Bruno", carte.Transfert.RecevantNom);
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
