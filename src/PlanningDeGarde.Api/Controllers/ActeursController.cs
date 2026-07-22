namespace PlanningDeGarde.Api.Controllers;

/// <summary>
/// Ressource <b>Acteurs du foyer</b> (BC Foyer) — controller MVC REST. CRUD des acteurs + affectation
/// du rôle du référentiel (sous-ressource <c>/role</c>). Écritures via handlers inchangés ; certaines
/// déclenchent la diffusion temps réel (lecture seule) sur succès.
/// </summary>
[ApiController]
public sealed class ActeursController(
    AjouterActeurHandler ajouter,
    EditerActeurHandler editer,
    SupprimerActeurHandler supprimer,
    AffecterRoleActeurHandler affecterRole,
    RetirerRoleActeurHandler retirerRole,
    IEnumerationActeursFoyer enumeration,
    IReferentielResponsables referentiel,
    IPaletteCouleurs palette) : ControllerBase
{
    /// <summary>Énumération des acteurs du foyer depuis le store (lecture seule).</summary>
    [HttpGet("/api/foyer/acteurs")]
    public IActionResult Lister()
    {
        var acteurs = enumeration.EnumererActeurs()
            .Select(id => new ActeurFoyerVue(
                id, referentiel.NomDe(id), palette.CouleurDe(id), enumeration.TypeDe(id), enumeration.RoleDe(id), enumeration.AdresseDe(id)))
            .ToList();
        return Ok(acteurs);
    }

    /// <summary>Ajout d'un acteur neuf (POST). L'id stable neuf est généré côté handler.</summary>
    [HttpPost("/api/foyer/acteurs")]
    public IActionResult Ajouter([FromBody] AjouterActeurRequete requete)
    {
        var resultat = ajouter.Handle(new AjouterActeurCommand(requete.Nom, requete.Couleur));
        return resultat.EstSucces ? Ok() : BadRequest(resultat.Motif);
    }

    /// <summary>Édition d'un acteur par id (PUT). Nom / couleur / adresse optionnels et indépendants.</summary>
    [HttpPut("/api/foyer/acteurs/{id}")]
    public IActionResult Editer(string id, [FromBody] EditerActeurCorps corps)
    {
        var resultat = editer.Handle(new EditerActeurCommand(id, corps.Nom, corps.Couleur, corps.Adresse));
        return resultat.EstSucces ? Ok() : BadRequest(resultat.Motif);
    }

    /// <summary>Suppression d'un acteur par son identifiant stable (idempotente côté handler).</summary>
    [HttpDelete("/api/foyer/acteurs/{id}")]
    public IActionResult Supprimer(string id)
    {
        var resultat = supprimer.Handle(new SupprimerActeurCommand(id));
        return resultat.EstSucces ? Ok() : BadRequest(resultat.Motif);
    }

    /// <summary>Affectation d'un rôle du référentiel à un acteur (PUT sous-ressource /role, s21).</summary>
    [HttpPut("/api/foyer/acteurs/{id}/role")]
    public IActionResult AffecterRole(string id, [FromBody] AffecterRoleCorps corps)
    {
        var resultat = affecterRole.Handle(new AffecterRoleActeurCommand(id, corps.RoleId));
        return resultat.EstSucces ? Ok() : BadRequest(resultat.Motif);
    }

    /// <summary>Retrait du rôle d'un acteur (DELETE sous-ressource /role, s21) : retombe « sans rôle ».</summary>
    [HttpDelete("/api/foyer/acteurs/{id}/role")]
    public IActionResult RetirerRole(string id)
    {
        var resultat = retirerRole.Handle(new RetirerRoleActeurCommand(id));
        return resultat.EstSucces ? Ok() : BadRequest(resultat.Motif);
    }
}
