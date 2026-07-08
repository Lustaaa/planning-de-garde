using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 29 — S12 — Acteur d'un transfert supprimé : neutre, pas de couleur fantôme (@back)
//   Étant donné un transfert le jour J dont le récupérant a depuis été supprimé du foyer
//   Quand on projette la grille agenda sur une fenêtre couvrant J
//   Alors la couleur d'arrivée retombe sur la couleur neutre (orphelin neutralisé, cf. Resolvable)
//   Et aucune couleur ni nom fantôme n'est produit pour l'acteur absent
//
// Discriminant fort : le récupérant supprimé garde une entrée couleur STALE dans la palette
// (« rose ») ; seul le contrat d'existence (IEnumerationActeursFoyer / Resolvable) neutralise —
// une résolution naïve par la palette produirait la couleur fantôme « rose ».
public class Scenario29_S12_TransfertActeurSupprimeNeutre
{
    private static readonly DateOnly Reference_24_06_2026 = new(2026, 6, 24);
    private static readonly DateOnly JourJ_25_06_2026 = new(2026, 6, 25);

    [Fact]
    public void Should_neutraliser_la_couleur_d_arrivee_de_l_orphelin_sans_couleur_fantome_When_le_recuperant_a_ete_supprime_du_foyer()
    {
        var transferts = new FakeTransfertRepository();
        transferts.Enregistrer(Transfert
            .Definir("papa", "maman", "ecole", TimeSpan.FromHours(8.5), JourJ_25_06_2026.ToDateTime(TimeOnly.MinValue)).Valeur!);

        var query = new GrilleAgendaQuery(
            new FakeSlotRepository(),
            new FakePeriodeRepository(),
            // La palette porte ENCORE une couleur pour "maman" (entrée stale) : c'est le contrat
            // d'existence, pas l'absence de palette, qui doit neutraliser.
            new FakePaletteCouleurs(new Dictionary<string, string> { ["papa"] = "bleu", ["maman"] = "rose" }),
            new FakeReferentielResponsables(new Dictionary<string, string> { ["papa"] = "Papa", ["maman"] = "Maman" }),
            acteurs: new FakeEnumerationActeursFoyer("papa"), // "maman" supprimée du foyer
            transferts: transferts);

        var caseJ = query.Projeter(Reference_24_06_2026).Jours.Single(j => j.Date == JourJ_25_06_2026);

        Assert.NotNull(caseJ.Transfert);
        // Départ (papa, existant) inchangé ; arrivée (maman, orpheline) → neutre, pas "rose".
        Assert.Equal("bleu", caseJ.Transfert!.CouleurDepart);
        Assert.Equal(FakePaletteCouleurs.Neutre, caseJ.Transfert!.CouleurArrivee);
        Assert.NotEqual("rose", caseJ.Transfert!.CouleurArrivee);
    }
}
