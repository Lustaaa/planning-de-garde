using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 07 — Sc.6 — Nom long : lisibilité de la case préservée (@limite)
//   Given un responsable au nom long « Marie-Hélène Grand-Dubois » (parent-a, bleu)
//   And une période lui confie la garde le vendredi 03/07/2026
//   When la grille est projetée
//   Then la case du 03/07 et la légende portent le nom COMPLET, sans troncature côté donnée
//
// CARACTÉRISATION backend (early green ATTENDU, filet — PAS un driver). Le read model est la
// SOURCE DE VÉRITÉ du nom complet : le référentiel renvoie la chaîne intégrale et la projection
// ne tronque jamais. La troncature visuelle de la case + le survol (title) sont de la PRÉSENTATION
// (.razor/CSS), driver routé ihm-builder. On verrouille ici qu'aucune altération de la donnée
// (troncature) ne fuit dans le read model.
public class Scenario_NomLongPorteComplet
{
    private const string ParentA = "parent-a";
    private const string NomLong = "Marie-Hélène Grand-Dubois";
    private const string Bleu = "bleu";

    private static readonly DateOnly Lundi_29_06_2026 = new(2026, 6, 29);
    private static readonly DateOnly Vendredi_03_07_2026 = new(2026, 7, 3);

    private static IPaletteCouleurs PaletteParentABleu()
        => new FakePaletteCouleurs(new Dictionary<string, string> { [ParentA] = Bleu });

    private static IReferentielResponsables ReferentielNomLong()
        => new FakeReferentielResponsables(new Dictionary<string, string> { [ParentA] = NomLong });

    private static FakePeriodeRepository PeriodeNomLongLe_03_07()
    {
        var periodes = new FakePeriodeRepository();
        periodes.Enregistrer(PeriodeDeGarde.Affecter(ParentA, new DateTime(2026, 7, 3), new DateTime(2026, 7, 3)).Valeur!);
        return periodes;
    }

    private static GrilleAgendaQuery Query()
        => new(new FakeSlotRepository(), PeriodeNomLongLe_03_07(), PaletteParentABleu(), ReferentielNomLong());

    // Test #1 — Caractérisation : le nom COMPLET est porté dans la case ET dans la légende (jamais tronqué).
    [Fact]
    public void Should_Porter_le_nom_complet_du_responsable_dans_la_case_et_dans_la_legende_When_son_nom_est_long()
    {
        var grille = Query().Projeter(Lundi_29_06_2026);

        // la case porte la chaîne intégrale (aucune troncature côté donnée)
        var caseNomLong = grille.Jours.Single(j => j.Date == Vendredi_03_07_2026);
        Assert.Equal(NomLong, caseNomLong.NomResponsable);

        // la légende porte également le nom complet
        var entree = Assert.Single(grille.Légende);
        Assert.Equal(NomLong, entree.Nom);
    }
}
