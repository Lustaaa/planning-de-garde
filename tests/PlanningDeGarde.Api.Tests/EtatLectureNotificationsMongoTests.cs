using System;
using MongoDB.Driver;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Infrastructure;

namespace PlanningDeGarde.Api.Tests;

/// <summary>
/// Sprint 47 — Sc.2 — Acceptation d'<b>intégration sur Mongo RÉEL</b> (2ᵉ des deux adaptateurs) : l'état LU /
/// non-lu des notifications PAR utilisateur (+ compteur de non-lus) survit au redémarrage, reste INDÉPENDANT
/// entre utilisateurs et est IDEMPOTENT (re-marquer ne crée pas de doublon). Le compteur est recalculé au
/// travers du journal durable (trace de lecture) enrichi de l'état de lecture persisté.
/// </summary>
public sealed class EtatLectureNotificationsMongoTests : IDisposable
{
    private const string ConnectionString = "mongodb://localhost:27017";
    private readonly string _baseDeTest = $"planning_test_{Guid.NewGuid():N}";

    private const string AliceId = "acteur-alice";
    private const string BrunoId = "acteur-bruno";
    private const string LeaId = "enfant-lea";
    private static readonly DateOnly J1 = new(2026, 7, 8);
    private static readonly DateOnly J2 = new(2026, 7, 9);

    private static EvenementChangementSnapshot Evt(string id, TypeChangement type, DateOnly jour, string cedant, string recevant, int minute)
        => new(id, type, jour, LeaId, cedant, recevant, new DateTime(2026, 7, 1, 8, minute, 0));

    private MongoJournalChangements JournalNeuf() => new(ConnectionString, _baseDeTest);
    private MongoEtatLectureNotifications EtatNeuf() => new(ConnectionString, _baseDeTest);

    [MongoRequisFact]
    public void Acceptation_Should_Persister_etat_lu_par_utilisateur_durable_independant_idempotent()
    {
        // --- Given : journal Mongo durable avec 2 événements concernant Bruno (recevant) ; Alice est cédante sur les deux ---
        var journal = JournalNeuf();
        var e1 = Evt("e1", TypeChangement.Delegation, J1, AliceId, BrunoId, 10);
        var e2 = Evt("e2", TypeChangement.Transfert, J2, AliceId, BrunoId, 20);
        journal.Consigner(e1);
        journal.Consigner(e2);

        var flux = new FluxNotificationsQuery(JournalNeuf(), EtatNeuf());
        var handler = new MarquerNotificationsLuesHandler(flux, EtatNeuf());

        // --- Then : tout non-lu — compteur = 2 pour Bruno ---
        Assert.Equal(2, flux.NombreNonLus(BrunoId));

        // --- When : Bruno marque e1 lu (Mongo réel) ---
        Assert.Equal(1, handler.Handle(new MarquerNotificationsLuesCommand(BrunoId, e1.Id)).Valeur);

        // --- Redémarrage : instances neuves — l'état de lecture est DURABLE ---
        var fluxApres = new FluxNotificationsQuery(JournalNeuf(), EtatNeuf());
        Assert.Equal(1, fluxApres.NombreNonLus(BrunoId));
        Assert.Contains(fluxApres.FluxAvecEtat(BrunoId), n => n.Evenement.Id == "e1" && n.Lu);
        Assert.Contains(fluxApres.FluxAvecEtat(BrunoId), n => n.Evenement.Id == "e2" && !n.Lu);

        // --- IDEMPOTENCE : re-marquer e1 ne crée pas de doublon ---
        EtatNeuf().MarquerLu(BrunoId, e1.Id);
        Assert.Single(EtatNeuf().EvenementsLus(BrunoId));

        // --- INDÉPENDANCE : Alice (cédante sur e1 & e2) garde ses 2 non-lus, inaffectée par le « lu » de Bruno ---
        Assert.Equal(2, fluxApres.NombreNonLus(AliceId));
        Assert.Empty(EtatNeuf().EvenementsLus(AliceId));
    }

    public void Dispose()
    {
        try
        {
            new MongoClient(ConnectionString).DropDatabase(_baseDeTest);
        }
        catch
        {
            // Best effort.
        }
    }
}
