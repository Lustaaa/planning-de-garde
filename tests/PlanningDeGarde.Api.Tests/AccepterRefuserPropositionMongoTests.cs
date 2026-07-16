using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Infrastructure;

namespace PlanningDeGarde.Api.Tests;

/// <summary>
/// Sprint 47 — Sc.6 — Acceptation d'<b>intégration sur Mongo RÉEL</b> : ACCEPTER une proposition
/// <c>pending</c> COMPOSE la délégation s44 (surcharge durable + transfert dérivé s31) et passe la
/// proposition à « accepté » ; REFUSER passe à « refusé » sans aucune écriture. Prouvé après redémarrage
/// (nouvelles instances de stores) sur la MÊME base persistée.
/// </summary>
public sealed class AccepterRefuserPropositionMongoTests : IDisposable
{
    private const string ConnectionString = "mongodb://localhost:27017";
    private readonly string _baseDeTest = $"planning_test_{Guid.NewGuid():N}";

    private static readonly DateOnly Mercredi_08_07_2026 = new(2026, 7, 8); // ISO 28 paire → fond Parent A
    private static readonly DateOnly Vendredi_10_07_2026 = new(2026, 7, 10); // même semaine ISO 28 → fond Parent A

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
    public void Acceptation_Should_Composer_la_delegation_durable_a_l_acceptation_et_ne_rien_ecrire_au_refus()
    {
        // --- Given : deux acteurs durables, cycle N=2 (index 0 pair → Parent A) ---
        var config = new ConfigurationFoyerMongo(ConnectionString, _baseDeTest);
        var parentA = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Alice")).Valeur!.ActeurId;
        var parentB = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Bruno")).Valeur!.ActeurId;
        new CycleDeFondMongo(ConnectionString, _baseDeTest)
            .DefinirCycle(new CycleDeFond(2, new Dictionary<int, string> { [0] = parentA, [1] = parentB }));

        var propositions = new MongoPropositionEchangeRepository(ConnectionString, _baseDeTest);
        var proposition = new ProposerEchangeHandler(GrilleNeuve(), propositions, new ConfigurationFoyerMongo(ConnectionString, _baseDeTest))
            .Handle(new ProposerEchangeCommand(Mercredi_08_07_2026, "enfant-lea", parentB)).Valeur!;

        // --- When : ACCEPTER via des stores Mongo neufs ---
        var accepte = new AccepterPropositionHandler(new MongoPropositionEchangeRepository(ConnectionString, _baseDeTest), DelegationNeuve())
            .Handle(new AccepterPropositionCommand(proposition.Id));
        Assert.True(accepte.EstSucces);

        // --- Redémarrage : surcharge durable Bruno + transfert dérivé + proposition "accepté" ---
        var surcharge = Assert.Single(new MongoPeriodeRepository(ConnectionString, _baseDeTest).AllSnapshots());
        Assert.Equal(parentB, surcharge.ResponsableId);
        var caseJour = CaseDuJour(GrilleNeuve(), Mercredi_08_07_2026);
        Assert.Equal(parentB, caseJour.ResponsableId);
        Assert.Equal("Alice", caseJour.Transfert!.NomDepart);
        Assert.Equal("Bruno", caseJour.Transfert.NomArrivee);
        Assert.Equal(StatutProposition.Acceptee,
            new MongoPropositionEchangeRepository(ConnectionString, _baseDeTest).ParId(proposition.Id)!.Statut);

        // --- REFUSER : une autre pending (vendredi même semaine, fond Alice) refusée sans écriture ---
        var pending2 = new ProposerEchangeHandler(GrilleNeuve(), new MongoPropositionEchangeRepository(ConnectionString, _baseDeTest), new ConfigurationFoyerMongo(ConnectionString, _baseDeTest))
            .Handle(new ProposerEchangeCommand(Vendredi_10_07_2026, "enfant-lea", parentB)).Valeur!;
        var surchargesAvant = new MongoPeriodeRepository(ConnectionString, _baseDeTest).AllSnapshots().Count;

        var refuse = new RefuserPropositionHandler(new MongoPropositionEchangeRepository(ConnectionString, _baseDeTest))
            .Handle(new RefuserPropositionCommand(pending2.Id));
        Assert.True(refuse.EstSucces);

        Assert.Equal(StatutProposition.Refusee,
            new MongoPropositionEchangeRepository(ConnectionString, _baseDeTest).ParId(pending2.Id)!.Statut);
        Assert.Equal(surchargesAvant, new MongoPeriodeRepository(ConnectionString, _baseDeTest).AllSnapshots().Count);
        Assert.Equal(parentA, CaseDuJour(GrilleNeuve(), Vendredi_10_07_2026).ResponsableId);
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
