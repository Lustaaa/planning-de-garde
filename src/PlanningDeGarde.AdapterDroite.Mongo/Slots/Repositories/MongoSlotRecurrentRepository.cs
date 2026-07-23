using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.AdapterDroite.Mongo.Slots.Repositories;

/// <summary>
/// Adaptateur de droite <b>durable</b> (Mongo) du port <see cref="ISlotRecurrentRepository"/> : les slots
/// récurrents hebdo (enfant → lieu, jour de semaine, plage horaire) survivent au redémarrage de l'hôte
/// (parité slot ponctuel). Remplaçant durable de <see cref="InMemorySlotRecurrentRepository"/> derrière
/// le <b>port inchangé</b> ; choisi par DI en runtime (« Foyer:Persistance = Mongo »), l'InMemory restant
/// le défaut. <b>Aucun seed Mongo</b> (parité asymétrie seed) : au 1er lancement la collection est vide.
///
/// <para><b>Write-through + relecture directe</b> : chaque <see cref="Enregistrer"/> insère le snapshot ;
/// <see cref="AllSnapshots"/> relit le store tel quel (une nouvelle instance = un redémarrage qui retrouve
/// l'état persisté). Prouvé contre un store Mongo réel (Docker), jamais une doublure.</para>
/// </summary>
public sealed class MongoSlotRecurrentRepository : ISlotRecurrentRepository
{
    private readonly IMongoCollection<SlotRecurrentDocument> _slots;

    public MongoSlotRecurrentRepository(string connectionString, string baseDeDonnees)
        => _slots = new MongoClient(connectionString).GetDatabase(baseDeDonnees).GetCollection<SlotRecurrentDocument>("slots_recurrents");

    public void Enregistrer(SlotRecurrent slot) => _slots.InsertOne(SlotRecurrentDocument.De(slot.ToSnapshot()));

    public IReadOnlyList<SlotRecurrentSnapshot> AllSnapshots()
        => _slots.Find(Builders<SlotRecurrentDocument>.Filter.Empty).ToList().Select(d => d.VersSnapshot()).ToList();

    public void Remplacer(string slotId, SlotRecurrent slot)
    {
        // Réécriture en place : l'ObjectId (identifiant stable) est conservé, seul le contenu change.
        // Idempotent sur identifiant absent / malformé (ReplaceOne sans upsert = no-op).
        if (!ObjectId.TryParse(slotId, out var id))
            return;
        var doc = SlotRecurrentDocument.De(slot.ToSnapshot());
        doc.Id = id;
        _slots.ReplaceOne(Builders<SlotRecurrentDocument>.Filter.Eq(d => d.Id, id), doc);
    }

    public void Supprimer(string slotId)
    {
        // Idempotent : un identifiant absent / malformé ne retire rien et ne lève pas (DeleteOne no-op).
        if (!ObjectId.TryParse(slotId, out var id))
            return;
        _slots.DeleteOne(Builders<SlotRecurrentDocument>.Filter.Eq(d => d.Id, id));
    }
}
