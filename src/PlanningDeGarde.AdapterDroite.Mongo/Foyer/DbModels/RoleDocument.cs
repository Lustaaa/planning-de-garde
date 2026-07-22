using MongoDB.Bson.Serialization.Attributes;

namespace PlanningDeGarde.AdapterDroite.Mongo.Foyer.DbModels;

/// <summary>Document persisté d'un rôle du foyer : identifiant stable (clé), libellé et flag
/// « est un rôle parent ». Défaut BSON <c>false</c> : un document antérieur sans ce
/// champ (donnée d'avant) se relit non-parent, sans crash.</summary>
internal sealed class RoleDocument
{
    [BsonId]
    public string Id { get; set; } = default!;
    public string Libelle { get; set; } = default!;
    [BsonDefaultValue(false)]
    public bool EstRoleParent { get; set; }
}
