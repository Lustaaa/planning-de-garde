using System.Threading.Tasks;
using PlanningDeGarde.Web.State;

namespace PlanningDeGarde.Web;

/// <summary>
/// Restaure la session de consultation au démarrage du client (volet 1 Login F5, s31) : relit le jeton
/// persisté via <see cref="IPersistanceSession"/> et, s'il est valide, ré-ouvre la session en mémoire
/// (<see cref="SessionPlanning.Connecter"/>) SANS repasser par le flux de connexion. Un jeton absent ou
/// invalide n'ouvre AUCUNE session (pas de session fantôme). Ne persiste rien de neuf : il ne fait que
/// rejouer l'amorçage d'identité déjà résolu serveur, la session reste en mémoire (borne R30 tenue).
/// </summary>
public sealed class RestaurateurSession
{
    private readonly IPersistanceSession _persistance;

    public RestaurateurSession(IPersistanceSession persistance) => _persistance = persistance;

    public async ValueTask RestaurerAsync(SessionPlanning session)
    {
        var jeton = await _persistance.LireAsync();

        // Jeton absent (stockage vide) ou invalide (acteur/nom manquant = jeton corrompu) : aucune session
        // n'est ouverte (pas de session fantôme). La garde de validité est forcée par le cas « invalide » (Sc.2).
        if (jeton is null || string.IsNullOrWhiteSpace(jeton.ActeurId) || string.IsNullOrWhiteSpace(jeton.Nom))
            return;

        // Jeton valide : ré-ouvre la session en ancrant l'identité réelle sur l'acteur du jeton, comme le ferait
        // la connexion (s23/s25) — sans repasser par le flux de connexion.
        session.Connecter(jeton.Nom, jeton.ActeurId, jeton.Type);
    }
}
