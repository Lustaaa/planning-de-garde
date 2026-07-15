using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using static PlanningDeGarde.Web.CanalEcriture;

namespace PlanningDeGarde.Web.Components;

/// <summary>
/// Mini-dialog « Reprendre ce jour » (s46) : confirmation de l'annulation de la délégation d'UN jour, émise
/// via le <b>canal requête/réponse</b> (endpoint HTTP <c>/api/canal/annuler-delegation</c>) — JAMAIS le canal
/// de diffusion. Aucune règle métier ici : le use case COMPOSE la suppression de surcharge existante (s16).
/// Issues : succès → <see cref="OnValide"/> ; API injoignable → message <b>dans</b> la dialog, dialog restée
/// OUVERTE. Portée par <c>ModalConfig</c> : Échap = « Annuler » (port <see cref="IEcouteurEchapModal"/> s33).
/// </summary>
public partial class ReprendreJourDialog
{
    private string? _motifEchec;

    /// <summary>Le jour dont on reprend la récupération (case portant une délégation active).</summary>
    [Parameter, EditorRequired]
    public DateOnly DateContexte { get; set; }

    /// <summary>Enfant sélectionné (parité s44) dont on reprend la récupération ce jour-là.</summary>
    [Parameter]
    public string EnfantId { get; set; } = "";

    /// <summary>Notifié sur écriture aboutie (succès) : le parent ferme la dialog et relit la grille.</summary>
    [Parameter]
    public EventCallback OnValide { get; set; }

    /// <summary>Notifié sur annulation (bouton ou Échap) : le parent ferme la dialog sans aucune écriture.</summary>
    [Parameter]
    public EventCallback OnAnnule { get; set; }

    private async Task Confirmer()
    {
        _motifEchec = null;

        HttpResponseMessage reponse;
        try
        {
            reponse = await Canal.PostAsJsonAsync(
                "api/canal/annuler-delegation", new AnnulerDelegationRequete(DateContexte, EnfantId));
        }
        catch (HttpRequestException)
        {
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
