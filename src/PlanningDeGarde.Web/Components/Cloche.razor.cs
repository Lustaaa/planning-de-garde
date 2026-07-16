using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Web.State;
using static PlanningDeGarde.Web.CanalEcriture;

namespace PlanningDeGarde.Web.Components;

/// <summary>
/// CLOCHE de notifications (s47) en en-tête du planning : badge du compteur de non-lus + panneau déroulant
/// listant les changements récents (délégations / reprises / transferts — informationnels, lu/non-lu) et les
/// propositions d'échange ACTIONNABLES (Accepter / Refuser via mini-dialog de confirmation). Parent-gated
/// (un Invité ne voit pas la cloche). Le compteur/flux est chargé à l'ouverture (GET initial), puis CONVERGE
/// en temps réel par REPROJECTION depuis la diffusion PORTEUSE DE PAYLOAD (SignalR) — AUCUN GET sur push
/// (garde-fou anti-flake). L'écriture (marquer-lu / accepter / refuser) transite par le canal requête/réponse,
/// jamais la diffusion. Échap ferme le mini-dialog puis le panneau (port <see cref="IEcouteurEchapModal"/> s33).
/// </summary>
public partial class Cloche : IAsyncDisposable
{
    private List<NotificationCloche> _notifications = new();
    private int _nonLus;
    private bool _ouvert;
    private HubConnection? _hub;
    private IAsyncDisposable? _abonnementEchap;

    // Mini-dialog de confirmation d'une réponse à une proposition actionnable (Sc.8). null = fermé.
    private string? _confirmPropositionId;
    private bool _confirmAccepter;

    [Inject] private HttpClient Canal { get; set; } = default!;
    [Inject] private SessionPlanning Session { get; set; } = default!;
    [Inject] private OptionsConnexionHub OptionsHub { get; set; } = default!;

    // Port Échap (s33) résolu PARESSEUSEMENT via le provider (jamais un [Inject] dur) : la cloche est rendue
    // en permanence dans l'en-tête, on n'impose donc pas sa présence à tout hôte qui rend le planning (l'app
    // réelle l'enregistre — Program.cs ; un contexte de test qui n'exerce pas Échap peut l'omettre sans casser
    // le rendu). L'écoute document ne s'attache qu'à l'ouverture du panneau.
    [Inject] private IServiceProvider Fournisseur { get; set; } = default!;
    private IEcouteurEchapModal? Echap => Fournisseur.GetService(typeof(IEcouteurEchapModal)) as IEcouteurEchapModal;

    /// <summary>Référentiel des acteurs (déjà chargé par la page) pour résoudre les ids en noms — parité de
    /// rendu entre le chargement initial et la reprojection depuis la diffusion (qui ne porte que des ids).</summary>
    [Parameter] public IReadOnlyList<ActeurFoyer> Acteurs { get; set; } = Array.Empty<ActeurFoyer>();

    private string MonId => Session.IdentiteEffective.Id;

    protected override async Task OnInitializedAsync()
    {
        // Parent-gated : un Invité (ou une identité effective non Parent/Admin) ne voit pas la cloche.
        if (!Session.EstParent)
            return;
        await ChargerAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender || !Session.EstParent)
            return;
        try
        {
            var urlHub = new Uri(Canal.BaseAddress!, "hubs/planning");
            _hub = new HubConnectionBuilder()
                .WithUrl(urlHub, OptionsHub.Configurer)
                .WithAutomaticReconnect()
                .Build();

            // REPROJECTION depuis la diffusion PORTEUSE DE PAYLOAD (0 GET sur push) : un changement du journal
            // qui me concerne apparaît en tête + incrémente le compteur (Sc.4) ; une proposition me concernant
            // apparaît / se met à jour / se retire (Sc.9). AUCUN GET n'est déclenché sur le push.
            _hub.On<EvenementChangementSnapshot>(PlanningHubEvenement.Changement,
                e => InvokeAsync(() => ReprojeterChangement(e)));
            _hub.On<PropositionEchangeSnapshot>(PlanningHubEvenement.Proposition,
                p => InvokeAsync(() => ReprojeterProposition(p)));

            await _hub.StartAsync();
        }
        catch
        {
            // Le temps réel est un confort : hub indisponible → la cloche reste chargée (GET initial).
        }
    }

    /// <summary>Chargement INITIAL du flux (GET, autorisé — jamais sur push) : compteur + notifications chrono.</summary>
    private async Task ChargerAsync()
    {
        try
        {
            var cloche = await Canal.GetFromJsonAsync<ClocheChargement>($"api/notifications/{MonId}");
            if (cloche is not null)
            {
                _notifications = cloche.Notifications.ToList();
                _nonLus = cloche.NonLus;
            }
        }
        catch (HttpRequestException)
        {
            // API distante injoignable : la cloche reste vide plutôt que de planter la vue.
        }
    }

    private void ReprojeterChangement(EvenementChangementSnapshot e)
    {
        if (e.CedantId != MonId && e.RecevantId != MonId)
            return; // ne me concerne pas
        if (_notifications.Any(n => n.Id == e.Id))
            return; // reprojection IDEMPOTENTE : une même diffusion re-reçue (reconnexion) ne double pas le compteur
        _notifications.Insert(0, new NotificationCloche(
            e.Id, e.Type.ToString().ToLowerInvariant(), e.Jour, e.EnfantId, e.CedantId, e.RecevantId,
            e.Horodatage, false, false, null, "changement"));
        RecalculerNonLus();
        StateHasChanged();
    }

    private void ReprojeterProposition(PropositionEchangeSnapshot p)
    {
        if (p.VersActeurId != MonId && p.DeActeurId != MonId)
            return; // ne me concerne pas
        _notifications.RemoveAll(n => n.PropositionId == p.Id);
        if (p.Statut != StatutProposition.Refusee)
        {
            _notifications.Insert(0, new NotificationCloche(
                p.Id, "echange", p.Jour, p.EnfantId, p.DeActeurId, p.VersActeurId, DateTime.MaxValue,
                p.Statut != StatutProposition.Proposee,
                p.Statut == StatutProposition.Proposee && p.VersActeurId == MonId,
                p.Id, p.Statut.ToString().ToLowerInvariant()));
        }
        RecalculerNonLus();
        StateHasChanged();
    }

    /// <summary>Compteur de non-lus = événements du journal non lus + propositions actionnables (pending
    /// adressées à moi). Recalculé après chaque reprojection / marquage (source unique = la liste en mémoire).</summary>
    private void RecalculerNonLus()
        => _nonLus = _notifications.Count(n => n.Type == "echange" ? n.Actionnable : !n.Lu);

    private async Task BasculerPanneau()
    {
        _ouvert = !_ouvert;
        if (_ouvert && Echap is { } ecouteur)
            _abonnementEchap = await ecouteur.EcouterAsync(SurEchap);
        else if (!_ouvert)
            await DetacherEchap();
    }

    /// <summary>Échap (port document s33) : ferme d'abord le mini-dialog de confirmation s'il est ouvert,
    /// sinon ferme le panneau — sans aucune commande (Annuler).</summary>
    private async Task SurEchap()
    {
        if (_confirmPropositionId is not null)
        {
            _confirmPropositionId = null;
            StateHasChanged();
            return;
        }
        _ouvert = false;
        await DetacherEchap();
        StateHasChanged();
    }

    private async Task DetacherEchap()
    {
        if (_abonnementEchap is not null)
        {
            await _abonnementEchap.DisposeAsync();
            _abonnementEchap = null;
        }
    }

    private async Task MarquerLu(string evenementId)
    {
        var reponse = await Canal.PostAsJsonAsync(
            "api/canal/marquer-notifications-lues", new MarquerNotificationsLuesRequete(MonId, evenementId));
        if (!reponse.IsSuccessStatusCode)
            return;
        // État lu/non-lu PRIVÉ (aucune diffusion) : le badge décroît sur la reprojection LOCALE de l'acteur.
        _notifications = _notifications.Select(n => n.Id == evenementId ? n with { Lu = true } : n).ToList();
        RecalculerNonLus();
    }

    private async Task MarquerToutLu()
    {
        var reponse = await Canal.PostAsJsonAsync(
            "api/canal/marquer-notifications-lues", new MarquerNotificationsLuesRequete(MonId, null));
        if (!reponse.IsSuccessStatusCode)
            return;
        // « Marquer tout lu » ne concerne que les événements du journal (les propositions restent actionnables).
        _notifications = _notifications.Select(n => n.Type == "echange" ? n : n with { Lu = true }).ToList();
        RecalculerNonLus();
    }

    private void DemanderAccepter(string propositionId) => OuvrirConfirmation(propositionId, true);
    private void DemanderRefuser(string propositionId) => OuvrirConfirmation(propositionId, false);

    private void OuvrirConfirmation(string propositionId, bool accepter)
    {
        _confirmPropositionId = propositionId;
        _confirmAccepter = accepter;
    }

    private void AnnulerConfirmation() => _confirmPropositionId = null;

    private async Task Confirmer()
    {
        var propositionId = _confirmPropositionId!;
        _confirmPropositionId = null;
        var endpoint = _confirmAccepter ? "api/canal/accepter-proposition" : "api/canal/refuser-proposition";
        // Émission par le CANAL D'ÉCRITURE (jamais la diffusion). La convergence de la notif (accepté / retirée)
        // arrive par REPROJECTION depuis la diffusion Proposition (le serveur re-diffuse à tous, dont l'émetteur).
        await Canal.PostAsJsonAsync(endpoint, new RepondrePropositionRequete(propositionId));
    }

    private string Nom(string id) => Acteurs.FirstOrDefault(a => a.Id == id)?.Nom ?? id;

    /// <summary>Libellé humain d'une notification (résolution des ids en noms sur le référentiel chargé).</summary>
    private string Libelle(NotificationCloche n) => n.Type switch
    {
        "delegation" => $"Délégation du {n.Jour:dd/MM} : {Nom(n.CedantId)} → {Nom(n.RecevantId)}",
        "reprise" => $"Reprise du {n.Jour:dd/MM} : {Nom(n.CedantId)}",
        "transfert" => $"Transfert du {n.Jour:dd/MM} : {Nom(n.CedantId)} → {Nom(n.RecevantId)}",
        "echange" when n.Statut == "acceptee" => $"Échange du {n.Jour:dd/MM} accepté : {Nom(n.CedantId)} → {Nom(n.RecevantId)}",
        _ => $"Échange proposé du {n.Jour:dd/MM} : {Nom(n.CedantId)} → {Nom(n.RecevantId)}",
    };

    public async ValueTask DisposeAsync()
    {
        await DetacherEchap();
        if (_hub is not null)
            await _hub.DisposeAsync();
    }
}
