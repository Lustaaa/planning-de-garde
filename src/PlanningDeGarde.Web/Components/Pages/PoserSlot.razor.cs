using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using PlanningDeGarde.Application;
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
        public DateTime Debut { get; set; }
        public DateTime Fin { get; set; }
    }

    private readonly Formulaire _form = new();
    private string? _motifEchec;

    /// <summary>
    /// Pré-remplit les bornes du formulaire à la <b>date du jour</b> injectée (port
    /// <see cref="IDateTimeProvider"/>), aux heures usuelles 08h30 → 16h30 — jamais une date figée
    /// ni <c>DateTime.Today</c> en dur. Une pose validée « sans toucher aux dates » tombe ainsi dans
    /// la fenêtre affichée (Sc.1) : seule la date est par défaut « aujourd'hui », l'heure reste celle
    /// du formulaire si l'utilisateur la modifie.
    /// </summary>
    protected override void OnInitialized()
    {
        var aujourdhui = Horloge.Aujourdhui;
        _form.Debut = aujourdhui.ToDateTime(new TimeOnly(8, 30));
        _form.Fin = aujourdhui.ToDateTime(new TimeOnly(16, 30));
    }

    /// <summary>
    /// Message affiché quand l'API distante est <b>injoignable</b> (échec de transport : connexion
    /// refusée, timeout) — distinct d'un refus métier 4xx (motif propagé par le canal). La saisie
    /// n'est alors pas appliquée et reste à resoumettre : aucune navigation, aucune mise en file
    /// (PWA hors périmètre). Cf. Sc.6.
    /// </summary>
    internal const string MessageServiceInjoignable =
        "Enregistrement impossible : le service est injoignable, réessayez.";

    private async Task Soumettre()
    {
        _motifEchec = null;

        HttpResponseMessage reponse;
        try
        {
            // Émission de la commande d'écriture via le canal HTTP (adaptateur de gauche).
            reponse = await Canal.PostAsJsonAsync(
                "api/canal/poser-slot",
                new PoserSlotRequete(Session.EnfantId, _form.LieuId, _form.Debut, _form.Fin));
        }
        catch (HttpRequestException)
        {
            // API distante injoignable (échec de transport, pas un refus métier) : message dédié,
            // saisie conservée (pas de navigation, pas de reset), aucune écriture ni mise en file.
            _motifEchec = MessageServiceInjoignable;
            return;
        }

        if (reponse.IsSuccessStatusCode)
            Nav.NavigateTo("planning");
        else
            _motifEchec = await reponse.Content.ReadAsStringAsync();
    }
}
