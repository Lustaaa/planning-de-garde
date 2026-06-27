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
/// éditable), seul le nom mute.
/// </summary>
public partial class ConfigurationFoyer
{
    private sealed class Formulaire
    {
        public string ActeurId { get; set; } = "";
        public string Nom { get; set; } = "";
    }

    private readonly Formulaire _form = new();
    private string? _confirmation;
    private string? _motifEchec;

    private async Task Soumettre()
    {
        _confirmation = null;
        _motifEchec = null;

        HttpResponseMessage reponse;
        try
        {
            // Émission de la commande d'édition via le canal HTTP de l'API distante (adaptateur de gauche).
            reponse = await Canal.PostAsJsonAsync(
                "api/canal/editer-acteur",
                new EditerActeurRequete(_form.ActeurId, _form.Nom));
        }
        catch (HttpRequestException)
        {
            // API distante injoignable (échec de transport) : message dédié, saisie conservée,
            // aucune écriture ni mise en file. Cf. Sc.9.
            _motifEchec = PoserSlot.MessageServiceInjoignable;
            return;
        }

        if (reponse.IsSuccessStatusCode)
            _confirmation = $"« {_form.Nom} » enregistré.";
        else
            _motifEchec = await reponse.Content.ReadAsStringAsync();
    }
}
