using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using static PlanningDeGarde.Web.CanalEcriture;

namespace PlanningDeGarde.Web.Components.Recurrents;

/// <summary>
/// Dialog <b>partagée</b> d'édition d'une série récurrente d'un enfant (lieu, jours, plage horaire),
/// réutilisée <b>hors /configuration</b> (ouverte au clic sur une activité récurrente de la grille, décision
/// PO post-s54) comme dans l'écran de configuration. Elle porte, en <b>mode édition</b>, la gestion des
/// <b>vacances</b> (plages d'exclusion — fusion de l'ex-dialog Vacances autonome, n°3) ET la <b>suppression</b>
/// avec portée : « cette occurrence » (quand un <see cref="DateOccurrence"/> est fourni, i.e. depuis la grille)
/// et « toute la série » (toujours). Le composant est <b>autonome</b> : il charge lui-même les lieux et la
/// série via le canal HTTP, écrit via le canal requête/réponse, et prévient le parent par <see cref="OnFerme"/>
/// (le parent referme et relit sa vue). Aucune écriture du domaine en direct (tout passe par l'API distante).
/// </summary>
public partial class EditerSerieRecurrenteDialog
{
    /// <summary>Enfant propriétaire de la série (porté par l'URL des sous-ressources récurrentes).</summary>
    [Parameter, EditorRequired] public string EnfantId { get; set; } = "";

    /// <summary>Identifiant stable de la série éditée ; <c>null</c> = mode CRÉATION (aucune série encore).</summary>
    [Parameter] public string? SerieId { get; set; }

    /// <summary>Date de l'occurrence ciblée quand la dialog est ouverte depuis la GRILLE (contexte du clic) :
    /// active la suppression « cette occurrence » (exception par date, s54 S9). <c>null</c> depuis la config
    /// (aucune date de contexte) → seule « toute la série » est offerte.</summary>
    [Parameter] public DateOnly? DateOccurrence { get; set; }

    /// <summary>Notifie le parent que la dialog se referme (annulation, enregistrement ou suppression aboutis)
    /// : le parent referme et relit sa vue (liste de config OU grille). Aucune commande émise ici.</summary>
    [Parameter] public EventCallback OnFerme { get; set; }

    [Inject] private HttpClient Canal { get; set; } = default!;

    private IReadOnlyList<ActiviteFoyer> _lieux = Array.Empty<ActiviteFoyer>();
    private string _lieuId = "";
    private readonly HashSet<DayOfWeek> _jours = new();
    private TimeOnly _heureDebut = new(8, 30);
    private TimeOnly _heureFin = new(16, 30);
    private List<PlageVacancesWeb> _exclusions = new();
    private string? _motifEchec;
    private bool _scopeSuppression; // vrai quand on a déplié le choix de portée de suppression
    private DateOnly _vacancesDu = DateOnly.FromDateTime(DateTime.Today);
    private DateOnly _vacancesAu = DateOnly.FromDateTime(DateTime.Today).AddDays(7);

    private bool EstEdition => SerieId is not null;

    /// <summary>Jours de la semaine dans l'ordre lundi→dimanche (sélecteur de récurrence).</summary>
    private static readonly (DayOfWeek Jour, string Libelle)[] JoursSemaine =
    {
        (DayOfWeek.Monday, "Lun"), (DayOfWeek.Tuesday, "Mar"), (DayOfWeek.Wednesday, "Mer"),
        (DayOfWeek.Thursday, "Jeu"), (DayOfWeek.Friday, "Ven"), (DayOfWeek.Saturday, "Sam"), (DayOfWeek.Sunday, "Dim"),
    };

    protected override async Task OnInitializedAsync()
    {
        _lieux = await Canal.GetFromJsonAsync<List<ActiviteFoyer>>("api/foyer/lieux") ?? new List<ActiviteFoyer>();
        if (EstEdition)
            await RechargerSerie(peuplerFormulaire: true);
    }

    /// <summary>Recharge la série depuis le store (GET liste, retrouve par id) : peuple le formulaire à
    /// l'ouverture, et resynchronise seulement les exclusions après une écriture vacances (formulaire intact).</summary>
    private async Task RechargerSerie(bool peuplerFormulaire)
    {
        var series = await Canal.GetFromJsonAsync<List<ActiviteRecurrenteVueWeb>>(
            $"api/enfants/{EnfantId}/activites/recurrentes") ?? new List<ActiviteRecurrenteVueWeb>();
        var serie = series.FirstOrDefault(s => s.Id == SerieId);
        if (serie is null)
            return;
        _exclusions = serie.Exclusions;
        if (peuplerFormulaire)
        {
            _lieuId = serie.LieuId;
            _jours.Clear();
            foreach (var j in serie.Jours) _jours.Add(j);
            _heureDebut = TimeOnly.FromTimeSpan(serie.HeureDebut);
            _heureFin = TimeOnly.FromTimeSpan(serie.HeureFin);
        }
    }

    private void BasculerJour(DayOfWeek jour, bool coche)
    {
        if (coche) _jours.Add(jour);
        else _jours.Remove(jour);
    }

    /// <summary>Enregistre la série (création OU édition de TOUTE la série) via le canal HTTP réel, puis
    /// notifie le parent (fermeture + relecture). Sur refus, le motif reste DANS la dialog restée ouverte.</summary>
    private async Task Enregistrer()
    {
        _motifEchec = null;
        var jours = JoursSemaine.Select(j => j.Jour).Where(_jours.Contains).ToList();

        HttpResponseMessage reponse;
        if (!EstEdition)
        {
            reponse = await Canal.PostAsJsonAsync(
                $"api/enfants/{EnfantId}/activites/recurrentes",
                new PoserSlotRecurrentRequete(
                    _lieuId, jours.FirstOrDefault(), _heureDebut.ToTimeSpan(), _heureFin.ToTimeSpan(),
                    JoursDeSemaine: jours));
        }
        else
        {
            reponse = await Canal.PutAsJsonAsync(
                $"api/enfants/{EnfantId}/activites/recurrentes/{SerieId}",
                new ModifierSlotRecurrentCorps(_lieuId, jours, _heureDebut.ToTimeSpan(), _heureFin.ToTimeSpan()));
        }

        if (reponse.IsSuccessStatusCode)
            await OnFerme.InvokeAsync();
        else
            _motifEchec = await reponse.Content.ReadAsStringAsync();
    }

    private async Task AjouterVacances()
    {
        _motifEchec = null;
        var reponse = await Canal.PostAsJsonAsync(
            $"api/enfants/{EnfantId}/activites/recurrentes/{SerieId}/exclusions",
            new ExclusionCorps(_vacancesDu, _vacancesAu));
        if (reponse.IsSuccessStatusCode)
            await RechargerSerie(peuplerFormulaire: false); // la dialog reste ouverte ; seules les plages suivent
        else
            _motifEchec = await reponse.Content.ReadAsStringAsync();
    }

    private async Task SupprimerVacances(PlageVacancesWeb plage)
    {
        _motifEchec = null;
        var requete = new HttpRequestMessage(
            HttpMethod.Delete,
            $"api/enfants/{EnfantId}/activites/recurrentes/{SerieId}/exclusions")
        {
            Content = JsonContent.Create(new ExclusionCorps(plage.Debut, plage.Fin)),
        };
        var reponse = await Canal.SendAsync(requete);
        if (reponse.IsSuccessStatusCode)
            await RechargerSerie(peuplerFormulaire: false);
        else
            _motifEchec = await reponse.Content.ReadAsStringAsync();
    }

    /// <summary>« Cette occurrence » (grille, s54 S9) : exception par date — la série continue. Puis fermeture.</summary>
    private async Task SupprimerCetteOccurrence()
    {
        if (DateOccurrence is not { } date)
            return;
        _motifEchec = null;
        var reponse = await Canal.DeleteAsync(
            $"api/enfants/{EnfantId}/activites/recurrentes/{SerieId}/occurrences/{date.Year}/{date.Month}/{date.Day}");
        if (reponse.IsSuccessStatusCode)
            await OnFerme.InvokeAsync();
        else
            _motifEchec = await reponse.Content.ReadAsStringAsync();
    }

    /// <summary>« Toute la série » (s54 S5) : suppression du récurrent entier. Puis fermeture.</summary>
    private async Task SupprimerSerie()
    {
        _motifEchec = null;
        var reponse = await Canal.DeleteAsync($"api/enfants/{EnfantId}/activites/recurrentes/{SerieId}");
        if (reponse.IsSuccessStatusCode)
            await OnFerme.InvokeAsync();
        else
            _motifEchec = await reponse.Content.ReadAsStringAsync();
    }

    private Task Annuler() => OnFerme.InvokeAsync();
}
