using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 29 — S11 — La légende signale le motif bicolore = transfert (@back)
//   Étant donné un transfert saisi dans la fenêtre projetée
//   Quand on projette la grille agenda
//   Alors la légende porte une entrée signalant le motif bicolore comme un transfert
//   Et cette entrée est absente quand aucun transfert ne couvre la fenêtre
//
// Projection backend GrilleAgendaQuery — testée sans Blazor, date de référence injectée.
public class Scenario29_S11_LegendeMotifTransfert
{
    private static readonly DateOnly Reference_24_06_2026 = new(2026, 6, 24);
    private static readonly DateOnly DansLaFenetre_25_06_2026 = new(2026, 6, 25);
    private static readonly DateOnly HorsFenetre_25_08_2026 = new(2026, 8, 25);

    private static GrilleAgendaQuery QueryAvecTransfertLe(DateOnly date)
    {
        var transferts = new FakeTransfertRepository();
        transferts.Enregistrer(Transfert
            .Definir("papa", "maman", "ecole", TimeSpan.FromHours(8.5), date.ToDateTime(TimeOnly.MinValue)).Valeur!);
        return new GrilleAgendaQuery(
            new FakeSlotRepository(),
            new FakePeriodeRepository(),
            new FakePaletteCouleurs(new Dictionary<string, string> { ["papa"] = "bleu", ["maman"] = "rose" }),
            new FakeReferentielResponsables(new Dictionary<string, string> { ["papa"] = "Papa", ["maman"] = "Maman" }),
            transferts: transferts);
    }

    // ---------- Test d'acceptation (boucle externe, BDD) ----------

    [Fact]
    public void Should_porter_une_entree_de_legende_du_motif_transfert_When_un_transfert_couvre_la_fenetre()
    {
        var grille = QueryAvecTransfertLe(DansLaFenetre_25_06_2026).Projeter(Reference_24_06_2026);

        Assert.NotNull(grille.LégendeMotifs);
        Assert.Contains(grille.LégendeMotifs!, m => m.Libelle == "Transfert");
    }

    [Fact]
    public void Should_ne_porter_aucune_entree_de_legende_du_motif_transfert_When_aucun_transfert_ne_couvre_la_fenetre()
    {
        // Transfert saisi HORS de la fenêtre projetée (2 mois plus tard).
        var grille = QueryAvecTransfertLe(HorsFenetre_25_08_2026).Projeter(Reference_24_06_2026);

        Assert.NotNull(grille.LégendeMotifs);
        Assert.DoesNotContain(grille.LégendeMotifs!, m => m.Libelle == "Transfert");
    }
}
