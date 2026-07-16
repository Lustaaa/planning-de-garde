using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Infrastructure;

namespace PlanningDeGarde.Api.Tests;

/// <summary>
/// Sprint 48 — Sc.3 — Acceptation d'<b>intégration sur Mongo RÉEL</b> (2ᵉ des deux adaptateurs) : un imprévu
/// signalé (s48) est consigné DURABLEMENT au journal de changements, avec son sous-type (malade / retard) et
/// son motif optionnel (y compris VIDE) qui survivent au redémarrage — SANS aucune écriture de surcharge (le
/// signalement ne touche jamais la résolution, invariant s48). Un jour HORS fenêtre s'enregistre sans crash.
/// </summary>
public sealed class ImprevuMongoTests : IDisposable
{
    private const string ConnectionString = "mongodb://localhost:27017";
    private readonly string _baseDeTest = $"planning_test_{Guid.NewGuid():N}";

    private sealed class HorlogeTest : IDateTimeProvider
    {
        public DateTime Maintenant { get; set; }
        public DateOnly Aujourdhui => DateOnly.FromDateTime(Maintenant);
    }

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
    public void Acceptation_Should_Consigner_durablement_l_imprevu_avec_sous_type_et_motif_sans_ecrire_de_surcharge()
    {
        // --- Given : deux acteurs durables, cycle N=2 (index 0 pair → Alice pour la semaine ISO 28) ---
        var config = new ConfigurationFoyerMongo(ConnectionString, _baseDeTest);
        var aliceId = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Alice")).Valeur!.ActeurId;
        var brunoId = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Bruno")).Valeur!.ActeurId;
        new CycleDeFondMongo(ConnectionString, _baseDeTest)
            .DefinirCycle(new CycleDeFond(2, new Dictionary<int, string> { [0] = aliceId, [1] = brunoId }));

        var horloge = new HorlogeTest { Maintenant = new DateTime(2026, 7, 1, 8, 0, 0) };
        var jour = new DateOnly(2026, 7, 8);

        // --- When : Alice signale MALADE avec un motif renseigné, puis RETARD avec motif VIDE (jour hors fenêtre) ---
        new SignalerImprevuHandler(new MongoJournalChangements(ConnectionString, _baseDeTest), horloge, GrilleNeuve())
            .Handle(new SignalerImprevuCommand(jour, "enfant-lea", TypeImprevu.Malade, aliceId, "fièvre 39"));
        horloge.Maintenant = horloge.Maintenant.AddMinutes(10);
        new SignalerImprevuHandler(new MongoJournalChangements(ConnectionString, _baseDeTest), horloge, GrilleNeuve())
            .Handle(new SignalerImprevuCommand(new DateOnly(2028, 12, 31), "enfant-lea", TypeImprevu.Retard, aliceId));

        // --- Then : redémarrage — les deux imprévus durables, sous-type + motif (y compris vide) round-trip ---
        var tout = new MongoJournalChangements(ConnectionString, _baseDeTest).Tout();
        Assert.Equal(2, tout.Count);

        var malade = tout.Single(e => e.Imprevu == TypeImprevu.Malade);
        Assert.Equal(TypeChangement.Imprevu, malade.Type);
        Assert.Equal(jour, malade.Jour);
        Assert.Equal("fièvre 39", malade.Motif);
        Assert.Equal(aliceId, malade.RecevantId); // signalant

        var retard = tout.Single(e => e.Imprevu == TypeImprevu.Retard);
        Assert.Equal(new DateOnly(2028, 12, 31), retard.Jour); // jour hors fenêtre, sans crash
        Assert.Equal("", retard.Motif); // motif vide round-trip

        // --- Then (anti vert-qui-ment) : AUCUNE surcharge écrite — le signalement ne touche jamais la résolution ---
        Assert.Empty(new MongoPeriodeRepository(ConnectionString, _baseDeTest).AllSnapshots());
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
