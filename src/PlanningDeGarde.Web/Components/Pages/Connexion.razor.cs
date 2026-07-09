using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using PlanningDeGarde.Web.State;
using static PlanningDeGarde.Web.CanalEcriture;

namespace PlanningDeGarde.Web.Components.Pages;

/// <summary>
/// Page de connexion dédiée (front <b>WASM</b>, s24) : landing par défaut et <b>seul chemin d'entrée</b>
/// (Sc.8/Sc.10). Emballe la connexion locale par email (<see cref="SeConnecterCommand"/> s23) via le
/// <b>canal requête/réponse</b> HTTP (<c>POST /api/canal/se-connecter</c>, règle 27 — aucune vue n'écrit le
/// domaine en direct). Le front ne porte AUCUNE règle d'admission : l'admission (compte existant ET Actif)
/// est tranchée par le handler. Sur succès, la session pré-positionne l'acteur du compte (incarnation bornée
/// s14, lecture seule, aucune persistance neuve) puis <b>redirige vers le planning</b>. Sur refus (email
/// inconnu / compte non activé), le motif clair est surfacé, on reste sur la page (Sc.9).
/// </summary>
public partial class Connexion
{
    private string _email = "";
    private string _motDePasse = "";
    private string? _motif;

    /// <summary>Visibilité en clair du mot de passe (Sc.4) : faux = champ masqué (type password, défaut),
    /// vrai = champ visible (type text). Pur état de présentation, aucune règle métier.</summary>
    private bool _motDePasseVisible;

    /// <summary>Bascule l'affichage/masquage du mot de passe (bouton œil, Sc.4).</summary>
    private void BasculerVisibiliteMotDePasse() => _motDePasseVisible = !_motDePasseVisible;

    /// <summary>Au montage, charge le catalogue d'acteurs incarnables depuis le référentiel réel
    /// (GET /api/foyer/acteurs) et le dépose dans la session : c'est ce catalogue que résout l'incarnation
    /// du compte connecté (pré-positionnement du sélecteur d'acteur, s23 Sc.8). Lecture seule.</summary>
    protected override async Task OnInitializedAsync()
    {
        try
        {
            var acteurs = await Canal.GetFromJsonAsync<List<ActeurFoyer>>("api/foyer/acteurs");
            if (acteurs is not null)
                Session.ActeursIncarnables = acteurs
                    .Select(a => new IdentiteActeur(a.Id, a.Nom, a.Type))
                    .ToList();
        }
        catch (HttpRequestException)
        {
            // Référentiel distant injoignable : le catalogue reste vide ; la connexion reste tentable.
        }
    }

    /// <summary>Déclenche le flux OAuth du provider (volet 4, s25) : navigue vers l'endpoint de
    /// démarrage OAuth côté serveur (<c>api/oauth/{provider}/demarrer</c>), qui redirige le navigateur
    /// vers l'authorize réel du provider (Google/Microsoft/Apple — secrets/callbacks vérifiés
    /// manuellement au G3). La vue ne porte AUCUNE règle métier : elle ne fait que déclencher le flux ;
    /// la résolution de l'identité externe et l'ouverture de session sont tranchées côté serveur
    /// (ConnexionOAuthHandler, Sc.14/Sc.15).</summary>
    private void DemarrerOAuth(string provider)
        => Nav.NavigateTo($"api/oauth/{provider}/demarrer", forceLoad: true);

    private async Task SeConnecterAsync()
    {
        _motif = null;

        HttpResponseMessage reponse;
        try
        {
            reponse = await Canal.PostAsJsonAsync(
                "api/canal/se-connecter", new SeConnecterRequete(_email, _motDePasse));
        }
        catch (HttpRequestException)
        {
            _motif = "Service injoignable, réessayez.";
            return;
        }

        if (!reponse.IsSuccessStatusCode)
        {
            // Refus métier (email inconnu / compte non activé) : motif clair, on reste sur la page (Sc.9).
            _motif = await reponse.Content.ReadAsStringAsync();
            return;
        }

        var session = await reponse.Content.ReadFromJsonAsync<SeConnecterReponse>();
        if (session is not null)
        {
            // Ouvre la session (nom résolu serveur, s23) en ANCRANT l'identité réelle sur l'acteur lié au
            // compte connecté ET son type (résolu serveur, s25 Sc.5) — état partagé de session qui surface le
            // menu utilisateur (Sc.11) — puis redirige vers le planning (Sc.8). Le gating d'écriture suit
            // désormais le type RÉEL de l'acteur, jamais un rôle Parent hérité du configurateur en dur.
            Session.Connecter(session.Nom, session.ActeurId, session.Type);
            // Persiste le jeton de session dans le stockage durable client (localStorage via le port) : c'est
            // lui que le démarrage suivant relira pour restaurer la session après un F5 (s31, Sc.1). On persiste
            // l'identité réelle déjà résolue serveur (acteur + nom + type), aucun secret. La session reste en
            // mémoire (borne R30) — on ne fait qu'écrire son amorce d'identité rejouable.
            await Persistance.PersisterAsync(new SessionPersistee(session.ActeurId, session.Nom, session.Type));
            Nav.NavigateTo("planning");
        }
    }
}
