using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 10 — Sc.2 — Une surcharge ponctuelle prime sur le fond puis le cycle reprend (@nominal, 🖥️ IHM)
//   Tranche BACKEND (tdd-auto) : CARACTÉRISATION (⚠️ early green ATTENDU, garde-fou de non-régression
//   des périodes explicites — PAS un driver). La priorité surcharge (période saisie) > fond (cycle) est
//   STRUCTURELLE : dans GrilleAgendaQuery.CaseJourAu, la branche `else période` reste intacte et le fond
//   ne s'applique QUE dans la branche `periode is null`. Une période explicite prime donc sans code neuf,
//   et chaque case résout indépendamment sa propre date → le fond ne déborde pas sur les jours voisins.
//   Ce test verrouille cette non-régression ; il ne pilote aucun rouge (cf. CP, 99-sprint10-retours.md).
//
//   L'acceptation RUNTIME (grille câblée : surcharge primer puis fond reprendre) est menée séparément
//   par ihm-builder. On NE teste PAS ici un rendu Blazor.
//
//   Données : cycle N=2, index pair → parent-a (Alice bleu), impair → parent-b (Bruno orange). Semaine
//   ISO 28 (06–12/07/2026, PAIRE) revient par défaut à Parent A (fond). Surcharge explicite : Parent B
//   affecté à la SEULE journée du 08/07/2026.
public class Scenario2_SurchargePrimeSurFond
{
    private const string ParentA = "parent-a";
    private const string ParentB = "parent-b";
    private const string Alice = "Alice";
    private const string Bruno = "Bruno";
    private const string Bleu = "bleu";
    private const string Orange = "orange";

    private static readonly DateOnly Lundi_29_06_2026 = new(2026, 6, 29); // ISO 27 — date de référence (fenêtre couvrant ISO 27→31)
    private static readonly DateOnly Mardi_07_07_2026 = new(2026, 7, 7);  // ISO 28, paire — sans période → fond Parent A
    private static readonly DateOnly Mercredi_08_07_2026 = new(2026, 7, 8); // ISO 28 — surcharge explicite Parent B
    private static readonly DateOnly Jeudi_09_07_2026 = new(2026, 7, 9);  // ISO 28, paire — sans période → fond Parent A

    private static Dictionary<int, string> MappingPairAImpairB()
        => new() { [0] = ParentA, [1] = ParentB }; // index pair → Parent A, impair → Parent B

    // Test #1 — Caractérisation (⚠️ early green attendu, pas driver) : sur une semaine couverte par le
    // fond (ISO 28 paire → Parent A), une période explicite Parent B au SEUL 08/07 surcharge cette
    // journée (Bruno/orange), tandis que les jours voisins 07 et 09/07, sans période, restent au fond
    // (Alice/bleu). La primauté surcharge > fond est structurelle (branche `else période` intacte) ;
    // le fond ne déborde pas sur les jours voisins (chaque case résout indépendamment sa date).
    [Fact]
    public void Should_Afficher_Bruno_orange_le_jour_surcharge_et_Alice_bleu_de_part_et_d_autre_When_une_journee_isolee_est_affectee_explicitement_sur_une_semaine_couverte_par_le_fond()
    {
        var periodes = new FakePeriodeRepository();
        periodes.Enregistrer(PeriodeDeGarde.Affecter(ParentB,
            new DateTime(2026, 7, 8), new DateTime(2026, 7, 8)).Valeur!); // surcharge au seul 08/07

        var grille = QueryAvec(periodes, CycleDeuxSemaines()).Projeter(Lundi_29_06_2026);

        // 08/07 : surcharge explicite Parent B → Bruno/orange (priorité surcharge > fond)
        var caseSurcharge = grille.Jours.Single(j => j.Date == Mercredi_08_07_2026);
        Assert.Equal(Bruno, caseSurcharge.NomResponsable);
        Assert.Equal(Orange, caseSurcharge.CouleurResponsable);

        // 07/07 et 09/07 : aucune période → le fond reprend de part et d'autre (Parent A → Alice/bleu)
        foreach (var date in new[] { Mardi_07_07_2026, Jeudi_09_07_2026 })
        {
            var caseFond = grille.Jours.Single(j => j.Date == date);
            Assert.Equal(Alice, caseFond.NomResponsable);
            Assert.Equal(Bleu, caseFond.CouleurResponsable);
        }
    }

    // ---------- Helpers ----------

    private static GrilleAgendaQuery QueryAvec(IPeriodeRepository periodes, IReferentielCycleDeFond cycle)
        => new(new FakeSlotRepository(), periodes,
            new FakePaletteCouleurs(new Dictionary<string, string> { [ParentA] = Bleu, [ParentB] = Orange }),
            new FakeReferentielResponsables(new Dictionary<string, string> { [ParentA] = Alice, [ParentB] = Bruno }),
            cycle);

    private static IReferentielCycleDeFond CycleDeuxSemaines()
        => new FakeReferentielCycleDeFond(new CycleDeFond(2, MappingPairAImpairB()));
}
