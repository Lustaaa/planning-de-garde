using MongoDB.Bson.Serialization.Attributes;

namespace PlanningDeGarde.AdapterDroite.Mongo.Foyer.DbModels;

/// <summary>Document persisté d'un admin du foyer : id stable de l'acteur (clé).</summary>
internal sealed class AdminDocument
{
    [BsonId]
    public string Id { get; set; } = default!;
}
