using System;
using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Web.State;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Acceptation de NIVEAU RUNTIME du Sc.7 (🖥️ IHM, <c>@erreur</c>) — gating Invité (règle 9) du
/// bouton supprimer posé au Sc.6, sur l'<b>écran de configuration réellement câblé</b>
/// (<see cref="ConfigurationFoyer"/>, DI réelle, API distante réelle <see cref="ApiDistanteFactory"/>,
/// store réel, contexte rôle réel <see cref="SessionPlanning"/>). Le bouton supprimer est <b>sous le
/// garde de rôle mutualisé</b> (<see cref="SessionPlanning.EstParent"/>) : un Invité en consultation
/// seule ne le voit pas, ne peut émettre aucune commande de suppression, et la liste reste inchangée.
///
/// Anti « vert qui ment » : <b>contrôle positif en regard</b> dans le même test — on prouve d'abord
/// qu'un <b>Parent</b> voit bien les boutons supprimer (le mécanisme n'est pas cassé pour tous), puis on
/// bascule en <b>Invité</b> et on prouve qu'<b>aucun</b> bouton supprimer n'est rendu et que la liste
/// des acteurs est inchangée. Sans le contrôle positif, l'absence de bouton serait un faux vert. Un
/// bUnit à doublure forçant l'interactivité ne prouverait pas le garde de rôle réel.
/// </summary>
[Collection("SignalRTempsReel")]
public sealed class FrontWasmConfigInviteNeSupprimePasTempsReelTests : TestContext
{
    [Fact]
    public void Should_Ne_proposer_aucun_bouton_supprimer_et_laisser_la_liste_inchangee_When_un_invite_en_consultation_seule_affiche_l_ecran_de_configuration()
    {
        // Given — l'écran de configuration réellement câblé à l'API distante réelle (store réel, énumération
        // réelle), avec un contexte de rôle réel. On part d'un Parent (contrôle positif).
        using var api = new ApiDistanteFactory();
        Services.AddSingleton(GrilleRuntimeHarness.ClientVers(api));
        var session = new SessionPlanning { Role = RoleAuteur.Parent };
        Services.AddSingleton(session);

        var config = RenderComponent<ConfigurationFoyer>();

        // … l'écran énumère les acteurs DEPUIS LE STORE (GET HTTP réel asynchrone) : la liste se peuple.
        config.WaitForState(
            () => config.FindAll("[data-testid='acteur-foyer']").Count > 0,
            TimeSpan.FromSeconds(10));
        var nombreActeurs = config.FindAll("[data-testid='acteur-foyer']").Count;

        // Contrôle positif — refonte s32 : le Parent voit un crayon d'édition par acteur (entrée d'écriture)
        // et, dans la modal ouverte, l'action supprimer est atteignable (le mécanisme n'est pas cassé pour tous).
        Assert.Equal(nombreActeurs, config.FindAll("[data-testid='crayon-acteur']").Count);
        ConfigActeursModalHarness.OuvrirEdition(this, config, "parent-a");
        Assert.NotEmpty(config.FindAll("[data-testid='bouton-supprimer']"));
        this.SurDispatcher(() => config.Find("[data-testid='dialog-acteur-annuler']").Click());

        // When — le rôle bascule en Invité (consultation seule) et l'écran est re-rendu.
        session.Role = RoleAuteur.Invite;
        config.Render();

        // Then — aucun crayon ni bouton d'ajout n'est proposé (aucune modal d'écriture atteignable, donc
        // aucune commande de suppression émissible) …
        Assert.Empty(config.FindAll("[data-testid='crayon-acteur']"));
        Assert.Empty(config.FindAll("[data-testid='bouton-supprimer']"));
        Assert.Empty(config.FindAll("[data-testid='bouton-ajouter-acteur']"));

        // … et la liste des acteurs reste inchangée (même nombre, lecture seule préservée).
        Assert.Equal(nombreActeurs, config.FindAll("[data-testid='acteur-foyer']").Count);
    }
}
