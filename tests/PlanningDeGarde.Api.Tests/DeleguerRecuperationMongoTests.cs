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

    // Plage s45 (semaine ISO 28, fond Parent A) : [J1=mardi 07 .. J2=jeudi 09], J2+1 = vendredi 10 (fond A de nouveau).
    private static readonly DateOnly Mardi_07_07_2026 = new(2026, 7, 7);
    private static readonly DateOnly Jeudi_09_07_2026 = new(2026, 7, 9);
    private static readonly DateOnly Vendredi_10_07_2026 = new(2026, 7, 10);

    // Observation de la case résolue d'un jour via la GRILLE (socle : les read models de lecture s42/s43
    // qui la composaient ont été retirés — décision PO s44 Sc.7). La semaine du jour le contient toujours.
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
    public void Acceptation_Should_Deleguer_un_jour_de_fond_ecrit_une_surcharge_durable_avec_transfert_derive()
    {
        // --- Given : deux acteurs durables, un cycle N=2 (index 0 pair → Parent A) ---
        var config = new ConfigurationFoyerMongo(ConnectionString, _baseDeTest);
        var parentA = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Alice")).Valeur!.ActeurId;
        var parentB = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Bruno")).Valeur!.ActeurId;

        var cycle = new CycleDeFondMongo(ConnectionString, _baseDeTest);
        cycle.DefinirCycle(new CycleDeFond(2, new Dictionary<int, string> { [0] = parentA, [1] = parentB }));

        // Précondition : le jour J est résolu par le fond (Parent A).
        Assert.Equal(parentA, CaseDuJour(GrilleNeuve(), Mercredi_08_07_2026).ResponsableId);

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

        // --- Then : la grille neuve résout B pour J + transfert dérivé A → B ---
        var caseJour = CaseDuJour(GrilleNeuve(), Mercredi_08_07_2026);
        Assert.Equal(parentB, caseJour.ResponsableId);
        Assert.Equal("Bruno", caseJour.NomResponsable);
        Assert.NotNull(caseJour.Transfert);
        Assert.Equal("Alice", caseJour.Transfert!.NomDepart);
        Assert.Equal("Bruno", caseJour.Transfert.NomArrivee);
    }

    [MongoRequisFact]
    public void Acceptation_Should_Deleguer_une_PLAGE_de_fond_ecrit_une_seule_surcharge_multi_jours_durable_avec_transferts_aux_frontieres()
    {
        // --- Given : deux acteurs durables, un cycle N=2 (index 0 pair → Parent A) ---
        var config = new ConfigurationFoyerMongo(ConnectionString, _baseDeTest);
        var parentA = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Alice")).Valeur!.ActeurId;
        var parentB = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Bruno")).Valeur!.ActeurId;

        var cycle = new CycleDeFondMongo(ConnectionString, _baseDeTest);
        cycle.DefinirCycle(new CycleDeFond(2, new Dictionary<int, string> { [0] = parentA, [1] = parentB }));

        // Précondition : chaque jour de la plage est résolu par le fond (Parent A).
        var grillePre = GrilleNeuve();
        foreach (var j in new[] { Mardi_07_07_2026, Mercredi_08_07_2026, Jeudi_09_07_2026 })
            Assert.Equal(parentA, CaseDuJour(grillePre, j).ResponsableId);

        // --- When : je délègue la récupération de la PLAGE [J1..J2] à Parent B (Mongo réel) ---
        var periodesEcriture = new MongoPeriodeRepository(ConnectionString, _baseDeTest);
        var resultat = new DeleguerRecuperationHandler(GrilleNeuve(), periodesEcriture, new ConfigurationFoyerMongo(ConnectionString, _baseDeTest))
            .Handle(new DeleguerRecuperationCommand(Mardi_07_07_2026, "enfant-lea", parentB, Jeudi_09_07_2026));
        Assert.True(resultat.EstSucces);

        // --- Redémarrage : UNE SEULE surcharge [J1..J2] durable, responsable Bruno ---
        var surcharge = Assert.Single(new MongoPeriodeRepository(ConnectionString, _baseDeTest).AllSnapshots());
        Assert.Equal(parentB, surcharge.ResponsableId);
        Assert.Equal(Mardi_07_07_2026, DateOnly.FromDateTime(surcharge.Debut));
        Assert.Equal(Jeudi_09_07_2026, DateOnly.FromDateTime(surcharge.Fin));

        // --- Then : la grille neuve résout B sur chaque jour + transferts dérivés aux DEUX frontières ---
        var grille = GrilleNeuve();
        foreach (var j in new[] { Mardi_07_07_2026, Mercredi_08_07_2026, Jeudi_09_07_2026 })
            Assert.Equal(parentB, CaseDuJour(grille, j).ResponsableId);
        var entree = CaseDuJour(grille, Mardi_07_07_2026);
        Assert.Equal("Alice", entree.Transfert!.NomDepart);
        Assert.Equal("Bruno", entree.Transfert.NomArrivee);
        var sortie = CaseDuJour(grille, Vendredi_10_07_2026);
        Assert.Equal("Bruno", sortie.Transfert!.NomDepart);
        Assert.Equal("Alice", sortie.Transfert.NomArrivee);
    }

    [MongoRequisFact]
    public void Acceptation_Should_Refuser_une_PLAGE_vers_un_delegataire_inconnu_avant_ecriture_store_intact()
    {
        // --- Given : un seul acteur durable (Alice), un cycle N=1 ---
        var config = new ConfigurationFoyerMongo(ConnectionString, _baseDeTest);
        var parentA = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Alice")).Valeur!.ActeurId;
        new CycleDeFondMongo(ConnectionString, _baseDeTest)
            .DefinirCycle(new CycleDeFond(1, new Dictionary<int, string> { [0] = parentA }));

        // --- When : déléguer une PLAGE vers un id ABSENT du store ---
        var orphelinId = "acteur-absent-" + Guid.NewGuid().ToString("N");
        var periodesEcriture = new MongoPeriodeRepository(ConnectionString, _baseDeTest);
        var resultat = new DeleguerRecuperationHandler(GrilleNeuve(), periodesEcriture, new ConfigurationFoyerMongo(ConnectionString, _baseDeTest))
            .Handle(new DeleguerRecuperationCommand(Mardi_07_07_2026, "enfant-lea", orphelinId, Jeudi_09_07_2026));

        // --- Then : refus AVANT écriture, store Mongo des périodes INTACT (aucun jour de la plage écrit) ---
        Assert.False(resultat.EstSucces);
        Assert.Empty(new MongoPeriodeRepository(ConnectionString, _baseDeTest).AllSnapshots());
    }

    [MongoRequisFact]
    public void Acceptation_Should_Refuser_un_delegataire_inconnu_avant_ecriture_store_intact()
    {
        // --- Given : un seul acteur durable (Alice), un cycle N=1 ---
        var config = new ConfigurationFoyerMongo(ConnectionString, _baseDeTest);
        var parentA = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Alice")).Valeur!.ActeurId;
        new CycleDeFondMongo(ConnectionString, _baseDeTest)
            .DefinirCycle(new CycleDeFond(1, new Dictionary<int, string> { [0] = parentA }));

        // --- When : déléguer vers un id ABSENT du store (inconnu / supprimé) ---
        var orphelinId = "acteur-absent-" + Guid.NewGuid().ToString("N");
        var periodesEcriture = new MongoPeriodeRepository(ConnectionString, _baseDeTest);
        var resultat = new DeleguerRecuperationHandler(GrilleNeuve(), periodesEcriture, new ConfigurationFoyerMongo(ConnectionString, _baseDeTest))
            .Handle(new DeleguerRecuperationCommand(Mercredi_08_07_2026, "enfant-lea", orphelinId));

        // --- Then : refus AVANT écriture, store Mongo des périodes INTACT ---
        Assert.False(resultat.EstSucces);
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
            // Best effort : si Mongo est injoignable au teardown, rien à nettoyer.
        }
    }
}
