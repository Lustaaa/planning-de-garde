using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Web.Components.Pages;
using PlanningDeGarde.Web.State;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Tests de composant (bUnit) de la vue centrale. Couvrent l'édition d'une période
/// (ModifierPeriodeHandler, Sc.10), l'avertissement de chevauchement (JourneeEnfantQuery)
/// et l'affichage du responsable actuel (ResponsabiliteQuery). On ne double que les ports.
/// </summary>
public sealed class PlanningPartageTests : TestContext
{
    private InMemoryPeriodeRepository CablerAvecPeriode(PeriodeSnapshot periode, SessionPlanning? session = null)
    {
        var slots = new InMemorySlotRepository();
        var periodes = new InMemoryPeriodeRepository();
        var transferts = new InMemoryTransfertRepository();
        var enregistree = PeriodeDeGarde.Affecter(periode.ResponsableId, periode.Debut, periode.Fin);
        periodes.Enregistrer(enregistree.Valeur!);

        Services.AddSingleton<ISlotRepository>(slots);
        Services.AddSingleton<IPeriodeRepository>(periodes);
        Services.AddSingleton<ITransfertRepository>(transferts);
        Services.AddSingleton(new JourneeEnfantQuery(slots));
        Services.AddSingleton(new ResponsabiliteQuery(periodes));
        Services.AddSingleton(new DeplacerSlotHandler(slots));
        Services.AddSingleton(new ModifierPeriodeHandler(periodes));
        Services.AddSingleton(session ?? new SessionPlanning());
        return periodes;
    }

    // Driver (Sc.4 #1) : cliquer « Modifier » ouvre le formulaire inline pré-rempli avec le
    // responsable et les bornes actuels (base observée = jeton optimiste).
    [Fact]
    public void Should_Pre_remplir_le_formulaire_inline_avec_le_responsable_et_les_bornes_actuels_When_un_parent_clique_Modifier_sur_une_periode()
    {
        var periode = new PeriodeSnapshot("Parent A", new DateTime(2025, 7, 14), new DateTime(2025, 7, 21));
        CablerAvecPeriode(periode);

        var page = RenderComponent<PlanningPartage>();
        page.Find("button.btn-outline-primary").Click(); // « Modifier »

        var selectResponsable = page.Find("li .col-auto select.form-select");
        Assert.Equal("Parent A", selectResponsable.GetAttribute("value"));

        var inputsDate = page.FindAll("li .col-auto input[type=date]");
        Assert.Equal("2025-07-14", inputsDate[0].GetAttribute("value"));
        Assert.Equal("2025-07-21", inputsDate[1].GetAttribute("value"));
    }

    [Fact]
    public void Modifier_une_periode_sur_un_etat_a_jour_remplace_le_responsable()
    {
        var periode = new PeriodeSnapshot("Parent A", new DateTime(2025, 7, 14), new DateTime(2025, 7, 21));
        var periodes = CablerAvecPeriode(periode);

        var page = RenderComponent<PlanningPartage>();
        page.Find("button.btn-outline-primary").Click(); // « Modifier »
        page.Find("li .col-auto select.form-select").Change("Parent B");
        page.Find("button.btn-success").Click();          // « Enregistrer »

        Assert.Empty(page.FindAll("[data-testid='motif-edition-periode']"));
        Assert.Equal("Parent B", periodes.AllSnapshots().Single().ResponsableId);
    }

    [Fact]
    public void Modifier_depuis_un_etat_perime_est_rejete_et_invite_a_recharger()
    {
        var periode = new PeriodeSnapshot("Parent A", new DateTime(2025, 7, 14), new DateTime(2025, 7, 21));
        var periodes = CablerAvecPeriode(periode);

        var page = RenderComponent<PlanningPartage>();
        page.Find("button.btn-outline-primary").Click(); // ouvre l'édition sur l'état affiché

        // Pendant ce temps, un autre parent a devancé l'état (remplacement du responsable).
        periodes.Modifier(periode, periode with { ResponsableId = "Parent B" });

        page.Find("button.btn-success").Click();           // enregistre depuis l'affichage périmé

        var motif = page.Find("[data-testid='motif-edition-periode']");
        Assert.Contains("périmé", motif.TextContent, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Recharger", motif.TextContent, StringComparison.OrdinalIgnoreCase);
        // L'état devancé n'est pas écrasé par la modification périmée.
        Assert.Equal("Parent B", periodes.AllSnapshots().Single().ResponsableId);
    }

    [Fact]
    public void Deux_slots_qui_se_recouvrent_affichent_l_avertissement_de_chevauchement()
    {
        var slots = new InMemorySlotRepository();
        var periodes = new InMemoryPeriodeRepository();
        var transferts = new InMemoryTransfertRepository();
        slots.Enregistrer(SlotDeLocalisation.Poser("Léa", "école",
            new DateTime(2025, 7, 15, 8, 30, 0), new DateTime(2025, 7, 15, 16, 30, 0)).Valeur!);
        slots.Enregistrer(SlotDeLocalisation.Poser("Léa", "nounou",
            new DateTime(2025, 7, 15, 16, 0, 0), new DateTime(2025, 7, 15, 18, 0, 0)).Valeur!);

        Services.AddSingleton<ISlotRepository>(slots);
        Services.AddSingleton<IPeriodeRepository>(periodes);
        Services.AddSingleton<ITransfertRepository>(transferts);
        Services.AddSingleton(new JourneeEnfantQuery(slots));
        Services.AddSingleton(new ResponsabiliteQuery(periodes));
        Services.AddSingleton(new DeplacerSlotHandler(slots));
        Services.AddSingleton(new ModifierPeriodeHandler(periodes));
        Services.AddSingleton(new SessionPlanning());

        var page = RenderComponent<PlanningPartage>();

        var avertissement = page.Find("[data-testid='avertissement-chevauchement']");
        Assert.Contains("chevauchement", avertissement.TextContent, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Le_responsable_actuel_est_affiche_depuis_le_read_model()
    {
        var maintenant = DateTime.Now;
        var periode = new PeriodeSnapshot("Parent A", maintenant.AddDays(-1), maintenant.AddDays(1));
        CablerAvecPeriode(periode);

        var page = RenderComponent<PlanningPartage>();

        var bloc = page.Find("[data-testid='responsable-actuel']");
        Assert.Contains("Parent A", bloc.TextContent);
    }
}
