using System;
using System.IO;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 26 — Sc.11 (🖥️ @ihm, dialogs ×6) — les six dialogs (PoserSlot, AffecterPeriode, DefinirTransfert,
/// EditerPeriode, SupprimerPeriode, SupprimerSlot) partagent un habillage cohérent tokenisé. Garde d'asset
/// sur les feuilles réellement livrées :
/// <list type="bullet">
///   <item>la surface de dialog est <b>centralisée et tokenisée</b> dans <c>app.css</c> (<c>.dialog-panneau</c>
///     en <c>--pdg-card</c>) — condition du rendu correct clair ET sombre ;</item>
///   <item>aucun des six dialogs ne re-déclare une surface blanche codée en dur (qui cassait le sombre et la
///     cohérence).</item>
/// </list>
/// La hiérarchie de boutons (primaire <c>btn-primary</c> / annulation <c>btn-link</c> / destructive
/// <c>btn-outline-danger</c>), les data-testid et tous les flux d'écriture restent intacts (Sc.14 le prouve).
/// </summary>
public sealed class FrontWasmDialogsHabillageCoherentTests
{
    private static readonly string[] Dialogs =
    {
        "PoserSlotDialog", "AffecterPeriodeDialog", "DefinirTransfertDialog",
        "EditerPeriodeDialog", "SupprimerPeriodeDialog", "SupprimerSlotDialog",
    };

    private static string RacineWeb()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "PlanningDeGarde.slnx")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        return Path.Combine(dir!.FullName, "src", "PlanningDeGarde.Web");
    }

    [Fact]
    public void La_surface_de_dialog_est_centralisee_et_tokenisee_dans_app_css()
    {
        var css = File.ReadAllText(Path.Combine(RacineWeb(), "wwwroot", "app.css"));
        var idx = css.IndexOf(".dialog-panneau", StringComparison.Ordinal);
        Assert.True(idx >= 0, "une surface de dialog partagée doit être définie dans app.css");
        var bloc = css[idx..Math.Min(css.Length, idx + 400)];
        Assert.Contains("var(--pdg-card)", bloc, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("PoserSlotDialog")]
    [InlineData("AffecterPeriodeDialog")]
    [InlineData("DefinirTransfertDialog")]
    [InlineData("EditerPeriodeDialog")]
    [InlineData("SupprimerPeriodeDialog")]
    [InlineData("SupprimerSlotDialog")]
    public void Aucun_dialog_ne_redeclare_une_surface_blanche_codee_en_dur(string dialog)
    {
        // Lot 6 (refacto hors-sprint) : les dialogs sont rangés par bounded context sous Components/<BC>/.
        var bc = dialog switch
        {
            "PoserSlotDialog" or "SupprimerSlotDialog" => "Slots",
            "AffecterPeriodeDialog" or "EditerPeriodeDialog" or "SupprimerPeriodeDialog" => "Periodes",
            "DefinirTransfertDialog" => "Transferts",
            _ => throw new ArgumentOutOfRangeException(nameof(dialog), dialog, "bounded context inconnu"),
        };
        var razor = File.ReadAllText(Path.Combine(RacineWeb(), "Components", bc, dialog + ".razor"));
        Assert.DoesNotContain("background: #fff", razor, StringComparison.OrdinalIgnoreCase);
    }
}
