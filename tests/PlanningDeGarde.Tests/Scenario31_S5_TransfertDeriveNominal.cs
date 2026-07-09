using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 31 — Sc.5 (D3, 1er cœur) — Nominal : transfert AUTO-dérivé le jour de bascule (@back)
//   Étant donné une période A responsable "Cédant" se terminant le jour J
//   Et une période B responsable "Recevant", même enfant, débutant le jour J+1
//   Quand la résolution s'exécute sur le jour de bascule
//   Alors un transfert est dérivé automatiquement (Cédant → Recevant) ce jour-là
//   Et aucun transfert n'a été saisi manuellement
//
// Frontière Application : la projection GrilleAgendaQuery DÉRIVE la bascule depuis la succession de
// périodes (fin A jour J + début B jour J+1 ⇒ transfert Cédant→Recevant), sans aucun transfert saisi.
// Le jour de bascule rendu = J+1 (le jour où le successeur prend le relais) — cohérent avec Sc.8 (si
// J+1 sort de la fenêtre, rien n'est rendu).
public class Scenario31_S5_TransfertDeriveNominal
{
    private static readonly DateOnly Reference_24_06_2026 = new(2026, 6, 24);
    private static readonly DateOnly JourJ_25_06_2026 = new(2026, 6, 25);       // fin de la période A (Cédant)
    private static readonly DateOnly JourBascule_26_06_2026 = new(2026, 6, 26); // début de la période B (Recevant)

    // Succession SANS aucun transfert saisi : A (Cédant) finit le J, B (Recevant) débute le J+1.
    private static FakePeriodeRepository SuccessionCedantPuisRecevant()
    {
        var repo = new FakePeriodeRepository();
        repo.Enregistrer(PeriodeDeGarde.Affecter(
            "cedant", new DateTime(2026, 6, 23), JourJ_25_06_2026.ToDateTime(new TimeOnly(23, 59))).Valeur!);
        repo.Enregistrer(PeriodeDeGarde.Affecter(
            "recevant", JourBascule_26_06_2026.ToDateTime(new TimeOnly(0, 0)), new DateTime(2026, 6, 28)).Valeur!);
        return repo;
    }

    private static GrilleAgendaQuery Query(IPeriodeRepository periodes, ITransfertRepository? transferts = null)
        => new(
            new FakeSlotRepository(),
            periodes,
            new FakePaletteCouleurs(new Dictionary<string, string> { ["cedant"] = "bleu", ["recevant"] = "rose" }),
            new FakeReferentielResponsables(new Dictionary<string, string> { ["cedant"] = "Cédant", ["recevant"] = "Recevant" }),
            transferts: transferts);

    // ---------- Test d'acceptation (boucle externe, BDD) ----------

    [Fact]
    public void Should_deriver_un_transfert_cedant_vers_recevant_le_jour_de_bascule_When_aucun_transfert_n_est_saisi()
    {
        // Given — la succession fin A (Cédant) le J + début B (Recevant) le J+1, aucun transfert saisi.
        var grille = Query(SuccessionCedantPuisRecevant()).Projeter(Reference_24_06_2026);

        // Then — la case du jour de bascule (J+1) porte une information bicolore dérivée : départ = Cédant,
        // arrivée = Recevant, résolus sur identifiant stable — sans qu'aucun transfert n'ait été saisi.
        var caseBascule = grille.Jours.Single(j => j.Date == JourBascule_26_06_2026);
        Assert.NotNull(caseBascule.Transfert);
        Assert.Equal("bleu", caseBascule.Transfert!.CouleurDepart);  // Cédant, déposant
        Assert.Equal("rose", caseBascule.Transfert!.CouleurArrivee); // Recevant, récupérant
    }

    // ---------- Tests unitaires (boucle interne, TDD) ----------

    // Test #1 — présence : le jour de bascule (J+1) porte une info bicolore non nulle, dérivée de la succession.
    [Fact]
    public void Should_porter_une_info_bicolore_non_nulle_sur_le_jour_de_bascule_When_deux_periodes_se_succedent()
    {
        var grille = Query(SuccessionCedantPuisRecevant()).Projeter(Reference_24_06_2026);

        Assert.NotNull(grille.Jours.Single(j => j.Date == JourBascule_26_06_2026).Transfert);
    }

    // Test #2 — un jour SANS bascule (au cœur de la période A) reste unicolore (aucune dérivation fantôme).
    [Fact]
    public void Should_laisser_unicolore_un_jour_sans_bascule_When_il_est_au_coeur_d_une_periode()
    {
        var grille = Query(SuccessionCedantPuisRecevant()).Projeter(Reference_24_06_2026);

        Assert.Null(grille.Jours.Single(j => j.Date == JourJ_25_06_2026).Transfert);
    }
}
