using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Web;
using PlanningDeGarde.Web.Components.Pages;
using PlanningDeGarde.Web.State;
using Xunit;
using static PlanningDeGarde.Web.CanalEcriture;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 47 — Sc.8 (🖥️ @ihm) — acceptation de NIVEAU RUNTIME : l'échange consenti sur l'app réellement câblée.
/// (1) L'ÉMETTEUR PROPOSE via l'entrée « proposer un échange » du MENU CLIC-CASE (Parent-gated) — la proposition
/// pending est écrite SANS aucune surcharge (canal de consentement). (2) Le RECEVANT voit la proposition comme
/// notification ACTIONNABLE dans sa cloche (Accepter / Refuser) ; Accepter (via mini-dialog de confirmation)
/// émet AccepterProposition par le canal d'écriture → surcharge composée (s44) ; Refuser émet RefuserProposition
/// sans écriture. La RÉPONSE vit dans la cloche (pas de badge sur la case, pas d'entrée conditionnelle du menu).
///
/// Anti « vert qui ment » : proposition / accord / refus prouvés jusqu'au store distant réel (repositories réels).
/// </summary>
[Collection("SignalRTempsReel")]
public sealed class FrontWasmEchangeActionnableTests : TestContext
{
    private static readonly DateTime Aujourdhui = GrilleRuntimeHarness.Lundi_29_06_2026; // 29/06 → parent-b (fond)
    private static readonly DateOnly Jour = new(2026, 6, 29);

    /// <summary>Double à la main du port d'écoute Échap (spy).</summary>
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
            private readonly EspionEchap _e;
            public Abonnement(EspionEchap e) => _e = e;
            public ValueTask DisposeAsync() { _e._onEchap = null; return ValueTask.CompletedTask; }
        }
    }

    private static SessionPlanning SessionComme(string acteurId, string nom)
    {
        var session = new SessionPlanning();
        session.Connecter(nom, acteurId, TypeActeur.Parent);
        return session;
    }

    private IRenderedComponent<PlanningPartage> Rendre(ApiDistanteFactory api, SessionPlanning session, IEcouteurEchapModal? echap = null)
    {
        Services.AddSingleton(GrilleRuntimeHarness.ClientVers(api));
        Services.AddSingleton(session);
        Services.AddSingleton<IDateTimeProvider>(new DateTimeProviderFige(Aujourdhui));
        Services.AddSingleton(new OptionsConnexionHub
        {
            Configurer = options =>
            {
                options.HttpMessageHandlerFactory = _ => api.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
            },
        });
        if (echap is not null)
            Services.AddSingleton(echap);
        var grille = RenderComponent<PlanningPartage>();
        grille.WaitForState(() => grille.FindAll("[data-testid='jour-case']").Count == 28, TimeSpan.FromSeconds(10));
        return grille;
    }

    /// <summary>Sème une proposition pending adressée à <paramref name="versActeurId"/> par le canal réel
    /// (cédant = responsable résolu du jour).</summary>
    private static async Task SemerPropositionVers(ApiDistanteFactory api, string versActeurId)
    {
        var client = GrilleRuntimeHarness.ClientVers(api);
        (await client.PostAsJsonAsync("api/canal/proposer-echange",
            new ProposerEchangeRequete(Jour, "Léa", versActeurId))).EnsureSuccessStatusCode();
    }

    private static void SemerCycle(ApiDistanteFactory api)
        => GrilleRuntimeHarness.SemerCycle(api, new CycleDeFond(2, new Dictionary<int, string> { [0] = "parent-a", [1] = "parent-b" }));

    [Fact]
    public void L_emetteur_propose_via_le_menu_clic_case_ce_qui_ecrit_une_proposition_pending_SANS_surcharge()
    {
        // Given — Parent émetteur (parent-b, responsable de fond du 29/06). Menu clic-case Parent-gated.
        using var api = new ApiDistanteFactory();
        SemerCycle(api);
        var grille = Rendre(api, SessionComme("parent-b", "Bruno"));

        // When — clic sur la case du jour → menu → « proposer un échange » → mini-dialog → recevant parent-a → valider.
        grille.WaitForAssertion(
            () =>
            {
                this.SurDispatcher(() => GrilleRuntimeHarness.CaseDuJour(grille, "29/06").Click());
                this.SurDispatcher(() => grille.Find("[data-testid='menu-actions-case'] [data-testid='action-proposer-echange']").Click());
                Assert.NotEmpty(grille.FindAll("[data-testid='dialog-proposer']"));
            },
            TimeSpan.FromSeconds(10));
        this.SurDispatcher(() => grille.Find("[data-testid='champ-recevant']").Change("parent-a"));
        this.SurDispatcher(() => grille.Find("[data-testid='dialog-proposer'] form").Submit());

        // Then — une proposition PENDING (de parent-b vers parent-a) est dans le store, et AUCUNE surcharge écrite.
        grille.WaitForAssertion(
            () =>
            {
                var propositions = api.Services.GetRequiredService<IPropositionEchangeRepository>().AllSnapshots();
                Assert.Contains(propositions, p => p.Jour == Jour && p.DeActeurId == "parent-b"
                    && p.VersActeurId == "parent-a" && p.Statut == StatutProposition.Proposee);
            },
            TimeSpan.FromSeconds(10));
        Assert.Empty(api.Services.GetRequiredService<IPeriodeRepository>().AllSnapshots()); // PROPOSER n'écrit rien
    }

    [Fact]
    public async Task Le_recevant_voit_une_notif_actionnable_et_ACCEPTER_via_confirmation_compose_la_delegation()
    {
        // Given — une proposition pending adressée à parent-a (le recevant), visible dans sa cloche.
        using var api = new ApiDistanteFactory();
        SemerCycle(api);
        await SemerPropositionVers(api, "parent-a");
        var grille = Rendre(api, SessionComme("parent-a", "Alice"));

        // When — ouvrir la cloche : la notification d'échange porte Accepter / Refuser (actionnable).
        this.SurDispatcher(() => grille.Find("[data-testid='cloche-bouton']").Click());
        grille.WaitForAssertion(
            () =>
            {
                var notif = grille.Find("[data-testid='cloche-panneau'] [data-testid='cloche-notif'][data-type='echange']");
                Assert.NotNull(notif.QuerySelector("[data-testid='cloche-accepter']"));
                Assert.NotNull(notif.QuerySelector("[data-testid='cloche-refuser']"));
            },
            TimeSpan.FromSeconds(10));

        // Accepter → mini-dialog de confirmation → Confirmer (émet AccepterProposition par le canal d'écriture).
        this.SurDispatcher(() => grille.Find("[data-testid='cloche-accepter']").Click());
        grille.WaitForAssertion(() => Assert.NotEmpty(grille.FindAll("[data-testid='cloche-confirmer']")), TimeSpan.FromSeconds(10));
        this.SurDispatcher(() => grille.Find("[data-testid='cloche-confirmer']").Click());

        // Then — la proposition passe à ACCEPTÉE et la délégation s44 est composée (surcharge parent-a du 29/06).
        grille.WaitForAssertion(
            () =>
            {
                Assert.Contains(api.Services.GetRequiredService<IPropositionEchangeRepository>().AllSnapshots(),
                    p => p.VersActeurId == "parent-a" && p.Statut == StatutProposition.Acceptee);
            },
            TimeSpan.FromSeconds(10));
        using var scope = api.Services.CreateScope();
        var projection = scope.ServiceProvider.GetRequiredService<GrilleAgendaQuery>();
        Assert.Equal("Alice", projection.Projeter(Jour).Jours.Single(j => j.Date == Jour).NomResponsable);
    }

    [Fact]
    public async Task Le_recevant_REFUSE_via_confirmation_sans_aucune_ecriture()
    {
        using var api = new ApiDistanteFactory();
        SemerCycle(api);
        await SemerPropositionVers(api, "parent-a");
        var grille = Rendre(api, SessionComme("parent-a", "Alice"));

        this.SurDispatcher(() => grille.Find("[data-testid='cloche-bouton']").Click());
        grille.WaitForAssertion(() => Assert.NotEmpty(grille.FindAll("[data-testid='cloche-refuser']")), TimeSpan.FromSeconds(10));
        this.SurDispatcher(() => grille.Find("[data-testid='cloche-refuser']").Click());
        grille.WaitForAssertion(() => Assert.NotEmpty(grille.FindAll("[data-testid='cloche-confirmer']")), TimeSpan.FromSeconds(10));
        this.SurDispatcher(() => grille.Find("[data-testid='cloche-confirmer']").Click());

        // Then — la proposition passe à REFUSÉE, AUCUNE surcharge n'est écrite (store intact).
        grille.WaitForAssertion(
            () => Assert.Contains(api.Services.GetRequiredService<IPropositionEchangeRepository>().AllSnapshots(),
                p => p.VersActeurId == "parent-a" && p.Statut == StatutProposition.Refusee),
            TimeSpan.FromSeconds(10));
        Assert.Empty(api.Services.GetRequiredService<IPeriodeRepository>().AllSnapshots());
    }

    [Fact]
    public async Task Echap_ferme_le_mini_dialog_de_confirmation_sans_emettre_le_panneau_reste_ouvert()
    {
        using var api = new ApiDistanteFactory();
        SemerCycle(api);
        await SemerPropositionVers(api, "parent-a");
        var espion = new EspionEchap();
        var grille = Rendre(api, SessionComme("parent-a", "Alice"), espion);

        this.SurDispatcher(() => grille.Find("[data-testid='cloche-bouton']").Click());
        grille.WaitForAssertion(() => Assert.Equal(1, espion.Attachements), TimeSpan.FromSeconds(10));
        this.SurDispatcher(() => grille.Find("[data-testid='cloche-accepter']").Click());
        grille.WaitForAssertion(() => Assert.NotEmpty(grille.FindAll("[data-testid='cloche-confirmer']")), TimeSpan.FromSeconds(10));

        // When — Échap document : ferme le mini-dialog de confirmation, le panneau reste ouvert, AUCUNE écriture.
        this.SurDispatcher(() => espion.DeclencherEchapDocument().GetAwaiter().GetResult());
        grille.WaitForAssertion(
            () =>
            {
                Assert.Empty(grille.FindAll("[data-testid='cloche-confirmer']"));
                Assert.NotEmpty(grille.FindAll("[data-testid='cloche-panneau']"));
            },
            TimeSpan.FromSeconds(10));
        Assert.Empty(api.Services.GetRequiredService<IPeriodeRepository>().AllSnapshots());
        Assert.Contains(api.Services.GetRequiredService<IPropositionEchangeRepository>().AllSnapshots(),
            p => p.VersActeurId == "parent-a" && p.Statut == StatutProposition.Proposee); // toujours pending
    }
}
