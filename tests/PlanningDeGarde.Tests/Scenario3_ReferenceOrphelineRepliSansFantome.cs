using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 19 — Sc.3 — Référence orpheline → repli surcharge > fond > neutre sans nom fantôme (@back)
//   Étant donné une surcharge (période) référençant un identifiant stable absent du store d'acteurs
//   Quand on projette la grille agenda
//   Alors la case orpheline retombe sur surcharge > fond > neutre selon la priorité acquise (palier 6)
//     Et aucun nom fantôme n'est affiché pour l'id orphelin (filtre Resolvable)
//     Et la légende n'expose pas l'acteur orphelin
//
// Acceptation à la frontière Application (CQRS lecture). Compose la CHAÎNE DE PRIORITÉ acquise sur
// une référence ORPHELINE (id présent dans une période mais ABSENT du store vivant des acteurs
// déclarés) :
//   - jour où un FOND résolvable couvre : surcharge orpheline neutralisée → la case retombe sur le
//     fond (surcharge > fond) ;
//   - jour SANS fond (index non mappé) : surcharge orpheline + pas de fond → repli neutre, aucun nom.
// L'orphelin porte un nom/couleur RÉSOLVABLES dans les doublures (« Fantôme » / « noir ») : la garde
// prouve qu'ils ne FUITENT jamais — ni en case, ni en légende (la légende n'expose pas l'orphelin).
//
// Dates déterministes (Projeter(ancre) — jamais Now), fenêtre 4 semaines (28 j) au 29/06/2026.
// Cycle N=2 mappant le SEUL index 1 (semaines ISO impaires) → resp-fond :
//   29/06 = ISO 27 (impaire, index 1) → fond resp-fond ; 06/07 = ISO 28 (paire, index 0) → pas de fond.
public class Scenario3_ReferenceOrphelineRepliSansFantome
{
    private const string RespFond = "resp-fond";    // acteur déclaré (existe dans le store vivant)
    private const string Orphelin = "orphelin-x";   // référencé par une période mais ABSENT du store
    private const string Fanny = "Fanny";
    private const string Rose = "rose";
    private const string Gris = FakePaletteCouleurs.Neutre;

    private static readonly DateOnly Lundi_29_06_2026 = new(2026, 6, 29); // ISO 27 (impaire) → fond resp-fond
    private static readonly DateOnly Lundi_06_07_2026 = new(2026, 7, 6);  // ISO 28 (paire)   → index 0 non mappé

    [Fact]
    public void Acceptation_Should_Faire_retomber_la_case_orpheline_sur_le_fond_puis_le_neutre_sans_nom_fantome_ni_entree_de_legende_When_une_periode_reference_un_id_absent_du_store()
    {
        // --- Given : store vivant = uniquement resp-fond (l'orphelin n'y figure pas) ---
        var storeVivant = new FakeEnumerationActeursFoyer(RespFond);

        // Doublures qui RÉSOUDRAIENT l'orphelin s'il n'était pas filtré : prouve l'absence de fuite.
        var referentiel = new FakeReferentielResponsables(new Dictionary<string, string>
        {
            [RespFond] = Fanny,
            [Orphelin] = "Fantôme",
        });
        var palette = new FakePaletteCouleurs(new Dictionary<string, string>
        {
            [RespFond] = Rose,
            [Orphelin] = "noir",
        });

        // deux surcharges sur l'ORPHELIN : l'une un jour avec fond, l'autre un jour sans fond
        var periodes = new FakePeriodeRepository();
        periodes.Enregistrer(PeriodeDeGarde
            .Affecter(Orphelin, Lundi_29_06_2026.ToDateTime(TimeOnly.MinValue), Lundi_29_06_2026.ToDateTime(TimeOnly.MinValue))
            .Valeur!);
        periodes.Enregistrer(PeriodeDeGarde
            .Affecter(Orphelin, Lundi_06_07_2026.ToDateTime(TimeOnly.MinValue), Lundi_06_07_2026.ToDateTime(TimeOnly.MinValue))
            .Valeur!);

        var cycle = new FakeReferentielCycleDeFond(new CycleDeFond(2, new Dictionary<int, string> { [1] = RespFond }));

        var query = new GrilleAgendaQuery(new FakeSlotRepository(), periodes, palette, referentiel, cycle, storeVivant);

        // --- When ---
        var grille = query.Projeter(Lundi_29_06_2026);

        // --- Then : surcharge orpheline + fond résolvable → la case retombe sur le FOND (surcharge > fond) ---
        var caseAvecFond = grille.Jours.Single(j => j.Date == Lundi_29_06_2026);
        Assert.Equal(Fanny, caseAvecFond.NomResponsable);
        Assert.Equal(Rose, caseAvecFond.CouleurResponsable);

        // --- Then : surcharge orpheline + pas de fond → repli NEUTRE, aucun nom fantôme ---
        var caseSansFond = grille.Jours.Single(j => j.Date == Lundi_06_07_2026);
        Assert.Equal("", caseSansFond.NomResponsable);
        Assert.Equal(Gris, caseSansFond.CouleurResponsable);

        // --- Then : aucun nom/couleur fantôme de l'orphelin ne fuit nulle part dans les cases ---
        Assert.DoesNotContain(grille.Jours, j => j.NomResponsable == "Fantôme");
        Assert.DoesNotContain(grille.Jours, j => j.CouleurResponsable == "noir");

        // --- Then : la légende n'expose PAS l'acteur orphelin (seul resp-fond, déclaré et référencé) ---
        Assert.DoesNotContain(grille.Légende, e => e.IdentifiantStable == Orphelin);
        Assert.DoesNotContain(grille.Légende, e => e.Nom == "Fantôme");
        var entree = Assert.Single(grille.Légende);
        Assert.Equal(RespFond, entree.IdentifiantStable);
        Assert.Equal(Fanny, entree.Nom);
        Assert.Equal(Rose, entree.Couleur);
    }
}
