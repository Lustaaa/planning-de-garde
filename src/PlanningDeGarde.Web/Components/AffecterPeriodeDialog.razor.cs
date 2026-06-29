using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using static PlanningDeGarde.Web.CanalEcriture;

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
        // Sur une plage (Sc.5), Fin = borne de fin de l'intervalle ; sinon Fin = Début (une seule case).
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
