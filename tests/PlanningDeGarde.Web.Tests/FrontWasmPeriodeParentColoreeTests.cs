extern alias api;
using System.Linq;
using System.Net.Http;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Web.Components.Pages;
using PlanningDeGarde.Web.State;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Acceptation de NIVEAU RUNTIME du Sc.6 (🖥️ scénario IHM, <c>@nominal</c>) — cœur du cadrage (B).
/// Le défaut vit à la <b>source</b> : le sélecteur <see cref="AffecterPeriode"/> bindait le
/// <b>libellé</b> « Parent A » (<c>Foyer.Responsables</c>), donc le canal recevait
/// <c>ResponsableId = "Parent A"</c> — clé <b>absente</b> de <c>CouleursParActeur</c> → repli gris au
/// lieu de la couleur du parent. La correction : <b>la source fournit l'identifiant stable</b>
/// (<c>parent-a</c>/<c>parent-b</c>) — option affichant le libellé mais dont la <c>value</c> est l'id —
/// pour rendre le set atteignable.
///
/// On rend la <b>vue réelle</b> (bUnit rend le vrai <see cref="AffecterPeriode"/> avec sa source réelle
/// de responsables) câblée à un <b>vrai transport HTTP</b> vers une <b>API distante réelle</b>
/// (<see cref="ApiDistanteFactory"/>, palette réelle <c>parent-a→bleu</c>/<c>parent-b→orange</c>,
/// projection réelle <see cref="GrilleAgendaQuery"/>). L'utilisateur sélectionne le responsable par son
/// <b>libellé affiché</b> (on lit la <c>value</c> de l'option dont le texte est « Parent A ») : c'est la
/// source qui doit binder l'id stable. L'observable : les cases réellement colorées (bleu/orange).
///
/// Anti « vert qui ment » : si la source bind encore le libellé, l'option « Parent A » a pour value le
/// libellé → le canal reçoit « Parent A » → repli gris → cases non bleues → rouge. Un bUnit à doublure
/// de transport ne verrait jamais que le canal reçoit le mauvais identifiant.
/// </summary>
public sealed class FrontWasmPeriodeParentColoreeTests : TestContext
{
    // Date de référence injectée (les dates de période sont saisies explicitement ci-dessous).
    private static readonly DateTime Aujourdhui = new(2026, 6, 26);

    [Fact]
    public void Should_Colorer_en_bleu_les_cases_de_Parent_A_et_en_orange_celles_de_Parent_B_When_le_front_WASM_affecte_des_periodes_via_l_API_distante_avec_l_identifiant_stable_du_responsable()
    {
        // Given — l'hôte d'API détaché réel démarré joue l'API distante (store réel vierge, palette réelle).
        using var apiDistante = new ApiDistanteFactory();
        var clientFront = new HttpClient(apiDistante.Server.CreateHandler())
        {
            BaseAddress = apiDistante.Server.BaseAddress,
        };

        Services.AddSingleton(clientFront);
        Services.AddSingleton(new SessionPlanning());
        Services.AddSingleton<IDateTimeProvider>(new DateTimeProviderFige(Aujourdhui));

        // Le parent affecte Parent A du 24 au 27/06, puis Parent B du 28 au 30/06 — via la vue réelle,
        // en choisissant le responsable par son LIBELLÉ affiché (la source doit binder l'id stable).
        AffecterViaVueReelle("Parent A", new DateTime(2026, 6, 24), new DateTime(2026, 6, 27));
        AffecterViaVueReelle("Parent B", new DateTime(2026, 6, 28), new DateTime(2026, 6, 30));

        // When — la grille est projetée à la semaine du lundi 22/06/2026 (palette + projection réelles).
        using var scope = apiDistante.Services.CreateScope();
        var projection = scope.ServiceProvider.GetRequiredService<GrilleAgendaQuery>();
        var grille = projection.Projeter(new DateOnly(2026, 6, 22));

        // Then — les cases du 24 au 27/06 sont bleues (Parent A) ...
        var casesParentA = grille.Jours
            .Where(j => j.Date >= new DateOnly(2026, 6, 24) && j.Date <= new DateOnly(2026, 6, 27))
            .ToList();
        Assert.Equal(4, casesParentA.Count);
        Assert.All(casesParentA, j => Assert.Equal("bleu", j.CouleurResponsable));

        // ... et les cases du 28 au 30/06 sont orange (Parent B).
        var casesParentB = grille.Jours
            .Where(j => j.Date >= new DateOnly(2026, 6, 28) && j.Date <= new DateOnly(2026, 6, 30))
            .ToList();
        Assert.Equal(3, casesParentB.Count);
        Assert.All(casesParentB, j => Assert.Equal("orange", j.CouleurResponsable));
    }

    /// <summary>
    /// Émet une affectation via la <b>vue réelle</b> : on sélectionne le responsable par son
    /// <b>libellé affiché</b> (on prend la <c>value</c> de l'option dont le texte est le libellé — comme
    /// le navigateur qui poste la value de l'option choisie), on saisit les dates, puis on valide. La
    /// vue émet vers l'API distante réelle ; on attend l'aboutissement (navigation succès).
    /// </summary>
    private void AffecterViaVueReelle(string libelleResponsable, DateTime debut, DateTime fin)
    {
        var nav = (Bunit.TestDoubles.FakeNavigationManager)
            Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>();
        nav.NavigateTo(nav.BaseUri); // réinitialise l'URL avant la saisie

        var page = RenderComponent<AffecterPeriode>();

        // L'utilisateur choisit l'option dont le TEXTE affiché est le libellé : on poste sa value (id
        // stable après correction, libellé avant). C'est le câblage de la source qui est sous test.
        var optionResponsable = page.FindAll("select.form-select option")
            .Single(o => o.TextContent.Trim() == libelleResponsable);
        page.Find("select.form-select").Change(optionResponsable.GetAttribute("value")!);

        page.FindAll("input.form-control")[0].Change(debut.ToString("yyyy-MM-dd"));
        page.FindAll("input.form-control")[1].Change(fin.ToString("yyyy-MM-dd"));
        page.Find("form").Submit();

        page.WaitForAssertion(
            () =>
            {
                Assert.Empty(page.FindAll("[data-testid='motif-echec']"));
                Assert.EndsWith("planning", nav.Uri);
            },
            TimeSpan.FromSeconds(10));
    }
}
