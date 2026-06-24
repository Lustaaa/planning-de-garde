using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Web.Components.Pages;
using PlanningDeGarde.Web.State;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Tests de composant (bUnit) du parcours « affecter une période ». L'UI peuple son sélecteur
/// de responsable depuis le foyer, appelle le use case et rend son Result ; on ne double que
/// les ports (persistance Infra). Pas de port notificateur sur ce use case.
/// </summary>
public sealed class AffecterPeriodeTests : TestContext
{
    private InMemoryPeriodeRepository Cabler(SessionPlanning? session = null)
    {
        var periodes = new InMemoryPeriodeRepository();
        var responsables = new FoyerResponsableRepository();
        Services.AddSingleton<IPeriodeRepository>(periodes);
        Services.AddSingleton<IResponsableRepository>(responsables);
        Services.AddSingleton(new AffecterPeriodeHandler(periodes, responsables));
        Services.AddSingleton(session ?? new SessionPlanning());
        return periodes;
    }

    // Driver (peuplement) : le sélecteur de responsable propose bien les responsables du foyer.
    [Fact]
    public void Should_Proposer_les_responsables_du_foyer_Parent_A_et_Parent_B_When_un_parent_ouvre_la_dialog_d_affectation()
    {
        Cabler();
        var page = RenderComponent<AffecterPeriode>();

        var options = page.FindAll("select.form-select option");
        var valeurs = options.Select(o => o.GetAttribute("value")).ToList();

        Assert.Contains("Parent A", valeurs);
        Assert.Contains("Parent B", valeurs);
    }

    // Driver / acceptation : choisir « Parent A » et valider du 14-07 au 21-07 enregistre la période
    // dans le dépôt partagé avec les valeurs métier concrètes, sans message d'échec.
    [Fact]
    public void Should_Enregistrer_la_periode_Parent_A_responsable_du_14_07_au_21_07_When_un_parent_choisit_Parent_A_et_valide_du_14_07_au_21_07()
    {
        var periodes = Cabler();
        var page = RenderComponent<AffecterPeriode>();

        page.Find("select.form-select").Change("Parent A");
        page.Find("form").Submit();

        Assert.Empty(page.FindAll("[data-testid='motif-echec']"));
        var periode = Assert.Single(periodes.AllSnapshots());
        Assert.Equal("Parent A", periode.ResponsableId);
        Assert.Equal(new System.DateTime(2025, 7, 14), periode.Debut);
        Assert.Equal(new System.DateTime(2025, 7, 21), periode.Fin);
    }
}
