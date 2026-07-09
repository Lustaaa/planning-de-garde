using System.Collections.Generic;
using System.Threading.Tasks;
using PlanningDeGarde.Application;
using PlanningDeGarde.Web;
using PlanningDeGarde.Web.State;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 31 — Sc.2 (@back — frontière de restauration de session, volet 1 Login F5, 0 changement de cœur).
/// Contrat : à la connexion, un jeton de session est <b>persisté</b> côté client via le port
/// <see cref="IPersistanceSession"/> (adaptateur JS localStorage — doublé À LA MAIN ici, seul le port l'est) ;
/// au <b>démarrage</b> du client, le <see cref="RestaurateurSession"/> relit le jeton et, s'il est valide,
/// <b>restaure la session</b> (<see cref="SessionPlanning.Connecter"/>, identité réelle ancrée) SANS repasser
/// par le flux de connexion. Un jeton <b>absent</b> ou <b>invalide</b> n'ouvre AUCUNE session (pas de session
/// fantôme). La borne anti-cliquet R30 (<see cref="SessionPlanning"/> en mémoire) n'est pas violée : la
/// persistance est un port de bord distinct, la session reste en mémoire — seul son amorçage est rejoué.
/// </summary>
public sealed class RestaurationSessionContratTests
{
    [Fact]
    public async Task Un_jeton_persiste_valide_est_relu_au_demarrage_et_restaure_la_session()
    {
        // Given — une connexion réussie a persisté un jeton via le chemin réel de persistance (port doublé).
        var persistance = new FakePersistanceSession();
        await persistance.PersisterAsync(new SessionPersistee("acteur-alice", "Alice", TypeActeur.Parent));

        // When — le client redémarre : la session repart vierge (mémoire vidée par le F5) et le restaurateur
        // relit le jeton persisté valide au démarrage.
        var session = new SessionPlanning();
        var restaurateur = new RestaurateurSession(persistance);
        await restaurateur.RestaurerAsync(session);

        // Then — la session est restaurée sans repasser par le flux de connexion : compte connecté, identité
        // réelle ancrée sur l'acteur du jeton (id stable + nom + type), aucune incarnation active.
        Assert.True(session.EstConnecte);
        Assert.Equal("Alice", session.CompteConnecteNom);
        Assert.Equal("acteur-alice", session.IdentiteReelle.Id);
        Assert.Equal(TypeActeur.Parent, session.IdentiteReelle.Type);
        Assert.False(session.IncarnationActive);
    }

    [Fact]
    public async Task Un_jeton_absent_n_ouvre_aucune_session()
    {
        // Given — aucun jeton persisté (premier accès / stockage vide).
        var persistance = new FakePersistanceSession();

        // When — le restaurateur s'exécute au démarrage.
        var session = new SessionPlanning();
        await new RestaurateurSession(persistance).RestaurerAsync(session);

        // Then — aucune session n'est ouverte (pas de session fantôme).
        Assert.False(session.EstConnecte);
    }

    [Fact]
    public async Task Un_jeton_invalide_n_ouvre_aucune_session()
    {
        // Given — un jeton persisté corrompu / incomplet (acteur manquant) : le stockage a rendu une valeur
        // mais elle n'identifie aucun compte.
        var persistance = new FakePersistanceSession();
        await persistance.PersisterAsync(new SessionPersistee("", "", TypeActeur.Parent));

        // When — le restaurateur s'exécute au démarrage.
        var session = new SessionPlanning();
        await new RestaurateurSession(persistance).RestaurerAsync(session);

        // Then — aucune session n'est ouverte (jeton invalide = pas de session fantôme).
        Assert.False(session.EstConnecte);
    }

    /// <summary>Double À LA MAIN du seul port <see cref="IPersistanceSession"/> : mime le stockage durable
    /// client (localStorage) sans navigateur. La persistance passe par le chemin réel <c>PersisterAsync</c>
    /// (amorçage convergent), la relecture par <c>LireAsync</c>.</summary>
    private sealed class FakePersistanceSession : IPersistanceSession
    {
        private SessionPersistee? _stocke;

        public ValueTask PersisterAsync(SessionPersistee jeton)
        {
            _stocke = jeton;
            return ValueTask.CompletedTask;
        }

        public ValueTask<SessionPersistee?> LireAsync() => ValueTask.FromResult(_stocke);
    }
}
