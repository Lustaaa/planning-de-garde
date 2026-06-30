using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using static PlanningDeGarde.Web.CanalEcriture;

namespace PlanningDeGarde.Web.Components;

/// <summary>
/// Dialog (modal) « Éditer une période » ouverte depuis le menu d'actions d'une case (palier 7, écriture
/// en contexte — 5ᵉ usage du menu clic-case). À l'ouverture elle LIT les périodes couvrant la date via le
/// <b>canal de lecture</b> HTTP (<c>GET /api/periodes/…</c>) et les acteurs du foyer (<c>GET
/// /api/foyer/acteurs</c>) pour le sélecteur. « Éditer » une ligne ouvre un formulaire <b>pré-rempli</b>
/// (responsable courant + bornes) ; « Enregistrer » émet la commande via le <b>canal requête/réponse</b>
/// (<c>POST /api/canal/editer-periode</c>) — JAMAIS un handler en DI direct ni le canal de diffusion.
/// Aucune règle métier ici : la clé envoyée est l'<b>identifiant stable</b>. Issues : succès →
/// <see cref="OnValide"/> (le parent ferme, accuse et relit la grille) ; refus métier (4xx) ou API
/// injoignable → message <b>dans</b> la dialog, la dialog reste ouverte.
/// </summary>
public partial class EditerPeriodeDialog
{
    /// <summary>Vue d'une période couvrant la date (miroir du DTO de lecture de l'API) : identifiant stable,
    /// identifiant stable du responsable (pré-sélection), nom d'affichage résolu côté API, bornes datées.</summary>
    public sealed record PeriodeDuJourVue(string Id, string ResponsableId, string ResponsableNom, DateTime Debut, DateTime Fin);

    private sealed class Formulaire
    {
        public string ResponsableId { get; set; } = "";
        public DateTime Debut { get; set; }
        public DateTime Fin { get; set; }
    }

    private List<PeriodeDuJourVue>? _periodes;
    private List<ActeurFoyer> _acteurs = new();
    private PeriodeDuJourVue? _enEdition;
    private readonly Formulaire _form = new();
    private string? _motifEchec;

    /// <summary>Date de la case cliquée : on liste les périodes qui la couvrent.</summary>
    [Parameter, EditorRequired]
    public DateOnly DateContexte { get; set; }

    /// <summary>Notifié sur édition aboutie : le parent ferme la dialog, accuse et relit la grille.</summary>
    [Parameter]
    public EventCallback OnValide { get; set; }

    /// <summary>Notifié sur fermeture sans édition : le parent ferme la dialog sans aucune écriture.</summary>
    [Parameter]
    public EventCallback OnAnnule { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await ChargerActeurs();
        await ChargerPeriodes();
    }

    private async Task ChargerActeurs()
    {
        try
        {
            _acteurs = await Canal.GetFromJsonAsync<List<ActeurFoyer>>("api/foyer/acteurs") ?? new List<ActeurFoyer>();
        }
        catch (HttpRequestException)
        {
            // Référentiel distant injoignable : le sélecteur reste vide plutôt que de planter la dialog.
            _acteurs = new List<ActeurFoyer>();
        }
    }

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
            // aucune liste fabriquée (aucune édition possible tant que la lecture n'a pas abouti).
            _periodes = new List<PeriodeDuJourVue>();
            _motifEchec = MessagesEcriture.ServiceInjoignable;
        }
    }

    /// <summary>Ouvre le formulaire pré-rempli sur la période choisie (responsable courant + bornes).</summary>
    private void Editer(PeriodeDuJourVue periode)
    {
        _motifEchec = null;
        _enEdition = periode;
        _form.ResponsableId = periode.ResponsableId;
        _form.Debut = periode.Debut;
        _form.Fin = periode.Fin;
    }

    private async Task Enregistrer()
    {
        if (_enEdition is null)
            return;

        _motifEchec = null;

        HttpResponseMessage reponse;
        try
        {
            reponse = await Canal.PostAsJsonAsync(
                "api/canal/editer-periode",
                new EditerPeriodeRequete(_enEdition.Id, _form.ResponsableId, _form.Debut, _form.Fin));
        }
        catch (HttpRequestException)
        {
            // API distante injoignable (échec de transport) : message dédié, dialog conservée ouverte,
            // rien appliqué, aucune mise en file ni rejeu (Sc.10, règle 28).
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
