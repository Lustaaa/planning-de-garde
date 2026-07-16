using System.Collections.Generic;

namespace PlanningDeGarde.Application;

/// <summary>
/// Port de persistance de l'état LU / NON-LU des notifications, <b>PAR utilisateur</b> (cloche s47). C'est le
/// SEUL vrai état persisté neuf de la cloche (le journal, lui, est une trace de lecture dérivée des écritures).
/// L'état de lecture d'un utilisateur est INDÉPENDANT de celui des autres (chacun garde ses non-lus).
/// <see cref="MarquerLu"/> est idempotent (re-marquer ne crée pas de doublon). 2 adaptateurs InMemory + Mongo.
/// </summary>
public interface IEtatLectureNotifications
{
    /// <summary>Marque l'événement <paramref name="evenementId"/> comme lu pour <paramref name="utilisateurId"/>. Idempotent.</summary>
    void MarquerLu(string utilisateurId, string evenementId);

    /// <summary>Identifiants d'événements déjà marqués lus par <paramref name="utilisateurId"/>.</summary>
    IReadOnlyCollection<string> EvenementsLus(string utilisateurId);
}
