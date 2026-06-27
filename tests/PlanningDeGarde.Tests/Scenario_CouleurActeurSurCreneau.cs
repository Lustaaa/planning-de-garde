using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Scénario 4 — Le slot d'un acteur non-responsable porte sa propre couleur sur son créneau (@nominal)
//   Given On est le 24/06/2026, le set par défaut associe Parent A au bleu et Nounou au vert,
//         une période confie Léa à Parent A le jeudi 25/06/2026 et un slot 'nounou' est
//         enregistré ce même jour de 17h00 à 19h00
//   When  Un Parent ouvre le hub /planning
//   Then  La case du jeudi 25/06/2026 porte la couleur de Parent A (bleu) au niveau de la
//         journée, et le créneau 'nounou 17h00–19h00' à l'intérieur de la case porte la
//         couleur de Nounou (vert)
//
// Projection backend GrilleAgendaQuery — testée sans Blazor. Deux niveaux de couleur distincts
// et coexistants : JourCase.CouleurResponsable (responsabilité du jour) vs SlotCase.CouleurActeur
// (acteur du créneau), lus du même set via le port IPaletteCouleurs (doublé à la main). Date de
// référence injectée ; fakes peuplés via les agrégats.
public class Scenario_CouleurActeurSurCreneau
{
    private const string ParentA = "parent-a";
    private const string Nounou = "nounou";
    private const string Bleu = "bleu";
    private const string Vert = "vert";

    private static readonly DateOnly Date_24_06_2026 = new(2026, 6, 24);
    private static readonly DateOnly Jeudi_25_06_2026 = new(2026, 6, 25);
    private static readonly TimeOnly H17 = new(17, 0);
    private static readonly TimeOnly H19 = new(19, 0);

    private static IPaletteCouleurs PaletteParentABleuNounouVert()
        => new FakePaletteCouleurs(new Dictionary<string, string>
        {
            [ParentA] = Bleu,
            [Nounou] = Vert,
        });

    private static FakeSlotRepository SlotsAvecNounouLe_25_06_de_17h_a_19h()
    {
        var slots = new FakeSlotRepository();
        var slot = SlotDeLocalisation
            .Poser("lea", Nounou, new DateTime(2026, 6, 25, 17, 0, 0), new DateTime(2026, 6, 25, 19, 0, 0))
            .Valeur!;
        slots.Enregistrer(slot);
        return slots;
    }

    private static FakePeriodeRepository PeriodeConfiantLeaAParentALe_25_06()
    {
        var periodes = new FakePeriodeRepository();
        var periode = PeriodeDeGarde
            .Affecter(ParentA, new DateTime(2026, 6, 25), new DateTime(2026, 6, 25))
            .Valeur!;
        periodes.Enregistrer(periode);
        return periodes;
    }

    private static GrilleAgendaQuery Query()
        => new(
            SlotsAvecNounouLe_25_06_de_17h_a_19h(),
            PeriodeConfiantLeaAParentALe_25_06(),
            PaletteParentABleuNounouVert(),
            new FakeReferentielResponsables(new Dictionary<string, string>()));

    private static SlotCase CreneauNounouDuJeudi(GrilleAgenda grille)
        => grille.Jours.Single(j => j.Date == Jeudi_25_06_2026).Slots.Single();

    // ---------- Test d'acceptation (boucle externe, BDD) ----------

    [Fact]
    public void Should_Porter_la_couleur_de_Parent_A_sur_la_case_du_jeudi_25_06_2026_et_la_couleur_de_Nounou_sur_le_creneau_nounou_17h00_19h00_a_l_interieur_de_cette_case_When_une_periode_confie_Lea_a_Parent_A_ce_jour_et_un_slot_nounou_y_est_enregistre()
    {
        // Given — Parent A = bleu, Nounou = vert ; période Parent A le 25/06 + slot nounou 17h→19h
        var query = Query();

        // When — un Parent consulte la grille le 24/06/2026
        var grille = query.Projeter(Date_24_06_2026);

        // Then — la case du jeudi 25/06 porte la couleur de Parent A (bleu) au niveau journée...
        var caseJeudi = grille.Jours.Single(j => j.Date == Jeudi_25_06_2026);
        Assert.Equal(Bleu, caseJeudi.CouleurResponsable);

        // And — le créneau 'nounou 17h00–19h00' porte la couleur de Nounou (vert)...
        var creneau = Assert.Single(caseJeudi.Slots);
        Assert.Equal(Nounou, creneau.Libelle);
        Assert.Equal(H17, creneau.Debut);
        Assert.Equal(H19, creneau.Fin);
        Assert.Equal(Vert, creneau.CouleurActeur);

        // And — les deux niveaux de couleur sont distincts (journée bleue / créneau vert)
        Assert.NotEqual(caseJeudi.CouleurResponsable, creneau.CouleurActeur);
    }

    // ---------- Tests unitaires (boucle interne, TDD) ----------

    // Test #1 — le créneau 'nounou' porte la couleur propre de son acteur (vert) via le set,
    // indépendamment du responsable de la journée. (TPP constante → valeur dérivée : mapping de
    // l'acteur du slot → couleur.) Driver : le SlotCase du Sc.2 n'a pas de couleur ; ce test
    // force le mapping de l'acteur du slot vers sa propre couleur.
    [Fact]
    public void Should_Attribuer_au_creneau_nounou_la_couleur_propre_de_Nounou_When_un_slot_nounou_est_place_dans_la_case()
    {
        var grille = Query().Projeter(Date_24_06_2026);

        var creneau = CreneauNounouDuJeudi(grille);
        Assert.Equal(Vert, creneau.CouleurActeur);
    }

    // Test #2 — coexistence de deux niveaux distincts : la couleur de journée (Parent A = bleu)
    // et la couleur de créneau (Nounou = vert) coexistent dans la même case sans se confondre.
    // Driver : une implémentation qui ferait porter au slot la couleur de la case-jour (un seul
    // niveau) satisfait le Sc.3 mais échoue ici ; force la séparation des deux niveaux.
    [Fact]
    public void Should_Faire_coexister_la_couleur_de_journee_de_Parent_A_et_la_couleur_de_creneau_de_Nounou_dans_la_meme_case_When_une_periode_de_Parent_A_couvre_le_jour_du_slot_nounou()
    {
        var grille = Query().Projeter(Date_24_06_2026);

        var caseJeudi = grille.Jours.Single(j => j.Date == Jeudi_25_06_2026);
        Assert.Equal(Bleu, caseJeudi.CouleurResponsable);
        Assert.Equal(Vert, caseJeudi.Slots.Single().CouleurActeur);
    }
}
