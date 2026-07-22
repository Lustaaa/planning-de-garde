namespace PlanningDeGarde.Api.Controllers;

/// <summary>
/// Ressource <b>Propositions d'échange</b> (BC Echanges) — controller MVC REST. Proposer n'écrit AUCUNE
/// surcharge (canal de consentement) mais DIFFUSE la proposition (payload) à la cloche du recevant ;
/// accepter COMPOSE la délégation (+ diffusion MiseAJour) ; refuser clôt sans écriture. La diffusion
/// suit une écriture aboutie, jamais l'inverse.
/// </summary>
[ApiController]
public sealed class PropositionsController(
    ProposerEchangeHandler proposer,
    ProposerEchangeSuiteImprevuHandler proposerSuiteImprevu,
    AccepterPropositionHandler accepter,
    RefuserPropositionHandler refuser,
    INotificateurChangement notificateur,
    INotificateurPlanning planning) : ControllerBase
{
    /// <summary>Proposer un échange sur une PLAGE (POST).</summary>
    [HttpPost("/api/propositions")]
    public IActionResult Proposer([FromBody] ProposerEchangeRequete requete)
    {
        var resultat = proposer.Handle(new ProposerEchangeCommand(requete.Jour, requete.EnfantId, requete.VersActeurId, requete.JourFin));
        if (!resultat.EstSucces)
            return BadRequest(resultat.Motif);

        notificateur.NotifierProposition(resultat.Valeur!);
        return Ok();
    }

    /// <summary>Action de suivi : proposer un échange en réaction à un imprévu journalisé (POST).</summary>
    [HttpPost("/api/propositions/suite-imprevu")]
    public IActionResult ProposerSuiteImprevu([FromBody] ProposerEchangeSuiteImprevuRequete requete)
    {
        var resultat = proposerSuiteImprevu.Handle(new ProposerEchangeSuiteImprevuCommand(requete.ImprevuEvenementId, requete.VersActeurId));
        if (!resultat.EstSucces)
            return BadRequest(resultat.Motif);

        notificateur.NotifierProposition(resultat.Valeur!);
        return Ok();
    }

    /// <summary>Accepter une proposition (POST) : COMPOSE la délégation + diffusion MiseAJour.</summary>
    [HttpPost("/api/propositions/{id}/acceptation")]
    public IActionResult Accepter(string id)
    {
        var resultat = accepter.Handle(new AccepterPropositionCommand(id));
        if (!resultat.EstSucces)
            return BadRequest(resultat.Motif);

        planning.NotifierMiseAJour();
        notificateur.NotifierProposition(resultat.Valeur!);
        return Ok();
    }

    /// <summary>Refuser une proposition (POST) : clôt sans aucune écriture de surcharge.</summary>
    [HttpPost("/api/propositions/{id}/refus")]
    public IActionResult Refuser(string id)
    {
        var resultat = refuser.Handle(new RefuserPropositionCommand(id));
        if (!resultat.EstSucces)
            return BadRequest(resultat.Motif);

        notificateur.NotifierProposition(resultat.Valeur!);
        return Ok();
    }
}
