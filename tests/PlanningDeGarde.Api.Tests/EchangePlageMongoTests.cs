using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Infrastructure;

namespace PlanningDeGarde.Api.Tests;

/// <summary>
/// Sprint 52 — Sc.1 &amp; Sc.3 — Acceptation d'<b>intégration sur Mongo RÉEL</b> (2ᵉ des deux adaptateurs) :
/// PROPOSER un échange sur la PLAGE <c>[J1..J3]</c> enregistre une Proposition <c>pending</c> durable portant
/// l'intervalle, SANS écrire aucune surcharge (invariant s47 : store intact, plage résolue par le fond) ;
/// ACCEPTER compose la délégation-plage s45 (UNE surcharge multi-jours durable + transferts dérivés s31 aux
/// deux frontières). Prouvé après redémarrage (nouvelles instances de stores) sur la MÊME base persistée.
/// </summary>
public sealed class EchangePlageMongoTests : IDisposable
{
    private const string ConnectionString = "mongodb://localhost:27017";
    private readonly string _baseDeTest = $"planning_test_{Guid.NewGuid():N}";

    // ISO 28 (semaine du lundi 06/07/2026) → index 0 (pair) → Parent A par le fond.
    private static readonly DateOnly Mardi_07 = new(2026, 7, 7);    // J1
    private static readonly DateOnly Mercredi_08 = new(2026, 7, 8); // J2
    private static readonly DateOnly Jeudi_09 = new(2026, 7, 9);    // J3
    private static readonly DateOnly Vendredi_10 = new(2026, 7, 10); // J3+1 (sortie, fond A de nouveau)

    private static JourCase CaseDuJour(GrilleAgendaQuery grille, DateOnly jour)
        => grille.Projeter(jour, VuePlanning.Semaine).Jours.Single(j => j.Date == jour);

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

    private DeleguerRecuperationHandler DelegationNeuve()
        => new(GrilleNeuve(), new MongoPeriodeRepository(ConnectionString, _baseDeTest), new ConfigurationFoyerMongo(ConnectionString, _baseDeTest));

    [MongoRequisFact]
    public void Acceptation_Should_Proposer_pending_de_plage_durable_puis_composer_la_delegation_plage_a_l_acceptation()
    {
        // --- Given : deux acteurs durables, cycle N=2 (index 0 pair → Parent A) ---
        var config = new ConfigurationFoyerMongo(ConnectionString, _baseDeTest);
        var parentA = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Alice")).Valeur!.ActeurId;
        var parentB = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Bruno")).Valeur!.ActeurId;
        new CycleDeFondMongo(ConnectionString, _baseDeTest)
            .DefinirCycle(new CycleDeFond(2, new Dictionary<int, string> { [0] = parentA, [1] = parentB }));

        // Précondition : la plage est résolue par le fond (Alice), aucune surcharge.
        foreach (var j in new[] { Mardi_07, Mercredi_08, Jeudi_09 })
            Assert.Equal(parentA, CaseDuJour(GrilleNeuve(), j).ResponsableId);

        // --- When : PROPOSER l'échange de la PLAGE [J1..J3] vers Parent B (Sc.1) ---
        var propositions = new MongoPropositionEchangeRepository(ConnectionString, _baseDeTest);
        var proposition = new ProposerEchangeHandler(GrilleNeuve(), propositions, new ConfigurationFoyerMongo(ConnectionString, _baseDeTest))
            .Handle(new ProposerEchangeCommand(Mardi_07, "enfant-lea", parentB, Jeudi_09)).Valeur!;

        // --- Redémarrage : la pending durable porte l'intervalle [J1..J3], AUCUNE surcharge, plage intacte ---
        var pendingDurable = Assert.Single(new MongoPropositionEchangeRepository(ConnectionString, _baseDeTest).AllSnapshots());
        Assert.Equal(StatutProposition.Proposee, pendingDurable.Statut);
        Assert.Equal(Mardi_07, pendingDurable.Jour);
        Assert.Equal(Jeudi_09, pendingDurable.JourFin);
        Assert.Empty(new MongoPeriodeRepository(ConnectionString, _baseDeTest).AllSnapshots());
        foreach (var j in new[] { Mardi_07, Mercredi_08, Jeudi_09 })
            Assert.Equal(parentA, CaseDuJour(GrilleNeuve(), j).ResponsableId);

        // --- When : ACCEPTER via des stores Mongo neufs (Sc.3) ---
        var accepte = new AccepterPropositionHandler(new MongoPropositionEchangeRepository(ConnectionString, _baseDeTest), DelegationNeuve())
            .Handle(new AccepterPropositionCommand(proposition.Id));
        Assert.True(accepte.EstSucces);

        // --- Redémarrage : UNE surcharge durable [J1..J3] responsable Bruno, chaque jour prime Bruno ---
        var surcharge = Assert.Single(new MongoPeriodeRepository(ConnectionString, _baseDeTest).AllSnapshots());
        Assert.Equal(parentB, surcharge.ResponsableId);
        Assert.Equal(Mardi_07, DateOnly.FromDateTime(surcharge.Debut));
        Assert.Equal(Jeudi_09, DateOnly.FromDateTime(surcharge.Fin));
        foreach (var j in new[] { Mardi_07, Mercredi_08, Jeudi_09 })
            Assert.Equal(parentB, CaseDuJour(GrilleNeuve(), j).ResponsableId);

        // --- Transferts dérivés s31 aux deux frontières + proposition « accepté » ---
        Assert.Equal("Bruno", CaseDuJour(GrilleNeuve(), Mardi_07).Transfert!.NomArrivee);
        Assert.Equal("Bruno", CaseDuJour(GrilleNeuve(), Vendredi_10).Transfert!.NomDepart);
        Assert.Equal(StatutProposition.Acceptee,
            new MongoPropositionEchangeRepository(ConnectionString, _baseDeTest).ParId(proposition.Id)!.Statut);
    }

    public void Dispose()
    {
        try
        {
            new MongoClient(ConnectionString).DropDatabase(_baseDeTest);
        }
        catch
        {
            // Best effort.
        }
    }
}
