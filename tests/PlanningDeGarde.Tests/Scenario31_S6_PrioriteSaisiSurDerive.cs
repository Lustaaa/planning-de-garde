using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 31 — Sc.6 (D3) — Priorité SAISI > DÉRIVÉ : pas de doublon (@back)
//   Étant donné une succession de périodes qui dériverait un transfert le jour J
//   Et un transfert SAISI existant le même jour J pour le même enfant
//   Quand la résolution s'exécute sur le jour J
//   Alors le transfert saisi prime et est seul retenu
//   Et aucun transfert dérivé en doublon n'est produit
//
// Frontière Application (GrilleAgendaQuery). Test DISCRIMINANT : la succession et le transfert saisi
// désignent des acteurs (donc des couleurs) DIFFÉRENTS, de sorte qu'un vert prouve que c'est bien le
// SAISI qui est rendu (et non la dérivation). Un contrôle (même succession SANS saisi) vérifie que le
// montage dériverait effectivement — le vert n'est donc pas vacant.
public class Scenario31_S6_PrioriteSaisiSurDerive
{
    private static readonly DateOnly Reference_24_06_2026 = new(2026, 6, 24);
    private static readonly DateOnly JourJ_26_06_2026 = new(2026, 6, 26); // jour de bascule (début du successeur)

    // Succession qui DÉRIVERAIT « cedant → recevant » le jour de bascule (J).
    private static FakePeriodeRepository SuccessionDerivable()
    {
        var repo = new FakePeriodeRepository();
        repo.Enregistrer(PeriodeDeGarde.Affecter(
            "cedant", new DateTime(2026, 6, 23), new DateTime(2026, 6, 25, 23, 59, 0)).Valeur!);
        repo.Enregistrer(PeriodeDeGarde.Affecter(
            "recevant", new DateTime(2026, 6, 26), new DateTime(2026, 6, 28)).Valeur!);
        return repo;
    }

    // Transfert SAISI le même jour, entre des acteurs DIFFÉRENTS de la succession (couleurs discriminantes).
    private static FakeTransfertRepository SaisiPapaVersMamanLeJ()
    {
        var repo = new FakeTransfertRepository();
        repo.Enregistrer(Transfert
            .Definir("papa", "maman", "ecole", TimeSpan.FromHours(8.5), JourJ_26_06_2026.ToDateTime(TimeOnly.MinValue)).Valeur!);
        return repo;
    }

    private static GrilleAgendaQuery Query(IPeriodeRepository periodes, ITransfertRepository? transferts = null)
        => new(
            new FakeSlotRepository(),
            periodes,
            new FakePaletteCouleurs(new Dictionary<string, string>
            {
                ["cedant"] = "bleu", ["recevant"] = "rose", ["papa"] = "vert", ["maman"] = "jaune",
            }),
            new FakeReferentielResponsables(new Dictionary<string, string>
            {
                ["cedant"] = "Cédant", ["recevant"] = "Recevant", ["papa"] = "Papa", ["maman"] = "Maman",
            }),
            transferts: transferts);

    // ---------- Test d'acceptation (boucle externe, BDD) ----------

    [Fact]
    public void Should_retenir_le_transfert_saisi_seul_et_aucun_derive_When_un_saisi_existe_le_jour_de_bascule()
    {
        // Given — une succession qui dériverait cedant→recevant le J, ET un transfert saisi papa→maman le J.
        var caseJ = Query(SuccessionDerivable(), SaisiPapaVersMamanLeJ())
            .Projeter(Reference_24_06_2026)
            .Jours.Single(j => j.Date == JourJ_26_06_2026);

        // Then — le SAISI prime et est seul retenu : les couleurs rendues sont celles du saisi (papa→maman),
        // jamais celles de la dérivation (cedant→recevant). La case ne porte qu'UNE info transfert (aucun doublon).
        Assert.NotNull(caseJ.Transfert);
        Assert.Equal("vert", caseJ.Transfert!.CouleurDepart);   // Papa (saisi), pas Cédant (dérivé)
        Assert.Equal("jaune", caseJ.Transfert!.CouleurArrivee); // Maman (saisi), pas Recevant (dérivé)
    }

    // ---------- Contrôle (le vert n'est pas vacant) ----------

    // La MÊME succession, SANS transfert saisi, dérive bien cedant→recevant le jour de bascule : la
    // priorité testée est donc réelle (le saisi masque une dérivation qui, autrement, se produirait).
    [Fact]
    public void Should_deriver_cedant_vers_recevant_When_la_meme_succession_n_a_aucun_saisi()
    {
        var caseJ = Query(SuccessionDerivable())
            .Projeter(Reference_24_06_2026)
            .Jours.Single(j => j.Date == JourJ_26_06_2026);

        Assert.NotNull(caseJ.Transfert);
        Assert.Equal("bleu", caseJ.Transfert!.CouleurDepart);   // Cédant (dérivé)
        Assert.Equal("rose", caseJ.Transfert!.CouleurArrivee);  // Recevant (dérivé)
    }
}
