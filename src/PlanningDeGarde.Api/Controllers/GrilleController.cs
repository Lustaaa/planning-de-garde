namespace PlanningDeGarde.Api.Controllers;

/// <summary>
/// Ressource <b>Grille</b> (BC Planning) — controller MVC REST de LECTURE (CQRS). Projette la grille
/// agenda à une ANCRE (segments yyyy/MM/dd, déterminisme côté front) selon une VUE (semaine / 4 semaines /
/// mois). Paramètre optionnel « enfant » : isolation multi-enfants. Lecture seule — jamais de diffusion.
/// </summary>
[ApiController]
public sealed class GrilleController(GrilleAgendaQuery projection) : ControllerBase
{
    /// <summary>Grille projetée à une ancre datée (lecture seule). Sans vue → défaut 4 semaines glissantes.</summary>
    [HttpGet("/api/grille/{annee:int}/{mois:int}/{jour:int}")]
    public IActionResult Projeter(int annee, int mois, int jour, [FromQuery] string? vue, [FromQuery] string? enfant)
    {
        var grille = projection.Projeter(
            new DateOnly(annee, mois, jour), VueDepuis(vue), string.IsNullOrWhiteSpace(enfant) ? null : enfant);
        return Ok(grille);
    }

    private static VuePlanning VueDepuis(string? code) => code switch
    {
        "semaine" => VuePlanning.Semaine,
        "mois" => VuePlanning.Mois,
        _ => VuePlanning.QuatreSemaines,
    };
}
