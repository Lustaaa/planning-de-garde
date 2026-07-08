using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 30 — S1 — Ajouter un enfant valide (@back)
//   Étant donné un foyer configuré
//   Quand un Parent ajoute un enfant de prénom "Léa"
//   Alors la commande réussit
//   Et l'enfant est enregistré avec un identifiant stable neuf (jamais dérivé du prénom)
//   Et son snapshot porte : prénom = "Léa"
//   Et la diffusion temps réel de mise à jour est déclenchée
//
// Miroir strict du référentiel de lieux (s27 S1) : commande/handler AjouterEnfant qui persiste
// l'enfant via le port d'écriture IEditeurEnfants, l'expose à l'énumération IEnumerationEnfants sur
// un identifiant stable OPAQUE (miroir création de rôle s21 — jamais dérivé du prénom), et diffuse
// la mise à jour temps réel. La durabilité Mongo est prouvée en S6.
public class Scenario30_S1_AjouterEnfant
{
    private const string Lea = "Léa";

    // ---------- Acceptation (boucle externe, frontière Application) ----------
    // Étant donné un foyer sans cet enfant, quand un Parent ajoute « Léa », alors l'énumération le
    // contient avec un id neuf (jamais « Léa »), son snapshot porte le prénom, et la diffusion part.
    [Fact]
    public void Acceptation_Should_Enregistrer_Lea_avec_un_id_stable_neuf_et_diffuser_When_un_Parent_ajoute_l_enfant_Lea()
    {
        var referentiel = new FakeReferentielEnfants();
        var notificateur = new FakeNotificateurPlanning();
        var handler = new AjouterEnfantHandler(referentiel, referentiel, notificateur);

        var resultat = handler.Handle(new AjouterEnfantCommand(Lea));

        // La commande réussit
        Assert.True(resultat.EstSucces);
        var enfantId = resultat.Valeur!.EnfantId;

        // Identifiant stable neuf OPAQUE : jamais dérivé du prénom
        Assert.NotEqual(Lea, enfantId);

        // L'enfant est énuméré, son snapshot porte le prénom « Léa » sur son id stable
        var enfant = referentiel.EnumererEnfants().Single(e => e.Id == enfantId);
        Assert.Equal(Lea, enfant.Prenom);

        // La diffusion temps réel de mise à jour est déclenchée
        Assert.Equal(1, notificateur.NombreDeNotifications);
    }

    // ---------- Test #1 — Driver : un ajout fait EXISTER l'enfant à l'énumération ----------
    // Contradiction : le handler ne persiste rien (stub) — l'enfant ajouté reste absent de
    // l'énumération. Force l'écriture via le port IEditeurEnfants (prénom résolu sur l'id stable).
    [Fact]
    public void Should_Faire_exister_l_enfant_ajoute_a_l_enumeration_When_le_parent_ajoute_un_enfant()
    {
        var referentiel = new FakeReferentielEnfants();
        var handler = new AjouterEnfantHandler(referentiel, referentiel, new FakeNotificateurPlanning());

        var resultat = handler.Handle(new AjouterEnfantCommand(Lea));

        Assert.True(resultat.EstSucces);
        var enfantId = resultat.Valeur!.EnfantId;
        var enfant = referentiel.EnumererEnfants().Single(e => e.Id == enfantId);
        Assert.Equal(Lea, enfant.Prenom);
        Assert.NotEqual(Lea, enfantId); // id opaque, jamais le prénom
    }

    // ---------- Test #2 — Driver : la diffusion temps réel est déclenchée ----------
    // Contradiction : l'impl minimale du #1 persiste mais ne diffuse pas. Force l'appel au
    // notificateur (miroir de la pose : une écriture du foyer diffuse la mise à jour).
    [Fact]
    public void Should_Declencher_la_diffusion_temps_reel_When_le_parent_ajoute_un_enfant()
    {
        var referentiel = new FakeReferentielEnfants();
        var notificateur = new FakeNotificateurPlanning();
        var handler = new AjouterEnfantHandler(referentiel, referentiel, notificateur);

        handler.Handle(new AjouterEnfantCommand(Lea));

        Assert.Equal(1, notificateur.NombreDeNotifications);
    }
}
