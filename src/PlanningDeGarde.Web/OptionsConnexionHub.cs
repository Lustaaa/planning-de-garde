using System;
using Microsoft.AspNetCore.Http.Connections.Client;

namespace PlanningDeGarde.Web;

/// <summary>
/// Point d'extension du câblage de la connexion au hub SignalR de lecture (« /hubs/planning »).
/// En WASM réel, la configuration est <b>neutre</b> (le navigateur négocie en WebSocket vers l'API
/// distante, comportement par défaut inchangé). Un hôte de <b>test</b> peut la surcharger pour
/// rediriger la connexion vers son serveur en mémoire (TestServer) — seul moyen d'asserter au
/// <b>runtime</b> que la grille suit la diffusion temps réel sans reconstruire le hub.
/// </summary>
public sealed class OptionsConnexionHub
{
    /// <summary>Configure la connexion au hub. Neutre par défaut (WebSocket navigateur).</summary>
    public Action<HttpConnectionOptions> Configurer { get; init; } = _ => { };
}
