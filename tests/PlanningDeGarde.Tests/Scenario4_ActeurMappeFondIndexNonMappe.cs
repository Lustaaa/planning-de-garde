using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 13 — Sc.4 — Acteur mappé au cycle de fond : son index devient non mappé → neutre (@limite, @driver)
//   Tranche BACKEND (tdd-auto, frontière Application — projection GrilleAgendaQuery). Vrai RED neuf
//   DISTINCT du Sc.2 : ici l'acteur supprimé est porté par le FOND (le mapping du cycle), pas par une
//   surcharge. Le filtre d'existence du Sc.2 ne couvre que la surcharge → un fond orphelin résout encore
//   l'id brut (nom fantôme), en case ET en légende. Force l'extension du filtre d'existence à la branche
//   FOND : un responsable de fond supprimé est traité comme NON mappé → null → neutre, sans nom fantôme,
//   et disparaît aussi de la légende. Le mapping du cycle n'est PAS muté (CycleDeFond InMemory, règle 30) :
//   la neutralisation est observable à la RÉSOLUTION.
//
//   NB parité : 23/06/2026 est en semaine ISO 26 → index 26 % 2 = 0. On mappe donc l'index 0 sur Nounou
//   (et l'index 1 sur Parent A) pour que le jour observé tombe sur l'index porté par l'acteur supprimé.
public class Scenario4_ActeurMappeFondIndexNonMappe
{
    private const string ParentA = "parent-a";
    private const string Alice = "Alice";
    private const string Bleu = "bleu";

    private static readonly DateOnly Lundi_22_06_2026 = new(2026, 6, 22);  // date de référence (fenêtre couvrant le 23/06)
    private static readonly DateOnly Mardi_23_06_2026 = new(2026, 6, 23);  // index 0 → porté par le fond (Nounou), aucune période

    // ---------- Acceptation (boucle externe, frontière Application — store réel + handlers réels) ----------
    [Fact]
    public void Acceptation_Should_Rendre_l_index_du_cycle_non_mappe_et_la_case_neutre_sans_nom_fantome_When_l_acteur_mappe_au_cycle_de_fond_est_supprime()
    {
        var store = new ConfigurationFoyerEnMemoire();
        var nounouId = new AjouterActeurHandler(store)
            .Handle(new AjouterActeurCommand("Nounou", "vert")).Valeur!.ActeurId;

        // Cycle N=2 : index 0 → Nounou (= 23/06), index 1 → Parent A. AUCUNE période (la case est portée par le fond).
        var cycle = new FakeReferentielCycleDeFond(
            new CycleDeFond(2, new Dictionary<int, string> { [0] = nounouId, [1] = ParentA }));

        new SupprimerActeurHandler(store, new FakeNotificateurPlanning(), new FakeReferentielComptes(), new FakeReferentielComptes())
            .Handle(new SupprimerActeurCommand(nounouId)); // Nounou supprimée : son index de fond devient orphelin

        var query = new GrilleAgendaQuery(new FakeSlotRepository(), new FakePeriodeRepository(), store, store, cycle, store);
        var grille = query.Projeter(Lundi_22_06_2026);

        var caseMardi = grille.Jours.Single(j => j.Date == Mardi_23_06_2026);
        Assert.Equal("", caseMardi.NomResponsable);                      // index 0 orphelin → comme non mappé → aucun nom
        Assert.Equal(store.CouleurNeutre, caseMardi.CouleurResponsable); // ... teinte neutre, jamais un fantôme

        // Cohérence légende : le fond orphelin ne laisse aucune entrée fantôme.
        Assert.DoesNotContain(grille.Légende, e => e.IdentifiantStable == nounouId);
        Assert.DoesNotContain(grille.Légende, e => e.Nom == nounouId); // pas de nom fantôme (id brut) en légende
    }

    // ---------- Test #1 — Driver : un fond orphelin est neutralisé en case ET en légende ----------
    // Contradiction : l'impl du Sc.2 ne filtre que la surcharge ; un fond orphelin (ResponsableDeFond
    // renvoie l'id de l'acteur supprimé, le mapping restant inchangé) résout encore l'id brut → nom
    // fantôme en case et entrée fantôme en légende. Force l'extension du filtre d'existence à la branche
    // fond → null → neutre.
    [Fact]
    public void Should_Rendre_la_case_neutre_sans_nom_fantome_When_l_acteur_porte_par_le_fond_du_cycle_est_supprime()
    {
        const string NounouOrpheline = "acteur-nounou"; // mappée au fond mais ABSENTE de l'énumération (supprimée)

        // Cycle N=2 : index 0 (= 23/06) → NounouOrpheline, index 1 → Parent A. Aucune période.
        var cycle = new FakeReferentielCycleDeFond(
            new CycleDeFond(2, new Dictionary<int, string> { [0] = NounouOrpheline, [1] = ParentA }));

        // Référentiel / palette SANS l'acteur supprimé (NomDe retombe sur l'id brut, couleur sur le neutre)
        // ET énumération NE listant que Parent A : le contrat d'existence rend NounouOrpheline orpheline.
        var query = new GrilleAgendaQuery(
            new FakeSlotRepository(), new FakePeriodeRepository(),
            new FakePaletteCouleurs(new Dictionary<string, string> { [ParentA] = Bleu }),
            new FakeReferentielResponsables(new Dictionary<string, string> { [ParentA] = Alice }),
            cycle,
            new FakeEnumerationActeursFoyer(ParentA)); // Parent A existe ; NounouOrpheline n'existe plus

        var grille = query.Projeter(Lundi_22_06_2026);

        var caseMardi = grille.Jours.Single(j => j.Date == Mardi_23_06_2026);
        Assert.Equal("", caseMardi.NomResponsable);                          // fond orphelin → neutralisé → aucun nom
        Assert.Equal(FakePaletteCouleurs.Neutre, caseMardi.CouleurResponsable); // ... repli neutre

        // Le fond orphelin ne doit pas non plus apparaître en légende (pas d'entrée fantôme).
        Assert.DoesNotContain(grille.Légende, e => e.IdentifiantStable == NounouOrpheline);
    }
}
