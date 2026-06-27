using PlanningDeGarde.Infrastructure;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 08 — Sc.10 — Volatilité : après redémarrage, le seed d'origine réapparaît (@limite)
//   Tranche BACKEND (tdd-auto) : CARACTÉRISATION (early green ATTENDU, filet anti-régression — pas
//   un driver). « Le serveur redémarre » = la mémoire partagée est RECONSTRUITE, donc RE-SEEDÉE
//   depuis Foyer. Le store ConfigurationFoyerEnMemoire seede ses dictionnaires À LA CONSTRUCTION
//   (miroir du seed en dur) ; une édition (renommer/recolorier) ne mute que l'instance courante.
//   Une NOUVELLE instance ne porte donc AUCUNE édition de la session précédente → seed d'origine
//   par construction. Aucun rouge attendu : ce test documente la VOLATILITÉ ASSUMÉE (dette à
//   éteindre au palier 13 — persistance réelle, sans toucher au domaine).
//
//   Pas de driver runtime : le redémarrage n'est pas un geste d'IHM. Reconstruire le store EST le
//   redémarrage — inutile de simuler un vrai redémarrage serveur en runtime.
public class Scenario10_VolatiliteReseed
{
    private const string ParentB = "parent-b";
    private const string Bruno = "Bruno";          // seed nom d'origine (Foyer.NomsParResponsable)
    private const string BrunoMartin = "Bruno Martin"; // nom édité dans la session précédente
    private const string Orange = "orange";        // seed couleur d'origine (Foyer.CouleursParActeur)
    private const string Violet = "violet";        // couleur éditée dans la session précédente

    // ---------- Test #1 — Caractérisation : la (re)construction du store restitue le seed d'origine ----------
    // ⚠️ early green ANTICIPÉ (cf. table 10-*.md) : le store seede depuis Foyer À LA CONSTRUCTION
    // (nécessaire pour lire « Bruno »/orange au départ, cf. Sc.1/Sc.2). Une instance fraîche ne
    // porte aucune édition → seed d'origine par construction. Filet documentant la volatilité assumée.
    [Fact]
    public void Should_Restituer_le_nom_et_la_couleur_d_origine_du_seed_en_perdant_les_editions_de_la_session_precedente_When_le_store_de_configuration_est_reconstruit()
    {
        // Session précédente : parent-b est renommé « Bruno » → « Bruno Martin » et recolorié
        // orange → violet. L'instance courante résout bien les valeurs éditées.
        var sessionPrecedente = new ConfigurationFoyerEnMemoire();
        sessionPrecedente.Renommer(ParentB, BrunoMartin);
        sessionPrecedente.Recolorier(ParentB, Violet);
        Assert.Equal(BrunoMartin, sessionPrecedente.NomDe(ParentB));   // l'édition vit dans la session...
        Assert.Equal(Violet, sessionPrecedente.CouleurDe(ParentB));    // ... courante

        // Le serveur redémarre : la mémoire partagée est reconstruite (= nouvelle instance re-seedée).
        var apresRedemarrage = new ConfigurationFoyerEnMemoire();

        // L'édition volatile est perdue : le seed d'origine (Foyer) réapparaît.
        Assert.Equal(Bruno, apresRedemarrage.NomDe(ParentB));    // « Bruno Martin » perdu → seed « Bruno »
        Assert.Equal(Orange, apresRedemarrage.CouleurDe(ParentB)); // violet perdu → seed orange
    }
}
