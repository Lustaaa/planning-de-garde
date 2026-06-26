extern alias api;
using System.Net.Http;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Web.Components.Pages;
using PlanningDeGarde.Web.State;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Acceptation de NIVEAU RUNTIME du Sc.3 (🖥️ scénario IHM, <c>@nominal</c>) — le défaut vit dans le
/// <b>formulaire</b> <see cref="DefinirTransfert"/> : sa date était <b>figée en 2025</b>
/// (<c>new(2025,7,21)</c>), <b>et</b> sa logique vivait dans le <b>template</b> (dette : pas de
/// code-behind, donc aucun point d'injection propre). La correction passe la vue en <b>code-behind</b>
/// et réutilise le port <see cref="IDateTimeProvider"/> (construit au Sc.1) pour pré-remplir la date
/// sur <c>Aujourdhui</c>.
///
/// Aucun transfert n'étant projeté dans la grille à ce palier (trou par construction, pas d'observable
/// couleur), l'acceptation observe la <b>commande réellement reçue par le canal de l'API distante</b>
/// (la date du transfert <b>persisté dans le store réel</b> de l'hôte d'API) <b>et</b> le succès. On
/// rend la <b>vue réelle</b> (bUnit rend le vrai <see cref="DefinirTransfert"/> avec sa DI réelle, dont
/// un <see cref="IDateTimeProvider"/> doublé fixé au <b>26/06/2026</b>) câblée à un <b>vrai transport
/// HTTP</b> vers une <b>API distante réelle</b> (<see cref="ApiDistanteFactory"/>, store réel singleton).
///
/// Anti « vert qui ment » : si le formulaire porte encore la date figée 2025 (ou si la logique reste
/// dans le template sans point d'injection), la commande reçue porte le 21/07/2025 → rouge. Un bUnit à
/// doublure de date/transport ne verrait pas ce câblage runtime « date par défaut = aujourd'hui ».
/// </summary>
public sealed class FrontWasmTransfertDateAujourdhuiTests : TestContext
{
    // Date de référence injectée (IDateTimeProvider.Aujourdhui) : 26 juin 2026 (vendredi).
    private static readonly DateTime Aujourdhui = new(2026, 6, 26);

    [Fact]
    public void Should_Horodater_le_transfert_au_26_06_2026_dans_la_commande_recue_par_le_canal_When_un_parent_definit_un_transfert_via_le_front_WASM_sans_modifier_la_date_pre_remplie()
    {
        // Given — l'hôte d'API détaché réel démarré joue l'API distante (store réel vierge).
        using var apiDistante = new ApiDistanteFactory();
        var transportVersApiDistante = apiDistante.Server.CreateHandler();
        var clientFront = new HttpClient(transportVersApiDistante)
        {
            BaseAddress = apiDistante.Server.BaseAddress,
        };

        // … et la vue réelle est câblée à ce transport distant + au port de date FIXÉ au 26/06/2026 :
        // le formulaire (passé en code-behind) pré-remplit sa date depuis Aujourdhui (et non figée 2025).
        Services.AddSingleton(clientFront);
        Services.AddSingleton(new SessionPlanning());
        Services.AddSingleton<IDateTimeProvider>(new DateTimeProviderFige(Aujourdhui));

        var page = RenderComponent<DefinirTransfert>();
        // Dépose / récupère (id stables par cohérence (B), sans observable couleur à ce palier) + lieu.
        page.FindAll("select.form-select")[0].Change("parent-a");
        page.FindAll("select.form-select")[1].Change("parent-b");
        page.FindAll("select.form-select")[2].Change("école");
        page.Find("input[type=time]").Change("16:30");

        // When — le parent valide le transfert SANS modifier la date pré-remplie, puis revient au planning.
        page.Find("form").Submit();

        // L'émission HTTP réelle vers l'API distante est asynchrone : on attend que la vue ait abouti
        // — navigation vers « planning » sur succès du canal (aucun motif d'échec affiché).
        var nav = (Bunit.TestDoubles.FakeNavigationManager)
            Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>();
        page.WaitForAssertion(
            () =>
            {
                Assert.Empty(page.FindAll("[data-testid='motif-echec']"));
                Assert.EndsWith("planning", nav.Uri);
            },
            TimeSpan.FromSeconds(10));

        // Then — la commande de transfert reçue par le canal de l'API DISTANTE porte la date du
        // 26/06/2026 : elle a réellement transité jusqu'au store réel de l'hôte d'API (pas un accusé,
        // pas une doublure de transport). Aucun transfert n'étant projeté dans la grille à ce palier,
        // l'observable est la commande persistée elle-même.
        using var scope = apiDistante.Services.CreateScope();
        var transferts = scope.ServiceProvider.GetRequiredService<ITransfertRepository>();
        var transfert = Assert.Single(transferts.AllSnapshots());
        Assert.Equal(new DateOnly(2026, 6, 26), DateOnly.FromDateTime(transfert.Date));
    }
}
