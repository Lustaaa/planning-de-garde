using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 29 — S9 — Un jour avec transfert porte l'information bicolore (@back)
//   Étant donné deux acteurs "Papa" et "Maman", chacun avec sa couleur
//   Et un transfert le jour J : déposé par "Papa", récupéré par "Maman"
//   Quand on projette la grille agenda sur une fenêtre couvrant J
//   Alors la case du jour J porte une information bicolore
//   Et couleur de départ = couleur de "Papa" (déposant), résolue sur son identifiant stable
//   Et couleur d'arrivée = couleur de "Maman" (récupérant), résolue sur son identifiant stable
//   Et la résolution de responsabilité de la case (surcharge > fond > neutre) est inchangée
//
// Projection backend GrilleAgendaQuery — testée sans Blazor, date de référence injectée.
public class Scenario29_S9_TransfertBicolore
{
    private static readonly DateOnly Reference_24_06_2026 = new(2026, 6, 24);
    private static readonly DateOnly JourJ_25_06_2026 = new(2026, 6, 25); // jeudi, dans la fenêtre

    private static FakeTransfertRepository TransfertPapaVersMamanLeJ()
    {
        var repo = new FakeTransfertRepository();
        repo.Enregistrer(Transfert
            .Definir("papa", "maman", "ecole", TimeSpan.FromHours(8.5), JourJ_25_06_2026.ToDateTime(TimeOnly.MinValue)).Valeur!);
        return repo;
    }

    private static GrilleAgendaQuery Query(ITransfertRepository transferts)
        => new(
            new FakeSlotRepository(),
            new FakePeriodeRepository(),
            new FakePaletteCouleurs(new Dictionary<string, string> { ["papa"] = "bleu", ["maman"] = "rose" }),
            new FakeReferentielResponsables(new Dictionary<string, string> { ["papa"] = "Papa", ["maman"] = "Maman" }),
            transferts: transferts);

    // ---------- Test d'acceptation (boucle externe, BDD) ----------

    [Fact]
    public void Should_porter_l_info_bicolore_couleurs_depart_papa_arrivee_maman_sur_la_case_du_jour_When_un_transfert_est_saisi_ce_jour()
    {
        var grille = Query(TransfertPapaVersMamanLeJ()).Projeter(Reference_24_06_2026);

        var caseJ = grille.Jours.Single(j => j.Date == JourJ_25_06_2026);
        Assert.NotNull(caseJ.Transfert);
        Assert.Equal("bleu", caseJ.Transfert!.CouleurDepart);  // Papa, déposant
        Assert.Equal("rose", caseJ.Transfert!.CouleurArrivee); // Maman, récupérant
    }

    // ---------- Tests unitaires (boucle interne, TDD) ----------

    // Test #1 — présence : un jour couvert par un transfert porte une info bicolore non nulle.
    [Fact]
    public void Should_porter_une_info_bicolore_non_nulle_When_le_jour_est_couvert_par_un_transfert()
    {
        var grille = Query(TransfertPapaVersMamanLeJ()).Projeter(Reference_24_06_2026);

        Assert.NotNull(grille.Jours.Single(j => j.Date == JourJ_25_06_2026).Transfert);
    }

    // Test #2 — résolution des couleurs sur identifiant stable (départ = déposant, arrivée = récupérant).
    [Fact]
    public void Should_resoudre_depart_sur_le_deposant_et_arrivee_sur_le_recuperant_When_les_couleurs_sont_au_referentiel()
    {
        var info = Query(TransfertPapaVersMamanLeJ()).Projeter(Reference_24_06_2026)
            .Jours.Single(j => j.Date == JourJ_25_06_2026).Transfert!;

        Assert.Equal("bleu", info.CouleurDepart);
        Assert.Equal("rose", info.CouleurArrivee);
    }

    // Test #3 — la résolution de responsabilité de la case reste inchangée (info bicolore additive) :
    // sans période ni fond, la case reste neutre et sans nom, malgré le transfert.
    [Fact]
    public void Should_laisser_la_resolution_de_responsabilite_inchangee_When_un_transfert_couvre_la_case()
    {
        var caseJ = Query(TransfertPapaVersMamanLeJ()).Projeter(Reference_24_06_2026)
            .Jours.Single(j => j.Date == JourJ_25_06_2026);

        Assert.Equal(FakePaletteCouleurs.Neutre, caseJ.CouleurResponsable);
        Assert.Equal("", caseJ.NomResponsable);
    }
}
