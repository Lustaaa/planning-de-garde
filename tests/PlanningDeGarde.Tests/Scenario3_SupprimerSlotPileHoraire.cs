using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Tests.Builders;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 18 — Scénario 3 — Pile horaire : supprimer un slot laisse les autres dans l'ordre horaire (@back)
//   Étant donné un foyer dont le store comporte les lieux "École", "Piscine", "Chez Mamie" et l'enfant "Léa"
//   Et trois slots du mardi 16/06/2026 : "École" 08h30-12h00, "Piscine" 14h00-15h30, "Chez Mamie" 17h00-19h00
//   Quand je supprime le slot "Piscine" 14h00-15h30 par son identifiant stable
//   Alors la suppression réussit
//   Et la case du mardi 16/06/2026 ne comporte plus le slot "Piscine"
//   Et la case du mardi 16/06/2026 comporte encore "École" 08h30-12h00 puis "Chez Mamie" 17h00-19h00, dans l'ordre horaire
//
// Boucle externe à la frontière Application : observable = la projection réelle GrilleAgendaQuery.
// Garde de composition : compose le retrait par identifiant stable (Sc.1) avec l'empilement horaire
// déjà acquis — la suppression ciblée NE retire QUE le slot visé et préserve l'ordre des autres.
public class Scenario3_SupprimerSlotPileHoraire
{
    private const string Ecole = "ecole";
    private const string Piscine = "piscine";
    private const string ChezMamie = "chez-mamie";
    private static readonly DateOnly Mardi16 = new(2026, 6, 16);

    [Fact]
    public void Should_retirer_seulement_Piscine_et_laisser_Ecole_puis_ChezMamie_dans_l_ordre_horaire_When_on_supprime_Piscine_par_son_identifiant_stable()
    {
        // Given — trois slots de Léa le mardi 16/06 : École 08h30-12h00, Piscine 14h00-15h30, Chez Mamie 17h00-19h00.
        var slots = new FakeSlotRepository();
        var lieux = new FakeLieuRepository().AvecLieu(Ecole).AvecLieu(Piscine).AvecLieu(ChezMamie);
        var handler = new PoserSlotHandler(slots, lieux, new FakeNotificateurPlanning());
        handler.Handle(new SlotBuilder().PourEnfant("lea").DansLieu(Ecole)
            .De(new DateTime(2026, 6, 16, 8, 30, 0)).A(new DateTime(2026, 6, 16, 12, 0, 0)).Build());
        handler.Handle(new SlotBuilder().PourEnfant("lea").DansLieu(Piscine)
            .De(new DateTime(2026, 6, 16, 14, 0, 0)).A(new DateTime(2026, 6, 16, 15, 30, 0)).Build());
        handler.Handle(new SlotBuilder().PourEnfant("lea").DansLieu(ChezMamie)
            .De(new DateTime(2026, 6, 16, 17, 0, 0)).A(new DateTime(2026, 6, 16, 19, 0, 0)).Build());

        var query = new GrilleAgendaQuery(
            slots,
            new FakePeriodeRepository(),
            new FakePaletteCouleurs(new Dictionary<string, string>()),
            new FakeReferentielResponsables(new Dictionary<string, string>()));

        var idPiscine = slots.AllSnapshots().Single(s => s.LieuId == Piscine).Id;

        // When — je supprime le slot Piscine par son identifiant stable.
        var resultat = new SupprimerSlotHandler(slots).Handle(new SupprimerSlotCommand(idPiscine));

        // Then — succès ; la case du mardi 16/06 ne comporte plus Piscine et garde École puis Chez Mamie ordonnés.
        Assert.True(resultat.EstSucces);
        var caseMardi = query.Projeter(Mardi16).Jours.Single(j => j.Date == Mardi16);

        Assert.DoesNotContain(caseMardi.Slots, s => s.Libelle == Piscine);
        Assert.Equal(new[] { Ecole, ChezMamie }, caseMardi.Slots.Select(s => s.Libelle).ToArray());
        Assert.Equal(
            new[] { new TimeOnly(8, 30), new TimeOnly(17, 0) },
            caseMardi.Slots.Select(s => s.Debut).ToArray());
        Assert.Equal(
            new[] { new TimeOnly(12, 0), new TimeOnly(19, 0) },
            caseMardi.Slots.Select(s => s.Fin).ToArray());
    }
}
