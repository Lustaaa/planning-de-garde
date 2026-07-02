using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 24 — Sc.2 — Idempotence : activer un compte déjà Actif (@back)
//   Tranche BACKEND (frontière Application) : activer un compte DÉJÀ Actif est un no-op qui RÉUSSIT
//   (miroir des suppressions idempotentes s16/s18) — aucune double mutation, le statut reste Actif,
//   le compte reste énuméré EXACTEMENT UNE FOIS. Verrouille la garantie d'idempotence (guard test).
public class Scenario2_ActiverCompteDejaActif
{
    private const string CompteId = "compte-alice-abc";
    private const string Email = "alice@foyer.fr";
    private const string ActeurId = "acteur-alice";

    // ---------- Acceptation (boucle externe, frontière Application, store réel) ----------
    // Traduit le scénario Gherkin : un CompteUtilisateur DÉJÀ « Actif » est activé ; la commande
    // réussit (no-op), le statut reste « Actif », et le store réel l'énumère exactement une fois
    // (aucune double mutation, aucun doublon), email et acteur inchangés.
    [Fact]
    public void Acceptation_Should_Reussir_en_no_op_statut_reste_Actif_enumere_une_seule_fois_When_on_active_un_compte_deja_Actif()
    {
        var referentiel = new ReferentielComptesEnMemoire();
        referentiel.Creer(CompteId, Email, StatutCompte.Actif, ActeurId);
        var handler = new ActiverCompteHandler(referentiel, referentiel);

        var resultat = handler.Handle(new ActiverCompteCommand(CompteId));

        Assert.True(resultat.EstSucces);                                    // no-op qui RÉUSSIT
        Assert.Single(referentiel.EnumererComptes(), c => c.Id == CompteId); // énuméré exactement une fois
        var compte = referentiel.EnumererComptes().Single(c => c.Id == CompteId);
        Assert.Equal(StatutCompte.Actif, compte.Statut);                   // le statut reste « Actif »
        Assert.Equal(Email, compte.Email);
        Assert.Equal(ActeurId, compte.ActeurId);
    }

    // ---------- Test #1 — Driver : idempotence — deux activations successives restent un unique compte Actif ----------
    // Verrouille l'absence de double mutation : activer deux fois de suite un compte Actif ne crée ni
    // doublon ni régression de statut — le référentiel reste à un seul compte Actif.
    [Fact]
    public void Should_Rester_un_unique_compte_Actif_When_on_active_deux_fois_de_suite()
    {
        var referentiel = new FakeReferentielComptes();
        referentiel.Creer(CompteId, Email, StatutCompte.Actif, ActeurId);
        var handler = new ActiverCompteHandler(referentiel, referentiel);

        Assert.True(handler.Handle(new ActiverCompteCommand(CompteId)).EstSucces);
        Assert.True(handler.Handle(new ActiverCompteCommand(CompteId)).EstSucces);

        var comptes = referentiel.EnumererComptes().Where(c => c.Id == CompteId).ToList();
        Assert.Single(comptes);
        Assert.Equal(StatutCompte.Actif, comptes[0].Statut);
    }
}
