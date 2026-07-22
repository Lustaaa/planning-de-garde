using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using static PlanningDeGarde.Web.CanalEcriture;

namespace PlanningDeGarde.Web.Components.Slots;

/// <summary>
/// Dialog (modal) « Supprimer un slot » ouverte depuis le menu d'actions d'une case (palier 7, écriture
/// en contexte — 6ᵉ usage du menu clic-case). À l'ouverture elle LIT les slots couvrant la date via le
/// <b>canal de lecture</b> HTTP (<c>GET /api/slots/…</c>) ; supprimer une ligne émet la commande via le
/// <b>canal requête/réponse</b> (<c>DELETE /api/slots/{id}</c>) — JAMAIS un handler en DI direct
/// ni le canal de diffusion. Aucune règle métier ici : la clé envoyée est l'<b>identifiant stable</b> du
/// slot. Issues : succès → <see cref="OnValide"/> (le parent ferme, accuse et relit la grille) ; refus
/// métier (4xx) ou API injoignable → message <b>dans</b> la dialog, la dialog reste ouverte (Sc.9).
/// </summary>
public partial class SupprimerSlotDialog
{
    /// <summary>Vue d'un slot couvrant la date (miroir du DTO de lecture de l'API) : identifiant stable
    /// (clé de suppression), enfant, lieu (libellé d'affichage), bornes horaires datées.</summary>
    public sealed record SlotDuJourVue(string Id, string EnfantId, string LieuId, DateTime Debut, DateTime Fin);

    private List<SlotDuJourVue>? _slots;
    private string? _motifEchec;

    /// <summary>Date de la case cliquée : on liste les slots qui la couvrent.</summary>
    [Parameter, EditorRequired]
    public DateOnly DateContexte { get; set; }

    /// <summary>Notifié sur suppression aboutie : le parent ferme la dialog, accuse et relit la grille.</summary>
    [Parameter]
    public EventCallback OnValide { get; set; }

    /// <summary>Notifié sur fermeture sans suppression : le parent ferme la dialog sans aucune écriture.</summary>
    [Parameter]
    public EventCallback OnAnnule { get; set; }

    protected override async Task OnInitializedAsync() => await ChargerSlots();

    private async Task ChargerSlots()
    {
        try
        {
            _slots = await Canal.GetFromJsonAsync<List<SlotDuJourVue>>(
                $"api/slots/{DateContexte.Year}/{DateContexte.Month}/{DateContexte.Day}");
        }
        catch (HttpRequestException)
        {
            // API distante injoignable à l'ouverture : la dialog reste ouverte avec un message clair,
            // aucune liste fabriquée (aucune suppression possible tant que la lecture n'a pas abouti).
            _slots = new List<SlotDuJourVue>();
            _motifEchec = MessagesEcriture.ServiceInjoignable;
        }
    }

    private async Task Supprimer(string slotId)
    {
        _motifEchec = null;

        HttpResponseMessage reponse;
        try
        {
            reponse = await Canal.DeleteAsync($"api/slots/{slotId}");
        }
        catch (HttpRequestException)
        {
            // API distante injoignable (échec de transport) : message dédié, dialog conservée ouverte,
            // rien appliqué, aucune mise en file ni rejeu (Sc.9, règle 28).
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
