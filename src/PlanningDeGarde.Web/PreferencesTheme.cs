using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace PlanningDeGarde.Web;

/// <summary>
/// Port d'ergonomie de surface — lecture du thème appliqué et persistance du choix
/// explicite clair/sombre. Refonte purement visuelle : aucune règle métier, aucun observable de domaine.
/// Le choix persisté prime sur la préférence système au chargement suivant (l'amorce inline lit
/// localStorage en premier).
/// </summary>
public interface IPreferencesTheme
{
    /// <summary>Thème actuellement appliqué (« clair » | « sombre »), lu depuis &lt;html data-theme&gt;.</summary>
    ValueTask<string> ThemeCourantAsync();

    /// <summary>Persiste le choix (localStorage) ET l'applique immédiatement (data-theme sur &lt;html&gt).</summary>
    ValueTask DefinirAsync(string theme);
}

/// <summary>
/// Adaptateur JS interop du port de thème : délègue au module <c>window.pdgTheme</c> défini inline dans
/// index.html (partagé avec l'amorce anti-flash). <c>definir</c> écrit localStorage et applique data-theme ;
/// <c>lire</c> retourne le thème appliqué. Adaptateur de bord : jamais doublé côté test (on double le port).
/// </summary>
public sealed class PreferencesThemeJs : IPreferencesTheme
{
    private readonly IJSRuntime _js;

    public PreferencesThemeJs(IJSRuntime js) => _js = js;

    public ValueTask<string> ThemeCourantAsync() => _js.InvokeAsync<string>("pdgTheme.lire");

    public ValueTask DefinirAsync(string theme) => _js.InvokeVoidAsync("pdgTheme.definir", theme);
}
