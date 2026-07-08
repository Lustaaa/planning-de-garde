using PlanningDeGarde.Application;

namespace PlanningDeGarde.Api;

/// <summary>
/// Adaptateur de gauche : les endpoints du <b>flux OAuth externe</b> (s28, volet 3) portés sur l'hôte
/// d'API détaché. <c>GET api/oauth/google/demarrer</c> démarre le flux en redirigeant le navigateur vers
/// l'<b>authorize</b> du provider Google (URL publique construite depuis la configuration — aucun secret) ;
/// <c>GET api/oauth/google/callback</c> reçoit le retour du provider et le route vers
/// <see cref="ConnexionOAuthHandler"/> (même chemin de session que la connexion locale s23), qui OUVRE ou
/// REFUSE la session selon l'identité restituée par le port <see cref="IFournisseurOAuth"/>.
///
/// <para><b>Dette de câblage (P0, G3)</b> : l'échange du <c>code</c> contre l'identité vérifiée
/// (client_secret / token endpoint Google) n'est pas réalisé en runtime local — le port est un placeholder
/// qui ne résout aucune identité tant que l'adaptateur Google réel n'est pas branché. La logique de
/// rapprochement est prouvée par doublure (S9) ; le provider réel est vérifié MANUELLEMENT au G3.</para>
/// </summary>
public static class OAuthEndpoints
{
    public static IEndpointRouteBuilder MapperOAuth(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/api/oauth/google/demarrer", (IConfiguration config) =>
        {
            // Construction de l'URL authorize Google (flux « authorization code ») depuis la configuration :
            // client_id + redirect_uri + scope sont publics ; le client_secret n'intervient qu'à l'échange
            // du code (callback), non ici. Redirection 302 = démarrage du flux côté navigateur.
            var clientId = config["OAuth:Google:ClientId"] ?? "google-client-id-a-configurer";
            var redirectUri = config["OAuth:Google:RedirectUri"] ?? "http://localhost:5180/api/oauth/google/callback";
            var authorize = "https://accounts.google.com/o/oauth2/v2/auth"
                + $"?client_id={Uri.EscapeDataString(clientId)}"
                + $"&redirect_uri={Uri.EscapeDataString(redirectUri)}"
                + "&response_type=code"
                + $"&scope={Uri.EscapeDataString("openid email")}";
            return Results.Redirect(authorize);
        });

        routes.MapGet("/api/oauth/google/callback", (string? code, ConnexionOAuthHandler handler, IConfiguration config) =>
        {
            // Le retour du provider est routé vers ConnexionOAuthHandler : il résout l'identité (via le port
            // IFournisseurOAuth) puis ouvre la session par le MÊME chemin s23 (compte Actif → acteur), ou la
            // refuse (identité non résolue / email inconnu / compte inactif). Sur succès on redirige le
            // navigateur vers le planning, sinon vers la connexion. La propagation de la session ouverte
            // jusqu'au front WASM (jeton / cookie) fait partie de la dette de câblage (G3).
            var frontOrigine = config["Front:Origine"] ?? "http://localhost:5292";
            var resultat = handler.Handle(new CallbackOAuthCommand(code ?? string.Empty));
            return resultat.EstSucces
                ? Results.Redirect($"{frontOrigine}/planning")
                : Results.Redirect($"{frontOrigine}/connexion?oauth=echec");
        });

        return routes;
    }
}
