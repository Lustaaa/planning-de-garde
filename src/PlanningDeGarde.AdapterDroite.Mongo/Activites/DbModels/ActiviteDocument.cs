using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace PlanningDeGarde.AdapterDroite.Mongo.Activites.DbModels;

/// <summary>Document persisté d'une activité du foyer : identifiant stable (clé), libellé,
/// <b>adresse</b> optionnelle (<c>null</c> = non renseignée → énumérée « vide ») et les
/// <b>enfants liés</b> (lien N-M — ids d'enfants du référentiel).</summary>
internal sealed class ActiviteDocument
{
    [BsonId]
    public string Id { get; set; } = default!;
    public string Libelle { get; set; } = default!;
    public string? Adresse { get; set; } // adresse optionnelle (s35 Sc.2), null = non renseignée
    public List<string> EnfantsLies { get; set; } = new(); // enfants liés (s35 Sc.3, lien N-M)
}
