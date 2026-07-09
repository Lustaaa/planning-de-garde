using MongoDB.Driver;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Infrastructure;

namespace PlanningDeGarde.Api.Tests;

/// <summary>
/// Sprint 30 — S8 — Migration de rétro-affectation idempotente des slots du fantôme, prouvée sur
/// <b>store Mongo RÉEL</b> (Docker, jamais une doublure : une doublure « mentirait au vert »). Des slots
/// existants (ponctuel ET récurrent) portent l'<c>EnfantId</c> fantôme « Léa » (le prénom, transmis par
/// la Session, jamais choisi) ; un enfant réel « Léa » est présent au référentiel (id opaque). La
/// migration réattache ces slots à l'identifiant stable de l'enfant réel ; rejouée, elle réussit en no-op.
///
/// <b>Skip propre</b> (<see cref="MongoRequisFactAttribute"/>) si Docker / Mongo est indisponible. Base
/// Mongo isolée par exécution (Guid), supprimée en fin de test.
/// </summary>
public sealed class MigrationRetroAffectationEnfantsMongoTests : IDisposable
{
    private const string ConnectionString = "mongodb://localhost:27017";
    private readonly string _baseDeTest = $"planning_migr_enfant_{Guid.NewGuid():N}";

    private sealed class NotificateurMuet : INotificateurPlanning
    {
        public void NotifierMiseAJour() { }
    }

    [MongoRequisFact]
    public void Acceptation_Should_Reattacher_les_slots_fantomes_a_l_enfant_reel_puis_no_op_au_rejeu_When_la_migration_est_executee_sur_Mongo_reel()
    {
        var debut = new DateTime(2026, 6, 10, 8, 0, 0);
        var fin = new DateTime(2026, 6, 10, 17, 0, 0);

        // Given — des slots existants portent le fantôme « Léa » (EnfantId = prénom), écrits DIRECTEMENT
        // sur le store durable (ils précèdent le hissage : jamais validés par le handler de pose).
        new MongoSlotRepository(ConnectionString, _baseDeTest)
            .Enregistrer(SlotDeLocalisation.Poser("Léa", "école", debut, fin).Valeur!);
        new MongoSlotRecurrentRepository(ConnectionString, _baseDeTest)
            .Enregistrer(SlotRecurrent.Poser("Léa", "piscine", DayOfWeek.Saturday, new TimeSpan(11, 30, 0), new TimeSpan(12, 15, 0)).Valeur!);

        // And — un enfant réel « Léa » présent au référentiel (identifiant stable OPAQUE via le handler).
        var enfants = new ReferentielEnfantsMongo(ConnectionString, _baseDeTest);
        var leaId = new AjouterEnfantHandler(enfants, enfants, new NotificateurMuet())
            .Handle(new AjouterEnfantCommand("Léa")).Valeur!.EnfantId;
        Assert.NotEqual("Léa", leaId); // id opaque, jamais le prénom fantôme

        // When — la migration de rétro-affectation est exécutée.
        var migration = new MigrationRetroAffectationEnfantsMongo(ConnectionString, _baseDeTest);
        var reattaches = migration.Executer(enfants);

        // Then — chaque slot du fantôme (ponctuel + récurrent) est réattaché à l'id stable de l'enfant réel.
        Assert.Equal(2, reattaches);
        Assert.All(new MongoSlotRepository(ConnectionString, _baseDeTest).AllSnapshots(),
            s => Assert.Equal(leaId, s.EnfantId));
        Assert.All(new MongoSlotRecurrentRepository(ConnectionString, _baseDeTest).AllSnapshots(),
            s => Assert.Equal(leaId, s.EnfantId));

        // When — la migration est rejouée sur le même store.
        var rejeu = migration.Executer(enfants);

        // Then — no-op idempotent : rien de réattaché, aucun double rattachement, aucune erreur.
        Assert.Equal(0, rejeu);
        Assert.All(new MongoSlotRepository(ConnectionString, _baseDeTest).AllSnapshots(),
            s => Assert.Equal(leaId, s.EnfantId));
        Assert.All(new MongoSlotRecurrentRepository(ConnectionString, _baseDeTest).AllSnapshots(),
            s => Assert.Equal(leaId, s.EnfantId));
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
