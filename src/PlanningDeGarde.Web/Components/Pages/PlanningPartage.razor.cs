using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;

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

    private GrilleAgenda _grille = new(Array.Empty<JourCase>(), Array.Empty<SemaineLigne>(), Array.Empty<EntreeLegende>());

    private HubConnection? _hub;

    // Écriture en contexte (palier 7) — la grille reste en LECTURE SEULE (règle 14) : la case ouvre un
    // menu d'actions, jamais une écriture. Un seul déclencheur par case (mutualise le gating Invité, Sc.6).
    // null = fermé. Date de la case dont le menu d'actions est ouvert :
    private DateOnly? _dateMenu;
    // Date de contexte de chaque dialog ouverte depuis le menu (null = dialog fermée) :
    private DateOnly? _dateDialogPoserSlot;
    private DateOnly? _dateDialogAffecterPeriode;
    private DateOnly? _dateDialogDefinirTransfert;
    // Avertissement de chevauchement « à part » (Sc.7, règle 16) : pose acceptée mais signalée, bandeau
    // NON bloquant et refermable. Drapeau porté par l'outcome de la commande (jamais recalculé ici).
    private bool _avertissementChevauchement;
    // Accusé « Transfert défini » à part (Sc.1) : feedback transitoire NON bloquant levé sur le simple
    // succès HTTP du canal (aucun read model neuf, aucun rendu en case — règle 27). Refermable.
    private bool _accuseTransfertDefini;

    private string Desactive => Session.EstParent ? string.Empty : "disabled";

    private RoleAuteur RoleSelectionne
    {
        get => Session.Role;
        set { Session.Role = value; }
    }

    protected override async Task OnInitializedAsync() => await ChargerAsync();

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
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

    // Date de référence = aujourd'hui, lue via le port d'horloge injecté (jamais DateTime.Now en dur :
    // déterminisme en test, symétrie avec Projeter(dateReference) côté lecture). Le canal de lecture
    // distant prend cette date en segments yyyy/MM/dd.
    private async Task ChargerAsync()
    {
        var aujourdHui = Horloge.Aujourdhui;
        try
        {
            var grille = await Canal.GetFromJsonAsync<GrilleAgenda>(
                $"api/grille/{aujourdHui.Year}/{aujourdHui.Month}/{aujourdHui.Day}");
            if (grille is not null)
                _grille = grille;
        }
        catch (HttpRequestException)
        {
            // API distante injoignable : la grille reste vide plutôt que de planter la vue.
        }
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

        _dateMenu = date;
    }

    /// <summary>Ferme le menu d'actions sans rien ouvrir (clic hors panneau).</summary>
    private void FermerMenu() => _dateMenu = null;

    /// <summary>Depuis le menu, ouvre la dialog « Poser un slot » pré-remplie sur la date de la case.</summary>
    private void OuvrirPoserSlot(DateOnly date)
    {
        _dateMenu = null;
        _avertissementChevauchement = false; // un avertissement précédent ne survit pas à une nouvelle saisie
        _dateDialogPoserSlot = date;
    }

    /// <summary>Depuis le menu, ouvre la dialog « Affecter une période » pré-remplie sur la date de la case.</summary>
    private void OuvrirAffecterPeriode(DateOnly date)
    {
        _dateMenu = null;
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
    }

    /// <summary>Teinte claire de la case-jour pour la couleur du responsable (fond pâle lisible
    /// avec du texte sombre), via le thème couleur partagé.</summary>
    private static string Teinte(string couleur) => CouleursTheme.Claire(couleur);

    /// <summary>Couleur pleine du créneau pour la couleur propre de l'acteur du slot, via le
    /// thème couleur partagé.</summary>
    private static string Couleur(string couleur) => CouleursTheme.Pleine(couleur);

    public async ValueTask DisposeAsync()
    {
        if (_hub is not null)
            await _hub.DisposeAsync();
    }
}
