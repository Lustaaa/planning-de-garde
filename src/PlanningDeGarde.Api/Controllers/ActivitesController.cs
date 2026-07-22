namespace PlanningDeGarde.Api.Controllers;

/// <summary>
/// Ressource <b>Activités du foyer</b> (BC Activites, ex-« lieux » s27→s35) — controller MVC REST. CRUD des
/// activités + liaison d'un enfant (sous-ressource <c>/enfants/{enfantId}</c>). Écritures via handlers
/// inchangés + diffusion temps réel (lecture seule) sur succès.
/// </summary>
[ApiController]
public sealed class ActivitesController(
    AjouterActiviteHandler ajouter,
    EditerActiviteHandler editer,
    SupprimerActiviteHandler supprimer,
    LierEnfantActiviteHandler lierEnfant,
    DelierEnfantActiviteHandler delierEnfant,
    IEnumerationActivites activites,
    INotificateurPlanning notificateur) : ControllerBase
{
    /// <summary>Énumération des activités du référentiel depuis le store vivant (lecture seule).</summary>
    [HttpGet("/api/foyer/activites")]
    public IActionResult Lister()
    {
        var vues = activites.EnumererActivites()
            .Select(a => new ActiviteFoyerVue(a.Id, a.Libelle, a.Adresse, a.EnfantsLies))
            .ToList();
        return Ok(vues);
    }

    /// <summary>Ajout d'une activité (POST, s35). L'id stable neuf est posé côté handler.</summary>
    [HttpPost("/api/foyer/activites")]
    public IActionResult Ajouter([FromBody] AjouterActiviteRequete requete)
    {
        var resultat = ajouter.Handle(new AjouterActiviteCommand(requete.Libelle));
        return Diffuser(resultat);
    }

    /// <summary>Édition d'une activité par id (PUT, s35) : libellé + adresse optionnels indépendants.</summary>
    [HttpPut("/api/foyer/activites/{id}")]
    public IActionResult Editer(string id, [FromBody] EditerActiviteCorps corps)
    {
        var resultat = editer.Handle(new EditerActiviteCommand(id, corps.Libelle, corps.Adresse));
        return Diffuser(resultat);
    }

    /// <summary>Suppression d'une activité par id (DELETE, s35). Idempotente ; slots déjà posés conservent leur lieu.</summary>
    [HttpDelete("/api/foyer/activites/{id}")]
    public IActionResult Supprimer(string id)
    {
        var resultat = supprimer.Handle(new SupprimerActiviteCommand(id));
        return Diffuser(resultat);
    }

    /// <summary>Liaison d'un enfant à une activité (PUT sous-ressource, s35). Lien N-M ; déjà lié = neutre.</summary>
    [HttpPut("/api/foyer/activites/{id}/enfants/{enfantId}")]
    public IActionResult LierEnfant(string id, string enfantId)
    {
        var resultat = lierEnfant.Handle(new LierEnfantActiviteCommand(enfantId, id));
        return Diffuser(resultat);
    }

    /// <summary>Retrait du lien enfant↔activité (DELETE sous-ressource, s35). Idempotent côté handler.</summary>
    [HttpDelete("/api/foyer/activites/{id}/enfants/{enfantId}")]
    public IActionResult DelierEnfant(string id, string enfantId)
    {
        var resultat = delierEnfant.Handle(new DelierEnfantActiviteCommand(enfantId, id));
        return Diffuser(resultat);
    }

    private IActionResult Diffuser<T>(Result<T> resultat)
    {
        if (!resultat.EstSucces)
            return BadRequest(resultat.Motif);

        notificateur.NotifierMiseAJour();
        return Ok();
    }
}
