using System;
using Bunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 26 (décision scrum-master, option D — assainissement racine sanctionné) — stabilise les
/// interactions des tests de NIVEAU RUNTIME bUnit face au hub SignalR réel.
///
/// <para><b>Course corrigée.</b> Ces tests câblent un hub SignalR réel qui pousse des re-renders EN FOND.
/// Une paire <c>Find</c>/<c>Click</c> (ou <c>Change</c>) exécutée hors du dispatcher du renderer peut voir
/// l'élément invalidé entre sa résolution et le déclenchement de l'événement → bUnit lève
/// <c>UnknownEventHandlerIdException</c> (« no event handler with ID … »). Flake PRÉEXISTANT, indépendant
/// du parallélisme xUnit (reproduit en exécution sérialisée), victimes rotatives, vertes en isolation.</para>
///
/// <para><b>Correctif.</b> Exécuter l'interaction sur le dispatcher du renderer bUnit, ce qui la sérialise
/// avec les re-renders du hub : la résolution de l'élément et le déclenchement sont atomiques. On ne change
/// QUE l'ordonnancement de l'interaction — <b>aucune assertion métier n'est affaiblie ni supprimée</b>.</para>
/// </summary>
internal static class InteractionDispatcherExtensions
{
    /// <summary>
    /// Exécute <paramref name="interaction"/> (typiquement <c>cut.Find(sel).Click()</c> /
    /// <c>… .Change(v)</c> en un seul trait, résolution incluse) sur le dispatcher du renderer bUnit,
    /// sérialisée avec les re-renders poussés par le hub SignalR réel.
    /// </summary>
    public static void SurDispatcher(this Bunit.TestContext ctx, Action interaction)
        => ctx.Renderer.Dispatcher.InvokeAsync(interaction).GetAwaiter().GetResult();

    /// <summary>
    /// Variante utilisable là où <c>this</c> (le <see cref="Bunit.TestContext"/>) n'est pas accessible —
    /// typiquement une méthode d'aide <c>static</c> qui reçoit un composant rendu en paramètre. Route
    /// l'interaction sur le même dispatcher partagé du renderer, via <c>InvokeAsync</c> du fragment.
    /// </summary>
    public static void SurDispatcher<TComponent>(this Bunit.IRenderedComponent<TComponent> cut, Action interaction)
        where TComponent : Microsoft.AspNetCore.Components.IComponent
        => cut.InvokeAsync(interaction).GetAwaiter().GetResult();
}
