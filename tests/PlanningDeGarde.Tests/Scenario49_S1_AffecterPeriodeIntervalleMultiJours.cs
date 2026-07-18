using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Tests.Builders;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 49 — Sc.1 — @back FILET de non-régression du chemin d'écriture réutilisé par la
// sélection de plage (drag J1→J3). Une période EST un intervalle [début..fin] (s06), réexercé
// par les plages s45 : ce test VERROUILLE (⚠️ early-green ATTENDU, pas driver) qu'affecter une
// période sur [J1..J3] pose la surcharge sur CHAQUE jour de l'intervalle et que la résolution
// (surcharge > fond) rend le responsable affecté sur les trois jours, le fond reprenant hors
// intervalle. Aucune mécanique d'écriture neuve : la sélection réemploie STRICTEMENT ce chemin.
//
// Données : cycle N=2, mapping {0→parent-b, 1→parent-a}. ISO 28 (06–12/07/2026) PAIRE → index 0
// → fond Parent B (Bruno/orange). Surcharge explicite Parent A (Alice/bleu) sur [07..09/07].
public class Scenario49_S1_AffecterPeriodeIntervalleMultiJours
{
    private const string ParentA = "parent-a"; // Alice — surcharge affectée
    private const string ParentB = "parent-b"; // Bruno — fond du cycle
    private const string Alice = "Alice";
    private const string Bruno = "Bruno";
    private const string Bleu = "bleu";
    private const string Orange = "orange";

    private static readonly DateOnly Lundi_29_06_2026 = new(2026, 6, 29); // ancre fenêtre (ISO 27 → couvre ISO 28)
    private static readonly DateTime J1 = new(2026, 7, 7);   // Mardi   — ISO 28 paire
    private static readonly DateTime J2 = new(2026, 7, 8);   // Mercredi
    private static readonly DateTime J3 = new(2026, 7, 9);   // Jeudi
    private static readonly DateOnly J1d = new(2026, 7, 7);
    private static readonly DateOnly J2d = new(2026, 7, 8);
    private static readonly DateOnly J3d = new(2026, 7, 9);
    private static readonly DateOnly Vendredi_10_07 = new(2026, 7, 10); // hors intervalle, même semaine → fond

    // Test d'acceptation (BDD, frontière Application) : affecter Parent A sur [J1..J3] rend
    // Alice résolu sur les trois jours (surcharge prime sur le fond Parent B), le fond reprenant
    // au jour voisin non couvert (Bruno le 10/07). C'est exactement le chemin que la sélection de
    // plage (drag) réutilise : un intervalle multi-jours = une seule période sur [début..fin].
    [Fact]
    public void Should_resoudre_le_responsable_affecte_sur_chaque_jour_de_l_intervalle_When_une_periode_est_ecrite_sur_J1_a_J3()
    {
        // Given — foyer avec cycle de fond (fond Parent B sur l'intervalle) et l'acteur Alice.
        var periodes = new FakePeriodeRepository();
        var handler = new AffecterPeriodeHandler(
            periodes,
            new FakeResponsableRepository().AvecResponsable(ParentA).AvecResponsable(ParentB));

        // When — une période affectant Alice est écrite sur l'intervalle [J1..J3].
        var resultat = handler.Handle(new PeriodeBuilder().PourResponsable(ParentA).Du(J1).Au(J3).Build());
        Assert.True(resultat.EstSucces);

        // Then — la résolution de la grille rend Alice/bleu sur J1, J2 et J3 (surcharge > fond).
        var grille = QueryAvec(periodes).Projeter(Lundi_29_06_2026);
        foreach (var jour in new[] { J1d, J2d, J3d })
        {
            var caseCouverte = grille.Jours.Single(j => j.Date == jour);
            Assert.Equal(Alice, caseCouverte.NomResponsable);
            Assert.Equal(Bleu, caseCouverte.CouleurResponsable);
        }

        // And — hors intervalle (jour voisin non couvert), le fond reprend : Bruno/orange.
        var caseFond = grille.Jours.Single(j => j.Date == Vendredi_10_07);
        Assert.Equal(Bruno, caseFond.NomResponsable);
        Assert.Equal(Orange, caseFond.CouleurResponsable);
    }

    private static GrilleAgendaQuery QueryAvec(IPeriodeRepository periodes)
        => new(new FakeSlotRepository(), periodes,
            new FakePaletteCouleurs(new Dictionary<string, string> { [ParentA] = Bleu, [ParentB] = Orange }),
            new FakeReferentielResponsables(new Dictionary<string, string> { [ParentA] = Alice, [ParentB] = Bruno }),
            new FakeReferentielCycleDeFond(new CycleDeFond(2, new Dictionary<int, string> { [0] = ParentB, [1] = ParentA })));
}
