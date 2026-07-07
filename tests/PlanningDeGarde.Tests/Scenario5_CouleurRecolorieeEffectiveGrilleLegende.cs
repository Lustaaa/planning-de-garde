using System;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 27 — S5 — Une couleur recoloriée en config est effective sur la grille et la légende (@back)
//   FILET DE NON-RÉGRESSION de la convergence config ↔ grille ↔ légende (s20) : la couleur est déjà
//   effective (IPaletteCouleurs sur store vivant) — ce test VERROUILLE que « config → planning » tient
//   pour la couleur, il ne pilote AUCUN code neuf (vert attendu dès le 1er run, caractérisation).
//
//   Frontière Application, câblage réel : store ConfigurationFoyerEnMemoire (seed parent-a = bleu),
//   période affectant parent-a, recolorie via le handler de config EditerActeur, puis re-projection de
//   la grille — la case ET la légende relisent la dernière écriture (rouge). On NE teste PAS de rendu Blazor.
public class Scenario5_CouleurRecolorieeEffectiveGrilleLegende
{
    private const string ParentA = "parent-a"; // seed : nom « Alice », couleur « bleu »
    private const string Bleu = "bleu";
    private const string Rouge = "rouge";

    private static readonly DateOnly Lundi_29_06_2026 = new(2026, 6, 29);

    private static FakePeriodeRepository GardeAffecteeAParentALe_29_06()
    {
        var periodes = new FakePeriodeRepository();
        periodes.Enregistrer(PeriodeDeGarde
            .Affecter(ParentA, new DateTime(2026, 6, 29), new DateTime(2026, 6, 29)).Valeur!);
        return periodes;
    }

    // ---------- Acceptation (boucle externe, frontière Application) ----------
    // Étant donné parent-a colorié « bleu » et une garde qui lui est affectée, quand un parent le
    // recolorie « rouge » depuis l'écran de config, alors la case de sa garde ET la légende se résolvent
    // en « rouge » (la palette relit la dernière écriture — convergence config → planning tenue).
    [Fact]
    public void Acceptation_Should_Resoudre_la_case_et_la_legende_en_rouge_When_parent_a_est_recolorie_rouge_en_config()
    {
        var store = new ConfigurationFoyerEnMemoire();                 // store vivant (seed parent-a = bleu)
        var periodes = GardeAffecteeAParentALe_29_06();
        var editerActeur = new EditerActeurHandler(store, new FakeNotificateurPlanning());
        var grilleQuery = new GrilleAgendaQuery(new FakeSlotRepository(), periodes, store, store, null, store);

        // Caractérisation d'origine : avant recoloriage, la case et la légende sont « bleu » (seed).
        var avant = grilleQuery.Projeter(Lundi_29_06_2026);
        Assert.Equal(Bleu, avant.Jours.Single(j => j.Date == Lundi_29_06_2026).CouleurResponsable);
        Assert.Equal(Bleu, Assert.Single(avant.Légende).Couleur);

        // Recoloriage « rouge » via le handler de config (chemin d'écriture config foyer).
        var recolor = editerActeur.Handle(new EditerActeurCommand(ParentA, Couleur: Rouge));
        Assert.True(recolor.EstSucces);

        // La grille re-projetée relit la dernière écriture : case ET légende en « rouge ».
        var apres = grilleQuery.Projeter(Lundi_29_06_2026);
        var caseGarde = apres.Jours.Single(j => j.Date == Lundi_29_06_2026);
        Assert.Equal(Rouge, caseGarde.CouleurResponsable);          // la case de la garde se résout en rouge
        var entree = Assert.Single(apres.Légende);
        Assert.Equal(ParentA, entree.IdentifiantStable);
        Assert.Equal(Rouge, entree.Couleur);                        // la légende affiche rouge pour parent-a
    }
}
