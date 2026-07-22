using System;
using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Web.Components.Pages;
using PlanningDeGarde.Web.State;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Acceptation de NIVEAU RUNTIME du Sc.8 (🖥️ IHM, <c>@erreur</c>) — VOLET RUNTIME de l'AJOUT : depuis
/// l'<b>écran de configuration réellement câblé</b> (<see cref="ConfigurationFoyer"/>), un parent tente
/// d'ajouter un acteur en laissant le <b>nom vide</b> et valide. L'écran — câblé à l'<b>API distante
/// réelle</b> (<see cref="ApiDistanteFactory"/>, store réel <c>ConfigurationFoyerEnMemoire</c>, énumération
/// réelle) — émet l'ajout via le <b>canal d'écriture HTTP réel</b> (<c>POST /api/foyer/acteurs</c>) ;
/// le handler applique sa garde « nom non vide » et renvoie un <c>Result.Echec</c> dont le <b>motif métier</b>
/// (« le nom ne peut pas être vide ») doit être <b>surfacé à l'écran</b>. Aucun identifiant n'est généré, le
/// store n'est pas muté : la liste des acteurs <b>reste inchangée</b>, sans rechargement.
///
/// Anti « vert qui ment » : le baseline (liste énumérée depuis le store) est asserté avant la tentative, et le
/// message d'erreur est observé sur le DOM réellement rendu. Si l'écran avalait silencieusement le refus de
/// l'API (cas actuel : <c>Ajouter</c> ne lit pas la réponse), aucun message n'apparaîtrait → rouge. Un bUnit à
/// doublure ne prouverait ni le refus via le canal HTTP réel, ni le rendu du message.
/// </summary>
[Collection("SignalRTempsReel")]
public sealed class FrontWasmConfigAjouterSansNomRefuseTempsReelTests : TestContext
{
    [Fact]
    public void Should_Refuser_l_ajout_avec_le_message_le_nom_ne_peut_pas_etre_vide_et_laisser_la_liste_des_acteurs_inchangee_When_un_parent_valide_un_ajout_au_nom_vide()
    {
        // Given — l'écran de configuration réellement câblé à l'API distante réelle (store réel seedé,
        // énumération réelle). L'écran énumère les acteurs DEPUIS LE STORE (GET HTTP réel) : la liste se peuple.
        using var api = new ApiDistanteFactory();
        Services.AddSingleton(GrilleRuntimeHarness.ClientVers(api));
        Services.AddSingleton(new SessionPlanning()); // contexte rôle réel (Parent par défaut) requis par l'écran (gating Sc.7)

        var config = RenderComponent<ConfigurationFoyer>();

        config.WaitForState(
            () => config.FindAll("[data-testid='acteur-foyer']").Count > 0,
            TimeSpan.FromSeconds(10));

        // … baseline : la liste compte N acteurs, aucun acteur au nom vide (pas de fantôme préexistant).
        var nombreInitial = config.FindAll("[data-testid='acteur-foyer']").Count;
        Assert.DoesNotContain(
            config.FindAll("[data-testid='acteur-foyer']"),
            li => string.IsNullOrWhiteSpace(li.QuerySelector(".acteur-nom")?.TextContent));

        // When — un parent ouvre la modal d'ajout (refonte s32) et valide en laissant le nom vide (émission
        // via le canal d'écriture HTTP réel). Sur refus, la modal RESTE OUVERTE avec le motif dedans.
        ConfigActeursModalHarness.OuvrirAjout(this, config);
        this.SurDispatcher(() => config.Find("#form-ajout").Submit());

        // Then — l'ajout est refusé : le motif métier renvoyé par l'API est affiché clairement à l'écran.
        config.WaitForAssertion(
            () => Assert.Equal(
                "le nom ne peut pas être vide",
                config.Find("[data-testid='motif-echec-ajout']").TextContent.Trim()),
            TimeSpan.FromSeconds(15));

        // … et la liste des acteurs reste inchangée : aucun identifiant généré, aucun acteur fantôme ajouté.
        Assert.Equal(nombreInitial, config.FindAll("[data-testid='acteur-foyer']").Count);
        Assert.DoesNotContain(
            config.FindAll("[data-testid='acteur-foyer']"),
            li => string.IsNullOrWhiteSpace(li.QuerySelector(".acteur-nom")?.TextContent));
    }
}
