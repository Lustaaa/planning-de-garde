using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Infrastructure;

/// <summary>
/// Adaptateur de droite <b>durable</b> (Mongo) du port <see cref="ISlotRecurrentRepository"/> : les slots
/// récurrents hebdo (enfant → lieu, jour de semaine, plage horaire) survivent au redémarrage de l'hôte
/// (parité slot ponctuel s15). Remplaçant durable de <see cref="InMemorySlotRecurrentRepository"/> derrière
/// le <b>port inchangé</b> ; choisi par DI en runtime (« Foyer:Persistance = Mongo »), l'InMemory restant
/// le défaut. <b>Aucun seed Mongo</b> (parité asymétrie seed s15) : au 1er lancement la collection est vide.
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

    public void Supprimer(string slotId)
    {
        // Idempotent : un identifiant absent / malformé ne retire rien et ne lève pas (DeleteOne no-op).
        if (!ObjectId.TryParse(slotId, out var id))
            return;
        _slots.DeleteOne(Builders<SlotRecurrentDocument>.Filter.Eq(d => d.Id, id));
    }

    /// <summary>Document persisté d'un slot récurrent (clé technique générée ; jour de semaine + plage horaire).</summary>
    private sealed class SlotRecurrentDocument
    {
        [BsonId]
        public ObjectId Id { get; set; }
        public string EnfantId { get; set; } = default!;
        public string LieuId { get; set; } = default!;
        public int JourDeSemaine { get; set; }
        public long HeureDebutTicks { get; set; }
        public long HeureFinTicks { get; set; }

        public static SlotRecurrentDocument De(SlotRecurrentSnapshot s)
            // Plage horaire en ticks (round-trip exact, sans fuseau) ; jour de semaine en entier stable.
            => new()
            {
                EnfantId = s.EnfantId,
                LieuId = s.LieuId,
                JourDeSemaine = (int)s.JourDeSemaine,
                HeureDebutTicks = s.HeureDebut.Ticks,
                HeureFinTicks = s.HeureFin.Ticks,
            };

        public SlotRecurrentSnapshot VersSnapshot()
            => new(EnfantId, LieuId, (DayOfWeek)JourDeSemaine, TimeSpan.FromTicks(HeureDebutTicks), TimeSpan.FromTicks(HeureFinTicks), Id.ToString());
    }
}
