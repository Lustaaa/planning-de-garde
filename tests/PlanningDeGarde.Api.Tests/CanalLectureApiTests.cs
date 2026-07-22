using System.Net.Http.Json;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.Api.Tests;

/// <summary>
/// Acceptation (intégration de bout en bout) du <b>canal de lecture</b> porté sur l'hôte d'API
/// détaché : la grille agenda projetée est lue à distance en HTTP (<c>GET /api/grille/…</c>),
/// exactement comme le front WASM la consomme (le navigateur n'a pas la projection en DI directe).
/// Driver : on écrit via le canal d'écriture, puis on relit la grille via le canal de lecture HTTP
/// et on observe le slot dans sa case — l'écriture a réellement transité jusqu'au store et la
/// lecture HTTP le restitue. Aucune doublure sur le chemin observé.
/// </summary>
public sealed class CanalLectureApiTests
{
    private static readonly object CommandePoseLea = new
    {
        EnfantId = "Léa",
        LieuId = "école",
        Debut = new DateTime(2026, 6, 24, 8, 30, 0),
        Fin = new DateTime(2026, 6, 24, 16, 30, 0),
    };

    [Fact]
    public async Task Should_Restituer_la_grille_avec_le_slot_ecole_du_mercredi_24_06_2026_via_le_canal_de_lecture_HTTP_When_un_slot_a_ete_pose_via_le_canal_d_ecriture()
    {
        using var hote = new ApiHoteFactory();
        var client = hote.CreateClient();

        // Given/When — pose émise via le canal d'écriture de l'API distante.
        var pose = await client.PostAsJsonAsync("/api/slots", CommandePoseLea);
        Assert.True(pose.IsSuccessStatusCode, $"la pose doit aboutir, statut {(int)pose.StatusCode}.");

        // Then — la grille relue via le canal de LECTURE HTTP (comme le front WASM) porte le slot.
        var grille = await client.GetFromJsonAsync<GrilleAgenda>("/api/grille/2026/6/22");
        Assert.NotNull(grille);

        var caseMercredi = grille!.Jours.Single(j => j.Date == new DateOnly(2026, 6, 24));
        var slot = Assert.Single(caseMercredi.Slots, s => s.Libelle == "école");
        Assert.Equal(new TimeOnly(8, 30), slot.Debut);
        Assert.Equal(new TimeOnly(16, 30), slot.Fin);
    }
}
