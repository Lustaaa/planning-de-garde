using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

/// <summary>
/// Sprint 54 — S5 (@back) — Éditer une activité récurrente : TOUTE LA SÉRIE (jours + plage + lieu),
/// par son identifiant stable. Acceptation à la frontière Application (handler d'édition + projection de
/// lecture, doublures à la main) : le slot est réécrit EN PLACE (même id), l'<b>EnfantId est PRÉSERVÉ</b>
/// (jamais réaffecté), la projection reflète la nouvelle série, aucun autre enfant n'est touché. Erreur :
/// durée non positive OU lieu inconnu → refus AVANT écriture, série intacte.
/// </summary>
public sealed class Scenario54_S5_ModifierSlotRecurrent
{
    private static readonly TimeSpan H09h00 = new(9, 0, 0);
    private static readonly TimeSpan H12h00 = new(12, 0, 0);
    private static readonly DateOnly Reference_24_06_2026 = new(2026, 6, 24);

    private static FakeReferentielActivites LieuxDuFoyer()
        => new FakeReferentielActivites().AvecActivite("ecole").AvecActivite("nounou");

    private static (FakeSlotRecurrentRepository slots, string leaId, string tomId) StoreAvecLeaEtTom()
    {
        var slots = new FakeSlotRecurrentRepository();
        slots.Enregistrer(SlotRecurrent.Poser("lea", "ecole", new[] { DayOfWeek.Wednesday }, new TimeSpan(8, 30, 0), new TimeSpan(16, 30, 0)).Valeur!);
        slots.Enregistrer(SlotRecurrent.Poser("tom", "ecole", new[] { DayOfWeek.Thursday }, new TimeSpan(8, 30, 0), new TimeSpan(16, 30, 0)).Valeur!);
        return (slots, slots.AllSnapshots().Single(s => s.EnfantId == "lea").Id, slots.AllSnapshots().Single(s => s.EnfantId == "tom").Id);
    }

    private static GrilleAgendaQuery Grille(ISlotRecurrentRepository recurrents)
        => new(
            new FakeSlotRepository(),
            new FakePeriodeRepository(),
            new FakePaletteCouleurs(new Dictionary<string, string>()),
            new FakeReferentielResponsables(new Dictionary<string, string>()),
            slotsRecurrents: recurrents);

    [Fact]
    public void Should_reecrire_la_serie_en_place_preserver_l_enfant_et_projeter_la_nouvelle_serie_When_on_edite_jours_plage_et_lieu()
    {
        // Given — Léa « École » le mercredi 08:30→16:30, et un slot de Tom (contrôle d'isolation).
        var (slots, leaId, tomId) = StoreAvecLeaEtTom();
        var tomAvant = slots.AllSnapshots().Single(s => s.Id == tomId);
        var notificateur = new FakeNotificateurPlanning();
        var handler = new ModifierSlotRecurrentHandler(slots, LieuxDuFoyer(), notificateur);

        // When — on édite toute la série : jours {lundi, mardi}, plage 09:00→12:00, lieu « Nounou ».
        var resultat = handler.Handle(new ModifierSlotRecurrentCommand(
            leaId, "nounou", new[] { DayOfWeek.Monday, DayOfWeek.Tuesday }, H09h00, H12h00));

        // Then — succès, réécrit EN PLACE (même id), aucune ligne créée, EnfantId (Léa) PRÉSERVÉ.
        Assert.True(resultat.EstSucces);
        Assert.Equal(leaId, resultat.Valeur!.Id);
        Assert.Equal(2, slots.AllSnapshots().Count);
        var edite = slots.AllSnapshots().Single(s => s.Id == leaId);
        Assert.Equal("lea", edite.EnfantId);
        Assert.Equal(new[] { DayOfWeek.Monday, DayOfWeek.Tuesday }, edite.JoursDeSemaine);
        Assert.Equal("nounou", edite.LieuId);
        Assert.Equal(H09h00, edite.HeureDebut);
        Assert.Equal(H12h00, edite.HeureFin);

        // And — Tom intact (isolation), diffusion temps réel déclenchée.
        Assert.Equal(tomAvant, slots.AllSnapshots().Single(s => s.Id == tomId));
        Assert.Equal(1, notificateur.NombreDeNotifications);

        // And — la projection reflète la nouvelle série : Nounou 09:00→12:00 lundi/mardi, jamais le mercredi (ancien jour).
        var grille = Grille(slots).Projeter(Reference_24_06_2026);
        foreach (var jour in grille.Jours.Where(j => j.Date.DayOfWeek is DayOfWeek.Monday or DayOfWeek.Tuesday))
        {
            var occ = Assert.Single(jour.Slots, s => s.Libelle == "nounou");
            Assert.Equal(TimeOnly.FromTimeSpan(H09h00), occ.Debut);
            Assert.Equal(TimeOnly.FromTimeSpan(H12h00), occ.Fin);
        }
        foreach (var mercredi in grille.Jours.Where(j => j.Date.DayOfWeek == DayOfWeek.Wednesday))
            Assert.DoesNotContain(mercredi.Slots, s => s.Libelle is "nounou" or "ecole");
    }

    [Fact]
    public void Should_refuser_avant_ecriture_et_laisser_la_serie_intacte_When_le_lieu_vise_est_inconnu()
    {
        var (slots, leaId, _) = StoreAvecLeaEtTom();
        var avant = slots.AllSnapshots().Single(s => s.Id == leaId);

        var resultat = new ModifierSlotRecurrentHandler(slots, LieuxDuFoyer(), new FakeNotificateurPlanning())
            .Handle(new ModifierSlotRecurrentCommand(leaId, "piscine", new[] { DayOfWeek.Monday }, H09h00, H12h00));

        Assert.False(resultat.EstSucces);
        Assert.Equal(avant, slots.AllSnapshots().Single(s => s.Id == leaId)); // série intacte
    }

    [Fact]
    public void Should_refuser_avant_ecriture_et_laisser_la_serie_intacte_When_la_duree_est_non_positive()
    {
        var (slots, leaId, _) = StoreAvecLeaEtTom();
        var avant = slots.AllSnapshots().Single(s => s.Id == leaId);

        var resultat = new ModifierSlotRecurrentHandler(slots, LieuxDuFoyer(), new FakeNotificateurPlanning())
            .Handle(new ModifierSlotRecurrentCommand(leaId, "nounou", new[] { DayOfWeek.Monday }, H12h00, H09h00));

        Assert.False(resultat.EstSucces);
        Assert.Equal(avant, slots.AllSnapshots().Single(s => s.Id == leaId)); // série intacte
    }
}
