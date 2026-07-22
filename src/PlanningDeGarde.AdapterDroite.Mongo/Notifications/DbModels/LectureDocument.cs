using MongoDB.Bson.Serialization.Attributes;

namespace PlanningDeGarde.AdapterDroite.Mongo.Notifications.DbModels;

/// <summary>Document persisté d'un état de lecture : clé composite (utilisateur::événement), + les deux
/// composantes portées à plat pour l'énumération par utilisateur.</summary>
internal sealed class LectureDocument
{
    [BsonId]
    public string Id { get; set; } = default!;
    public string UtilisateurId { get; set; } = default!;
    public string EvenementId { get; set; } = default!;
}
