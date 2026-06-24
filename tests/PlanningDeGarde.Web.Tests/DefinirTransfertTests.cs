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
    private InMemoryTransfertRepository Cabler(SessionPlanning? session = null)
    {
        var transferts = new InMemoryTransfertRepository();
        Services.AddSingleton<ITransfertRepository>(transferts);
        Services.AddSingleton(new DefinirTransfertHandler(transferts));
        Services.AddSingleton(session ?? new SessionPlanning());
        return transferts;
    }

    // Renseigne dépose / récupère / lieu (les 3 sélecteurs dans l'ordre du DOM). Chaque Change
    // déclenche un re-render (InputSelect @bind) qui invalide les handlers : on re-issue FindAll
    // avant chaque interaction, comme recommandé par bUnit.
    private static void RenseignerSelecteurs(IRenderedComponent<DefinirTransfert> page,
        string depose, string recupere, string lieu)
    {
        page.FindAll("select.form-select")[0].Change(depose);
        page.FindAll("select.form-select")[1].Change(recupere);
        page.FindAll("select.form-select")[2].Change(lieu);
    }

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

    // Driver : renseigner récupération + lieu + heure (≠ zéro) atteint la branche succès du use case
    // (pas de retombée sur « Transfert incomplet ») → aucun motif d'échec affiché.
    [Fact]
    public void Should_Ne_pas_afficher_de_message_d_echec_When_un_parent_renseigne_la_recuperation_le_lieu_et_l_heure_et_valide()
    {
        Cabler();
        var page = RenderComponent<DefinirTransfert>();

        RenseignerSelecteurs(page, "Parent A", "Parent B", "école");
        page.Find("input[type=time]").Change("08:30");
        page.Find("form").Submit();

        Assert.Empty(page.FindAll("[data-testid='motif-echec']"));
    }

    // Driver / acceptation : le transfert (dépose Parent A → récupère Parent B, école, 08:30 le 21-07)
    // est enregistré dans le dépôt partagé avec les valeurs métier exactes — l'heure 08:30 est conservée.
    [Fact]
    public void Should_Enregistrer_le_transfert_depose_Parent_A_recupere_Parent_B_ecole_a_08h30_le_21_07_When_un_parent_renseigne_recuperation_lieu_et_heure_et_valide()
    {
        var transferts = Cabler();
        var page = RenderComponent<DefinirTransfert>();

        RenseignerSelecteurs(page, "Parent A", "Parent B", "école");
        page.Find("input[type=time]").Change("08:30");
        page.Find("form").Submit();

        var transfert = Assert.Single(transferts.AllSnapshots());
        Assert.Equal("Parent A", transfert.DeposeParId);
        Assert.Equal("Parent B", transfert.RecupereParId);
        Assert.Equal("école", transfert.LieuId);
        Assert.Equal(new System.TimeSpan(8, 30, 0), transfert.Heure);
        Assert.Equal(new System.DateTime(2025, 7, 21), transfert.Date);
    }
}
