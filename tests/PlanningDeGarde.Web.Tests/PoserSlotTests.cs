using System.Net;
using System.Net.Http;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Web.Components.Pages;
using PlanningDeGarde.Web.State;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Tests de composant (bUnit) du parcours « poser un slot » après câblage au canal d'écriture.
/// La vue n'appelle plus le handler en DI direct : elle <b>émet sa commande via le canal HTTP</b>
/// <c>/api/canal/poser-slot</c>. On stub le transport (<see cref="FakeCanalHttpHandler"/>) et on
/// vérifie la requête sortante + la réaction de la vue à l'accusé. Le bout en bout du canal
/// (handler → store réel → projection) est couvert par <see cref="PoserSlotCanalTests"/>.
/// </summary>
public sealed class PoserSlotTests : TestContext
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

    // La vue émet sa commande de pose via le canal HTTP (PAS un handler en DI direct).
    [Fact]
    public void Should_Emettre_une_commande_de_pose_via_l_endpoint_du_canal_When_un_parent_choisit_un_lieu_et_valide()
    {
        var canal = Cabler();
        var page = RenderComponent<PoserSlot>();

        page.Find("select.form-select").Change("école");
        page.Find("form").Submit();

        var requete = Assert.Single(canal.RequetesRecues);
        Assert.Equal(HttpMethod.Post, requete.Method);
        Assert.Equal("/api/canal/poser-slot", requete.RequestUri!.AbsolutePath);
    }

    // La commande émise transporte bien les valeurs métier saisies (enfant, lieu, bornes).
    [Fact]
    public void Should_Transporter_le_slot_de_Lea_a_l_ecole_le_15_07_de_08h30_a_16h30_When_un_parent_choisit_le_lieu_ecole_et_valide()
    {
        var canal = Cabler();
        var page = RenderComponent<PoserSlot>();

        page.Find("select.form-select").Change("école");
        page.Find("form").Submit();

        var corps = Assert.Single(canal.CorpsRecus);
        // Le corps JSON échappe les accents (Léa -> Léa, école -> école) : on observe
        // les bornes (ASCII) et les champs, l'identité accentuée étant transportée telle quelle.
        Assert.Contains("enfantId", corps);
        Assert.Contains("lieuId", corps);
        Assert.Contains("2025-07-15T08:30:00", corps);
        Assert.Contains("2025-07-15T16:30:00", corps);
    }

    // Le canal accuse un succès -> la vue n'affiche aucun motif d'échec.
    [Fact]
    public void Should_Ne_pas_afficher_de_motif_d_echec_When_le_canal_acquitte_la_pose_en_succes()
    {
        Cabler(HttpStatusCode.OK);
        var page = RenderComponent<PoserSlot>();

        page.Find("select.form-select").Change("école");
        page.Find("form").Submit();

        Assert.Empty(page.FindAll("[data-testid='motif-echec']"));
    }

    // Le canal refuse (motif métier propagé) -> la vue affiche le motif renvoyé par le canal.
    [Fact]
    public void Should_Afficher_le_motif_renvoye_par_le_canal_When_le_canal_refuse_la_pose()
    {
        Cabler(HttpStatusCode.BadRequest, "le lieu visé n'existe pas dans les lieux du foyer");
        var page = RenderComponent<PoserSlot>();

        page.Find("form").Submit();

        var motif = page.Find("[data-testid='motif-echec']");
        Assert.Contains("lieu", motif.TextContent, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Un_invite_ne_voit_pas_le_formulaire_de_pose()
    {
        Cabler(session: new SessionPlanning { Role = RoleAuteur.Invite });

        var page = RenderComponent<PoserSlot>();

        Assert.Empty(page.FindAll("form"));
        Assert.Contains("consultation seule", page.Markup, System.StringComparison.OrdinalIgnoreCase);
    }
}
