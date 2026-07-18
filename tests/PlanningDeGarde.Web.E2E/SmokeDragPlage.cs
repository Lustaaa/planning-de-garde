namespace PlanningDeGarde.Web.E2E;

/// <summary>
/// TEMPS 2 (s49) — SMOKE TESTS navigateur du geste de sélection de plage, sur l'app RÉELLEMENT SERVIE
/// (front 5292 / API 5180, WASM réel, module JS <c>window.pdgPointeur</c>, elementFromPoint, SignalR).
/// Ils PROUVENT ce que bUnit ne peut pas : le drag souris natif surligne les cases et ouvre la dialog
/// « Affecter une période » (s06) pré-remplie sur l'intervalle ; un clic simple garde le menu clic-case.
///
/// RED->GREEN démontré : contre le build SERVI PÉRIMÉ (docker `web` d'avant les correctifs s49, cases
/// SANS <c>data-date</c>) ces tests ÉCHOUENT (aucune surbrillance, aucune dialog) ; contre le build
/// RÉ-COMPILÉ depuis la source courante (re-run du service `build`, recréation web/api) ils PASSENT.
/// </summary>
public sealed class SmokeDragPlage
{
    [Fact]
    public async Task Drag_de_J1_a_J3_surligne_les_cases_puis_ouvre_la_dialog_affecter_preremplie()
    {
        await using var h = await HarnaisNavigateur.DemarrerAsync();
        await h.ConnecterEtAllerAuPlanningAsync();
        var page = h.Page;

        var (c1, c2, c3, d1, _, d3) = await h.TroisCasesInternesAsync();
        Assert.False(string.IsNullOrEmpty(d1), "les cases doivent porter data-date (sinon build servi périmé)");

        // Geste souris natif : down sur J1, glisser jusqu'à J3, observer la surbrillance AVANT de relâcher.
        var (xa, ya) = await HarnaisNavigateur.CentreAsync(c1);
        var (xb, yb) = await HarnaisNavigateur.CentreAsync(c3);
        await page.Mouse.MoveAsync(xa, ya);
        await page.Mouse.DownAsync();
        for (var i = 1; i <= 8; i++)
        {
            var t = (float)i / 8;
            await page.Mouse.MoveAsync(xa + (xb - xa) * t, ya + (yb - ya) * t);
            await page.WaitForTimeoutAsync(15);
        }

        // PENDANT le geste : J1, J2 et J3 sont en surbrillance de plage.
        var surlignees = await h.CasesEnSurbrillanceAsync();
        Assert.Contains(d1, surlignees);
        Assert.Contains(d3, surlignees);
        Assert.True(surlignees.Length >= 3, $"attendu >= 3 cases surlignées, obtenu [{string.Join(",", surlignees)}]");

        // Relâchement (capté au niveau document par window.pdgPointeur) → finalisation.
        await page.Mouse.UpAsync();

        // AU RELÂCHEMENT : la dialog « Affecter une période » EXISTANTE (s06) s'ouvre.
        var dialog = page.Locator("[data-testid=dialog-affecter-periode]");
        await dialog.WaitForAsync(new() { State = Microsoft.Playwright.WaitForSelectorState.Visible, Timeout = 5000 });
        Assert.Equal(1, await dialog.CountAsync());
        Assert.Contains("Affecter une période", await dialog.InnerTextAsync());

        // PRÉ-REMPLIE sur l'intervalle NORMALISÉ [J1..J3] : Début = J1, Fin = J3 (aucune dialog neuve).
        var champsDate = dialog.Locator("input[type=date]");
        Assert.Equal(2, await champsDate.CountAsync());
        Assert.Equal(d1, await champsDate.Nth(0).InputValueAsync());
        Assert.Equal(d3, await champsDate.Nth(1).InputValueAsync());

        // La surbrillance de geste a disparu (état volatil) une fois la plage relâchée.
        var apres = await h.CasesEnSurbrillanceAsync();
        Assert.Empty(apres);
    }

    [Fact]
    public async Task Clic_simple_sur_une_case_ouvre_le_menu_clic_case_et_PAS_la_dialog_plage()
    {
        await using var h = await HarnaisNavigateur.DemarrerAsync();
        await h.ConnecterEtAllerAuPlanningAsync();
        var page = h.Page;

        var (c1, _, _, d1, _, _) = await h.TroisCasesInternesAsync();
        Assert.False(string.IsNullOrEmpty(d1), "les cases doivent porter data-date (sinon build servi périmé)");

        // Clic SIMPLE : down puis up au même endroit, sans déplacement (curseur resté sur l'ancre).
        var (x, y) = await HarnaisNavigateur.CentreAsync(c1);
        await page.Mouse.MoveAsync(x, y);
        await page.Mouse.DownAsync();
        await page.Mouse.UpAsync();

        // Le menu clic-case EXISTANT s'ouvre…
        var menu = page.Locator("[data-testid=menu-actions-case]");
        await menu.WaitForAsync(new() { State = Microsoft.Playwright.WaitForSelectorState.Visible, Timeout = 5000 });
        Assert.Equal(1, await menu.CountAsync());

        // …et surtout PAS la dialog « Affecter une période » pré-remplie sur une PLAGE (non-régression du clic simple).
        Assert.Equal(0, await page.Locator("[data-testid=dialog-affecter-periode]").CountAsync());
    }
}
