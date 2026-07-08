using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using PlanningDeGarde.Web.State;
using static PlanningDeGarde.Web.CanalEcriture;

namespace PlanningDeGarde.Web.Components;

/// <summary>
/// Dialog (modal) « Poser un slot récurrent » (s29, S8), ouverte depuis une case du planning (menu
/// clic-case, écriture en contexte). L'écriture passe par le <b>canal requête/réponse</b> (endpoint HTTP
/// <c>/api/canal/poser-slot-recurrent</c>) — JAMAIS un handler en DI direct ni le canal de diffusion.
/// Aucune règle métier ici. Issues : succès → <see cref="OnValide"/> (le parent ferme et relit la grille,
/// les occurrences apparaissent sur chaque jour de récurrence) ; refus métier (lieu inconnu / durée non
/// positive, motif propagé) ou API injoignable → message <b>dans</b> la dialog, saisie conservée.
/// </summary>
public partial class PoserSlotRecurrentDialog
{
    private static readonly DayOfWeek[] JoursDeLaSemaine =
    {
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday,
        DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday,
    };

    private sealed class Formulaire
    {
        public string LieuId { get; set; } = "";
        public DayOfWeek Jour { get; set; }
        public TimeOnly Debut { get; set; } = new(8, 30);
        public TimeOnly Fin { get; set; } = new(16, 30);
    }

    private readonly Formulaire _form = new();
    private string? _motifEchec;

    /// <summary>Date de la case cliquée : son <b>jour de semaine</b> pré-remplit la récurrence (« chez papa
    /// tous les samedis » → cliquer un samedi). Le slot récurrent n'est pas daté : seul le jour est retenu.</summary>
    [Parameter, EditorRequired]
    public DateOnly DateContexte { get; set; }

    /// <summary>Lieux du référentiel du foyer (id stable + libellé), fournis par le parent depuis le store
    /// vivant (GET /api/foyer/lieux) : le sélecteur ne propose que ces lieux réels (jamais une liste en dur).</summary>
    [Parameter]
    public IReadOnlyList<LieuFoyer> Lieux { get; set; } = Array.Empty<LieuFoyer>();

    /// <summary>Notifié sur écriture aboutie (succès) : le parent ferme la dialog et relit la grille (les
    /// occurrences du récurrent apparaissent sur chaque jour de récurrence).</summary>
    [Parameter]
    public EventCallback OnValide { get; set; }

    /// <summary>Notifié sur annulation : le parent ferme la dialog sans aucune écriture.</summary>
    [Parameter]
    public EventCallback OnAnnule { get; set; }

    protected override void OnInitialized() => _form.Jour = DateContexte.DayOfWeek;

    private static string LibelleJour(DayOfWeek jour) => jour switch
    {
        DayOfWeek.Monday => "Lundi",
        DayOfWeek.Tuesday => "Mardi",
        DayOfWeek.Wednesday => "Mercredi",
        DayOfWeek.Thursday => "Jeudi",
        DayOfWeek.Friday => "Vendredi",
        DayOfWeek.Saturday => "Samedi",
        _ => "Dimanche",
    };

    private async Task Soumettre()
    {
        _motifEchec = null;

        HttpResponseMessage reponse;
        try
        {
            reponse = await Canal.PostAsJsonAsync(
                "api/canal/poser-slot-recurrent",
                new PoserSlotRecurrentRequete(Session.EnfantId, _form.LieuId, _form.Jour, _form.Debut.ToTimeSpan(), _form.Fin.ToTimeSpan()));
        }
        catch (HttpRequestException)
        {
            // API distante injoignable (échec de transport) : message dédié, saisie conservée, pas de fermeture.
            _motifEchec = MessagesEcriture.ServiceInjoignable;
            return;
        }

        if (reponse.IsSuccessStatusCode)
            await OnValide.InvokeAsync();
        else
            _motifEchec = await reponse.Content.ReadAsStringAsync();
    }

    private async Task Annuler() => await OnAnnule.InvokeAsync();
}
