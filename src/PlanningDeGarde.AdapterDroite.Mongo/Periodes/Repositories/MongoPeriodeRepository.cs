using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.AdapterDroite.Mongo.Periodes.Repositories;

/// <summary>
/// Adaptateur de droite <b>durable</b> (Mongo) du port <see cref="IPeriodeRepository"/> : les périodes
/// de garde (responsable, intervalle) survivent au redémarrage de l'hôte (Sc.9, s15). Remplaçant durable
/// de <see cref="InMemoryPeriodeRepository"/> derrière le <b>port inchangé</b> ; choisi par DI en runtime
/// (« Foyer:Persistance = Mongo »).
///
/// <para><b>Write-through + relecture directe</b> ; <see cref="Modifier"/> reste <b>optimiste</b> :
/// l'écriture n'aboutit que si <c>baseObservee</c> correspond encore à une période persistée (sinon état
/// périmé, refus). Le jeton de version = le snapshot relu depuis Mongo, donc comparé à même précision.</para>
/// </summary>
public sealed class MongoPeriodeRepository : IPeriodeRepository
{
    private readonly IMongoCollection<PeriodeDocument> _periodes;

    public MongoPeriodeRepository(string connectionString, string baseDeDonnees)
        => _periodes = new MongoClient(connectionString).GetDatabase(baseDeDonnees).GetCollection<PeriodeDocument>("periodes");

    public void Enregistrer(PeriodeDeGarde periode) => _periodes.InsertOne(PeriodeDocument.De(periode.ToSnapshot()));

    public IReadOnlyList<PeriodeSnapshot> AllSnapshots()
        => _periodes.Find(Builders<PeriodeDocument>.Filter.Empty).ToList().Select(d => d.VersSnapshot()).ToList();

    public void Supprimer(string periodeId)
    {
        // Idempotent : un identifiant absent / malformé ne retire rien et ne lève pas (DeleteOne no-op).
        if (!ObjectId.TryParse(periodeId, out var id))
            return;
        _periodes.DeleteOne(Builders<PeriodeDocument>.Filter.Eq(d => d.Id, id));
    }

    public bool Modifier(PeriodeSnapshot baseObservee, PeriodeSnapshot modification)
    {
        // Concurrence optimiste : on remplace UN document correspondant exactement à la base observée
        // (responsable + bornes). Aucun document concordant → écriture périmée (état devancé) → false.
        var filtre = Builders<PeriodeDocument>.Filter.Where(d =>
            d.ResponsableId == baseObservee.ResponsableId && d.Debut == baseObservee.Debut && d.Fin == baseObservee.Fin);
        var courant = _periodes.Find(filtre).FirstOrDefault();
        if (courant is null)
            return false;

        _periodes.ReplaceOne(
            Builders<PeriodeDocument>.Filter.Eq(d => d.Id, courant.Id),
            PeriodeDocument.De(modification, courant.Id));
        return true;
    }
}
