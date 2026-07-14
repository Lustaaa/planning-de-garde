using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 42 — Sc.2 — Composer le « où » (slots) + le transfert du jour (@back)
//   Étant donné la carte du jour (Sc.1) pour une date + l'enfant sélectionné
//   Et cette date porte un ou plusieurs SLOTS de localisation (s29) et un TRANSFERT de responsabilité
//   Alors elle restitue AUSSI le(s) slot(s) du jour (le « où ») dans le même payload
//   Et elle restitue le TRANSFERT cédant → recevant (noms + couleurs résolus), SAISI ou DÉRIVÉ, priorité SAISI > DÉRIVÉ
//   Et un jour SANS transfert est unicolore (Transfert absent) ; un jour SANS slot est SANS lieu (pas d'erreur)
//   Et le transfert est LU sans être modifié (composition de la dérivation s31 existante)
public class Scenario42_S2_CarteDuJourOuEtTransfert
{
    private const string Papa = "papa";
    private const string Maman = "maman";
    private const string Bleu = "bleu";
    private const string Rose = "rose";
    private const string LeaId = "enfant-lea";
    private const string TomId = "enfant-tom";
    private const string Ecole = "ecole";

    private static readonly DateOnly JourJ_08_07_2026 = new(2026, 7, 8);
    private static readonly DateOnly JourVide_09_07_2026 = new(2026, 7, 9);

    private static CarteDuJourQuery Query(ISlotRepository slots, IPeriodeRepository periodes, ITransfertRepository transferts)
        => new(new GrilleAgendaQuery(
            slots, periodes,
            new FakePaletteCouleurs(new Dictionary<string, string> { [Papa] = Bleu, [Maman] = Rose, [Ecole] = "vert" }),
            new FakeReferentielResponsables(new Dictionary<string, string> { [Papa] = "Papa", [Maman] = "Maman" }),
            new FakeReferentielCycleDeFond(),
            new FakeEnumerationActeursFoyer(Papa, Maman),
            transferts: transferts));

    // ---------- Acceptation (frontière Application) ----------
    [Fact]
    public void Acceptation_Should_Restituer_le_ou_de_lenfant_et_le_transfert_saisi_du_jour()
    {
        var slots = new FakeSlotRepository();
        slots.Enregistrer(SlotDeLocalisation.Poser(LeaId, Ecole,
            new DateTime(2026, 7, 8, 8, 30, 0), new DateTime(2026, 7, 8, 16, 30, 0)).Valeur!);
        slots.Enregistrer(SlotDeLocalisation.Poser(TomId, Ecole,
            new DateTime(2026, 7, 8, 9, 0, 0), new DateTime(2026, 7, 8, 17, 0, 0)).Valeur!); // autre enfant

        var transferts = new FakeTransfertRepository();
        transferts.Enregistrer(Transfert.Definir(Papa, Maman, Ecole,
            TimeSpan.FromHours(8.5), JourJ_08_07_2026.ToDateTime(TimeOnly.MinValue)).Valeur!);

        var carte = Query(slots, new FakePeriodeRepository(), transferts).Lire(JourJ_08_07_2026, LeaId);

        // Le « où » : seul le slot de Léa (l'enfant sélectionné), pas celui de Tom
        var slot = Assert.Single(carte.Slots);
        Assert.Equal(Ecole, slot.Libelle);

        // Le transfert saisi cédant → recevant, noms + couleurs résolus
        Assert.NotNull(carte.Transfert);
        Assert.Equal("Papa", carte.Transfert!.CedantNom);
        Assert.Equal(Bleu, carte.Transfert.CedantCouleur);
        Assert.Equal("Maman", carte.Transfert.RecevantNom);
        Assert.Equal(Rose, carte.Transfert.RecevantCouleur);
    }

    [Fact]
    public void Should_Restituer_un_jour_sans_transfert_unicolore_et_sans_slot_sans_lieu()
    {
        var carte = Query(new FakeSlotRepository(), new FakePeriodeRepository(), new FakeTransfertRepository())
            .Lire(JourVide_09_07_2026, LeaId);

        Assert.Null(carte.Transfert); // unicolore : aucun cédant/recevant
        Assert.Empty(carte.Slots);    // sans lieu : le « où » est absent, pas d'erreur
    }

    // ---------- Boucle interne (TDD) ----------

    // Le transfert DÉRIVÉ (succession de périodes, s31) est LU par composition, sans réimplémentation.
    [Fact]
    public void Should_Restituer_le_transfert_derive_par_succession_de_periodes()
    {
        var periodes = new FakePeriodeRepository();
        // Papa finit le 07/07, Maman débute le 08/07 → bascule dérivée le 08/07 (D3, s31)
        periodes.Enregistrer(PeriodeDeGarde.Affecter(Papa,
            new DateTime(2026, 7, 1), new DateTime(2026, 7, 7)).Valeur!);
        periodes.Enregistrer(PeriodeDeGarde.Affecter(Maman,
            new DateTime(2026, 7, 8), new DateTime(2026, 7, 14)).Valeur!);

        var carte = Query(new FakeSlotRepository(), periodes, new FakeTransfertRepository())
            .Lire(JourJ_08_07_2026, LeaId);

        Assert.NotNull(carte.Transfert);
        Assert.Equal("Papa", carte.Transfert!.CedantNom);   // cédant = période finissante
        Assert.Equal("Maman", carte.Transfert.RecevantNom); // recevant = période débutante
    }
}
