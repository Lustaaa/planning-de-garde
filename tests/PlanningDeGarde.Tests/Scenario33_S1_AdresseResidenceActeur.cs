using System.Collections.Generic;
using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 33 — Sc.1 — Adresse de résidence de l'acteur (frontière Application) @back
//   Tranche BACKEND : champ de MODÈLE neuf « adresse de résidence » — port d'écriture
//   (ChangerAdresse), relecture (AdresseDe), commande d'édition étendue, snapshot enrichi.
//   L'adresse est un attribut OPTIONNEL et INDÉPENDANT du nom/couleur : une édition adresse-seule
//   ne touche pas les autres surfaces, une adresse VIDE est acceptée (contrairement au nom vide,
//   refusé). L'acceptation runtime durable (Mongo réel) est menée séparément (Api.Tests).
public class Scenario33_S1_AdresseResidenceActeur
{
    private const string ParentA = "parent-a";
    private const string Alice = "Alice";
    private const string AdresseA = "12 rue des Lilas, Lyon";

    // ---------- Test #1 — Driver store : ChangerAdresse écrit, AdresseDe relit la dernière écriture ----------
    // Contradiction : le store ne porte AUCUNE surface « adresse » — rien à écrire ni relire.
    // Force un champ adresse éditable sur l'id stable, dont la relecture suit la dernière écriture,
    // ET l'acceptation d'une adresse VIDE (champ optionnel).
    [Fact]
    public void Should_Relire_l_adresse_ecrite_pour_l_identifiant_stable_When_l_adresse_de_residence_est_changee_dans_le_store()
    {
        var store = new ConfigurationFoyerEnMemoire();
        Assert.Null(store.AdresseDe(ParentA)); // aucune adresse par défaut (attribut optionnel)

        store.ChangerAdresse(ParentA, AdresseA);
        Assert.Equal(AdresseA, store.AdresseDe(ParentA)); // la lecture suit la dernière écriture

        store.ChangerAdresse(ParentA, string.Empty);
        Assert.Equal(string.Empty, store.AdresseDe(ParentA)); // adresse VIDE acceptée telle quelle
    }

    // ---------- Test #2 — Handler : la commande applique l'adresse et confirme l'effet, id inchangé ----------
    // Contradiction : la commande d'édition ne porte pas d'adresse — rien ne mute cette surface.
    // Force l'orchestration : une adresse renseignée est écrite via le port, confirmée au snapshot,
    // sur l'id stable inchangé, SANS toucher le nom ni la couleur (surfaces indépendantes).
    [Fact]
    public void Should_Appliquer_l_adresse_et_confirmer_l_effet_sans_toucher_nom_ni_couleur_When_la_commande_edite_l_adresse_seule()
    {
        var configuration = new FakeConfigurationFoyer(
            new Dictionary<string, string> { [ParentA] = Alice },
            new Dictionary<string, string> { [ParentA] = "bleu" });
        var handler = new EditerActeurHandler(configuration, new FakeNotificateurPlanning());

        var resultat = handler.Handle(new EditerActeurCommand(ParentA, Adresse: AdresseA));

        Assert.True(resultat.EstSucces);
        Assert.Equal(AdresseA, resultat.Valeur!.Adresse);   // l'adresse appliquée est confirmée
        Assert.Equal(ParentA, resultat.Valeur!.ActeurId);   // sur l'id stable inchangé (édition, pas recréation)
        Assert.Equal(AdresseA, configuration.AdresseDe(ParentA)); // le store a réellement été muté
        Assert.Equal(Alice, configuration.NomDe(ParentA));  // nom NON touché (adresse-seule)
        Assert.Equal("bleu", configuration.CouleurDe(ParentA)); // couleur NON touchée
    }

    // ---------- Test #3 — Handler : une adresse VIDE est acceptée sans écriture partielle des autres champs ----
    // Contradiction : sans traitement dédié, une adresse vide pourrait être refusée (comme le nom) ou
    // bloquer les autres champs. Force l'acceptation d'une adresse vide (optionnelle), le reste intact.
    [Fact]
    public void Should_Accepter_une_adresse_vide_sans_bloquer_les_autres_champs_When_la_commande_porte_une_adresse_vide()
    {
        var configuration = new FakeConfigurationFoyer(
            new Dictionary<string, string> { [ParentA] = Alice });
        var handler = new EditerActeurHandler(configuration, new FakeNotificateurPlanning());

        var resultat = handler.Handle(new EditerActeurCommand(ParentA, Nom: "Alicia", Adresse: string.Empty));

        Assert.True(resultat.EstSucces);                          // adresse vide acceptée (≠ nom vide, refusé)
        Assert.Equal(string.Empty, configuration.AdresseDe(ParentA)); // adresse vide écrite telle quelle
        Assert.Equal("Alicia", configuration.NomDe(ParentA));    // le nom fourni a bien été appliqué
    }
}
