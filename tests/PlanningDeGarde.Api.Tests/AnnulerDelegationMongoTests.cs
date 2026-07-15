using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Infrastructure;

namespace PlanningDeGarde.Api.Tests;

/// <summary>
/// Sprint 46 — Acceptation d'<b>intégration sur Mongo RÉEL</b> (conteneur Docker), 2ᵉ des deux adaptateurs
/// (InMemory prouvé côté PlanningDeGarde.Tests). « Reprendre ce jour » (AnnulerDelegation) COMPOSE la
/// SUPPRESSION de surcharge existante (s16) : après redémarrage (nouvelles instances de stores), la case
/// reprise retombe sur le FOND et le transfert dérivé s31 disparaît. Écriture réelle, jamais une doublure.
/// <b>Skip propre</b> (<see cref="MongoRequisFactAttribute"/>) si Docker / Mongo est indisponible.
/// </summary>
public sealed class AnnulerDelegationMongoTests : IDisposable
{
    private const string ConnectionString = "mongodb://localhost:27017";
    private readonly string _baseDeTest = $"planning_test_{Guid.NewGuid():N}";

    private static readonly DateOnly Mercredi_08_07_2026 = new(2026, 7, 8); // ISO 28 paire → fond Parent A

    // Plage s45 (semaine ISO 28, fond Parent A) : [J1=mardi 07 .. J3=jeudi 09], J milieu = mercredi 08.
    private static readonly DateOnly Mardi_07_07_2026 = new(2026, 7, 7);
    private static readonly DateOnly Jeudi_09_07_2026 = new(2026, 7, 9);
    private static readonly DateOnly Vendredi_10_07_2026 = new(2026, 7, 10);

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
    public void Acceptation_Should_Reprendre_un_jour_delegue_ponctuel_supprime_la_surcharge_durable_et_retombe_sur_le_fond()
    {
        // --- Given : deux acteurs durables, un cycle N=2 (index 0 pair → Parent A) ---
        var config = new ConfigurationFoyerMongo(ConnectionString, _baseDeTest);
        var parentA = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Alice")).Valeur!.ActeurId;
        var parentB = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Bruno")).Valeur!.ActeurId;
        new CycleDeFondMongo(ConnectionString, _baseDeTest)
            .DefinirCycle(new CycleDeFond(2, new Dictionary<int, string> { [0] = parentA, [1] = parentB }));

        // Délégation ponctuelle (s44) posée à Bruno via Mongo réel.
        new DeleguerRecuperationHandler(GrilleNeuve(), new MongoPeriodeRepository(ConnectionString, _baseDeTest), new ConfigurationFoyerMongo(ConnectionString, _baseDeTest))
            .Handle(new DeleguerRecuperationCommand(Mercredi_08_07_2026, "enfant-lea", parentB));
        Assert.Equal(parentB, CaseDuJour(GrilleNeuve(), Mercredi_08_07_2026).ResponsableId);

        // --- When : je reprends ce jour (AnnulerDelegation) sur Mongo réel ---
        var resultat = new AnnulerDelegationHandler(new MongoPeriodeRepository(ConnectionString, _baseDeTest))
            .Handle(new AnnulerDelegationCommand(Mercredi_08_07_2026, "enfant-lea"));
        Assert.True(resultat.EstSucces);

        // --- Redémarrage : la surcharge durable a disparu du store ---
        Assert.Empty(new MongoPeriodeRepository(ConnectionString, _baseDeTest).AllSnapshots());

        // --- Then : la grille neuve résout le FOND (Parent A) pour J, transfert dérivé disparu ---
        var caseJour = CaseDuJour(GrilleNeuve(), Mercredi_08_07_2026);
        Assert.Equal(parentA, caseJour.ResponsableId);
        Assert.Equal("Alice", caseJour.NomResponsable);
        Assert.Null(caseJour.Transfert);
    }

    public void Dispose()
    {
        try { new MongoClient(ConnectionString).DropDatabase(_baseDeTest); }
        catch { /* best effort */ }
    }
}
