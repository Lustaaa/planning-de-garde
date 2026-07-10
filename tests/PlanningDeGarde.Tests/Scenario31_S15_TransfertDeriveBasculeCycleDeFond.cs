using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 31 — Sc.15 (D3, rework G3 — décision PO option A) — Transfert dérivé sur bascule du CYCLE DE FOND (@back)
//   Étant donné un cycle de fond en alternance hebdomadaire (le responsable résolu bascule chaque semaine)
//   Et un profil de données RÉALISTE (cycle de fond + périodes surchargées éparses, PAS de succession sur-mesure)
//   Quand la grille projette un jour où le responsable RÉSOLU (surcharge > fond) change d'un jour à l'autre
//     du fait du cycle de fond (aucune période ne couvre ces jours)
//   Alors une pastille bicolore est dérivée ce jour-là (cédant = responsable de la veille, recevant = du jour)
//   Et le motif « Transfert » apparaît en légende (en case comme en légende)
//
// Frontière Application (GrilleAgendaQuery). Ce scénario cible le TROU découvert au gate G3 : la dérivation
// période→période FONCTIONNE mais ne voyait PAS les relais du cycle de fond, qui pilotent le planning réel.
// Profil réaliste (cycle hebdo + période éparse), NON un seed de deux périodes adjacentes sur-mesure — c'est
// ce profil sur-mesure qui masquait le trou. Les couleurs attendues sont lues sur les cases elles-mêmes
// (frontière publique) pour ne pas ré-encoder la parité ISO du cycle dans le test.
public class Scenario31_S15_TransfertDeriveBasculeCycleDeFond
{
    private static readonly DateOnly Reference_24_06_2026 = new(2026, 6, 24);
    private static readonly DateOnly BasculeCycle_06_07_2026 = new(2026, 7, 6);  // lundi : nouvelle semaine ISO → le cycle bascule
    private static readonly DateOnly VeilleBascule_05_07_2026 = new(2026, 7, 5); // dimanche : cœur de la semaine précédente

    // Cycle de fond en ALTERNANCE HEBDO (N=2) : parité ISO → papa (index 0) / maman (index 1). Chaque lundi
    // (nouvelle semaine ISO) le responsable résolu bascule de l'un à l'autre.
    private static FakeReferentielCycleDeFond CycleAlternanceHebdo()
        => new(new CycleDeFond(2, new Dictionary<int, string> { [0] = "papa", [1] = "maman" }));

    // Profil RÉALISTE : UNE période surchargée ÉPARSE (non adjacente), loin de la bascule observée — reflète
    // un planning réel piloté par le cycle avec des surcharges ponctuelles, pas une succession fabriquée.
    private static FakePeriodeRepository PeriodesEparses()
    {
        var repo = new FakePeriodeRepository();
        repo.Enregistrer(PeriodeDeGarde.Affecter(
            "maman", new DateTime(2026, 7, 15), new DateTime(2026, 7, 16, 23, 59, 0)).Valeur!);
        return repo;
    }

    private static GrilleAgendaQuery Query()
        => new(
            new FakeSlotRepository(),
            PeriodesEparses(),
            new FakePaletteCouleurs(new Dictionary<string, string> { ["papa"] = "bleu", ["maman"] = "rose" }),
            new FakeReferentielResponsables(new Dictionary<string, string> { ["papa"] = "Papa", ["maman"] = "Maman" }),
            cycle: CycleAlternanceHebdo());

    // ---------- Test d'acceptation (boucle externe, BDD) ----------

    [Fact]
    public void Should_deriver_une_pastille_bicolore_au_relais_du_cycle_When_le_responsable_resolu_bascule_dun_jour_a_lautre()
    {
        var grille = Query().Projeter(Reference_24_06_2026);
        var veille = grille.Jours.Single(j => j.Date == VeilleBascule_05_07_2026);
        var lundi = grille.Jours.Single(j => j.Date == BasculeCycle_06_07_2026);

        // Given (profil réaliste) : ces deux jours sont PUREMENT pilotés par le cycle (aucune période ne les
        // couvre) et le cycle bascule d'un responsable à l'autre entre la veille et le lundi.
        Assert.False(string.IsNullOrEmpty(veille.NomResponsable));
        Assert.False(string.IsNullOrEmpty(lundi.NomResponsable));
        Assert.NotEqual(veille.NomResponsable, lundi.NomResponsable);

        // Then — le jour de bascule du CYCLE porte une pastille bicolore dérivée : départ = responsable de la
        // veille (cédant), arrivée = responsable du jour (recevant), mêmes couleurs que les cases elles-mêmes.
        Assert.NotNull(lundi.Transfert);
        Assert.Equal(veille.CouleurResponsable, lundi.Transfert!.CouleurDepart);
        Assert.Equal(lundi.CouleurResponsable, lundi.Transfert!.CouleurArrivee);

        // … et la veille (au cœur d'une semaine de cycle) reste unicolore (pas de dérivation fantôme).
        Assert.Null(veille.Transfert);

        // … et le motif « Transfert » apparaît en légende dès que la fenêtre porte une bascule (en case comme en légende).
        Assert.NotNull(grille.LégendeMotifs);
        Assert.Contains(grille.LégendeMotifs!, m => m.Libelle == "Transfert");
    }

    // ---------- Test unitaire (boucle interne, TDD) : pas de doublon SAISI/DÉRIVÉ, priorité conservée ----------

    // Un transfert SAISI le jour de bascule du cycle prime et reste seul retenu (aucun doublon dérivé du cycle).
    [Fact]
    public void Should_retenir_le_saisi_seul_When_un_transfert_est_saisi_le_jour_de_bascule_du_cycle()
    {
        var transferts = new FakeTransfertRepository();
        transferts.Enregistrer(Transfert
            .Definir("papa", "maman", "ecole", TimeSpan.FromHours(8.5), BasculeCycle_06_07_2026.ToDateTime(TimeOnly.MinValue)).Valeur!);

        var lundi = new GrilleAgendaQuery(
                new FakeSlotRepository(),
                PeriodesEparses(),
                new FakePaletteCouleurs(new Dictionary<string, string> { ["papa"] = "bleu", ["maman"] = "rose" }),
                new FakeReferentielResponsables(new Dictionary<string, string> { ["papa"] = "Papa", ["maman"] = "Maman" }),
                cycle: CycleAlternanceHebdo(),
                transferts: transferts)
            .Projeter(Reference_24_06_2026)
            .Jours.Single(j => j.Date == BasculeCycle_06_07_2026);

        // Le saisi (papa→maman) est rendu tel quel : le cycle ne produit pas un second transfert en doublon.
        Assert.NotNull(lundi.Transfert);
        Assert.Equal("bleu", lundi.Transfert!.CouleurDepart);
        Assert.Equal("rose", lundi.Transfert!.CouleurArrivee);
    }
}
