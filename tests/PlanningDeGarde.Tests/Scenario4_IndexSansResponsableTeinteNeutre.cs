using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 10 — Sc.4 — Un index de cycle sans responsable retombe sur la teinte neutre (@limite)
//   Tranche BACKEND (tdd-auto) : contrat de repli « index non mappé → pas de fond → neutre »,
//   miroir de IPaletteCouleurs.CouleurDe (clé absente → neutre) et IReferentielResponsables.NomDe.
//   ⚠️ Annoncé « probablement early green » par tdd-analyse, mais tranché INCERTAIN par le CP :
//   la résolution du fond (CycleDeFond.ResponsableDeFond) indexe en DUR via _affectations[index],
//   qui LÈVE KeyNotFoundException sur un index non mappé → c'est un VRAI DRIVER. Le repli neutre
//   doit émerger (TryGetValue → null), priorité fond > neutre, sans casser la résolution d'un
//   index affecté (contrôle positif = pur Sc.1).
//
//   L'acceptation RUNTIME (grille câblée rendant les cases neutres + légende sans entrée fantôme,
//   front WASM + API distante + SignalR) est menée séparément par ihm-builder. On NE teste PAS ici
//   un rendu Blazor.
//
//   Données (déterministes, via Projeter(dateReference) — jamais Now) : parent-a = Alice bleu,
//   parent-b = Bruno orange (seed Foyer). Lundi 29/06/2026 = ISO 27 (impaire) ; lundi 06/07/2026 =
//   ISO 28 (paire).
public class Scenario4_IndexSansResponsableTeinteNeutre
{
    private const string ParentA = "parent-a";
    private const string ParentB = "parent-b";
    private const string Alice = "Alice";
    private const string Bruno = "Bruno";
    private const string Bleu = "bleu";
    private const string Orange = "orange";
    private const string Gris = FakePaletteCouleurs.Neutre;

    private static readonly DateOnly Lundi_29_06_2026 = new(2026, 6, 29); // ISO 27 (impaire) — index impair
    private static readonly DateOnly Lundi_06_07_2026 = new(2026, 7, 6);  // ISO 28 (paire)   — index pair

    private static Dictionary<int, string> MappingPairAImpairNonAffecte()
        => new() { [0] = ParentA }; // index pair → Parent A ; index impair NON affecté

    // ---------- Acceptation (boucle externe, frontière Application) ----------
    // Traduit le Gherkin sans IHM : via DefinirCycleHandler (cycle de 2 semaines, index pair → Parent A,
    // index impair NON affecté) puis GrilleAgendaQuery.Projeter(29/06/2026) sur le MÊME store de cycle —
    // la semaine ISO 27 (impaire, index non mappé) retombe en gris neutre sans nom et SANS entrée de
    // légende fantôme, tandis que la semaine ISO 28 (paire, index mappé) reste « Parent A » bleu.
    [Fact]
    public void Acceptation_Should_Retomber_en_gris_neutre_sans_nom_ni_entree_de_legende_sur_la_semaine_d_index_non_affecte_et_conserver_le_responsable_de_l_index_affecte_When_le_cycle_n_affecte_que_l_index_pair()
    {
        var foyer = new ConfigurationFoyerEnMemoire();              // parent-a=Alice/bleu, parent-b=Bruno/orange (seed Foyer)
        var cycleStore = new CycleDeFondEnMemoire();
        var handler = new DefinirCycleHandler(cycleStore, new FakeNotificateurPlanning());

        Assert.True(handler.Handle(new DefinirCycleCommand(2, MappingPairAImpairNonAffecte())).EstSucces);

        var grille = QueryAvec(cycleStore, foyer).Projeter(Lundi_29_06_2026);

        // ISO 27 (impaire, index non affecté) → gris neutre, sans nom
        var caseIso27 = grille.Jours.Single(j => j.Date == Lundi_29_06_2026);
        Assert.Equal("", caseIso27.NomResponsable);
        Assert.Equal(Gris, caseIso27.CouleurResponsable);

        // ISO 28 (paire, index affecté) → contrôle positif : Parent A bleu
        var caseIso28 = grille.Jours.Single(j => j.Date == Lundi_06_07_2026);
        Assert.Equal(Alice, caseIso28.NomResponsable);
        Assert.Equal(Bleu, caseIso28.CouleurResponsable);

        // Légende : Parent A présent (index pair mappé) ; aucune entrée fantôme pour l'index non affecté
        Assert.Contains(grille.Légende, e => e.IdentifiantStable == ParentA);
        Assert.DoesNotContain(grille.Légende, e => e.IdentifiantStable == ParentB);
        Assert.DoesNotContain(grille.Légende, e => string.IsNullOrEmpty(e.Nom));
    }

    // ---------- Test unitaire (boucle interne, TDD) — Theory, 3 exemples de l'Outline ----------
    // Driver : la résolution du fond indexe en dur (_affectations[index]) → KeyNotFoundException sur
    // un index non mappé. Force le repli « index non mappé → pas de fond → neutre » (TryGetValue →
    // null → branche neutre existante du CaseJourAu), sans entrée de légende fantôme, tout en gardant
    // intacte la résolution d'un index affecté (ex.2, contrôle positif = pur Sc.1).
    public static IEnumerable<object[]> Exemples() => new[]
    {
        // mapping partiel {pair → Parent A}, ISO 27 (impaire, index impair non affecté) → neutre
        new object[] { false, Lundi_29_06_2026, "",    Gris,  true  },
        // mapping partiel {pair → Parent A}, ISO 28 (paire, index pair affecté) → contrôle positif Parent A bleu
        new object[] { false, Lundi_06_07_2026, Alice, Bleu,  false },
        // cycle vide (aucun index affecté), ISO 27 (impaire) → neutre
        new object[] { true,  Lundi_29_06_2026, "",    Gris,  true  },
    };

    [Theory]
    [MemberData(nameof(Exemples))]
    public void Should_Retomber_sur_la_teinte_neutre_sans_nom_ni_entree_de_legende_When_l_index_de_la_semaine_n_est_associe_a_aucun_responsable_de_fond(
        bool cycleVide, DateOnly dateCible, string nomAttendu, string couleurAttendue, bool neutreAttendu)
    {
        var mapping = cycleVide ? new Dictionary<int, string>() : MappingPairAImpairNonAffecte();
        var cycle = new FakeReferentielCycleDeFond(new CycleDeFond(2, mapping));

        var grille = QueryAvec(cycle).Projeter(Lundi_29_06_2026);

        var caseCible = grille.Jours.Single(j => j.Date == dateCible);
        Assert.Equal(nomAttendu, caseCible.NomResponsable);
        Assert.Equal(couleurAttendue, caseCible.CouleurResponsable);

        if (neutreAttendu)
        {
            // Aucune entrée de légende fantôme pour l'index non affecté (ni Parent B, ni entrée sans nom)
            Assert.DoesNotContain(grille.Légende, e => e.IdentifiantStable == ParentB);
            Assert.DoesNotContain(grille.Légende, e => string.IsNullOrEmpty(e.Nom));
        }
    }

    // ---------- Helpers ----------

    private static GrilleAgendaQuery QueryAvec(IReferentielCycleDeFond cycle, ConfigurationFoyerEnMemoire foyer)
        => new(new FakeSlotRepository(), new FakePeriodeRepository(), foyer, foyer, cycle);

    private static GrilleAgendaQuery QueryAvec(IReferentielCycleDeFond cycle)
        => new(new FakeSlotRepository(), new FakePeriodeRepository(),
            new FakePaletteCouleurs(new Dictionary<string, string> { [ParentA] = Bleu, [ParentB] = Orange }),
            new FakeReferentielResponsables(new Dictionary<string, string> { [ParentA] = Alice, [ParentB] = Bruno }),
            cycle);
}
