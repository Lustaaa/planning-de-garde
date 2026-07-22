namespace PlanningDeGarde.Api.Controllers;

/// <summary>
/// Ressource <b>Admins du foyer</b> (BC Foyer, s22/s41) — controller MVC REST. Désigner (PUT {acteurId})
/// exige un parent (invariant Domain) ; dé-désigner (DELETE {acteurId}) respecte la borne « dernier admin ».
/// Diffusion temps réel (lecture seule) sur succès.
/// </summary>
[ApiController]
public sealed class AdminsController(
    DesignerAdminHandler designer,
    DeDesignerAdminHandler deDesigner,
    IEnumerationAdminsFoyer admins,
    INotificateurPlanning notificateur) : ControllerBase
{
    /// <summary>Énumération des ids d'acteurs admins depuis le store (lecture seule).</summary>
    [HttpGet("/api/foyer/admins")]
    public IActionResult Lister() => Ok(admins.EnumererAdmins().ToList());

    /// <summary>Désignation d'un acteur comme admin (PUT). Invariant admin=parent tranché côté Domain.</summary>
    [HttpPut("/api/foyer/admins/{acteurId}")]
    public IActionResult Designer(string acteurId)
    {
        var resultat = designer.Handle(new DesignerAdminCommand(acteurId));
        if (!resultat.EstSucces)
            return BadRequest(resultat.Motif);

        notificateur.NotifierMiseAJour();
        return Ok();
    }

    /// <summary>Dé-désignation d'un admin (DELETE, s41) : borne « dernier admin » tranchée côté Domain.</summary>
    [HttpDelete("/api/foyer/admins/{acteurId}")]
    public IActionResult DeDesigner(string acteurId)
    {
        var resultat = deDesigner.Handle(new DeDesignerAdminCommand(acteurId));
        if (!resultat.EstSucces)
            return BadRequest(resultat.Motif);

        notificateur.NotifierMiseAJour();
        return Ok();
    }
}
