using System.Linq;
using MongoDB.Driver;
using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;

namespace PlanningDeGarde.Api.Tests;

/// <summary>
/// Sprint 28 — S2 — Store de jetons reset <b>durable Mongo</b> (@back, preuve runtime réelle, Docker).
/// Le port <see cref="IReferentielJetonsReset"/> est réalisé par <c>ReferentielJetonsResetMongo</c> :
///   1. <b>émission</b> — un jeton d'expiration 60 min est enregistré dans le store durable ;
///   2. <b>relecture</b> — une instance FRAÎCHE du store (= redémarrage) relit le jeton persisté ;
///   3. <b>consommation usage-unique</b> — présenté au <c>RedefinirMotDePasseHandler</c> (frontière
///      Application, hacheur PBKDF2 réel + horloge injectée), le jeton redéfinit le mot de passe (haché,
///      durable) et est consommé ; un SECOND usage du même jeton est rejeté sans aucune mutation.
/// Aucune doublure sur le chemin observé (store réel, R4). Base Mongo isolée par exécution (Guid),
/// supprimée au teardown. <b>Skip propre</b> si Docker / Mongo est indisponible.
/// </summary>
public sealed class ReferentielJetonsResetMongoTests : IDisposable
{
    private const string ConnectionString = "mongodb://localhost:27017";
    private const string CompteId = "compte-papa";
    private const string AncienMotDePasse = "ancien-papa";
    private const string NouveauMotDePasse = "nouveau-papa";
    private static readonly DateTime Maintenant = new(2026, 7, 2, 12, 0, 0, DateTimeKind.Utc);
    private readonly string _baseDeTest = $"planning_test_{Guid.NewGuid():N}";

    /// <summary>Horloge figée locale (Api.Tests ne référence pas les Fakes de PlanningDeGarde.Tests) :
    /// fige l'instant courant pour prouver l'expiration de façon déterministe (ici, jeton non expiré).</summary>
    private sealed class HorlogeFigee(DateTime maintenant) : IDateTimeProvider
    {
        public DateTime Maintenant { get; set; } = maintenant;
        public DateOnly Aujourdhui => DateOnly.FromDateTime(Maintenant);
    }

    private ReferentielJetonsResetMongo NouveauStoreJetons() => new(ConnectionString, _baseDeTest);
    private ReferentielComptesMongo NouveauStoreComptes() => new(ConnectionString, _baseDeTest);

    [MongoRequisFact]
    public void Acceptation_Should_Emettre_relire_puis_consommer_le_jeton_en_usage_unique_When_le_store_de_jetons_reset_est_durable_Mongo()
    {
        var hacheur = new HacheurMotDePassePbkdf2();
        var comptes = NouveauStoreComptes();
        comptes.Creer(CompteId, "papa@foyer.fr", StatutCompte.Actif, "acteur-papa", hacheur.Hacher(AncienMotDePasse));

        // 1. Émission — un jeton d'expiration 60 min enregistré dans le store durable.
        var jetons = NouveauStoreJetons();
        jetons.Enregistrer(new JetonReset("jeton-papa", CompteId, Maintenant.AddMinutes(60), Consomme: false));

        // 2. Relecture — une instance FRAÎCHE (redémarrage) relit le jeton persisté, non consommé.
        var relu = NouveauStoreJetons().Trouver("jeton-papa");
        Assert.NotNull(relu);
        Assert.Equal(CompteId, relu!.CompteId);
        Assert.False(relu.Consomme);

        // 3. Consommation — présenté au handler (frontière Application), sur le store durable réel.
        var horloge = new HorlogeFigee(Maintenant);
        var handler = new RedefinirMotDePasseHandler(comptes, jetons, hacheur, horloge);
        var premier = handler.Handle(new RedefinirMotDePasseCommand("jeton-papa", NouveauMotDePasse));

        Assert.True(premier.EstSucces);
        // Le mot de passe est redéfini (haché PBKDF2), durablement — relu depuis une instance fraîche.
        var compteRelu = NouveauStoreComptes().EnumererComptes().Single(c => c.Id == CompteId);
        Assert.True(hacheur.Verifier(NouveauMotDePasse, compteRelu.MotDePasseHache!));
        Assert.False(hacheur.Verifier(AncienMotDePasse, compteRelu.MotDePasseHache!));
        // Le jeton est consommé, durablement (relu depuis une instance fraîche).
        Assert.True(NouveauStoreJetons().Trouver("jeton-papa")!.Consomme);

        // Usage unique — un SECOND usage du même jeton est rejeté sans aucune mutation.
        var second = handler.Handle(new RedefinirMotDePasseCommand("jeton-papa", "encore-un-autre"));
        Assert.False(second.EstSucces);
        var compteApresSecond = NouveauStoreComptes().EnumererComptes().Single(c => c.Id == CompteId);
        Assert.True(hacheur.Verifier(NouveauMotDePasse, compteApresSecond.MotDePasseHache!)); // resté celui du 1er usage
    }

    public void Dispose()
    {
        try { new MongoClient(ConnectionString).DropDatabase(_baseDeTest); }
        catch { /* best effort : Mongo injoignable au teardown, rien à nettoyer. */ }
    }
}
