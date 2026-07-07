using System;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Builders;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 27 — S1 — Un lieu ajouté au foyer devient disponible à la saisie (@back)
//   Tranche BACKEND (frontière Application) : commande/handler AjouterLieu qui persiste le lieu via
//   le port d'écriture IEditeurLieux, l'expose à l'énumération IEnumerationLieux, et — CRUCIAL — le
//   rend acceptable à la POSE d'un slot (PoserSlotHandler valide l'existence sur ce même canal de
//   lecture vivant, plus la liste en dur Foyer.Lieux). La durabilité Mongo est prouvée en S4.
public class Scenario1_LieuAjouteDisponible
{
    private const string Piscine = "piscine";

    // ---------- Acceptation (boucle externe, frontière Application) ----------
    // Étant donné le référentiel de lieux du foyer, quand un parent ajoute le lieu « piscine »,
    // alors l'énumération le contient ET poser un slot au lieu « piscine » est accepté (il existe).
    [Fact]
    public void Acceptation_Should_Enumerer_piscine_et_accepter_la_pose_d_un_slot_a_ce_lieu_When_le_parent_ajoute_le_lieu_piscine()
    {
        var referentiel = new ReferentielLieuxEnMemoire();
        var ajouter = new AjouterLieuHandler(referentiel);
        var poser = new PoserSlotHandler(new FakeSlotRepository(), referentiel, new FakeNotificateurPlanning());

        var ajout = ajouter.Handle(new AjouterLieuCommand(Piscine));

        Assert.True(ajout.EstSucces);
        var lieuId = ajout.Valeur!.LieuId;
        // L'énumération des lieux du foyer contient « piscine »
        Assert.Contains(referentiel.EnumererLieux(), lieu => lieu.Libelle == Piscine);

        // Poser un slot au lieu « piscine » est accepté (le lieu existe désormais)
        var pose = poser.Handle(new PoserSlotCommand("lea", lieuId,
            new DateTime(2025, 7, 15, 8, 30, 0), new DateTime(2025, 7, 15, 16, 30, 0)));
        Assert.True(pose.EstSucces);
    }

    // ---------- Test #1 — Driver : un ajout fait EXISTER le lieu à l'énumération ----------
    // Contradiction : le handler ne persiste rien (stub) — le lieu ajouté reste absent de
    // l'énumération. Force l'écriture via le port IEditeurLieux, de sorte que le lieu soit énuméré
    // avec son libellé « piscine ».
    [Fact]
    public void Should_Faire_exister_le_lieu_ajoute_a_l_enumeration_When_le_parent_ajoute_un_lieu()
    {
        var referentiel = new FakeReferentielLieux();
        var handler = new AjouterLieuHandler(referentiel);

        var resultat = handler.Handle(new AjouterLieuCommand(Piscine));

        Assert.True(resultat.EstSucces);
        var lieuId = resultat.Valeur!.LieuId;
        var lieu = referentiel.EnumererLieux().Single(l => l.Id == lieuId);
        Assert.Equal(Piscine, lieu.Libelle); // le lieu ajouté est résolu par son libellé sur son id
    }
}
