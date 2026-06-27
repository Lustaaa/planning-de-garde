using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 07 — Sc.3 — Fenêtre sans aucune affectation : légende masquée (@limite)
//   Given une fenêtre affichée sans aucune période de garde
//   When la grille est projetée
//   Then aucune case ne porte de nom de responsable, et la légende est vide
//
// CARACTÉRISATION backend (early green ATTENDU, filet anti-régression — PAS un driver). Le vrai
// driver de ce scénario est le MASQUAGE du bloc légende dans le .razor (présentation), routé
// ihm-builder. Côté read model, la garantie est déjà acquise par Sc.1 : la légende est dérivée
// des PRÉSENTS (aucun → liste vide) et le nom n'est résolu que sur les cases COUVERTES (aucune →
// pas de nom). Ce test verrouille qu'on n'invente jamais une entrée de légende « fantôme » ni un
// nom sur une fenêtre vide (on ne « ment » pas en suggérant que quelqu'un garde).
public class Scenario_FenetreVideLegendeMasquee
{
    private static readonly DateOnly Lundi_29_06_2026 = new(2026, 6, 29);

    private static GrilleAgendaQuery QuerySansAucunePeriode()
        => new(
            new FakeSlotRepository(),
            new FakePeriodeRepository(),
            new FakePaletteCouleurs(new Dictionary<string, string>()),
            new FakeReferentielResponsables(new Dictionary<string, string>()));

    // Test #1 — Caractérisation : fenêtre sans période → légende vide ET aucune case nommée.
    [Fact]
    public void Should_Produire_une_legende_vide_et_aucune_case_nommee_When_aucune_periode_ne_couvre_la_fenetre()
    {
        var grille = QuerySansAucunePeriode().Projeter(Lundi_29_06_2026);

        // la légende ne contient aucune entrée (bloc masquable côté IHM)
        Assert.Empty(grille.Légende);

        // aucune des 35 cases ne porte de nom de responsable
        Assert.All(grille.Jours, j => Assert.Equal("", j.NomResponsable));
    }
}
