using System.Linq;
using MongoDB.Driver;
using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;

namespace PlanningDeGarde.Api.Tests;

/// <summary>
/// Sprint 28 — S3 — Jeton expiré (> 60 min) rejeté <b>sans mutation</b>, sur store <b>Mongo réel</b>
/// (@back, Docker actif, horloge injectée). Confirme, contre le store durable, la garde d'expiration
/// portée par <c>RedefinirMotDePasseHandler</c> (introduite s25) : un jeton émis avec une expiration à
/// 60 minutes, présenté alors que l'horloge injectée est positionnée <b>61 minutes après l'émission</b>,
/// est refusé (motif clair) — le mot de passe antérieur reste inchangé et le jeton n'est pas consommé.
/// Prouve que le round-trip durable de l'instant d'expiration (UTC) est correctement comparé à l'horloge
/// injectée (aucune doublure du store, R4). Base Mongo isolée par exécution (Guid), supprimée au teardown.
/// <b>Skip propre</b> si Docker / Mongo est indisponible.
/// </summary>
public sealed class JetonResetExpireMongoTests : IDisposable
{
    private const string ConnectionString = "mongodb://localhost:27017";
    private const string CompteId = "compte-papa";
    private const string AncienMotDePasse = "ancien-papa";
    private static readonly DateTime Emission = new(2026, 7, 2, 12, 0, 0, DateTimeKind.Utc);
    private readonly string _baseDeTest = $"planning_test_{Guid.NewGuid():N}";

    /// <summary>Horloge figée locale : positionne l'instant courant pour prouver l'expiration de façon
    /// déterministe, sans attendre le temps réel.</summary>
    private sealed class HorlogeFigee(DateTime maintenant) : IDateTimeProvider
    {
        public DateTime Maintenant { get; set; } = maintenant;
        public DateOnly Aujourdhui => DateOnly.FromDateTime(Maintenant);
    }

    private ReferentielJetonsResetMongo NouveauStoreJetons() => new(ConnectionString, _baseDeTest);
    private ReferentielComptesMongo NouveauStoreComptes() => new(ConnectionString, _baseDeTest);

    [MongoRequisFact]
    public void Acceptation_Should_Rejeter_sans_aucune_mutation_When_le_jeton_est_expire_au_dela_de_60_minutes_sur_le_store_Mongo_reel()
    {
        var hacheur = new HacheurMotDePassePbkdf2();
        var comptes = NouveauStoreComptes();
        comptes.Creer(CompteId, "papa@foyer.fr", StatutCompte.Actif, "acteur-papa", hacheur.Hacher(AncienMotDePasse));

        // Émission d'un jeton d'expiration 60 minutes après l'émission, dans le store durable.
        var jetons = NouveauStoreJetons();
        jetons.Enregistrer(new JetonReset("jeton-expire", CompteId, Emission.AddMinutes(60), Consomme: false));

        // L'horloge injectée est positionnée 61 minutes après l'émission → le jeton a expiré.
        var horloge = new HorlogeFigee(Emission.AddMinutes(61));
        var handler = new RedefinirMotDePasseHandler(comptes, jetons, hacheur, horloge);

        var resultat = handler.Handle(new RedefinirMotDePasseCommand("jeton-expire", "nouveau-papa"));

        // Rejet avec motif clair.
        Assert.False(resultat.EstSucces);
        Assert.False(string.IsNullOrWhiteSpace(resultat.Motif));

        // Aucune mutation du compte : le mot de passe antérieur reste actif (relu depuis instance fraîche).
        var compteRelu = NouveauStoreComptes().EnumererComptes().Single(c => c.Id == CompteId);
        Assert.True(hacheur.Verifier(AncienMotDePasse, compteRelu.MotDePasseHache!));

        // Le jeton expiré n'est pas consommé : il reste présent, non consommé, dans le store durable.
        var jetonRelu = NouveauStoreJetons().Trouver("jeton-expire");
        Assert.NotNull(jetonRelu);
        Assert.False(jetonRelu!.Consomme);
    }

    public void Dispose()
    {
        try { new MongoClient(ConnectionString).DropDatabase(_baseDeTest); }
        catch { /* best effort : Mongo injoignable au teardown, rien à nettoyer. */ }
    }
}
