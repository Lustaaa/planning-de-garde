using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using static PlanningDeGarde.Web.CanalEcriture;

namespace PlanningDeGarde.Web.Components.Comptes;

/// <summary>
/// Écran « mot de passe oublié » (front <b>WASM</b>) : émet une demande de récupération de
/// mot de passe via le <b>canal requête/réponse</b> HTTP (<c>POST /api/comptes/recuperation</c>,
/// — aucune vue n'écrit le domaine en direct) puis affiche un <b>message neutre fixe</b>. Anti
/// énumération : le message est le même que l'email soit connu ou non (l'API répond toujours par un succès
/// neutre, jeton + mail restant hors du canal de réponse). Le front ne porte AUCUNE règle métier.
/// </summary>
public partial class MotDePasseOublie
{
    private string _email = "";
    private bool _messageNeutre;

    private async Task DemanderAsync()
    {
        // Émet la demande de récupération via le canal requête/réponse (l'API répond toujours par un succès
        // neutre — anti-énumération). On affiche le message neutre fixe une fois la demande émise ; il est
        // identique que l'email soit connu ou non, ne révélant jamais l'existence d'un compte.
        var reponse = await Canal.PostAsJsonAsync(
            "api/comptes/recuperation", new DemanderRecuperationRequete(_email));
        if (reponse.IsSuccessStatusCode)
            _messageNeutre = true;
    }
}
