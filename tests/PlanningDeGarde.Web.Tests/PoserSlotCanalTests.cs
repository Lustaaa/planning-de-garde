using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Acceptation (intégration de bout en bout) du scénario 1 « poser un slot via le canal
/// d'écriture le rend visible dans sa case ». Hôte Web réel (<see cref="WebApplicationFactory{T}"/>),
/// store réel singleton, projection réelle <see cref="GrilleAgendaQuery"/> : aucune doublure
/// sur le chemin observé (anti « vert qui ment »). Le driver est l'endpoint HTTP du canal
/// requête/réponse, l'observable est la grille projetée à la semaine de référence.
/// Un hôte neuf par test (store singleton frais) → isolation des écritures.
/// </summary>
public sealed class PoserSlotCanalTests
{
    // Mercredi 24/06/2026, école, 08:30 → 16:30 (commande de pose de Léa).
    private static readonly object CommandePoseLea = new
    {
        EnfantId = "Léa",
        LieuId = "école",
        Debut = new DateTime(2026, 6, 24, 8, 30, 0),
        Fin = new DateTime(2026, 6, 24, 16, 30, 0),
    };

    [Fact]
    public async Task Should_Confirmer_la_pose_par_une_reponse_de_succes_When_la_commande_de_pose_d_un_slot_valide_est_emise_via_le_canal_requete_reponse()
    {
        using var hote = new WebApplicationFactory<Program>();
        var client = hote.CreateClient();

        var reponse = await client.PostAsJsonAsync("/api/canal/poser-slot", CommandePoseLea);

        Assert.True(reponse.IsSuccessStatusCode, $"statut HTTP attendu de succès, obtenu {(int)reponse.StatusCode}.");
    }

    [Fact]
    public async Task Should_Faire_apparaitre_le_slot_ecole_08h30_16h30_dans_la_case_du_mercredi_24_06_2026_de_la_projection_reelle_When_la_pose_de_Lea_a_abouti_via_le_canal()
    {
        using var hote = new WebApplicationFactory<Program>();
        var client = hote.CreateClient();

        var reponse = await client.PostAsJsonAsync("/api/canal/poser-slot", CommandePoseLea);
        Assert.True(reponse.IsSuccessStatusCode, $"la pose via le canal doit aboutir, statut {(int)reponse.StatusCode}.");

        // Observable de bout en bout : la projection réelle lit le store réel singleton de l'hôte
        // (aucune doublure sur le chemin observé). Date de référence injectée = lundi 22/06/2026.
        using var scope = hote.Services.CreateScope();
        var projection = scope.ServiceProvider.GetRequiredService<GrilleAgendaQuery>();
        var grille = projection.Projeter(new DateOnly(2026, 6, 22));

        var caseMercredi = grille.Jours.Single(j => j.Date == new DateOnly(2026, 6, 24));
        var slot = Assert.Single(caseMercredi.Slots, s => s.Libelle == "école");
        Assert.Equal(new TimeOnly(8, 30), slot.Debut);
        Assert.Equal(new TimeOnly(16, 30), slot.Fin);
    }
}
