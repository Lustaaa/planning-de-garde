using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.Web;

/// <summary>
/// Jeton de session persisté côté client (volet 1 Login F5, s31) : l'identité du compte connecté
/// re-hydratable au démarrage (id d'acteur stable + nom d'affichage + type). C'est le strict minimum
/// pour ré-ouvrir la même session sans repasser par le flux de connexion. Aucune donnée domaine, aucun
/// secret : ce n'est que l'ancrage d'identité déjà résolu serveur à la connexion (s23/s25).
/// </summary>
public sealed record SessionPersistee(string ActeurId, string Nom, TypeActeur Type);

/// <summary>
/// Port de persistance durable de la session côté client (s31, volet 1). Distinct de
/// <see cref="State.SessionPlanning"/> qui reste en MÉMOIRE (borne anti-cliquet R30) : ce port ne fait que
/// persister/relire le jeton d'amorçage à travers un stockage qui survit au rechargement (F5). Adaptateur de
/// bord (JS localStorage) — jamais doublé en dehors du test ; côté test, on double ce port à la main.
/// </summary>
public interface IPersistanceSession
{
    /// <summary>Persiste le jeton de session (à la connexion) dans le stockage durable client.</summary>
    ValueTask PersisterAsync(SessionPersistee jeton);

    /// <summary>Relit le jeton persisté (au démarrage), ou <c>null</c> si le stockage est vide.</summary>
    ValueTask<SessionPersistee?> LireAsync();
}

/// <summary>
/// Adaptateur JS interop du port de persistance de session (s31) : délègue au module <c>window.pdgSession</c>
/// défini inline dans index.html (localStorage['pdg-session']). Le jeton est sérialisé/désérialisé en JSON à
/// la frontière JS. Adaptateur de bord : jamais doublé côté test (on double le port). Un jeton relu illisible
/// (JSON corrompu) est traité comme absent — la restauration n'ouvrira aucune session (jeton invalide, Sc.2).
/// </summary>
public sealed class PersistanceSessionJs : IPersistanceSession
{
    private readonly IJSRuntime _js;

    public PersistanceSessionJs(IJSRuntime js) => _js = js;

    public ValueTask PersisterAsync(SessionPersistee jeton)
        => _js.InvokeVoidAsync("pdgSession.persister", JsonSerializer.Serialize(jeton));

    public async ValueTask<SessionPersistee?> LireAsync()
    {
        var json = await _js.InvokeAsync<string?>("pdgSession.lire");
        if (string.IsNullOrWhiteSpace(json))
            return null;
        try
        {
            return JsonSerializer.Deserialize<SessionPersistee>(json);
        }
        catch (JsonException)
        {
            return null; // jeton illisible → traité comme absent (aucune session fantôme)
        }
    }
}
