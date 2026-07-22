using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson.Serialization.Attributes;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.AdapterDroite.Mongo.CyclesDeFond.DbModels;

/// <summary>Document persisté du cycle : longueur + mapping index→responsable (clés BSON stables).</summary>
internal sealed class CycleDocument
{
    [BsonId]
    public MongoDB.Bson.ObjectId Id { get; set; }
    // s53 : "" = cycle partagé/legacy ; sinon cycle propre à l'enfant. Élément BSON absent → "" (rétro-compat).
    public string EnfantId { get; set; } = "";
    public int NombreSemaines { get; set; }
    public List<AffectationDocument> Affectations { get; set; } = new();

    public static CycleDocument De(CycleDeFond cycle, string enfantId = "")
        => new()
        {
            Id = MongoDB.Bson.ObjectId.GenerateNewId(),
            EnfantId = enfantId,
            NombreSemaines = cycle.NombreSemaines,
            // Mapping sérialisé en liste de paires (les clés de dictionnaire BSON doivent être des
            // chaînes ; une liste de { Index, ResponsableId } préserve les index entiers tels quels).
            Affectations = cycle.Affectations.Select(kv => new AffectationDocument { Index = kv.Key, ResponsableId = kv.Value }).ToList(),
        };
}
