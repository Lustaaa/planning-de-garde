using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using static PlanningDeGarde.Web.CanalEcriture;

namespace PlanningDeGarde.Web.Components.Pages;

/// <summary>
/// Écran de configuration du foyer (front <b>WASM</b>) : renomme un acteur déjà semé. L'écriture
/// passe par le <b>canal requête/réponse</b> (endpoint HTTP <c>/api/canal/editer-acteur</c>),
/// JAMAIS par un appel de handler en DI direct ni par le canal de diffusion (SignalR, lecture
/// seule) — règle 27. Sur succès, la vue confirme l'effet et <b>reste</b> sur l'écran (l'édition
/// est volatile, on peut en enchaîner d'autres) : la grille partagée suit sans rechargement via la
/// diffusion temps réel déclenchée par l'édition aboutie côté API. Sur refus, le motif métier
/// propagé est affiché. Aucune règle métier dans l'UI : l'identifiant stable est la clé (jamais
/// éditable), seuls le nom et la couleur mutent — deux surfaces indépendantes (un champ laissé
/// vide est envoyé <c>null</c> et n'est pas appliqué).
/// </summary>
public partial class ConfigurationFoyer
{
    private sealed class Formulaire
    {
        public string ActeurId { get; set; } = "";
        public string Nom { get; set; } = "";
        public string Couleur { get; set; } = "";
    }

    private sealed class FormulaireAjout
    {
        public string Nom { get; set; } = "";
        public string Couleur { get; set; } = "";
    }

    /// <summary>Onglet de thème actuellement affiché (Sc.2). L'écran est réorganisé en trois onglets par
    /// thème — <c>acteurs</c> (CRUD acteurs), <c>periode-garde</c> (cycle de fond), <c>slot-recurrent</c>
    /// (réservé, placeholder) — <b>sans</b> fonctionnalité neuve : c'est un simple cloisonnement de
    /// surface du contenu existant. « Acteurs » est actif par défaut ; changer d'onglet ne perd pas
    /// l'état de saisie et ne déclenche aucune écriture.</summary>
    private string _ongletActif = "acteurs";

    private void ActiverOnglet(string onglet) => _ongletActif = onglet;

    private readonly Formulaire _form = new();
    private string? _confirmation;
    private string? _motifEchec;

    private readonly FormulaireAjout _ajout = new();
    private string? _motifEchecAjout;

    /// <summary>Accusé non bloquant de suppression (registre avertissement-à-part, aligné « Transfert
    /// défini » — D5) : affiché à côté de la liste sans interrompre la consultation, effacé à la
    /// suppression suivante.</summary>
    private string? _accuseSuppression;

    /// <summary>Motif d'échec de suppression (service injoignable, règle 28) — surface distincte de
    /// l'accusé : la liste/grille/légende restent inchangées, aucune mise en file (Sc.8).</summary>
    private string? _motifEchecSuppression;

    private sealed class FormulaireCycle
    {
        public int NombreSemaines { get; set; } = 2;
        public Dictionary<int, string> Affectations { get; } = new();
    }

    private readonly FormulaireCycle _cycle = new();
    private string? _confirmationCycle;
    private string? _motifEchecCycle;

    /// <summary>Affecte (ou retire, si vide) un responsable à un index de semaine du cycle en cours de
    /// saisie. La valeur bindée est l'identifiant stable de l'acteur (jamais le libellé, règle 19).</summary>
    private void AffecterIndex(int index, string? responsableId)
    {
        if (string.IsNullOrWhiteSpace(responsableId))
            _cycle.Affectations.Remove(index);
        else
            _cycle.Affectations[index] = responsableId;
    }

    /// <summary>Acteurs du foyer énumérés <b>depuis le store durable</b> (canal de lecture HTTP), et non
    /// la liste statique front : c'est cette énumération qui fait apparaître un acteur ajouté (Sc.1).</summary>
    private IReadOnlyList<ActeurFoyer> _acteurs = Array.Empty<ActeurFoyer>();

    /// <summary>Formulaire de saisie du libellé d'un rôle à créer (référentiel du foyer, s21). Le front
    /// n'émet que le libellé ; l'identifiant stable neuf opaque est généré côté handler.</summary>
    private sealed class FormulaireRole
    {
        public string Libelle { get; set; } = "";
    }

    private readonly FormulaireRole _role = new();
    private string? _motifEchecRole;

    /// <summary>Rôles du référentiel du foyer énumérés <b>depuis le store durable</b> (GET /api/foyer/roles),
    /// jamais un rôle en dur : alimente la liste des rôles de l'onglet Acteurs (créés / renommés / supprimés
    /// suivent sans rechargement, Sc.7).</summary>
    private IReadOnlyList<RoleFoyer> _roles = Array.Empty<RoleFoyer>();

    /// <summary>Tampon d'édition du libellé par ligne de rôle (renommage inline) : clé = identifiant stable
    /// du rôle, valeur = nouveau libellé saisi. La clé n'est jamais éditable (règle 19).</summary>
    private readonly Dictionary<string, string> _renommageRole = new();

    /// <summary>Fournisseur de services pour résoudre <see cref="OptionsConnexionHub"/> de façon
    /// <b>optionnelle</b> : présent, il redirige la connexion au hub vers le TestServer (acceptation runtime
    /// Sc.6) ; absent (écrans de config qui n'observent pas le temps réel), la connexion reste neutre et son
    /// éventuel échec est simplement avalé — l'écran demeure fonctionnel.</summary>
    [Inject] private IServiceProvider Services { get; set; } = default!;

    private HubConnection? _hub;

    /// <summary>Au montage de l'écran, charge acteurs et rôles depuis le store via l'API distante.</summary>
    protected override async Task OnInitializedAsync()
    {
        await RechargerActeurs();
        await RechargerRoles();
    }

    private async Task RechargerActeurs()
        => _acteurs = await Canal.GetFromJsonAsync<List<ActeurFoyer>>("api/foyer/acteurs")
            ?? new List<ActeurFoyer>();

    /// <summary>Ré-énumère les rôles du référentiel depuis le store durable (GET /api/foyer/roles) : c'est
    /// cette relecture qui fait suivre la liste des rôles après création / renommage / suppression (Sc.7).</summary>
    private async Task RechargerRoles()
        => _roles = await Canal.GetFromJsonAsync<List<RoleFoyer>>("api/foyer/roles")
            ?? new List<RoleFoyer>();

    /// <summary>
    /// S'abonne au <b>hub SignalR de lecture</b> de l'API distante (même hôte que le canal) pour préserver
    /// le <b>temps réel</b> sur l'écran de configuration (Sc.6) : une écriture aboutie ailleurs — typiquement
    /// un acteur ajouté ou renommé depuis un second écran (store partagé) — <b>ré-énumère</b> les acteurs
    /// depuis le store unifié, si bien que le sélecteur d'édition (onglet Acteurs) et la liste suivent
    /// <b>sans rechargement</b>, cohérents avec la grille, la légende et les sélecteurs des dialogs. Lecture
    /// seule : la diffusion ne déclenche jamais d'écriture. Le temps réel est un confort : si le hub est
    /// indisponible, l'écran reste fonctionnel (rechargement à la navigation).
    /// </summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        try
        {
            var urlHub = new Uri(Canal.BaseAddress!, "hubs/planning");
            var configurer = Services.GetService<OptionsConnexionHub>()?.Configurer ?? (_ => { });
            _hub = new HubConnectionBuilder()
                .WithUrl(urlHub, configurer)
                .WithAutomaticReconnect()
                .Build();

            _hub.On(PlanningHubEvenement.MiseAJour, async () =>
            {
                await RechargerActeurs();
                await InvokeAsync(StateHasChanged);
            });

            await _hub.StartAsync();
        }
        catch
        {
            // Hub indisponible : le temps réel est un confort, l'écran reste consultable et éditable.
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_hub is not null)
            await _hub.DisposeAsync();
    }

    /// <summary>Revient à l'identité réelle depuis le bandeau d'incarnation de l'écran de configuration
    /// (sprint 14, cohérence inter-écrans, Sc.2) : l'incarnation est levée → les écritures config
    /// redeviennent visibles (gating sur l'identité effective). Aucune écriture domaine.</summary>
    private void RevenirIdentiteReelle() => Session.RevenirIdentiteReelle();

    /// <summary>Nom d'affichage courant de l'acteur sélectionné (aide de saisie, miroir du seed) —
    /// <c>null</c> tant qu'aucun acteur n'est choisi. Sert d'indicateur « ce que vous éditez ».</summary>
    private string? NomActuel
        => _acteurs.FirstOrDefault(a => a.Id == _form.ActeurId)?.Nom;

    /// <summary>À la sélection d'un acteur, pré-remplit le champ nom avec son nom courant et efface les
    /// messages de l'édition précédente — pour qu'on parte de la valeur en place plutôt que d'un champ
    /// vide. L'utilisateur ajuste ensuite le nom et/ou la couleur.</summary>
    private void PreRemplirNom()
    {
        _confirmation = null;
        _motifEchec = null;
        _form.Nom = NomActuel ?? "";
    }

    private async Task Soumettre()
    {
        _confirmation = null;
        _motifEchec = null;

        // Un champ laissé vide n'est pas une édition : il part null (non appliqué côté handler), pour
        // ne pas écraser le nom par une chaîne vide lors d'un recoloriage seul (et inversement).
        var couleur = string.IsNullOrWhiteSpace(_form.Couleur) ? null : _form.Couleur;
        // Sc.8 : un nom vide / tout-espaces soumis SANS recoloriage concurrent est une tentative de
        // renommage à vide — on transmet la valeur brute pour que le serveur la refuse avec son motif
        // métier (« le nom ne peut pas être vide »), surfacé à l'écran. Avec un recoloriage, un nom vide
        // reste un recoloriage-seul (nom non appliqué, Sc.2) : il part null.
        var nom = string.IsNullOrWhiteSpace(_form.Nom)
            ? (couleur is null ? _form.Nom : null)
            : _form.Nom;

        HttpResponseMessage reponse;
        try
        {
            // Émission de la commande d'édition via le canal HTTP de l'API distante (adaptateur de gauche).
            reponse = await Canal.PostAsJsonAsync(
                "api/canal/editer-acteur",
                new EditerActeurRequete(_form.ActeurId, nom, couleur));
        }
        catch (HttpRequestException)
        {
            // API distante injoignable (échec de transport) : message dédié, saisie conservée,
            // aucune écriture ni mise en file. Cf. Sc.9.
            _motifEchec = MessagesEcriture.ServiceInjoignable;
            return;
        }

        if (reponse.IsSuccessStatusCode)
            _confirmation = "Modification enregistrée.";
        else
            // Le canal renvoie le motif métier en corps JSON (Results.BadRequest(string)) : on le
            // désérialise comme la chaîne qu'il est, pour surfacer un message propre (« le nom ne peut
            // pas être vide ») sans guillemets parasites (Sc.8).
            _motifEchec = await reponse.Content.ReadFromJsonAsync<string>();
    }

    /// <summary>
    /// Ajoute un acteur neuf au foyer via le <b>canal d'écriture HTTP</b> de l'API distante
    /// (<c>POST /api/canal/ajouter-acteur</c>, règle 27 — aucune vue n'écrit le domaine en direct),
    /// puis ré-énumère le store pour faire apparaître l'acteur ajouté <b>sans rechargement</b> (Sc.1).
    /// Sur refus métier (Sc.8, nom vide), le motif renvoyé par le canal est surfacé sans muter la liste.
    /// Sur <b>service injoignable</b> (Sc.9 s09, échec de transport <see cref="HttpRequestException"/> avant
    /// que le handler ne tourne), un message dédié s'affiche, la saisie est conservée et rien n'est enregistré.
    /// </summary>
    private async Task Ajouter()
    {
        _motifEchecAjout = null;

        HttpResponseMessage reponse;
        try
        {
            reponse = await Canal.PostAsJsonAsync(
                "api/canal/ajouter-acteur",
                new AjouterActeurRequete(_ajout.Nom, _ajout.Couleur));
        }
        catch (HttpRequestException)
        {
            // Service de configuration injoignable (échec de transport, pas un refus métier Sc.8) : le
            // handler AjouterActeur ne s'exécute jamais. Message dédié, saisie « Carla / rose » conservée
            // à resoumettre, aucune écriture ni mise en file (règle 28). Cf. Sc.9 (s09).
            _motifEchecAjout = MessagesEcriture.ServiceInjoignable;
            return;
        }

        if (!reponse.IsSuccessStatusCode)
        {
            // Refus métier (nom vide / tout-espaces, Sc.8) : le canal renvoie le motif en corps JSON
            // (Results.BadRequest(string)). On le surface tel quel à l'écran, sans muter la liste ni
            // effacer la saisie — aucun identifiant n'est généré, la liste des acteurs reste inchangée.
            _motifEchecAjout = await reponse.Content.ReadFromJsonAsync<string>();
            return;
        }

        // La liste reflète l'ajout sans recharger la page : on relit l'énumération du store durable.
        await RechargerActeurs();
        _ajout.Nom = "";
        _ajout.Couleur = "";
    }

    /// <summary>
    /// Supprime un acteur du foyer via le <b>canal d'écriture HTTP</b> de l'API distante
    /// (<c>POST /api/canal/supprimer-acteur</c>, règle 27 — aucune vue n'écrit le domaine en direct),
    /// puis ré-énumère le store pour que l'acteur supprimé <b>quitte la liste sans rechargement</b> (Sc.6).
    /// Sur succès, un accusé <b>« Acteur supprimé »</b> non bloquant s'affiche à part (D5) et le handler a
    /// muté le store ET déclenché la diffusion temps réel (grilles et légende dédoublonnée suivent — le
    /// filtre d'existence côté projection neutralise l'acteur orphelin). Sur <b>service injoignable</b>
    /// (échec de transport <see cref="HttpRequestException"/>, règle 28), un message dédié s'affiche, la
    /// liste/grille/légende restent inchangées, rien n'est mis en file (Sc.8). La clé est l'identifiant
    /// stable opaque (jamais le libellé, règle 19) ; aucune règle métier dans l'UI (idempotence côté handler).
    /// </summary>
    private async Task Supprimer(string acteurId)
    {
        _accuseSuppression = null;
        _motifEchecSuppression = null;

        HttpResponseMessage reponse;
        try
        {
            reponse = await Canal.PostAsJsonAsync(
                "api/canal/supprimer-acteur",
                new SupprimerActeurRequete(acteurId));
        }
        catch (HttpRequestException)
        {
            // Service de configuration injoignable (échec de transport, pas un refus métier) : le handler
            // SupprimerActeur ne s'exécute jamais. Message dédié, liste/grille/légende inchangées, aucune
            // suppression ni mise en file (règle 28). Cf. Sc.8.
            _motifEchecSuppression = MessagesEcriture.ServiceInjoignable;
            return;
        }

        if (!reponse.IsSuccessStatusCode)
        {
            // Refus métier éventuel : on surface le motif renvoyé par le canal sans muter la liste.
            _motifEchecSuppression = await reponse.Content.ReadFromJsonAsync<string>();
            return;
        }

        // La liste reflète la suppression sans recharger la page : on relit l'énumération du store durable.
        await RechargerActeurs();
        _accuseSuppression = "Acteur supprimé.";
    }

    /// <summary>
    /// Crée un rôle du référentiel du foyer via le <b>canal d'écriture HTTP</b> de l'API distante
    /// (<c>POST /api/canal/creer-role</c>, règle 27 — aucune vue n'écrit le domaine en direct), puis
    /// ré-énumère le référentiel pour faire apparaître le rôle créé <b>sans rechargement</b> (Sc.7). Le
    /// front n'émet que le libellé ; l'identifiant stable neuf est généré côté handler. Sur refus métier
    /// (libellé vide / doublon, Sc.3), le motif renvoyé par le canal est surfacé sans muter la liste.
    /// </summary>
    private async Task CreerRole()
    {
        _motifEchecRole = null;

        HttpResponseMessage reponse;
        try
        {
            reponse = await Canal.PostAsJsonAsync(
                "api/canal/creer-role",
                new CreerRoleRequete(_role.Libelle));
        }
        catch (HttpRequestException)
        {
            _motifEchecRole = MessagesEcriture.ServiceInjoignable;
            return;
        }

        if (!reponse.IsSuccessStatusCode)
        {
            _motifEchecRole = await reponse.Content.ReadFromJsonAsync<string>();
            return;
        }

        await RechargerRoles();
        _role.Libelle = "";
    }

    /// <summary>Libellé courant du champ de renommage inline d'un rôle : le tampon saisi s'il existe,
    /// sinon le libellé persisté (valeur de départ).</summary>
    private string LibelleRenommage(string roleId, string libellePersiste)
        => _renommageRole.TryGetValue(roleId, out var l) ? l : libellePersiste;

    /// <summary>Mémorise le nouveau libellé saisi pour le renommage inline d'un rôle (clé = identifiant
    /// stable, jamais éditable).</summary>
    private void SaisirRenommage(string roleId, string? libelle)
        => _renommageRole[roleId] = libelle ?? "";

    /// <summary>Renomme un rôle du référentiel via le <b>canal d'écriture HTTP</b>
    /// (<c>POST /api/canal/renommer-role</c>) : la clé est l'identifiant stable du rôle (jamais éditable,
    /// règle 19), seul le libellé change. Sur succès, on relit le référentiel (le libellé suit sans
    /// rechargement, même id, Sc.7) ; sur refus métier, le motif est surfacé.</summary>
    private async Task RenommerRole(string roleId)
    {
        _motifEchecRole = null;
        var nouveauLibelle = _renommageRole.TryGetValue(roleId, out var l) ? l : "";

        HttpResponseMessage reponse;
        try
        {
            reponse = await Canal.PostAsJsonAsync(
                "api/canal/renommer-role",
                new RenommerRoleRequete(roleId, nouveauLibelle));
        }
        catch (HttpRequestException)
        {
            _motifEchecRole = MessagesEcriture.ServiceInjoignable;
            return;
        }

        if (!reponse.IsSuccessStatusCode)
        {
            _motifEchecRole = await reponse.Content.ReadFromJsonAsync<string>();
            return;
        }

        await RechargerRoles();
    }

    /// <summary>Supprime un rôle du référentiel via le <b>canal d'écriture HTTP</b>
    /// (<c>POST /api/canal/supprimer-role</c>) : la clé est l'identifiant stable du rôle. Sur succès, on
    /// relit le référentiel (le rôle quitte la liste sans rechargement, Sc.7). Idempotence côté handler.</summary>
    private async Task SupprimerRole(string roleId)
    {
        _motifEchecRole = null;

        HttpResponseMessage reponse;
        try
        {
            reponse = await Canal.PostAsJsonAsync(
                "api/canal/supprimer-role",
                new SupprimerRoleRequete(roleId));
        }
        catch (HttpRequestException)
        {
            _motifEchecRole = MessagesEcriture.ServiceInjoignable;
            return;
        }

        if (!reponse.IsSuccessStatusCode)
        {
            _motifEchecRole = await reponse.Content.ReadFromJsonAsync<string>();
            return;
        }

        await RechargerRoles();
    }

    /// <summary>
    /// Définit / ré-édite le cycle de fond via le <b>canal d'écriture HTTP</b> de l'API distante
    /// (<c>POST /api/canal/definir-cycle</c>, règle 27). Sur succès, la grille partagée suit sans
    /// rechargement via la diffusion temps réel déclenchée côté API. Sur refus métier (N &lt; 1, Sc.7),
    /// le motif propagé est affiché.
    /// </summary>
    private async Task DefinirCycle()
    {
        _confirmationCycle = null;
        _motifEchecCycle = null;

        HttpResponseMessage reponse;
        try
        {
            reponse = await Canal.PostAsJsonAsync(
                "api/canal/definir-cycle",
                new DefinirCycleRequete(_cycle.NombreSemaines, _cycle.Affectations));
        }
        catch (HttpRequestException)
        {
            // Service de configuration injoignable (échec de transport, pas un refus métier Sc.7) : le
            // handler DefinirCycle ne s'exécute jamais. Message dédié, saisie du cycle (N + mapping)
            // conservée à resoumettre, aucun cycle enregistré ni mis en file (règle 28). Cf. Sc.8 / s09 Sc.9.
            _motifEchecCycle = MessagesEcriture.ServiceInjoignable;
            return;
        }

        if (reponse.IsSuccessStatusCode)
            _confirmationCycle = "Cycle de fond enregistré.";
        else
            _motifEchecCycle = await reponse.Content.ReadFromJsonAsync<string>();
    }
}
