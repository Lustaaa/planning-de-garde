using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Web.State;
using static PlanningDeGarde.Web.CanalEcriture;

namespace PlanningDeGarde.Web.Components.Imprevus;

/// <summary>
/// Mini-dialog « Signaler un imprévu » : choix du TYPE (malade / retard) + motif OPTIONNEL, émission de la
/// commande via le <b>canal requête/réponse</b> (endpoint HTTP <c>/api/imprevus</c>) — JAMAIS le
/// canal de diffusion. Aucune règle métier ici (le refus « type inconnu » est tranché côté domaine, AVANT toute
/// écriture). Le signalement est purement INFORMATIF : il N'ÉCRIT AUCUNE surcharge, la résolution du planning
/// reste inchangée (invariant) ; la cloche des concernés reprojette la notification. L'acteur SIGNALANT est
/// l'identité effective de la session (jamais un champ saisi). Issues : succès → <see cref="OnValide"/> ; refus
/// métier (motif propagé) ou API injoignable → message <b>dans</b> la dialog, saisie <b>conservée</b>, dialog
/// restée OUVERTE. Portée par <c>ModalConfig</c> : Échap = « Annuler » (port <see cref="IEcouteurEchapModal"/>).
/// </summary>
public partial class SignalerImprevuDialog
{
    private sealed class Formulaire
    {
        public TypeImprevu Type { get; set; } = TypeImprevu.Malade;
        public string Motif { get; set; } = "";
    }

    private readonly Formulaire _form = new();
    private string? _motifEchec;

    [Inject] private SessionPlanning Session { get; set; } = default!;

    /// <summary>Le jour de la case cliquée (menu clic-case) sur lequel porte l'imprévu.</summary>
    [Parameter, EditorRequired]
    public DateOnly DateContexte { get; set; }

    /// <summary>Enfant sélectionné (parité) concerné par l'imprévu ce jour-là.</summary>
    [Parameter, EditorRequired]
    public string EnfantId { get; set; } = "";

    /// <summary>Notifié sur écriture aboutie (succès) : le parent ferme la dialog.</summary>
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
                "api/imprevus",
                new SignalerImprevuRequete(DateContexte, EnfantId, _form.Type, Session.IdentiteEffective.Id, _form.Motif));
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
