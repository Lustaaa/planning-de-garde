namespace PlanningDeGarde.Api.Controllers;

/// <summary>
/// Ressource <b>Cycles de fond</b> (BC CyclesDeFond) — controller MVC REST. Le cycle est clé PAR
/// ENFANT : lecture et écriture portent l'enfant en paramètre/corps, jamais dans l'URL de ressource.
/// Une nouvelle définition remplace intégralement le cycle courant de l'enfant (dernière écriture gagne).
/// </summary>
[ApiController]
public sealed class CyclesController(DefinirCycleHandler definir, CyclesFoyerQuery query) : ControllerBase
{
    /// <summary>Cycles DÉCLARÉS lus depuis le store. Paramètre optionnel « enfant » : cycle
    /// par enfant ; absent = cycle partagé/legacy (compat ascendante).</summary>
    [HttpGet("/api/foyer/cycles")]
    public IActionResult Lire([FromQuery] string? enfant)
    {
        var vues = query.Lire(string.IsNullOrWhiteSpace(enfant) ? null : enfant)
            .Select(c => new CycleFoyerVue(c.IndexSemaine, c.ResponsableId))
            .ToList();
        return Ok(vues);
    }

    /// <summary>Définition / ré-édition du cycle de fond (PUT). Refus métier (N &lt; 1) avec motif.</summary>
    [HttpPut("/api/foyer/cycles")]
    public IActionResult Definir([FromBody] DefinirCycleRequete requete)
    {
        var resultat = definir.Handle(new DefinirCycleCommand(requete.NombreSemaines, requete.Affectations, requete.EnfantId));
        return resultat.EstSucces ? Ok() : BadRequest(resultat.Motif);
    }
}
