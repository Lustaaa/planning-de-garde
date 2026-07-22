using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.AdapterDroite.Mongo.Transferts.Repositories;

/// <summary>
/// Adaptateur de droite <b>durable</b> (Mongo) du port <see cref="ITransfertRepository"/> : les transferts
/// de bascule (déposant, récupérant, lieu, heure, date) survivent au redémarrage de l'hôte.
/// Remplaçant durable de <see cref="InMemoryTransfertRepository"/> derrière le <b>port inchangé</b> ;
/// choisi par DI en runtime (« Foyer:Persistance = Mongo »). Write-through + relecture directe.
/// </summary>
public sealed class MongoTransfertRepository : ITransfertRepository
{
    private readonly IMongoCollection<TransfertDocument> _transferts;

    public MongoTransfertRepository(string connectionString, string baseDeDonnees)
        => _transferts = new MongoClient(connectionString).GetDatabase(baseDeDonnees).GetCollection<TransfertDocument>("transferts");

    public void Enregistrer(Transfert transfert) => _transferts.InsertOne(TransfertDocument.De(transfert.ToSnapshot()));

    public IReadOnlyList<TransfertSnapshot> AllSnapshots()
        => _transferts.Find(Builders<TransfertDocument>.Filter.Empty).ToList().Select(d => d.VersSnapshot()).ToList();
}
