using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Infrastructure;

namespace PlanningDeGarde.Api.Tests;

/// <summary>
/// Sprint 51 — Sc.2 — Acceptation d'<b>intégration sur Mongo RÉEL</b> (2ᵉ des deux adaptateurs) : une proposition
/// GREFFÉE SUR UN IMPRÉVU journalisé (composition s51) est un échange s47 STANDARD — ACCEPTER COMPOSE la délégation
/// s44 (surcharge durable + transfert dérivé s31) et passe la proposition à « accepté » ; l'imprévu d'origine reste
/// au JOURNAL durable, inchangé (fait informatif jamais lu par la résolution). Prouvé après redémarrage (nouvelles
/// instances de stores) sur la MÊME base persistée.
/// </summary>
public sealed class Scenario51_AccepterSuiteImprevuMongoTests : IDisposable
{
    private const string ConnectionString = "mongodb://localhost:27017";
    private readonly string _baseDeTest = $"planning_test_{Guid.NewGuid():N}";

    private static readonly DateOnly Mercredi_08_07_2026 = new(2026, 7, 8); // ISO 28 paire → fond Parent A

    private sealed class HorlogeTest : IDateTimeProvider
    {
        public DateTime Maintenant { get; set; }
        public DateOnly Aujourdhui => DateOnly.FromDateTime(Maintenant);
    }

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

    private ProposerEchangeSuiteImprevuHandler ProposerSuiteImprevuNeuf()
        => new(
            new MongoJournalChangements(ConnectionString, _baseDeTest),
            new ProposerEchangeHandler(GrilleNeuve(), new MongoPropositionEchangeRepository(ConnectionString, _baseDeTest), new ConfigurationFoyerMongo(ConnectionString, _baseDeTest)));

    [MongoRequisFact]
    public void Acceptation_Should_Composer_la_delegation_durable_a_l_acceptation_dune_proposition_greffee_sur_imprevu()
    {
        // --- Given : deux acteurs durables, cycle N=2 (index 0 pair → Parent A) ---
        var config = new ConfigurationFoyerMongo(ConnectionString, _baseDeTest);
        var parentA = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Alice")).Valeur!.ActeurId;
        var parentB = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Bruno")).Valeur!.ActeurId;
        new CycleDeFondMongo(ConnectionString, _baseDeTest)
            .DefinirCycle(new CycleDeFond(2, new Dictionary<int, string> { [0] = parentA, [1] = parentB }));

        // --- Given : un imprévu « malade » durable au journal sur le jour J, PUIS une proposition greffée A → B ---
        var horloge = new HorlogeTest { Maintenant = new DateTime(2026, 7, 1, 8, 0, 0) };
        var imprevu = new SignalerImprevuHandler(new MongoJournalChangements(ConnectionString, _baseDeTest), horloge, GrilleNeuve())
            .Handle(new SignalerImprevuCommand(Mercredi_08_07_2026, "enfant-lea", TypeImprevu.Malade, parentA)).Valeur!;
        var proposition = ProposerSuiteImprevuNeuf().Handle(new ProposerEchangeSuiteImprevuCommand(imprevu.Id, parentB)).Valeur!;
        Assert.Empty(new MongoPeriodeRepository(ConnectionString, _baseDeTest).AllSnapshots()); // pending greffée n'écrit rien

        // --- When : ACCEPTER via des stores Mongo neufs ---
        var accepte = new AccepterPropositionHandler(new MongoPropositionEchangeRepository(ConnectionString, _baseDeTest), DelegationNeuve())
            .Handle(new AccepterPropositionCommand(proposition.Id));
        Assert.True(accepte.EstSucces);

        // --- Redémarrage : surcharge durable Bruno + transfert dérivé + proposition « accepté » ---
        var surcharge = Assert.Single(new MongoPeriodeRepository(ConnectionString, _baseDeTest).AllSnapshots());
        Assert.Equal(parentB, surcharge.ResponsableId);
        var caseJour = CaseDuJour(GrilleNeuve(), Mercredi_08_07_2026);
        Assert.Equal(parentB, caseJour.ResponsableId);
        Assert.Equal("Alice", caseJour.Transfert!.NomDepart);
        Assert.Equal("Bruno", caseJour.Transfert.NomArrivee);
        Assert.Equal(StatutProposition.Acceptee,
            new MongoPropositionEchangeRepository(ConnectionString, _baseDeTest).ParId(proposition.Id)!.Statut);

        // --- Then : l'imprévu d'origine reste au JOURNAL durable, inchangé (fait informatif jamais « résolu ») ---
        var imprevuApres = new MongoJournalChangements(ConnectionString, _baseDeTest).Tout().Single(e => e.Type == TypeChangement.Imprevu);
        Assert.Equal(imprevu.Id, imprevuApres.Id);
        Assert.Equal(TypeImprevu.Malade, imprevuApres.Imprevu);
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
