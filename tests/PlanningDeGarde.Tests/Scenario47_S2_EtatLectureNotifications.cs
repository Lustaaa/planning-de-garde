using System;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Infrastructure;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 47 — Sc.2 — État lu / non-lu PAR utilisateur + compteur (@back)
//   Étant donné un flux de notifications pour un utilisateur avec des événements non lus
//   Quand on lit le compteur de non-lus → il reflète le nombre d'événements non encore marqués lus PAR CET utilisateur
//   Quand l'utilisateur marque une notification (ou toutes) comme lue(s) → l'état "lu" est persisté PAR utilisateur
//     (un autre utilisateur garde son propre état non-lu)
//   Et re-marquer lu est idempotent (aucun doublon, compteur stable)
//
// Frontière Application : FluxNotificationsQuery (lecture) + MarquerNotificationsLuesHandler (écriture d'état de lecture)
// sur l'adaptateur InMemory RÉEL (1ᵉʳ des deux ; Mongo durable prouvé en Api.Tests).
public class Scenario47_S2_EtatLectureNotifications
{
    private const string AliceId = "acteur-alice";
    private const string BrunoId = "acteur-bruno";
    private const string LeaId = "enfant-lea";
    private static readonly DateOnly J1 = new(2026, 7, 8);
    private static readonly DateOnly J2 = new(2026, 7, 9);
    private static readonly DateOnly J3 = new(2026, 7, 10);

    private static EvenementChangementSnapshot Evt(string id, TypeChangement type, DateOnly jour, string cedant, string recevant, int minute)
        => new(id, type, jour, LeaId, cedant, recevant, new DateTime(2026, 7, 1, 8, minute, 0));

    [Fact]
    public void Acceptation_Should_Compter_marquer_lu_par_utilisateur_de_maniere_independante_et_idempotente()
    {
        // --- Given : un journal réel portant 3 événements concernant Bruno (recevant) ; Alice figure sur e1 & e2 ---
        var journal = new InMemoryJournalChangements();
        var e1 = Evt("e1", TypeChangement.Delegation, J1, AliceId, BrunoId, 10);
        var e2 = Evt("e2", TypeChangement.Transfert, J2, AliceId, BrunoId, 20);
        var e3 = Evt("e3", TypeChangement.Reprise, J3, BrunoId, "", 30);
        journal.Consigner(e1);
        journal.Consigner(e2);
        journal.Consigner(e3);

        var etat = new InMemoryEtatLectureNotifications(); // adaptateur RÉEL
        var flux = new FluxNotificationsQuery(journal, etat);
        var handler = new MarquerNotificationsLuesHandler(flux, etat);

        // --- Then : tout est non-lu au départ — compteur = 3 pour Bruno ---
        Assert.Equal(3, flux.NombreNonLus(BrunoId));
        Assert.All(flux.FluxAvecEtat(BrunoId), n => Assert.False(n.Lu));

        // --- When : Bruno marque UNE notification (e2) comme lue ---
        var apres1 = handler.Handle(new MarquerNotificationsLuesCommand(BrunoId, e2.Id));

        // --- Then : compteur = 2, e2 lu, e1 & e3 non-lus ---
        Assert.True(apres1.EstSucces);
        Assert.Equal(2, apres1.Valeur);
        Assert.Equal(2, flux.NombreNonLus(BrunoId));
        Assert.Contains(flux.FluxAvecEtat(BrunoId), n => n.Evenement.Id == "e2" && n.Lu);
        Assert.Contains(flux.FluxAvecEtat(BrunoId), n => n.Evenement.Id == "e1" && !n.Lu);

        // --- Then : INDÉPENDANCE — Alice (concernée par e1 & e2) garde son propre état non-lu (2), inaffectée par Bruno ---
        Assert.Equal(2, flux.NombreNonLus(AliceId));

        // --- When : Bruno marque TOUT comme lu (EvenementId null) ---
        var apresTout = handler.Handle(new MarquerNotificationsLuesCommand(BrunoId, null));

        // --- Then : compteur = 0, tout lu pour Bruno ---
        Assert.Equal(0, apresTout.Valeur);
        Assert.Equal(0, flux.NombreNonLus(BrunoId));
        Assert.All(flux.FluxAvecEtat(BrunoId), n => Assert.True(n.Lu));

        // --- When : re-marquer TOUT (idempotence) ---
        var apresRe = handler.Handle(new MarquerNotificationsLuesCommand(BrunoId, null));

        // --- Then : compteur stable à 0, aucun doublon dans l'état de lecture (3 événements distincts) ---
        Assert.Equal(0, apresRe.Valeur);
        Assert.Equal(3, etat.EvenementsLus(BrunoId).Count);

        // --- Then : Alice reste indépendante — toujours 2 non-lus ---
        Assert.Equal(2, flux.NombreNonLus(AliceId));
    }
}
