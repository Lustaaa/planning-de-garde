using MongoDB.Bson.Serialization.Attributes;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.AdapterDroite.Mongo.Comptes.DbModels;

/// <summary>Document persisté d'un compte du foyer : identifiant stable (clé), email, statut et
/// id de l'acteur associé (null quand désassocié).</summary>
internal sealed class CompteDocument
{
    [BsonId]
    public string Id { get; set; } = default!;
    public string Email { get; set; } = default!;
    public StatutCompte Statut { get; set; }
    public string? ActeurId { get; set; }

    /// <summary>Condensat du mot de passe local (jamais le clair). Null pour un
    /// compte sans mot de passe (email-only / OAuth).</summary>
    [BsonIgnoreIfNull]
    public string? MotDePasseHache { get; set; }
}
