using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Infrastructure;

namespace PlanningDeGarde.Web.Components.Pages;

/// <summary>
/// Vue centrale du planning partagé, rendue en grille agenda 5×7 (5 lignes-semaines de
/// 7 cases-jour) en LECTURE SEULE. Chaque case-jour porte la couleur du parent responsable
/// de la période qui la couvre ; chaque slot est empilé dans sa case avec son libellé et son
/// horaire, son créneau portant la couleur propre de l'acteur. Lit la projection
/// <see cref="GrilleAgendaQuery"/> (CQRS) ; se rafraîchit en temps réel sur l'évènement SignalR.
/// Aucune règle métier ici — les écritures restent sur les routes dédiées (/planning/poser-slot…).
/// </summary>
public partial class PlanningPartage
{
    private static readonly string[] JoursDeLaSemaine =
        { "Lundi", "Mardi", "Mercredi", "Jeudi", "Vendredi", "Samedi", "Dimanche" };

    private GrilleAgenda _grille = new(Array.Empty<JourCase>(), Array.Empty<SemaineLigne>());

    private HubConnection? _hub;

    private string Desactive => Session.EstParent ? string.Empty : "disabled";

    private RoleAuteur RoleSelectionne
    {
        get => Session.Role;
        set { Session.Role = value; }
    }

    protected override void OnInitialized() => Charger();

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        try
        {
            _hub = new HubConnectionBuilder()
                .WithUrl(Nav.ToAbsoluteUri("/hubs/planning"))
                .WithAutomaticReconnect()
                .Build();

            _hub.On(PlanningHub.EvenementMiseAJour, async () =>
            {
                Charger();
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

    // Date de référence = aujourd'hui (la projection prend une DateOnly injectée pour le
    // déterminisme côté tests ; à l'exécution réelle on lui passe la date courante).
    private void Charger() => _grille = Grille.Projeter(DateOnly.FromDateTime(DateTime.Now));

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
