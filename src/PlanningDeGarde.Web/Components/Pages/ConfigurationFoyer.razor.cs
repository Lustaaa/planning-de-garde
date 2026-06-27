using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
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

    private readonly Formulaire _form = new();
    private string? _confirmation;
    private string? _motifEchec;

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
            _motifEchec = PoserSlot.MessageServiceInjoignable;
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
}
