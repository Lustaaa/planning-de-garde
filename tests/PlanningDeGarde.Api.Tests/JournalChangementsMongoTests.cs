using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Infrastructure;

namespace PlanningDeGarde.Api.Tests;

/// <summary>
/// Sprint 47 — Sc.1 — Acceptation d'<b>intégration sur Mongo RÉEL</b> (2ᵉ des deux adaptateurs) : le JOURNAL
/// DE CHANGEMENTS append-only survit au redémarrage. En particulier une REPRISE s46 (qui SUPPRIME la
/// surcharge) consigne bien son événement durable — le journal ne dérive PAS de l'état courant — et n'est
/// JAMAIS lu par la résolution (la case retombe sur le fond, vérité = périodes).
/// </summary>
public sealed class JournalChangementsMongoTests : IDisposable
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

    [MongoRequisFact]
    public void Acceptation_Should_Consigner_durablement_la_reprise_sans_etre_lu_par_la_resolution()
    {
        // --- Given : deux acteurs durables, cycle N=2 (index 0 pair → Parent A) ---
        var config = new ConfigurationFoyerMongo(ConnectionString, _baseDeTest);
        var parentA = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Alice")).Valeur!.ActeurId;
        var parentB = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Bruno")).Valeur!.ActeurId;
        new CycleDeFondMongo(ConnectionString, _baseDeTest)
            .DefinirCycle(new CycleDeFond(2, new Dictionary<int, string> { [0] = parentA, [1] = parentB }));

        var horloge = new HorlogeTest { Maintenant = new DateTime(2026, 7, 1, 8, 0, 0) };

        // --- DÉLÉGATION Alice → Bruno du jour J (journal Mongo réel) ---
        new DeleguerRecuperationHandler(
                GrilleNeuve(), new MongoPeriodeRepository(ConnectionString, _baseDeTest), new ConfigurationFoyerMongo(ConnectionString, _baseDeTest),
                new MongoJournalChangements(ConnectionString, _baseDeTest), horloge)
            .Handle(new DeleguerRecuperationCommand(Mercredi_08_07_2026, "enfant-lea", parentB));

        // --- REPRISE du jour J — SUPPRIME la surcharge, consigne son événement ---
        horloge.Maintenant = horloge.Maintenant.AddMinutes(10);
        new AnnulerDelegationHandler(
                new MongoPeriodeRepository(ConnectionString, _baseDeTest),
                new MongoJournalChangements(ConnectionString, _baseDeTest), horloge)
            .Handle(new AnnulerDelegationCommand(Mercredi_08_07_2026, "enfant-lea"));

        // --- Redémarrage : journal durable, la reprise a bien été consignée malgré la suppression ---
        var flux = new FluxNotificationsQuery(new MongoJournalChangements(ConnectionString, _baseDeTest)).Flux(parentB);
        Assert.Equal(new[] { TypeChangement.Reprise, TypeChangement.Delegation }, flux.Select(e => e.Type).ToArray());
        Assert.True(flux[0].Horodatage > flux[1].Horodatage);

        // --- Then : la résolution IGNORE le journal — surcharge disparue, case retombe sur le fond Alice ---
        Assert.Empty(new MongoPeriodeRepository(ConnectionString, _baseDeTest).AllSnapshots());
        Assert.Equal(parentA, CaseDuJour(GrilleNeuve(), Mercredi_08_07_2026).ResponsableId);
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
