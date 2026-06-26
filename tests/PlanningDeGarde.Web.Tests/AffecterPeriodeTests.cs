using System.Net;
using System.Net.Http;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
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
        // Le formulaire pré-remplit ses dates depuis le port d'horloge (jamais des dates figées) : on le
        // fige au 26/06/2026 (semaine du lundi 22/06 → dimanche 28/06) pour le déterminisme du corps émis.
        Services.AddSingleton<IDateTimeProvider>(new DateTimeProviderFige(new System.DateTime(2026, 6, 26)));
        return canal;
    }

    // Driver (peuplement) : le sélecteur affiche les LIBELLÉS mais bind les IDENTIFIANTS STABLES
    // (cadrage (B), Sc.6) — l'option « Parent A » a pour value « parent-a », atteignable par la palette.
    [Fact]
    public void Should_Proposer_les_responsables_du_foyer_affichant_le_libelle_mais_bindant_l_id_stable_When_un_parent_ouvre_la_dialog_d_affectation()
    {
        Cabler();
        var page = RenderComponent<AffecterPeriode>();

        var options = page.FindAll("select.form-select option");
        var valeurs = options.Select(o => o.GetAttribute("value")).ToList();
        var libelles = options.Select(o => o.TextContent.Trim()).ToList();

        // Les value bindées sont les identifiants stables ...
        Assert.Contains("parent-a", valeurs);
        Assert.Contains("parent-b", valeurs);
        // ... et les textes affichés restent les libellés lisibles.
        Assert.Contains("Parent A", libelles);
        Assert.Contains("Parent B", libelles);
    }

    // La vue émet la commande d'affectation via le canal HTTP avec les valeurs métier saisies. Les
    // dates sont pré-remplies « aujourd'hui » depuis le port d'horloge figé (26/06/2026) : la semaine
    // en cours (lundi 22/06 → dimanche 28/06), pas un intervalle 2025.
    [Fact]
    public void Should_Emettre_via_le_canal_l_affectation_de_parent_a_du_22_06_au_28_06_2026_When_un_parent_choisit_Parent_A_et_valide()
    {
        var canal = Cabler();
        var page = RenderComponent<AffecterPeriode>();

        // L'utilisateur choisit « Parent A » : la value bindée est l'id stable « parent-a ».
        page.Find("select.form-select").Change("parent-a");
        page.Find("form").Submit();

        var requete = Assert.Single(canal.RequetesRecues);
        Assert.Equal(HttpMethod.Post, requete.Method);
        Assert.Equal("/api/canal/affecter-periode", requete.RequestUri!.AbsolutePath);

        var corps = Assert.Single(canal.CorpsRecus);
        Assert.Contains("parent-a", corps);
        Assert.Contains("2026-06-22", corps);
        Assert.Contains("2026-06-28", corps);
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
