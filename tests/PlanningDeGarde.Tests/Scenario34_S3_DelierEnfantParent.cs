using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 34 — S3 — Commande « délier » un enfant d'un parent (@back)
//   Étant donné un enfant lié à un parent
//   Quand la commande « délier un enfant d'un parent » est émise (enfantId, acteurId)
//   Alors le lien est retiré et l'état persisté, l'enfant relu sans ce parent
//   Et l'identifiant stable de l'enfant et ses autres liens éventuels restent inchangés
//   Et délier un parent DÉJÀ non lié est IDEMPOTENT (neutre, sans erreur, sans écriture partielle)
//
// Frontière Application : le handler DelierEnfantParent retire le lien via le port d'écriture.
public class Scenario34_S3_DelierEnfantParent
{
    private const string LeaId = "enfant-lea";
    private const string Papa = "acteur-papa";
    private const string Maman = "acteur-maman";
    private const string Mamie = "acteur-mamie";

    // ---------- Acceptation — délier retire le lien, conserve id + autres liens ----------
    [Fact]
    public void Acceptation_Should_Retirer_le_parent_en_conservant_id_et_autres_liens_When_on_delie_un_parent()
    {
        var referentiel = new FakeReferentielEnfants().AvecEnfant(LeaId, "Léa");
        referentiel.LierParent(LeaId, Papa);
        referentiel.LierParent(LeaId, Maman);
        var handler = new DelierEnfantParentHandler(referentiel);

        var resultat = handler.Handle(new DelierEnfantParentCommand(LeaId, Papa));

        Assert.True(resultat.EstSucces);
        var lea = referentiel.EnumererEnfants().Single(e => e.Id == LeaId);
        Assert.Equal("Léa", lea.Prenom);            // id/prénom inchangés
        Assert.DoesNotContain(lea.ParentsLies, p => p.ActeurId == Papa); // le parent délié a disparu
        Assert.Contains(lea.ParentsLies, p => p.ActeurId == Maman);      // les autres liens restent
    }

    // ---------- Driver — délier un parent déjà non lié est idempotent (neutre, sans écriture) ----------
    [Fact]
    public void Should_Etre_neutre_sans_erreur_When_on_delie_un_parent_deja_non_lie()
    {
        var referentiel = new FakeReferentielEnfants().AvecEnfant(LeaId, "Léa");
        referentiel.LierParent(LeaId, Papa);
        var handler = new DelierEnfantParentHandler(referentiel);

        var resultat = handler.Handle(new DelierEnfantParentCommand(LeaId, Mamie)); // Mamie jamais liée

        Assert.True(resultat.EstSucces); // neutre, sans erreur
        var lea = referentiel.EnumererEnfants().Single(e => e.Id == LeaId);
        Assert.Single(lea.ParentsLies);      // aucune écriture partielle
        Assert.Contains(lea.ParentsLies, p => p.ActeurId == Papa); // l'unique lien existant intact
    }
}
