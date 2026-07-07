using MongoDB.Bson;
using MongoDB.Driver;
using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;

namespace PlanningDeGarde.Api.Tests;

/// <summary>
/// Sprint 28 — S4 — Réponse NEUTRE anti-énumération sur un email inconnu (@back, preuve runtime réelle :
/// store de jetons Mongo durable + canal mail SMTP réel, Docker). Une demande de récupération sur un email
/// qu'aucun compte ne porte renvoie le <b>MÊME succès neutre</b> qu'un email connu, mais ne déclenche
/// AUCUN effet : aucun mail capté par Smtp4dev, aucun jeton écrit au store durable. Le contraste avec le
/// chemin « email connu » (qui, lui, émet mail + jeton) prouve que la réponse ne fuit pas l'existence.
/// Aucune doublure sur le chemin observé (R4). Base Mongo isolée par exécution ; mailbox partagée →
/// collection SMTP sérialisée. <b>Skip propre</b> si Mongo ou Smtp4dev est indisponible.
/// </summary>
[Collection("Smtp4dev")]
public sealed class RecuperationEmailInconnuRuntimeTests : IDisposable
{
    private const string ConnectionString = "mongodb://localhost:27017";
    private const string EmailConnu = "papa@foyer.fr";
    private const string EmailInconnu = "inconnu@foyer.fr";
    private static readonly DateTime Maintenant = new(2026, 7, 2, 12, 0, 0, DateTimeKind.Utc);
    private readonly string _baseDeTest = $"planning_test_{Guid.NewGuid():N}";

    private sealed class HorlogeFigee(DateTime maintenant) : IDateTimeProvider
    {
        public DateTime Maintenant { get; set; } = maintenant;
        public DateOnly Aujourdhui => DateOnly.FromDateTime(Maintenant);
    }

    private long CompterJetons() => new MongoClient(ConnectionString).GetDatabase(_baseDeTest)
        .GetCollection<BsonDocument>("jetons_reset")
        .CountDocuments(Builders<BsonDocument>.Filter.Empty);

    [MongoEtSmtpRequisFact]
    public async Task Acceptation_Should_Repondre_le_meme_succes_neutre_sans_mail_ni_jeton_When_l_email_est_inconnu_alors_qu_un_email_connu_emet_mail_et_jeton()
    {
        await SmtpDev.ViderMessages();

        var comptes = new ReferentielComptesMongo(ConnectionString, _baseDeTest);
        comptes.Creer("compte-papa", EmailConnu, StatutCompte.Actif, "acteur-papa");
        var jetons = new ReferentielJetonsResetMongo(ConnectionString, _baseDeTest);
        var mail = new EnvoiMailSmtp(SmtpDev.SmtpHote, SmtpDev.SmtpPort, "no-reply@planning-de-garde.fr");
        var handler = new DemanderRecuperationMotDePasseHandler(comptes, mail, jetons, new HorlogeFigee(Maintenant));

        // Email INCONNU d'abord : aucun effet attendu.
        var reponseInconnu = handler.Handle(new DemanderRecuperationMotDePasseCommand(EmailInconnu));
        Assert.True(reponseInconnu.EstSucces);                 // succès neutre
        Assert.Equal(0, CompterJetons());                      // aucun jeton écrit au store durable
        Assert.Equal(0, await SmtpDev.NombreDeMessages());     // aucun mail capté

        // Email CONNU : émet bien mail + jeton (contraste prouvant que l'absence ci-dessus est significative).
        var reponseConnu = handler.Handle(new DemanderRecuperationMotDePasseCommand(EmailConnu));
        Assert.True(reponseConnu.EstSucces);
        Assert.Equal(1, CompterJetons());                      // un jeton émis pour le compte connu
        Assert.NotNull(await SmtpDev.TrouverSourceMailPour(EmailConnu)); // un mail capté pour le compte connu

        // Anti-énumération : la réponse au client est STRICTEMENT identique dans les deux cas.
        Assert.Equal(reponseConnu.EstSucces, reponseInconnu.EstSucces);
        Assert.Equal(reponseConnu.Motif, reponseInconnu.Motif);
        Assert.Equal(reponseConnu.Valeur, reponseInconnu.Valeur);
    }

    public void Dispose()
    {
        try { new MongoClient(ConnectionString).DropDatabase(_baseDeTest); }
        catch { /* best effort : Mongo injoignable au teardown, rien à nettoyer. */ }
    }
}
