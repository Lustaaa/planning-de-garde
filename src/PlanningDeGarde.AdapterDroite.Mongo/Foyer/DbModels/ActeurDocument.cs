using MongoDB.Bson.Serialization.Attributes;

namespace PlanningDeGarde.AdapterDroite.Mongo.Foyer.DbModels;

/// <summary>Document persisté d'un acteur du foyer : identifiant stable (clé), nom et couleur
/// d'affichage (chacun optionnel — résolus sur l'id, jamais sur le libellé).</summary>
internal sealed class ActeurDocument
{
    [BsonId]
    public string Id { get; set; } = default!;
    public string? Nom { get; set; }
    public string? Couleur { get; set; }
    public string? RoleId { get; set; } // id de rôle porté par l'acteur (null = « sans rôle »)
    public string? Adresse { get; set; } // adresse de résidence (null = non renseignée, optionnelle)
}
