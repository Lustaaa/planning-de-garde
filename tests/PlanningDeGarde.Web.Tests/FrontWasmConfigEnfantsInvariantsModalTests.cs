using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Web.State;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 34 — Sc.6 (🖥️ @ihm — acceptation de NIVEAU RUNTIME, jamais une doublure) : invariants de la
/// surface Enfants (patron s34 Sc.4/Sc.5). Sur l'écran réellement câblé (API distante réelle, store réel,
/// DI réelle, hub SignalR réel) :
/// <list type="number">
///   <item>un prénom refusé par le domaine (doublon d'un autre enfant) — alors qu'un parent est SÉLECTIONNÉ
///   dans le sélecteur — laisse la modal OUVERTE, le motif DEDANS, la saisie (prénom + sélection parents)
///   CONSERVÉE, et le tableau INCHANGÉ (aucune écriture partielle : le lien parent n'est PAS posé en
///   store) ;</item>
///   <item>sous identité EFFECTIVE non-Parent (Invité), l'onglet Enfants reste en LECTURE SEULE (table
///   visible, colonne « Parents liés » consultable) sans crayon ni « Ajouter », aucune modal atteignable ;</item>
///   <item>deux écrans convergent en temps réel : une édition (prénom + lien parent) émise depuis le 2ᵉ
///   écran fait converger la table du 1ᵉʳ (prénom + colonne « Parents liés ») sans rechargement, la
///   diffusion SignalR étant en lecture seule (ré-énumération du store partagé).</item>
/// </list>
/// </summary>
public sealed class FrontWasmConfigEnfantsInvariantsModalTests : TestContext
{
    private static string? Prenom(AngleSharp.Dom.IElement ligne)
        => ligne.QuerySelector(".role-libelle")?.TextContent.Trim();

    private static AngleSharp.Dom.IElement LigneParPrenom(IRenderedComponent<ConfigurationFoyer> config, string prenom)
        => config.FindAll("[data-testid='enfant-foyer']").Single(li => Prenom(li) == prenom);

    private static string ParentsLies(IRenderedComponent<ConfigurationFoyer> config, string prenom)
        => LigneParPrenom(config, prenom).QuerySelector("[data-testid='enfant-parents-lies']")!.TextContent.Trim();

    /// <summary>Sème le rôle « Parent » et l'affecte à parent-a (Alice) : seul un acteur portant ce rôle est
    /// proposé au sélecteur de parents et liable par le domaine (Sc.2 / Sc.5).</summary>
    private static void SemerParentAlice(ApiDistanteFactory api)
    {
        api.Services.GetRequiredService<IEditeurReferentielRoles>().Creer("role-parent", "Parent");
        api.Services.GetRequiredService<IEditeurConfigurationFoyer>().AffecterRole("parent-a", "role-parent");
    }

    private IRenderedComponent<ConfigurationFoyer> RendreConfig(
        Bunit.TestContext ctx, ApiDistanteFactory api, SessionPlanning? session = null, bool hubReel = false)
    {
        ctx.Services.AddSingleton(GrilleRuntimeHarness.ClientVers(api));
        ctx.Services.AddSingleton(session ?? new SessionPlanning());
        if (hubReel)
        {
            ctx.Services.AddSingleton(new OptionsConnexionHub
            {
                Configurer = options =>
                {
                    options.HttpMessageHandlerFactory = _ => api.Server.CreateHandler();
                    options.Transports = HttpTransportType.LongPolling;
                },
            });
        }

        var config = ctx.RenderComponent<ConfigurationFoyer>();
        config.WaitForState(
            () => config.FindAll("[data-testid='enfant-foyer']").Count > 0,
            TimeSpan.FromSeconds(10));
        return config;
    }

    [Fact]
    public void Un_prenom_doublon_avec_un_parent_selectionne_garde_la_modal_ouverte_motif_dedans_saisie_et_selection_conservees_table_inchangee()
    {
        // Given — « Alice » (parent-a) est Parent ; un second enfant « Tom » existe (pour créer un doublon
        // de prénom) ; « Léa » n'a aucun parent lié.
        using var api = new ApiDistanteFactory();
        SemerParentAlice(api);
        api.Services.GetRequiredService<IEditeurEnfants>().Ajouter("tom-id", "Tom");
        var config = RendreConfig(this, api);
        Assert.Equal("—", ParentsLies(config, "Léa"));

        // When — j'ouvre la modal de « Léa », je COCHE Alice (parent-a) dans le sélecteur, puis je change le
        // prénom en « Tom » (doublon d'un AUTRE enfant) et j'enregistre → editer-enfant refusé par le domaine.
        this.SurDispatcher(() => LigneParPrenom(config, "Léa").QuerySelector("[data-testid='crayon-enfant']")!.Click());
        config.WaitForElement("[data-testid='selecteur-parents-enfant']", TimeSpan.FromSeconds(10));
        this.SurDispatcher(() => config.FindAll("[data-testid='checkbox-parent']")
            .Single(c => c.GetAttribute("data-acteur-id") == "parent-a").Change(true));
        this.SurDispatcher(() => config.Find("[data-testid='champ-prenom-enfant']").Change("Tom"));
        this.SurDispatcher(() => config.Find("#form-enfant").Submit());

        // Then — la modal RESTE OUVERTE, le motif de refus est DEDANS, la saisie (prénom « Tom » + Alice
        // toujours cochée) est CONSERVÉE.
        config.WaitForAssertion(
            () =>
            {
                var modal = config.Find("[data-testid='dialog-enfant']");
                Assert.NotNull(modal.QuerySelector("[data-testid='motif-echec-enfant']"));
                Assert.Equal("Tom", modal.QuerySelector("[data-testid='champ-prenom-enfant']")!.GetAttribute("value"));
                Assert.True(config.FindAll("[data-testid='checkbox-parent']")
                    .Single(c => c.GetAttribute("data-acteur-id") == "parent-a").HasAttribute("checked"));
            },
            TimeSpan.FromSeconds(10));

        // … et le tableau est INCHANGÉ : AUCUNE écriture partielle — le lien parent n'a PAS été posé en store
        // (Léa toujours sans parent lié), et « Léa » n'a pas été renommée (toujours 2 enfants distincts).
        Assert.Empty(api.Services.GetRequiredService<IEnumerationEnfants>()
            .EnumererEnfants().Single(e => e.Id == "Léa").ParentsLies);
        Assert.Contains("Léa", api.Services.GetRequiredService<IEnumerationEnfants>()
            .EnumererEnfants().Select(e => e.Prenom));
    }

    [Fact]
    public void Sous_identite_Invite_la_table_des_enfants_reste_en_lecture_seule_sans_crayon_ni_ajouter_ni_modal()
    {
        // Given — « Léa » liée à « Alice » (colonne « Parents liés » consultable) ; écran sous identité
        // effective « Invité » (non-Parent).
        using var api = new ApiDistanteFactory();
        SemerParentAlice(api);
        api.Services.GetRequiredService<IEditeurEnfants>().LierParent("Léa", "parent-a");
        var session = new SessionPlanning { Role = RoleAuteur.Invite };
        var config = RendreConfig(this, api, session);
        Assert.False(session.EstParent); // garde-fou

        // Then — la table reste VISIBLE en LECTURE (« Léa » + sa colonne « Parents liés » résout « Alice »),
        // mais aucune surface d'écriture : ni crayon, ni bouton « Ajouter », ni modal atteignable.
        Assert.NotEmpty(config.FindAll("[data-testid='liste-enfants']"));
        Assert.Equal("Alice (parent)", ParentsLies(config, "Léa"));
        Assert.Empty(config.FindAll("[data-testid='crayon-enfant']"));
        Assert.Empty(config.FindAll("[data-testid='bouton-ajouter-enfant']"));
        Assert.Empty(config.FindAll("[data-testid='dialog-enfant']"));

        // Contrôle positif (anti faux-vert) — sous Parent, crayon (par ligne) et « Ajouter » REDEVIENNENT rendus.
        session.Role = RoleAuteur.Parent;
        config.Render();
        Assert.NotEmpty(config.FindAll("[data-testid='crayon-enfant']"));
        Assert.NotEmpty(config.FindAll("[data-testid='bouton-ajouter-enfant']"));
    }

    [Fact]
    public async Task Deux_ecrans_convergent_sur_edition_prenom_et_lien_parent_sans_rechargement()
    {
        // Given — UNE seule API distante réelle (store singleton partagé, hub réel commun). « Alice »
        // (parent-a) est Parent ; « Léa » sans parent lié au départ, sur les DEUX écrans.
        using var api = new ApiDistanteFactory();
        SemerParentAlice(api);
        var config1 = RendreConfig(this, api, hubReel: true);
        using var ecran2 = new TestContext();
        var config2 = RendreConfig(ecran2, api, hubReel: true);

        // … baseline asserté sur les DEUX écrans : « Léa » présente, aucun parent lié.
        Assert.Equal("—", ParentsLies(config1, "Léa"));
        Assert.Equal("—", ParentsLies(config2, "Léa"));

        // Diffusion de fond idempotente : le push SignalR tombe forcément APRÈS l'établissement des
        // connexions long polling, sans dépendre du timing (convention anti-flake *TempsReel*).
        var notificateur = api.Services.GetRequiredService<INotificateurPlanning>();
        using var diffusionContinue = new CancellationTokenSource();
        var pousseur = Task.Run(async () =>
        {
            while (!diffusionContinue.IsCancellationRequested)
            {
                notificateur.NotifierMiseAJour();
                try { await Task.Delay(150, diffusionContinue.Token); }
                catch (TaskCanceledException) { break; }
            }
        });

        try
        {
            // When (édition depuis l'écran 2) — le 2ᵉ écran ouvre la modal de « Léa », la renomme « Léana »
            // ET lie « Alice », puis enregistre (POST éditer-enfant + lier-enfant-parent réels).
            config2.InvokeAsync(() => LigneParPrenom(config2, "Léa").QuerySelector("[data-testid='crayon-enfant']")!.Click());
            config2.WaitForElement("[data-testid='selecteur-parents-enfant']", TimeSpan.FromSeconds(10));
            config2.InvokeAsync(() => config2.FindAll("[data-testid='checkbox-parent']")
                .Single(c => c.GetAttribute("data-acteur-id") == "parent-a").Change(true));
            config2.InvokeAsync(() => config2.Find("[data-testid='champ-prenom-enfant']").Change("Léana"));
            config2.InvokeAsync(() => config2.Find("#form-enfant").Submit());

            // Then (convergence) — sans rechargement, le 1ᵉʳ écran voit « Léana » avec « Alice » dans la
            // colonne « Parents liés » (relu via SignalR, lecture seule).
            config1.WaitForAssertion(
                () =>
                {
                    Assert.Contains(config1.FindAll("[data-testid='enfant-foyer']"), li => Prenom(li) == "Léana");
                    Assert.Equal("Alice (parent)", ParentsLies(config1, "Léana"));
                },
                TimeSpan.FromSeconds(15));
        }
        finally
        {
            diffusionContinue.Cancel();
            await pousseur;
        }
    }
}
