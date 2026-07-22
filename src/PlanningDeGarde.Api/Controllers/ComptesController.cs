namespace PlanningDeGarde.Api.Controllers;

/// <summary>
/// Ressource <b>Comptes</b> (BC Comptes, s22/s24/s28/s41) — controller MVC REST. Énumération + création
/// de comptes, activation/désactivation (sous-ressource <c>/activation</c>), pose de mot de passe
/// (sous-ressource <c>/mot-de-passe</c>) et flux mot-de-passe-oublié (<c>/api/comptes/recuperation</c> +
/// <c>/reinitialisation</c>). Écritures via handlers inchangés ; certaines diffusent (lecture seule).
/// </summary>
[ApiController]
public sealed class ComptesController(
    CreerCompteHandler creer,
    ActiverCompteHandler activer,
    DesactiverCompteHandler desactiver,
    DefinirMotDePasseHandler definirMotDePasse,
    DemanderRecuperationMotDePasseHandler demanderRecuperation,
    RedefinirMotDePasseHandler redefinir,
    IEnumerationComptes comptes,
    INotificateurPlanning notificateur) : ControllerBase
{
    /// <summary>Énumération des comptes du foyer depuis le store (lecture seule, statut minuscule stable).</summary>
    [HttpGet("/api/foyer/comptes")]
    public IActionResult Lister()
    {
        var vues = comptes.EnumererComptes()
            .Select(c => new CompteFoyerVue(c.Id, c.Email, c.Statut.ToString().ToLowerInvariant(), c.ActeurId))
            .ToList();
        return Ok(vues);
    }

    /// <summary>Création / association d'un compte à un acteur (POST, s22) + diffusion temps réel.</summary>
    [HttpPost("/api/foyer/comptes")]
    public IActionResult Creer([FromBody] CreerCompteRequete requete)
    {
        var resultat = creer.Handle(new CreerCompteCommand(requete.Email, requete.ActeurId));
        if (!resultat.EstSucces)
            return BadRequest(resultat.Motif);

        notificateur.NotifierMiseAJour();
        return Ok();
    }

    /// <summary>Activation d'un compte (POST sous-ressource /activation, s24) + diffusion temps réel.</summary>
    [HttpPost("/api/foyer/comptes/{id}/activation")]
    public IActionResult Activer(string id)
    {
        var resultat = activer.Handle(new ActiverCompteCommand(id));
        if (!resultat.EstSucces)
            return BadRequest(resultat.Motif);

        notificateur.NotifierMiseAJour();
        return Ok();
    }

    /// <summary>Désactivation d'un compte (DELETE sous-ressource /activation, s41) + diffusion temps réel.</summary>
    [HttpDelete("/api/foyer/comptes/{id}/activation")]
    public IActionResult Desactiver(string id)
    {
        var resultat = desactiver.Handle(new DesactiverCompteCommand(id));
        if (!resultat.EstSucces)
            return BadRequest(resultat.Motif);

        notificateur.NotifierMiseAJour();
        return Ok();
    }

    /// <summary>Pose du mot de passe d'un compte (PUT sous-ressource /mot-de-passe, s28) — haché côté serveur.</summary>
    [HttpPut("/api/foyer/comptes/{id}/mot-de-passe")]
    public IActionResult DefinirMotDePasse(string id, [FromBody] DefinirMotDePasseCorps corps)
    {
        var resultat = definirMotDePasse.Handle(new DefinirMotDePasseCommand(id, corps.MotDePasse));
        return resultat.EstSucces ? Ok() : BadRequest(resultat.Motif);
    }

    /// <summary>Demande de récupération de mot de passe (POST, s28) : réponse TOUJOURS neutre (anti-énumération).</summary>
    [HttpPost("/api/comptes/recuperation")]
    public IActionResult DemanderRecuperation([FromBody] DemanderRecuperationRequete requete)
    {
        demanderRecuperation.Handle(new DemanderRecuperationMotDePasseCommand(requete.Email));
        return Ok();
    }

    /// <summary>Redéfinition de mot de passe par jeton (POST, s28) : jeton usage unique consommé sur succès.</summary>
    [HttpPost("/api/comptes/reinitialisation")]
    public IActionResult Reinitialiser([FromBody] RedefinirMotDePasseRequete requete)
    {
        var resultat = redefinir.Handle(new RedefinirMotDePasseCommand(requete.Jeton, requete.NouveauMotDePasse));
        return resultat.EstSucces ? Ok() : BadRequest(resultat.Motif);
    }
}
