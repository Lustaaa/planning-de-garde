using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Web.Components.Pages;
using PlanningDeGarde.Web.State;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Tests de composant (bUnit) du parcours « poser un slot ». L'UI appelle le use case et
/// rend son Result ; on ne double que les ports (persistance Infra + notificateur fake).
/// </summary>
public sealed class PoserSlotTests : TestContext
{
    private FakeNotificateurPlanning Cabler()
    {
        var slots = new InMemorySlotRepository();
        var notif = new FakeNotificateurPlanning();
        Services.AddSingleton<ISlotRepository>(slots);
        Services.AddSingleton<ILieuRepository, FoyerLieuRepository>();
        Services.AddSingleton<INotificateurPlanning>(notif);
        Services.AddSingleton(new PoserSlotHandler(slots, new FoyerLieuRepository(), notif));
        Services.AddSingleton(new SessionPlanning());
        return notif;
    }

    [Fact]
    public void Un_parent_pose_un_slot_valide_le_use_case_est_appele_et_notifie()
    {
        var notif = Cabler();
        var page = RenderComponent<PoserSlot>();

        page.Find("select.form-select").Change("école");
        page.Find("form").Submit();

        // Slot valide -> navigation vers le planning (pas de motif d'échec affiché) + notification émise.
        Assert.Empty(page.FindAll("[data-testid='motif-echec']"));
        Assert.Equal(1, notif.Notifications);
    }

    // Caractérisation (early green anticipé) du câblage déjà présent : poser un slot sur un lieu
    // du foyer n'affiche aucun motif d'échec et notifie le planning une fois. Filet de non-régression.
    [Fact]
    public void Should_Ne_pas_afficher_de_message_d_echec_et_notifier_le_planning_When_un_parent_pose_un_slot_a_un_lieu_du_foyer()
    {
        var notif = Cabler();
        var page = RenderComponent<PoserSlot>();

        page.Find("select.form-select").Change("école");
        page.Find("form").Submit();

        Assert.Empty(page.FindAll("[data-testid='motif-echec']"));
        Assert.Equal(1, notif.Notifications);
    }

    // Driver d'acceptation : choisir « école » dans le sélecteur peuplé depuis le foyer puis valider
    // enregistre dans le dépôt partagé le slot de Léa avec les valeurs métier concrètes (lieu + bornes).
    [Fact]
    public void Should_Enregistrer_le_slot_de_Lea_a_l_ecole_le_15_07_de_08h30_a_16h30_When_un_parent_choisit_le_lieu_ecole_du_sapeur_de_lieux_et_valide()
    {
        var slots = new InMemorySlotRepository();
        var notif = new FakeNotificateurPlanning();
        var lieux = new FoyerLieuRepository();
        Services.AddSingleton<ISlotRepository>(slots);
        Services.AddSingleton<ILieuRepository>(lieux);
        Services.AddSingleton<INotificateurPlanning>(notif);
        Services.AddSingleton(new PoserSlotHandler(slots, lieux, notif));
        Services.AddSingleton(new SessionPlanning());

        var page = RenderComponent<PoserSlot>();
        page.Find("select.form-select").Change("école");
        page.Find("form").Submit();

        var slot = Assert.Single(slots.AllSnapshots());
        Assert.Equal("Léa", slot.EnfantId);
        Assert.Equal("école", slot.LieuId);
        Assert.Equal(new System.DateTime(2025, 7, 15, 8, 30, 0), slot.Debut);
        Assert.Equal(new System.DateTime(2025, 7, 15, 16, 30, 0), slot.Fin);
    }

    [Fact]
    public void Un_lieu_inexistant_affiche_le_motif_du_result_sans_logique_dupliquee()
    {
        var slots = new InMemorySlotRepository();
        var notif = new FakeNotificateurPlanning();
        var lieux = new FoyerLieuRepository();
        Services.AddSingleton<ISlotRepository>(slots);
        Services.AddSingleton<INotificateurPlanning>(notif);
        Services.AddSingleton(new PoserSlotHandler(slots, lieux, notif));
        var session = new SessionPlanning();
        Services.AddSingleton(session);

        var page = RenderComponent<PoserSlot>();
        // Aucun lieu choisi -> LieuId vide -> refus par le use case (lieu inexistant).
        page.Find("form").Submit();

        var motif = page.Find("[data-testid='motif-echec']");
        Assert.Contains("lieu", motif.TextContent, System.StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, notif.Notifications);
    }

    [Fact]
    public void Un_invite_ne_voit_pas_le_formulaire_de_pose()
    {
        var slots = new InMemorySlotRepository();
        var notif = new FakeNotificateurPlanning();
        Services.AddSingleton<ISlotRepository>(slots);
        Services.AddSingleton(new PoserSlotHandler(slots, new FoyerLieuRepository(), notif));
        Services.AddSingleton(new SessionPlanning { Role = RoleAuteur.Invite });

        var page = RenderComponent<PoserSlot>();

        Assert.Empty(page.FindAll("form"));
        Assert.Contains("consultation seule", page.Markup, System.StringComparison.OrdinalIgnoreCase);
    }
}
