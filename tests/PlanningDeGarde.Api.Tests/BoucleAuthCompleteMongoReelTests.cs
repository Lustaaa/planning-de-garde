using MongoDB.Driver;
using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;

namespace PlanningDeGarde.Api.Tests;

/// <summary>
/// Sprint 24 — Sc.4 — Boucle auth complète de bout en bout, sur <b>Mongo RÉEL</b> (conteneur Docker,
/// jamais une doublure : R4 — le rempart anti vert-qui-ment). Assemble les use cases s22/s23/s24 câblés
/// sur le <b>même store durable</b> :
///   1. un acteur déclaré + un compte créé pour lui (naît « Inactif », s22) ;
///   2. connexion tentée par l'email → <b>refusée</b> (compte Inactif, motif clair, aucune session — s23 Sc.3) ;
///   3. activation du compte (Inactif→Actif, s24) ;
///   4. nouvelle tentative de connexion par le même email → <b>réussit</b>, session ouverte dont
///      l'identité effective = l'acteur lié 1-1 au compte (s23 Sc.1).
/// Prouve que l'activation DÉBLOQUE réellement la connexion sur données persistées (le nouveau statut
/// est relu par le handler de connexion via le store Mongo réel, pas un cache de doublure).
///
/// <b>Skip propre</b> (<see cref="MongoRequisFactAttribute"/>) si Docker / Mongo est indisponible.
/// Base Mongo isolée par exécution (Guid), supprimée en fin de test.
/// </summary>
public sealed class BoucleAuthCompleteMongoReelTests : IDisposable
{
    private const string ConnectionString = "mongodb://localhost:27017";
    private const string Email = "alice@foyer.fr";
    private readonly string _baseDeTest = $"planning_test_{Guid.NewGuid():N}";

    private ReferentielComptesMongo NouveauStoreComptes() => new(ConnectionString, _baseDeTest);
    private ConfigurationFoyerMongo NouvelleConfig() => new(ConnectionString, _baseDeTest);

    [MongoRequisFact]
    public void Acceptation_Should_Refuser_la_connexion_tant_que_Inactif_puis_la_reussir_apres_activation_When_la_boucle_auth_complete_s_execute_sur_Mongo_reel()
    {
        var config = NouvelleConfig();
        var comptes = NouveauStoreComptes();

        // 1. Un acteur déclaré + un compte créé pour lui (naît « Inactif », s22).
        var acteurId = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Alice", "rose")).Valeur!.ActeurId;
        var compteId = new CreerCompteHandler(comptes, comptes, config).Handle(new CreerCompteCommand(Email, acteurId)).Valeur!.CompteId;

        // 2. Connexion tentée AVANT activation → refusée (compte Inactif, motif clair, aucune session).
        var connexionAvant = new SeConnecterHandler(comptes, new HacheurMotDePassePbkdf2()).Handle(new SeConnecterCommand(Email));
        Assert.False(connexionAvant.EstSucces);
        Assert.False(string.IsNullOrWhiteSpace(connexionAvant.Motif));

        // 3. Activation du compte (Inactif→Actif) sur le store réel.
        var activation = new ActiverCompteHandler(comptes, comptes).Handle(new ActiverCompteCommand(compteId));
        Assert.True(activation.EstSucces);

        // 4. Nouvelle tentative par le même email → réussit, session ouverte, identité effective = l'acteur lié.
        var connexionApres = new SeConnecterHandler(comptes, new HacheurMotDePassePbkdf2()).Handle(new SeConnecterCommand(Email));
        Assert.True(connexionApres.EstSucces);
        Assert.Equal(acteurId, connexionApres.Valeur!.IdentiteEffective);
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
