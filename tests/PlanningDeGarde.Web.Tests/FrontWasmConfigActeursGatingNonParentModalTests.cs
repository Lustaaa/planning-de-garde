using System;
using System.Collections.Generic;
using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Web.Components.Pages;
using PlanningDeGarde.Web.State;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 32 — Sc.6 (🖥️ @ihm — acceptation de NIVEAU RUNTIME) : sous une identité EFFECTIVE non-Parent
/// (Invité, ou incarnation d'un acteur « Autre »), l'onglet « Acteurs » de l'écran réellement câblé
/// (<see cref="ConfigurationFoyer"/>, API distante réelle, store réel) garde la table des acteurs visible
/// en LECTURE SEULE (consultation préservée), mais NE rend NI crayon d'édition, NI bouton « Ajouter un
/// acteur », et AUCUNE modal d'écriture n'est atteignable — le gating règle 9 (identité effective) est lu
/// sur la surface d'écriture introduite par la refonte s32 (crayon → modal).
///
/// <para>Contrôle positif (anti faux-vert) : sous l'identité Parent, crayon et bouton d'ajout REDEVIENNENT
/// rendus — sinon leur absence serait un faux vert (surface cassée pour tous). Déterministe (aucun hub
/// SignalR câblé) : le gating se lit sur l'identité effective, jamais un effet de temps réel. Le gating par
/// onglet / sur les autres surfaces (durcissement s14/s20) est couvert par les tests frères
/// <c>FrontWasmConfigGatingInvite…</c> / <c>FrontWasmConfigGatingAutreIncarne…</c>.</para>
/// </summary>
public sealed class FrontWasmConfigActeursGatingNonParentModalTests : TestContext
{
    private IRenderedComponent<ConfigurationFoyer> RendreSous(ApiDistanteFactory api, SessionPlanning session)
    {
        Services.AddSingleton(GrilleRuntimeHarness.ClientVers(api));
        Services.AddSingleton(session);
        var config = RenderComponent<ConfigurationFoyer>();
        config.WaitForState(
            () => config.FindAll("[data-testid='acteur-foyer']").Count > 0,
            TimeSpan.FromSeconds(10));
        return config;
    }

    private static void AssertLectureSeuleSansEcriture(IRenderedComponent<ConfigurationFoyer> config)
    {
        // La table reste visible en LECTURE (consultation préservée) …
        Assert.NotEmpty(config.FindAll("[data-testid='liste-acteurs']"));
        Assert.NotEmpty(config.FindAll("[data-testid='acteur-foyer']"));
        // … mais aucune surface d'écriture : ni crayon, ni bouton d'ajout, ni modal atteignable.
        Assert.Empty(config.FindAll("[data-testid='crayon-acteur']"));
        Assert.Empty(config.FindAll("[data-testid='bouton-ajouter-acteur']"));
        Assert.Empty(config.FindAll("[data-testid='dialog-acteur']"));
    }

    [Fact]
    public void Un_Invite_garde_la_table_en_lecture_seule_sans_crayon_ni_bouton_ajouter_ni_modal_atteignable()
    {
        // Given — l'écran de configuration réellement câblé, sous identité effective « Invité » (non-Parent).
        using var api = new ApiDistanteFactory();
        var session = new SessionPlanning { Role = RoleAuteur.Invite };
        var config = RendreSous(api, session);
        Assert.False(session.EstParent); // garde-fou

        // Then — table en lecture seule, aucune surface d'écriture d'acteur.
        AssertLectureSeuleSansEcriture(config);

        // Contrôle positif (anti faux-vert) — sous Parent, crayon (par ligne) et bouton d'ajout REDEVIENNENT rendus.
        session.Role = RoleAuteur.Parent;
        config.Render();
        Assert.Equal(config.FindAll("[data-testid='acteur-foyer']").Count, config.FindAll("[data-testid='crayon-acteur']").Count);
        Assert.NotEmpty(config.FindAll("[data-testid='bouton-ajouter-acteur']"));
    }

    [Fact]
    public void Une_incarnation_d_un_acteur_Autre_garde_la_table_en_lecture_seule_sans_crayon_ni_bouton_ajouter_ni_modal()
    {
        // Given — l'écran réellement câblé ; le configurateur (Parent réel) INCARNE un acteur de type « Autre »
        // (Nina la nounou) → identité effective non-Parent (règle 9).
        using var api = new ApiDistanteFactory();
        var session = new SessionPlanning
        {
            ActeursIncarnables = new List<IdentiteActeur> { new("nounou", "Nina la nounou", TypeActeur.Autre) },
        };
        var config = RendreSous(api, session);

        // … sous identité réelle (Parent), les surfaces d'écriture sont bien là (baseline anti faux-vert).
        Assert.NotEmpty(config.FindAll("[data-testid='crayon-acteur']"));
        Assert.NotEmpty(config.FindAll("[data-testid='bouton-ajouter-acteur']"));

        // When — j'incarne Nina la nounou (type Autre) ; l'écran est re-rendu.
        session.Incarner("nounou");
        Assert.False(session.EstParent); // l'incarnation d'un Autre retire le droit d'écrire
        config.Render();

        // Then — table en lecture seule, aucune surface d'écriture d'acteur atteignable.
        AssertLectureSeuleSansEcriture(config);
    }
}
