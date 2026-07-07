using System;
using PlanningDeGarde.Application;
using PlanningDeGarde.Tests.Builders;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 18 — Scénario 4 — Lister les slots couvrant une date alimente la dialog (@back)
//   Étant donné le store comporte les lieux "École" et "Chez Mamie" et l'enfant "Léa"
//   Et un slot place "Léa" à "École" le mardi 16/06/2026 de 08h30 à 16h30
//   Et un slot place "Léa" à "Chez Mamie" du mardi 16/06/2026 22h00 au mercredi 17/06/2026 07h00
//   Et aucun slot ne couvre le jeudi 18/06/2026
//   Quand je liste les slots couvrant le mardi 16/06/2026
//   Alors la liste comporte les deux slots, chacun avec son identifiant stable, son enfant, son lieu et ses bornes
//   Et la liste des slots couvrant le jeudi 18/06/2026 est vide
//
// Lecture neuve (canal lecture) : SlotsDuJourQuery. Frontière Application (port d'écriture pour le
// Given via PoserSlotHandler, query en lecture). Ne déclenche jamais la diffusion.
public class Scenario4_SlotsDuJourQuery
{
    private const string Ecole = "ecole";
    private const string ChezMamie = "chez-mamie";

    private static readonly DateTime Mardi16_0830 = new(2026, 6, 16, 8, 30, 0);
    private static readonly DateTime Mardi16_1630 = new(2026, 6, 16, 16, 30, 0);
    private static readonly DateTime Mardi16_22h = new(2026, 6, 16, 22, 0, 0);
    private static readonly DateTime Mercredi17_07h = new(2026, 6, 17, 7, 0, 0);
    private static readonly DateOnly Mardi16Juin2026 = new(2026, 6, 16);
    private static readonly DateOnly Jeudi18Juin2026 = new(2026, 6, 18);

    [Fact]
    public void Should_lister_les_slots_couvrant_la_date_avec_id_enfant_lieu_et_bornes_et_vide_hors_couverture()
    {
        // Given — deux slots de Léa couvrant le mardi 16/06/2026, aucun le jeudi 18/06/2026.
        var slots = new FakeSlotRepository();
        var lieux = new FakeReferentielLieux().AvecLieu(Ecole).AvecLieu(ChezMamie);
        var handler = new PoserSlotHandler(slots, lieux, new FakeNotificateurPlanning());
        handler.Handle(new SlotBuilder().PourEnfant("lea").DansLieu(Ecole).De(Mardi16_0830).A(Mardi16_1630).Build());
        handler.Handle(new SlotBuilder().PourEnfant("lea").DansLieu(ChezMamie).De(Mardi16_22h).A(Mercredi17_07h).Build());
        var query = new SlotsDuJourQuery(slots);

        // When — je liste les slots couvrant le mardi 16/06/2026.
        var couvrants = query.Lister(Mardi16Juin2026);

        // Then — les deux slots, chacun avec identifiant stable non vide, enfant, lieu et bornes.
        Assert.Equal(2, couvrants.Count);
        Assert.All(couvrants, s => Assert.False(string.IsNullOrEmpty(s.Id), "chaque slot listé porte son identifiant stable."));

        var ecole = Assert.Single(couvrants, s => s.LieuId == Ecole);
        Assert.Equal("lea", ecole.EnfantId);
        Assert.Equal(Mardi16_0830, ecole.Debut);
        Assert.Equal(Mardi16_1630, ecole.Fin);

        var chezMamie = Assert.Single(couvrants, s => s.LieuId == ChezMamie);
        Assert.Equal("lea", chezMamie.EnfantId);
        Assert.Equal(Mardi16_22h, chezMamie.Debut);
        Assert.Equal(Mercredi17_07h, chezMamie.Fin);

        // And — aucun slot ne couvre le jeudi 18/06/2026.
        Assert.Empty(query.Lister(Jeudi18Juin2026));
    }
}
