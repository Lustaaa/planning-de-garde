using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.AdapterDroite.Mongo.Notifications.Repositories;

/// <summary>
/// Adaptateur de droite <b>durable</b> (Mongo) du port <see cref="IJournalChangements"/> : les événements
/// de changement (cloche) survivent au redémarrage. Append-only : chaque écriture est insérée, jamais
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
}
