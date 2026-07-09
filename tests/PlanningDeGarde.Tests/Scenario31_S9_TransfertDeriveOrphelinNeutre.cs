using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 31 — Sc.9 (D3) — Erreur orphelin (R6) : acteur d'un transfert dérivé supprimé (@back)
//   Étant donné une succession de périodes qui dériverait un transfert le jour J
//   Et le cédant OU le recevant a été supprimé du référentiel (R6)
//   Quand la résolution s'exécute sur le jour de bascule
//   Alors le côté orphelin retombe sur le neutre (sans nom fantôme)
//   Et aucune couleur/nom n'est résolu pour l'acteur supprimé
//
// Frontière Application (GrilleAgendaQuery). Discriminant fort (comme s29 S12) : la palette porte
// ENCORE une couleur STALE pour l'acteur supprimé ; c'est le contrat d'existence
// (IEnumerationActeursFoyer / Resolvable) — appliqué aux DEUX côtés du transfert dérivé — qui
// neutralise, pas l'absence de palette. La bascule reste dérivée (les périodes existent) ; seul le
// côté orphelin retombe sur le neutre.
public class Scenario31_S9_TransfertDeriveOrphelinNeutre
{
    private static readonly DateOnly Reference_24_06_2026 = new(2026, 6, 24);
    private static readonly DateOnly JourBascule_26_06_2026 = new(2026, 6, 26);

    private static FakePeriodeRepository SuccessionCedantPuisRecevant()
    {
        var repo = new FakePeriodeRepository();
        repo.Enregistrer(PeriodeDeGarde.Affecter(
            "cedant", new DateTime(2026, 6, 23), new DateTime(2026, 6, 25, 23, 59, 0)).Valeur!);
        repo.Enregistrer(PeriodeDeGarde.Affecter(
            "recevant", new DateTime(2026, 6, 26), new DateTime(2026, 6, 28)).Valeur!);
        return repo;
    }

    // Palette et référentiel portent ENCORE les deux acteurs (entrées stale) ; seul le set d'acteurs
    // du foyer (contrat d'existence) reflète la suppression.
    private static GrilleAgendaQuery Query(IEnumerationActeursFoyer acteurs)
        => new(
            new FakeSlotRepository(),
            SuccessionCedantPuisRecevant(),
            new FakePaletteCouleurs(new Dictionary<string, string> { ["cedant"] = "bleu", ["recevant"] = "rose" }),
            new FakeReferentielResponsables(new Dictionary<string, string> { ["cedant"] = "Cédant", ["recevant"] = "Recevant" }),
            acteurs: acteurs);

    // ---------- Tests d'acceptation (boucle externe, BDD) ----------

    [Fact]
    public void Should_neutraliser_le_depart_orphelin_sans_couleur_fantome_When_le_cedant_a_ete_supprime()
    {
        // Given — le cédant a été supprimé du référentiel (seul "recevant" existe encore).
        var caseBascule = Query(new FakeEnumerationActeursFoyer("recevant"))
            .Projeter(Reference_24_06_2026)
            .Jours.Single(j => j.Date == JourBascule_26_06_2026);

        // Then — la bascule reste dérivée, mais le DÉPART (cédant orphelin) retombe sur le neutre,
        // sans couleur fantôme ; l'arrivée (recevant, existant) est résolue normalement.
        Assert.NotNull(caseBascule.Transfert);
        Assert.Equal(FakePaletteCouleurs.Neutre, caseBascule.Transfert!.CouleurDepart);
        Assert.NotEqual("bleu", caseBascule.Transfert!.CouleurDepart);
        Assert.Equal("rose", caseBascule.Transfert!.CouleurArrivee);
    }

    [Fact]
    public void Should_neutraliser_l_arrivee_orpheline_sans_couleur_fantome_When_le_recevant_a_ete_supprime()
    {
        // Given — le recevant a été supprimé du référentiel (seul "cedant" existe encore).
        var caseBascule = Query(new FakeEnumerationActeursFoyer("cedant"))
            .Projeter(Reference_24_06_2026)
            .Jours.Single(j => j.Date == JourBascule_26_06_2026);

        // Then — l'ARRIVÉE (recevant orphelin) retombe sur le neutre, sans couleur fantôme ; le départ
        // (cédant, existant) est résolu normalement.
        Assert.NotNull(caseBascule.Transfert);
        Assert.Equal("bleu", caseBascule.Transfert!.CouleurDepart);
        Assert.Equal(FakePaletteCouleurs.Neutre, caseBascule.Transfert!.CouleurArrivee);
        Assert.NotEqual("rose", caseBascule.Transfert!.CouleurArrivee);
    }
}
