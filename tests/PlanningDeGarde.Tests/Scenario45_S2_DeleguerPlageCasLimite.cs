using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 45 — Sc.2 — Cas LIMITE plage : chevauchement / plage vide / soi-même / frontière de fenêtre (@back)
//   - Plage [J1..J2] chevauchant une surcharge existante (C) → déléguer à B RÉAFFECTE la plage (aucun doublon)
//   - Plage INVALIDE fin < début (plage vide) → REFUS AVANT écriture, aucun jour écrit, store intact
//   - Plage [J1..J2] dont TOUS les jours résolvent A, déléguer à A (soi-même) → REFUS explicite, store intact
//   - Plage dont la fin est HORS de la fenêtre chargée → l'écriture RÉUSSIT sans crash
//
// Frontière Application (DeleguerRecuperationHandler). Refus = ATOMIQUE sur toute la plage.
public class Scenario45_S2_DeleguerPlageCasLimite
{
    private const string ParentA = "parent-a";
    private const string ParentB = "parent-b";
    private const string ParentC = "parent-c";
    private const string LeaId = "enfant-lea";

    // ISO 28 paire → fond Parent A pour toute la semaine du lundi 06/07/2026.
    private static readonly DateOnly Mardi_07 = new(2026, 7, 7);      // J1
    private static readonly DateOnly Mercredi_08 = new(2026, 7, 8);
    private static readonly DateOnly Jeudi_09 = new(2026, 7, 9);      // J2
    private static readonly DateOnly LointainDebut = new(2027, 3, 17);   // très loin de toute fenêtre par défaut
    private static readonly DateOnly LointainFin = new(2027, 3, 20);

    private static Dictionary<int, string> MappingPairAImpairB() => new() { [0] = ParentA, [1] = ParentB };

    private static GrilleAgendaQuery Grille(IPeriodeRepository periodes)
        => new(
            new FakeSlotRepository(), periodes,
            new FakePaletteCouleurs(new Dictionary<string, string> { [ParentA] = "bleu", [ParentB] = "orange", [ParentC] = "vert" }),
            new FakeReferentielResponsables(new Dictionary<string, string> { [ParentA] = "Alice", [ParentB] = "Bruno", [ParentC] = "Chloe" }),
            new FakeReferentielCycleDeFond(new CycleDeFond(2, MappingPairAImpairB())),
            new FakeEnumerationActeursFoyer(ParentA, ParentB, ParentC));

    private static DeleguerRecuperationHandler Handler(IPeriodeRepository periodes)
        => new(Grille(periodes), periodes, new FakeEnumerationActeursFoyer(ParentA, ParentB, ParentC));

    private static JourCase CaseDuJour(GrilleAgendaQuery grille, DateOnly jour)
        => grille.Projeter(jour, VuePlanning.Semaine).Jours.Single(j => j.Date == jour);

    [Fact]
    public void Should_Reaffecter_toute_la_plage_sans_doublon_When_elle_chevauche_une_surcharge_existante()
    {
        var periodes = new FakePeriodeRepository();
        // Une surcharge existante (Parent C) sur un jour AU MILIEU de la plage.
        var debutC = Mercredi_08.ToDateTime(TimeOnly.MinValue);
        periodes.Enregistrer(PeriodeDeGarde.Affecter(ParentC, debutC, debutC, LeaId).Valeur!);

        var resultat = Handler(periodes)
            .Handle(new DeleguerRecuperationCommand(Mardi_07, LeaId, ParentB, Jeudi_09));

        Assert.True(resultat.EstSucces);
        // Last-write-wins R11 : UNE SEULE période [J1..J2] responsable B, SANS doublon.
        var periode = Assert.Single(periodes.AllSnapshots());
        Assert.Equal(ParentB, periode.ResponsableId);
        Assert.Equal(Mardi_07, DateOnly.FromDateTime(periode.Debut));
        Assert.Equal(Jeudi_09, DateOnly.FromDateTime(periode.Fin));
        // Chaque jour de la plage résout désormais B.
        var grille = Grille(periodes);
        foreach (var j in new[] { Mardi_07, Mercredi_08, Jeudi_09 })
            Assert.Equal(ParentB, CaseDuJour(grille, j).ResponsableId);
    }

    [Fact]
    public void Should_Refuser_avant_ecriture_When_fin_avant_debut_plage_vide()
    {
        var periodes = new FakePeriodeRepository();
        // Plage INVALIDE : fin (Mardi_07) < début (Jeudi_09).
        var resultat = Handler(periodes)
            .Handle(new DeleguerRecuperationCommand(Jeudi_09, LeaId, ParentB, Mardi_07));

        Assert.False(resultat.EstSucces);
        Assert.False(string.IsNullOrWhiteSpace(resultat.Motif));
        // Store intact : AUCUN jour écrit.
        Assert.Empty(periodes.AllSnapshots());
    }

    [Fact]
    public void Should_Refuser_sans_ecrire_When_delegation_de_toute_la_plage_a_soi_meme()
    {
        var periodes = new FakePeriodeRepository();
        // Toute la plage résout Parent A (fond). Déléguer à Parent A (soi-même) → refus, store intact.
        var resultat = Handler(periodes)
            .Handle(new DeleguerRecuperationCommand(Mardi_07, LeaId, ParentA, Jeudi_09));

        Assert.False(resultat.EstSucces);
        Assert.False(string.IsNullOrWhiteSpace(resultat.Motif));
        Assert.Empty(periodes.AllSnapshots());
    }

    [Fact]
    public void Should_Ecrire_sans_crash_When_la_fin_de_plage_est_hors_de_la_fenetre_chargee()
    {
        var periodes = new FakePeriodeRepository();
        // Délégataire Parent C (hors cycle de fond) : l'enjeu est qu'une plage lointaine s'écrive sans crash.
        var resultat = Handler(periodes)
            .Handle(new DeleguerRecuperationCommand(LointainDebut, LeaId, ParentC, LointainFin));

        Assert.True(resultat.EstSucces);
        var periode = Assert.Single(periodes.AllSnapshots());
        Assert.Equal(ParentC, periode.ResponsableId);
        Assert.Equal(LointainDebut, DateOnly.FromDateTime(periode.Debut));
        Assert.Equal(LointainFin, DateOnly.FromDateTime(periode.Fin));
    }
}
