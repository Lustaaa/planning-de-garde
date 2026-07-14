using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 43 — Sc.3 — Cas limites / repli fidèle : neutre, orphelin, fenêtre sans à-venir (@back)
//   Un jour à venir sans responsable résolu → entrée NEUTRE « personne assignée » (aucun nom/couleur fantôme)
//   Un jour dont le responsable pointe un acteur ORPHELIN (supprimé) → repli neutre SANS nom fantôme (Resolvable s13)
//   Une fenêtre sans jour strictement après aujourd'hui (aujourd'hui en fin de fenêtre) / store VIDE →
//     LISTE VIDE (« aucun événement à venir »), sans crash ni racine fantôme, à l'identique sur les deux adaptateurs
//
// Caractérisation du repli (⚠️ green attendu : la liste COMPOSE la retombée neutre R5/R6 de la grille —
// c'est précisément l'objet du sprint, ne PAS réimplémenter le repli).
public class Scenario43_S3_AVenirRepliFidele
{
    private const string Papa = "papa";
    private const string Orphelin = "acteur-supprime";
    private const string LeaId = "enfant-lea";

    private static readonly DateOnly Mardi_07_07_2026 = new(2026, 7, 7);      // « aujourd'hui » (ancre) mi-semaine
    private static readonly DateOnly Mercredi_08_07_2026 = new(2026, 7, 8);   // jour à venir ciblé
    private static readonly DateOnly Dimanche_12_07_2026 = new(2026, 7, 12);  // « aujourd'hui » en FIN de fenêtre (semaine)

    private static AVenirQuery Query(IPeriodeRepository periodes, params string[] acteursExistants)
        => new(new GrilleAgendaQuery(
            new FakeSlotRepository(), periodes,
            new FakePaletteCouleurs(new Dictionary<string, string> { [Papa] = "bleu" }),
            new FakeReferentielResponsables(new Dictionary<string, string> { [Papa] = "Papa", [Orphelin] = "Fantôme" }),
            new FakeReferentielCycleDeFond(),
            new FakeEnumerationActeursFoyer(acteursExistants)));

    // ---------- Acceptation : neutre + orphelin + fin de fenêtre ----------
    [Fact]
    public void Acceptation_Should_Retomber_neutre_sans_fantome_et_liste_vide_en_fin_de_fenetre()
    {
        // Jour à venir SANS responsable résolu (aucune période, aucun cycle) → neutre « personne assignée »
        var neutre = Query(new FakePeriodeRepository(), Papa)
            .Lire(Mardi_07_07_2026, LeaId).Single(j => j.Date == Mercredi_08_07_2026).Responsable;
        Assert.False(neutre.EstAssigne);
        Assert.Null(neutre.ActeurId);
        Assert.Equal("", neutre.Nom);
        Assert.Equal(FakePaletteCouleurs.Neutre, neutre.Couleur);

        // Surcharge pointant un acteur ORPHELIN (absent du store) → repli neutre SANS nom fantôme
        var periodesOrphelin = new FakePeriodeRepository();
        periodesOrphelin.Enregistrer(PeriodeDeGarde.Affecter(Orphelin,
            new DateTime(2026, 7, 8), new DateTime(2026, 7, 8)).Valeur!);
        var orphelin = Query(periodesOrphelin, Papa)
            .Lire(Mardi_07_07_2026, LeaId).Single(j => j.Date == Mercredi_08_07_2026).Responsable;
        Assert.False(orphelin.EstAssigne);
        Assert.Null(orphelin.ActeurId);
        Assert.NotEqual("Fantôme", orphelin.Nom); // aucun nom fantôme de l'acteur supprimé
        Assert.Equal("", orphelin.Nom);

        // « Aujourd'hui » en FIN de fenêtre (dimanche) → aucun jour strictement après → LISTE VIDE
        var finFenetre = Query(new FakePeriodeRepository(), Papa).Lire(Dimanche_12_07_2026, LeaId);
        Assert.Empty(finFenetre);
    }

    // ---------- Acceptation runtime — adaptateur InMemory RÉEL : store VIDE + fin de fenêtre → liste vide ----------
    [Fact]
    public void Acceptation_InMemory_Should_Restituer_une_liste_vide_sur_store_vide_en_fin_de_fenetre()
    {
        var config = new ConfigurationFoyerEnMemoire();
        var grille = new GrilleAgendaQuery(
            new InMemorySlotRepository(), new InMemoryPeriodeRepository(), config, config,
            new CycleDeFondEnMemoire(), config, new InMemorySlotRecurrentRepository(), new InMemoryTransfertRepository());

        var aVenir = new AVenirQuery(grille).Lire(Dimanche_12_07_2026, LeaId, VuePlanning.Semaine);

        Assert.Empty(aVenir); // « aucun événement à venir », sans crash ni racine fantôme
    }

    // ---------- Acceptation runtime — store VIDE mi-semaine → jours à venir NEUTRES (pas de crash, pas de fantôme) ----------
    [Fact]
    public void Acceptation_InMemory_Should_Restituer_des_jours_a_venir_neutres_sur_store_vide_mi_semaine()
    {
        var config = new ConfigurationFoyerEnMemoire();
        var grille = new GrilleAgendaQuery(
            new InMemorySlotRepository(), new InMemoryPeriodeRepository(), config, config,
            new CycleDeFondEnMemoire(), config, new InMemorySlotRecurrentRepository(), new InMemoryTransfertRepository());

        var aVenir = new AVenirQuery(grille).Lire(Mardi_07_07_2026, LeaId, VuePlanning.Semaine);

        Assert.NotEmpty(aVenir);
        Assert.All(aVenir, j =>
        {
            Assert.False(j.Responsable.EstAssigne);
            Assert.Null(j.Responsable.ActeurId);
            Assert.Equal("", j.Responsable.Nom);
            Assert.Empty(j.Slots);
            Assert.Null(j.Transfert);
        });
    }
}
