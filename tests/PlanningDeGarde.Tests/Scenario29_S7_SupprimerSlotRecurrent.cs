using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 29 — S7 — Suppression idempotente d'un slot récurrent par identifiant stable (@back)
//   Étant donné un slot récurrent enregistré d'identifiant stable connu
//   Quand un Parent supprime ce slot récurrent par son identifiant stable
//   Alors le slot récurrent est retiré du store durable
//   Et la diffusion temps réel de mise à jour est déclenchée
//   Et ses occurrences disparaissent de toutes les cases à la re-projection
//   Quand la même suppression est rejouée (déjà absent) → réussit en no-op (idempotence), sans erreur
//
// Boucle externe à la frontière Application (handler + ports, doublures à la main).
// La re-projection est observée sur GrilleAgendaQuery réel (store partagé).
public class Scenario29_S7_SupprimerSlotRecurrent
{
    private static readonly DateOnly Reference_24_06_2026 = new(2026, 6, 24);

    private static GrilleAgendaQuery Grille(ISlotRecurrentRepository recurrents)
        => new(
            new FakeSlotRepository(),
            new FakePeriodeRepository(),
            new FakePaletteCouleurs(new Dictionary<string, string>()),
            new FakeReferentielResponsables(new Dictionary<string, string>()),
            slotsRecurrents: recurrents);

    // ---------- Test d'acceptation (boucle externe, BDD) ----------

    [Fact]
    public void Should_retirer_le_recurrent_diffuser_et_faire_disparaitre_ses_occurrences_puis_etre_idempotente_When_un_Parent_supprime_par_id_stable()
    {
        // Given — un slot récurrent (samedi 11h30–12h15 "Piscine") enregistré, d'identifiant stable connu.
        var slots = new FakeSlotRecurrentRepository();
        slots.Enregistrer(SlotRecurrent
            .Poser("lea", "piscine", DayOfWeek.Saturday, new TimeSpan(11, 30, 0), new TimeSpan(12, 15, 0)).Valeur!);
        var idStable = Assert.Single(slots.AllSnapshots()).Id;
        Assert.False(string.IsNullOrEmpty(idStable));
        // Précondition : ses occurrences sont bien projetées avant suppression.
        Assert.Contains(Grille(slots).Projeter(Reference_24_06_2026).Jours.SelectMany(j => j.Slots), s => s.Libelle == "piscine");

        var notificateur = new FakeNotificateurPlanning();
        var handler = new SupprimerSlotRecurrentHandler(slots, notificateur);

        // When — un Parent supprime le slot récurrent par son identifiant stable.
        var resultat = handler.Handle(new SupprimerSlotRecurrentCommand(idStable));

        // Then — la suppression réussit et le slot est retiré du store.
        Assert.True(resultat.EstSucces);
        Assert.Empty(slots.AllSnapshots());

        // And — la diffusion temps réel de mise à jour est déclenchée.
        Assert.Equal(1, notificateur.NombreDeNotifications);

        // And — ses occurrences disparaissent de toutes les cases à la re-projection.
        Assert.DoesNotContain(Grille(slots).Projeter(Reference_24_06_2026).Jours.SelectMany(j => j.Slots), s => s.Libelle == "piscine");

        // When — la même suppression est rejouée (déjà absent) → réussit en no-op sans erreur.
        var rejeu = handler.Handle(new SupprimerSlotRecurrentCommand(idStable));
        Assert.True(rejeu.EstSucces);
        Assert.Empty(slots.AllSnapshots());
    }

    // ---------- Tests unitaires (boucle interne, TDD) ----------

    // Test #1 — retrait : la commande retire le slot d'identifiant stable donné.
    [Fact]
    public void Should_retirer_le_slot_recurrent_du_store_When_on_supprime_par_son_identifiant_stable()
    {
        var slots = new FakeSlotRecurrentRepository();
        slots.Enregistrer(SlotRecurrent.Poser("lea", "piscine", DayOfWeek.Saturday, new TimeSpan(11, 30, 0), new TimeSpan(12, 15, 0)).Valeur!);
        var id = Assert.Single(slots.AllSnapshots()).Id;

        new SupprimerSlotRecurrentHandler(slots, new FakeNotificateurPlanning()).Handle(new SupprimerSlotRecurrentCommand(id));

        Assert.Empty(slots.AllSnapshots());
    }

    // Test #2 — diffusion : la suppression déclenche la mise à jour temps réel.
    [Fact]
    public void Should_declencher_la_diffusion_temps_reel_When_on_supprime_un_slot_recurrent()
    {
        var slots = new FakeSlotRecurrentRepository();
        slots.Enregistrer(SlotRecurrent.Poser("lea", "piscine", DayOfWeek.Saturday, new TimeSpan(11, 30, 0), new TimeSpan(12, 15, 0)).Valeur!);
        var id = Assert.Single(slots.AllSnapshots()).Id;
        var notificateur = new FakeNotificateurPlanning();

        new SupprimerSlotRecurrentHandler(slots, notificateur).Handle(new SupprimerSlotRecurrentCommand(id));

        Assert.Equal(1, notificateur.NombreDeNotifications);
    }

    // Test #3 — idempotence : supprimer un identifiant absent réussit en no-op, sans erreur.
    [Fact]
    public void Should_reussir_en_no_op_When_on_supprime_un_identifiant_deja_absent()
    {
        var slots = new FakeSlotRecurrentRepository();

        var resultat = new SupprimerSlotRecurrentHandler(slots, new FakeNotificateurPlanning())
            .Handle(new SupprimerSlotRecurrentCommand("id-inexistant"));

        Assert.True(resultat.EstSucces);
        Assert.Empty(slots.AllSnapshots());
    }
}
