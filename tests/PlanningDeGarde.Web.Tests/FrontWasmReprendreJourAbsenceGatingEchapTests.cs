using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Web;
using PlanningDeGarde.Web.Components.Pages;
using PlanningDeGarde.Web.State;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 46 — Sc.5 (🖥️ @ihm) — acceptation de NIVEAU RUNTIME de la CONDITIONNALITÉ et du gating de l'entrée
/// « reprendre ce jour » : ABSENTE quand la case ne porte AUCUNE délégation active (fond seul — rien à
/// reprendre) ; l'Invité ne voit NI le menu clic-case NI l'entrée (Parent-gated, OuvrirMenu) ; Échap FERME la
/// confirmation de reprise sans émettre (port <see cref="IEcouteurEchapModal"/> s33, capture document), store
/// distant INTACT. Grille réellement câblée à l'API distante (store réel, projection réelle).
/// </summary>
public sealed class FrontWasmReprendreJourAbsenceGatingEchapTests : TestContext
{
    private static readonly DateTime Aujourdhui = GrilleRuntimeHarness.Lundi_29_06_2026;
    private static readonly DateOnly Mercredi_08_07_2026 = new(2026, 7, 8); // ISO 28 PAIRE → fond parent-a (Alice)

    private static CycleDeFond CycleAliceBruno()
        => new(2, new Dictionary<int, string> { [0] = "parent-a", [1] = "parent-b" });

    /// <summary>Double à la main du port d'écoute Échap (spy) : capte le callback d'attache et rejoue Échap document.</summary>
    private sealed class EspionEchap : IEcouteurEchapModal
    {
        private Func<Task>? _onEchap;
        public int Attachements { get; private set; }

        public ValueTask<IAsyncDisposable> EcouterAsync(Func<Task> onEchap)
        {
            Attachements++;
            _onEchap = onEchap;
            return ValueTask.FromResult<IAsyncDisposable>(new Abonnement(this));
        }

        public Task DeclencherEchapDocument() => _onEchap?.Invoke() ?? Task.CompletedTask;

        private sealed class Abonnement : IAsyncDisposable
        {
            private readonly EspionEchap _espion;
            public Abonnement(EspionEchap espion) => _espion = espion;
            public ValueTask DisposeAsync() { _espion._onEchap = null; return ValueTask.CompletedTask; }
        }
    }

    [Fact]
    public void L_entree_reprendre_est_absente_sur_une_case_sans_delegation_active()
    {
        // Given — grille câblée réelle, cycle de fond seul (aucune surcharge) : le 08/07 est résolu par le FOND (Alice).
        using var api = new ApiDistanteFactory();
        GrilleRuntimeHarness.SemerCycle(api, CycleAliceBruno());
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Aujourdhui);
        grille.WaitForAssertion(
            () => Assert.Equal(
                "Alice",
                GrilleRuntimeHarness.CaseDuJour(grille, "08/07").QuerySelector("[data-testid='nom-responsable']")!.TextContent.Trim()),
            TimeSpan.FromSeconds(10));

        // When — le Parent ouvre le menu de cette case (fond seul).
        this.SurDispatcher(() => GrilleRuntimeHarness.CaseDuJour(grille, "08/07").Click());
        var menu = grille.Find("[data-testid='menu-actions-case']");

        // Then — l'entrée « reprendre ce jour » est ABSENTE (rien à reprendre), « déléguer ce jour » reste présente.
        Assert.Null(menu.QuerySelector("[data-testid='action-reprendre']"));
        Assert.NotNull(menu.QuerySelector("[data-testid='action-deleguer']"));
    }

    [Fact]
    public void L_invite_ne_voit_ni_le_menu_ni_l_entree_reprendre()
    {
        // Given — grille câblée réelle, une délégation active existe sur le 08/07 (surcharge Bruno).
        using var api = new ApiDistanteFactory();
        GrilleRuntimeHarness.SemerCycle(api, CycleAliceBruno());
        GrilleRuntimeHarness.SemerPeriode(api, "parent-b",
            Mercredi_08_07_2026.ToDateTime(TimeOnly.MinValue), Mercredi_08_07_2026.ToDateTime(TimeOnly.MinValue));
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Aujourdhui);

        // When — l'identité effective bascule en Invité (consultation seule).
        var session = Services.GetRequiredService<SessionPlanning>();
        session.Role = RoleAuteur.Invite;
        grille.Render();

        // Then — cliquer une case n'ouvre PAS le menu (Parent-gated) : ni menu, ni entrée « reprendre ce jour ».
        this.SurDispatcher(() => GrilleRuntimeHarness.CaseDuJour(grille, "08/07").Click());
        Assert.Empty(grille.FindAll("[data-testid='menu-actions-case']"));
        Assert.Empty(grille.FindAll("[data-testid='action-reprendre']"));
    }

    [Fact]
    public void Echap_ferme_la_confirmation_de_reprise_sans_emettre_aucune_commande()
    {
        // Given — grille câblée réelle, port Échap DOUBLÉ (spy), une délégation active sur le 08/07 (surcharge Bruno).
        using var api = new ApiDistanteFactory();
        GrilleRuntimeHarness.SemerCycle(api, CycleAliceBruno());
        GrilleRuntimeHarness.SemerPeriode(api, "parent-b",
            Mercredi_08_07_2026.ToDateTime(TimeOnly.MinValue), Mercredi_08_07_2026.ToDateTime(TimeOnly.MinValue));
        var espion = new EspionEchap();
        Services.AddSingleton<IEcouteurEchapModal>(espion);
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Aujourdhui);

        // When — ouvre la confirmation de reprise (menu → entrée « reprendre ce jour »).
        grille.WaitForAssertion(
            () =>
            {
                this.SurDispatcher(() => GrilleRuntimeHarness.CaseDuJour(grille, "08/07").Click());
                this.SurDispatcher(() => grille.Find("[data-testid='menu-actions-case'] [data-testid='action-reprendre']").Click());
                Assert.NotEmpty(grille.FindAll("[data-testid='dialog-reprendre']"));
            },
            TimeSpan.FromSeconds(10));
        grille.WaitForAssertion(() => Assert.Equal(1, espion.Attachements), TimeSpan.FromSeconds(10));

        // When — Échap document (rejoué via le callback capté par le spy).
        this.SurDispatcher(() => espion.DeclencherEchapDocument().GetAwaiter().GetResult());

        // Then — la dialog se ferme SANS émettre : la surcharge du 08/07 est TOUJOURS dans le store distant (intact).
        grille.WaitForAssertion(
            () => Assert.Empty(grille.FindAll("[data-testid='dialog-reprendre']")),
            TimeSpan.FromSeconds(10));
        var restante = Assert.Single(api.Services.GetRequiredService<IPeriodeRepository>().AllSnapshots());
        Assert.Equal("parent-b", restante.ResponsableId);
        Assert.Equal(Mercredi_08_07_2026, DateOnly.FromDateTime(restante.Debut));
    }
}
