namespace PlanningDeGarde.Api.Controllers;

/// <summary>
/// Ressource <b>Enfants du foyer</b> (BC Enfants) — controller MVC REST. CRUD des enfants + liaison à un
/// parent-acteur (sous-ressource <c>/parents/{acteurId}</c>). La diffusion temps réel de l'ajout /
/// édition est déclenchée PAR LE HANDLER ; la liaison/déliaison de parent notifie ici (lecture seule).
/// </summary>
[ApiController]
public sealed class EnfantsController(
    AjouterEnfantHandler ajouter,
    EditerEnfantHandler editer,
    LierEnfantParentHandler lierParent,
    DelierEnfantParentHandler delierParent,
    IEnumerationEnfants enfants,
    INotificateurPlanning notificateur) : ControllerBase
{
    /// <summary>Énumération des enfants du foyer depuis le store (lecture seule).</summary>
    [HttpGet("/api/foyer/enfants")]
    public IActionResult Lister()
    {
        var vues = enfants.EnumererEnfants()
            .Select(e => new EnfantFoyerVue(e.Id, e.Prenom, e.ParentsLies))
            .ToList();
        return Ok(vues);
    }

    /// <summary>Ajout d'un enfant (POST). L'id stable opaque neuf est généré côté handler.</summary>
    [HttpPost("/api/foyer/enfants")]
    public IActionResult Ajouter([FromBody] AjouterEnfantRequete requete)
    {
        var resultat = ajouter.Handle(new AjouterEnfantCommand(requete.Prenom));
        return resultat.EstSucces ? Ok() : BadRequest(resultat.Motif);
    }

    /// <summary>Édition du prénom d'un enfant par id (PUT).</summary>
    [HttpPut("/api/foyer/enfants/{id}")]
    public IActionResult Editer(string id, [FromBody] EditerEnfantCorps corps)
    {
        var resultat = editer.Handle(new EditerEnfantCommand(id, corps.NouveauPrenom));
        return resultat.EstSucces ? Ok() : BadRequest(resultat.Motif);
    }

    /// <summary>Liaison d'un enfant à un parent-acteur (PUT sous-ressource) : rôle-du-lien en corps.
    /// Déjà lié = maj du rôle. Diffusion temps réel sur succès.</summary>
    [HttpPut("/api/foyer/enfants/{id}/parents/{acteurId}")]
    public IActionResult LierParent(string id, string acteurId, [FromBody] LierEnfantParentCorps corps)
    {
        var resultat = lierParent.Handle(new LierEnfantParentCommand(id, acteurId, corps.Role));
        if (!resultat.EstSucces)
            return BadRequest(resultat.Motif);

        notificateur.NotifierMiseAJour();
        return Ok();
    }

    /// <summary>Retrait du lien enfant↔parent (DELETE sous-ressource). Idempotent côté handler.</summary>
    [HttpDelete("/api/foyer/enfants/{id}/parents/{acteurId}")]
    public IActionResult DelierParent(string id, string acteurId)
    {
        var resultat = delierParent.Handle(new DelierEnfantParentCommand(id, acteurId));
        if (!resultat.EstSucces)
            return BadRequest(resultat.Motif);

        notificateur.NotifierMiseAJour();
        return Ok();
    }
}
