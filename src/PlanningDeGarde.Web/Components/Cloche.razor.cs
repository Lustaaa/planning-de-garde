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
/// CLOCHE de notifications (s47) dans la BARRE D'APPLICATION (MainLayout, retour PO au gate visuel) : badge du
/// compteur de non-lus + panneau déroulant listant les changements récents (délégations / reprises / transferts
/// — informationnels, lu/non-lu) et les propositions d'échange ACTIONNABLES (Accepter / Refuser via mini-dialog
/// de confirmation). Gating identique au menu utilisateur (rien hors session) + Parent-gated (un Invité ne voit
/// pas la cloche) : elle ne s'affiche donc PAS sur l'écran de connexion ni pour un non-Parent. Le composant est
/// AUTONOME dans le layout — il charge lui-même son référentiel d'acteurs (résolution ids→noms) et son flux, se
/// connecte à SignalR, et suit la session (il s'abonne à <see cref="SessionPlanning.EtatConnexionChange"/> pour
/// (re)charger quand une connexion survient pendant la vie du layout, présent sur toutes les routes). Le
/// compteur/flux est chargé à l'ouverture (GET initial), puis CONVERGE en temps réel par REPROJECTION depuis la
/// diffusion PORTEUSE DE PAYLOAD (SignalR) — AUCUN GET sur push (garde-fou anti-flake). L'écriture (marquer-lu /
/// accepter / refuser) transite par le canal requête/réponse, jamais la diffusion. Échap ferme le mini-dialog
/// puis le panneau (port <see cref="IEcouteurEchapModal"/> s33).
/// </summary>
public partial class Cloche : IAsyncDisposable
{
    private List<NotificationCloche> _notifications = new();
    private List<ActeurFoyer> _acteurs = new();
    private int _nonLus;
    private bool _ouvert;
    private HubConnection? _hub;
    private IAsyncDisposable? _abonnementEchap;
    private bool _charge;                 // flux + acteurs chargés (une seule fois par session ouverte)
    private bool _hubDemarrageDemande;    // le hub ne se démarre qu'une fois par session ouverte

    // Mini-dialog de confirmation d'une réponse à une proposition actionnable (Sc.8). null = fermé.
    private string? _confirmPropositionId;
    private bool _confirmAccepter;

    [Inject] private HttpClient Canal { get; set; } = default!;
    [Inject] private SessionPlanning Session { get; set; } = default!;
    [Inject] private OptionsConnexionHub OptionsHub { get; set; } = default!;

    // Port Échap (s33) résolu PARESSEUSEMENT via le provider (jamais un [Inject] dur) : la cloche est rendue
    // en permanence dans la barre d'application, on n'impose donc pas sa présence à tout hôte qui rend le layout
    // (l'app réelle l'enregistre — Program.cs ; un contexte de test qui n'exerce pas Échap peut l'omettre sans
    // casser le rendu). L'écoute document ne s'attache qu'à l'ouverture du panneau.
    [Inject] private IServiceProvider Fournisseur { get; set; } = default!;
    private IEcouteurEchapModal? Echap => Fournisseur.GetService(typeof(IEcouteurEchapModal)) as IEcouteurEchapModal;

    /// <summary>Gating d'affichage : même règle que le menu utilisateur (rien hors session) COMPOSÉE au
    /// Parent-gating (un Invité / une identité effective non Parent ne voit pas la cloche). Rendue dans le layout
    /// (présent sur toutes les routes, dont /connexion), elle reste donc invisible tant qu'aucun Parent n'est
    /// connecté.</summary>
    private bool DoitAfficher => Session.EstConnecte && Session.EstParent;

    private string MonId => Session.IdentiteEffective.Id;

    protected override void OnInitialized()
        // Le layout persiste à travers la navigation (login → planning) : la cloche s'abonne à la session pour
        // (re)charger quand la connexion est déclenchée depuis un AUTRE composant (la page de connexion dédiée).
        => Session.EtatConnexionChange += SurChangementConnexion;

    protected override async Task OnInitializedAsync()
    {
        if (DoitAfficher)
            await ChargerAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // Démarre le hub dès que la cloche doit s'afficher et qu'il n'est pas déjà (re)demandé — que la session
        // soit déjà ouverte au premier rendu, ou ouverte plus tard pendant la vie du layout (post-login).
        if (DoitAfficher && !_hubDemarrageDemande)
        {
            _hubDemarrageDemande = true;
            await DemarrerHubAsync();
        }
    }

    private async Task DemarrerHubAsync()
    {
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

    /// <summary>Réagit à un changement d'état de connexion (Sc.11 pattern menu utilisateur). À l'ouverture d'une
    /// session Parent : charge le flux + référentiel puis re-rend (le rendu déclenche le démarrage du hub). À la
    /// déconnexion : réinitialise (aucune notif fantôme, hub fermé) pour repartir propre à la prochaine session.</summary>
    private async void SurChangementConnexion()
    {
        if (DoitAfficher && !_charge)
        {
            await ChargerAsync();
            await InvokeAsync(StateHasChanged);
        }
        else if (!Session.EstConnecte)
        {
            await ReinitialiserAsync();
            await InvokeAsync(StateHasChanged);
        }
    }

    /// <summary>Chargement INITIAL (GET, autorisé — jamais sur push) : le référentiel d'acteurs (résolution
    /// ids→noms, parité rendu initial / reprojeté) puis le flux (compteur + notifications chrono). Le composant
    /// étant AUTONOME dans le layout, il charge lui-même ces deux sources (la page ne les lui passe plus).</summary>
    private async Task ChargerAsync()
    {
        _charge = true; // tentative marquée d'emblée : évite un double chargement concurrent
        try
        {
            var acteurs = await Canal.GetFromJsonAsync<List<ActeurFoyer>>("api/foyer/acteurs");
            if (acteurs is not null)
                _acteurs = acteurs;
        }
        catch (HttpRequestException)
        {
            // Référentiel injoignable : les libellés retombent sur l'id brut (Nom), la cloche reste fonctionnelle.
        }

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

    /// <summary>Réinitialise l'état à la déconnexion : aucune notification résiduelle d'une session précédente,
    /// panneau/mini-dialog fermés, écoute Échap détachée, hub fermé — la prochaine connexion repart de zéro.</summary>
    private async Task ReinitialiserAsync()
    {
        _notifications = new();
        _acteurs = new();
        _nonLus = 0;
        _ouvert = false;
        _confirmPropositionId = null;
        _charge = false;
        _hubDemarrageDemande = false;
        await DetacherEchap();
        if (_hub is not null)
        {
            await _hub.DisposeAsync();
            _hub = null;
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
            // Statut porte le SOUS-TYPE d'imprévu (s48 : « malade » / « retard ») pour le libellé informatif,
            // sinon « changement » — parité EXACTE avec le rendu du GET initial (canal de lecture).
            e.Horodatage, false, false, null, e.Imprevu?.ToString().ToLowerInvariant() ?? "changement"));
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

    private string Nom(string id) => _acteurs.FirstOrDefault(a => a.Id == id)?.Nom ?? id;

    /// <summary>Libellé humain d'une notification (résolution des ids en noms sur le référentiel chargé).</summary>
    private string Libelle(NotificationCloche n) => n.Type switch
    {
        "delegation" => $"Délégation du {n.Jour:dd/MM} : {Nom(n.CedantId)} → {Nom(n.RecevantId)}",
        "reprise" => $"Reprise du {n.Jour:dd/MM} : {Nom(n.CedantId)}",
        "transfert" => $"Transfert du {n.Jour:dd/MM} : {Nom(n.CedantId)} → {Nom(n.RecevantId)}",
        // Imprévu (s48) : informatif, non négociable. « malade » porte sur l'ENFANT ; « retard » sur le SIGNALANT
        // (RecevantId = acteur signalant). Le sous-type vit dans Statut (parité GET initial / reprojection).
        "imprevu" when n.Statut == "retard" => $"{Nom(n.RecevantId)} sera en retard le {n.Jour:dd/MM}",
        "imprevu" => $"{Nom(n.EnfantId)} est malade le {n.Jour:dd/MM}",
        "echange" when n.Statut == "acceptee" => $"Échange du {n.Jour:dd/MM} accepté : {Nom(n.CedantId)} → {Nom(n.RecevantId)}",
        _ => $"Échange proposé du {n.Jour:dd/MM} : {Nom(n.CedantId)} → {Nom(n.RecevantId)}",
    };

    public async ValueTask DisposeAsync()
    {
        Session.EtatConnexionChange -= SurChangementConnexion;
        await DetacherEchap();
        if (_hub is not null)
            await _hub.DisposeAsync();
    }
}
