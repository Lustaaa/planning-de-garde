using System.Threading.Tasks;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.Web;

/// <summary>
/// Jeton de session persisté côté client (volet 1 Login F5, s31) : l'identité du compte connecté
/// re-hydratable au démarrage (id d'acteur stable + nom d'affichage + type). C'est le strict minimum
/// pour ré-ouvrir la même session sans repasser par le flux de connexion. Aucune donnée domaine, aucun
/// secret : ce n'est que l'ancrage d'identité déjà résolu serveur à la connexion (s23/s25).
/// </summary>
public sealed record SessionPersistee(string ActeurId, string Nom, TypeActeur Type);

/// <summary>
/// Port de persistance durable de la session côté client (s31, volet 1). Distinct de
/// <see cref="State.SessionPlanning"/> qui reste en MÉMOIRE (borne anti-cliquet R30) : ce port ne fait que
/// persister/relire le jeton d'amorçage à travers un stockage qui survit au rechargement (F5). Adaptateur de
/// bord (JS localStorage) — jamais doublé en dehors du test ; côté test, on double ce port à la main.
/// </summary>
public interface IPersistanceSession
{
    /// <summary>Persiste le jeton de session (à la connexion) dans le stockage durable client.</summary>
    ValueTask PersisterAsync(SessionPersistee jeton);

    /// <summary>Relit le jeton persisté (au démarrage), ou <c>null</c> si le stockage est vide.</summary>
    ValueTask<SessionPersistee?> LireAsync();
}
