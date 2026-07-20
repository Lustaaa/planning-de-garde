using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Infrastructure;

namespace PlanningDeGarde.Api.Tests;

/// <summary>
/// Sprint 47 — Sc.5 — Acceptation d'<b>intégration sur Mongo RÉEL</b> (2ᵉ des deux adaptateurs) : PROPOSER
/// l'échange d'un jour RÉSOLU PAR LE FOND (Parent A) vers Parent B enregistre une Proposition <c>pending</c>
/// durable chez le recevant, SANS écrire aucune surcharge. Après redémarrage (nouvelles instances de stores),
/// la proposition subsiste, le store des surcharges est INTACT et la case résout toujours A par le fond.
/// </summary>
public sealed class ProposerEchangeMongoTests : IDisposable
{
    private const string ConnectionString = "mongodb://localhost:27017";
    private readonly string _baseDeTest = $"planning_test_{Guid.NewGuid():N}";

    private static readonly DateOnly Mercredi_08_07_2026 = new(2026, 7, 8); // ISO 28 paire → fond Parent A

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

    [MongoRequisFact]
    public void Acceptation_Should_Proposer_une_pending_durable_sans_ecrire_de_surcharge()
    {
        // --- Given : deux acteurs durables, un cycle N=2 (index 0 pair → Parent A) ---
        var config = new ConfigurationFoyerMongo(ConnectionString, _baseDeTest);
        var parentA = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Alice")).Valeur!.ActeurId;
        var parentB = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Bruno")).Valeur!.ActeurId;
        var _cyRepo = new CycleDeFondMongo(ConnectionString, _baseDeTest);
        var _cy = new CycleDeFond(2, new Dictionary<int, string> { [0] = parentA, [1] = parentB });
        _cyRepo.DefinirCycle(_cy); _cyRepo.DefinirCycle(_cy, "enfant-lea");

        // Précondition : le jour J est résolu par le fond (Alice), aucune surcharge.
        Assert.Equal(parentA, CaseDuJour(GrilleNeuve(), Mercredi_08_07_2026).ResponsableId);
        Assert.Empty(new MongoPeriodeRepository(ConnectionString, _baseDeTest).AllSnapshots());

        // --- When : PROPOSER l'échange du jour J vers Parent B via le use case câblé sur Mongo réel ---
        var propositions = new MongoPropositionEchangeRepository(ConnectionString, _baseDeTest);
        var resultat = new ProposerEchangeHandler(GrilleNeuve(), propositions, new ConfigurationFoyerMongo(ConnectionString, _baseDeTest))
            .Handle(new ProposerEchangeCommand(Mercredi_08_07_2026, "enfant-lea", parentB));
        Assert.True(resultat.EstSucces);

        // --- Redémarrage : NOUVELLES instances de stores sur la MÊME base persistée ---
        var proposition = Assert.Single(new MongoPropositionEchangeRepository(ConnectionString, _baseDeTest).AllSnapshots());
        Assert.Equal(StatutProposition.Proposee, proposition.Statut);
        Assert.Equal(parentB, proposition.VersActeurId);
        Assert.Equal(parentA, proposition.DeActeurId);
        Assert.Equal(Mercredi_08_07_2026, proposition.Jour);

        // --- Then : ANTI vert-qui-ment — store des surcharges INTACT, case toujours résolue par le fond A ---
        Assert.Empty(new MongoPeriodeRepository(ConnectionString, _baseDeTest).AllSnapshots());
        var caseJour = CaseDuJour(GrilleNeuve(), Mercredi_08_07_2026);
        Assert.Equal(parentA, caseJour.ResponsableId);
        Assert.Null(caseJour.Transfert);
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
