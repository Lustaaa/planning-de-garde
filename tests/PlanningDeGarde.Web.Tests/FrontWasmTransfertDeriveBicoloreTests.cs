using System;
using System.Linq;
using Bunit;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 31 — Sc.10 (🖥️ @ihm — acceptation de NIVEAU RUNTIME, clôt le volet 2 D3) : un transfert
/// AUTO-dérivé (volet back Sc.5) se rend avec la MÊME présentation bicolore qu'un transfert saisi (s29),
/// sans aucune règle de rendu neuve. Le store réel de l'API distante ne porte AUCUN transfert saisi —
/// seulement une SUCCESSION de périodes (fin A jour J + début B jour J+1). La grille réellement câblée
/// (front WASM <see cref="Web.Components.Planning.PlanningPartage"/> + API distante réelle, projection réelle)
/// dérive la bascule et coupe la pastille de date du jour de bascule par une diagonale bicolore
/// (cédant → recevant), la légende signalant le motif « Transfert ». Un jour SANS bascule reste unicolore.
///
/// Anti « vert qui ment » : aucun transfert n'est semé ; la diagonale n'apparaît QUE si la dérivation
/// backend est réellement projetée par l'API et rendue par la grille — chemin non doublé.
/// </summary>
public sealed class FrontWasmTransfertDeriveBicoloreTests : TestContext
{
    // Lundi 29/06/2026 : ancre de référence (début de la fenêtre 4 semaines).
    private static readonly DateTime Lundi_29_06_2026 = new(2026, 6, 29);

    [Fact]
    public void Should_rendre_le_jour_de_bascule_en_diagonale_bicolore_comme_un_saisi_When_deux_periodes_se_succedent_sans_transfert_saisi()
    {
        // Given — l'API distante réelle porte une SUCCESSION de périodes (aucun transfert saisi) : Alice
        // (parent-a, bleu) jusqu'au 30/06, puis Bruno (parent-b, orange) à partir du 01/07 (jour de bascule).
        using var api = new ApiDistanteFactory();
        GrilleRuntimeHarness.SemerPeriode(api, "parent-a", new DateTime(2026, 6, 29), new DateTime(2026, 6, 30, 23, 59, 0));
        GrilleRuntimeHarness.SemerPeriode(api, "parent-b", new DateTime(2026, 7, 1), new DateTime(2026, 7, 3));

        // When — la grille réellement câblée est affichée à la date de référence.
        var grille = GrilleRuntimeHarness.RendreGrille(this, api, Lundi_29_06_2026);

        // Then — le jour de bascule (01/07) porte la diagonale bicolore dérivée : départ bleu (Alice, cédant),
        // arrivée orange (Bruno, recevant), portée par la PASTILLE DE DATE (comme un transfert saisi, s29).
        var caseBascule = GrilleRuntimeHarness.CaseDuJour(grille, "01/07");
        var bicolore = caseBascule.QuerySelector("[data-testid='case-transfert-bicolore']");
        Assert.NotNull(bicolore);
        Assert.Equal("bleu", bicolore!.GetAttribute("data-couleur-depart"));
        Assert.Equal("orange", bicolore!.GetAttribute("data-couleur-arrivee"));
        Assert.Contains("grille-jour-date-pastille", bicolore!.ClassName);
        Assert.Contains("01/07", bicolore!.TextContent);

        // … la légende signale le motif bicolore = transfert (comme pour un saisi).
        var motifs = grille.FindAll("[data-testid='legende-motif']");
        Assert.Contains(motifs, m => m.TextContent.Contains("Transfert", StringComparison.OrdinalIgnoreCase));

        // … et un jour SANS bascule (le dernier jour d'Alice, 30/06) reste unicolore : aucune diagonale.
        var caseSansBascule = GrilleRuntimeHarness.CaseDuJour(grille, "30/06");
        Assert.Null(caseSansBascule.QuerySelector("[data-testid='case-transfert-bicolore']"));
    }
}
