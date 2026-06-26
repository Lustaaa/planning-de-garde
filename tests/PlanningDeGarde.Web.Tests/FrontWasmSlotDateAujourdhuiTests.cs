extern alias api;
using System.Net.Http;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Web.Components.Pages;
using PlanningDeGarde.Web.State;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Acceptation de NIVEAU RUNTIME du Sc.1 (🖥️ scénario IHM, <c>@nominal</c>) — le défaut vit dans le
/// <b>formulaire</b> <see cref="PoserSlot"/> : sa date était <b>figée en 2025</b>, si bien qu'une pose
/// validée <b>sans corriger la date</b> tombait hors de la fenêtre affichée et <b>semblait disparaître</b>.
/// La correction est l'injection du port <see cref="IDateTimeProvider"/> pré-remplissant la date sur
/// <c>Today</c> (heure conservée).
///
/// On rend la <b>vue réelle</b> (bUnit ne stube pas la logique du composant : il rend le vrai
/// <see cref="PoserSlot"/> avec sa DI réelle, dont un <see cref="IDateTimeProvider"/> doublé fixé au
/// <b>26/06/2026</b>) et on lui câble un <b>vrai transport HTTP</b> vers une <b>API distante réelle</b>
/// (<see cref="ApiDistanteFactory"/>, store réel singleton, projection réelle <see cref="GrilleAgendaQuery"/>).
/// Le parent valide <b>sans toucher aucune date pré-remplie</b> ; l'observable est le slot réellement
/// enregistré dans le store de l'API distante, relu par sa projection à la semaine du lundi 22/06/2026.
///
/// Anti « vert qui ment » : si le formulaire porte encore la date figée 2025 (ou si le port n'est pas
/// injecté), la pose tombe hors fenêtre → la case du 26/06 reste vide → rouge. Un bUnit à doublure de
/// date stub ne verrait pas ce câblage runtime « date par défaut = aujourd'hui ».
/// </summary>
public sealed class FrontWasmSlotDateAujourdhuiTests : TestContext
{
    // Date de référence injectée (IDateTimeProvider.Today) : 26 juin 2026 (vendredi), à 09:00 pour
    // distinguer la date du jour (seule par défaut) de l'heure du formulaire (08:30 → 16:30 conservée).
    private static readonly DateTime Aujourdhui = new(2026, 6, 26, 9, 0, 0);

    [Fact]
    public void Should_Faire_reapparaitre_le_slot_ecole_08h30_16h30_dans_la_case_du_26_06_2026_When_un_parent_pose_un_slot_via_le_front_WASM_sans_modifier_les_dates_pre_remplies()
    {
        // Given — l'hôte d'API détaché réel démarré joue l'API distante (store réel vierge).
        using var apiDistante = new ApiDistanteFactory();
        var transportVersApiDistante = apiDistante.Server.CreateHandler();
        var clientFront = new HttpClient(transportVersApiDistante)
        {
            BaseAddress = apiDistante.Server.BaseAddress,
        };

        // … et la vue réelle est câblée à ce transport distant + au port de date FIXÉ au 26/06/2026 :
        // le formulaire pré-remplit ses dates depuis IDateTimeProvider.Today (et non une date figée 2025).
        Services.AddSingleton(clientFront);
        Services.AddSingleton(new SessionPlanning());
        Services.AddSingleton<IDateTimeProvider>(new DateTimeProviderFige(Aujourdhui));

        // Aucun slot pour le 26/06/2026 avant la pose (store distant vierge — pas de seed sous « Testing »).

        var page = RenderComponent<PoserSlot>();
        page.Find("select.form-select").Change("école");

        // When — le parent valide la pose SANS modifier aucune date pré-remplie, puis revient au planning.
        page.Find("form").Submit();

        // L'émission HTTP réelle vers l'API distante est asynchrone : on attend que la vue ait
        // réellement abouti — navigation vers « planning » sur succès du canal (et aucun motif d'échec).
        // C'est la preuve que l'écriture a bien transité jusqu'à l'API distante avant qu'on relise.
        var nav = (Bunit.TestDoubles.FakeNavigationManager)
            Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>();
        page.WaitForAssertion(
            () =>
            {
                Assert.Empty(page.FindAll("[data-testid='motif-echec']"));
                Assert.EndsWith("planning", nav.Uri);
            },
            TimeSpan.FromSeconds(10));

        // Then — le slot « école » 08h30 → 16h30 est réellement enregistré dans le store de l'API
        // DISTANTE et relu par sa projection à la semaine du lundi 22/06/2026 : la case du 26/06/2026
        // (vendredi) le porte. L'écriture a réellement transité par le canal HTTP distant.
        using var scope = apiDistante.Services.CreateScope();
        var projection = scope.ServiceProvider.GetRequiredService<GrilleAgendaQuery>();
        var grille = projection.Projeter(new DateOnly(2026, 6, 22));

        var caseVendredi = grille.Jours.Single(j => j.Date == new DateOnly(2026, 6, 26));
        var slot = Assert.Single(caseVendredi.Slots, s => s.Libelle == "école");
        Assert.Equal(new TimeOnly(8, 30), slot.Debut);
        Assert.Equal(new TimeOnly(16, 30), slot.Fin);
    }
}
