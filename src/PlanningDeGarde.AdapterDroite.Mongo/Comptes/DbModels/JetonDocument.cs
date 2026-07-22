using System;
using MongoDB.Bson.Serialization.Attributes;

namespace PlanningDeGarde.AdapterDroite.Mongo.Comptes.DbModels;

/// <summary>Document persisté d'un jeton de réinitialisation : valeur opaque (clé), id du compte
/// visé, instant d'expiration (UTC) et drapeau de consommation (usage unique).</summary>
internal sealed class JetonDocument
{
    [BsonId]
    public string Jeton { get; set; } = default!;
    public string CompteId { get; set; } = default!;
    public DateTime Expiration { get; set; }
    public bool Consomme { get; set; }
}
