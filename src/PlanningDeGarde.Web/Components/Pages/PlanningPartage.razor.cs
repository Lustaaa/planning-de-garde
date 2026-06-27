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

    // Date de contexte de la dialog « Poser un slot » ouverte depuis une case (null = aucune dialog).
    // La grille reste en LECTURE SEULE (règle 14) : la case ne fait qu'ouvrir la dialog, jamais écrire.
    private DateOnly? _dateDialogPoserSlot;

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
    /// Ouvre la dialog « Poser un slot » pré-remplie sur la date de la case cliquée. Gating Invité
    /// (règle 9) : en consultation seule, le clic n'ouvre rien — le déclencheur d'écriture est gardé
    /// à l'entrée. Aucune écriture ici : la dialog porte la commande, la grille reste en lecture seule.
    /// </summary>
    private void OuvrirPoserSlot(DateOnly date)
    {
        if (!Session.EstParent)
            return;

        _dateDialogPoserSlot = date;
    }

    /// <summary>Ferme la dialog sur succès et <b>relit</b> la grille depuis l'API distante : la pose
    /// aboutie réapparaît, positionnée à la date de la case (relecture, jamais une mutation locale).</summary>
    private async Task FermerDialogEtRecharger()
    {
        _dateDialogPoserSlot = null;
        await ChargerAsync();
    }

    /// <summary>Ferme la dialog sans aucune écriture (annulation) : la grille reste intacte.</summary>
    private void FermerDialog() => _dateDialogPoserSlot = null;

    /// <summary>Teinte claire de la case-jour pour la couleur du responsable (fond pâle lisible
    /// avec du texte sombre), via le thème couleur partagé.</summary>
    private static string Teinte(string couleur) => CouleursTheme.Claire(couleur);

    /// <summary>Couleur pleine du créneau pour la couleur propre de l'acteur du slot, via le
    /// thème couleur partagé.</summary>
    private static string Couleur(string couleur) => CouleursTheme.Pleine(couleur);

    public async ValueTask DisposeAsync()
    {
        if (_hub is not null)
            await _hub.DisposeAsync();
    }
}
