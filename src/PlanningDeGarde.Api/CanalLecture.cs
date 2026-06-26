using PlanningDeGarde.Application;

namespace PlanningDeGarde.Api;

/// <summary>
/// Adaptateur de lecture (CQRS) porté sur l'hôte d'API détaché : expose la projection
/// <see cref="GrilleAgendaQuery"/> de la grille agenda comme endpoint HTTP que le front
/// <b>WASM</b> consomme à distance (le navigateur n'a pas accès au store ni à la projection
/// en DI directe — il lit la grille via l'API distante, comme il y écrit via le canal
/// d'écriture). Lecture seule : n'écrit jamais, ne déclenche jamais la diffusion.
/// </summary>
public static class CanalLecture
{
    public static IEndpointRouteBuilder MapperCanalLecture(this IEndpointRouteBuilder routes)
    {
        // Grille projetée à une date de référence (« aujourd'hui »), passée en segments yyyy/MM/dd
        // pour le déterminisme côté front (jamais DateTime.Now côté serveur). Renvoie le read model
        // GrilleAgenda (records framework-free de l'Application), sérialisé en JSON.
        routes.MapGet("/api/grille/{annee:int}/{mois:int}/{jour:int}",
            (int annee, int mois, int jour, GrilleAgendaQuery projection) =>
            {
                var grille = projection.Projeter(new DateOnly(annee, mois, jour));
                return Results.Ok(grille);
            });

        return routes;
    }
}
