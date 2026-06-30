using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 19 — Sc.2 — Store d'acteurs vide → grille neutre, aucun fictif injecté (@back)
//   Étant donné un foyer dont le store d'acteurs est vide
//   Quand on projette la grille agenda
//   Alors toutes les cases sont en repli neutre (aucun nom, couleur neutre)
//     Et la légende est vide
//     Et aucun acteur fictif (« Parent A / Parent B ») n'est injecté dans la projection
//
// Acceptation à la frontière Application (CQRS lecture). Le store VIVANT des acteurs déclarés
// (IEnumerationActeursFoyer) est VIDE : par le contrat d'existence Resolvable (s13), toute
// référence — surcharge (période) comme fond (cycle) — devient orpheline → neutralisée. La grille
// retombe donc entièrement sur le repli neutre (couleur neutre, aucun nom) et la légende est vide.
// Garde-fou clé du sprint : AUCUN acteur fictif (« Parent A / Parent B ») n'est injecté par défaut
// quand le store est vide — la projection ne fabrique jamais d'acteur de secours. On charge
// pourtant le référentiel/palette de doublures avec Alice/Bruno pour PROUVER qu'un nom résolvable
// existant ne « fuite » pas si l'acteur n'est pas déclaré dans le store vivant.
//
// Dates déterministes (Projeter(ancre) — jamais Now) : fenêtre 4 semaines (28 j) au 29/06/2026.
public class Scenario2_StoreVideGrilleNeutre
{
    private const string ParentA = "parent-a";
    private const string ParentB = "parent-b";
    private const string Gris = FakePaletteCouleurs.Neutre;

    private static readonly DateOnly Lundi_29_06_2026 = new(2026, 6, 29);

    [Fact]
    public void Acceptation_Should_Rendre_toutes_les_cases_neutres_sans_nom_et_une_legende_vide_sans_injecter_aucun_acteur_fictif_When_le_store_d_acteurs_est_vide()
    {
        // --- Given : store vivant des acteurs déclarés VIDE ---
        var storeVide = new FakeEnumerationActeursFoyer(/* aucun acteur déclaré */);

        // référentiel/palette qui RÉSOUDRAIENT parent-a/parent-b s'ils n'étaient pas filtrés :
        // prouve qu'un nom résolvable ne fuit pas quand l'acteur n'est pas déclaré dans le store vivant.
        var referentiel = new FakeReferentielResponsables(new Dictionary<string, string>
        {
            [ParentA] = "Alice",
            [ParentB] = "Bruno",
        });
        var palette = new FakePaletteCouleurs(new Dictionary<string, string>
        {
            [ParentA] = "bleu",
            [ParentB] = "orange",
        });

        // une période (surcharge) et un cycle de fond référençant des id ABSENTS du store vide
        var periodes = new FakePeriodeRepository();
        periodes.Enregistrer(PeriodeDeGarde
            .Affecter(ParentA, new DateTime(2026, 7, 6), new DateTime(2026, 7, 6))
            .Valeur!);
        var cycle = new FakeReferentielCycleDeFond(new CycleDeFond(2, new Dictionary<int, string>
        {
            [0] = ParentA,
            [1] = ParentB,
        }));

        var query = new GrilleAgendaQuery(new FakeSlotRepository(), periodes, palette, referentiel, cycle, storeVide);

        // --- When ---
        var grille = query.Projeter(Lundi_29_06_2026);

        // --- Then : toutes les cases sont neutres (aucun nom, couleur neutre) ---
        Assert.NotEmpty(grille.Jours);
        Assert.All(grille.Jours, j =>
        {
            Assert.Equal("", j.NomResponsable);
            Assert.Equal(Gris, j.CouleurResponsable);
        });

        // --- Then : la légende est vide ---
        Assert.Empty(grille.Légende);

        // --- Then : aucun acteur fictif n'est injecté (ni en case, ni en légende) ---
        Assert.DoesNotContain(grille.Jours, j => j.NomResponsable is "Parent A" or "Parent B" or "Alice" or "Bruno");
    }
}
