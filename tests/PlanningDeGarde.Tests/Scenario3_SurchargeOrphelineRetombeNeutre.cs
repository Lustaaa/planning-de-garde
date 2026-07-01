using System;
using System.Collections.Generic;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 13 — Sc.3 — Surcharge orpheline sur un index de cycle NON mappé : repli neutre sans nom fantôme
//   (@limite, @caractérisation). Tranche BACKEND (tdd-auto) : CARACTÉRISATION (⚠️ early green ATTENDU,
//   filet anti-régression — PAS un driver). Le comportement émerge de la COMPOSITION de deux mécanismes
//   DÉJÀ verts, sans code neuf :
//     - le filtre d'existence du Sc.2 neutralise la surcharge orpheline (acteur supprimé) AVANT le fond ;
//     - le fond à un index de cycle NON mappé renvoie déjà `null` (CycleDeFond.ResponsableDeFond, s10 Sc.4).
//   La case n'a alors NI surcharge valide NI fond → `responsableId is null` → couleur neutre + nom vide
//   (jamais de nom fantôme : `nom = responsableId is null ? "" : NomDe(...)`). Si ce test passait ROUGE,
//   c'est que le filtre du Sc.2 a été posé au mauvais endroit (sur le responsableId combiné) — corriger
//   le Sc.2, ne pas ajouter de code ici.
//
//   NB parité : 23/06/2026 tombe sur la semaine ISO 26 → index 26 % 2 = 0. On laisse donc l'index 0 NON
//   mappé (le cycle mappe l'index 1) pour que le jour de la surcharge tombe sur un index sans fond.
public class Scenario3_SurchargeOrphelineRetombeNeutre
{
    private const string ParentA = "parent-a";
    private const string Alice = "Alice";
    private const string Bleu = "bleu";

    private static readonly DateOnly Lundi_22_06_2026 = new(2026, 6, 22);  // date de référence (fenêtre couvrant le 23/06)
    private static readonly DateOnly Mardi_23_06_2026 = new(2026, 6, 23);  // surcharge ponctuelle, sur un index de cycle NON mappé

    // Cycle N=2 mappant le SEUL index 1 sur Parent A : l'index 0 (= 23/06, ISO 26) reste NON mappé → pas de fond.
    private static IReferentielCycleDeFond CycleIndex0NonMappe()
        => new FakeReferentielCycleDeFond(new CycleDeFond(2, new Dictionary<int, string> { [1] = ParentA }));

    // ---------- Acceptation (boucle externe, frontière Application — store réel + handlers réels) ----------
    [Fact]
    public void Acceptation_Should_Faire_retomber_la_case_sur_la_teinte_neutre_sans_aucun_nom_When_l_acteur_d_une_surcharge_sur_un_index_non_mappe_est_supprime()
    {
        var store = new ConfigurationFoyerEnMemoire();
        var nounouId = new AjouterActeurHandler(store)
            .Handle(new AjouterActeurCommand("Nounou", "vert")).Valeur!.ActeurId;

        var periodes = new FakePeriodeRepository();
        periodes.Enregistrer(PeriodeDeGarde.Affecter(nounouId,
            new DateTime(2026, 6, 23), new DateTime(2026, 6, 23)).Valeur!); // surcharge au seul 23/06 (index 0 non mappé)

        new SupprimerActeurHandler(store, new FakeNotificateurPlanning(), new FakeReferentielComptes(), new FakeReferentielComptes())
            .Handle(new SupprimerActeurCommand(nounouId)); // Nounou supprimée : surcharge orpheline

        var query = new GrilleAgendaQuery(new FakeSlotRepository(), periodes, store, store, CycleIndex0NonMappe(), store);
        var grille = query.Projeter(Lundi_22_06_2026);

        var caseMardi = grille.Jours.Single(j => j.Date == Mardi_23_06_2026);
        Assert.Equal("", caseMardi.NomResponsable);                       // aucun nom (ni fantôme : ni surcharge valide ni fond)
        Assert.Equal(store.CouleurNeutre, caseMardi.CouleurResponsable);  // ... teinte neutre par contrat
    }

    // ---------- Test #1 — Caractérisation (⚠️ early green attendu, pas driver) ----------
    // Documente le repli neutre sans nom fantôme : surcharge orpheline neutralisée (Sc.2) + index de fond
    // non mappé (null) → la case n'a ni surcharge valide ni fond → couleur neutre + nom vide.
    [Fact]
    public void Should_Faire_retomber_la_case_sur_la_teinte_neutre_sans_aucun_nom_When_l_acteur_d_une_surcharge_sur_un_index_de_cycle_non_mappe_est_supprime()
    {
        const string NounouOrpheline = "acteur-nounou"; // référencée par la période mais ABSENTE de l'énumération (supprimée)

        var periodes = new FakePeriodeRepository();
        periodes.Enregistrer(PeriodeDeGarde.Affecter(NounouOrpheline,
            new DateTime(2026, 6, 23), new DateTime(2026, 6, 23)).Valeur!);

        var query = new GrilleAgendaQuery(
            new FakeSlotRepository(), periodes,
            new FakePaletteCouleurs(new Dictionary<string, string> { [ParentA] = Bleu }),
            new FakeReferentielResponsables(new Dictionary<string, string> { [ParentA] = Alice }),
            CycleIndex0NonMappe(),
            new FakeEnumerationActeursFoyer(ParentA)); // Parent A existe ; NounouOrpheline n'existe plus

        var grille = query.Projeter(Lundi_22_06_2026);

        var caseMardi = grille.Jours.Single(j => j.Date == Mardi_23_06_2026);
        Assert.Equal("", caseMardi.NomResponsable);                          // ni surcharge valide, ni fond → aucun nom
        Assert.Equal(FakePaletteCouleurs.Neutre, caseMardi.CouleurResponsable); // ... repli neutre, jamais la teinte d'un orphelin
    }
}
