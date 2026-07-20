using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 44 — Sc.2 — Cas LIMITE : last-write-wins + délégation à soi-même + jour hors fenêtre (@back)
//   - Jour J déjà couvert par une SURCHARGE (responsable C) → déléguer à B (B≠C) RÉAFFECTE (aucun doublon)
//   - Jour J résolu = A, déléguer à A (soi-même) → REFUS explicite, AUCUNE écriture, store intact
//   - Jour J hors de la fenêtre de grille chargée → l'écriture RÉUSSIT sans crash (affichage suit s42/s43)
//
// Frontière Application (DeleguerRecuperationHandler).
public class Scenario44_S2_DeleguerCasLimite
{
    private const string ParentA = "parent-a";
    private const string ParentB = "parent-b";
    private const string ParentC = "parent-c";
    private const string LeaId = "enfant-lea";

    private static readonly DateOnly Mercredi_08_07_2026 = new(2026, 7, 8);   // ISO 28 paire → fond Parent A
    private static readonly DateOnly LointainHorsFenetre = new(2027, 3, 17);  // très loin de toute fenêtre par défaut

    private static Dictionary<int, string> MappingPairAImpairB() => new() { [0] = ParentA, [1] = ParentB };

    private static GrilleAgendaQuery Grille(IPeriodeRepository periodes)
        => new(
            new FakeSlotRepository(), periodes,
            new FakePaletteCouleurs(new Dictionary<string, string> { [ParentA] = "bleu", [ParentB] = "orange", [ParentC] = "vert" }),
            new FakeReferentielResponsables(new Dictionary<string, string> { [ParentA] = "Alice", [ParentB] = "Bruno", [ParentC] = "Chloe" }),
            new FakeReferentielCycleDeFond(new CycleDeFond(2, MappingPairAImpairB()), LeaId),
            new FakeEnumerationActeursFoyer(ParentA, ParentB, ParentC));

    private static DeleguerRecuperationHandler Handler(IPeriodeRepository periodes)
        => new(Grille(periodes), periodes, new FakeEnumerationActeursFoyer(ParentA, ParentB, ParentC));

    // Observation de la case résolue d'un jour via la GRILLE (socle : les read models de lecture s42/s43
    // qui la composaient ont été retirés — décision PO s44 Sc.7). La semaine du jour le contient toujours.
    private static JourCase CaseDuJour(GrilleAgendaQuery grille, DateOnly jour)
        => grille.Projeter(jour, VuePlanning.Semaine).Jours.Single(j => j.Date == jour);

    [Fact]
    public void Should_Reaffecter_sans_doublon_When_le_jour_est_deja_couvert_par_une_surcharge()
    {
        var periodes = new FakePeriodeRepository();
        var debut = Mercredi_08_07_2026.ToDateTime(TimeOnly.MinValue);
        periodes.Enregistrer(PeriodeDeGarde.Affecter(ParentC, debut, debut, LeaId).Valeur!);

        var resultat = Handler(periodes).Handle(new DeleguerRecuperationCommand(Mercredi_08_07_2026, LeaId, ParentB));

        Assert.True(resultat.EstSucces);
        // Last-write-wins R11 : la surcharge du jour est RÉAFFECTÉE à B, SANS doublon de période.
        var periode = Assert.Single(periodes.AllSnapshots());
        Assert.Equal(ParentB, periode.ResponsableId);
        // La case résout désormais B.
        Assert.Equal(ParentB, CaseDuJour(Grille(periodes), Mercredi_08_07_2026).ResponsableId);
    }

    [Fact]
    public void Should_Refuser_sans_ecrire_When_delegation_a_soi_meme()
    {
        var periodes = new FakePeriodeRepository();
        // Jour J résolu par le fond = Parent A. Déléguer à Parent A (soi-même).
        var resultat = Handler(periodes).Handle(new DeleguerRecuperationCommand(Mercredi_08_07_2026, LeaId, ParentA));

        Assert.False(resultat.EstSucces);
        Assert.False(string.IsNullOrWhiteSpace(resultat.Motif));
        // Store intact : aucune écriture.
        Assert.Empty(periodes.AllSnapshots());
    }

    [Fact]
    public void Should_Refuser_soi_meme_When_le_jour_est_deja_surcharge_par_le_delegataire()
    {
        var periodes = new FakePeriodeRepository();
        var debut = Mercredi_08_07_2026.ToDateTime(TimeOnly.MinValue);
        periodes.Enregistrer(PeriodeDeGarde.Affecter(ParentC, debut, debut, LeaId).Valeur!);

        // Le responsable RÉSOLU du jour est déjà C (surcharge). Déléguer à C = soi-même → refus, store intact.
        var resultat = Handler(periodes).Handle(new DeleguerRecuperationCommand(Mercredi_08_07_2026, LeaId, ParentC));

        Assert.False(resultat.EstSucces);
        var periode = Assert.Single(periodes.AllSnapshots());
        Assert.Equal(ParentC, periode.ResponsableId); // inchangé
    }

    [Fact]
    public void Should_Ecrire_sans_crash_When_le_jour_est_hors_de_la_fenetre_chargee()
    {
        var periodes = new FakePeriodeRepository();
        // Délégataire Parent C (hors cycle de fond, donc distinct du responsable résolu quel qu'il soit) :
        // le seul enjeu ici est que l'écriture d'un jour lointain n'échoue/ne crashe pas.
        var resultat = Handler(periodes).Handle(new DeleguerRecuperationCommand(LointainHorsFenetre, LeaId, ParentC));

        Assert.True(resultat.EstSucces);
        var periode = Assert.Single(periodes.AllSnapshots());
        Assert.Equal(ParentC, periode.ResponsableId);
        Assert.Equal(LointainHorsFenetre, DateOnly.FromDateTime(periode.Debut));
    }
}
