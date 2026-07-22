using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.AdapterDroite.Mongo.Enfants.DbModels;

/// <summary>Document persisté d'un enfant du foyer : identifiant stable opaque (clé), prénom, la liste
/// des ids stables de ses parents-acteurs liés (0.2, forme inchangée) et le champ ADDITIF 
/// <see cref="RolesDesLiens"/> (id d'acteur → rôle-du-lien), <b>absent sur les documents </b>
/// (<c>null</c> → tous « parent-libre » à la lecture, compat non destructive).</summary>
internal sealed class EnfantDocument
{
    [BsonId]
    public string Id { get; set; } = default!;
    public string Prenom { get; set; } = default!;
    public List<string> ParentsLies { get; set; } = new();

    [BsonIgnoreIfNull]
    public Dictionary<string, RoleDuLien>? RolesDesLiens { get; set; }
}
