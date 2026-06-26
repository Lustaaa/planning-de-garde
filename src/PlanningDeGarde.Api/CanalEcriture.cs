using PlanningDeGarde.Application;

namespace PlanningDeGarde.Api;

/// <summary>
/// Adaptateur de gauche : le canal requête/réponse d'écriture du planning, porté sur l'hôte
/// d'API détaché (<see cref="ApiProgram"/>). Expose les commandes d'écriture comme endpoints
/// HTTP qui invoquent les handlers <b>inchangés</b> (Application/write) et renvoient un accusé
/// succès/échec. Le front (WASM, agent tiers) émet ses commandes ici à distance plutôt qu'en
/// appelant les handlers en direct. N'écrit jamais par le canal de diffusion (lecture seule).
/// </summary>
public static class CanalEcriture
{
    /// <summary>Corps de la requête de pose de slot émise via le canal requête/réponse.</summary>
    public sealed record PoserSlotRequete(string EnfantId, string LieuId, DateTime Debut, DateTime Fin);

    /// <summary>Corps de la requête d'affectation de période émise via le canal requête/réponse.</summary>
    public sealed record AffecterPeriodeRequete(string ResponsableId, DateTime Debut, DateTime Fin);

    public static IEndpointRouteBuilder MapperCanalEcriture(this IEndpointRouteBuilder routes)
    {
        routes.MapPost("/api/canal/poser-slot", (PoserSlotRequete requete, PoserSlotHandler handler) =>
        {
            var resultat = handler.Handle(new PoserSlotCommand(
                requete.EnfantId, requete.LieuId, requete.Debut, requete.Fin));

            // Le canal propage l'issue du handler : succès acquitté, refus métier renvoyé avec son motif.
            return resultat.EstSucces
                ? Results.Ok()
                : Results.BadRequest(resultat.Motif);
        });

        routes.MapPost("/api/canal/affecter-periode", (AffecterPeriodeRequete requete, AffecterPeriodeHandler handler) =>
        {
            var resultat = handler.Handle(new AffecterPeriodeCommand(
                requete.ResponsableId, requete.Debut, requete.Fin));

            // Même convention que la pose : succès acquitté, refus métier renvoyé avec son motif.
            return resultat.EstSucces
                ? Results.Ok()
                : Results.BadRequest(resultat.Motif);
        });

        return routes;
    }
}
