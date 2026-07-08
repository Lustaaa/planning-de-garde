using MongoDB.Driver;
using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;

namespace PlanningDeGarde.Api.Tests;

/// <summary>
/// Sprint 28 — S7 — Poser un mot de passe sur un compte → login email + mot de passe (@back, frontière
/// Application, store <b>Mongo réel</b>, Docker). Sur un compte Actif email-only (s23), poser un mot de
/// passe (<see cref="DefinirMotDePasseHandler"/>, haché PBKDF2 via le port
/// <see cref="IEditeurComptes.RedefinirMotDePasse"/>) le rend connectable par « email + mot de passe » :
///   - le BON couple ouvre une session dont l'identité réelle est l'acteur du compte ;
///   - le MAUVAIS couple est refusé avec un motif NEUTRE, <b>identique</b> à celui d'un email inconnu
///     (anti-énumération) ;
///   - un compte SANS mot de passe reste connectable email-only (s23 non régressé).
/// Aucune doublure sur le chemin observé (store + hacheur réels, R4). Base Mongo isolée par exécution.
/// <b>Skip propre</b> si Docker / Mongo est indisponible.
/// </summary>
public sealed class DefinirMotDePasseSeConnecterMongoTests : IDisposable
{
    private const string ConnectionString = "mongodb://localhost:27017";
    private const string EmailMaman = "maman@foyer.fr";
    private const string ActeurMaman = "acteur-maman";
    private const string EmailPapa = "papa@foyer.fr"; // reste email-only (non-régression s23)
    private const string BonMotDePasse = "bon-secret-maman";
    private const string MauvaisMotDePasse = "mauvais-essai";
    private readonly string _baseDeTest = $"planning_test_{Guid.NewGuid():N}";

    private ReferentielComptesMongo NouveauStoreComptes() => new(ConnectionString, _baseDeTest);

    [MongoRequisFact]
    public void Acceptation_Should_Permettre_le_bon_couple_refuser_neutre_le_mauvais_et_laisser_l_email_only_When_un_mot_de_passe_est_pose_sur_un_compte_sur_Mongo_reel()
    {
        var hacheur = new HacheurMotDePassePbkdf2();
        var comptes = NouveauStoreComptes();
        // Deux comptes Actifs email-only (aucun mot de passe posé au départ).
        comptes.Creer("compte-maman", EmailMaman, StatutCompte.Actif, ActeurMaman);
        comptes.Creer("compte-papa", EmailPapa, StatutCompte.Actif, "acteur-papa");

        // When — un mot de passe est posé sur le compte de maman (haché, via le port d'écriture).
        var definition = new DefinirMotDePasseHandler(comptes, hacheur)
            .Handle(new DefinirMotDePasseCommand("compte-maman", BonMotDePasse));
        Assert.True(definition.EstSucces);

        // Relecture depuis une instance FRAÎCHE (le condensat est durable), puis connexion réelle.
        var connexion = new SeConnecterHandler(NouveauStoreComptes(), hacheur);

        // Then #1 — bon couple : session ouverte, identité réelle = l'acteur du compte.
        var bonCouple = connexion.Handle(new SeConnecterCommand(EmailMaman, BonMotDePasse));
        Assert.True(bonCouple.EstSucces);
        Assert.Equal(ActeurMaman, bonCouple.Valeur!.IdentiteReelle);

        // Then #2 — mauvais couple : refus avec motif NEUTRE, IDENTIQUE à celui d'un email inconnu.
        var mauvaisCouple = connexion.Handle(new SeConnecterCommand(EmailMaman, MauvaisMotDePasse));
        var emailInconnu = connexion.Handle(new SeConnecterCommand("inconnu@foyer.fr", "peu-importe"));
        Assert.False(mauvaisCouple.EstSucces);
        Assert.False(emailInconnu.EstSucces);
        Assert.Equal(emailInconnu.Motif, mauvaisCouple.Motif); // même motif neutre (anti-énumération)

        // Then #3 — le compte SANS mot de passe reste connectable email-only (s23 non régressé).
        var papaEmailOnly = connexion.Handle(new SeConnecterCommand(EmailPapa));
        Assert.True(papaEmailOnly.EstSucces);
        Assert.Equal("acteur-papa", papaEmailOnly.Valeur!.IdentiteReelle);
    }

    public void Dispose()
    {
        try { new MongoClient(ConnectionString).DropDatabase(_baseDeTest); }
        catch { /* best effort : Mongo injoignable au teardown, rien à nettoyer. */ }
    }
}
