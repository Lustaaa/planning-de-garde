namespace PlanningDeGarde.Api.Controllers;

/// <summary>
/// Ressource <b>Rôles du référentiel</b> (BC Foyer, s21/s36) — controller MVC REST. CRUD des rôles +
/// bascule du flag « est rôle parent » (sous-ressource <c>/parent</c>, source de vérité de l'éligibilité
/// au lien enfant↔parent). Écritures via handlers inchangés ; diffusion temps réel sur le flag parent.
/// </summary>
[ApiController]
public sealed class RolesController(
    CreerRoleHandler creer,
    RenommerRoleHandler renommer,
    SupprimerRoleHandler supprimer,
    MarquerRoleParentHandler marquerParent,
    IEnumerationRoles roles,
    INotificateurPlanning notificateur) : ControllerBase
{
    /// <summary>Énumération des rôles du référentiel depuis le store (lecture seule).</summary>
    [HttpGet("/api/foyer/roles")]
    public IActionResult Lister()
    {
        var vues = roles.EnumererRoles()
            .Select(r => new RoleFoyerVue(r.Id, r.Libelle, r.EstRoleParent))
            .ToList();
        return Ok(vues);
    }

    /// <summary>Création d'un rôle (POST). L'id stable neuf est généré côté handler.</summary>
    [HttpPost("/api/foyer/roles")]
    public IActionResult Creer([FromBody] CreerRoleRequete requete)
    {
        var resultat = creer.Handle(new CreerRoleCommand(requete.Libelle));
        return resultat.EstSucces ? Ok() : BadRequest(resultat.Motif);
    }

    /// <summary>Renommage d'un rôle par id (PUT).</summary>
    [HttpPut("/api/foyer/roles/{id}")]
    public IActionResult Renommer(string id, [FromBody] RenommerRoleCorps corps)
    {
        var resultat = renommer.Handle(new RenommerRoleCommand(id, corps.NouveauLibelle));
        return resultat.EstSucces ? Ok() : BadRequest(resultat.Motif);
    }

    /// <summary>Suppression d'un rôle par id (DELETE) : ses porteurs retombent « sans rôle ». Idempotente.</summary>
    [HttpDelete("/api/foyer/roles/{id}")]
    public IActionResult Supprimer(string id)
    {
        var resultat = supprimer.Handle(new SupprimerRoleCommand(id));
        return resultat.EstSucces ? Ok() : BadRequest(resultat.Motif);
    }

    /// <summary>Bascule du flag « est rôle parent » (PUT sous-ressource /parent, s36) + diffusion temps réel.</summary>
    [HttpPut("/api/foyer/roles/{id}/parent")]
    public IActionResult MarquerParent(string id, [FromBody] MarquerRoleParentCorps corps)
    {
        var resultat = marquerParent.Handle(new MarquerRoleParentCommand(id, corps.EstParent));
        if (!resultat.EstSucces)
            return BadRequest(resultat.Motif);

        notificateur.NotifierMiseAJour();
        return Ok();
    }
}
