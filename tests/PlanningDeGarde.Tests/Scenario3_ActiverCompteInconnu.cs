using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 24 — Sc.3 — Rejet : activer un compte inconnu (@back)
//   Tranche BACKEND (frontière Application) : activer un id qui ne correspond à AUCUN compte est un
//   REFUS motif clair (Result échec, pas d'exception silencieuse) — AUCUNE mutation au store (aucun
//   compte créé, aucun statut changé). Miroir des refus « sans écriture » (s22).
public class Scenario3_ActiverCompteInconnu
{
    private const string IdAbsent = "id-absent";

    // ---------- Acceptation (boucle externe, frontière Application, store réel) ----------
    // Traduit le scénario Gherkin : aucun compte ne porte « id-absent » ; l'activation échoue avec un
    // motif clair, et le store réel reste vide (aucune mutation, aucun compte fantôme créé).
    [Fact]
    public void Acceptation_Should_Echouer_avec_motif_clair_sans_aucune_mutation_When_on_active_un_compte_inconnu()
    {
        var referentiel = new ReferentielComptesEnMemoire();
        var handler = new ActiverCompteHandler(referentiel, referentiel);

        var resultat = handler.Handle(new ActiverCompteCommand(IdAbsent));

        Assert.False(resultat.EstSucces);                        // refus
        Assert.False(string.IsNullOrWhiteSpace(resultat.Motif)); // ... avec un motif clair
        Assert.Empty(referentiel.EnumererComptes());             // ... aucune mutation (store inchangé)
    }

    // ---------- Test #1 — Driver : un id inconnu est refusé (motif clair), pas un faux succès ----------
    // Contradiction : le handler retourne aujourd'hui TOUJOURS un succès (aucune résolution du compte).
    // Force la garde « compte introuvable » : résoudre le compte sur le référentiel et refuser si absent.
    [Fact]
    public void Should_Refuser_avec_motif_compte_introuvable_When_l_id_ne_correspond_a_aucun_compte()
    {
        var referentiel = new FakeReferentielComptes();
        var handler = new ActiverCompteHandler(referentiel, referentiel);

        var resultat = handler.Handle(new ActiverCompteCommand(IdAbsent));

        Assert.False(resultat.EstSucces);
        Assert.False(string.IsNullOrWhiteSpace(resultat.Motif));
    }

    // ---------- Test #2 — Driver : aucune mutation n'est appliquée au store ----------
    // Contradiction : le handler appelle aujourd'hui _editeur.Activer(id) même pour un id inconnu.
    // Force le refus AVANT toute écriture : aucun compte créé, aucun statut changé (store inchangé).
    [Fact]
    public void Should_Ne_creer_aucun_compte_ni_changer_aucun_statut_When_on_active_un_id_inconnu()
    {
        var referentiel = new FakeReferentielComptes();
        var handler = new ActiverCompteHandler(referentiel, referentiel);

        handler.Handle(new ActiverCompteCommand(IdAbsent));

        Assert.Empty(referentiel.EnumererComptes());
    }
}
