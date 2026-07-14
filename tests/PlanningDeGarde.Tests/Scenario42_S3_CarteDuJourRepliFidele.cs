using System;
using System.Collections.Generic;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 42 — Sc.3 — Cas limites / repli fidèle : neutre, orphelin, bord de fenêtre (@back)
//   Aucun responsable résolu → état NEUTRE « personne assignée » (aucun nom / couleur fantôme)
//   Responsable pointant un acteur ORPHELIN (supprimé) → repli neutre SANS nom fantôme (Resolvable s13)
//   Date en BORD de fenêtre (jour non chargé) / store VIDE → neutre sans crash, à l'identique sur les deux adaptateurs
//
// Caractérisation du repli (⚠️ early green attendu : la carte COMPOSE la retombée neutre R5/R6 de la
// grille — c'est précisément l'objet du sprint, ne PAS réimplémenter le repli).
public class Scenario42_S3_CarteDuJourRepliFidele
{
    private const string Papa = "papa";
    private const string Orphelin = "acteur-supprime";
    private const string LeaId = "enfant-lea";

    private static readonly DateOnly JourJ_08_07_2026 = new(2026, 7, 8);
    private static readonly DateOnly JourLointain_15_02_2030 = new(2030, 2, 15);

    private static CarteDuJourQuery Query(IPeriodeRepository periodes, params string[] acteursExistants)
        => new(new GrilleAgendaQuery(
            new FakeSlotRepository(), periodes,
            new FakePaletteCouleurs(new Dictionary<string, string> { [Papa] = "bleu" }),
            new FakeReferentielResponsables(new Dictionary<string, string> { [Papa] = "Papa", [Orphelin] = "Fantôme" }),
            new FakeReferentielCycleDeFond(),
            new FakeEnumerationActeursFoyer(acteursExistants)));

    // ---------- Acceptation (frontière Application) ----------
    [Fact]
    public void Acceptation_Should_Retomber_neutre_sans_fantome_pour_neutre_orphelin_et_bord_de_fenetre()
    {
        // Aucun responsable résolu (aucune période, aucun cycle) → neutre « personne assignée »
        var neutre = Query(new FakePeriodeRepository(), Papa).Lire(JourJ_08_07_2026, LeaId).Responsable;
        Assert.False(neutre.EstAssigne);
        Assert.Null(neutre.ActeurId);
        Assert.Equal("", neutre.Nom);
        Assert.Equal(FakePaletteCouleurs.Neutre, neutre.Couleur);

        // Surcharge pointant un acteur ORPHELIN (absent du store) → repli neutre SANS nom fantôme
        var periodesOrphelin = new FakePeriodeRepository();
        periodesOrphelin.Enregistrer(PeriodeDeGarde.Affecter(Orphelin,
            new DateTime(2026, 7, 8), new DateTime(2026, 7, 8)).Valeur!);
        var orphelin = Query(periodesOrphelin, Papa).Lire(JourJ_08_07_2026, LeaId).Responsable;
        Assert.False(orphelin.EstAssigne);
        Assert.Null(orphelin.ActeurId);
        Assert.NotEqual("Fantôme", orphelin.Nom); // aucun nom fantôme de l'acteur supprimé
        Assert.Equal("", orphelin.Nom);

        // Bord de fenêtre / jour lointain non chargé → neutre sans crash (aucune racine fantôme)
        var lointain = Query(new FakePeriodeRepository(), Papa).Lire(JourLointain_15_02_2030, LeaId);
        Assert.False(lointain.Responsable.EstAssigne);
        Assert.Empty(lointain.Slots);
        Assert.Null(lointain.Transfert);
    }

    // ---------- Acceptation runtime — adaptateur InMemory RÉEL : store VIDE → carte neutre sans crash ----------
    [Fact]
    public void Acceptation_InMemory_Should_Restituer_une_carte_neutre_sur_un_store_vide()
    {
        var config = new ConfigurationFoyerEnMemoire();
        var grille = new GrilleAgendaQuery(
            new InMemorySlotRepository(), new InMemoryPeriodeRepository(), config, config,
            new CycleDeFondEnMemoire(), config, new InMemorySlotRecurrentRepository(), new InMemoryTransfertRepository());

        var carte = new CarteDuJourQuery(grille).Lire(JourJ_08_07_2026, LeaId);

        Assert.False(carte.Responsable.EstAssigne);
        Assert.Null(carte.Responsable.ActeurId);
        Assert.Empty(carte.Slots);
        Assert.Null(carte.Transfert);
    }
}
