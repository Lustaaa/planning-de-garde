using System.Collections.Generic;
using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 08 — Sc.2 — Recolorier un acteur : la case et la légende changent de couleur (@nominal, 🖥️ IHM)
//   Tranche BACKEND (tdd-auto) : store mutable « recolorier », handler EditerActeur étendu à la
//   couleur, + une caractérisation de l'indépendance nom↔couleur (filet anti-régression).
//   L'acceptation runtime (la case ET la légende changent de teinte SANS rechargement, front
//   WASM + API distante + store réel + SignalR) est menée séparément par ihm-builder.
//
//   On NE teste PAS ici un rendu Blazor : on pilote (1) le store réel qui rend la couleur
//   éditable sur l'id stable (autre surface que le nom), (2) le handler qui route la couleur
//   vers le store et confirme l'effet, (3) l'indépendance nom↔couleur (deux surfaces séparées).
public class Scenario2_RecolorierActeur
{
    private const string ParentB = "parent-b";
    private const string Bruno = "Bruno";    // seed en dur (Foyer.NomsParResponsable)
    private const string Orange = "orange";  // seed couleur en dur (Foyer.CouleursParActeur)
    private const string Violet = "violet";  // couleur éditée

    // ---------- Test #1 — Driver : store mutable seedé, CouleurDe reflète la dernière écriture ----------
    // Contradiction : aujourd'hui la couleur est lue d'un dictionnaire static readonly
    // (Foyer.CouleursParActeur) — aucun moyen de recolorier, la couleur résolue est immuable.
    // Distinct du renommage (Sc.1) : autre surface du store. Force recolorier(id, couleur) dont
    // CouleurDe(id) reflète la dernière écriture.
    [Fact]
    public void Should_Resoudre_la_nouvelle_couleur_pour_l_identifiant_stable_When_un_acteur_deja_seme_est_recolorie_dans_le_store_de_configuration()
    {
        var store = new ConfigurationFoyerEnMemoire();
        Assert.Equal(Orange, store.CouleurDe(ParentB)); // seed d'origine (miroir de Foyer.CouleursParActeur)

        store.Recolorier(ParentB, Violet);

        Assert.Equal(Violet, store.CouleurDe(ParentB)); // la lecture suit la dernière écriture
    }

    // ---------- Test #2 — Driver : la commande applique la nouvelle couleur et confirme l'effet ----------
    // Contradiction : le handler EditerActeur (Sc.1) ne route que le NOM — le champ couleur? n'est pas
    // appliqué. Force le routage de la couleur vers la surface couleur du store + confirmation, sans
    // toucher au nom (édition couleur-seule : le nom absent ne doit pas écraser le libellé existant).
    [Fact]
    public void Should_Appliquer_la_nouvelle_couleur_et_confirmer_l_effet_When_la_commande_recolorie_un_acteur_connu_vers_une_teinte_du_set()
    {
        var configuration = new FakeConfigurationFoyer(
            new Dictionary<string, string> { [ParentB] = Bruno },
            new Dictionary<string, string> { [ParentB] = Orange });
        var handler = new EditerActeurHandler(configuration, new FakeNotificateurPlanning());

        var resultat = handler.Handle(new EditerActeurCommand(ParentB, Couleur: Violet));

        Assert.True(resultat.EstSucces);                         // l'effet est confirmé
        Assert.Equal(Violet, resultat.Valeur!.Couleur);         // ... porteur de la couleur appliquée
        Assert.Equal(ParentB, resultat.Valeur!.ActeurId);       // ... sur l'identifiant stable inchangé
        Assert.Equal(Violet, configuration.CouleurDe(ParentB)); // le store a réellement été recolorié
        Assert.Equal(Bruno, configuration.NomDe(ParentB));      // ... sans toucher au nom (couleur-seule)
    }

    // ---------- Test #3 — Caractérisation : recolorier ne touche pas le nom (early green attendu) ----------
    // ⚠️ early green ANTICIPÉ (cf. table 02-*.md) : nom et couleur vivent dans deux surfaces séparées
    // du store ; recolorier ne touche pas le nom PAR CONSTRUCTION. Filet anti-régression documentant
    // l'indépendance nom↔couleur (pas un driver) — verrouille la séparation des deux dictionnaires.
    [Fact]
    public void Should_Conserver_le_nom_de_l_acteur_When_seule_sa_couleur_est_modifiee()
    {
        var store = new ConfigurationFoyerEnMemoire();
        Assert.Equal(Bruno, store.NomDe(ParentB)); // seed nom (Foyer.NomsParResponsable)

        store.Recolorier(ParentB, Violet);

        Assert.Equal(Bruno, store.NomDe(ParentB));      // le nom est conservé : surface distincte du nom
        Assert.Equal(Violet, store.CouleurDe(ParentB)); // seule la couleur a changé
    }
}
