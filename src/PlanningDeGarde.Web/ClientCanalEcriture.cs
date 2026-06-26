using Microsoft.Extensions.Configuration;

namespace PlanningDeGarde.Web;

/// <summary>
/// Fabrique du client HTTP d'écriture du front <b>WASM</b>. Le front s'exécutant dans le
/// navigateur, il émet ses écritures vers une <b>API distante</b> dont l'URL est <b>configurable</b>
/// (clé « Api:BaseUrl »), et non plus vers son propre hôte (<c>nav.BaseUri</c>, valable seulement
/// quand le front était rendu côté serveur). C'est ce câblage runtime que le Sc.2 prouve.
/// </summary>
public static class ClientCanalEcriture
{
    /// <summary>Clé de configuration portant l'URL de l'API distante consommée par le front.</summary>
    public const string CleUrlApi = "Api:BaseUrl";

    /// <summary>
    /// Construit le client d'écriture du front à partir de la configuration. <paramref name="transport"/>
    /// permet d'injecter un transport réel en test (cross-host) ; en WASM, il est laissé nul (le
    /// runtime fournit le <see cref="HttpClient"/>).
    /// </summary>
    public static HttpClient Construire(IConfiguration configuration, HttpMessageHandler? transport = null)
    {
        var urlApi = configuration[CleUrlApi]
            ?? throw new InvalidOperationException(
                $"URL de l'API distante absente : renseignez la clé de configuration « {CleUrlApi} ».");

        var client = transport is null ? new HttpClient() : new HttpClient(transport, disposeHandler: false);
        // Le front WASM émet vers l'API DISTANTE configurable (et non son propre hôte).
        client.BaseAddress = new Uri(urlApi);
        return client;
    }
}
