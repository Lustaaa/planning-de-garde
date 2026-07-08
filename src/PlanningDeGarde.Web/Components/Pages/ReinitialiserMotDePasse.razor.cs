using System;
using System.Net.Http.Json;
using System.Threading.Tasks;
using static PlanningDeGarde.Web.CanalEcriture;

namespace PlanningDeGarde.Web.Components.Pages;

/// <summary>
/// Écran « redéfinir par jeton » (front <b>WASM</b>, s28, volet 1) : le jeton de réinitialisation reçu par
/// mail est porté par l'URL (<c>?jeton=…</c>). L'utilisateur saisit un nouveau mot de passe → redéfinition
/// via le <b>canal requête/réponse</b> HTTP (<c>POST /api/canal/redefinir-mot-de-passe</c>) — le jeton est
/// consommé (usage unique) côté handler. Sur succès, un message invite à se connecter ; sur refus (jeton
/// invalide / consommé / expiré), le motif est surfacé. Le front ne porte AUCUNE règle métier.
/// </summary>
public partial class ReinitialiserMotDePasse
{
    private string _jeton = "";
    private string _nouveauMotDePasse = "";
    private string? _messageSucces;
    private string? _messageErreur;

    /// <summary>Au montage, extrait le jeton de réinitialisation porté par l'URL (<c>?jeton=…</c>, lien
    /// reçu par mail) — c'est la clé opaque présentée au handler lors de la redéfinition. Lecture seule.</summary>
    protected override void OnInitialized()
    {
        // Extraction du paramètre « jeton » de la chaîne de requête (?jeton=…) sans dépendance framework
        // (WebUtilities absent du profil WASM) : split simple sur & puis =, valeur dé-échappée.
        var query = new Uri(Nav.Uri).Query.TrimStart('?');
        foreach (var paire in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = paire.Split('=', 2);
            if (kv.Length == 2 && kv[0] == "jeton")
                _jeton = Uri.UnescapeDataString(kv[1]);
        }
    }

    private async Task RedefinirAsync()
    {
        _messageSucces = null;
        _messageErreur = null;

        // Redéfinition via le canal requête/réponse. Sur succès (jeton valide), le mot de passe est redéfini
        // (haché côté serveur) et le jeton consommé (usage unique) : on invite à se connecter. Sur refus
        // (jeton inconnu / déjà consommé / expiré), le motif renvoyé est surfacé — usage unique observable.
        var reponse = await Canal.PostAsJsonAsync(
            "api/canal/redefinir-mot-de-passe", new RedefinirMotDePasseRequete(_jeton, _nouveauMotDePasse));
        if (reponse.IsSuccessStatusCode)
            _messageSucces = "Votre mot de passe a été redéfini. Vous pouvez vous connecter.";
        else
            _messageErreur = await reponse.Content.ReadAsStringAsync();
    }
}
