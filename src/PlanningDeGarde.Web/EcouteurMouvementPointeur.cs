using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace PlanningDeGarde.Web;

/// <summary>
/// Port de geste de surface (sans lui, le drag ne surlignait AUCUNE case intermédiaire
/// en navigateur RÉEL). Écoute le MOUVEMENT du pointeur au niveau <b>document</b> (<c>pointermove</c>,
/// bouton appuyé) : à chaque déplacement, l'adaptateur résout la case sous le curseur
/// (<c>document.elementFromPoint</c> → plus proche <c>[data-testid="jour-case"]</c> → son <c>data-date</c>)
/// et remonte l'identifiant de jour au composant, qui met à jour le CURSEUR de sélection. C'est la voie FIABLE
/// du drag continu : indépendante des <c>@onpointerover</c> posés par case (fragiles / manqués pendant un
/// glisser, et court-circuités par toute capture de pointeur). Aucune règle métier, aucun observable de domaine.
/// </summary>
public interface IEcouteurMouvementPointeur
{
    /// <summary>Attache un écouteur <c>document</c> de <c>pointermove</c> (bouton appuyé) : <paramref name="onSurvolCase"/>
    /// est rappelé avec le <c>data-date</c> (« yyyy-MM-dd ») de la case sous le curseur, ou <c>null</c> hors d'une case.
    /// L'<see cref="IAsyncDisposable"/> retourné détache l'écouteur (à disposer à la fermeture de la page).</summary>
    ValueTask<IAsyncDisposable> EcouterAsync(Func<string?, Task> onSurvolCase);
}

/// <summary>
/// Adaptateur JS interop du port : délègue au module <c>window.pdgPointeur</c> (défini inline dans index.html).
/// <c>attacherMouvement</c> pose un <c>document.addEventListener('pointermove', …)</c> qui, bouton primaire appuyé,
/// résout la case sous le curseur par <c>elementFromPoint</c> et rappelle.NET ; <c>detacherMouvement</c> le retire.
/// Adaptateur de bord : jamais doublé côté test (on double le port).
/// </summary>
public sealed class EcouteurMouvementPointeurJs : IEcouteurMouvementPointeur
{
    private readonly IJSRuntime _js;

    public EcouteurMouvementPointeurJs(IJSRuntime js) => _js = js;

    public async ValueTask<IAsyncDisposable> EcouterAsync(Func<string?, Task> onSurvolCase)
    {
        var pont = new PontSurvol(onSurvolCase);
        var reference = DotNetObjectReference.Create(pont);
        var id = await _js.InvokeAsync<int>("pdgPointeur.attacherMouvement", reference);
        return new Abonnement(_js, reference, id);
    }

    /// <summary>Pont.NET rappelé depuis le listener document JS : chaque <c>pointermove</c> bouton-appuyé
    /// invoque le callback avec le <c>data-date</c> résolu (ou <c>null</c> hors case).</summary>
    private sealed class PontSurvol
    {
        private readonly Func<string?, Task> _onSurvolCase;

        public PontSurvol(Func<string?, Task> onSurvolCase) => _onSurvolCase = onSurvolCase;

        [JSInvokable]
        public Task Survoler(string? date) => _onSurvolCase(date);
    }

    private sealed class Abonnement : IAsyncDisposable
    {
        private readonly IJSRuntime _js;
        private readonly DotNetObjectReference<PontSurvol> _reference;
        private readonly int _id;

        public Abonnement(IJSRuntime js, DotNetObjectReference<PontSurvol> reference, int id)
        {
            _js = js;
            _reference = reference;
            _id = id;
        }

        public async ValueTask DisposeAsync()
        {
            try { await _js.InvokeVoidAsync("pdgPointeur.detacherMouvement", _id); }
            catch { /* runtime JS déjà tombé (navigation) : rien à détacher. */ }
            _reference.Dispose();
        }
    }
}
