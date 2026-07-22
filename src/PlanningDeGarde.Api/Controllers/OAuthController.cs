namespace PlanningDeGarde.Api.Controllers;

/// <summary>
/// Ressource <b>OAuth externe</b> (BC Comptes) — controller MVC. <c>GET api/oauth/google/demarrer</c>
/// démarre le flux (redirection vers l'authorize Google, URL publique construite depuis la config — aucun
/// secret) ; <c>GET api/oauth/google/callback</c> route le retour du provider vers <see cref="ConnexionOAuthHandler"/>
/// (même chemin de session que la connexion locale).
///
/// <para><b>Dette de câblage (P0, G3)</b> : l'échange du code contre l'identité vérifiée (Google) n'est pas
/// réalisé en runtime local — le port est un placeholder. La logique de rapprochement est prouvée par doublure.</para>
/// </summary>
[ApiController]
public sealed class OAuthController(ConnexionOAuthHandler handler, IConfiguration config) : ControllerBase
{
    /// <summary>Démarrage du flux « authorization code » : redirection 302 vers l'authorize Google.</summary>
    [HttpGet("/api/oauth/google/demarrer")]
    public IActionResult Demarrer()
    {
        var clientId = config["OAuth:Google:ClientId"] ?? "google-client-id-a-configurer";
        var redirectUri = config["OAuth:Google:RedirectUri"] ?? "http://localhost:5180/api/oauth/google/callback";
        var authorize = "https://accounts.google.com/o/oauth2/v2/auth"
            + $"?client_id={Uri.EscapeDataString(clientId)}"
            + $"&redirect_uri={Uri.EscapeDataString(redirectUri)}"
            + "&response_type=code"
            + $"&scope={Uri.EscapeDataString("openid email")}";
        return Redirect(authorize);
    }

    /// <summary>Callback du provider routé vers ConnexionOAuthHandler : ouvre ou refuse la session (chemin),
    /// puis redirige le navigateur vers le planning (succès) ou la connexion (échec).</summary>
    [HttpGet("/api/oauth/google/callback")]
    public IActionResult Callback([FromQuery] string? code)
    {
        var frontOrigine = config["Front:Origine"] ?? "http://localhost:5292";
        var resultat = handler.Handle(new CallbackOAuthCommand(code ?? string.Empty));
        return resultat.EstSucces
            ? Redirect($"{frontOrigine}/planning")
            : Redirect($"{frontOrigine}/connexion?oauth=echec");
    }
}
