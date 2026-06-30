using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using static PlanningDeGarde.Web.CanalEcriture;

namespace PlanningDeGarde.Web.Components;

/// <summary>
/// Dialog (modal) « Définir un transfert » réutilisable, ouverte depuis le menu d'actions d'une case
/// (palier 7, écriture en contexte — 3ᵉ dialog qui referme l'épic É12). L'écriture passe par le
/// <b>canal requête/réponse</b> (endpoint HTTP <c>/api/canal/definir-transfert</c>) — JAMAIS un handler
/// en DI direct ni le canal de diffusion. Aucune règle métier ici. Les sélecteurs dépose/récupère
/// bindent l'<b>identifiant stable</b> (clé atteignable du référentiel, règle 19), jamais le libellé.
/// Issues : succès → <see cref="OnValide"/> (le parent ferme la dialog, l'accusé « Transfert défini »
/// s'affiche à part) ; refus métier (4xx, motif propagé) ou API injoignable → message <b>dans</b> la
/// dialog, saisie conservée, aucune fermeture.
/// </summary>
public partial class DefinirTransfertDialog
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

    /// <summary>Acteurs du foyer énumérés <b>depuis le store vivant</b> (canal de lecture HTTP
    /// <c>GET /api/foyer/acteurs</c>) : les sélecteurs dépose/récupère ne proposent que les acteurs RÉELS
    /// déclarés (id stable, jamais le libellé), y compris un acteur fraîchement ajouté (sprint 19, Sc.5).</summary>
    private List<ActeurFoyer> _acteurs = new();

    /// <summary>Date de la case cliquée : elle ancre le transfert sur ce seul jour (la date de
    /// contexte prime sur le défaut « aujourd'hui », règle 17 composée, Sc.2).</summary>
    [Parameter, EditorRequired]
    public DateOnly DateContexte { get; set; }

    /// <summary>Notifié sur écriture aboutie (succès) : le parent ferme la dialog et lève l'accusé
    /// « Transfert défini » à part, non bloquant.</summary>
    [Parameter]
    public EventCallback OnValide { get; set; }

    /// <summary>Notifié sur annulation : le parent ferme la dialog sans aucune écriture.</summary>
    [Parameter]
    public EventCallback OnAnnule { get; set; }

    protected override async Task OnInitializedAsync()
    {
        _form.Date = DateContexte.ToDateTime(TimeOnly.MinValue);
        await ChargerActeurs();
    }

    /// <summary>Charge les acteurs déclarés du foyer depuis le store via l'API distante. Référentiel
    /// distant injoignable → sélecteurs vides plutôt que dialog plantée (parité EditerPeriodeDialog).</summary>
    private async Task ChargerActeurs()
    {
        try
        {
            _acteurs = await Canal.GetFromJsonAsync<List<ActeurFoyer>>("api/foyer/acteurs") ?? new List<ActeurFoyer>();
        }
        catch (HttpRequestException)
        {
            _acteurs = new List<ActeurFoyer>();
        }
    }

    private async Task Soumettre()
    {
        _motifEchec = null;
        var heure = _heure?.ToTimeSpan() ?? TimeSpan.Zero;

        HttpResponseMessage reponse;
        try
        {
            reponse = await Canal.PostAsJsonAsync(
                "api/canal/definir-transfert",
                new DefinirTransfertRequete(_form.DeposeParId, _form.RecupereParId, _form.LieuId, heure, _form.Date));
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
