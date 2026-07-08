using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 29 — S10 — Un jour sans transfert reste unicolore (non-régression) (@back)
//   Étant donné aucun transfert saisi le jour K
//   Quand on projette la grille agenda sur une fenêtre couvrant K
//   Alors la case du jour K ne porte aucune information bicolore
//   Et son rendu de couleur unique (responsable résolu) est inchangé
//
// Limite de InfoTransfertDuJour (déjà forcée par S9 : null hors d'un jour de transfert).
// Garde de non-régression : présence d'un transfert un AUTRE jour ne « bavure » pas sur K.
public class Scenario29_S10_JourSansTransfertUnicolore
{
    private static readonly DateOnly Reference_24_06_2026 = new(2026, 6, 24);
    private static readonly DateOnly JourJ_25_06_2026 = new(2026, 6, 25); // porte un transfert
    private static readonly DateOnly JourK_26_06_2026 = new(2026, 6, 26); // sans transfert

    [Fact]
    public void Should_ne_porter_aucune_info_bicolore_et_conserver_la_couleur_du_responsable_When_le_jour_n_a_aucun_transfert()
    {
        var transferts = new FakeTransfertRepository();
        transferts.Enregistrer(Transfert
            .Definir("papa", "maman", "ecole", TimeSpan.FromHours(8.5), JourJ_25_06_2026.ToDateTime(TimeOnly.MinValue)).Valeur!);

        // Une période affecte "papa" (couleur bleue) au jour K : sa couleur unique doit rester inchangée.
        var periodes = new FakePeriodeRepository();
        periodes.Enregistrer(PeriodeDeGarde
            .Affecter("papa", JourK_26_06_2026.ToDateTime(TimeOnly.MinValue), JourK_26_06_2026.ToDateTime(TimeOnly.MinValue)).Valeur!);

        var query = new GrilleAgendaQuery(
            new FakeSlotRepository(),
            periodes,
            new FakePaletteCouleurs(new Dictionary<string, string> { ["papa"] = "bleu", ["maman"] = "rose" }),
            new FakeReferentielResponsables(new Dictionary<string, string> { ["papa"] = "Papa", ["maman"] = "Maman" }),
            transferts: transferts);

        var grille = query.Projeter(Reference_24_06_2026);

        // K sans transfert : aucune info bicolore, couleur unique du responsable inchangée.
        var caseK = grille.Jours.Single(j => j.Date == JourK_26_06_2026);
        Assert.Null(caseK.Transfert);
        Assert.Equal("bleu", caseK.CouleurResponsable);
        Assert.Equal("Papa", caseK.NomResponsable);

        // Garde : seul J porte l'info bicolore, aucune bavure sur les autres jours.
        Assert.Single(grille.Jours.Where(j => j.Transfert is not null), j => j.Date == JourJ_25_06_2026);
    }
}
