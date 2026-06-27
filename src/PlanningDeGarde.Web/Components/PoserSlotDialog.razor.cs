using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using PlanningDeGarde.Web.State;
using static PlanningDeGarde.Web.CanalEcriture;

namespace PlanningDeGarde.Web.Components;

/// <summary>
/// Dialog (modal) « Poser un slot » réutilisable, ouverte depuis une case du planning (palier 7,
/// écriture en contexte). L'écriture passe par le <b>canal requête/réponse</b> (endpoint HTTP
/// <c>/api/canal/poser-slot</c>) — JAMAIS un handler en DI direct ni le canal de diffusion. Aucune
/// règle métier ici. Issues : succès → <see cref="OnValide"/> (le parent ferme et relit la grille) ;
/// refus métier (4xx, motif propagé) ou API injoignable (échec de transport) → message <b>dans</b> la
/// dialog, saisie conservée, aucune fermeture.
/// </summary>
public partial class PoserSlotDialog
{
    private sealed class Formulaire
    {
        public string LieuId { get; set; } = "";
        public DateTime Debut { get; set; }
        public DateTime Fin { get; set; }
    }

    private readonly Formulaire _form = new();
    private string? _motifEchec;

    /// <summary>Date de la case cliquée : elle <b>prime</b> sur le défaut « aujourd'hui » et ancre la
    /// saisie sur la date de contexte (règle 17 composée, Sc.3). Heures usuelles 08h30 → 16h30.
    /// <para><b>Garde-fou (décision CP, Sc.3)</b> : ce sprint retire les routes/écrans dédiés → la dialog
    /// est <b>toujours</b> ouverte depuis une case, donc <see cref="DateContexte"/> est toujours fourni et
    /// le repli horloge n'a plus de point d'entrée hors-contexte. NE PAS supprimer le port
    /// <see cref="IDateTimeProvider"/> (la grille s'en sert) ; si un futur palier réintroduit un point
    /// d'entrée hors-contexte, réintroduire ici le repli <c>IDateTimeProvider.Aujourdhui</c> par défaut.</para></summary>
    [Parameter, EditorRequired]
    public DateOnly DateContexte { get; set; }

    /// <summary>Notifié sur écriture aboutie (succès) : le parent ferme la dialog et relit la grille.</summary>
    [Parameter]
    public EventCallback OnValide { get; set; }

    /// <summary>Notifié sur annulation : le parent ferme la dialog sans aucune écriture.</summary>
    [Parameter]
    public EventCallback OnAnnule { get; set; }

    protected override void OnInitialized()
    {
        _form.Debut = DateContexte.ToDateTime(new TimeOnly(8, 30));
        _form.Fin = DateContexte.ToDateTime(new TimeOnly(16, 30));
    }

    private async Task Soumettre()
    {
        _motifEchec = null;

        HttpResponseMessage reponse;
        try
        {
            reponse = await Canal.PostAsJsonAsync(
                "api/canal/poser-slot",
                new PoserSlotRequete(Session.EnfantId, _form.LieuId, _form.Debut, _form.Fin));
        }
        catch (HttpRequestException)
        {
            // API distante injoignable (échec de transport) : message dédié, saisie conservée, pas de fermeture.
            _motifEchec = PlanningDeGarde.Web.Components.Pages.PoserSlot.MessageServiceInjoignable;
            return;
        }

        if (reponse.IsSuccessStatusCode)
            await OnValide.InvokeAsync();
        else
            _motifEchec = await reponse.Content.ReadAsStringAsync();
    }

    private async Task Annuler() => await OnAnnule.InvokeAsync();
}
