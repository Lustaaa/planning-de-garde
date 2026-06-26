using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Scénario 7 — Acteur hors set retombe sur le neutre (gris assumé) (@limite)
//   Given le set de couleurs ne contient PAS l'identifiant "grand-pere" ; une période est
//         affectée au responsable d'identifiant stable "grand-pere" le 24/06/2026
//   When  la grille est projetée
//   Then  la case du 24/06/2026 est grise par repli neutre CONFORME ; et ce gris traduit un
//         acteur non encore colorié, pas un défaut de résolution
//
// CARACTÉRISATION (early green attendu) : le repli neutre sur clé absente est garanti par le
// CONTRAT d'IPaletteCouleurs.CouleurDe (déjà vert, Scenario_CouleurResponsableCaseJour). Ce
// test ne corrige rien — il caractérise le GRIS ASSUMÉ (acteur légitimement hors set, règle
// 17) pour le DISTINGUER du gris-BUG du Sc.8 (libellé fourni à la place de l'identifiant).
// Discriminance : la période est BIEN PRÉSENTE (grand-pere affecté, case COUVERTE) → le gris
// vient du repli légitime, pas d'une absence de période. On asserte aussi qu'un parent du set
// (parent-a=bleu) coexistant garde sa couleur — donc la résolution fonctionne, c'est bien un
// acteur non colorié et non un défaut de mapping.
public class Scenario_ActeurHorsSetGris
{
    private const string GrandPere = "grand-pere";
    private const string ParentA = "parent-a";
    private const string Bleu = "bleu";

    private static readonly DateOnly Date_24_06_2026 = new(2026, 6, 24);
    private static readonly DateOnly Lundi_22_06_2026 = new(2026, 6, 22);
    private static readonly DateOnly Mardi_23_06_2026 = new(2026, 6, 23);
    private static readonly DateOnly Mercredi_24_06_2026 = new(2026, 6, 24);

    // Set qui colore parent-a en bleu mais NE CONTIENT PAS grand-pere → repli neutre conforme.
    private static IPaletteCouleurs PaletteSansGrandPere()
        => new FakePaletteCouleurs(new Dictionary<string, string>
        {
            [ParentA] = Bleu,
        });

    // Deux périodes : parent-a couvre le 22-23/06 (dans le set → bleu) ; grand-pere couvre le
    // 24/06 (hors set → gris assumé). Coexistence = discriminance : la case de grand-pere est
    // grise alors qu'une autre case couverte (parent-a) est bleue → la résolution marche, c'est
    // un acteur non colorié, pas un bug.
    private static FakePeriodeRepository PeriodesParentABleuEtGrandPereHorsSet()
    {
        var periodes = new FakePeriodeRepository();
        var pParentA = PeriodeDeGarde
            .Affecter(ParentA, new DateTime(2026, 6, 22), new DateTime(2026, 6, 23))
            .Valeur!;
        var pGrandPere = PeriodeDeGarde
            .Affecter(GrandPere, new DateTime(2026, 6, 24), new DateTime(2026, 6, 24))
            .Valeur!;
        periodes.Enregistrer(pParentA);
        periodes.Enregistrer(pGrandPere);
        return periodes;
    }

    private static GrilleAgendaQuery Query()
        => new(new FakeSlotRepository(), PeriodesParentABleuEtGrandPereHorsSet(), PaletteSansGrandPere(),
            new FakeReferentielResponsables(new Dictionary<string, string>()));

    // ---------- Test d'acceptation (boucle externe, BDD) ----------

    [Fact]
    public void Should_Colorer_la_case_du_24_06_2026_en_gris_neutre_conforme_When_une_periode_est_affectee_a_un_acteur_d_identifiant_stable_absent_du_set()
    {
        // Given — set sans grand-pere ; grand-pere affecté le 24/06, parent-a (bleu) le 22-23/06
        var query = Query();

        // When — la grille est projetée le 24/06/2026
        var grille = query.Projeter(Date_24_06_2026);

        // Then — la case du 24/06 (couverte par grand-pere, hors set) est grise par repli neutre
        var caseGrandPere = grille.Jours.Single(j => j.Date == Mercredi_24_06_2026);
        Assert.Equal(FakePaletteCouleurs.Neutre, caseGrandPere.CouleurResponsable);

        // And — ce gris traduit un acteur non colorié, PAS un défaut de résolution : un parent
        // du set coexistant (parent-a) garde bien sa couleur bleue.
        var caseParentA = grille.Jours.Single(j => j.Date == Lundi_22_06_2026);
        Assert.Equal(Bleu, caseParentA.CouleurResponsable);
    }

    // ---------- Test unitaire (boucle interne, TDD) ----------

    // Test #1 — la case COUVERTE par grand-pere (hors set) reçoit la couleur neutre, en présence
    // d'un parent-a=bleu coexistant. Discriminance : la période grand-pere est bien présente
    // (case couverte), donc le gris vient du repli légitime du contrat CouleurDe, pas d'une
    // absence de couverture. Couplé à parent-a=bleu pour prouver que la résolution fonctionne.
    // Early green ANTICIPÉ (caractérisation) : CouleurDe renvoie déjà CouleurNeutre sur clé absente.
    [Fact]
    public void Should_Replier_la_case_du_24_06_2026_sur_la_couleur_neutre_When_l_acteur_grand_pere_est_absent_du_set_de_couleurs_mais_bien_affecte()
    {
        var query = Query();

        var grille = query.Projeter(Date_24_06_2026);

        // case couverte par grand-pere (hors set) → gris assumé
        var caseGrandPere = grille.Jours.Single(j => j.Date == Mercredi_24_06_2026);
        Assert.Equal(FakePaletteCouleurs.Neutre, caseGrandPere.CouleurResponsable);

        // discriminance : une case couverte par parent-a (dans le set) reste bleue → la
        // résolution n'est pas en défaut, grand-pere est juste non colorié
        var caseParentA = grille.Jours.Single(j => j.Date == Mardi_23_06_2026);
        Assert.Equal(Bleu, caseParentA.CouleurResponsable);
    }
}
