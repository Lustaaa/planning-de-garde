using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Web.State;

namespace PlanningDeGarde.Web.Components.Pages;

/// <summary>
/// Vue centrale du planning partagé (front <b>WASM</b>), rendue en grille agenda 5×7 en LECTURE
/// SEULE. La grille est lue via le <b>canal de lecture de l'API distante</b> (HTTP
/// <c>GET /api/grille/…</c>) — le navigateur n'a pas la projection en DI directe. Le rafraîchissement
/// temps réel passe par le <b>hub SignalR de l'API distante</b>, consommé côté navigateur ; une
/// écriture aboutie le déclenche, jamais l'inverse. Aucune règle métier ici.
/// </summary>
public partial class PlanningPartage
{
    private static readonly string[] JoursDeLaSemaine =
        { "Lundi", "Mardi", "Mercredi", "Jeudi", "Vendredi", "Samedi", "Dimanche" };

    private GrilleAgenda _grille = new(
        Array.Empty<JourCase>(), Array.Empty<SemaineLigne>(), Array.Empty<EntreeLegende>(), Array.Empty<EntreeLegendeMotif>());

    // Acteurs DÉCLARÉS du foyer (énumérés depuis le store via api/foyer/acteurs) : source UNIQUE des
    // sélecteurs de responsable des dialogs d'écriture, passée en paramètre (les dialogs ne s'auto-chargent
    // plus — évite un re-render async pendant la saisie). Rafraîchie à l'init, à chaque ouverture de dialog
    // (fetch-on-open : un acteur ajouté apparaît, Sc.5) et à chaque diffusion temps réel (propagation au
    // second écran, Sc.7). _acteursFoyerCharges distingue « en chargement » de « chargé et vide » (invite
    // « aucun acteur », Sc.6).
    private List<ActeurFoyer> _acteursFoyer = new();
    private bool _acteursFoyerCharges;

    // Activités du référentiel du foyer (énumérées depuis le store vivant via api/foyer/activites, s35) : source UNIQUE
    // des sélecteurs de lieu des dialogs Poser un slot / Définir un transfert, passée en paramètre (les
    // dialogs ne lisent plus la liste en dur Foyer.Lieux). Rafraîchie à l'init, à l'ouverture de dialog et à
    // chaque diffusion temps réel — un lieu ajouté / supprimé en config suit sans rechargement (S6).
    private List<ActiviteFoyer> _lieuxFoyer = new();

    // Enfants du référentiel du foyer (énumérés depuis le store vivant via api/foyer/enfants) : source UNIQUE
    // du sélecteur d'enfant de la dialog « Poser un slot », passée en paramètre (la dialog ne lit plus le
    // fantôme Session.EnfantId, s29). Rafraîchie à l'init et à chaque diffusion temps réel — un enfant ajouté /
    // édité en config suit sans rechargement (S9/S10).
    private List<EnfantFoyer> _enfantsFoyer = new();

    // Enfant sélectionné dont on délègue la récupération d'un jour (s44) : passé en EnfantId au mini-dialog
    // « déléguer ce jour ». Défaut = 1er enfant du référentiel ; la sélection n'écrit rien par elle-même.
    // null tant qu'aucun enfant n'est chargé.
    private string? _enfantSelectionne;

    private HubConnection? _hub;

    // Écriture en contexte (palier 7) — la grille reste en LECTURE SEULE (règle 14) : la case ouvre un
    // menu d'actions, jamais une écriture. Un seul déclencheur par case (mutualise le gating Invité, Sc.6).
    // null = fermé. Date de la case dont le menu d'actions est ouvert :
    private DateOnly? _dateMenu;
    // Date de contexte de chaque dialog ouverte depuis le menu (null = dialog fermée) :
    private DateOnly? _dateDialogPoserSlot;
    private DateOnly? _dateDialogAffecterPeriode;
    private DateOnly? _dateDialogDefinirTransfert;
    private DateOnly? _dateDialogSupprimerPeriode;
    private DateOnly? _dateDialogEditerPeriode;
    private DateOnly? _dateDialogSupprimerSlot;
    // Délégation de la récupération d'UN jour (s44) : la carte « Aujourd'hui » (s42) et le panneau à-venir
    // (s43) HÉBERGENT une action « déléguer ce jour » ouvrant un mini-dialog de choix du délégataire. Jour de
    // contexte de la dialog (null = fermée). La grille reste LECTURE SEULE : la dialog porte l'écriture.
    private DateOnly? _dateDialogDeleguer;
    // Reprise d'UN jour délégué (s46, ferme la boucle undo s44/s45) : l'entrée conditionnelle « reprendre ce
    // jour » du menu clic-case ouvre un mini-dialog de confirmation. Jour de contexte (null = fermé). La grille
    // reste LECTURE SEULE : la dialog porte l'écriture (canal requête/réponse, composition suppression s16).
    private DateOnly? _dateDialogReprendre;
    // Proposition d'échange d'UN jour (s47) : l'entrée « proposer un échange » du menu clic-case ouvre un
    // mini-dialog de choix du recevant. Jour de contexte (null = fermé). PROPOSER n'écrit rien : la case reste
    // inchangée ; le recevant est notifié via sa cloche et répond depuis la notification actionnable.
    private DateOnly? _dateDialogProposer;
    // Signalement d'imprévu d'UN jour (s48) : l'entrée « signaler un imprévu » du menu clic-case ouvre un
    // mini-dialog de choix du type (malade/retard) + motif optionnel. Jour de contexte (null = fermé). Purement
    // INFORMATIF : le signalement n'écrit AUCUNE surcharge (résolution inchangée) ; la cloche des concernés reprojette.
    private DateOnly? _dateDialogSignalerImprevu;
    // Sélection de plage de cases contiguës (Sc.5) : un mode de sélection (gardé EstParent, mutualise le
    // gating Invité avec le menu — Sc.7) où l'on clique la case de début puis la case de fin pour émettre
    // UNE période sur l'intervalle. État de PRÉSENTATION uniquement (la grille reste lecture seule) :
    // _modePlage = mode actif ; _plageDebut / _plageFin = bornes [min, max] de l'intervalle sélectionné.
    private bool _modePlage;
    private DateOnly? _plageDebut;
    private DateOnly? _plageFin;
    // Sélection de plage par DRAG (s49, tranche 2 palier 9) — affordance tranchée par le scrum-master :
    // la GRILLE est la seule surface. État d'interaction VOLATILE (jamais persisté, borne anti-cliquet) :
    // mousedown pose l'ANCRE, le survol met à jour le CURSEUR, mouseup ouvre la dialog EXISTANTE « Affecter
    // une période » (s06) pré-remplie sur l'intervalle normalisé [min, max]. Effacé à fin de geste / Échap /
    // changement de vue / rechargement. Aucun store, aucune persistance, aucune surface de lecture neuve.
    private DateOnly? _ancreDrag;
    private DateOnly? _curseurDrag;
    // Avertissement de chevauchement « à part » (Sc.7, règle 16) : pose acceptée mais signalée, bandeau
    // NON bloquant et refermable. Drapeau porté par l'outcome de la commande (jamais recalculé ici).
    private bool _avertissementChevauchement;
    // Accusé « Transfert défini » à part (Sc.1) : feedback transitoire NON bloquant levé sur le simple
    // succès HTTP du canal (aucun read model neuf, aucun rendu en case — règle 27). Refermable.
    private bool _accuseTransfertDefini;
    // Accusé « Période supprimée » à part (Sc.6) : feedback transitoire NON bloquant levé sur le succès du
    // canal d'écriture de suppression. La re-résolution de la case vient de la relecture de la grille
    // distante (repli surcharge > fond > neutre), jamais d'une mutation locale. Refermable.
    private bool _accusePeriodeSupprimee;
    // Accusé « Période modifiée » à part (Sc.7) : feedback transitoire NON bloquant levé sur le succès du
    // canal d'écriture d'édition. La case re-résolue (nouveau responsable, ou repli surcharge > fond > neutre
    // pour la portion libérée) provient de la relecture de la grille distante, jamais d'une mutation locale.
    private bool _accusePeriodeModifiee;
    // Accusé « Slot supprimé » à part (s18 Sc.6) : feedback transitoire NON bloquant levé sur le succès du
    // canal d'écriture de suppression de slot. La case relue ne rend plus le slot retiré (les autres slots
    // demeurent) ; le retrait provient de la relecture de la grille distante, jamais d'une mutation locale.
    private bool _accuseSlotSupprime;
    // Échec de navigation (Sc.6) : la re-requête de la date naviguée a échoué (API distante injoignable).
    // Bandeau d'échec clair, NON bloquant et refermable. La fenêtre affichée est conservée (l'ancre est
    // restaurée), et la navigation échouée n'est NI mise en file NI rejouée (règle 28). Levé à part.
    private bool _echecNavigation;

    // Bandeau de connexion inline RETIRÉ (s24, Sc.10) : la connexion/déconnexion ne se fait plus depuis le
    // planning — un seul chemin d'entrée = la page de connexion dédiée (/connexion, Sc.8). Le
    // pré-positionnement de l'acteur du compte connecté (incarnation bornée s14) est porté par la session.

    private RoleAuteur RoleSelectionne
    {
        get => Session.Role;
        set { Session.Role = value; }
    }

    /// <summary>Identifiant stable de l'acteur incarné via le sélecteur d'incarnation (impersonation
    /// bornée, sprint 14). Vide = identité réelle. La sélection lit le <b>référentiel réel</b> chargé
    /// dans la session (id + type surfacé read-only) : Parent/Admin incarné garde le menu d'écriture,
    /// Autre le masque (règle 8) ; la valeur vide revient à l'identité réelle (Sc.2). Aucune écriture,
    /// aucune persistance : l'état d'incarnation reste en session (borne anti-cliquet règle 30).</summary>
    private string IncarnationSelectionnee
    {
        get => Session.IncarnationActive ? Session.IdentiteEffective.Id : "";
        set
        {
            if (string.IsNullOrEmpty(value))
                Session.RevenirIdentiteReelle();
            else
                Session.Incarner(value);
        }
    }

    // Route protégée (s25, Sc.1) : sans session ouverte, la route redirige vers la page de connexion et
    // NE rend AUCUN contenu (pas de flash de grille). Vrai tant que la session n'est pas connectée.
    private bool _redirigeVersConnexion;

    /// <summary>Résolution OPTIONNELLE du port d'écoute Échap document (s33) : présent (app réelle / test qui
    /// double le port) il capte Échap au niveau document pour ANNULER une sélection de plage (Sc.7) ; absent
    /// (tests de lecture pure qui ne l'enregistrent pas) la grille reste fonctionnelle sans écoute Échap.</summary>
    [Inject] private IServiceProvider Services { get; set; } = default!;

    // Abonnement à l'écouteur Échap document (détaché à la fermeture de la page — aucune fuite).
    private IAsyncDisposable? _abonnementEchap;

    protected override async Task OnInitializedAsync()
    {
        // Garde d'accès (s25, Sc.1) : aucune session ouverte → redirection vers /connexion, aucun contenu
        // rendu. La page de connexion dédiée reste librement accessible (elle n'est pas protégée).
        if (!Session.EstConnecte)
        {
            _redirigeVersConnexion = true;
            Nav.NavigateTo("connexion");
            return;
        }

        // L'ancre de navigation démarre sur la semaine en cours (lundi de la date d'aujourd'hui), via
        // le port d'horloge injecté. Idempotent : une ancre déjà décalée par la navigation est conservée.
        Session.InitialiserAncre(Horloge.Aujourdhui);
        // Acteurs déclarés AVANT la grille : ainsi, dès que la grille (28 cases) est rendue, la liste des
        // acteurs est déjà chargée et STABLE — les dialogs ouverts ensuite reçoivent leur sélecteur peuplé
        // d'emblée, sans re-render async pendant la saisie (robustesse runtime + parité tests).
        await ChargerActeursIncarnablesAsync();
        await ChargerLieuxAsync();
        await ChargerEnfantsAsync();
        await ChargerAsync();
    }

    /// <summary>Charge les enfants du référentiel du foyer depuis le store vivant via le canal de lecture HTTP
    /// (<c>GET /api/foyer/enfants</c>) : alimente le sélecteur d'enfant de la dialog de pose (jamais le fantôme
    /// Session.EnfantId). Lecture seule ; sur référentiel distant injoignable, la liste reste inchangée.</summary>
    private async Task ChargerEnfantsAsync()
    {
        try
        {
            var enfants = await Canal.GetFromJsonAsync<List<EnfantFoyer>>("api/foyer/enfants");
            if (enfants is not null)
            {
                _enfantsFoyer = enfants;
                // Défaut = 1er enfant ; on conserve une sélection encore présente (l'enfant courant survit à
                // une diffusion), sinon on retombe sur le premier disponible (jamais une sélection fantôme).
                if (_enfantSelectionne is null || _enfantsFoyer.All(e => e.Id != _enfantSelectionne))
                    _enfantSelectionne = _enfantsFoyer.FirstOrDefault()?.Id;
            }
        }
        catch (HttpRequestException)
        {
            // Référentiel distant injoignable : le sélecteur d'enfant conserve son dernier état plutôt que
            // de planter la vue (le planning en lecture reste consultable).
        }
    }

    /// <summary>Charge les activités du référentiel du foyer depuis le store vivant via le canal de lecture HTTP
    /// (<c>GET /api/foyer/activites</c>, s35 — ex-« lieux ») : alimente le sélecteur de lieu (axe LOCALISATION du
    /// slot, préservé) des dialogs. Lecture seule ; sur référentiel distant injoignable, la liste reste inchangée.</summary>
    private async Task ChargerLieuxAsync()
    {
        try
        {
            var lieux = await Canal.GetFromJsonAsync<List<ActiviteFoyer>>("api/foyer/activites");
            if (lieux is not null)
                _lieuxFoyer = lieux;
        }
        catch (HttpRequestException)
        {
            // Référentiel distant injoignable : le sélecteur de lieu conserve son dernier état plutôt que
            // de planter la vue (le planning en lecture reste consultable).
        }
    }

    /// <summary>Charge le catalogue des acteurs incarnables depuis le <b>référentiel réel</b> via le
    /// canal de lecture HTTP (<c>GET /api/foyer/acteurs</c>, type surfacé read-only) et le dépose dans la
    /// session. Alimente le sélecteur d'incarnation ; aucune règle métier ici (lecture seule).</summary>
    private async Task ChargerActeursIncarnablesAsync()
    {
        try
        {
            var acteurs = await Canal.GetFromJsonAsync<List<ActeurFoyer>>("api/foyer/acteurs");
            if (acteurs is not null)
            {
                // Source unique : alimente À LA FOIS le sélecteur d'incarnation (id+nom+type) et les
                // sélecteurs de responsable des dialogs (liste complète, id stable — jamais le libellé).
                _acteursFoyer = acteurs;
                Session.ActeursIncarnables = acteurs
                    .Select(a => new IdentiteActeur(a.Id, a.Nom, a.Type))
                    .ToList();
            }
        }
        catch (HttpRequestException)
        {
            // Référentiel distant injoignable : les sélecteurs (incarnation + dialogs) restent vides plutôt
            // que de planter la vue (le planning en lecture reste consultable sous l'identité réelle).
        }
        _acteursFoyerCharges = true; // tentative faite : les dialogs peuvent décider d'afficher l'invite (Sc.6)
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender || _redirigeVersConnexion)
            return;

        try
        {
            // Hub SignalR de l'API DISTANTE (même hôte que le canal d'écriture/lecture : Canal.BaseAddress).
            // OptionsHub.Configurer est neutre en WASM réel (WebSocket navigateur) ; un hôte de test le
            // surcharge pour rediriger la connexion vers son TestServer en mémoire (acceptation runtime Sc.4).
            var urlHub = new Uri(Canal.BaseAddress!, "hubs/planning");
            _hub = new HubConnectionBuilder()
                .WithUrl(urlHub, OptionsHub.Configurer)
                .WithAutomaticReconnect()
                .Build();

            _hub.On(PlanningHubEvenement.MiseAJour, async () =>
            {
                await ChargerAsync();
                // Une mise à jour diffusée peut être une SUPPRESSION concurrente de l'acteur incarné (Sc.5,
                // D2) : on rafraîchit le catalogue d'incarnables depuis le référentiel réel, puis on replie
                // automatiquement sur l'identité réelle si l'acteur incarné n'y figure plus (sans nom fantôme).
                await ChargerActeursIncarnablesAsync();
                // Le référentiel de lieux peut avoir changé en config (ajout / suppression d'un lieu) : on le
                // ré-énumère depuis le store vivant, si bien que le sélecteur de lieu des dialogs suit sans
                // rechargement (temps réel SignalR lecture, S6).
                await ChargerLieuxAsync();
                // Le référentiel d'enfants peut avoir changé en config (ajout / édition) : on le ré-énumère
                // depuis le store vivant, si bien que le sélecteur d'enfant de la dialog suit sans rechargement
                // (temps réel SignalR lecture, S9/S10).
                await ChargerEnfantsAsync();
                Session.ReplierSiActeurIncarneAbsent();
                await InvokeAsync(StateHasChanged);
            });

            await _hub.StartAsync();
        }
        catch
        {
            // Le temps réel est un confort : si le hub est indisponible, la vue reste
            // fonctionnelle (rechargement à la navigation).
        }
    }

    /// <summary>Attache PARESSEUSEMENT l'écouteur Échap document (port s33) au PREMIER armement d'une sélection
    /// de plage (Sc.7) — pas en permanence : ainsi une page qui n'entame aucune sélection n'attache rien (les
    /// autres modals, ex. mini-dialogs, gardent la maîtrise exclusive d'Échap). Résolution OPTIONNELLE du port
    /// (tests de lecture pure / sans port restent fonctionnels). L'abonnement est conservé pour la durée de vie
    /// de la page et détaché à sa fermeture (DisposeAsync) — aucune fuite, aucun double abonnement.</summary>
    private async Task AssurerEcouteEchapPlageAsync()
    {
        if (_abonnementEchap is not null)
            return;

        var ecouteur = Services.GetService<IEcouteurEchapModal>();
        if (ecouteur is not null)
            _abonnementEchap = await ecouteur.EcouterAsync(AnnulerSelectionPlage);
    }

    // Date de référence = l'ANCRE DE NAVIGATION (en session/mémoire), décalée par les contrôles
    // préc./suiv. (Sc.1) — initialisée sur la semaine en cours via le port d'horloge. Le canal de
    // lecture distant prend cette ancre en segments yyyy/MM/dd, plus le paramètre de VUE (span). La
    // navigation ne fait que re-projeter à la date naviguée : lecture seule, aucune écriture.
    private async Task<bool> ChargerAsync()
    {
        var ancre = Session.Ancre;
        try
        {
            var grille = await Canal.GetFromJsonAsync<GrilleAgenda>(
                $"api/grille/{ancre.Year}/{ancre.Month}/{ancre.Day}?vue={CodeVue(Session.Vue)}");
            if (grille is not null)
                _grille = grille;
            return true;
        }
        catch (HttpRequestException)
        {
            // API distante injoignable : à l'ouverture, la grille reste vide plutôt que de planter la vue
            // (vue consultable). L'appelant (navigation) décide quoi faire de l'échec — voir NaviguerAsync.
            return false;
        }
    }

    /// <summary>Code de la vue prédéfinie passé en paramètre de lecture (CQRS) : <c>semaine</c> /
    /// <c>4semaines</c> (défaut) / <c>mois</c>. Le défaut couvre la compatibilité ascendante de
    /// l'endpoint (sans vue → 4 semaines glissantes, Sc.3).</summary>
    private static string CodeVue(VuePlanning vue) => vue switch
    {
        VuePlanning.Semaine => "semaine",
        VuePlanning.Mois => "mois",
        _ => "4semaines",
    };

    /// <summary>Vue prédéfinie correspondant au code du sélecteur (inverse de <see cref="CodeVue"/>) :
    /// <c>semaine</c> / <c>mois</c> / défaut <c>4 semaines glissantes</c> (compatibilité ascendante).</summary>
    private static VuePlanning VueDepuisCode(string? code) => code switch
    {
        "semaine" => VuePlanning.Semaine,
        "mois" => VuePlanning.Mois,
        _ => VuePlanning.QuatreSemaines,
    };

    /// <summary>« Changer de vue » (Sc.2/Sc.3, sélecteur de vue) : fixe la vue choisie puis re-projette en
    /// re-requêtant l'API distante avec le paramètre de vue. L'ancre lundi est conservée (seul le span
    /// change). Sur échec de la re-requête, la vue est <b>restaurée</b> (la fenêtre affichée ne diverge
    /// pas de l'état) et le bandeau d'échec levé — même pivot que la navigation (Sc.6). Aucune écriture.</summary>
    private async Task ChangerVueAsync(ChangeEventArgs e)
    {
        var vueAvant = Session.Vue;
        Session.Vue = VueDepuisCode(e.Value?.ToString());
        await ReprojeterAsync(() => Session.Vue = vueAvant);
    }

    /// <summary>« Semaine suivante » (Sc.1) : décale l'ancre de +7 jours puis re-projette en
    /// re-requêtant l'API distante à la date naviguée. Aucune écriture (lecture seule).</summary>
    private Task DemanderSemaineSuivante() => NaviguerAsync(Session.SemaineSuivante);

    /// <summary>« Semaine précédente » (Sc.1) : décale l'ancre de −7 jours puis re-projette.</summary>
    private Task DemanderSemainePrecedente() => NaviguerAsync(Session.SemainePrecedente);

    /// <summary>« Aujourd'hui » (Sc.4) : réinitialise l'ancre à la semaine en cours (lundi de la date du
    /// jour, via le port d'horloge injecté), quel que soit le décalage de navigation accumulé, puis
    /// re-projette en re-requêtant l'API distante à l'ancre réinitialisée. Aucune écriture (lecture seule).</summary>
    private Task DemanderRetourAujourdhui() => NaviguerAsync(() => Session.RevenirAujourdhui(Horloge.Aujourdhui));

    /// <summary>
    /// Pivot commun de navigation (Sc.1/Sc.4/Sc.6) : décale l'ancre via <paramref name="decalerAncre"/>
    /// puis re-projette en re-requêtant l'API distante à la date naviguée. <b>Gestion d'échec (Sc.6)</b> :
    /// si la re-requête échoue (API distante injoignable), l'ancre est <b>restaurée</b> à celle de la
    /// fenêtre affichée — l'affichage et l'état de navigation ne divergent pas — et un <b>bandeau d'échec
    /// clair</b> est levé. La navigation échouée n'est <b>ni mise en file ni rejouée</b> (règle 28). Un
    /// succès efface tout échec antérieur. Aucune écriture : la navigation ne fait que re-projeter.
    /// </summary>
    private async Task NaviguerAsync(Action decalerAncre)
    {
        var ancreAvant = Session.Ancre;
        decalerAncre();
        await ReprojeterAsync(() => Session.RestaurerAncre(ancreAvant)); // fenêtre conservée, aucun rejeu
    }

    /// <summary>
    /// Pivot de re-projection partagé entre la navigation (décalage d'ancre, Sc.1/Sc.4) et le changement
    /// de vue (Sc.2/Sc.3) : re-requête l'API distante à l'état courant (ancre + vue). Sur <b>échec</b> de
    /// la re-requête (API distante injoignable), exécute <paramref name="restaurerSiEchec"/> pour ramener
    /// l'état (ancre ou vue) à celui de la fenêtre affichée — affichage et état ne divergent pas — et lève
    /// le bandeau d'échec clair (Sc.6) ; l'opération échouée n'est <b>ni mise en file ni rejouée</b>
    /// (règle 28). Un succès efface tout échec antérieur. Aucune écriture : pure re-projection en lecture.
    /// </summary>
    private async Task ReprojeterAsync(Action restaurerSiEchec)
    {
        if (await ChargerAsync())
        {
            _echecNavigation = false;
        }
        else
        {
            restaurerSiEchec();
            _echecNavigation = true;
        }
    }

    /// <summary>Referme le bandeau d'échec de navigation (Sc.6, non bloquant).</summary>
    private void FermerEchecNavigation() => _echecNavigation = false;

    /// <summary>Revient à l'identité réelle (bouton du bandeau d'incarnation, Sc.2) : l'incarnation est
    /// levée, la vue restaurée à l'identité réelle de l'utilisateur principal. Aucune écriture.</summary>
    private void RevenirIdentiteReelle()
    {
        Session.RevenirIdentiteReelle();
        _dateMenu = null; // un menu ouvert sous l'incarnation ne survit pas au retour
    }

    /// <summary>
    /// Ouvre le <b>menu d'actions</b> de la case cliquée (décision CP, palier 7) : un seul déclencheur
    /// d'écriture par case, deux entrées (poser un slot / affecter une période). Gating Invité (règle 9)
    /// mutualisé ici : en consultation seule, le clic n'ouvre rien — le déclencheur est gardé à l'entrée.
    /// Aucune écriture : le menu et les dialogs portent la commande, la grille reste en lecture seule.
    /// </summary>
    private void OuvrirMenu(DateOnly date)
    {
        if (!Session.EstParent)
            return;

        // En mode plage (Sc.5), le clic-case ne déclenche PAS le menu single-jour : il alimente la
        // sélection d'intervalle. Hors mode plage, comportement palier 7 inchangé (menu d'actions).
        if (_modePlage)
        {
            SelectionnerCasePlage(date);
            return;
        }

        _dateMenu = date;
    }

    /// <summary>
    /// Bascule le <b>mode sélection de plage</b> (Sc.5). Gardé <see cref="SessionPlanning.EstParent"/> :
    /// le déclencheur de plage est réservé Parent/Admin (règle 9), mutualisant le gating Invité du menu
    /// clic-case — c'est ce gate partagé que Sc.7 caractérise (en consultation, le bouton n'est même pas
    /// rendu). Toute (dé)activation repart d'une sélection vierge. État de présentation, aucune écriture.
    /// </summary>
    private void BasculerModePlage()
    {
        if (!Session.EstParent)
            return;

        _modePlage = !_modePlage;
        _plageDebut = null;
        _plageFin = null;
        _dateMenu = null;
    }

    /// <summary>
    /// Alimente la sélection de plage (Sc.5) : le 1ᵉʳ clic-case fixe le début ; le 2ᵉ borne l'intervalle
    /// <c>[min, max]</c> des deux dates contiguës puis ouvre l'affectation <b>pré-remplie sur l'intervalle</b>
    /// (une seule commande <c>AffecterPeriode</c> couvrant la plage — backend inchangé). Re-cliquer la
    /// même case avant la 2ᵉ borne ne fait rien (intervalle dégénéré ignoré, variantes riches → tranche 2).
    /// </summary>
    private void SelectionnerCasePlage(DateOnly date)
    {
        if (_plageDebut is null)
        {
            _plageDebut = date;
            return;
        }

        if (date == _plageDebut)
            return; // 2ᵉ clic sur la même case : intervalle dégénéré, on attend une case distincte

        var debut = _plageDebut.Value;
        _plageDebut = date < debut ? date : debut;
        _plageFin = date < debut ? debut : date;
        _modePlage = false; // l'intervalle est complet : on sort du mode et on ouvre l'affectation
        _dateDialogAffecterPeriode = _plageDebut; // borne de début ; la fin passe par _plageFin
    }

    /// <summary>Vrai si la case <paramref name="date"/> appartient à la sélection de plage en cours (borne
    /// de début déjà posée, ou intervalle complet) — sert au surlignage visuel des cases sélectionnées.</summary>
    private bool EstDansSelectionPlage(DateOnly date)
    {
        if (_plageFin is { } fin && _plageDebut is { } debutComplet)
            return date >= debutComplet && date <= fin;
        return _plageDebut == date;
    }

    /// <summary>
    /// mousedown sur une case : pose l'<b>ANCRE</b> de la sélection de plage par drag (s49) et arme l'état
    /// volatile. Parent-gated à la SOURCE (règle 9, mutualisé avec le menu clic-case) : en consultation seule,
    /// aucun état n'est armé → geste inerte (Sc.8). Neutralisé pendant le mode-plage bouton (tranche 1) pour
    /// ne pas interférer avec son flux clic-début / clic-fin. Aucune écriture (état de présentation).
    /// </summary>
    private async Task DebutSelectionPlage(DateOnly date)
    {
        if (!Session.EstParent || _modePlage)
            return;

        _ancreDrag = date;
        _curseurDrag = date;
        // Arme l'écoute Échap (annulation du geste, Sc.7) au premier drag seulement — attache paresseuse.
        await AssurerEcouteEchapPlageAsync();
    }

    /// <summary>
    /// Survol d'une case pendant le geste : met à jour le <b>CURSEUR</b> (la surbrillance [ancre..curseur] est
    /// recalculée). Sans ancre armée, le survol est ignoré (aucune sélection hors geste). Le curseur est
    /// naturellement <b>borné à la vue</b> : seules les cases rendues (fenêtre chargée) émettent le survol, un
    /// débordement au-delà du bord ne sélectionne donc aucune case hors-vue et ne navigue pas (Sc.6).
    /// </summary>
    private void SurvolerCasePlage(DateOnly date)
    {
        if (_ancreDrag is null)
            return;

        _curseurDrag = date;
    }

    /// <summary>
    /// mouseup : fin du geste. Distingue <b>clic vs drag par les CASES</b> (jamais les pixels) : si le curseur
    /// est resté sur l'ancre → CLIC SIMPLE → menu clic-case existant, INCHANGÉ (Sc.4) ; sinon → PLAGE → ouvre
    /// la dialog « Affecter une période » EXISTANTE (s06) pré-remplie sur l'intervalle <b>NORMALISÉ</b>
    /// <c>[min, max]</c> (début ≤ fin garanti, jamais vide/inversée — Sc.3/Sc.5). La surbrillance disparaît
    /// (état d'ancre/curseur vidé). Aucune écriture ici : la dialog porte la commande (réemploi strict s06).
    /// </summary>
    private void FinSelectionPlage()
    {
        if (_ancreDrag is not { } ancre)
            return;

        var curseur = _curseurDrag ?? ancre;
        var debut = ancre < curseur ? ancre : curseur;
        var fin = ancre < curseur ? curseur : ancre;
        _ancreDrag = null;
        _curseurDrag = null;

        if (debut == fin)
        {
            OuvrirMenu(debut); // clic simple (curseur resté sur l'ancre) : comportement inchangé (Sc.4)
            return;
        }

        _plageDebut = debut;
        _plageFin = fin;
        _dateDialogAffecterPeriode = debut; // dialog EXISTANTE pré-remplie sur [début, fin] (borne fin = _plageFin)
    }

    /// <summary>Vrai si la case appartient à la <b>surbrillance de plage EN COURS</b> (drag armé) — recalculée
    /// à chaque survol sur l'intervalle <c>[min(ancre,curseur), max(ancre,curseur)]</c>. Pure présentation
    /// cliente (aucune persistance) : disparaît à fin de geste / Échap / changement de vue.</summary>
    private bool EstDansPlageDrag(DateOnly date)
    {
        if (_ancreDrag is not { } ancre || _curseurDrag is not { } curseur)
            return false;

        var lo = ancre < curseur ? ancre : curseur;
        var hi = ancre < curseur ? curseur : ancre;
        return date >= lo && date <= hi;
    }

    /// <summary>
    /// Échap (port document s33) ANNULE la sélection de plage (Sc.7) : pendant le drag OU sur la plage
    /// relâchée (dialog pré-remplie ouverte AVANT validation), vide l'état d'ancre/curseur et de bornes, retire
    /// la surbrillance et FERME la dialog de plage — AUCUNE écriture (store intact). N'agit QUE sur la
    /// sélection de plage : sans sélection en cours, Échap est laissé aux autres surfaces (garde de portée).
    /// </summary>
    private async Task AnnulerSelectionPlage()
    {
        if (_ancreDrag is null && _plageFin is null)
            return;

        _ancreDrag = null;
        _curseurDrag = null;
        _plageDebut = null;
        _plageFin = null;
        _dateDialogAffecterPeriode = null; // ferme la dialog de plage relâchée avant validation
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>Ferme le menu d'actions sans rien ouvrir (clic hors panneau).</summary>
    private void FermerMenu() => _dateMenu = null;

    /// <summary>
    /// Ouvre le mini-dialog « déléguer ce jour » (s44) sur la <paramref name="date"/> de la case, depuis
    /// l'entrée « déléguer ce jour » du <b>menu clic-case</b> (surface tranchée par le PO au gate G3 :
    /// SEULE surface d'écriture de la délégation ; les cartes de lecture s42/s43 n'en portent plus).
    /// Gating Invité (règle 9) mutualisé avec le menu (il ne s'ouvre que pour un Parent, OuvrirMenu) et
    /// re-gardé ici par sécurité. La grille reste en LECTURE SEULE : la dialog porte l'écriture (canal
    /// requête/réponse). Ferme le menu à l'ouverture de la dialog, comme les autres entrées.
    /// </summary>
    private void OuvrirDeleguer(DateOnly date)
    {
        if (!Session.EstParent)
            return;

        _dateMenu = null;
        _dateDialogDeleguer = date;
    }

    /// <summary>Ouvre le mini-dialog « proposer un échange » (s47) sur la <paramref name="date"/> de la case,
    /// depuis l'entrée « proposer un échange » du menu clic-case. Gating Invité (règle 9) mutualisé avec le menu
    /// (il ne s'ouvre que pour un Parent) et re-gardé ici. PROPOSER n'écrit rien (canal de consentement) : la
    /// case reste inchangée tant que le recevant n'a pas accepté depuis sa cloche.</summary>
    private void OuvrirProposer(DateOnly date)
    {
        if (!Session.EstParent)
            return;

        _dateMenu = null;
        _dateDialogProposer = date;
    }

    /// <summary>Ouvre le mini-dialog « signaler un imprévu » (s48) sur la <paramref name="date"/> de la case, depuis
    /// l'entrée « signaler un imprévu » du menu clic-case. Gating Invité (règle 9) mutualisé avec le menu (il ne
    /// s'ouvre que pour un Parent) et re-gardé ici. Purement INFORMATIF : le signalement n'écrit AUCUNE surcharge —
    /// la résolution reste inchangée (invariant s48), seule la cloche des concernés est notifiée.</summary>
    private void OuvrirSignalerImprevu(DateOnly date)
    {
        if (!Session.EstParent)
            return;

        _dateMenu = null;
        _dateDialogSignalerImprevu = date;
    }

    /// <summary>
    /// Ouvre le mini-dialog « reprendre ce jour » (s46) sur la <paramref name="date"/> de la case, depuis
    /// l'entrée CONDITIONNELLE du menu clic-case (visible seulement sur une case portant une délégation active).
    /// Gating Invité (règle 9) mutualisé avec le menu (il ne s'ouvre que pour un Parent, OuvrirMenu) et re-gardé
    /// ici par sécurité. La grille reste en LECTURE SEULE : la dialog porte l'écriture (canal requête/réponse).
    /// </summary>
    private void OuvrirReprendre(DateOnly date)
    {
        if (!Session.EstParent)
            return;

        _dateMenu = null;
        _dateDialogReprendre = date;
    }

    /// <summary>Vrai si la case de la <paramref name="date"/> porte une DÉLÉGATION ACTIVE (surcharge résolvable
    /// couvrant ce jour, PorteSurcharge du read model) — condition d'affichage de l'entrée « reprendre ce jour »
    /// (s46). Pur affichage : la décision de résolution vient de la projection distante, jamais recalculée ici.</summary>
    private bool CasePorteDelegationActive(DateOnly date)
        => _grille.Jours.FirstOrDefault(j => j.Date == date)?.PorteSurcharge == true;

    /// <summary>Depuis le menu, ouvre la dialog « Poser un slot » pré-remplie sur la date de la case.</summary>
    private void OuvrirPoserSlot(DateOnly date)
    {
        _dateMenu = null;
        _avertissementChevauchement = false; // un avertissement précédent ne survit pas à une nouvelle saisie
        _dateDialogPoserSlot = date;
    }

    /// <summary>Depuis le menu, ouvre la dialog « Affecter une période » pré-remplie sur la date de la case.
    /// La dialog reçoit la liste des acteurs déclarés en PARAMÈTRE (chargée à l'init + rafraîchie en temps
    /// réel) : aucun chargement async en son sein → pas de re-render pendant la saisie.</summary>
    private void OuvrirAffecterPeriode(DateOnly date)
    {
        _dateMenu = null;
        _plageFin = null; // ouverture single-jour : pas d'intervalle (Début = Fin = date dans la dialog)
        _dateDialogAffecterPeriode = date;
    }

    /// <summary>Depuis le menu (3ᵉ entrée), ouvre la dialog « Définir un transfert » pré-remplie sur la
    /// date de la case (Sc.1). Un accusé précédent ne survit pas à une nouvelle saisie.</summary>
    private void OuvrirDefinirTransfert(DateOnly date)
    {
        _dateMenu = null;
        _accuseTransfertDefini = false;
        _dateDialogDefinirTransfert = date;
    }

    /// <summary>Depuis le menu (4ᵉ entrée, Sc.6), ouvre la dialog « Supprimer une période » sur la date de
    /// la case : elle listera les périodes couvrant ce jour. Un accusé précédent ne survit pas à l'ouverture.</summary>
    private void OuvrirSupprimerPeriode(DateOnly date)
    {
        _dateMenu = null;
        _accusePeriodeSupprimee = false;
        _dateDialogSupprimerPeriode = date;
    }

    /// <summary>Depuis le menu (6ᵉ entrée, s18 Sc.6), ouvre la dialog « Supprimer un slot » sur la date de la
    /// case : elle listera les slots couvrant ce jour. Un accusé précédent ne survit pas à l'ouverture.</summary>
    private void OuvrirSupprimerSlot(DateOnly date)
    {
        _dateMenu = null;
        _accuseSlotSupprime = false;
        _dateDialogSupprimerSlot = date;
    }

    /// <summary>Issue succès de la suppression de slot (s18 Sc.6) : ferme la dialog, <b>relit</b> la grille
    /// distante (la case ne rend plus le slot retiré, les autres slots demeurent) et lève l'accusé « Slot
    /// supprimé » à part, non bloquant. Le retrait provient de la relecture, jamais d'une mutation locale.</summary>
    private async Task FermerSuppressionSlotEtAccuser()
    {
        await FermerDialogEtRecharger();
        _accuseSlotSupprime = true;
    }

    /// <summary>Referme l'accusé « Slot supprimé » (non bloquant).</summary>
    private void FermerAccuseSlotSupprime() => _accuseSlotSupprime = false;

    /// <summary>Depuis le menu (5ᵉ entrée, Sc.7), ouvre la dialog « Éditer une période » sur la date de la
    /// case : elle listera les périodes couvrant ce jour, chaque ligne ouvrant un formulaire pré-rempli. Un
    /// accusé précédent ne survit pas à l'ouverture.</summary>
    private void OuvrirEditerPeriode(DateOnly date)
    {
        _dateMenu = null;
        _accusePeriodeModifiee = false;
        _dateDialogEditerPeriode = date;
    }

    /// <summary>Issue succès de l'édition (Sc.7) : ferme la dialog, <b>relit</b> la grille distante (la case
    /// affiche le nouveau responsable ; une portion libérée retombe sur le fond / le neutre sans nom fantôme)
    /// et lève l'accusé « Période modifiée » à part, non bloquant. La re-résolution provient de la relecture,
    /// jamais d'une mutation locale de la grille.</summary>
    private async Task FermerEditionEtAccuser()
    {
        await FermerDialogEtRecharger();
        _accusePeriodeModifiee = true;
    }

    /// <summary>Referme l'accusé « Période modifiée » (non bloquant).</summary>
    private void FermerAccusePeriodeModifiee() => _accusePeriodeModifiee = false;

    /// <summary>Issue succès de la suppression (Sc.6) : ferme la dialog, <b>relit</b> la grille distante
    /// (la case re-résolue retombe sur le fond ou le neutre, sans nom fantôme) et lève l'accusé « Période
    /// supprimée » à part, non bloquant. La re-résolution provient de la relecture, jamais d'une mutation
    /// locale de la grille.</summary>
    private async Task FermerSuppressionEtAccuser()
    {
        await FermerDialogEtRecharger();
        _accusePeriodeSupprimee = true;
    }

    /// <summary>Referme l'accusé « Période supprimée » (non bloquant).</summary>
    private void FermerAccusePeriodeSupprimee() => _accusePeriodeSupprimee = false;

    /// <summary>Ferme la dialog ouverte sur succès et <b>relit</b> la grille depuis l'API distante :
    /// l'écriture aboutie réapparaît, positionnée à la date de la case (relecture, jamais une mutation
    /// locale de la grille).</summary>
    private async Task FermerDialogEtRecharger()
    {
        FermerDialog();
        await ChargerAsync();
    }

    /// <summary>Issue succès de la pose (Sc.7) : ferme la dialog, relit la grille, et lève le bandeau
    /// d'avertissement « à part » <b>si</b> l'outcome de la commande a signalé un chevauchement (règle 16,
    /// accepté + averti). Le drapeau vient de l'API (read model existant) — jamais recalculé ici.</summary>
    private async Task FermerPoserSlotEtRecharger(bool chevauchement)
    {
        await FermerDialogEtRecharger();
        _avertissementChevauchement = chevauchement;
    }

    /// <summary>Referme le bandeau d'avertissement de chevauchement (non bloquant).</summary>
    private void FermerAvertissement() => _avertissementChevauchement = false;

    /// <summary>Issue succès du transfert (Sc.1) : ferme la dialog SANS relire la grille (le transfert
    /// n'est pas projeté en case — règle 27, panneau cloche hors scope) et lève l'accusé « Transfert
    /// défini » à part, non bloquant. L'accusé se déclenche sur le simple succès HTTP du canal.</summary>
    private void FermerTransfertEtAccuser()
    {
        FermerDialog();
        _accuseTransfertDefini = true;
    }

    /// <summary>Referme l'accusé « Transfert défini » (non bloquant).</summary>
    private void FermerAccuseTransfert() => _accuseTransfertDefini = false;

    /// <summary>Ferme toute dialog sans aucune écriture (annulation / succès) : la grille reste intacte.</summary>
    private void FermerDialog()
    {
        _dateDialogPoserSlot = null;
        _dateDialogAffecterPeriode = null;
        _dateDialogDefinirTransfert = null;
        _dateDialogSupprimerPeriode = null;
        _dateDialogEditerPeriode = null;
        _dateDialogSupprimerSlot = null;
        _dateDialogDeleguer = null;
        _dateDialogReprendre = null;
        _dateDialogProposer = null;
        _dateDialogSignalerImprevu = null;
        // Une sélection de plage consommée (ou annulée) ne survit pas à la fermeture de la dialog.
        _plageDebut = null;
        _plageFin = null;
    }

    /// <summary>Vrai si la case correspond à la date du jour (port d'horloge injecté) — sert au marquage
    /// visuel « aujourd'hui » (Sc.4). Pur affichage : aucune règle métier, aucun observable de domaine.</summary>
    private bool EstAujourdhui(DateOnly date) => date == Horloge.Aujourdhui;

    /// <summary>Couleur PLEINE d'un acteur (responsable de case en pastille, ou créneau) via le thème
    /// couleur partagé. La pastille de responsable et le slot portent la couleur de la personne (donnée,
    /// inline) ; la case elle-même reste neutre (surface tokenisée) — rendu correct clair ET sombre.</summary>
    private static string Couleur(string couleur) => CouleursTheme.Pleine(couleur);

    /// <summary>Vrai en vue « Semaine » : la grille bascule alors du format cartes-jour (Mois / 4 semaines)
    /// vers la <b>grille horaire</b> (colonnes = jours, lignes = heures), façon agenda hebdomadaire.</summary>
    private bool VueHoraire => Session.Vue == VuePlanning.Semaine;

    // Grille horaire : hauteur d'une heure (px). Le placement d'un slot est purement dérivé de ses bornes
    // horaires (donnée), aucune règle métier — pure présentation.
    private const int HauteurHeurePx = 48;

    /// <summary>Position fractionnaire d'une heure (ex. 8h30 → 8.5) pour placer un slot sur l'axe vertical.</summary>
    private static double FractionHeure(TimeOnly t) => t.Hour + t.Minute / 60.0;

    /// <summary>Plage horaire [début, fin[ couverte par la grille horaire : englobe tous les slots de la
    /// fenêtre, élargie à un socle 8h→20h lisible (et jamais vide). Bornes entières d'heures.</summary>
    private (int Debut, int Fin) PlageHoraire()
    {
        var slots = _grille.Jours.SelectMany(j => j.Slots).ToList();
        var debut = 8;
        var fin = 20;
        foreach (var s in slots)
        {
            debut = Math.Min(debut, s.Debut.Hour);
            fin = Math.Max(fin, s.Fin.Minute > 0 ? s.Fin.Hour + 1 : s.Fin.Hour);
        }
        return (debut, Math.Max(fin, debut + 1));
    }

    /// <summary>Décalage vertical (px, entier) du haut d'un slot dans sa colonne, relatif au début de la
    /// plage. Entier → sérialisation CSS insensible à la culture (pas de virgule décimale en WASM).</summary>
    private static int HautSlotPx(SlotCase slot, int heureDebut)
        => (int)Math.Round((FractionHeure(slot.Debut) - heureDebut) * HauteurHeurePx);

    /// <summary>Hauteur (px, entier) d'un slot d'après sa durée, avec un minimum lisible (libellé + horaire).</summary>
    private static int HauteurSlotPx(SlotCase slot)
        => (int)Math.Round(Math.Max(20, (FractionHeure(slot.Fin) - FractionHeure(slot.Debut)) * HauteurHeurePx - 4));

    public async ValueTask DisposeAsync()
    {
        if (_abonnementEchap is not null)
            await _abonnementEchap.DisposeAsync();
        if (_hub is not null)
            await _hub.DisposeAsync();
    }
}
