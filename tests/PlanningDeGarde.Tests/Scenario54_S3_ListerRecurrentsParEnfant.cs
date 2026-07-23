using System;
using System.Linq;
using PlanningDeGarde.Application.Slots.Queries;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

/// <summary>
/// Sprint 54 — S3 (@back) — Lister les activités récurrentes d'UN enfant. Acceptation à la frontière
/// Application : le read model <see cref="SlotsRecurrentsParEnfantQuery"/> filtre STRICTEMENT sur
/// l'enfant demandé (isolation s53), résout le libellé de lieu depuis le référentiel (s35) et rend
/// jours + plage + id stable. Doublures à la main sur les ports (repo récurrents, énumération lieux).
/// </summary>
public sealed class Scenario54_S3_ListerRecurrentsParEnfant
{
    private static void Semer(
        FakeSlotRecurrentRepository repo, string enfant, string lieu, DayOfWeek jour, TimeSpan debut, TimeSpan fin)
        => repo.Enregistrer(SlotRecurrent.Poser(enfant, lieu, jour, debut, fin).Valeur!);

    [Fact]
    public void Should_Ne_retourner_que_les_recurrents_de_Lea_avec_lieu_resolu_jours_plage_et_id_stable_When_on_liste_les_recurrents_de_Lea()
    {
        // Given — des récurrents pour Léa ET pour Tom, référentiel de lieux résolvant les libellés.
        var repo = new FakeSlotRecurrentRepository();
        var referentiel = new FakeReferentielActivites();
        referentiel.Ajouter("piscine", "Piscine");
        referentiel.Ajouter("danse", "Danse");
        Semer(repo, "Léa", "piscine", DayOfWeek.Saturday, new TimeSpan(11, 30, 0), new TimeSpan(12, 15, 0));
        Semer(repo, "Tom", "danse", DayOfWeek.Wednesday, new TimeSpan(17, 0, 0), new TimeSpan(18, 0, 0));

        // When — on liste les récurrents de Léa.
        var vues = new SlotsRecurrentsParEnfantQuery(repo, referentiel).PourEnfant("Léa");

        // Then — uniquement celui de Léa, lieu résolu (« Piscine »), jour + plage + id stable ; jamais Tom.
        var vue = Assert.Single(vues);
        Assert.Equal("Piscine", vue.ActiviteLibelle);
        Assert.Contains(DayOfWeek.Saturday, vue.Jours);
        Assert.Equal(new TimeSpan(11, 30, 0), vue.HeureDebut);
        Assert.Equal(new TimeSpan(12, 15, 0), vue.HeureFin);
        Assert.False(string.IsNullOrEmpty(vue.Id), "la vue doit porter l'id stable de la série.");
        Assert.DoesNotContain(vues, v => v.ActiviteLibelle == "Danse");
    }

    [Fact]
    public void Should_Exposer_le_set_complet_de_jours_When_le_recurrent_est_multi_jours()
    {
        // Given — un récurrent MULTI-JOURS de Léa (école lun/mar/jeu/ven).
        var repo = new FakeSlotRecurrentRepository();
        var referentiel = new FakeReferentielActivites();
        referentiel.Ajouter("ecole", "École");
        repo.Enregistrer(SlotRecurrent.Poser(
            "Léa", "ecole",
            new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Thursday, DayOfWeek.Friday },
            new TimeSpan(8, 30, 0), new TimeSpan(16, 30, 0)).Valeur!);

        // When — on liste les récurrents de Léa.
        var vue = Assert.Single(new SlotsRecurrentsParEnfantQuery(repo, referentiel).PourEnfant("Léa"));

        // Then — la vue expose le SET COMPLET de jours (pas seulement le premier), pour la config par enfant.
        Assert.Equal(
            new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Thursday, DayOfWeek.Friday },
            vue.Jours);
    }

    [Fact]
    public void Should_Exposer_les_plages_d_exclusion_de_la_serie_When_le_recurrent_a_des_vacances()
    {
        // Given — un récurrent de Léa avec une plage d'exclusion (vacances) rattachée.
        var repo = new FakeSlotRecurrentRepository();
        var referentiel = new FakeReferentielActivites();
        referentiel.Ajouter("ecole", "École");
        repo.Enregistrer(SlotRecurrent.Poser("Léa", "ecole", new[] { DayOfWeek.Monday }, new TimeSpan(8, 30, 0), new TimeSpan(16, 30, 0)).Valeur!
            .AjouterExclusion(new DateOnly(2026, 6, 29), new DateOnly(2026, 7, 5)));

        // When — on liste les récurrents de Léa.
        var vue = Assert.Single(new SlotsRecurrentsParEnfantQuery(repo, referentiel).PourEnfant("Léa"));

        // Then — la vue expose la plage d'exclusion (pour l'afficher / la retirer dans la config).
        Assert.Contains(new PlageExclusion(new DateOnly(2026, 6, 29), new DateOnly(2026, 7, 5)), vue.Exclusions);
    }

    [Fact]
    public void Should_Retourner_une_liste_vide_sans_erreur_When_l_enfant_n_a_aucun_recurrent()
    {
        // Given — Léa a un récurrent, Tom n'en a aucun.
        var repo = new FakeSlotRecurrentRepository();
        var referentiel = new FakeReferentielActivites();
        referentiel.Ajouter("piscine", "Piscine");
        Semer(repo, "Léa", "piscine", DayOfWeek.Saturday, new TimeSpan(11, 30, 0), new TimeSpan(12, 15, 0));

        // When/Then — lister les récurrents de Tom donne une liste vide (aucune erreur).
        Assert.Empty(new SlotsRecurrentsParEnfantQuery(repo, referentiel).PourEnfant("Tom"));
    }
}
