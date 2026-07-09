using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 31 — Sc.7 (D3) — Limite NEUTRE : fin de garde sans successeur (@back)
//   Étant donné une période A se terminant le jour J
//   Et aucune période débutant le jour J+1 pour le même enfant
//   Quand la résolution s'exécute sur le jour de bascule
//   Alors aucun transfert n'est dérivé (retombée neutre)
//
// Frontière Application (GrilleAgendaQuery) : sans successeur qui débute, la bascule ne se dérive pas —
// la case du lendemain (et toute la fenêtre) reste sans info transfert. Test non vacant : le jour J et
// le jour J+1 sont dans la fenêtre projetée (une dérivation, si elle avait lieu, serait observable).
public class Scenario31_S7_FinDeGardeSansSuccesseurNeutre
{
    private static readonly DateOnly Reference_24_06_2026 = new(2026, 6, 24);
    private static readonly DateOnly JourFinA_25_06_2026 = new(2026, 6, 25);      // fin de la période A
    private static readonly DateOnly JourApresFinA_26_06_2026 = new(2026, 6, 26); // J+1 : aucun successeur

    // Une SEULE période A (Cédant) qui se termine le J, aucune période ne débute ensuite.
    private static FakePeriodeRepository PeriodeSeuleSansSuccesseur()
    {
        var repo = new FakePeriodeRepository();
        repo.Enregistrer(PeriodeDeGarde.Affecter(
            "cedant", new DateTime(2026, 6, 23), JourFinA_25_06_2026.ToDateTime(new TimeOnly(23, 59))).Valeur!);
        return repo;
    }

    private static GrilleAgendaQuery Query(IPeriodeRepository periodes)
        => new(
            new FakeSlotRepository(),
            periodes,
            new FakePaletteCouleurs(new Dictionary<string, string> { ["cedant"] = "bleu" }),
            new FakeReferentielResponsables(new Dictionary<string, string> { ["cedant"] = "Cédant" }));

    // ---------- Test d'acceptation (boucle externe, BDD) ----------

    [Fact]
    public void Should_ne_deriver_aucun_transfert_le_lendemain_When_aucune_periode_ne_debute_apres_la_fin_de_A()
    {
        // Given — la période A se termine le J, sans aucun successeur.
        var grille = Query(PeriodeSeuleSansSuccesseur()).Projeter(Reference_24_06_2026);

        // Then — le lendemain (J+1) ne porte AUCUN transfert dérivé (retombée neutre).
        Assert.Null(grille.Jours.Single(j => j.Date == JourApresFinA_26_06_2026).Transfert);
    }

    // ---------- Tests unitaires (boucle interne, TDD) ----------

    // Aucune case de la fenêtre ne porte de transfert : sans bascule, il n'y a pas de dérivation fantôme.
    [Fact]
    public void Should_laisser_toute_la_fenetre_sans_transfert_When_il_n_y_a_pas_de_bascule()
    {
        var grille = Query(PeriodeSeuleSansSuccesseur()).Projeter(Reference_24_06_2026);

        Assert.All(grille.Jours, j => Assert.Null(j.Transfert));
    }
}
