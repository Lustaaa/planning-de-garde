using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 43 — Sc.2 — Composer le « où » (slots) + le transfert de chaque jour à venir (@back)
//   Chaque entrée restitue AUSSI le(s) slot(s) du jour (le « où » de l'enfant sélectionné, s29)
//   Chaque entrée restitue le TRANSFERT cédant → recevant (noms + couleurs résolus), SAISI ou DÉRIVÉ (s31),
//     priorité SAISI > DÉRIVÉ (aucun doublon)
//   Un jour SANS transfert est unicolore (Transfert absent) ; un jour SANS slot est SANS lieu (pas d'erreur)
//   Le transfert et les slots sont LUS sans être modifiés (composition de la dérivation s31 / projection s29)
//
// Caractérisation du « où + transfert » composé (⚠️ green attendu : la liste COMPOSE les slots et l'info
// transfert que la grille résout déjà — c'est précisément l'objet du sprint, ne PAS réimplémenter s29/s31).
public class Scenario43_S2_AVenirOuEtTransfert
{
    private const string Papa = "papa";
    private const string Maman = "maman";
    private const string Bleu = "bleu";
    private const string Rose = "rose";
    private const string LeaId = "enfant-lea";
    private const string TomId = "enfant-tom";
    private const string Ecole = "ecole";

    private static readonly DateOnly Mardi_07_07_2026 = new(2026, 7, 7);      // « aujourd'hui » (ancre)
    private static readonly DateOnly Mercredi_08_07_2026 = new(2026, 7, 8);   // slot + transfert
    private static readonly DateOnly Jeudi_09_07_2026 = new(2026, 7, 9);      // sans slot, sans transfert

    private static AVenirQuery Query(ISlotRepository slots, IPeriodeRepository periodes, ITransfertRepository transferts)
        => new(new GrilleAgendaQuery(
            slots, periodes,
            new FakePaletteCouleurs(new Dictionary<string, string> { [Papa] = Bleu, [Maman] = Rose, [Ecole] = "vert" }),
            new FakeReferentielResponsables(new Dictionary<string, string> { [Papa] = "Papa", [Maman] = "Maman" }),
            new FakeReferentielCycleDeFond(),
            new FakeEnumerationActeursFoyer(Papa, Maman),
            transferts: transferts));

    // ---------- Acceptation (frontière Application) : le « où » de l'enfant + le transfert saisi ----------
    [Fact]
    public void Acceptation_Should_Restituer_le_ou_de_lenfant_et_le_transfert_saisi_de_chaque_jour()
    {
        var slots = new FakeSlotRepository();
        slots.Enregistrer(SlotDeLocalisation.Poser(LeaId, Ecole,
            new DateTime(2026, 7, 8, 8, 30, 0), new DateTime(2026, 7, 8, 16, 30, 0)).Valeur!);
        slots.Enregistrer(SlotDeLocalisation.Poser(TomId, Ecole,
            new DateTime(2026, 7, 8, 9, 0, 0), new DateTime(2026, 7, 8, 17, 0, 0)).Valeur!); // autre enfant

        var transferts = new FakeTransfertRepository();
        transferts.Enregistrer(Transfert.Definir(Papa, Maman, Ecole,
            TimeSpan.FromHours(8.5), Mercredi_08_07_2026.ToDateTime(TimeOnly.MinValue)).Valeur!);

        var aVenir = Query(slots, new FakePeriodeRepository(), transferts).Lire(Mardi_07_07_2026, LeaId);

        var jourJ = aVenir.Single(j => j.Date == Mercredi_08_07_2026);

        // Le « où » : seul le slot de Léa (l'enfant sélectionné), pas celui de Tom
        var slot = Assert.Single(jourJ.Slots);
        Assert.Equal(Ecole, slot.Libelle);

        // Le transfert SAISI cédant → recevant, noms + couleurs résolus
        Assert.NotNull(jourJ.Transfert);
        Assert.Equal("Papa", jourJ.Transfert!.CedantNom);
        Assert.Equal(Bleu, jourJ.Transfert.CedantCouleur);
        Assert.Equal("Maman", jourJ.Transfert.RecevantNom);
        Assert.Equal(Rose, jourJ.Transfert.RecevantCouleur);
    }

    // Un jour à venir SANS transfert est unicolore ; SANS slot est sans lieu (pas d'erreur).
    [Fact]
    public void Should_Restituer_un_jour_a_venir_sans_transfert_unicolore_et_sans_slot_sans_lieu()
    {
        var aVenir = Query(new FakeSlotRepository(), new FakePeriodeRepository(), new FakeTransfertRepository())
            .Lire(Mardi_07_07_2026, LeaId);

        var jourVide = aVenir.Single(j => j.Date == Jeudi_09_07_2026);
        Assert.Null(jourVide.Transfert); // unicolore : aucun cédant/recevant
        Assert.Empty(jourVide.Slots);    // sans lieu : le « où » est absent, pas d'erreur
    }

    // Le transfert DÉRIVÉ (succession de périodes, s31) est LU par composition, sans réimplémentation.
    [Fact]
    public void Should_Restituer_le_transfert_derive_par_succession_de_periodes_sur_un_jour_a_venir()
    {
        var periodes = new FakePeriodeRepository();
        // Papa finit le 07/07, Maman débute le 08/07 → bascule dérivée le 08/07 (D3, s31)
        periodes.Enregistrer(PeriodeDeGarde.Affecter(Papa,
            new DateTime(2026, 7, 1), new DateTime(2026, 7, 7)).Valeur!);
        periodes.Enregistrer(PeriodeDeGarde.Affecter(Maman,
            new DateTime(2026, 7, 8), new DateTime(2026, 7, 14)).Valeur!);

        var aVenir = Query(new FakeSlotRepository(), periodes, new FakeTransfertRepository())
            .Lire(Mardi_07_07_2026, LeaId);

        var jourBascule = aVenir.Single(j => j.Date == Mercredi_08_07_2026);
        Assert.NotNull(jourBascule.Transfert);
        Assert.Equal("Papa", jourBascule.Transfert!.CedantNom);   // cédant = période finissante
        Assert.Equal("Maman", jourBascule.Transfert.RecevantNom); // recevant = période débutante
    }

    // Priorité SAISI > DÉRIVÉ : un transfert saisi ce jour-là prime sur la bascule dérivée (aucun doublon).
    [Fact]
    public void Should_Prioriser_le_transfert_saisi_sur_le_derive_sur_un_jour_a_venir()
    {
        var periodes = new FakePeriodeRepository();
        periodes.Enregistrer(PeriodeDeGarde.Affecter(Papa,
            new DateTime(2026, 7, 1), new DateTime(2026, 7, 7)).Valeur!);
        periodes.Enregistrer(PeriodeDeGarde.Affecter(Maman,
            new DateTime(2026, 7, 8), new DateTime(2026, 7, 14)).Valeur!);

        // Transfert SAISI Maman → Papa le 08/07 : doit primer sur la bascule dérivée Papa → Maman
        var transferts = new FakeTransfertRepository();
        transferts.Enregistrer(Transfert.Definir(Maman, Papa, Ecole,
            TimeSpan.FromHours(8.5), Mercredi_08_07_2026.ToDateTime(TimeOnly.MinValue)).Valeur!);

        var aVenir = Query(new FakeSlotRepository(), periodes, transferts).Lire(Mardi_07_07_2026, LeaId);

        var jour = aVenir.Single(j => j.Date == Mercredi_08_07_2026);
        Assert.NotNull(jour.Transfert);
        Assert.Equal("Maman", jour.Transfert!.CedantNom);   // le SAISI (Maman → Papa) prime
        Assert.Equal("Papa", jour.Transfert.RecevantNom);
    }
}
