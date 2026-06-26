using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using PlanningDeGarde.Web.State;
using static PlanningDeGarde.Web.CanalEcriture;

namespace PlanningDeGarde.Web.Components.Pages;

/// <summary>
/// Vue d'écriture « poser un slot ». L'écriture passe par le <b>canal requête/réponse</b>
/// (endpoint HTTP <c>/api/canal/poser-slot</c>), JAMAIS par un appel de handler en DI direct
/// ni par le canal de diffusion (SignalR, lecture seule). Le canal renvoie un accusé succès/échec ;
/// sur succès la vue retourne au planning (qui se rafraîchit via SignalR après l'écriture aboutie),
/// sur refus elle affiche le motif métier propagé. Aucune règle métier dans l'UI.
/// </summary>
public partial class PoserSlot
{
    private sealed class Formulaire
    {
        public string LieuId { get; set; } = "";
        public DateTime Debut { get; set; } = new(2025, 7, 15, 8, 30, 0);
        public DateTime Fin { get; set; } = new(2025, 7, 15, 16, 30, 0);
    }

    private readonly Formulaire _form = new();
    private string? _motifEchec;

    private async Task Soumettre()
    {
        _motifEchec = null;

        // Émission de la commande d'écriture via le canal HTTP (adaptateur de gauche).
        var reponse = await Canal.PostAsJsonAsync(
            "api/canal/poser-slot",
            new PoserSlotRequete(Session.EnfantId, _form.LieuId, _form.Debut, _form.Fin));

        if (reponse.IsSuccessStatusCode)
            Nav.NavigateTo("planning");
        else
            _motifEchec = await reponse.Content.ReadAsStringAsync();
    }
}
