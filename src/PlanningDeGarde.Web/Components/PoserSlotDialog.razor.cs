using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using PlanningDeGarde.Web.State;
using static PlanningDeGarde.Web.CanalEcriture;

namespace PlanningDeGarde.Web.Components;

/// <summary>
/// Dialog (modal) « Poser un slot » <b>unifiée</b>, ouverte depuis une case du planning (palier 7, écriture
/// en contexte). Elle porte DEUX chemins d'écriture derrière un seul formulaire (retour PO G3) : un slot
/// <b>ponctuel</b> (endpoint <c>/api/canal/poser-slot</c>, inchangé) ou — si « Répéter chaque semaine » est
/// coché — un slot <b>récurrent</b> hebdomadaire (endpoint <c>/api/canal/poser-slot-recurrent</c>, s29) dont
/// le jour de semaine est celui de la case cliquée. L'enfant n'est plus affiché (dette « déclaration des
/// enfants », backlog P1) mais reste transmis implicitement au back (<see cref="SessionPlanning.EnfantId"/>),
/// contrat inchangé. Aucune règle métier ici. Issues : succès → <see cref="OnValide"/> (le parent ferme et
/// relit la grille) ; refus métier (motif propagé) ou API injoignable → message <b>dans</b> la dialog.
/// </summary>
public partial class PoserSlotDialog
{
    private sealed class Formulaire
    {
        public string LieuId { get; set; } = "";
        // Slot ponctuel : bornes datées (date + heure).
        public DateTime Debut { get; set; }
        public DateTime Fin { get; set; }
        // Slot récurrent : plage horaire seule (le jour de semaine est déduit de la case).
        public TimeOnly HeureDebut { get; set; } = new(8, 30);
        public TimeOnly HeureFin { get; set; } = new(16, 30);
        // Option de récurrence : cochée = slot récurrent hebdomadaire ; décochée = slot ponctuel.
        public bool Repeter { get; set; }
    }

    private readonly Formulaire _form = new();
    private string? _motifEchec;

    /// <summary>Date de la case cliquée : elle <b>prime</b> comme date de contexte du slot ponctuel (bornes
    /// pré-remplies sur ce jour) ET fournit le <b>jour de semaine</b> de la récurrence quand « Répéter chaque
    /// semaine » est coché (le slot récurrent n'est pas daté : seul son jour de semaine est retenu).</summary>
    [Parameter, EditorRequired]
    public DateOnly DateContexte { get; set; }

    /// <summary>Lieux du référentiel du foyer (id stable + libellé), fournis par le parent depuis le store
    /// vivant (GET /api/foyer/lieux) : le sélecteur de lieu ne propose que ces lieux réels (jamais la liste
    /// en dur <c>Foyer.Lieux</c>), y compris un lieu fraîchement ajouté / privé d'un lieu supprimé, propagé
    /// en temps réel par le parent (S6).</summary>
    [Parameter]
    public IReadOnlyList<LieuFoyer> Lieux { get; set; } = Array.Empty<LieuFoyer>();

    /// <summary>Notifié sur écriture aboutie (succès) : le parent ferme la dialog et relit la grille.
    /// L'argument <c>bool</c> = un <b>chevauchement</b> a été signalé par l'outcome de la commande (règle 16,
    /// accepté + averti) → le parent affiche un bandeau à part, non bloquant (Sc.7). Un slot récurrent ne
    /// porte pas de chevauchement (présentation d'occurrences) : l'argument est alors <c>false</c>.</summary>
    [Parameter]
    public EventCallback<bool> OnValide { get; set; }

    /// <summary>Notifié sur annulation : le parent ferme la dialog sans aucune écriture.</summary>
    [Parameter]
    public EventCallback OnAnnule { get; set; }

    protected override void OnInitialized()
    {
        _form.Debut = DateContexte.ToDateTime(new TimeOnly(8, 30));
        _form.Fin = DateContexte.ToDateTime(new TimeOnly(16, 30));
    }

    private static string LibelleJour(DayOfWeek jour) => jour switch
    {
        DayOfWeek.Monday => "lundis",
        DayOfWeek.Tuesday => "mardis",
        DayOfWeek.Wednesday => "mercredis",
        DayOfWeek.Thursday => "jeudis",
        DayOfWeek.Friday => "vendredis",
        DayOfWeek.Saturday => "samedis",
        _ => "dimanches",
    };

    private async Task Soumettre()
    {
        _motifEchec = null;

        HttpResponseMessage reponse;
        try
        {
            reponse = _form.Repeter
                ? await Canal.PostAsJsonAsync(
                    "api/canal/poser-slot-recurrent",
                    new PoserSlotRecurrentRequete(Session.EnfantId, _form.LieuId, DateContexte.DayOfWeek, _form.HeureDebut.ToTimeSpan(), _form.HeureFin.ToTimeSpan()))
                : await Canal.PostAsJsonAsync(
                    "api/canal/poser-slot",
                    new PoserSlotRequete(Session.EnfantId, _form.LieuId, _form.Debut, _form.Fin));
        }
        catch (HttpRequestException)
        {
            // API distante injoignable (échec de transport) : message dédié, saisie conservée, pas de fermeture.
            _motifEchec = MessagesEcriture.ServiceInjoignable;
            return;
        }

        if (!reponse.IsSuccessStatusCode)
        {
            _motifEchec = await reponse.Content.ReadAsStringAsync();
            return;
        }

        if (_form.Repeter)
        {
            // Slot récurrent : aucun chevauchement à signaler (présentation d'occurrences).
            await OnValide.InvokeAsync(false);
            return;
        }

        // Slot ponctuel : l'outcome de la commande porte l'avertissement de chevauchement (résolu côté API
        // depuis le read model existant) : le front ne fait que le LIRE, jamais le recalculer.
        var corps = await reponse.Content.ReadFromJsonAsync<PoserSlotReponse>();
        await OnValide.InvokeAsync(corps?.Chevauchement ?? false);
    }

    private async Task Annuler() => await OnAnnule.InvokeAsync();
}
