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

    [MongoRequisFact]
    public void Sc4_Deux_delegations_le_meme_jour_coexistent_durable()
    {
        var config = new ConfigurationFoyerMongo(ConnectionString, _baseDeTest);
        var alice = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Alice")).Valeur!.ActeurId;
        var bob = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Bob")).Valeur!.ActeurId;
        var carla = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Carla")).Valeur!.ActeurId;
        var david = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("David")).Valeur!.ActeurId;

        new CycleDeFondMongo(ConnectionString, _baseDeTest)
            .DefinirCycle(new CycleDeFond(2, new Dictionary<int, string> { [0] = alice, [1] = bob }));

        var delegation = new DeleguerRecuperationHandler(
            GrilleNeuve(), new MongoPeriodeRepository(ConnectionString, _baseDeTest), new ConfigurationFoyerMongo(ConnectionString, _baseDeTest));

        Assert.True(delegation.Handle(new DeleguerRecuperationCommand(J, LeaId, carla)).EstSucces);
        Assert.True(delegation.Handle(new DeleguerRecuperationCommand(J, TomId, david)).EstSucces);

        // Redémarrage : deux surcharges durables coexistent, aucune n'écrase l'autre.
        var relues = new MongoPeriodeRepository(ConnectionString, _baseDeTest).AllSnapshots();
        Assert.Equal(2, relues.Count);
        Assert.Equal(carla, Assert.Single(relues, p => p.EnfantId == LeaId).ResponsableId);
        Assert.Equal(david, Assert.Single(relues, p => p.EnfantId == TomId).ResponsableId);

        var grille = GrilleNeuve();
        Assert.Equal(carla, Case(grille, LeaId).ResponsableId);
        Assert.Equal(david, Case(grille, TomId).ResponsableId);
    }

    [MongoRequisFact]
    public void Sc5_Digest_resolu_par_enfant_durable_et_pur()
    {
        var config = new ConfigurationFoyerMongo(ConnectionString, _baseDeTest);
        var alice = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Alice")).Valeur!.ActeurId;
        var bob = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Bob")).Valeur!.ActeurId;
        var carla = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Carla")).Valeur!.ActeurId;

        new CycleDeFondMongo(ConnectionString, _baseDeTest)
            .DefinirCycle(new CycleDeFond(2, new Dictionary<int, string> { [0] = alice, [1] = bob }));

        // Léa surchargée aujourd'hui (Carla), Tom résolu par le fond (Alice).
        new MongoPeriodeRepository(ConnectionString, _baseDeTest).Enregistrer(
            PeriodeDeGarde.Affecter(carla, J.ToDateTime(TimeOnly.MinValue), J.ToDateTime(TimeOnly.MinValue), LeaId).Valeur!);

        var avant = new MongoPeriodeRepository(ConnectionString, _baseDeTest).AllSnapshots().Count;
        var query = new DigestImmediatQuery(GrilleNeuve());

        var digestLea = query.Composer(J, J, LeaId);
        var digestTom = query.Composer(J, J, TomId);

        Assert.Equal(carla, digestLea.Immediat!.Responsable.ActeurId);
        Assert.Equal(alice, digestTom.Immediat!.Responsable.ActeurId);
        Assert.DoesNotContain(digestTom.AVenir, j => j.Transfert!.CedantNom == "Carla" || j.Transfert.RecevantNom == "Carla");

        // Query PURE : store durable inchangé.
        Assert.Equal(avant, new MongoPeriodeRepository(ConnectionString, _baseDeTest).AllSnapshots().Count);
    }

    [MongoRequisFact]
    public void Sc6_Orphelin_dun_enfant_laisse_lautre_intact_durable_sans_crash()
    {
        var config = new ConfigurationFoyerMongo(ConnectionString, _baseDeTest);
        var david = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("David")).Valeur!.ActeurId;
        var carlaOrpheline = "acteur-absent-" + Guid.NewGuid().ToString("N");

        // Léa orpheline : son responsable n'existe pas (jamais ajouté) — surcharge orpheline durable (Resolvable).
        var periodes = new MongoPeriodeRepository(ConnectionString, _baseDeTest);
        periodes.Enregistrer(PeriodeDeGarde.Affecter(carlaOrpheline, J.ToDateTime(TimeOnly.MinValue), J.ToDateTime(TimeOnly.MinValue), LeaId).Valeur!);
        periodes.Enregistrer(PeriodeDeGarde.Affecter(david, J.ToDateTime(TimeOnly.MinValue), J.ToDateTime(TimeOnly.MinValue), TomId).Valeur!);

        // Redémarrage : store durable relu.
        var grille = GrilleNeuve();

        // L'autre enfant (Tom) intact : David, surcharge durable préservée.
        var caseTom = Case(grille, TomId);
        Assert.Equal(david, caseTom.ResponsableId);
        Assert.Equal(david, Assert.Single(new MongoPeriodeRepository(ConnectionString, _baseDeTest).AllSnapshots(), p => p.EnfantId == TomId).ResponsableId);

        // Enfant orphelin : lecture sans crash, repli neutre (responsable absent, aucun fond).
        var configLecture = new ConfigurationFoyerMongo(ConnectionString, _baseDeTest);
        var caseLea = Case(grille, LeaId);
        Assert.Null(caseLea.ResponsableId);
        Assert.Equal("", caseLea.NomResponsable);
        Assert.Equal(configLecture.CouleurNeutre, caseLea.CouleurResponsable);
    }

    [MongoRequisFact]
    public void Sc10_Affecter_periode_en_vue_Lea_visible_de_Lea_seul_durable()
    {
        var config = new ConfigurationFoyerMongo(ConnectionString, _baseDeTest);
        var alice = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Alice")).Valeur!.ActeurId;
        var bob = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Bob")).Valeur!.ActeurId;
        var carla = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Carla")).Valeur!.ActeurId;

        new CycleDeFondMongo(ConnectionString, _baseDeTest)
            .DefinirCycle(new CycleDeFond(2, new Dictionary<int, string> { [0] = alice, [1] = bob }));

        // When — affecter une période (Carla) le jour J EN VUE de Léa via le use case d'écriture RÉEL (Mongo).
        var resultat = new AffecterPeriodeHandler(
                new MongoPeriodeRepository(ConnectionString, _baseDeTest), new FoyerResponsableRepository())
            .Handle(new AffecterPeriodeCommand(carla, J.ToDateTime(TimeOnly.MinValue), J.ToDateTime(TimeOnly.MinValue), LeaId));
        Assert.True(resultat.EstSucces);

        // Then — redémarrage : la période durable porte EnfantId = Léa (jamais le bucket partagé "").
        Assert.Equal(LeaId, Assert.Single(new MongoPeriodeRepository(ConnectionString, _baseDeTest).AllSnapshots()).EnfantId);

        // Then — grille neuve : Carla prime pour Léa ; Tom résout SON fond (Alice), jamais Carla.
        var grille = GrilleNeuve();
        Assert.Equal(carla, Case(grille, LeaId).ResponsableId);
        Assert.Equal(alice, Case(grille, TomId).ResponsableId);
        Assert.DoesNotContain(grille.Projeter(J, VuePlanning.Semaine, TomId).Jours, j => j.ResponsableId == carla);
    }

    [MongoRequisFact]
    public void Sc12_Transfert_saisi_en_vue_Lea_visible_de_Lea_seul_durable()
    {
        var config = new ConfigurationFoyerMongo(ConnectionString, _baseDeTest);
        var alice = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Alice")).Valeur!.ActeurId;
        var bob = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Bob")).Valeur!.ActeurId;

        // When — définir un transfert saisi EN VUE de Léa via le use case d'écriture RÉEL (Mongo durable).
        new DefinirTransfertHandler(new MongoTransfertRepository(ConnectionString, _baseDeTest))
            .Handle(new DefinirTransfertCommand(alice, bob, "ecole", new TimeSpan(8, 30, 0), J.ToDateTime(TimeOnly.MinValue), LeaId));

        // Then — redémarrage : le transfert durable porte EnfantId = Léa (jamais partagé "").
        Assert.Equal(LeaId, Assert.Single(new MongoTransfertRepository(ConnectionString, _baseDeTest).AllSnapshots()).EnfantId);

        // Then — grille neuve : la case J de Léa porte le transfert bicolore ; celle de Tom NON (aucune fuite).
        var grille = GrilleNeuve();
        Assert.NotNull(Case(grille, LeaId).Transfert);
        Assert.Null(Case(grille, TomId).Transfert);
    }

    [MongoRequisFact]
    public void Sc14_Editer_le_cycle_de_Lea_ne_change_pas_celui_de_Tom_durable()
    {
        var config = new ConfigurationFoyerMongo(ConnectionString, _baseDeTest);
        var alice = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Alice")).Valeur!.ActeurId;
        var bob = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Bob")).Valeur!.ActeurId;
        var carla = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Carla")).Valeur!.ActeurId;

        var handler = new DefinirCycleHandler(new CycleDeFondMongo(ConnectionString, _baseDeTest), new FakeNotificateurPlanningApi());
        handler.Handle(new DefinirCycleCommand(1, new Dictionary<int, string> { [0] = alice }, LeaId));
        handler.Handle(new DefinirCycleCommand(1, new Dictionary<int, string> { [0] = bob }, TomId));

        // When — ré-éditer le cycle de Léa (Alice → Carla) EN VUE de Léa (Mongo réel).
        handler.Handle(new DefinirCycleCommand(1, new Dictionary<int, string> { [0] = carla }, LeaId));

        // Then — redémarrage : Léa résout Carla ; Tom reste STRICTEMENT sur Bob.
        var grille = GrilleNeuve();
        Assert.Equal(carla, grille.Projeter(J, VuePlanning.Semaine, LeaId).Jours.Single(j => j.Date == J).ResponsableId);
        Assert.Equal(bob, grille.Projeter(J, VuePlanning.Semaine, TomId).Jours.Single(j => j.Date == J).ResponsableId);
    }

    /// <summary>Doublure à la main du notificateur (le use case cycle diffuse sur succès ; ici on ne teste pas la diffusion).</summary>
    private sealed class FakeNotificateurPlanningApi : INotificateurPlanning
    {
        public void NotifierMiseAJour() { }
    }

    public void Dispose()
    {
        try { new MongoClient(ConnectionString).DropDatabase(_baseDeTest); }
        catch { /* best effort */ }
    }
}
