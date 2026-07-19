using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Infrastructure;

namespace PlanningDeGarde.Api.Tests;

/// <summary>
/// Sprint 53 — Isolation multi-enfants prouvée sur <b>Mongo RÉEL</b> (2ᵉ adaptateur). Une écriture ciblée
/// enfant A (délégation / échange) ne touche JAMAIS le store des surcharges ni la résolution de l'enfant B ;
/// deux enfants le même jour = deux surcharges qui COEXISTENT (pas de last-write-wins entre enfants).
///
/// <b>Skip propre</b> (<see cref="MongoRequisFactAttribute"/>) si Docker / Mongo est indisponible.
/// </summary>
public sealed class Scenario53_MultiEnfantsMongoTests : IDisposable
{
    private const string ConnectionString = "mongodb://localhost:27017";
    private readonly string _baseDeTest = $"planning_test_{Guid.NewGuid():N}";

    private const string LeaId = "enfant-lea";
    private const string TomId = "enfant-tom";

    private static readonly DateOnly J = new(2026, 7, 8); // mercredi, ISO 28 → index 0 → fond parentA

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

    private static JourCase Case(GrilleAgendaQuery grille, string enfantId)
        => grille.Projeter(J, VuePlanning.Semaine, enfantId).Jours.Single(j => j.Date == J);

    [MongoRequisFact]
    public void Sc2_Deleguer_Lea_laisse_la_surcharge_de_Tom_intacte_durable()
    {
        var config = new ConfigurationFoyerMongo(ConnectionString, _baseDeTest);
        var alice = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Alice")).Valeur!.ActeurId;
        var bob = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Bob")).Valeur!.ActeurId;
        var carla = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Carla")).Valeur!.ActeurId;

        new CycleDeFondMongo(ConnectionString, _baseDeTest)
            .DefinirCycle(new CycleDeFond(2, new Dictionary<int, string> { [0] = alice, [1] = bob }));

        // Given — Tom a sa propre surcharge (Carla) le jour J, durable.
        new MongoPeriodeRepository(ConnectionString, _baseDeTest).Enregistrer(
            PeriodeDeGarde.Affecter(carla, J.ToDateTime(TimeOnly.MinValue), J.ToDateTime(TimeOnly.MinValue), TomId).Valeur!);

        // When — je délègue la récupération de Léa le jour J à Bob (Mongo réel).
        var resultat = new DeleguerRecuperationHandler(
                GrilleNeuve(), new MongoPeriodeRepository(ConnectionString, _baseDeTest), new ConfigurationFoyerMongo(ConnectionString, _baseDeTest))
            .Handle(new DeleguerRecuperationCommand(J, LeaId, bob));
        Assert.True(resultat.EstSucces);

        // Then — redémarrage : DEUX surcharges durables coexistent, scope enfant respecté.
        var relues = new MongoPeriodeRepository(ConnectionString, _baseDeTest).AllSnapshots();
        Assert.Equal(2, relues.Count);
        Assert.Equal(bob, Assert.Single(relues, p => p.EnfantId == LeaId).ResponsableId);
        Assert.Equal(carla, Assert.Single(relues, p => p.EnfantId == TomId).ResponsableId);

        // Then — grille neuve : Léa prime Bob (+ transfert dérivé Alice→Bob) ; Tom reste sur Carla, inchangé.
        var grille = GrilleNeuve();
        var caseLea = Case(grille, LeaId);
        Assert.Equal(bob, caseLea.ResponsableId);
        Assert.Equal("Alice", caseLea.Transfert!.NomDepart);
        Assert.Equal("Bob", caseLea.Transfert.NomArrivee);
        Assert.Equal(carla, Case(grille, TomId).ResponsableId);
    }

    [MongoRequisFact]
    public void Sc3_Echange_Lea_accepte_compose_delegation_isolee_de_Tom_durable()
    {
        var config = new ConfigurationFoyerMongo(ConnectionString, _baseDeTest);
        var alice = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Alice")).Valeur!.ActeurId;
        var bob = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Bob")).Valeur!.ActeurId;
        var carla = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Carla")).Valeur!.ActeurId;

        new CycleDeFondMongo(ConnectionString, _baseDeTest)
            .DefinirCycle(new CycleDeFond(2, new Dictionary<int, string> { [0] = alice, [1] = bob }));

        // Tom a sa surcharge (Carla) le jour J.
        new MongoPeriodeRepository(ConnectionString, _baseDeTest).Enregistrer(
            PeriodeDeGarde.Affecter(carla, J.ToDateTime(TimeOnly.MinValue), J.ToDateTime(TimeOnly.MinValue), TomId).Valeur!);

        var propositions = new MongoPropositionEchangeRepository(ConnectionString, _baseDeTest);
        var delegation = new DeleguerRecuperationHandler(
            GrilleNeuve(), new MongoPeriodeRepository(ConnectionString, _baseDeTest), new ConfigurationFoyerMongo(ConnectionString, _baseDeTest));

        var proposition = new ProposerEchangeHandler(GrilleNeuve(), propositions, new ConfigurationFoyerMongo(ConnectionString, _baseDeTest))
            .Handle(new ProposerEchangeCommand(J, LeaId, bob)).Valeur!;
        Assert.Equal(alice, proposition.DeActeurId); // cédant résolu de Léa, isolé de Tom

        Assert.True(new AccepterPropositionHandler(propositions, delegation)
            .Handle(new AccepterPropositionCommand(proposition.Id)).EstSucces);

        // Redémarrage : 2 surcharges durables coexistent, scope enfant respecté.
        var relues = new MongoPeriodeRepository(ConnectionString, _baseDeTest).AllSnapshots();
        Assert.Equal(2, relues.Count);
        Assert.Equal(bob, Assert.Single(relues, p => p.EnfantId == LeaId).ResponsableId);
        Assert.Equal(carla, Assert.Single(relues, p => p.EnfantId == TomId).ResponsableId);

        var grille = GrilleNeuve();
        Assert.Equal(bob, Case(grille, LeaId).ResponsableId);
        Assert.Equal(carla, Case(grille, TomId).ResponsableId);
    }

    public void Dispose()
    {
        try { new MongoClient(ConnectionString).DropDatabase(_baseDeTest); }
        catch { /* best effort */ }
    }
}
