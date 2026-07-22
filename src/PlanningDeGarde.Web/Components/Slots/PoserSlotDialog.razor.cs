using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using PlanningDeGarde.Web.State;
using static PlanningDeGarde.Web.CanalEcriture;

namespace PlanningDeGarde.Web.Components.Slots;

/// <summary>
/// Dialog (modal) « Poser un slot » <b>unifiée</b>, ouverte depuis une case du planning (écriture
/// en contexte). Elle porte DEUX chemins d'écriture derrière un seul formulaire : un slot
/// <b>ponctuel</b> (endpoint <c>/api/slots</c>, inchangé) ou — si « Répéter chaque semaine » est
/// coché — un slot <b>récurrent</b> hebdomadaire (endpoint <c>/api/slots/recurrents</c>) dont
/// le jour de semaine est celui de la case cliquée. L'enfant n'est plus affiché (dette « déclaration des
/// enfants », backlog P1) mais reste transmis implicitement au back (<see cref="SessionPlanning.EnfantId"/>),
/// contrat inchangé. Aucune règle métier ici. Issues : succès → <see cref="OnValide"/> (le parent ferme et
/// relit la grille) ; refus métier (motif propagé) ou API injoignable → message <b>dans</b> la dialog.
/// </summary>
public partial class PoserSlotDialog
{
    private sealed class Formulaire
    {
        // Enfant CHOISI explicitement (s30 S10) : remplace le fantôme Session.EnfantId transmis à l'aveugle
        // (s29). Bindé par le sélecteur d'enfant, transmis tel quel aux DEUX chemins d'écriture (ponctuel /
        // récurrent). Pré-sélectionné sur le premier enfant du foyer à l'ouverture (OnInitialized).
        public string EnfantId { get; set; } = "";
        public string LieuId { get; set; } = "";
        // Slot ponctuel : bornes datées (date + heure).
        public DateTime Debut { get; set; }
        public DateTime Fin { get; set; }
        // Slot récurrent : plage horaire seule (le jour de semaine est déduit de la case).
        public TimeOnly HeureDebut { get; set; } = new(8, 30);
        public TimeOnly HeureFin { get; set; } = new(16, 30);
        // Option de récurrence : cochée = slot récurrent hebdomadaire ; décochée = slot ponctuel.
        public bool Repeter { get; set; }
        // D1 (s31) : conditionne le slot récurrent à la garde (« seulement les jours où l'enfant est chez
        // moi »). Décochée par défaut = comportement s29 inchangé. Pertinente seulement quand Repeter.
        public bool ConditionneGarde { get; set; }
    }

    private readonly Formulaire _form = new();
    private string? _motifEchec;

    /// <summary>Date de la case cliquée : elle <b>prime</b> comme date de contexte du slot ponctuel (bornes
    /// pré-remplies sur ce jour) ET fournit le <b>jour de semaine</b> de la récurrence quand « Répéter chaque
    /// semaine » est coché (le slot récurrent n'est pas daté : seul son jour de semaine est retenu).</summary>
    [Parameter, EditorRequired]
    public DateOnly DateContexte { get; set; }

    /// <summary>Activités du référentiel du foyer (id stable + libellé), fournies par le parent depuis le store
    /// vivant (GET /api/foyer/activites) : le sélecteur de lieu (axe LOCALISATION du slot, préservé) ne
    /// propose que ces activités réelles (jamais la liste en dur), y compris une activité fraîchement ajoutée /
    /// privée d'une activité supprimée, propagée en temps réel par le parent.</summary>
    [Parameter]
    public IReadOnlyList<ActiviteFoyer> Lieux { get; set; } = Array.Empty<ActiviteFoyer>();

    /// <summary>Enfants du référentiel du foyer (id stable opaque + prénom), fournis par le parent depuis le
    /// store vivant (GET /api/foyer/enfants) : le sélecteur d'enfant ne propose que ces enfants réels (jamais
    /// un enfant en dur / fantôme), y compris un enfant fraîchement ajouté en config, propagé en temps réel.
    /// L'enfant choisi remplace <c>Session.EnfantId</c> transmis à l'aveugle.</summary>
    [Parameter]
    public IReadOnlyList<EnfantFoyer> Enfants { get; set; } = Array.Empty<EnfantFoyer>();

    /// <summary>Notifié sur écriture aboutie (succès) : le parent ferme la dialog et relit la grille.
    /// L'argument <c>bool</c> = un <b>chevauchement</b> a été signalé par l'outcome de la commande (
    /// accepté + averti) → le parent affiche un bandeau à part, non bloquant. Un slot récurrent ne
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
        // Pré-sélection du premier enfant du foyer (le sélecteur reste explicite : visible et modifiable).
        // Un foyer part toujours avec ≥1 enfant (R1, garanti par la migration s30) : la pose d'un slot n'est
        // donc jamais bloquée par un sélecteur vide dans le cas nominal.
        _form.EnfantId = Enfants.FirstOrDefault()?.Id ?? "";
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
                    "api/slots/recurrents",
                    new PoserSlotRecurrentRequete(
                        _form.EnfantId, _form.LieuId, DateContexte.DayOfWeek, _form.HeureDebut.ToTimeSpan(), _form.HeureFin.ToTimeSpan(),
                        // D1 (s31) : le conditionnement à la garde porte l'identité du parent COURANT (identité
                        // effective de la session) comme poseur — sa responsabilité pilote la projection du slot.
                        _form.ConditionneGarde, Session.IdentiteEffective.Id))
                : await Canal.PostAsJsonAsync(
                    "api/slots",
                    new PoserSlotRequete(_form.EnfantId, _form.LieuId, _form.Debut, _form.Fin));
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
