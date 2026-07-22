using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace PlanningDeGarde.Web;

/// <summary>
/// Port d'ergonomie de surface (finition PO) — écoute la touche <b>Échap</b> au niveau <b>document</b>
/// (jamais un div qui devrait être focus : un <c>@onkeydown</c> sur un élément non focus ne reçoit rien en
/// navigateur réel — c'est le piège du vert-qui-ment). Une modal ouverte <see cref="EcouterAsync"/> et
/// <b>dispose</b> l'abonnement à sa fermeture pour détacher l'écouteur (aucune fuite, aucun double abonnement).
/// Aucune règle métier, aucun observable de domaine : Échap = « Annuler » (ferme sans muter).
/// </summary>
public interface IEcouteurEchapModal
{
    /// <summary>Attache un écouteur <c>document</c> de la touche Échap : <paramref name="onEchap"/> est
    /// rappelé à chaque appui. L'<see cref="IAsyncDisposable"/> retourné détache l'écouteur (à disposer à la
    /// fermeture de la modal).</summary>
    ValueTask<IAsyncDisposable> EcouterAsync(Func<Task> onEchap);
}

/// <summary>
/// Adaptateur JS interop du port : délègue au module <c>window.pdgModal</c> défini inline dans index.html.
/// <c>attacher</c> pose un <c>document.addEventListener('keydown', …)</c> filtrant Échap et rappelant.NET ;
/// <c>detacher</c> retire l'écouteur. Adaptateur de bord : jamais doublé côté test (on double le port).
/// </summary>
public sealed class EcouteurEchapModalJs : IEcouteurEchapModal
{
    private readonly IJSRuntime _js;

    public EcouteurEchapModalJs(IJSRuntime js) => _js = js;

    public async ValueTask<IAsyncDisposable> EcouterAsync(Func<Task> onEchap)
    {
        var pont = new PontEchap(onEchap);
        var reference = DotNetObjectReference.Create(pont);
        // Le module JS renvoie un identifiant d'abonnement opaque, requis pour détacher LE MÊME écouteur.
        var id = await _js.InvokeAsync<int>("pdgModal.attacher", reference);
        return new Abonnement(_js, reference, id);
    }

    /// <summary>Pont.NET rappelé depuis le listener document JS : chaque appui Échap invoque le callback fourni.</summary>
    private sealed class PontEchap
    {
        private readonly Func<Task> _onEchap;

        public PontEchap(Func<Task> onEchap) => _onEchap = onEchap;

        [JSInvokable]
        public Task Declencher() => _onEchap();
    }

    private sealed class Abonnement : IAsyncDisposable
    {
        private readonly IJSRuntime _js;
        private readonly DotNetObjectReference<PontEchap> _reference;
        private readonly int _id;

        public Abonnement(IJSRuntime js, DotNetObjectReference<PontEchap> reference, int id)
        {
            _js = js;
            _reference = reference;
            _id = id;
        }

        public async ValueTask DisposeAsync()
        {
            try { await _js.InvokeVoidAsync("pdgModal.detacher", _id); }
            catch { /* runtime JS déjà tombé (navigation) : rien à détacher. */ }
            _reference.Dispose();
        }
    }
}
