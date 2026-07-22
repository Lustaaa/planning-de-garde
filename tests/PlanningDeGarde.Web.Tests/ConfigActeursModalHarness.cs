using System;
using System.Linq;
using AngleSharp.Dom;
using Bunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Harnais d'acceptation de NIVEAU RUNTIME de la refonte s32 (patron « tableau lecture seule + crayon →
/// modal », brief refonte-config) : centralise l'ouverture de la MODAL d'écriture d'un acteur, désormais
/// seul point d'entrée de l'écriture (l'édition inline de la table a été retirée). Toutes les acceptations
/// config qui pilotaient l'écriture inline passent par ces helpers pour rester sur le PARCOURS RÉEL de
/// l'IHM (clic crayon → modal pré-remplie ; clic « Ajouter » → modal vide), sans doublure. L'interaction
/// est exécutée sur le dispatcher du renderer (anti-flake *TempsReel*, hub SignalR réel).
/// </summary>
internal static class ConfigActeursModalHarness
{
    /// <summary>Attend que la table des acteurs (relue depuis le store via GET HTTP réel) soit peuplée —
    /// garde déterministe avant toute interaction (les crayons ne sont rendus qu'une fois les lignes là).</summary>
    public static void AttendreLignes(IRenderedComponent<ConfigurationFoyer> config)
        => config.WaitForState(
            () => config.FindAll("[data-testid='acteur-foyer']").Count > 0,
            TimeSpan.FromSeconds(10));

    /// <summary>Nom d'affichage d'une ligne d'acteur de la table (lecture seule).</summary>
    public static string? NomLigne(IElement ligne)
        => ligne.QuerySelector(".acteur-nom")?.TextContent.Trim();

    /// <summary>La ligne (lecture seule) de l'acteur dont le nom affiché est <paramref name="nom"/>.</summary>
    public static IElement LigneParNom(IRenderedComponent<ConfigurationFoyer> config, string nom)
        => config.FindAll("[data-testid='acteur-foyer']").Single(li => NomLigne(li) == nom);

    /// <summary>Ouvre la modal d'ÉDITION sur l'acteur d'identifiant stable <paramref name="acteurId"/> :
    /// clique son crayon (sur le dispatcher) et attend l'apparition de la modal.</summary>
    public static void OuvrirEdition(Bunit.TestContext ctx, IRenderedComponent<ConfigurationFoyer> config, string acteurId)
    {
        AttendreLignes(config);
        ctx.SurDispatcher(() => config.FindAll("[data-testid='crayon-acteur']")
            .Single(b => b.GetAttribute("data-acteur-id") == acteurId)
            .Click());
        config.WaitForElement("[data-testid='dialog-acteur']", TimeSpan.FromSeconds(10));
    }

    /// <summary>Ouvre la modal d'ÉDITION sur l'acteur dont le NOM affiché est <paramref name="nom"/>.</summary>
    public static void OuvrirEditionParNom(Bunit.TestContext ctx, IRenderedComponent<ConfigurationFoyer> config, string nom)
    {
        AttendreLignes(config);
        ctx.SurDispatcher(() => LigneParNom(config, nom)
            .QuerySelector("[data-testid='crayon-acteur']")!
            .Click());
        config.WaitForElement("[data-testid='dialog-acteur']", TimeSpan.FromSeconds(10));
    }

    /// <summary>Ferme la modal acteur ouverte (bouton « Annuler ») et attend sa disparition — sans émettre
    /// aucune commande.</summary>
    public static void Fermer(Bunit.TestContext ctx, IRenderedComponent<ConfigurationFoyer> config)
    {
        ctx.SurDispatcher(() => config.Find("[data-testid='dialog-acteur-annuler']").Click());
        config.WaitForState(() => config.FindAll("[data-testid='dialog-acteur']").Count == 0, TimeSpan.FromSeconds(10));
    }

    /// <summary>Ouvre la MÊME modal en mode CRÉATION (bouton « Ajouter un acteur » au bas du tableau).</summary>
    public static void OuvrirAjout(Bunit.TestContext ctx, IRenderedComponent<ConfigurationFoyer> config)
    {
        AttendreLignes(config);
        ctx.SurDispatcher(() => config.Find("[data-testid='bouton-ajouter-acteur']").Click());
        config.WaitForElement("[data-testid='dialog-acteur']", TimeSpan.FromSeconds(10));
    }
}
