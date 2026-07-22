using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.AdapterDroite.Mongo.Notifications.Repositories;

/// <summary>
/// Adaptateur de droite <b>durable</b> (Mongo) du port <see cref="IEtatLectureNotifications"/> : l'état LU /
/// non-lu par utilisateur (cloche) survit au redémarrage. Un document par couple (utilisateur, événement) —
/// clé composite stable ; l'upsert rend <see cref="MarquerLu"/> idempotent (aucun doublon).
/// </summary>
public sealed class MongoEtatLectureNotifications : IEtatLectureNotifications
{
    private readonly IMongoCollection<LectureDocument> _lectures;

    public MongoEtatLectureNotifications(string connectionString, string baseDeDonnees)
        => _lectures = new MongoClient(connectionString).GetDatabase(baseDeDonnees).GetCollection<LectureDocument>("lectures_notifications");

    public void MarquerLu(string utilisateurId, string evenementId)
        => _lectures.ReplaceOne(
            Builders<LectureDocument>.Filter.Eq(d => d.Id, Cle(utilisateurId, evenementId)),
            new LectureDocument { Id = Cle(utilisateurId, evenementId), UtilisateurId = utilisateurId, EvenementId = evenementId },
            new ReplaceOptions { IsUpsert = true });

    public IReadOnlyCollection<string> EvenementsLus(string utilisateurId)
        => _lectures.Find(Builders<LectureDocument>.Filter.Eq(d => d.UtilisateurId, utilisateurId))
            .ToList().Select(d => d.EvenementId).ToList();

    private static string Cle(string utilisateurId, string evenementId) => $"{utilisateurId}::{evenementId}";
}
