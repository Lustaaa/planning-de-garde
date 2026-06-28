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
/// Vue d'écriture « définir un transfert ». L'écriture passe par le <b>canal requête/réponse</b>
/// (endpoint HTTP <c>/api/canal/definir-transfert</c>), JAMAIS par un appel de handler en DI direct
/// (impossible côté navigateur) ni par le canal de diffusion (SignalR, lecture seule). Le canal
/// renvoie un accusé succès/échec ; sur succès la vue retourne au planning, sur refus elle affiche le
/// motif métier propagé. Aucune règle métier dans l'UI.
///
/// Passée en <b>code-behind</b> (dette template levée au sprint 06) pour offrir un point d'injection
/// propre du port <see cref="IDateTimeProvider"/> : la date est pré-remplie « aujourd'hui », jamais
/// figée ni <c>DateTime.Today</c> en dur.
/// </summary>
public partial class DefinirTransfert
{
    private sealed class Formulaire
    {
        public string DeposeParId { get; set; } = "";
        public string RecupereParId { get; set; } = "";
        public string LieuId { get; set; } = "";
        public DateTime Date { get; set; }
    }

    private readonly Formulaire _form = new();
    private TimeOnly? _heure = new(8, 30);
    private string? _motifEchec;

    /// <summary>
    /// Pré-remplit la date du transfert à la <b>date du jour</b> injectée (port
    /// <see cref="IDateTimeProvider"/>) — jamais une date figée ni <c>DateTime.Today</c> en dur. Un
    /// transfert validé « sans toucher la date » est ainsi horodaté à aujourd'hui (Sc.3).
    /// </summary>
    protected override void OnInitialized()
        => _form.Date = Horloge.Aujourdhui.ToDateTime(TimeOnly.MinValue);

    private async Task Soumettre()
    {
        _motifEchec = null;
        var heure = _heure?.ToTimeSpan() ?? TimeSpan.Zero;

        // Émission de la commande d'écriture via le canal HTTP de l'API distante (adaptateur de
        // gauche), JAMAIS par un handler en DI direct (impossible côté navigateur).
        HttpResponseMessage reponse;
        try
        {
            reponse = await Canal.PostAsJsonAsync(
                "api/canal/definir-transfert",
                new DefinirTransfertRequete(_form.DeposeParId, _form.RecupereParId, _form.LieuId, heure, _form.Date));
        }
        catch (HttpRequestException)
        {
            _motifEchec = MessagesEcriture.ServiceInjoignable;
            return;
        }

        if (reponse.IsSuccessStatusCode)
            Nav.NavigateTo("planning");
        else
            _motifEchec = await reponse.Content.ReadAsStringAsync();
    }
}
