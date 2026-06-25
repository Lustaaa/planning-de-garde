using PlanningDeGarde.Application;

namespace PlanningDeGarde.Web;

/// <summary>
/// Adaptateur de gauche : le canal requête/réponse d'écriture du planning. Expose les
/// commandes d'écriture comme endpoints HTTP qui invoquent les handlers <b>inchangés</b>
/// (Application/write) et renvoient un accusé succès/échec. Découple le front du back :
/// l'IHM (front côté navigateur, agent tiers) émet ses commandes ici plutôt qu'en appelant
/// les handlers en direct. N'écrit jamais par le canal de diffusion (lecture seule).
/// </summary>
public static class CanalEcriture
{
    /// <summary>Corps de la requête de pose de slot émise via le canal requête/réponse.</summary>
    public sealed record PoserSlotRequete(string EnfantId, string LieuId, DateTime Debut, DateTime Fin);

    public static IEndpointRouteBuilder MapperCanalEcriture(this IEndpointRouteBuilder routes)
    {
        routes.MapPost("/api/canal/poser-slot", (PoserSlotRequete requete, PoserSlotHandler handler) =>
        {
            var resultat = handler.Handle(new PoserSlotCommand(
                requete.EnfantId, requete.LieuId, requete.Debut, requete.Fin));

            return resultat.EstSucces
                ? Results.Ok()
                : Results.BadRequest(resultat.Motif);
        });

        return routes;
    }
}
