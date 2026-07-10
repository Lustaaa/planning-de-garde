using System;
using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Web.Components.Pages;
using PlanningDeGarde.Web.State;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 32 — Sc.5 (🖥️ @ihm — acceptation de NIVEAU RUNTIME) : depuis la modal d'édition ouverte sur un
/// acteur (Parent, écran réellement câblé, API distante réelle), tenter d'enregistrer une valeur REFUSÉE
/// par le domaine (nom tout-espaces) via le canal HTTP réel n'écrit rien : la modal RESTE OUVERTE, le
/// motif d'échec métier est affiché DANS la modal, la saisie est CONSERVÉE, et le tableau reste INCHANGÉ
/// (aucune écriture partielle). Le contrat d'erreur est prouvé À LA FRONTIÈRE DE LA MODAL — la surface
/// d'écriture introduite par la refonte s32 (patron crayon → modal).
///
/// <para>Le volet « API injoignable » de Sc.5 (échec de transport → modal ouverte, saisie conservée) est
/// prouvé par les tests frères de niveau runtime <c>FrontWasmConfigApiInjoignable…</c> (édition) et
/// <c>FrontWasmConfigAjouterServiceInjoignable…</c> (ajout), désormais menés dans la modal ; on ne
/// redouble pas ici la voie transport. Ce test borne le refus DOMAINE sur le parcours d'édition modal.</para>
/// </summary>
public sealed class FrontWasmConfigActeursModalRefusResteOuverteTests : TestContext
{
    [Fact]
    public void Un_refus_domaine_depuis_la_modal_laisse_la_modal_ouverte_avec_le_motif_dedans_la_saisie_conservee_et_la_table_inchangee()
    {
        // Given — l'écran de configuration réellement câblé, identité Parent, modal d'édition ouverte sur
        // parent-a (Alice).
        using var api = new ApiDistanteFactory();
        Services.AddSingleton(GrilleRuntimeHarness.ClientVers(api));
        Services.AddSingleton(new SessionPlanning());

        var config = RenderComponent<ConfigurationFoyer>();
        ConfigActeursModalHarness.OuvrirEdition(this, config, "parent-a");

        // When — je saisis un nom tout-espaces (vide utile, refusé par le domaine « le nom ne peut pas être
        // vide ») SANS recoloriage, puis j'enregistre (POST réel /api/canal/editer-acteur → refus métier).
        this.SurDispatcher(() => config.Find("[data-testid='champ-nom']").Change("   "));
        this.SurDispatcher(() => config.Find("#form-edition").Submit());

        // Then — la modal RESTE OUVERTE, le motif d'échec est affiché DANS la modal, la saisie «    » est
        // CONSERVÉE dans le champ nom, et la table reste INCHANGÉE (Alice, aucune écriture partielle).
        config.WaitForAssertion(
            () =>
            {
                var modal = config.Find("[data-testid='dialog-acteur']");
                var motif = modal.QuerySelector("[data-testid='motif-echec']");
                Assert.NotNull(motif); // motif DANS la modal
                Assert.Equal("le nom ne peut pas être vide", motif!.TextContent.Trim());
                Assert.Equal("   ", modal.QuerySelector("[data-testid='champ-nom']")!.GetAttribute("value")); // saisie conservée
            },
            TimeSpan.FromSeconds(10));

        // … et le tableau reste inchangé : parent-a s'affiche toujours « Alice » (aucune écriture partielle).
        Assert.Equal("Alice", config.FindAll("[data-testid='acteur-foyer']")
            .Single(li => li.GetAttribute("data-acteur-id") == "parent-a")
            .QuerySelector(".acteur-nom")!.TextContent.Trim());
    }
}
