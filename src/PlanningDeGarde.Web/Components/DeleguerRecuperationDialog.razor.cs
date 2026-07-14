using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using static PlanningDeGarde.Web.CanalEcriture;

namespace PlanningDeGarde.Web.Components;

/// <summary>
/// Mini-dialog « Déléguer ce jour » (s44) : choix de l'acteur RECEVANT, émission de la commande de
/// délégation via le <b>canal requête/réponse</b> (endpoint HTTP <c>/api/canal/deleguer-recuperation</c>)
/// — JAMAIS le canal de diffusion. Aucune règle métier ici (le refus « soi-même » / « inconnu » est tranché
/// côté domaine). Issues : succès → <see cref="OnValide"/> ; refus métier (motif propagé) ou API injoignable
/// → message <b>dans</b> la dialog, saisie <b>conservée</b>, dialog restée OUVERTE (Sc.5). Portée par
/// <c>ModalConfig</c> : Échap = « Annuler » (port <see cref="IEcouteurEchapModal"/> s33).
/// </summary>
public partial class DeleguerRecuperationDialog
{
    private sealed class Formulaire
    {
        public string VersActeurId { get; set; } = "";
    }

    private readonly Formulaire _form = new();
    private string? _motifEchec;

    /// <summary>Acteurs éligibles du foyer (id stable + nom), fournis par le parent depuis le store vivant :
    /// le sélecteur ne propose que ces acteurs réels (jamais un libellé en dur).</summary>
    [Parameter]
    public IReadOnlyList<ActeurFoyer> Acteurs { get; set; } = Array.Empty<ActeurFoyer>();

    /// <summary>Vrai une fois l'énumération du store chargée par le parent : distingue « en cours de
    /// chargement » de « chargé et vide » (invite à ajouter un acteur, sans flash transitoire).</summary>
    [Parameter]
    public bool ActeursCharges { get; set; }

    /// <summary>Le jour dont on délègue la récupération (carte « Aujourd'hui » ou un jour du panneau à-venir).</summary>
    [Parameter, EditorRequired]
    public DateOnly DateContexte { get; set; }

    /// <summary>Enfant sélectionné (s42/s43) dont on délègue la récupération ce jour-là.</summary>
    [Parameter, EditorRequired]
    public string EnfantId { get; set; } = "";

    /// <summary>Notifié sur écriture aboutie (succès) : le parent ferme la dialog et relit la grille.</summary>
    [Parameter]
    public EventCallback OnValide { get; set; }

    /// <summary>Notifié sur annulation (bouton ou Échap) : le parent ferme la dialog sans aucune écriture.</summary>
    [Parameter]
    public EventCallback OnAnnule { get; set; }

    private async Task Soumettre()
    {
        _motifEchec = null;

        HttpResponseMessage reponse;
        try
        {
            reponse = await Canal.PostAsJsonAsync(
                "api/canal/deleguer-recuperation",
                new DeleguerRecuperationRequete(DateContexte, EnfantId, _form.VersActeurId));
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
