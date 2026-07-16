using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Infrastructure;

/// <summary>
/// Adaptateur de droite <b>durable</b> (Mongo) du port <see cref="IJournalChangements"/> : les événements
/// de changement (cloche s47) survivent au redémarrage. Append-only : chaque écriture est insérée, jamais
/// modifiée (trace de lecture horodatée). Remplaçant durable de <see cref="InMemoryJournalChangements"/>
/// derrière le <b>port inchangé</b>.
/// </summary>
public sealed class MongoJournalChangements : IJournalChangements
{
    private readonly IMongoCollection<EvenementDocument> _evenements;

    public MongoJournalChangements(string connectionString, string baseDeDonnees)
        => _evenements = new MongoClient(connectionString).GetDatabase(baseDeDonnees).GetCollection<EvenementDocument>("journal_changements");

    public void Consigner(EvenementChangementSnapshot evenement) => _evenements.InsertOne(EvenementDocument.De(evenement));

    public IReadOnlyList<EvenementChangementSnapshot> Tout()
        => _evenements.Find(Builders<EvenementDocument>.Filter.Empty).ToList().Select(d => d.VersSnapshot()).ToList();

    /// <summary>Document persisté d'un événement (clé métier stable ; jour + horodatage wall-clock).</summary>
    private sealed class EvenementDocument
    {
        [BsonId]
        public string Id { get; set; } = default!;
        public string Type { get; set; } = default!;
        public DateTime Jour { get; set; }
        public string EnfantId { get; set; } = default!;
        public string CedantId { get; set; } = default!;
        public string RecevantId { get; set; } = default!;
        public DateTime Horodatage { get; set; }

        /// <summary>Sous-type d'imprévu (s48) — présent seulement quand <see cref="Type"/> vaut Imprevu (null sinon).</summary>
        public string? Imprevu { get; set; }

        /// <summary>Motif optionnel d'un imprévu (s48) — vide par défaut.</summary>
        public string Motif { get; set; } = "";

        public static EvenementDocument De(EvenementChangementSnapshot e)
            => new()
            {
                Id = e.Id,
                Type = e.Type.ToString(),
                Jour = DateTimeMongo.WallClock(e.Jour.ToDateTime(TimeOnly.MinValue)),
                EnfantId = e.EnfantId,
                CedantId = e.CedantId,
                RecevantId = e.RecevantId,
                Horodatage = DateTimeMongo.WallClock(e.Horodatage),
                Imprevu = e.Imprevu?.ToString(),
                Motif = e.Motif,
            };

        public EvenementChangementSnapshot VersSnapshot()
            => new(Id, Enum.Parse<TypeChangement>(Type), DateOnly.FromDateTime(Jour), EnfantId, CedantId, RecevantId, Horodatage,
                Imprevu is null ? null : Enum.Parse<TypeImprevu>(Imprevu), Motif ?? "");
    }
}
