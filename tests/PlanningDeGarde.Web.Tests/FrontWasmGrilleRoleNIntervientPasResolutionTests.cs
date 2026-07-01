using System;
using System.Collections.Generic;
using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 21 — Sc.11 (🖥️ @ihm — acceptation de NIVEAU RUNTIME, NON-RÉGRESSION) : le rôle porté par un
/// acteur (caractéristique d'organisation, s21) <b>n'intervient pas</b> dans la projection de la grille ni
/// de la légende. Avec des acteurs <b>porteurs de rôles</b>, un cycle de fond ET une période de surcharge,
/// la grille réellement câblée à l'API distante réelle résout la responsabilité <b>strictement comme sans
/// rôle</b> : <b>surcharge &gt; fond &gt; neutre</b>. Ni la teinte, ni le nom de case, ni la légende ne
/// dépendent du rôle — aucun libellé de rôle (« Nounou » / « Grand-parent ») n'apparaît nulle part.
///
/// <para>Rempart de non-régression : la résolution (paliers 6/8/9, s13/s19) est une propriété métier que
/// l'ajout du référentiel de rôles ne doit pas toucher (décision CP — le rôle ne pilote pas la garde ce
/// sprint). Anti « vert qui ment » : les acteurs portent réellement des rôles (semés au store réel), et
/// l'on asserte à la fois la résolution attendue ET l'absence de tout libellé de rôle dans le rendu.</para>
/// </summary>
public sealed class FrontWasmGrilleRoleNIntervientPasResolutionTests : TestContext
{
    [Fact]
    public void Should_resoudre_surcharge_fond_neutre_inchange_et_ne_jamais_afficher_le_role_dans_la_grille_ni_la_legende()
    {
        // Given — l'API distante réelle (référentiel + palette réels : parent-a « Alice » bleu, parent-b
        // « Bruno » orange). On donne un RÔLE à chaque acteur (référentiel réel + affectation réelle) : si
        // le rôle intervenait dans la projection, il transparaîtrait en case ou en légende.
        using var api = new ApiDistanteFactory();
        var editeurRoles = api.Services.GetRequiredService<IEditeurReferentielRoles>();
        editeurRoles.Creer("role-nounou", "Nounou");
        editeurRoles.Creer("role-grand-parent", "Grand-parent");
        var config = api.Services.GetRequiredService<IEditeurConfigurationFoyer>();
        config.AffecterRole("parent-a", "role-nounou");
        config.AffecterRole("parent-b", "role-grand-parent");

        // … un cycle de fond 2 semaines (index 0 → parent-a, index 1 → parent-b) : ISO 27 (impaire) → Bruno,
        // ISO 28 (paire) → Alice. PLUS une période de SURCHARGE affectée à parent-a le mercredi 01/07/2026
        // (semaine ISO 27) : elle doit primer sur le fond Bruno de cette semaine (surcharge > fond).
        GrilleRuntimeHarness.SemerCycle(api,
            new CycleDeFond(2, new Dictionary<int, string> { [0] = "parent-a", [1] = "parent-b" }));
        GrilleRuntimeHarness.SemerPeriode(api, "parent-a", new DateTime(2026, 7, 1), new DateTime(2026, 7, 1));

        // When — la grille réellement câblée est affichée à la date de référence lundi 29/06/2026 (ISO 27).
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, GrilleRuntimeHarness.Lundi_29_06_2026);

        // Then (surcharge > fond) — le mercredi 01/07 (ISO 27) porte la SURCHARGE Alice (bleu), pas le fond
        // Bruno : la résolution prime la période sur le cycle, inchangée par le rôle porté par Alice.
        var caseSurcharge = GrilleRuntimeHarness.CaseDuJour(grille, "01/07");
        Assert.Equal("Alice", caseSurcharge.QuerySelector("[data-testid='nom-responsable']")!.TextContent.Trim());
        Assert.Equal("bleu", caseSurcharge.GetAttribute("data-couleur"));

        // Then (fond) — le lundi 29/06 (ISO 27, sans surcharge) porte le FOND Bruno (orange) : le rôle
        // « Grand-parent » de Bruno n'y change rien.
        var caseFondIso27 = GrilleRuntimeHarness.CaseDuJour(grille, "29/06");
        Assert.Equal("Bruno", caseFondIso27.QuerySelector("[data-testid='nom-responsable']")!.TextContent.Trim());
        Assert.Equal("orange", caseFondIso27.GetAttribute("data-couleur"));

        // … et la semaine ISO 28 (paire) porte le fond Alice (bleu) : alternance inchangée.
        var caseFondIso28 = GrilleRuntimeHarness.CaseDuJour(grille, "06/07");
        Assert.Equal("Alice", caseFondIso28.QuerySelector("[data-testid='nom-responsable']")!.TextContent.Trim());
        Assert.Equal("bleu", caseFondIso28.GetAttribute("data-couleur"));

        // Then (le rôle n'apparaît nulle part) — aucun libellé de rôle ne transparaît dans la grille rendue
        // (cases, noms, légende) : le rôle est une caractéristique d'organisation, pas une donnée de garde.
        Assert.DoesNotContain("Nounou", grille.Markup);
        Assert.DoesNotContain("Grand-parent", grille.Markup);

        // … la légende porte exactement les responsables résolus (Alice bleu, Bruno orange), jamais un rôle.
        var legende = grille.FindAll("[data-testid='legende-entree']");
        Assert.Equal(2, legende.Count);
        Assert.Contains(legende, e => e.QuerySelector(".legende-nom")!.TextContent.Trim() == "Alice"
            && e.GetAttribute("data-couleur") == "bleu");
        Assert.Contains(legende, e => e.QuerySelector(".legende-nom")!.TextContent.Trim() == "Bruno"
            && e.GetAttribute("data-couleur") == "orange");
    }
}
