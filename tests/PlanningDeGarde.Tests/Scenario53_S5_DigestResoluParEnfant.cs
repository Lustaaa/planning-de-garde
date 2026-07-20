using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Infrastructure;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 53 — Sc.5 — Digest « qui récupère ce soir » résolu PAR enfant (@back)
//   Étant donné deux enfants "Léa" et "Tom" avec des responsables du jour distincts
//   Quand je compose le digest immédiat s50 pour "Léa" puis pour "Tom"
//   Alors chaque digest restitue le responsable résolu (surcharge>fond>neutre) de SON enfant
//   Et les « transferts à venir » de chaque digest ne portent que sur SON enfant
//   Et la query reste PURE (aucun store neuf, aucune mutation)
public class Scenario53_S5_DigestResoluParEnfant
{
    private const string LeaId = "enfant-lea";
    private const string TomId = "enfant-tom";
    private static readonly DateOnly Aujourdhui = new(2026, 7, 8); // mercredi, ISO 28 → index 0 → fond alice (les deux)

    [Fact]
    public void Acceptation_InMemory_Digest_resolu_par_enfant()
    {
        var config = new ConfigurationFoyerEnMemoire();
        var alice = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Alice")).Valeur!.ActeurId;
        var bob = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Bob")).Valeur!.ActeurId;
        var carla = new AjouterActeurHandler(config).Handle(new AjouterActeurCommand("Carla")).Valeur!.ActeurId;

        var cycle = new CycleDeFondEnMemoire();
        var cycleDef = new CycleDeFond(2, new Dictionary<int, string> { [0] = alice, [1] = bob }); cycle.DefinirCycle(cycleDef, "enfant-lea"); cycle.DefinirCycle(cycleDef, "enfant-tom");

        var periodes = new InMemoryPeriodeRepository();
        var grille = new GrilleAgendaQuery(
            new InMemorySlotRepository(), periodes, config, config,
            cycle, config, new InMemorySlotRecurrentRepository(), new InMemoryTransfertRepository());

        // Given — Léa surchargée aujourd'hui (Carla) ; Tom résolu par le fond (Alice). Responsables distincts.
        periodes.Enregistrer(PeriodeDeGarde.Affecter(
            carla, Aujourdhui.ToDateTime(TimeOnly.MinValue), Aujourdhui.ToDateTime(TimeOnly.MinValue), LeaId).Valeur!);

        var query = new DigestImmediatQuery(grille);
        var avant = periodes.AllSnapshots().Count;

        // When — composer le digest pour Léa puis pour Tom.
        var digestLea = query.Composer(Aujourdhui, Aujourdhui, LeaId);
        var digestTom = query.Composer(Aujourdhui, Aujourdhui, TomId);

        // Then — chacun restitue le responsable résolu de SON enfant.
        Assert.Equal(carla, digestLea.Immediat!.Responsable.ActeurId);
        Assert.Equal("Carla", digestLea.Immediat.Responsable.Nom);
        Assert.Equal(alice, digestTom.Immediat!.Responsable.ActeurId);

        // Then — « à venir » : Léa porte la bascule dérivée de SA surcharge (Carla → Alice le lendemain) ; le
        // digest de Tom ne reflète JAMAIS la surcharge de Léa (aucun transfert vers/depuis Carla) — il ne porte
        // que les bascules de SON propre cycle de fond.
        Assert.Contains(digestLea.AVenir, j => j.Transfert!.CedantNom == "Carla" && j.Transfert.RecevantNom == "Alice");
        Assert.DoesNotContain(digestTom.AVenir, j => j.Transfert!.CedantNom == "Carla" || j.Transfert.RecevantNom == "Carla");

        // Then — query PURE : aucune mutation du store.
        Assert.Equal(avant, periodes.AllSnapshots().Count);
    }
}
