using System;
using System.Collections.Generic;
using System.IO;
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
/// Sprint 33 — finition PO (rework in-goal, aucun handler/commande/invariant neuf) : les modals de la Config
/// foyer se ferment à la touche <b>Échap</b>, strictement comme « Annuler » — fermeture SANS aucune mutation
/// (aucune commande émise, saisie abandonnée), jamais confondu avec « Enregistrer ».
///
/// <para><b>Correctif anti vert-qui-ment.</b> Une 1ʳᵉ version câblait <c>@onkeydown</c> sur le backdrop : bUnit
/// dispatche le keydown DIRECTEMENT sur l'élément porteur, mais en navigateur réel l'événement part de
/// <c>document</c> et n'atteint jamais une modal non focus → les modals ne se fermaient pas. La capture est
/// désormais au niveau <b>document</b> via le port <see cref="IEcouteurEchapModal"/> (module JS
/// <c>window.pdgModal</c>), factorisé dans le composant commun <c>ModalConfig</c>.</para>
///
/// <para><b>Ce que ces tests prouvent (et leurs limites).</b> Le port est <b>doublé à la main</b> (spy) : on
/// vérifie que (1) l'ouverture d'une modal ATTACHE l'écouteur document (contrat câblé), (2) quand cet écouteur
/// se déclenche (Échap document, simulé via le callback capté par le spy) la modal se ferme SANS muter le
/// store réel, (3) la fermeture DÉTACHE l'écouteur (pas de fuite). Ce qui n'est PAS exécuté ici : le
/// <c>document.addEventListener</c> JS lui-même — non simulable hors navigateur. Il est couvert par la garde
/// d'asset (ci-dessous) qui vérifie le module JS, et la preuve finale reste le <b>gate navigateur du PO</b>.
/// Les onglets Acteurs, Rôles et Cycle passent par le MÊME <c>ModalConfig</c>, donc le même mécanisme.</para>
/// </summary>
public sealed class FrontWasmConfigModalsEchapFermeSansMutationTests : TestContext
{
    /// <summary>Double à la main du port d'écoute Échap (seul le port est doublé) : capte le callback fourni à
    /// l'attache et compte attaches/détaches. <see cref="DeclencherEchapDocument"/> rejoue ce que ferait le
    /// listener JS document sur un appui Échap réel.</summary>
    private sealed class EspionEchap : IEcouteurEchapModal
    {
        private Func<Task>? _onEchap;

        public int Attachements { get; private set; }
        public int Detachements { get; private set; }

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

            public ValueTask DisposeAsync()
            {
                _espion.Detachements++;
                _espion._onEchap = null;
                return ValueTask.CompletedTask;
            }
        }
    }

    private (IRenderedComponent<ConfigurationFoyer> config, EspionEchap espion) RendreConfig(ApiDistanteFactory api)
    {
        var espion = new EspionEchap();
        Services.AddSingleton(GrilleRuntimeHarness.ClientVers(api));
        Services.AddSingleton(new SessionPlanning());
        Services.AddSingleton<IEcouteurEchapModal>(espion);
        var config = RenderComponent<ConfigurationFoyer>();
        config.WaitForState(
            () => config.FindAll("[data-testid='acteur-foyer']").Count > 0,
            TimeSpan.FromSeconds(10));
        return (config, espion);
    }

    [Fact]
    public void Echap_document_sur_la_modal_Acteur_la_ferme_sans_muter_le_store()
    {
        // Given — écran Parent réellement câblé, modal d'édition ouverte sur parent-a (Alice) ; je modifie le nom.
        using var api = new ApiDistanteFactory();
        var (config, espion) = RendreConfig(api);
        ConfigActeursModalHarness.OuvrirEdition(this, config, "parent-a");
        // La modal ouverte a ATTACHÉ l'écouteur document (contrat câblé).
        config.WaitForAssertion(() => Assert.Equal(1, espion.Attachements), TimeSpan.FromSeconds(10));
        this.SurDispatcher(() => config.Find("[data-testid='champ-nom']").Change("ZZZ modifié non enregistré"));

        // When — l'écouteur document se déclenche (Échap réel simulé au bon niveau : callback capté par le spy).
        this.SurDispatcher(() => espion.DeclencherEchapDocument().GetAwaiter().GetResult());

        // Then — la modal se ferme, l'écouteur est DÉTACHÉ (pas de fuite), et le store réel porte toujours « Alice ».
        config.WaitForAssertion(
            () =>
            {
                Assert.Empty(config.FindAll("[data-testid='dialog-acteur']"));
                Assert.Equal(1, espion.Detachements);
            },
            TimeSpan.FromSeconds(10));
        var nom = api.Services.GetRequiredService<IReferentielResponsables>().NomDe("parent-a");
        Assert.Equal("Alice", nom);
    }

    [Fact]
    public void Echap_document_sur_la_modal_Role_la_ferme_sans_muter_le_referentiel()
    {
        // Given — le rôle « Nounou » est présent au référentiel (seed B2, s36) ; modal d'édition ouverte
        // dessus ; je modifie le libellé.
        using var api = new ApiDistanteFactory();
        var (config, espion) = RendreConfig(api);
        config.WaitForState(() => config.FindAll("[data-testid='role-foyer']").Count >= 1, TimeSpan.FromSeconds(10));

        this.SurDispatcher(() => config.FindAll("[data-testid='crayon-role']")
            .Single(b => b.GetAttribute("data-role-id") == "role-nounou").Click());
        config.WaitForElement("[data-testid='dialog-role']", TimeSpan.FromSeconds(10));
        config.WaitForAssertion(() => Assert.Equal(1, espion.Attachements), TimeSpan.FromSeconds(10));
        this.SurDispatcher(() => config.Find("[data-testid='champ-libelle-role']").Change("Renommage non enregistré"));

        // When — Échap document.
        this.SurDispatcher(() => espion.DeclencherEchapDocument().GetAwaiter().GetResult());

        // Then — la modal se ferme, l'écouteur est détaché, et le référentiel porte toujours « Nounou ».
        config.WaitForAssertion(
            () =>
            {
                Assert.Empty(config.FindAll("[data-testid='dialog-role']"));
                Assert.Equal(1, espion.Detachements);
            },
            TimeSpan.FromSeconds(10));
        var roles = api.Services.GetRequiredService<IEnumerationRoles>().EnumererRoles();
        Assert.Equal("Nounou", roles.Single(r => r.Id == "role-nounou").Libelle);
    }

    [Fact]
    public void Echap_document_sur_la_modal_Cycle_la_ferme_sans_muter_le_cycle()
    {
        // Given — un cycle N=2 est déclaré (parent-a semaine 0, parent-b semaine 1) ; modal d'édition ouverte ;
        // je réaffecte la semaine 1 dans la saisie (non enregistrée).
        using var api = new ApiDistanteFactory();
        api.Services.GetRequiredService<IReferentielCycleDeFond>()
            .DefinirCycle(new CycleDeFond(2, new Dictionary<int, string> { [0] = "parent-a", [1] = "parent-b" }));
        var (config, espion) = RendreConfig(api);
        config.WaitForState(() => config.FindAll("[data-testid='cycle-foyer']").Count == 2, TimeSpan.FromSeconds(10));

        this.SurDispatcher(() => config.Find("[data-testid='crayon-cycle']").Click());
        config.WaitForElement("[data-testid='dialog-cycle']", TimeSpan.FromSeconds(10));
        config.WaitForAssertion(() => Assert.Equal(1, espion.Attachements), TimeSpan.FromSeconds(10));
        this.SurDispatcher(() => config.Find("[data-testid='champ-cycle-index-1']").Change("parent-a"));

        // When — Échap document.
        this.SurDispatcher(() => espion.DeclencherEchapDocument().GetAwaiter().GetResult());

        // Then — la modal se ferme, l'écouteur est détaché, et le store cycle porte toujours parent-b en semaine 1.
        config.WaitForAssertion(
            () =>
            {
                Assert.Empty(config.FindAll("[data-testid='dialog-cycle']"));
                Assert.Equal(1, espion.Detachements);
            },
            TimeSpan.FromSeconds(10));
        var cycle = api.Services.GetRequiredService<IReferentielCycleDeFond>().CycleCourant()!;
        Assert.Equal("parent-b", cycle.Affectations[1]);
    }

    [Fact]
    public void Echap_document_sur_la_modal_Enfant_la_ferme_sans_muter_le_referentiel()
    {
        // Given — l'enfant « Léa » est semé ; modal d'édition ouverte dessus (crayon) ; je modifie le prénom.
        using var api = new ApiDistanteFactory();
        var (config, espion) = RendreConfig(api);
        config.WaitForState(() => config.FindAll("[data-testid='enfant-foyer']").Count >= 1, TimeSpan.FromSeconds(10));

        this.SurDispatcher(() => config.FindAll("[data-testid='crayon-enfant']")
            .Single(b => b.GetAttribute("data-enfant-id") == "Léa").Click());
        config.WaitForElement("[data-testid='dialog-enfant']", TimeSpan.FromSeconds(10));
        config.WaitForAssertion(() => Assert.Equal(1, espion.Attachements), TimeSpan.FromSeconds(10));
        this.SurDispatcher(() => config.Find("[data-testid='champ-prenom-enfant']").Change("Renommage non enregistré"));

        // When — Échap document.
        this.SurDispatcher(() => espion.DeclencherEchapDocument().GetAwaiter().GetResult());

        // Then — la modal se ferme, l'écouteur est détaché, et le référentiel porte toujours « Léa ».
        config.WaitForAssertion(
            () =>
            {
                Assert.Empty(config.FindAll("[data-testid='dialog-enfant']"));
                Assert.Equal(1, espion.Detachements);
            },
            TimeSpan.FromSeconds(10));
        var enfants = api.Services.GetRequiredService<IEnumerationEnfants>().EnumererEnfants();
        Assert.Single(enfants, e => e.Prenom == "Léa");
    }

    [Fact]
    public void Echap_document_sur_la_modal_Activite_la_ferme_sans_muter_le_referentiel()
    {
        // Given — l'activité « école » est semée (référentiel) ; modal d'édition ouverte dessus (crayon) ;
        // je modifie le libellé.
        using var api = new ApiDistanteFactory();
        var (config, espion) = RendreConfig(api);
        config.WaitForState(() => config.FindAll("[data-testid='activite-foyer']").Count >= 1, TimeSpan.FromSeconds(10));

        this.SurDispatcher(() => config.FindAll("[data-testid='crayon-activite']")
            .Single(b => b.GetAttribute("data-activite-id") == "école").Click());
        config.WaitForElement("[data-testid='dialog-activite']", TimeSpan.FromSeconds(10));
        config.WaitForAssertion(() => Assert.Equal(1, espion.Attachements), TimeSpan.FromSeconds(10));
        this.SurDispatcher(() => config.Find("[data-testid='champ-libelle-activite']").Change("Renommage non enregistré"));

        // When — Échap document.
        this.SurDispatcher(() => espion.DeclencherEchapDocument().GetAwaiter().GetResult());

        // Then — la modal se ferme, l'écouteur est détaché, et le référentiel porte toujours « école ».
        config.WaitForAssertion(
            () =>
            {
                Assert.Empty(config.FindAll("[data-testid='dialog-activite']"));
                Assert.Equal(1, espion.Detachements);
            },
            TimeSpan.FromSeconds(10));
        var activites = api.Services.GetRequiredService<IEnumerationActivites>().EnumererActivites();
        Assert.Single(activites, a => a.Libelle == "école");
    }

    [Fact]
    public void Echap_document_ferme_meme_une_modal_en_etat_de_refus_sans_rien_reemettre()
    {
        // Given — invariant : « Nounou » existe (seed B2, s36) ; on ouvre l'ajout et on tente le doublon
        // « Nounou » → REFUS (motif affiché, saisie conservée, modal restée ouverte).
        using var api = new ApiDistanteFactory();
        var nbRolesAvant = api.Services.GetRequiredService<IEnumerationRoles>().EnumererRoles().Count;
        var (config, espion) = RendreConfig(api);
        config.WaitForState(() => config.FindAll("[data-testid='role-foyer']").Count >= 1, TimeSpan.FromSeconds(10));

        this.SurDispatcher(() => config.Find("[data-testid='bouton-ajouter-role']").Click());
        this.SurDispatcher(() => config.Find("[data-testid='champ-libelle-role']").Change("Nounou"));
        this.SurDispatcher(() => config.Find("#form-role").Submit());
        config.WaitForElement("[data-testid='motif-echec-role']", TimeSpan.FromSeconds(10));

        // When — Échap document sur la modal EN ÉTAT DE REFUS.
        this.SurDispatcher(() => espion.DeclencherEchapDocument().GetAwaiter().GetResult());

        // Then — Échap ferme quand même (= annuler) et n'a rien réémis : le référentiel est INCHANGÉ
        // (aucun rôle gagné, toujours un unique « Nounou »).
        config.WaitForAssertion(
            () => Assert.Empty(config.FindAll("[data-testid='dialog-role']")),
            TimeSpan.FromSeconds(10));
        var rolesApres = api.Services.GetRequiredService<IEnumerationRoles>().EnumererRoles();
        Assert.Equal(nbRolesAvant, rolesApres.Count);
        Assert.Single(rolesApres, r => r.Libelle == "Nounou");
    }

    // ── Garde d'asset : le module JS window.pdgModal capte Échap au niveau DOCUMENT et le détache (miroir de
    //    la garde pdgTheme). C'est ce module — non exécutable hors navigateur — qui porte l'effet réel prouvé
    //    au gate PO ; les tests ci-dessus prouvent le contrat .NET du port qui le pilote.
    private static string LireIndexHtml()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "PlanningDeGarde.slnx")))
            dir = dir.Parent;
        Assert.NotNull(dir);
        return File.ReadAllText(Path.Combine(dir!.FullName, "src", "PlanningDeGarde.Web", "wwwroot", "index.html"));
    }

    [Fact]
    public void Le_module_pdgModal_capte_Echap_au_niveau_document_et_le_detache()
    {
        var html = LireIndexHtml().Replace(" ", string.Empty).Replace("\r", string.Empty).Replace("\n", string.Empty);
        Assert.Contains("pdgModal", html, StringComparison.Ordinal);
        // Capture au niveau DOCUMENT (jamais un div à focus) et filtre Échap.
        Assert.Contains("document.addEventListener('keydown'", html, StringComparison.Ordinal);
        Assert.Contains("'Escape'", html, StringComparison.Ordinal);
        Assert.Contains("invokeMethodAsync('Declencher')", html, StringComparison.Ordinal);
        // Détache le MÊME listener (pas de fuite).
        Assert.Contains("document.removeEventListener('keydown'", html, StringComparison.Ordinal);
    }
}
