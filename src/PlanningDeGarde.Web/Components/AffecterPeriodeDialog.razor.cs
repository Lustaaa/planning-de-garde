using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using static PlanningDeGarde.Web.CanalEcriture;

// Le sélecteur de responsable est alimenté par la liste d'acteurs déclarés passée en PARAMÈTRE par le
// parent (PlanningPartage), rafraîchie à l'ouverture (fetch-on-open) et en temps réel : la dialog ne
// charge plus rien elle-même (aucun re-render async pendant la saisie).

namespace PlanningDeGarde.Web.Components;

/// <summary>
/// Dialog (modal) « Affecter une période » réutilisable, ouverte depuis le menu d'actions d'une case
/// (palier 7, écriture en contexte). L'écriture passe par le <b>canal requête/réponse</b> (endpoint
/// HTTP <c>/api/canal/affecter-periode</c>) — JAMAIS un handler en DI direct ni le canal de diffusion.
/// Aucune règle métier ici. Le sélecteur bind l'<b>identifiant stable</b> (clé atteignable de la
/// palette/du référentiel, cadrage (B) du s06). Issues : succès → <see cref="OnValide"/> ; refus métier
/// (4xx, motif propagé) ou API injoignable → message <b>dans</b> la dialog, saisie conservée.
/// </summary>
public partial class AffecterPeriodeDialog
{
    private sealed class Formulaire
    {
        public string ResponsableId { get; set; } = "";
        public DateTime Debut { get; set; }
        public DateTime Fin { get; set; }
    }

    private readonly Formulaire _form = new();
    private string? _motifEchec;

    /// <summary>Acteurs DÉCLARÉS du foyer (id stable + nom), fournis par le parent depuis le store vivant :
    /// le sélecteur ne propose que ces acteurs réels (jamais un libellé en dur), y compris un acteur
    /// fraîchement ajouté (sprint 19, Sc.5).</summary>
    [Parameter]
    public IReadOnlyList<ActeurFoyer> Acteurs { get; set; } = Array.Empty<ActeurFoyer>();

    /// <summary>Vrai une fois l'énumération du store chargée par le parent : distingue « en cours de
    /// chargement » de « chargé et vide » (store sans acteur, 1er lancement) — qui seul déclenche l'invite
    /// à ajouter un acteur (sprint 19, Sc.6), sans flash transitoire.</summary>
    [Parameter]
    public bool ActeursCharges { get; set; }

    /// <summary>Date de la case cliquée : elle ancre l'affectation sur ce jour (la date de
    /// contexte prime sur le défaut « aujourd'hui », règle 17 composée). En contexte de plage (Sc.5),
    /// c'est la borne de DÉBUT de l'intervalle sélectionné.</summary>
    [Parameter, EditorRequired]
    public DateOnly DateContexte { get; set; }

    /// <summary>Borne de FIN de l'intervalle quand l'affectation est ouverte depuis une <b>sélection de
    /// plage</b> de cases contiguës (Sc.5) : la dialog est alors pré-remplie sur <c>[DateContexte,
    /// DateFinContexte]</c> et émet UNE seule commande <c>AffecterPeriode</c> couvrant l'intervalle.
    /// <c>null</c> = ouverture sur une seule case (comportement palier 7 inchangé : Début = Fin = date).</summary>
    [Parameter]
    public DateOnly? DateFinContexte { get; set; }

    /// <summary>Notifié sur écriture aboutie (succès) : le parent ferme la dialog et relit la grille.</summary>
    [Parameter]
    public EventCallback OnValide { get; set; }

    /// <summary>Notifié sur annulation : le parent ferme la dialog sans aucune écriture.</summary>
    [Parameter]
    public EventCallback OnAnnule { get; set; }

    protected override void OnInitialized()
    {
        _form.Debut = DateContexte.ToDateTime(TimeOnly.MinValue);
        // Sur une plage (Sc.5 s11), Fin = borne de fin de l'intervalle ; sinon Fin = Début (une seule case).
        _form.Fin = (DateFinContexte ?? DateContexte).ToDateTime(TimeOnly.MinValue);
    }

    private async Task Soumettre()
    {
        _motifEchec = null;

        HttpResponseMessage reponse;
        try
        {
            reponse = await Canal.PostAsJsonAsync(
                "api/canal/affecter-periode",
                new AffecterPeriodeRequete(_form.ResponsableId, _form.Debut, _form.Fin));
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
