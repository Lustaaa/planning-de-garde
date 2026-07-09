using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 31 — Sc.8 (D3) — Limite bord de fenêtre : J+1 hors fenêtre (@back)
//   Étant donné une période A se terminant le dernier jour de la fenêtre chargée
//   Et le jour J+1 est hors de la fenêtre chargée
//   Quand la résolution s'exécute sur le bord de la fenêtre
//   Alors aucun transfert dérivé fantôme n'est produit (pas de dérivation sur données non chargées)
//
// Frontière Application (GrilleAgendaQuery). Le jour de bascule = premier jour du successeur (J+1) ;
// s'il tombe HORS de la fenêtre projetée, aucune case ne le porte → aucune dérivation fantôme, alors
// même que le successeur EXISTE dans le store. Un contrôle (fenêtre ancrée pour inclure J+1) prouve
// que la succession EST dérivable : le vert du cas limite n'est donc pas vacant.
public class Scenario31_S8_BordDeFenetrePasDeFantome
{
    // Fenêtre par défaut (4 semaines) ancrée le 24/06/2026 : lundi 22/06 → dernier jour = 19/07/2026.
    private static readonly DateOnly Reference_24_06_2026 = new(2026, 6, 24);
    private static readonly DateOnly DernierJourFenetre_19_07_2026 = new(2026, 7, 19); // A finit ici (bord)
    private static readonly DateOnly HorsFenetre_20_07_2026 = new(2026, 7, 20);         // J+1 : B débute, hors fenêtre

    // A (Cédant) finit le dernier jour de la fenêtre ; B (Recevant) débute le lendemain, HORS fenêtre.
    private static FakePeriodeRepository SuccessionAuBord()
    {
        var repo = new FakePeriodeRepository();
        repo.Enregistrer(PeriodeDeGarde.Affecter(
            "cedant", new DateTime(2026, 7, 15), DernierJourFenetre_19_07_2026.ToDateTime(new TimeOnly(23, 59))).Valeur!);
        repo.Enregistrer(PeriodeDeGarde.Affecter(
            "recevant", HorsFenetre_20_07_2026.ToDateTime(new TimeOnly(0, 0)), new DateTime(2026, 7, 25)).Valeur!);
        return repo;
    }

    private static GrilleAgendaQuery Query(IPeriodeRepository periodes)
        => new(
            new FakeSlotRepository(),
            periodes,
            new FakePaletteCouleurs(new Dictionary<string, string> { ["cedant"] = "bleu", ["recevant"] = "rose" }),
            new FakeReferentielResponsables(new Dictionary<string, string> { ["cedant"] = "Cédant", ["recevant"] = "Recevant" }));

    // ---------- Test d'acceptation (boucle externe, BDD) ----------

    [Fact]
    public void Should_ne_produire_aucun_transfert_fantome_When_le_successeur_debute_hors_de_la_fenetre()
    {
        // Given/When — projection de la fenêtre par défaut ; A finit au bord, B débute juste après (hors fenêtre).
        var grille = Query(SuccessionAuBord()).Projeter(Reference_24_06_2026);

        // Then — le jour de bascule (J+1) n'est pas dans la fenêtre, donc aucune case ne le porte…
        Assert.DoesNotContain(grille.Jours, j => j.Date == HorsFenetre_20_07_2026);
        // …et le dernier jour de la fenêtre (fin de A) ne porte AUCUN transfert dérivé (pas de fantôme).
        Assert.Null(grille.Jours.Single(j => j.Date == DernierJourFenetre_19_07_2026).Transfert);
        // Aucune case de la fenêtre ne porte de transfert (aucune dérivation sur données non chargées).
        Assert.All(grille.Jours, j => Assert.Null(j.Transfert));
    }

    // ---------- Contrôle (le vert n'est pas vacant) ----------

    // La MÊME succession, projetée sur une fenêtre qui INCLUT J+1, dérive bien cedant→recevant ce jour-là :
    // seule la limite de fenêtre supprime la dérivation dans le cas d'acceptation.
    [Fact]
    public void Should_deriver_cedant_vers_recevant_le_J_plus_1_When_la_fenetre_inclut_le_jour_de_bascule()
    {
        // Ancre au 20/07/2026 (un lundi) : la fenêtre inclut alors le jour de bascule 20/07.
        var grille = Query(SuccessionAuBord()).Projeter(HorsFenetre_20_07_2026);

        var caseBascule = grille.Jours.Single(j => j.Date == HorsFenetre_20_07_2026);
        Assert.NotNull(caseBascule.Transfert);
        Assert.Equal("bleu", caseBascule.Transfert!.CouleurDepart);
        Assert.Equal("rose", caseBascule.Transfert!.CouleurArrivee);
    }
}
