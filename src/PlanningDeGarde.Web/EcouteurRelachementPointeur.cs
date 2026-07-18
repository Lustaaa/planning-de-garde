using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace PlanningDeGarde.Web;

/// <summary>
/// Port de geste de surface (s49 — correctif du gate G3 : le drag ne fonctionnait pas en navigateur RÉEL).
/// Écoute le RELÂCHEMENT du pointeur au niveau <b>document</b> (<c>pointerup</c>), jamais un
/// <c>@onmouseup</c>/<c>@onpointerup</c> posé sur la case seule : en navigateur réel, relâcher le bouton
/// <b>hors d'une case</b> (dans une gouttière, au-delà du bord de la grille, sur le document) N'ATTEINT PAS
/// le handler de la case → la plage n'est jamais finalisée. La capture <b>document</b> attrape ce relâchement
/// quel que soit l'endroit du lâcher. Aucune règle métier, aucun observable de domaine.
/// </summary>
public interface IEcouteurRelachementPointeur
{
    /// <summary>Attache un écouteur <c>document</c> de <c>pointerup</c> : <paramref name="onRelache"/> est
    /// rappelé à chaque relâchement du pointeur (le composant décide s'il finalise une sélection en cours).
    /// L'<see cref="IAsyncDisposable"/> retourné détache l'écouteur (à disposer à la fermeture de la page).</summary>
    ValueTask<IAsyncDisposable> EcouterAsync(Func<Task> onRelache);
}

/// <summary>
/// Adaptateur JS interop du port : délègue au module <c>window.pdgPointeur</c> défini inline dans index.html.
/// <c>attacher</c> pose un <c>document.addEventListener('pointerup', …)</c> rappelant .NET ; <c>detacher</c>
/// retire l'écouteur. Adaptateur de bord : jamais doublé côté test (on double le port).
/// </summary>
public sealed class EcouteurRelachementPointeurJs : IEcouteurRelachementPointeur
{
    private readonly IJSRuntime _js;

    public EcouteurRelachementPointeurJs(IJSRuntime js) => _js = js;

    public async ValueTask<IAsyncDisposable> EcouterAsync(Func<Task> onRelache)
    {
        var pont = new PontRelachement(onRelache);
        var reference = DotNetObjectReference.Create(pont);
        var id = await _js.InvokeAsync<int>("pdgPointeur.attacher", reference);
        return new Abonnement(_js, reference, id);
    }

    /// <summary>Pont .NET rappelé depuis le listener document JS : chaque <c>pointerup</c> invoque le callback fourni.</summary>
    private sealed class PontRelachement
    {
        private readonly Func<Task> _onRelache;

        public PontRelachement(Func<Task> onRelache) => _onRelache = onRelache;

        [JSInvokable]
        public Task Declencher() => _onRelache();
    }

    private sealed class Abonnement : IAsyncDisposable
    {
        private readonly IJSRuntime _js;
        private readonly DotNetObjectReference<PontRelachement> _reference;
        private readonly int _id;

        public Abonnement(IJSRuntime js, DotNetObjectReference<PontRelachement> reference, int id)
        {
            _js = js;
            _reference = reference;
            _id = id;
        }

        public async ValueTask DisposeAsync()
        {
            try { await _js.InvokeVoidAsync("pdgPointeur.detacher", _id); }
            catch { /* runtime JS déjà tombé (navigation) : rien à détacher. */ }
            _reference.Dispose();
        }
    }
}
