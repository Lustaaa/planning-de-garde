using System.Linq;
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
    /// <summary>Vue d'un acteur du foyer énumérée pour l'écran de configuration : identifiant stable
    /// (clé de résolution) + nom d'affichage courant + couleur courante, tous deux résolus sur cet
    /// identifiant (couleur neutre par contrat si l'acteur n'en a pas) — pour que la liste de
    /// configuration affiche le nom ET sa pastille de couleur, cohérente avec la grille. Le
    /// <see cref="Type"/> (Admin / Parent / Autre) est surfacé en LECTURE SEULE depuis le seed (D3,
    /// sprint 14) pour piloter le rôle de l'identité effective lors d'une impersonation bornée.</summary>
    public sealed record ActeurFoyerVue(string Id, string Nom, string Couleur, TypeActeur Type);

    /// <summary>Vue d'une période couvrant une date, pour alimenter les dialogs de suppression et d'édition :
    /// identifiant stable (clé, jamais le libellé), identifiant stable du responsable (pour pré-sélectionner
    /// l'édition), nom d'affichage du responsable résolu sur l'id, et bornes datées. Lecture seule — ne
    /// déclenche jamais la diffusion.</summary>
    public sealed record PeriodeDuJourVue(string Id, string ResponsableId, string ResponsableNom, DateTime Debut, DateTime Fin);

    public static IEndpointRouteBuilder MapperCanalLecture(this IEndpointRouteBuilder routes)
    {
        // Énumération des acteurs du foyer DEPUIS LE STORE (et non la liste statique front
        // Foyer.ActeursEditables) : l'écran de configuration la lit pour faire apparaître un acteur
        // fraîchement ajouté (Sc.1). Le nom est résolu sur l'identifiant stable (jamais le libellé).
        routes.MapGet("/api/foyer/acteurs",
            (IEnumerationActeursFoyer enumeration, IReferentielResponsables referentiel, IPaletteCouleurs palette) =>
            {
                var acteurs = enumeration.EnumererActeurs()
                    .Select(id => new ActeurFoyerVue(id, referentiel.NomDe(id), palette.CouleurDe(id), enumeration.TypeDe(id)))
                    .ToList();
                return Results.Ok(acteurs);
            });

        // Grille projetée à une ANCRE (date de référence / date naviguée), passée en segments yyyy/MM/dd
        // pour le déterminisme côté front (jamais DateTime.Now côté serveur), plus un paramètre de VUE
        // (span : semaine / 4semaines / mois) sur le canal de LECTURE (CQRS — ne déclenche jamais la
        // diffusion, Sc.1/Sc.2). Compatibilité ascendante : sans vue → défaut 4 semaines glissantes
        // (Sc.3). Renvoie le read model GrilleAgenda (records framework-free de l'Application) en JSON.
        routes.MapGet("/api/grille/{annee:int}/{mois:int}/{jour:int}",
            (int annee, int mois, int jour, string? vue, GrilleAgendaQuery projection) =>
            {
                var grille = projection.Projeter(new DateOnly(annee, mois, jour), VueDepuis(vue));
                return Results.Ok(grille);
            });

        // Périodes COUVRANT une date (canal de lecture, CQRS) — alimente la dialog de suppression du menu
        // clic-case. Chaque période est rendue avec son identifiant stable, le nom du responsable résolu
        // sur l'identifiant (jamais le libellé brut) et ses bornes. Ne déclenche jamais la diffusion.
        routes.MapGet("/api/periodes/{annee:int}/{mois:int}/{jour:int}",
            (int annee, int mois, int jour, PeriodesDuJourQuery periodes, IReferentielResponsables referentiel) =>
            {
                var vues = periodes.Lister(new DateOnly(annee, mois, jour))
                    .Select(p => new PeriodeDuJourVue(p.Id, p.ResponsableId, referentiel.NomDe(p.ResponsableId), p.Debut, p.Fin))
                    .ToList();
                return Results.Ok(vues);
            });

        return routes;
    }

    /// <summary>Résout le code de vue (paramètre de lecture) en <see cref="VuePlanning"/> : <c>semaine</c>
    /// / <c>mois</c>, sinon <see cref="VuePlanning.QuatreSemaines"/> par défaut (et compat ascendante :
    /// absence de vue → 4 semaines glissantes, Sc.3).</summary>
    private static VuePlanning VueDepuis(string? code) => code switch
    {
        "semaine" => VuePlanning.Semaine,
        "mois" => VuePlanning.Mois,
        _ => VuePlanning.QuatreSemaines,
    };
}
