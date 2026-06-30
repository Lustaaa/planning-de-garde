using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Infrastructure;

/// <summary>
/// Adaptateur de droite <b>durable</b> (Mongo) du port <see cref="ISlotRepository"/> : les slots de
/// localisation (enfant → lieu, intervalle) survivent au redémarrage de l'hôte (Sc.9, s15). Remplaçant
/// durable de <see cref="InMemorySlotRepository"/> derrière le <b>port inchangé</b> ; choisi par DI en
/// runtime (« Foyer:Persistance = Mongo »), l'InMemory restant le défaut des tests.
///
/// <para><b>Write-through + relecture directe</b> : chaque <see cref="Enregistrer"/> insère le
/// <see cref="SlotSnapshot"/> dans Mongo ; <see cref="AllSnapshots"/> relit le store tel quel (une
/// nouvelle instance = un redémarrage qui retrouve l'état persisté). Prouvé contre un store Mongo réel
/// (Docker), jamais une doublure.</para>
/// </summary>
public sealed class MongoSlotRepository : ISlotRepository
{
    private readonly IMongoCollection<SlotDocument> _slots;

    public MongoSlotRepository(string connectionString, string baseDeDonnees)
        => _slots = new MongoClient(connectionString).GetDatabase(baseDeDonnees).GetCollection<SlotDocument>("slots");

    public void Enregistrer(SlotDeLocalisation slot) => _slots.InsertOne(SlotDocument.De(slot.ToSnapshot()));

    public IReadOnlyList<SlotSnapshot> AllSnapshots()
        => _slots.Find(Builders<SlotDocument>.Filter.Empty).ToList().Select(d => d.VersSnapshot()).ToList();

    public void Remplacer(SlotSnapshot ancien, SlotDeLocalisation nouveau)
    {
        // Déplacement = retrait du slot d'origine (par ses bornes) puis insertion de sa version déplacée.
        _slots.DeleteOne(Builders<SlotDocument>.Filter.Where(d =>
            d.EnfantId == ancien.EnfantId && d.LieuId == ancien.LieuId && d.Debut == ancien.Debut && d.Fin == ancien.Fin));
        _slots.InsertOne(SlotDocument.De(nouveau.ToSnapshot()));
    }

    public void Supprimer(string slotId)
    {
        // Idempotent : un identifiant absent / malformé ne retire rien et ne lève pas (DeleteOne no-op).
        if (!ObjectId.TryParse(slotId, out var id))
            return;
        _slots.DeleteOne(Builders<SlotDocument>.Filter.Eq(d => d.Id, id));
    }

    /// <summary>Document persisté d'un slot (clé technique générée ; bornes datées + enfant/lieu).</summary>
    private sealed class SlotDocument
    {
        [BsonId]
        public ObjectId Id { get; set; }
        public string EnfantId { get; set; } = default!;
        public string LieuId { get; set; } = default!;
        public System.DateTime Debut { get; set; }
        public System.DateTime Fin { get; set; }

        public static SlotDocument De(SlotSnapshot s)
            // Bornes traitées en wall-clock STABLE (Kind=Utc) : les dates du planning n'ont pas de fuseau,
            // marquer Utc évite la conversion locale→UTC de BSON qui décalerait la date d'un jour près de minuit.
            => new() { EnfantId = s.EnfantId, LieuId = s.LieuId, Debut = DateTimeMongo.WallClock(s.Debut), Fin = DateTimeMongo.WallClock(s.Fin) };

        public SlotSnapshot VersSnapshot() => new(EnfantId, LieuId, Debut, Fin, Id.ToString());
    }
}
