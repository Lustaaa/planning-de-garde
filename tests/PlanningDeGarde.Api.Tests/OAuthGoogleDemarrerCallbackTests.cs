using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;

namespace PlanningDeGarde.Api.Tests;

/// <summary>
/// Sprint 28 — S10 — Endpoint <c>api/oauth/google/demarrer</c> + DI du handler OAuth branchés (@back
/// @preuve-doublure, volet 3). Prouve le CÂBLAGE sur l'hôte d'API détaché réel :
///   - le port <see cref="IFournisseurOAuth"/> est enregistré en DI et <see cref="ConnexionOAuthHandler"/>
///     est résolvable ;
///   - <c>GET api/oauth/google/demarrer</c> démarre le flux OAuth Google (302 vers l'authorize du provider) ;
///   - <c>api/oauth/google/callback</c> est routé vers <see cref="ConnexionOAuthHandler"/> qui OUVRE ou
///     REFUSE la session selon l'identité restituée (ici par une DOUBLURE à la main du port — le provider
///     Google réel, secrets/token endpoint, est une DETTE DE CÂBLAGE vérifiée MANUELLEMENT au G3).
/// Statut cible « ✅ logique / ⚠️ câblage », jamais un ✅ franc.
/// </summary>
public sealed class OAuthGoogleDemarrerCallbackTests
{
    /// <summary>Doublure à la main du port OAuth (Api.Tests ne référence pas les Fakes de
    /// PlanningDeGarde.Tests) : restitue l'identité externe programmée (ou aucune), court-circuitant le
    /// protocole OAuth réel — le provider Google réel reste la dette de câblage (G3).</summary>
    private sealed class DoublureFournisseurOAuth : IFournisseurOAuth
    {
        private readonly IdentiteExterne? _identite;
        public DoublureFournisseurOAuth(string? emailVerifie) => _identite = emailVerifie is null ? null : new IdentiteExterne(emailVerifie);
        public IdentiteExterne? ResoudreIdentite(string callback) => _identite;
    }

    private static WebApplicationFactoryClientOptions SansRedirection => new() { AllowAutoRedirect = false };

    [Fact]
    public async Task Should_Rediriger_vers_l_authorize_Google_When_le_navigateur_atteint_api_oauth_google_demarrer()
    {
        using var hote = new ApiHoteFactory();
        var client = hote.CreateClient(SansRedirection);

        var reponse = await client.GetAsync("/api/oauth/google/demarrer");

        Assert.Equal(HttpStatusCode.Redirect, reponse.StatusCode); // 302 : démarrage du flux OAuth
        var location = reponse.Headers.Location!.ToString();
        Assert.Contains("accounts.google.com/o/oauth2", location);      // authorize du provider Google
        Assert.Contains("response_type=code", location);               // flux « authorization code »
        Assert.Contains("client_id=", location);
        Assert.Contains("redirect_uri=", location);
    }

    [Fact]
    public void Should_Enregistrer_le_port_OAuth_et_rendre_le_handler_de_callback_resolvable_en_DI()
    {
        using var hote = new ApiHoteFactory();

        Assert.NotNull(hote.Services.GetService<IFournisseurOAuth>());     // port enregistré
        using var scope = hote.Services.CreateScope();
        Assert.NotNull(scope.ServiceProvider.GetService<ConnexionOAuthHandler>()); // handler résolvable
    }

    [Fact]
    public async Task Should_Ouvrir_la_session_sur_email_connu_et_refuser_sur_email_inconnu_When_le_callback_est_route_vers_ConnexionOAuthHandler()
    {
        // Email CONNU (doublure) + compte local Actif → le callback ouvre la session : redirection planning.
        using (var hoteConnuBase = new ApiHoteFactory())
        using (var hoteConnu = hoteConnuBase.WithWebHostBuilder(b => b.ConfigureTestServices(s =>
            s.AddSingleton<IFournisseurOAuth>(new DoublureFournisseurOAuth("papa@foyer.fr")))))
        {
            hoteConnu.Services.GetRequiredService<ReferentielComptesEnMemoire>()
                .Creer("compte-papa", "papa@foyer.fr", StatutCompte.Actif, "acteur-papa");
            var client = hoteConnu.CreateClient(SansRedirection);

            var reponse = await client.GetAsync("/api/oauth/google/callback?code=code-google-papa");

            Assert.Equal(HttpStatusCode.Redirect, reponse.StatusCode);
            Assert.Contains("planning", reponse.Headers.Location!.ToString()); // session ouverte
        }

        // Email INCONNU (doublure) → le callback refuse : redirection connexion, aucune session.
        using (var hoteInconnuBase = new ApiHoteFactory())
        using (var hoteInconnu = hoteInconnuBase.WithWebHostBuilder(b => b.ConfigureTestServices(s =>
            s.AddSingleton<IFournisseurOAuth>(new DoublureFournisseurOAuth("inconnu@foyer.fr")))))
        {
            var client = hoteInconnu.CreateClient(SansRedirection);

            var reponse = await client.GetAsync("/api/oauth/google/callback?code=code-google-inconnu");

            Assert.Equal(HttpStatusCode.Redirect, reponse.StatusCode);
            Assert.Contains("connexion", reponse.Headers.Location!.ToString()); // refus, retour connexion
        }
    }
}
