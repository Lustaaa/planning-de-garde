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

    /// <summary>Acteurs DÉCLARÉS du foyer (id stable + nom), fournis par le parent depuis le store vivant :
    /// les sélecteurs dépose/récupère ne proposent que ces acteurs réels (jamais un libellé en dur), y
    /// compris un acteur fraîchement ajouté (sprint 19, Sc.5).</summary>
    [Parameter]
    public IReadOnlyList<ActeurFoyer> Acteurs { get; set; } = Array.Empty<ActeurFoyer>();

    /// <summary>Vrai une fois l'énumération du store chargée par le parent : distingue « en cours de
    /// chargement » de « chargé et vide » (store sans acteur, 1er lancement) — qui seul déclenche l'invite
    /// à ajouter un acteur (sprint 19, Sc.6), sans flash transitoire.</summary>
    [Parameter]
    public bool ActeursCharges { get; set; }

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

    protected override void OnInitialized()
        => _form.Date = DateContexte.ToDateTime(TimeOnly.MinValue);

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
