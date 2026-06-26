using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Stub d'<see cref="HttpMessageHandler"/> pour les tests de composant : il n'envoie rien sur
/// le réseau, il <b>enregistre</b> les requêtes émises par la vue vers le canal d'écriture HTTP
/// et renvoie une réponse programmée. Permet de vérifier que la vue écrit bien <b>via le canal</b>
/// <c>/api/canal/*</c> (et non par un handler en DI direct), sans hôte Web réel. Le bout en bout
/// du canal (handler → store → projection) est couvert par les tests d'intégration dédiés.
/// </summary>
public sealed class FakeCanalHttpHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _statut;
    private readonly string _corpsReponse;

    public List<HttpRequestMessage> RequetesRecues { get; } = new();
    public List<string> CorpsRecus { get; } = new();

    public FakeCanalHttpHandler(HttpStatusCode statut = HttpStatusCode.OK, string corpsReponse = "")
    {
        _statut = statut;
        _corpsReponse = corpsReponse;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        RequetesRecues.Add(request);
        CorpsRecus.Add(request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken));
        return new HttpResponseMessage(_statut) { Content = new StringContent(_corpsReponse) };
    }
}
