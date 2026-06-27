using System;
using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Acceptation de NIVEAU RUNTIME du Sc.1 (🖥️ scénario IHM, <c>@nominal</c> — palier 7 « écriture en
/// contexte ») — le comportement neuf vit dans le <c>.razor</c> : <b>cliquer une case du planning ouvre
/// la dialog « Poser un slot »</b>, pré-remplie sur la date de la case ; la pose validée <b>réapparaît</b>
/// positionnée dans cette case. On rend la <b>vraie</b> grille <see cref="Web.Components.Pages.PlanningPartage"/>
/// (front WASM) câblée à une <b>API distante réelle</b> (<see cref="ApiDistanteFactory"/>, store réel,
/// projection réelle <see cref="GrilleAgendaQuery"/>, canal d'écriture HTTP réel) — exactement le câblage
/// des acceptations runtime s05/s10. Aucun handler ni règle backend neuf : on réutilise la commande
/// <c>PoserSlot</c> et l'endpoint <c>POST /api/canal/poser-slot</c>.
///
/// Anti « vert qui ment » : si le clic sur la case n'ouvre aucune dialog (câblage <c>@onclick</c> → dialog
/// absent), si la pose ne transite pas jusqu'au store distant, ou si le slot retombe à une autre date,
/// l'observable reste vide → rouge. Un bUnit composant à doublure ne verrait jamais ce câblage distant ni
/// l'ouverture/fermeture réelle de la dialog depuis la grille.
/// </summary>
public sealed class FrontWasmPoserSlotDepuisCaseTests : TestContext
{
    // Mardi 16/06/2026 : la case cliquée. Référence « aujourd'hui » au 16/06 → fenêtre de 5 semaines
    // démarrant au lundi 15/06, qui couvre le mardi 16/06.
    private static readonly DateTime Mardi_16_06_2026 = new(2026, 6, 16);

    [Fact]
    public void Should_Faire_apparaitre_le_slot_ecole_08h30_16h30_dans_la_case_du_mardi_16_06_2026_When_un_parent_pose_un_slot_via_la_dialog_ouverte_depuis_cette_case()
    {
        // Given — la grille réellement câblée à l'API distante, affichée pour un Parent (store réel vierge),
        // fenêtre couvrant le mardi 16/06/2026.
        using var api = new ApiDistanteFactory();
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Mardi_16_06_2026);

        // … la case du mardi 16/06 est sans slot, et aucune dialog n'est ouverte.
        Assert.Empty(GrilleRuntimeHarness.CaseDuJour(grille, "16/06").QuerySelectorAll("[data-testid='slot-case']"));
        Assert.Empty(grille.FindAll("[data-testid='dialog-poser-slot']"));

        // When — un Parent clique la case du mardi 16/06 → la dialog « Poser un slot » s'ouvre.
        GrilleRuntimeHarness.CaseDuJour(grille, "16/06").Click();
        Assert.NotEmpty(grille.FindAll("[data-testid='dialog-poser-slot']"));

        // … il choisit le lieu « école » (08:30 → 16:30 pré-remplis sur la date de la case) et valide.
        grille.Find("[data-testid='champ-lieu']").Change("école");
        grille.Find("[data-testid='dialog-poser-slot'] form").Submit();

        // Then — la dialog se ferme ET le slot « école » 08h30→16h30 réapparaît, positionné dans la case
        // du 16/06, relu depuis le store réel de l'API distante (relecture sur succès), pas une mutation
        // locale de la grille.
        grille.WaitForAssertion(
            () =>
            {
                Assert.Empty(grille.FindAll("[data-testid='dialog-poser-slot']"));
                var slot = GrilleRuntimeHarness.CaseDuJour(grille, "16/06")
                    .QuerySelectorAll("[data-testid='slot-case']").Single();
                Assert.Contains("école", slot.QuerySelector(".grille-slot-libelle")!.TextContent);
                Assert.Contains("08:30", slot.QuerySelector(".grille-slot-horaire")!.TextContent);
                Assert.Contains("16:30", slot.QuerySelector(".grille-slot-horaire")!.TextContent);
            },
            TimeSpan.FromSeconds(10));

        // … et l'écriture a réellement transité jusqu'au store de l'API distante (rempart anti vert-qui-ment) :
        // observée via la projection réelle de l'hôte d'API, à la semaine du lundi 15/06/2026.
        using var scope = api.Services.CreateScope();
        var projection = scope.ServiceProvider.GetRequiredService<GrilleAgendaQuery>();
        var grilleStore = projection.Projeter(new DateOnly(2026, 6, 15));
        var caseStore = grilleStore.Jours.Single(j => j.Date == new DateOnly(2026, 6, 16));
        var slotStore = Assert.Single(caseStore.Slots, s => s.Libelle == "école");
        Assert.Equal(new TimeOnly(8, 30), slotStore.Debut);
        Assert.Equal(new TimeOnly(16, 30), slotStore.Fin);
    }
}
