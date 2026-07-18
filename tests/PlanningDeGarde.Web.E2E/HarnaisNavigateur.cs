using Microsoft.Playwright;

namespace PlanningDeGarde.Web.E2E;

/// <summary>
/// Socle du harnais E2E navigateur (s49). Démarre Chromium, ouvre l'app SERVIE, se connecte en
/// Parent via le VRAI formulaire de connexion (compte de démo semé), puis atteint la grille
/// <c>/planning</c>. Capture systématiquement la console navigateur et les erreurs de page pour
/// le diagnostic. Réglable par variables d'environnement :
///   PDG_E2E_BASEURL (défaut http://localhost:5292), PDG_E2E_HEADED=1 (fenêtre visible),
///   PDG_E2E_EMAIL / PDG_E2E_MOTDEPASSE (défaut compte de démo).
/// </summary>
public sealed class HarnaisNavigateur : IAsyncDisposable
{
    public static string BaseUrl =>
        Environment.GetEnvironmentVariable("PDG_E2E_BASEURL") ?? "http://localhost:5292";

    private static string Email =>
        Environment.GetEnvironmentVariable("PDG_E2E_EMAIL") ?? "deveaux.cyril@gmail.com";

    private static string MotDePasse =>
        Environment.GetEnvironmentVariable("PDG_E2E_MOTDEPASSE") ?? "Toto123@";

    private static bool Headed =>
        Environment.GetEnvironmentVariable("PDG_E2E_HEADED") == "1";

    private IPlaywright _pw = default!;
    private IBrowser _browser = default!;

    public IPage Page { get; private set; } = default!;
    public List<string> MessagesConsole { get; } = new();
    public List<string> ErreursPage { get; } = new();

    public static async Task<HarnaisNavigateur> DemarrerAsync()
    {
        var h = new HarnaisNavigateur();
        await h.InitAsync();
        return h;
    }

    private async Task InitAsync()
    {
        _pw = await Playwright.CreateAsync();
        _browser = await _pw.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = !Headed,
        });
        var context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1400, Height = 1000 },
        });
        Page = await context.NewPageAsync();

        Page.Console += (_, msg) => MessagesConsole.Add($"[{msg.Type}] {msg.Text}");
        Page.PageError += (_, err) => ErreursPage.Add(err);
    }

    /// <summary>Se connecte en Parent via le formulaire réel et attend l'affichage de la grille.</summary>
    public async Task ConnecterEtAllerAuPlanningAsync()
    {
        await Page.GotoAsync(BaseUrl + "/connexion", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

        // Le WASM peut mettre un instant à hydrater le formulaire.
        await Page.WaitForSelectorAsync("[data-testid=champ-email-connexion]", new PageWaitForSelectorOptions { Timeout = 30_000 });
        await Page.FillAsync("[data-testid=champ-email-connexion]", Email);
        await Page.FillAsync("[data-testid=champ-mot-de-passe-connexion]", MotDePasse);
        await Page.ClickAsync("[data-testid=bouton-se-connecter]");

        // Redirection vers /planning + grille rendue (au moins une case).
        await Page.WaitForURLAsync("**/planning", new PageWaitForURLOptions { Timeout = 30_000 });
        await Page.WaitForSelectorAsync("[data-testid=jour-case]", new PageWaitForSelectorOptions { Timeout = 30_000 });
    }

    /// <summary>Trois cases CONSÉCUTIVES bien à l'intérieur de la vue (mêmes repères que le geste réel).
    /// Renvoie les locators + leurs data-date, pour piloter un drag horizontal net sur une même rangée.</summary>
    public async Task<(ILocator C1, ILocator C2, ILocator C3, string D1, string D2, string D3)> TroisCasesInternesAsync()
    {
        var cases = Page.Locator("[data-testid=jour-case]");
        var n = await cases.CountAsync();
        var i = Math.Max(0, Math.Min(7, n - 3));
        var c1 = cases.Nth(i);
        var c2 = cases.Nth(i + 1);
        var c3 = cases.Nth(i + 2);
        var d1 = await c1.GetAttributeAsync("data-date") ?? "";
        var d2 = await c2.GetAttributeAsync("data-date") ?? "";
        var d3 = await c3.GetAttributeAsync("data-date") ?? "";
        return (c1, c2, c3, d1, d2, d3);
    }

    /// <summary>Centre écran d'une case (pour piloter la souris au pixel).</summary>
    public static async Task<(float X, float Y)> CentreAsync(ILocator c)
    {
        var b = await c.BoundingBoxAsync() ?? throw new InvalidOperationException("case hors écran");
        return (b.X + b.Width / 2, b.Y + b.Height / 2);
    }

    /// <summary>Rejoue le GESTE souris natif : down au départ, déplacements progressifs (plusieurs
    /// pointermove, comme un vrai glisser), up à l'arrivée. Reproduit le geste que bUnit ne sait pas simuler.</summary>
    public async Task DragAsync(ILocator depart, ILocator arrivee, int pas = 8)
    {
        var (xa, ya) = await CentreAsync(depart);
        var (xb, yb) = await CentreAsync(arrivee);
        await Page.Mouse.MoveAsync(xa, ya);
        await Page.Mouse.DownAsync();
        for (var i = 1; i <= pas; i++)
        {
            var t = (float)i / pas;
            await Page.Mouse.MoveAsync(xa + (xb - xa) * t, ya + (yb - ya) * t);
            await Page.WaitForTimeoutAsync(15);
        }
        await Page.Mouse.UpAsync();
    }

    /// <summary>data-date des cases actuellement en surbrillance de plage EN COURS (data-plage-drag=1).</summary>
    public async Task<string[]> CasesEnSurbrillanceAsync() =>
        await Page.EvaluateAsync<string[]>(@"() => Array.from(document.querySelectorAll('[data-testid=jour-case]'))
            .filter(e => e.getAttribute('data-plage-drag') === '1')
            .map(e => e.getAttribute('data-date'))");

    public async ValueTask DisposeAsync()
    {
        try { await _browser.CloseAsync(); } catch { }
        _pw?.Dispose();
    }
}
