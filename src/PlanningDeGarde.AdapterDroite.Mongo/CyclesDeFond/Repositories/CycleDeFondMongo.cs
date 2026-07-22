using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.AdapterDroite.Mongo.CyclesDeFond.Repositories;

/// <summary>
/// Adaptateur de droite <b>durable</b> (Mongo) du port <see cref="IReferentielCycleDeFond"/> : le cycle
/// de fond (nombre de semaines + mapping index→responsable) survit au redémarrage de l'hôte (Sc.9, s15) —
/// le fond se re-résout après relance. Remplaçant durable de <see cref="CycleDeFondEnMemoire"/> derrière
/// le <b>port inchangé</b> ; choisi par DI en runtime (« Foyer:Persistance = Mongo »).
///
/// <para><b>Document unique</b> (dernière écriture gagne) : <see cref="DefinirCycle"/> remplace le cycle
/// courant ; <see cref="CycleCourant"/> relit le store (instance fraîche = redémarrage). <c>null</c> si
/// aucun cycle défini (store vierge → pas de fond).</para>
/// </summary>
public sealed class CycleDeFondMongo : IReferentielCycleDeFond
{
    private readonly IMongoCollection<CycleDocument> _cycles;

    public CycleDeFondMongo(string connectionString, string baseDeDonnees)
        => _cycles = new MongoClient(connectionString).GetDatabase(baseDeDonnees).GetCollection<CycleDocument>("cycle_de_fond");

    public CycleDeFond? CycleCourant(string? enfantId = null)
    {
        // ISOLATION STRICTE s53 (gate G3 4e passage) : la résolution d'un enfant NON-NULL ne lit QUE SON cycle
        // (EnfantId == enfantId) — AUCUN repli sur le bucket partagé "" (c'était la fuite : « Charlie » sans cycle
        // propre affichait le cycle global). Un enfant sans cycle propre → null → NEUTRE. enfantId null = legacy "".
        var cle = enfantId ?? "";
        var doc = _cycles.Find(Builders<CycleDocument>.Filter.Eq(d => d.EnfantId, cle)).FirstOrDefault();
        if (doc is null)
            return null;

        var affectations = doc.Affectations.ToDictionary(a => a.Index, a => a.ResponsableId);
        return new CycleDeFond(doc.NombreSemaines, affectations);
    }

    public void DefinirCycle(CycleDeFond cycle, string? enfantId = null)
    {
        // Dernière écriture gagne : on remplace l'unique document de cycle DE CET ENFANT (pas de version ni rejet).
        var cle = enfantId ?? "";
        _cycles.DeleteMany(Builders<CycleDocument>.Filter.Eq(d => d.EnfantId, cle));
        _cycles.InsertOne(CycleDocument.De(cycle, cle));
    }
}
