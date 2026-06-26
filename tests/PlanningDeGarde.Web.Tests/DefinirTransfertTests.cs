using System.Net;
using System.Net.Http;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Web.Components.Pages;
using PlanningDeGarde.Web.State;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Tests de composant (bUnit) du parcours « définir un transfert » du front WASM. La vue n'appelle
/// plus le handler en DI directe (impossible côté navigateur) : elle <b>émet sa commande via le
/// canal HTTP</b> <c>/api/canal/definir-transfert</c> de l'API distante. On stub le transport
/// (<see cref="FakeCanalHttpHandler"/>) et on vérifie la requête sortante + la réaction de la vue à
/// l'accusé. Le bout en bout du canal (handler → store réel) est couvert côté hôte d'API
/// (<c>DefinirTransfertCanalApiTests</c>).
/// </summary>
public sealed class DefinirTransfertTests : TestContext
{
    private FakeCanalHttpHandler Cabler(
        HttpStatusCode statut = HttpStatusCode.OK,
        string corpsReponse = "",
        SessionPlanning? session = null)
    {
        var canal = new FakeCanalHttpHandler(statut, corpsReponse);
        Services.AddSingleton(new HttpClient(canal) { BaseAddress = new System.Uri("http://localhost/") });
        Services.AddSingleton(session ?? new SessionPlanning());
        // La vue (passée en code-behind) pré-remplit sa date depuis le port d'horloge : on le fige
        // au 26/06/2026 pour le déterminisme.
        Services.AddSingleton<IDateTimeProvider>(new DateTimeProviderFige(new System.DateTime(2026, 6, 26)));
        return canal;
    }

    // Renseigne dépose / récupère / lieu (les 3 sélecteurs dans l'ordre du DOM). Chaque Change
    // déclenche un re-render (InputSelect @bind) : on re-issue FindAll avant chaque interaction.
    private static void RenseignerSelecteurs(IRenderedComponent<DefinirTransfert> page,
        string depose, string recupere, string lieu)
    {
        page.FindAll("select.form-select")[0].Change(depose);
        page.FindAll("select.form-select")[1].Change(recupere);
        page.FindAll("select.form-select")[2].Change(lieu);
    }

    // La vue émet la commande de transfert via le canal HTTP avec les valeurs métier saisies.
    [Fact]
    public void Should_Emettre_via_le_canal_le_transfert_Parent_A_Parent_B_ecole_a_08h30_When_un_parent_renseigne_la_recuperation_le_lieu_et_l_heure_et_valide()
    {
        var canal = Cabler();
        var page = RenderComponent<DefinirTransfert>();

        RenseignerSelecteurs(page, "Parent A", "Parent B", "école");
        page.Find("input[type=time]").Change("08:30");
        page.Find("form").Submit();

        var requete = Assert.Single(canal.RequetesRecues);
        Assert.Equal(HttpMethod.Post, requete.Method);
        Assert.Equal("/api/canal/definir-transfert", requete.RequestUri!.AbsolutePath);

        var corps = Assert.Single(canal.CorpsRecus);
        Assert.Contains("Parent A", corps);
        Assert.Contains("Parent B", corps);
        Assert.Contains("08:30:00", corps);
        Assert.Empty(page.FindAll("[data-testid='motif-echec']"));
    }

    // Le canal refuse (motif métier propagé par le Result du use case) -> la vue affiche le motif.
    [Fact]
    public void Should_Afficher_le_motif_renvoye_par_le_canal_When_le_canal_refuse_le_transfert_incomplet()
    {
        Cabler(HttpStatusCode.BadRequest, "un parent de récupération est requis pour définir un transfert");
        var page = RenderComponent<DefinirTransfert>();

        page.Find("form").Submit();

        var motif = page.Find("[data-testid='motif-echec']");
        Assert.Contains("récupération", motif.TextContent, System.StringComparison.OrdinalIgnoreCase);
    }

    // Un invité ne voit pas le formulaire (consultation seule).
    [Fact]
    public void Un_invite_ne_voit_pas_le_formulaire_de_transfert()
    {
        Cabler(session: new SessionPlanning { Role = PlanningDeGarde.Application.RoleAuteur.Invite });

        var page = RenderComponent<DefinirTransfert>();

        Assert.Empty(page.FindAll("form"));
        Assert.Contains("consultation seule", page.Markup, System.StringComparison.OrdinalIgnoreCase);
    }
}
