using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Tests.Builders;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 18 — Scénario 2 — Slot franchissant minuit : la suppression efface son rendu sur les deux jours (@back)
//   Étant donné un foyer dont le store comporte le lieu "Chez Mamie" et l'enfant "Léa"
//   Et un slot durable place "Léa" à "Chez Mamie" du mardi 16/06/2026 22h00 au mercredi 17/06/2026 07h00
//   Et ce slot est rendu à la fois sur la case du mardi 16/06/2026 et sur celle du mercredi 17/06/2026
//   Quand je supprime ce slot par son identifiant stable
//   Alors la suppression réussit
//   Et le slot n'apparaît plus dans la case du mardi 16/06/2026
//   Et le slot n'apparaît plus dans la case du mercredi 17/06/2026
//
// Boucle externe à la frontière Application : observable = la projection réelle GrilleAgendaQuery
// (les « cases » du mardi et du mercredi), doublures à la main pour les ports.
public class Scenario2_SupprimerSlotFranchissantMinuit
{
    private const string ChezMamie = "chez-mamie";
    private static readonly DateOnly Mardi16 = new(2026, 6, 16);
    private static readonly DateOnly Mercredi17 = new(2026, 6, 17);
    private static readonly DateTime Mardi16_22h = new(2026, 6, 16, 22, 0, 0);
    private static readonly DateTime Mercredi17_07h = new(2026, 6, 17, 7, 0, 0);

    private static JourCase Case(GrilleAgenda grille, DateOnly date) => grille.Jours.Single(j => j.Date == date);

    [Fact]
    public void Should_effacer_le_rendu_du_slot_sur_les_deux_jours_qu_il_couvrait_When_on_supprime_un_slot_franchissant_minuit()
    {
        // Given — un slot de nuit "Chez Mamie" pour Léa, du mardi 16/06 22h au mercredi 17/06 07h.
        var slots = new FakeSlotRepository();
        var lieux = new FakeReferentielActivites().AvecActivite(ChezMamie);
        new PoserSlotHandler(slots, lieux, new FakeReferentielEnfants().AvecEnfant("lea"), new FakeNotificateurPlanning()).Handle(
            new SlotBuilder().PourEnfant("lea").DansLieu(ChezMamie).De(Mardi16_22h).A(Mercredi17_07h).Build());
        var idStable = Assert.Single(slots.AllSnapshots()).Id;

        var query = new GrilleAgendaQuery(
            slots,
            new FakePeriodeRepository(),
            new FakePaletteCouleurs(new Dictionary<string, string>()),
            new FakeReferentielResponsables(new Dictionary<string, string>()));

        // And — ce slot est rendu à la fois sur la case du mardi 16/06 ET celle du mercredi 17/06.
        var avant = query.Projeter(Mardi16);
        Assert.Contains(Case(avant, Mardi16).Slots, s => s.Libelle == ChezMamie);
        Assert.Contains(Case(avant, Mercredi17).Slots, s => s.Libelle == ChezMamie);

        // When — je supprime ce slot par son identifiant stable.
        var resultat = new SupprimerSlotHandler(slots).Handle(new SupprimerSlotCommand(idStable));

        // Then — la suppression réussit et le slot n'apparaît plus sur aucun des deux jours.
        Assert.True(resultat.EstSucces);
        var apres = query.Projeter(Mardi16);
        Assert.DoesNotContain(Case(apres, Mardi16).Slots, s => s.Libelle == ChezMamie);
        Assert.DoesNotContain(Case(apres, Mercredi17).Slots, s => s.Libelle == ChezMamie);
    }
}
