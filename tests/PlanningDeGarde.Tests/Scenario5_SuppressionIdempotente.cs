using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 13 — Sc.5 — Supprimer un acteur absent ou déjà supprimé : no-op qui réussit (@erreur, @caractérisation)
//   Tranche BACKEND (tdd-auto) : CARACTÉRISATION (⚠️ early green ATTENDU, filet anti-régression de la
//   sémantique DELETE idempotente — PAS un driver). L'impl minimale du Sc.1 garantit déjà l'idempotence :
//   ConfigurationFoyerEnMemoire.Supprimer délègue à Dictionary.Remove (no-op sur clé absente) et le
//   handler renvoie Result.Succes INCONDITIONNELLEMENT — aucun chemin de refus (philosophie non-refus,
//   règle 6 ; D3). Supprimer un id absent réussit sans effet ; supprimer deux fois réussit aux deux appels.
//   Si ce test passait ROUGE, corriger le retrait côté adaptateur (le rendre tolérant à l'absence), JAMAIS
//   ajouter une garde de refus.
public class Scenario5_SuppressionIdempotente
{
    private const string ParentA = "parent-a";
    private const string ParentB = "parent-b";

    // ---------- Acceptation (boucle externe, frontière Application — store réel + handler réel) ----------
    [Fact]
    public void Acceptation_Should_Reussir_sans_lever_d_erreur_ni_modifier_la_configuration_When_un_acteur_absent_ou_deja_supprime_est_supprime()
    {
        var store = new ConfigurationFoyerEnMemoire(); // seeds : parent-a, parent-b, …
        var handler = new SupprimerActeurHandler(store, new FakeNotificateurPlanning());

        // Supprimer un id INEXISTANT : succès sans effet (Parent A / Parent B toujours présents).
        var rInexistant = handler.Handle(new SupprimerActeurCommand("acteur-inexistant"));
        Assert.True(rInexistant.EstSucces);
        Assert.Contains(ParentA, store.EnumererActeurs());
        Assert.Contains(ParentB, store.EnumererActeurs());

        // Supprimer DEUX FOIS Parent B : succès aux deux appels, aucun effet supplémentaire après le 1er.
        var rPremier = handler.Handle(new SupprimerActeurCommand(ParentB));
        var rSecond = handler.Handle(new SupprimerActeurCommand(ParentB));
        Assert.True(rPremier.EstSucces);
        Assert.True(rSecond.EstSucces);
        Assert.DoesNotContain(ParentB, store.EnumererActeurs()); // retiré une seule fois, état stable
        Assert.Contains(ParentA, store.EnumererActeurs());       // les autres acteurs restent
    }

    // ---------- Test #1 — Caractérisation (⚠️ early green attendu, pas driver) ----------
    // Documente le DELETE idempotent : un acteur absent (ou déjà supprimé) supprimé renvoie succès sans
    // effet ni erreur — aucun refus. La seconde suppression est elle aussi un no-op qui réussit.
    [Fact]
    public void Should_Reussir_sans_effet_ni_erreur_When_l_acteur_a_supprimer_est_absent_ou_deja_supprime()
    {
        var store = new ConfigurationFoyerEnMemoire();
        var handler = new SupprimerActeurHandler(store, new FakeNotificateurPlanning());
        var avant = store.EnumererActeurs().OrderBy(id => id).ToList();

        var premier = handler.Handle(new SupprimerActeurCommand("acteur-inexistant"));
        var second = handler.Handle(new SupprimerActeurCommand("acteur-inexistant"));

        Assert.True(premier.EstSucces);  // no-op qui réussit (pas de refus, règle 6)
        Assert.True(second.EstSucces);   // ... idempotent : la seconde suppression réussit aussi
        Assert.Equal(avant, store.EnumererActeurs().OrderBy(id => id).ToList()); // configuration inchangée
    }
}
