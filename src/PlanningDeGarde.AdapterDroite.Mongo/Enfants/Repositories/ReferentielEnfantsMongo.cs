using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.AdapterDroite.Mongo.Enfants.Repositories;

/// <summary>
/// Adaptateur de droite <b>durable</b> (Mongo) du référentiel d'enfants du foyer — réalise,
/// derrière les <b>ports inchangés</b>, la lecture (<see cref="IEnumerationEnfants"/>) et l'écriture
/// (<see cref="IEditeurEnfants"/>). Remplaçant durable de <c>ReferentielEnfantsEnMemoire</c> : un enfant
/// ajouté/édité/lié survit au redémarrage du serveur (une instance fraîche = un redémarrage relit l'état
/// persisté), prouvé contre un store Mongo réel (Docker).
///
/// <para><b>Borné à la config foyer</b> : réutilise le socle Mongo config déjà acquis (même base),
/// collection dédiée « enfants ». <b>Aucun seed</b> (parité asymétrie seed). La clé est l'identifiant
/// stable opaque de l'enfant.</para>
///
/// <para><b>Rôle-du-lien — enrichissement ADDITIF, compat non destructive.</b> La forme du lien
/// (<c>ParentsLies</c> = liste d'ids d'acteurs) est <b>conservée telle quelle</b> ; le rôle-du-lien est
/// porté par un champ parallèle <c>RolesDesLiens</c> (id d'acteur → rôle), ABSENT sur les documents 
/// déjà persistés. À la lecture, un parent sans entrée dans <c>RolesDesLiens</c> est relu à
/// <see cref="RoleDuLien.ParentLibre"/> (défaut neutre) — jamais un crash de désérialisation, aucune
/// migration destructive du store.</para>
/// </summary>
public sealed class ReferentielEnfantsMongo : IEnumerationEnfants, IEditeurEnfants
{
    private readonly IMongoCollection<EnfantDocument> _enfants;
    private readonly Dictionary<string, EnfantDocument> _cache;

    public ReferentielEnfantsMongo(string connectionString, string baseDeDonnees)
    {
        var db = new MongoClient(connectionString).GetDatabase(baseDeDonnees);
        _enfants = db.GetCollection<EnfantDocument>("enfants");
        // Aucun seed : on relit l'état persisté tel quel (vide au premier lancement).
        _cache = _enfants.Find(Builders<EnfantDocument>.Filter.Empty).ToList().ToDictionary(d => d.Id);
    }

    public void Ajouter(string enfantId, string prenom) => EcrireWriteThrough(enfantId, prenom);

    public void Editer(string enfantId, string nouveauPrenom) => EcrireWriteThrough(enfantId, nouveauPrenom);

    private void EcrireWriteThrough(string enfantId, string prenom)
    {
        // Écriture write-through : cache de session ET store durable (upsert sur l'id stable) — l'enfant
        // survit au redémarrage. Le même id reste un unique document ; l'édition écrase le prénom sur la
        // même clé. Les parents liés déjà persistés ET leurs rôles-du-lien sont conservés.
        var doc = Copie(enfantId);
        doc.Prenom = prenom;
        Persister(doc);
    }

    public void LierParent(string enfantId, string acteurId, RoleDuLien role = RoleDuLien.ParentLibre)
    {
        // Enrichissement durable : le parent-acteur lié (forme s34 conservée) + son rôle-du-lien (champ
        // additif s37), write-through sur le store durable. Upsert par acteur : re-lier un parent déjà
        // lié MET À JOUR son rôle-du-lien sans dupliquer le lien (id de l'enfant, prénom, autres liens inchangés).
        var doc = Copie(enfantId);
        if (!doc.ParentsLies.Contains(acteurId))
            doc.ParentsLies.Add(acteurId);
        (doc.RolesDesLiens ??= new Dictionary<string, RoleDuLien>())[acteurId] = role;
        Persister(doc);
    }

    public void DelierParent(string enfantId, string acteurId)
    {
        // Retrait DURABLE du lien (id de l'enfant et prénom inchangés, autres liens conservés). Idempotent :
        // un enfant absent ou un parent déjà non lié → aucune écriture (write-through seulement si mutation).
        if (!_cache.TryGetValue(enfantId, out var existant) || !existant.ParentsLies.Contains(acteurId))
            return;
        var doc = Copie(enfantId);
        doc.ParentsLies.Remove(acteurId);
        doc.RolesDesLiens?.Remove(acteurId);
        Persister(doc);
    }

    /// <summary>Copie profonde du document existant (ou un document neuf) : mutation locale avant write-through.</summary>
    private EnfantDocument Copie(string enfantId)
        => _cache.TryGetValue(enfantId, out var existant)
            ? new EnfantDocument
            {
                Id = existant.Id,
                Prenom = existant.Prenom,
                ParentsLies = new List<string>(existant.ParentsLies),
                RolesDesLiens = existant.RolesDesLiens is null ? null : new Dictionary<string, RoleDuLien>(existant.RolesDesLiens),
            }
            : new EnfantDocument { Id = enfantId, Prenom = string.Empty, ParentsLies = new List<string>() };

    private void Persister(EnfantDocument doc)
    {
        _cache[doc.Id] = doc;
        _enfants.ReplaceOne(
            Builders<EnfantDocument>.Filter.Eq(d => d.Id, doc.Id),
            doc,
            new ReplaceOptions { IsUpsert = true });
    }

    public IReadOnlyCollection<EnfantFoyer> EnumererEnfants()
        => _cache.Values.Select(d => new EnfantFoyer(d.Id, d.Prenom, ParentsDe(d))).ToList();

    /// <summary>Relit les parents liés d'un document AVEC leur rôle-du-lien : le rôle est cherché dans le
    /// champ additif <c>RolesDesLiens</c> ; un parent sans entrée (document sans le champ) est relu à
    /// <see cref="RoleDuLien.ParentLibre"/> (défaut neutre, compat non destructive).</summary>
    private static IReadOnlyCollection<ParentLie> ParentsDe(EnfantDocument doc)
        => doc.ParentsLies
            .Select(id => new ParentLie(id, doc.RolesDesLiens is not null && doc.RolesDesLiens.TryGetValue(id, out var r) ? r : RoleDuLien.ParentLibre))
            .ToList();
}
