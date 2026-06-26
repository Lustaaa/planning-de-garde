extern alias api;
using System.Net.Http;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Web.Components.Pages;
using PlanningDeGarde.Web.State;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Acceptation de NIVEAU RUNTIME du Sc.2 (🖥️ scénario IHM, <c>@nominal</c>) — le défaut vit dans le
/// <b>formulaire</b> <see cref="AffecterPeriode"/> : ses dates étaient <b>figées en 2025</b>
/// (<c>new(2025,7,14)</c> / <c>new(2025,7,21)</c>), si bien qu'une affectation validée <b>sans corriger
/// les dates</b> tombait hors de la fenêtre affichée et la grille <b>ne se colorait pas</b>. La
/// correction réutilise le port <see cref="IDateTimeProvider"/> (déjà construit au Sc.1), appliqué
/// cette fois à <see cref="AffecterPeriode"/> : le formulaire pré-remplit ses dates depuis
/// <c>Aujourdhui</c> (un intervalle couvrant le jour).
///
/// On rend la <b>vue réelle</b> (bUnit rend le vrai <see cref="AffecterPeriode"/> avec sa DI réelle,
/// dont un <see cref="IDateTimeProvider"/> doublé fixé au <b>26/06/2026</b>) et on lui câble un
/// <b>vrai transport HTTP</b> vers une <b>API distante réelle</b> (<see cref="ApiDistanteFactory"/>,
/// store réel singleton, palette réelle, projection réelle <see cref="GrilleAgendaQuery"/>). Le parent
/// valide <b>sans toucher aucune date pré-remplie</b> ; l'observable est la case du 26/06/2026
/// <b>colorée pour la période</b> (couleur du responsable, NON neutre) dans la projection distante.
///
/// Anti « vert qui ment » : si le formulaire porte encore les dates figées 2025 (ou si le port n'est
/// pas injecté), la période tombe hors fenêtre → la case du 26/06 reste neutre (gris) → rouge. Un
/// bUnit à doublure de date stub ne verrait pas ce câblage runtime « dates par défaut = aujourd'hui ».
///
/// Couleur par identifiant stable = Sc.6 : ici l'identifiant <c>parent-a</c> (déjà connu de la palette
/// réelle = bleu) est posté tel quel pour rendre observable que la case <b>n'est plus neutre</b> ;
/// l'observable de ce scénario est que la période tombe bien dans la fenêtre, pas le mapping du libellé.
/// </summary>
public sealed class FrontWasmPeriodeDateAujourdhuiTests : TestContext
{
    // Date de référence injectée (IDateTimeProvider.Aujourdhui) : 26 juin 2026 (vendredi).
    private static readonly DateTime Aujourdhui = new(2026, 6, 26);

    [Fact]
    public void Should_Colorer_la_case_du_26_06_2026_pour_la_periode_affectee_When_un_parent_affecte_une_periode_via_le_front_WASM_sans_modifier_les_dates_pre_remplies()
    {
        // Given — l'hôte d'API détaché réel démarré joue l'API distante (store réel vierge, palette réelle).
        using var apiDistante = new ApiDistanteFactory();
        var transportVersApiDistante = apiDistante.Server.CreateHandler();
        var clientFront = new HttpClient(transportVersApiDistante)
        {
            BaseAddress = apiDistante.Server.BaseAddress,
        };

        // … et la vue réelle est câblée à ce transport distant + au port de date FIXÉ au 26/06/2026 :
        // le formulaire pré-remplit ses dates depuis IDateTimeProvider.Aujourdhui (et non figées 2025).
        Services.AddSingleton(clientFront);
        Services.AddSingleton(new SessionPlanning());
        Services.AddSingleton<IDateTimeProvider>(new DateTimeProviderFige(Aujourdhui));

        // Aucune période avant l'affectation (store distant vierge — pas de seed sous « Testing »).

        var page = RenderComponent<AffecterPeriode>();
        // Responsable « parent-a » (id stable connu de la palette réelle = bleu). Le mapping
        // libellé→id stable est l'objet du Sc.6 ; ici on rend seulement observable que la case
        // n'est plus neutre (la période tombe dans la fenêtre).
        page.Find("select.form-select").Change("parent-a");

        // When — le parent valide l'affectation SANS modifier aucune date pré-remplie, puis revient au planning.
        page.Find("form").Submit();

        // L'émission HTTP réelle vers l'API distante est asynchrone : on attend que la vue ait
        // réellement abouti — navigation vers « planning » sur succès du canal (aucun motif d'échec).
        var nav = (Bunit.TestDoubles.FakeNavigationManager)
            Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>();
        page.WaitForAssertion(
            () =>
            {
                Assert.Empty(page.FindAll("[data-testid='motif-echec']"));
                Assert.EndsWith("planning", nav.Uri);
            },
            TimeSpan.FromSeconds(10));

        // Then — la période est réellement enregistrée dans le store de l'API DISTANTE et relue par sa
        // projection à la semaine du lundi 22/06/2026 : la case du 26/06/2026 (vendredi) est COLORÉE
        // pour la période (couleur du responsable, NON neutre). L'affectation a transité par le canal.
        using var scope = apiDistante.Services.CreateScope();
        var projection = scope.ServiceProvider.GetRequiredService<GrilleAgendaQuery>();
        var palette = scope.ServiceProvider.GetRequiredService<IPaletteCouleurs>();
        var grille = projection.Projeter(new DateOnly(2026, 6, 22));

        var caseVendredi = grille.Jours.Single(j => j.Date == new DateOnly(2026, 6, 26));
        Assert.NotEqual(palette.CouleurNeutre, caseVendredi.CouleurResponsable);
    }
}
