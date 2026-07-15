using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Infrastructure;

/// <summary>
/// Adaptateur de droite <b>durable</b> (Mongo) du port <see cref="IPropositionEchangeRepository"/> : les
/// propositions d'échange (s47) survivent au redémarrage. Remplaçant durable de
/// <see cref="InMemoryPropositionEchangeRepository"/> derrière le <b>port inchangé</b>. Upsert par
/// identifiant stable (clé métier générée par l'agrégat) — la transition de statut (proposé → accepté /
/// refusé) remplace le document, jamais ne duplique.
/// </summary>
public sealed class MongoPropositionEchangeRepository : IPropositionEchangeRepository
{
    private readonly IMongoCollection<PropositionDocument> _propositions;

    public MongoPropositionEchangeRepository(string connectionString, string baseDeDonnees)
        => _propositions = new MongoClient(connectionString).GetDatabase(baseDeDonnees).GetCollection<PropositionDocument>("propositions_echange");

    public void Sauvegarder(PropositionEchange proposition)
        => _propositions.ReplaceOne(
            Builders<PropositionDocument>.Filter.Eq(d => d.Id, proposition.Id),
            PropositionDocument.De(proposition.ToSnapshot()),
            new ReplaceOptions { IsUpsert = true });

    public IReadOnlyList<PropositionEchangeSnapshot> AllSnapshots()
        => _propositions.Find(Builders<PropositionDocument>.Filter.Empty).ToList().Select(d => d.VersSnapshot()).ToList();

    public PropositionEchangeSnapshot? ParId(string id)
        => _propositions.Find(Builders<PropositionDocument>.Filter.Eq(d => d.Id, id)).FirstOrDefault()?.VersSnapshot();

    public void Supprimer(string id)
        => _propositions.DeleteOne(Builders<PropositionDocument>.Filter.Eq(d => d.Id, id));

    /// <summary>Document persisté d'une proposition (clé métier stable ; jour wall-clock ; statut).</summary>
    private sealed class PropositionDocument
    {
        [BsonId]
        public string Id { get; set; } = default!;
        public DateTime Jour { get; set; }
        public string EnfantId { get; set; } = default!;
        public string DeActeurId { get; set; } = default!;
        public string VersActeurId { get; set; } = default!;
        public string Statut { get; set; } = default!;

        public static PropositionDocument De(PropositionEchangeSnapshot p)
            => new()
            {
                Id = p.Id,
                Jour = DateTimeMongo.WallClock(p.Jour.ToDateTime(TimeOnly.MinValue)),
                EnfantId = p.EnfantId,
                DeActeurId = p.DeActeurId,
                VersActeurId = p.VersActeurId,
                Statut = p.Statut.ToString(),
            };

        public PropositionEchangeSnapshot VersSnapshot()
            => new(Id, DateOnly.FromDateTime(Jour), EnfantId, DeActeurId, VersActeurId, Enum.Parse<StatutProposition>(Statut));
    }
}
