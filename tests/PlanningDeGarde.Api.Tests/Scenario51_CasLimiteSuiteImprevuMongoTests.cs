using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Infrastructure;

namespace PlanningDeGarde.Api.Tests;

/// <summary>
/// Sprint 51 — Sc.3 — Cas limite sur <b>Mongo RÉEL</b> (2ᵉ des deux adaptateurs) : ré-proposition greffée sur le
/// même imprévu = last-write-wins R11 (une seule pending durable, sans doublon) ; un imprévu HORS de la fenêtre
/// de grille chargée accepte une proposition greffée sans crash (la délégation s44 surcharge une date isolée).
/// Prouvé après redémarrage (nouvelles instances de stores) sur la MÊME base persistée.
/// </summary>
public sealed class Scenario51_CasLimiteSuiteImprevuMongoTests : IDisposable
{
    private const string ConnectionString = "mongodb://localhost:27017";
    private readonly string _baseDeTest = $"planning_test_{Guid.NewGuid():N}";

    private static readonly DateOnly Mercredi_08_07_2026 = new(2026, 7, 8);  // ISO 28 paire → fond Parent A
    private static readonly DateOnly LointainHorsFenetre = new(2027, 3, 17); // très loin de toute fenêtre par défaut

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

    private ProposerEchangeSuiteImprevuHandler ProposerSuiteImprevuNeuf()
        => new(
            new MongoJournalChangements(ConnectionString, _baseDeTest),
            new ProposerEchangeHandler(GrilleNeuve(), new MongoPropositionEchangeRepository(ConnectionString, _baseDeTest), new ConfigurationFoyerMongo(ConnectionString, _baseDeTest)));

    private string SignalerImprevu(DateOnly jour, string signalantId)
    {
        var horloge = new HorlogeTest { Maintenant = new DateTime(2026, 7, 1, 8, 0, 0) };
        return new SignalerImprevuHandler(new MongoJournalChangements(ConnectionString, _baseDeTest), horloge, GrilleNeuve())
            .Handle(new SignalerImprevuCommand(jour, "enfant-lea", TypeImprevu.Malade, signalantId)).Valeur!.Id;
    }

    [MongoRequisFact]
    public void Acceptation_Should_Last_write_wins_et_accepter_hors_fenetre_sans_crash_sur_store_durable()
    {
        // --- Given : trois acteurs durables, cycle N=2 (index 0 pair → Parent A) ---
        var config = new ConfigurationFoyerMongo(ConnectionString, _baseDeTest);
        var parentA = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Alice")).Valeur!.ActeurId;
        var parentB = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Bruno")).Valeur!.ActeurId;
        var parentC = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Chloe")).Valeur!.ActeurId;
        new CycleDeFondMongo(ConnectionString, _baseDeTest)
            .DefinirCycle(new CycleDeFond(2, new Dictionary<int, string> { [0] = parentA, [1] = parentB }));

        // --- Last-write-wins : deux propositions greffées sur le même imprévu (B puis C) → une seule pending durable ---
        var imprevu = SignalerImprevu(Mercredi_08_07_2026, parentA);
        var premiere = ProposerSuiteImprevuNeuf().Handle(new ProposerEchangeSuiteImprevuCommand(imprevu, parentB)).Valeur!;
        ProposerSuiteImprevuNeuf().Handle(new ProposerEchangeSuiteImprevuCommand(imprevu, parentC));

        var pendings = new MongoPropositionEchangeRepository(ConnectionString, _baseDeTest)
            .AllSnapshots().Where(p => p.Statut == StatutProposition.Proposee && p.Jour == Mercredi_08_07_2026).ToList();
        var pending = Assert.Single(pendings);
        Assert.Equal(parentC, pending.VersActeurId);
        Assert.NotEqual(premiere.Id, pending.Id);
        Assert.Empty(new MongoPeriodeRepository(ConnectionString, _baseDeTest).AllSnapshots());

        // --- Hors fenêtre : imprévu très lointain → proposition greffée acceptée sans crash, surcharge durable ---
        // Le recevant est l'acteur NON résolu ce jour-là (proposer au résolu = soi-même, refusé — indép. parité ISO).
        var resolu = CaseDuJour(GrilleNeuve(), LointainHorsFenetre).ResponsableId;
        var recevant = resolu == parentA ? parentB : parentA;

        var imprevuLointain = SignalerImprevu(LointainHorsFenetre, parentA);
        var propositionLointaine = ProposerSuiteImprevuNeuf().Handle(new ProposerEchangeSuiteImprevuCommand(imprevuLointain, recevant)).Valeur!;
        var accepte = new AccepterPropositionHandler(
                new MongoPropositionEchangeRepository(ConnectionString, _baseDeTest),
                new DeleguerRecuperationHandler(GrilleNeuve(), new MongoPeriodeRepository(ConnectionString, _baseDeTest), new ConfigurationFoyerMongo(ConnectionString, _baseDeTest)))
            .Handle(new AccepterPropositionCommand(propositionLointaine.Id));
        Assert.True(accepte.EstSucces);

        var surcharge = Assert.Single(new MongoPeriodeRepository(ConnectionString, _baseDeTest).AllSnapshots());
        Assert.Equal(recevant, surcharge.ResponsableId);
        Assert.Equal(LointainHorsFenetre, DateOnly.FromDateTime(surcharge.Debut));
        Assert.Equal(recevant, CaseDuJour(GrilleNeuve(), LointainHorsFenetre).ResponsableId);
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
