using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Web.Components.Pages;

/// <summary>
/// Vue centrale du planning partagé (front <b>WASM</b>), rendue en grille agenda 5×7 en LECTURE
/// SEULE. La grille est lue via le <b>canal de lecture de l'API distante</b> (HTTP
/// <c>GET /api/grille/…</c>) — le navigateur n'a pas la projection en DI directe. Le rafraîchissement
/// temps réel passe par le <b>hub SignalR de l'API distante</b>, consommé côté navigateur ; une
/// écriture aboutie le déclenche, jamais l'inverse. Aucune règle métier ici.
/// </summary>
public partial class PlanningPartage
{
    private static readonly string[] JoursDeLaSemaine =
        { "Lundi", "Mardi", "Mercredi", "Jeudi", "Vendredi", "Samedi", "Dimanche" };

    private GrilleAgenda _grille = new(Array.Empty<JourCase>(), Array.Empty<SemaineLigne>(), Array.Empty<EntreeLegende>());

    private HubConnection? _hub;

    private string Desactive => Session.EstParent ? string.Empty : "disabled";

    private RoleAuteur RoleSelectionne
    {
        get => Session.Role;
        set { Session.Role = value; }
    }

    protected override async Task OnInitializedAsync() => await ChargerAsync();

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        try
        {
            // Hub SignalR de l'API DISTANTE (même hôte que le canal d'écriture/lecture : Canal.BaseAddress).
            // OptionsHub.Configurer est neutre en WASM réel (WebSocket navigateur) ; un hôte de test le
            // surcharge pour rediriger la connexion vers son TestServer en mémoire (acceptation runtime Sc.4).
            var urlHub = new Uri(Canal.BaseAddress!, "hubs/planning");
            _hub = new HubConnectionBuilder()
                .WithUrl(urlHub, OptionsHub.Configurer)
                .WithAutomaticReconnect()
                .Build();

            _hub.On(PlanningHubEvenement.MiseAJour, async () =>
            {
                await ChargerAsync();
                await InvokeAsync(StateHasChanged);
            });

            await _hub.StartAsync();
        }
        catch
        {
            // Le temps réel est un confort : si le hub est indisponible, la vue reste
            // fonctionnelle (rechargement à la navigation).
        }
    }

    // Date de référence = aujourd'hui, lue via le port d'horloge injecté (jamais DateTime.Now en dur :
    // déterminisme en test, symétrie avec Projeter(dateReference) côté lecture). Le canal de lecture
    // distant prend cette date en segments yyyy/MM/dd.
    private async Task ChargerAsync()
    {
        var aujourdHui = Horloge.Aujourdhui;
        try
        {
            var grille = await Canal.GetFromJsonAsync<GrilleAgenda>(
                $"api/grille/{aujourdHui.Year}/{aujourdHui.Month}/{aujourdHui.Day}");
            if (grille is not null)
                _grille = grille;
        }
        catch (HttpRequestException)
        {
            // API distante injoignable : la grille reste vide plutôt que de planter la vue.
        }
    }

    /// <summary>
    /// Teinte claire de la case-jour pour la couleur du responsable (fond pâle lisible avec
    /// du texte sombre) ; repli blanc pour la couleur neutre / inconnue.
    /// </summary>
    private static string Teinte(string couleur) => couleur switch
    {
        "bleu" => "#dbeafe",
        "orange" => "#ffedd5",
        "vert" => "#dcfce7",
        "gris" => "#f1f3f5",
        _ => "#ffffff",
    };

    /// <summary>Couleur pleine du créneau pour la couleur propre de l'acteur du slot.</summary>
    private static string Couleur(string couleur) => couleur switch
    {
        "bleu" => "#2563eb",
        "orange" => "#ea580c",
        "vert" => "#16a34a",
        "gris" => "#6b7280",
        _ => "#6b7280",
    };

    public async ValueTask DisposeAsync()
    {
        if (_hub is not null)
            await _hub.DisposeAsync();
    }
}
