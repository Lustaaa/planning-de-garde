using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using static PlanningDeGarde.Web.CanalEcriture;

namespace PlanningDeGarde.Web.Components.Periodes;

/// <summary>
/// Dialog (modal) « Supprimer une période » ouverte depuis le menu d'actions d'une case (
/// écriture en contexte — 4ᵉ usage du menu clic-case). À l'ouverture elle LIT les périodes couvrant la
/// date via le <b>canal de lecture</b> HTTP (<c>GET /api/periodes/…</c>) ; supprimer une ligne émet la
/// commande via le <b>canal requête/réponse</b> (<c>DELETE /api/periodes/{id}</c>) — JAMAIS un
/// handler en DI direct ni le canal de diffusion. Aucune règle métier ici : la clé envoyée est
/// l'<b>identifiant stable</b> de la période. Issues : succès → <see cref="OnValide"/> (le parent ferme,
/// accuse et relit la grille) ; refus métier (4xx) ou API injoignable → message <b>dans</b> la dialog,
/// la dialog reste ouverte.
/// </summary>
public partial class SupprimerPeriodeDialog
{
    /// <summary>Vue d'une période couvrant la date (miroir du DTO de lecture de l'API) : identifiant
    /// stable (clé de suppression), nom d'affichage du responsable résolu côté API, bornes datées.</summary>
    public sealed record PeriodeDuJourVue(string Id, string ResponsableNom, DateTime Debut, DateTime Fin);

    private List<PeriodeDuJourVue>? _periodes;
    private string? _motifEchec;

    /// <summary>Date de la case cliquée : on liste les périodes qui la couvrent.</summary>
    [Parameter, EditorRequired]
    public DateOnly DateContexte { get; set; }

    /// <summary>Notifié sur suppression aboutie : le parent ferme la dialog, accuse et relit la grille.</summary>
    [Parameter]
    public EventCallback OnValide { get; set; }

    /// <summary>Notifié sur fermeture sans suppression : le parent ferme la dialog sans aucune écriture.</summary>
    [Parameter]
    public EventCallback OnAnnule { get; set; }

    protected override async Task OnInitializedAsync() => await ChargerPeriodes();

    private async Task ChargerPeriodes()
    {
        try
        {
            _periodes = await Canal.GetFromJsonAsync<List<PeriodeDuJourVue>>(
                $"api/periodes/{DateContexte.Year}/{DateContexte.Month}/{DateContexte.Day}");
        }
        catch (HttpRequestException)
        {
            // API distante injoignable à l'ouverture : la dialog reste ouverte avec un message clair,
            // aucune liste fabriquée (aucune suppression possible tant que la lecture n'a pas abouti).
            _periodes = new List<PeriodeDuJourVue>();
            _motifEchec = MessagesEcriture.ServiceInjoignable;
        }
    }

    private async Task Supprimer(string periodeId)
    {
        _motifEchec = null;

        HttpResponseMessage reponse;
        try
        {
            reponse = await Canal.DeleteAsync($"api/periodes/{periodeId}");
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
