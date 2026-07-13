using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Web.Components.Pages;
using PlanningDeGarde.Web.State;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 35 — Sc.6 (🖥️ @ihm — acceptation de NIVEAU RUNTIME, jamais une doublure) : invariants de la
/// surface Activités (patron s35 Sc.4/Sc.5). Sur l'écran réellement câblé (API distante réelle, store réel,
/// DI réelle, hub SignalR réel) :
/// <list type="number">
///   <item>un libellé refusé par le domaine (vidé → « libellé requis ») — alors qu'un enfant est SÉLECTIONNÉ
///   dans le sélecteur — laisse la modal OUVERTE, le motif DEDANS, la saisie (libellé + adresse + sélection
///   d'enfants) CONSERVÉE, et le tableau INCHANGÉ (aucune écriture partielle : le lien enfant n'est PAS posé
///   en store, editer-activite ayant échoué en premier) ;</item>
///   <item>sous identité EFFECTIVE non-Parent (Invité), l'onglet Activités reste en LECTURE SEULE (table
///   visible, colonne « Enfants liés » consultable) sans crayon ni « Ajouter », aucune modal atteignable ;</item>
///   <item>deux écrans convergent en temps réel : une édition (libellé + lien enfant) émise depuis le 2ᵉ écran
///   fait converger la table du 1ᵉʳ (libellé + colonne « Enfants liés ») sans rechargement, la diffusion
///   SignalR étant en lecture seule (ré-énumération du store partagé).</item>
/// </list>
/// </summary>
public sealed class FrontWasmConfigActivitesInvariantsModalTests : TestContext
{
    private static string? Libelle(AngleSharp.Dom.IElement ligne)
        => ligne.QuerySelector(".role-libelle")?.TextContent.Trim();

    private static AngleSharp.Dom.IElement LigneParLibelle(IRenderedComponent<ConfigurationFoyer> config, string libelle)
        => config.FindAll("[data-testid='activite-foyer']").Single(li => Libelle(li) == libelle);

    private static string EnfantsLies(IRenderedComponent<ConfigurationFoyer> config, string libelle)
        => LigneParLibelle(config, libelle).QuerySelector("[data-testid='activite-enfants-lies']")!.TextContent.Trim();

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
            () => config.FindAll("[data-testid='activite-foyer']").Count > 0,
            TimeSpan.FromSeconds(10));
        return config;
    }

    [Fact]
    public void Un_libelle_vide_avec_un_enfant_selectionne_garde_la_modal_ouverte_motif_dedans_saisie_et_selection_conservees_table_inchangee()
    {
        // Given — l'activité « école » sans enfant lié ; l'enfant « Léa » (seed).
        using var api = new ApiDistanteFactory();
        var config = RendreConfig(this, api);
        Assert.Equal("—", EnfantsLies(config, "école"));

        // When — j'ouvre la modal de « école », je COCHE « Léa » dans le sélecteur, je saisis une adresse, puis
        // je VIDE le libellé et j'enregistre → editer-activite refusé par le domaine (« libellé requis »).
        this.SurDispatcher(() => LigneParLibelle(config, "école").QuerySelector("[data-testid='crayon-activite']")!.Click());
        config.WaitForElement("[data-testid='selecteur-enfants-activite']", TimeSpan.FromSeconds(10));
        this.SurDispatcher(() => config.FindAll("[data-testid='checkbox-enfant-activite']")
            .Single(c => c.GetAttribute("data-enfant-id") == "Léa").Change(true));
        this.SurDispatcher(() => config.Find("[data-testid='champ-adresse-activite']").Change("7 rue du Test"));
        this.SurDispatcher(() => config.Find("[data-testid='champ-libelle-activite']").Change(""));
        this.SurDispatcher(() => config.Find("#form-activite").Submit());

        // Then — la modal RESTE OUVERTE, le motif de refus est DEDANS, la saisie (libellé vidé + adresse
        // « 7 rue du Test » + « Léa » toujours cochée) est CONSERVÉE.
        config.WaitForAssertion(
            () =>
            {
                var modal = config.Find("[data-testid='dialog-activite']");
                Assert.NotNull(modal.QuerySelector("[data-testid='motif-echec-activite']"));
                Assert.Equal("", modal.QuerySelector("[data-testid='champ-libelle-activite']")!.GetAttribute("value"));
                Assert.Equal("7 rue du Test", modal.QuerySelector("[data-testid='champ-adresse-activite']")!.GetAttribute("value"));
                Assert.True(config.FindAll("[data-testid='checkbox-enfant-activite']")
                    .Single(c => c.GetAttribute("data-enfant-id") == "Léa").HasAttribute("checked"));
            },
            TimeSpan.FromSeconds(10));

        // … et le tableau est INCHANGÉ : AUCUNE écriture partielle — le lien enfant n'a PAS été posé en store
        // (école toujours sans enfant lié), et l'adresse refusée n'a pas été écrite.
        var ecole = api.Services.GetRequiredService<IEnumerationActivites>()
            .EnumererActivites().Single(a => a.Id == "école");
        Assert.Empty(ecole.EnfantsLies);
        Assert.Equal("", ecole.Adresse);
    }

    [Fact]
    public void Sous_identite_Invite_la_table_des_activites_reste_en_lecture_seule_sans_crayon_ni_ajouter_ni_modal()
    {
        // Given — « école » liée à « Léa » (colonne « Enfants liés » consultable) ; écran sous identité
        // effective « Invité » (non-Parent).
        using var api = new ApiDistanteFactory();
        api.Services.GetRequiredService<IEditeurActivites>().LierEnfant("école", "Léa");
        var session = new SessionPlanning { Role = RoleAuteur.Invite };
        var config = RendreConfig(this, api, session);
        Assert.False(session.EstParent); // garde-fou

        // Then — la table reste VISIBLE en LECTURE (« école » + sa colonne « Enfants liés » résout « Léa »),
        // mais aucune surface d'écriture : ni crayon, ni bouton « Ajouter », ni modal atteignable.
        Assert.NotEmpty(config.FindAll("[data-testid='liste-activites']"));
        Assert.Equal("Léa", EnfantsLies(config, "école"));
        Assert.Empty(config.FindAll("[data-testid='crayon-activite']"));
        Assert.Empty(config.FindAll("[data-testid='bouton-ajouter-activite']"));
        Assert.Empty(config.FindAll("[data-testid='dialog-activite']"));

        // Contrôle positif (anti faux-vert) — sous Parent, crayon (par ligne) et « Ajouter » REDEVIENNENT rendus.
        session.Role = RoleAuteur.Parent;
        config.Render();
        Assert.NotEmpty(config.FindAll("[data-testid='crayon-activite']"));
        Assert.NotEmpty(config.FindAll("[data-testid='bouton-ajouter-activite']"));
    }

    [Fact]
    public async Task Deux_ecrans_convergent_sur_edition_libelle_et_lien_enfant_sans_rechargement()
    {
        // Given — UNE seule API distante réelle (store singleton partagé, hub réel commun). « école » sans
        // enfant lié au départ, sur les DEUX écrans.
        using var api = new ApiDistanteFactory();
        var config1 = RendreConfig(this, api, hubReel: true);
        using var ecran2 = new TestContext();
        var config2 = RendreConfig(ecran2, api, hubReel: true);

        // … baseline asserté sur les DEUX écrans : « école » présente, aucun enfant lié.
        Assert.Equal("—", EnfantsLies(config1, "école"));
        Assert.Equal("—", EnfantsLies(config2, "école"));

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
            // When (édition depuis l'écran 2) — le 2ᵉ écran ouvre la modal de « école », la renomme
            // « école élémentaire » ET lie « Léa », puis enregistre (POST éditer-activite + lier-enfant-activite réels).
            config2.InvokeAsync(() => LigneParLibelle(config2, "école").QuerySelector("[data-testid='crayon-activite']")!.Click());
            config2.WaitForElement("[data-testid='selecteur-enfants-activite']", TimeSpan.FromSeconds(10));
            config2.InvokeAsync(() => config2.FindAll("[data-testid='checkbox-enfant-activite']")
                .Single(c => c.GetAttribute("data-enfant-id") == "Léa").Change(true));
            config2.InvokeAsync(() => config2.Find("[data-testid='champ-libelle-activite']").Change("école élémentaire"));
            config2.InvokeAsync(() => config2.Find("#form-activite").Submit());

            // Then (convergence) — sans rechargement, le 1ᵉʳ écran voit « école élémentaire » avec « Léa » dans
            // la colonne « Enfants liés » (relu via SignalR, lecture seule).
            config1.WaitForAssertion(
                () =>
                {
                    Assert.Contains(config1.FindAll("[data-testid='activite-foyer']"), li => Libelle(li) == "école élémentaire");
                    Assert.Equal("Léa", EnfantsLies(config1, "école élémentaire"));
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
