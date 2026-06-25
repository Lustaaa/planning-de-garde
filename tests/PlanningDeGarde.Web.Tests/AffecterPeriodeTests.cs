using System.Net;
using System.Net.Http;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Web.Components.Pages;
using PlanningDeGarde.Web.State;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Tests de composant (bUnit) du parcours « affecter une période » après câblage au canal
/// d'écriture. La vue peuple son sélecteur de responsable depuis le foyer puis <b>émet sa
/// commande via le canal HTTP</b> <c>/api/canal/affecter-periode</c> (PAS un handler en DI direct).
/// On stub le transport (<see cref="FakeCanalHttpHandler"/>) ; le bout en bout du canal est couvert
/// par <see cref="AffecterPeriodeCanalTests"/>.
/// </summary>
public sealed class AffecterPeriodeTests : TestContext
{
    private FakeCanalHttpHandler Cabler(
        HttpStatusCode statut = HttpStatusCode.OK,
        string corpsReponse = "",
        SessionPlanning? session = null)
    {
        var canal = new FakeCanalHttpHandler(statut, corpsReponse);
        Services.AddSingleton(new HttpClient(canal) { BaseAddress = new System.Uri("http://localhost/") });
        Services.AddSingleton(session ?? new SessionPlanning());
        return canal;
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

    // La vue émet la commande d'affectation via le canal HTTP avec les valeurs métier saisies.
    [Fact]
    public void Should_Emettre_via_le_canal_l_affectation_Parent_A_du_14_07_au_21_07_When_un_parent_choisit_Parent_A_et_valide()
    {
        var canal = Cabler();
        var page = RenderComponent<AffecterPeriode>();

        page.Find("select.form-select").Change("Parent A");
        page.Find("form").Submit();

        var requete = Assert.Single(canal.RequetesRecues);
        Assert.Equal(HttpMethod.Post, requete.Method);
        Assert.Equal("/api/canal/affecter-periode", requete.RequestUri!.AbsolutePath);

        var corps = Assert.Single(canal.CorpsRecus);
        Assert.Contains("Parent A", corps);
        Assert.Contains("2025-07-14", corps);
        Assert.Contains("2025-07-21", corps);
        Assert.Empty(page.FindAll("[data-testid='motif-echec']"));
    }

    // Le canal refuse (responsable manquant) -> la vue affiche le motif renvoyé par le canal.
    [Fact]
    public void Should_Afficher_le_motif_renvoye_par_le_canal_When_le_canal_refuse_l_affectation()
    {
        Cabler(HttpStatusCode.BadRequest, "un responsable est requis pour affecter une période");
        var page = RenderComponent<AffecterPeriode>();

        page.Find("form").Submit();

        var motif = page.Find("[data-testid='motif-echec']");
        Assert.Contains("responsable", motif.TextContent, System.StringComparison.OrdinalIgnoreCase);
    }
}
