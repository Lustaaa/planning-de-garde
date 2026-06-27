using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 10 — Sc.1 — Définir un cycle de 2 semaines : le fond alterne par parité ISO (@nominal, 🖥️ IHM)
//   Tranche BACKEND (tdd-auto) : la couche de résolution du « fond » sous les périodes. Un parent
//   définit un cycle de 2 semaines (index pair → Parent A, impair → Parent B) ; sans aucune période
//   explicite, la grille résout le responsable de fond d'un jour par parité ISO
//   (index = ISOWeek(jour) mod N), case + nom + couleur + légende, sur l'identifiant STABLE.
//   L'acceptation RUNTIME (grille réellement câblée affichant le fond, front WASM + API distante +
//   SignalR) est menée séparément par ihm-builder. On NE teste PAS ici un rendu Blazor.
//
//   Données (déterministes, via Projeter(dateReference) — jamais Now) : parent-a = Alice bleu,
//   parent-b = Bruno orange (seed Foyer). Date de référence = lundi 29/06/2026 (ISO 27, impaire).
//   Fenêtre 5 semaines : ISO 27 (29/06–05/07) impaire → Parent B ; ISO 28 (06–12/07) paire → Parent A.
public class Scenario1_DefinirCycleAlternanceISO
{
    private const string ParentA = "parent-a";
    private const string ParentB = "parent-b";
    private const string Alice = "Alice";
    private const string Bruno = "Bruno";
    private const string Bleu = "bleu";
    private const string Orange = "orange";

    private static readonly DateOnly Lundi_29_06_2026 = new(2026, 6, 29);   // ISO 27 (impaire)
    private static readonly DateOnly Dimanche_05_07_2026 = new(2026, 7, 5);
    private static readonly DateOnly Lundi_06_07_2026 = new(2026, 7, 6);    // ISO 28 (paire)
    private static readonly DateOnly Dimanche_12_07_2026 = new(2026, 7, 12);

    private static Dictionary<int, string> MappingPairAImpairB()
        => new() { [0] = ParentA, [1] = ParentB }; // index pair → Parent A, impair → Parent B

    // ---------- Acceptation (boucle externe, frontière Application) ----------
    // Traduit le Gherkin sans IHM : via DefinirCycleHandler (définir un cycle de 2 semaines, SUCCÈS)
    // puis GrilleAgendaQuery.Projeter(29/06/2026) sur le MÊME store de cycle, les jours ISO 27
    // affichent Bruno/orange et les jours ISO 28 Alice/bleu, en case ET en légende, sur l'id stable.
    [Fact]
    public void Acceptation_Should_Resoudre_le_fond_Bruno_orange_sur_ISO_27_impaire_et_Alice_bleu_sur_ISO_28_paire_en_case_et_en_legende_When_un_cycle_de_deux_semaines_est_defini()
    {
        var foyer = new ConfigurationFoyerEnMemoire();              // parent-a=Alice/bleu, parent-b=Bruno/orange (seed Foyer)
        var cycleStore = new CycleDeFondEnMemoire();
        var handler = new DefinirCycleHandler(cycleStore, new FakeNotificateurPlanning());

        var resultat = handler.Handle(new DefinirCycleCommand(2, MappingPairAImpairB()));
        Assert.True(resultat.EstSucces);

        var grille = QueryAvec(cycleStore, foyer).Projeter(Lundi_29_06_2026);

        // ISO 27 (29/06–05/07, impaire) → Parent B en orange
        Assert.All(JoursEntre(grille, Lundi_29_06_2026, Dimanche_05_07_2026), j =>
        {
            Assert.Equal(Bruno, j.NomResponsable);
            Assert.Equal(Orange, j.CouleurResponsable);
        });
        // ISO 28 (06–12/07, paire) → Parent A en bleu
        Assert.All(JoursEntre(grille, Lundi_06_07_2026, Dimanche_12_07_2026), j =>
        {
            Assert.Equal(Alice, j.NomResponsable);
            Assert.Equal(Bleu, j.CouleurResponsable);
        });
        // ... en légende aussi (dédoublonnée par id stable)
        Assert.Contains(grille.Légende, e => e.IdentifiantStable == ParentB && e.Nom == Bruno && e.Couleur == Orange);
        Assert.Contains(grille.Légende, e => e.IdentifiantStable == ParentA && e.Nom == Alice && e.Couleur == Bleu);
    }

    // ---------- Tests unitaires (boucle interne, TDD) ----------

    // Test #1 — Driver : aujourd'hui un jour SANS période = couleur neutre + nom vide
    // (CaseJourAu : periode is null ? neutre). Aucune couche de fond n'existe. Force la résolution
    // du fond dans la branche periode is null : un jour de la semaine ISO 27 (impaire), sans période,
    // sous un cycle mappant l'index impair sur Parent B, doit afficher Bruno/orange.
    [Fact]
    public void Should_Afficher_Bruno_en_orange_en_fond_When_la_semaine_ISO_impaire_n_a_aucune_periode_et_le_cycle_mappe_l_index_impair_sur_Parent_B()
    {
        var grille = QueryAvec(CycleDeuxSemaines()).Projeter(Lundi_29_06_2026);

        var caseLundi = grille.Jours.Single(j => j.Date == Lundi_29_06_2026); // ISO 27, impaire
        Assert.Equal(Bruno, caseLundi.NomResponsable);
        Assert.Equal(Orange, caseLundi.CouleurResponsable);
    }

    // Test #2 — Driver : l'impl minimale du #1 (constante index impair → Bruno pour tout jour de
    // fond) est CASSÉE par une semaine de parité opposée. ISO 28 (paire) doit résoudre l'index PAIR
    // → Alice/bleu. Force le calcul réel index = ISOWeek(jour) mod N → mapping[index], contredisant
    // la constante.
    [Fact]
    public void Should_Afficher_Alice_en_bleu_en_fond_When_la_semaine_ISO_paire_n_a_aucune_periode_et_le_cycle_mappe_l_index_pair_sur_Parent_A()
    {
        var grille = QueryAvec(CycleDeuxSemaines()).Projeter(Lundi_29_06_2026);

        var caseLundi = grille.Jours.Single(j => j.Date == Lundi_06_07_2026); // ISO 28, paire
        Assert.Equal(Alice, caseLundi.NomResponsable);
        Assert.Equal(Bleu, caseLundi.CouleurResponsable);
    }

    // Test #3 — Driver : LegendeDesPresents n'agrège QUE les périodes ; sans période la légende est
    // VIDE alors que les cases portent le fond (incohérence « en case comme en légende »). Force
    // l'extension de la légende aux responsables de fond présents dans la fenêtre (Alice + Bruno),
    // dédoublonnés par id stable.
    [Fact]
    public void Should_Faire_figurer_le_responsable_de_fond_dans_la_legende_de_la_fenetre_When_le_cycle_couvre_des_jours_sans_periode_explicite()
    {
        var grille = QueryAvec(CycleDeuxSemaines()).Projeter(Lundi_29_06_2026);

        Assert.Contains(grille.Légende, e => e.IdentifiantStable == ParentA && e.Nom == Alice && e.Couleur == Bleu);
        Assert.Contains(grille.Légende, e => e.IdentifiantStable == ParentB && e.Nom == Bruno && e.Couleur == Orange);
        // dédoublonnée par id : exactement deux responsables de fond présents dans la fenêtre
        Assert.Equal(2, grille.Légende.Count);
    }

    // Test #4 — Caractérisation (⚠️ early green attendu, pas driver) : la résolution étant une
    // fonction pure de la date, ISO 29 (impaire) → Bruno et ISO 30 (paire) → Alice se résolvent par
    // le MÊME calcul que #1/#2, sans saisie supplémentaire. Filet verrouillant « l'alternance se
    // poursuit sur la fenêtre ».
    [Fact]
    public void Should_Poursuivre_l_alternance_sur_les_semaines_suivantes_sans_nouvelle_saisie_When_la_fenetre_couvre_plusieurs_semaines_du_cycle()
    {
        var grille = QueryAvec(CycleDeuxSemaines()).Projeter(Lundi_29_06_2026);

        var lundi_13_07 = new DateOnly(2026, 7, 13); // ISO 29, impaire → Bruno
        var lundi_20_07 = new DateOnly(2026, 7, 20); // ISO 30, paire   → Alice

        var caseIso29 = grille.Jours.Single(j => j.Date == lundi_13_07);
        Assert.Equal(Bruno, caseIso29.NomResponsable);
        Assert.Equal(Orange, caseIso29.CouleurResponsable);

        var caseIso30 = grille.Jours.Single(j => j.Date == lundi_20_07);
        Assert.Equal(Alice, caseIso30.NomResponsable);
        Assert.Equal(Bleu, caseIso30.CouleurResponsable);
    }

    // ---------- Helpers ----------

    private static GrilleAgendaQuery QueryAvec(IReferentielCycleDeFond cycle, ConfigurationFoyerEnMemoire foyer)
        => new(new FakeSlotRepository(), new FakePeriodeRepository(), foyer, foyer, cycle);

    private static GrilleAgendaQuery QueryAvec(IReferentielCycleDeFond cycle)
        => new(new FakeSlotRepository(), new FakePeriodeRepository(),
            new FakePaletteCouleurs(new Dictionary<string, string> { [ParentA] = Bleu, [ParentB] = Orange }),
            new FakeReferentielResponsables(new Dictionary<string, string> { [ParentA] = Alice, [ParentB] = Bruno }),
            cycle);

    private static IReferentielCycleDeFond CycleDeuxSemaines()
        => new FakeReferentielCycleDeFond(new CycleDeFond(2, MappingPairAImpairB()));

    private static IEnumerable<JourCase> JoursEntre(GrilleAgenda grille, DateOnly debut, DateOnly fin)
        => grille.Jours.Where(j => j.Date >= debut && j.Date <= fin);
}
