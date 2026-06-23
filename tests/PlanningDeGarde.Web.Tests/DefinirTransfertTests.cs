using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Web.Components.Pages;
using PlanningDeGarde.Web.State;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Tests de composant (bUnit) du parcours « définir un transfert ». Le refus d'un transfert
/// incomplet vient du Result du use case, jamais d'une règle dupliquée dans l'UI.
/// </summary>
public sealed class DefinirTransfertTests : TestContext
{
    [Fact]
    public void Un_transfert_incomplet_affiche_le_motif_du_result()
    {
        var transferts = new InMemoryTransfertRepository();
        Services.AddSingleton<ITransfertRepository>(transferts);
        Services.AddSingleton(new DefinirTransfertHandler(transferts));
        Services.AddSingleton(new SessionPlanning());

        var page = RenderComponent<DefinirTransfert>();
        // Aucun champ rempli (dépose/récupère/lieu vides) -> refus par le use case.
        page.Find("form").Submit();

        var motif = page.Find("[data-testid='motif-echec']");
        Assert.Contains("récupération", motif.TextContent, System.StringComparison.OrdinalIgnoreCase);
        Assert.Empty(transferts.AllSnapshots());
    }
}
