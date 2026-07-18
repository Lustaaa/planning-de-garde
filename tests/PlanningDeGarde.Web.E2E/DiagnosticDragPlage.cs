using System.Text;
using Microsoft.Playwright;

namespace PlanningDeGarde.Web.E2E;

/// <summary>
/// TEMPS 1 (s49) — DIAGNOSTIC PUR du drag de sélection de plage sur le VRAI navigateur.
/// N'affirme presque rien : il OBSERVE et consigne (console, erreurs de page, état du module JS,
/// data-date, classes de surbrillance pendant/après le geste) dans un fichier lisible, pour
/// identifier la cause runtime avec des PREUVES au lieu d'une hypothèse.
/// </summary>
public sealed class DiagnosticDragPlage
{
    private static readonly string RapportPath =
        Environment.GetEnvironmentVariable("PDG_E2E_RAPPORT")
        ?? Path.Combine(Path.GetTempPath(), "pdg-e2e-diagnostic.txt");

    [Fact]
    public async Task Observer_le_geste_de_drag_reel()
    {
        var log = new StringBuilder();
        void L(string s) => log.AppendLine(s);

        await using var h = await HarnaisNavigateur.DemarrerAsync();
        try
        {
            await h.ConnecterEtAllerAuPlanningAsync();
            L("== CONNEXION OK, /planning atteint ==");
        }
        catch (Exception ex)
        {
            L("!! ECHEC connexion/navigation : " + ex.Message);
            Vider(log, h);
            throw;
        }

        var page = h.Page;

        // 1) Le module JS window.pdgPointeur existe-t-il ? et ses fonctions ?
        var moduleExiste = await page.EvaluateAsync<bool>("() => typeof window.pdgPointeur === 'object' && window.pdgPointeur !== null");
        var aAttacher = await page.EvaluateAsync<bool>("() => window.pdgPointeur && typeof window.pdgPointeur.attacher === 'function'");
        var aAttacherMvt = await page.EvaluateAsync<bool>("() => window.pdgPointeur && typeof window.pdgPointeur.attacherMouvement === 'function'");
        L($"window.pdgPointeur présent = {moduleExiste} ; attacher = {aAttacher} ; attacherMouvement = {aAttacherMvt}");

        // 2) Combien de cases, portent-elles data-date ? sur quelle vue ?
        var nbCases = await page.Locator("[data-testid=jour-case]").CountAsync();
        var vue = await page.GetAttributeAsync("[data-testid=grille-agenda]", "data-vue");
        L($"vue = {vue} ; nb cases = {nbCases}");

        var cases = page.Locator("[data-testid=jour-case]");
        // Sélectionne 3 cases distinctes bien à l'intérieur de la vue.
        var idx1 = Math.Min(7, nbCases - 3);
        if (idx1 < 0) idx1 = 0;
        var c1 = cases.Nth(idx1);
        var c2 = cases.Nth(idx1 + 1);
        var c3 = cases.Nth(idx1 + 2);

        var d1 = await c1.GetAttributeAsync("data-date");
        var d2 = await c2.GetAttributeAsync("data-date");
        var d3 = await c3.GetAttributeAsync("data-date");
        L($"cases cibles data-date : c1={d1} c2={d2} c3={d3}");

        // 3) Instrumente le module JS pour tracer chaque appel .NET remonté (attacherMouvement/attacher).
        //    On enveloppe les invokeMethodAsync du dotNetRef passé à attacher/attacherMouvement.
        await page.EvaluateAsync(@"() => {
            window.__pdgTrace = [];
            const p = window.pdgPointeur;
            if (!p || p.__trace) return;
            p.__trace = true;
            const wrap = (nom, fn) => function(dotNetRef) {
                const ref = {
                    invokeMethodAsync: function(m, ...args) {
                        window.__pdgTrace.push(nom + '->' + m + '(' + JSON.stringify(args) + ')');
                        return dotNetRef.invokeMethodAsync(m, ...args);
                    }
                };
                return fn.call(p, ref);
            };
            if (p.attacher) { const o = p.attacher.bind(p); p.attacher = wrap('up', o); }
            if (p.attacherMouvement) { const o = p.attacherMouvement.bind(p); p.attacherMouvement = wrap('move', o); }
        }");
        L("(note : le wrap de trace ne prend effet que pour les abonnements POSTÉRIEURS ; les abonnements posés à OnAfterRender sont déjà en place)");

        // 4) Trace brute des événements pointeur natifs au niveau document (indépendant de Blazor).
        await page.EvaluateAsync(@"() => {
            window.__ptr = [];
            const rec = (t) => (e) => window.__ptr.push(t + ' buttons=' + e.buttons + ' tgt=' + (e.target && e.target.getAttribute ? (e.target.getAttribute('data-testid')||e.target.tagName) : '?'));
            document.addEventListener('pointerdown', rec('down'), true);
            document.addEventListener('pointermove', rec('move'), true);
            document.addEventListener('pointerup', rec('up'), true);
        }");

        // 5) Le GESTE réel : mousedown sur c1, move progressif vers c2 puis c3, up.
        var b1 = await c1.BoundingBoxAsync();
        var b2 = await c2.BoundingBoxAsync();
        var b3 = await c3.BoundingBoxAsync();
        L($"bbox c1={Fmt(b1)} c2={Fmt(b2)} c3={Fmt(b3)}");

        if (b1 is null || b2 is null || b3 is null) { L("!! bbox null, abandon"); Vider(log, h); Assert.Fail("bbox null"); }

        var (x1, y1) = (b1!.X + b1.Width / 2, b1.Y + b1.Height / 2);
        var (x2, y2) = (b2!.X + b2.Width / 2, b2.Y + b2.Height / 2);
        var (x3, y3) = (b3!.X + b3.Width / 2, b3.Y + b3.Height / 2);

        await page.Mouse.MoveAsync(x1, y1);
        await page.Mouse.DownAsync();
        L("-- souris DOWN sur c1 --");
        // Bouge par petits pas pour émettre plusieurs pointermove.
        await MoveEnPasAsync(page, x1, y1, x2, y2, 6);
        // Capture surbrillance à mi-parcours (attendu : c1 et c2 surlignées).
        var dragMid = await CasesSurlignees(page);
        L($"pendant le geste (c1->c2), cases data-plage-drag=1 : [{string.Join(", ", dragMid)}]");
        await MoveEnPasAsync(page, x2, y2, x3, y3, 6);
        var dragFin = await CasesSurlignees(page);
        L($"pendant le geste (->c3), cases data-plage-drag=1 : [{string.Join(", ", dragFin)}]");

        await page.Mouse.UpAsync();
        L("-- souris UP --");
        await page.WaitForTimeoutAsync(400);

        // 6) Après relâchement : dialog « Affecter une période » ouverte ? menu clic-case ?
        var dialogVisible = await page.Locator("[data-testid=dialog-affecter-periode], .affecter-periode-dialog, [data-testid=champ-date-debut]").CountAsync();
        var menuVisible = await page.Locator("[data-testid=menu-actions-case]").CountAsync();
        L($"après UP : elements dialog-affecter (heuristique) = {dialogVisible} ; menu-actions-case = {menuVisible}");

        // Dump de la trace JS.
        var ptr = await page.EvaluateAsync<string[]>("() => window.__ptr || []");
        L("---- trace pointer natifs document ----");
        foreach (var p in ptr.Take(40)) L("  " + p);
        L($"  (total {ptr.Length} evenements pointeur)");

        var trace = await page.EvaluateAsync<string[]>("() => window.__pdgTrace || []");
        L("---- trace invoke .NET (abonnements re-wrappés seulement) ----");
        foreach (var t in trace.Take(40)) L("  " + t);

        L("---- messages console navigateur ----");
        foreach (var m in h.MessagesConsole.Take(60)) L("  " + m);
        L("---- erreurs de page ----");
        foreach (var e in h.ErreursPage) L("  " + e);

        var shot = Path.Combine(Path.GetDirectoryName(RapportPath)!, "pdg-e2e-apres-drag.png");
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = shot, FullPage = true });
        L("screenshot : " + shot);

        Vider(log, h);
    }

    private static async Task MoveEnPasAsync(IPage page, float xa, float ya, float xb, float yb, int pas)
    {
        for (var i = 1; i <= pas; i++)
        {
            var t = (float)i / pas;
            await page.Mouse.MoveAsync(xa + (xb - xa) * t, ya + (yb - ya) * t);
            await page.WaitForTimeoutAsync(20);
        }
    }

    private static async Task<string[]> CasesSurlignees(IPage page) =>
        await page.EvaluateAsync<string[]>(@"() => Array.from(document.querySelectorAll('[data-testid=jour-case]'))
            .filter(e => e.getAttribute('data-plage-drag') === '1')
            .map(e => e.getAttribute('data-date'))");

    private static string Fmt(LocatorBoundingBoxResult? b) => b is null ? "null" : $"({b.X:0},{b.Y:0} {b.Width:0}x{b.Height:0})";

    private static void Vider(StringBuilder log, HarnaisNavigateur h)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(RapportPath)!);
        File.WriteAllText(RapportPath, log.ToString());
    }
}
