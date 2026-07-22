using System.Net.Http.Json;

namespace PlanningDeGarde.Api.Tests;

/// <summary>
/// Sprint 33 — Sc.3 (acceptation runtime) — le trou de lecture du cycle est corrigé sur l'app
/// RÉELLEMENT câblée (hôte d'API détaché, DI réelle, store de cycle réel — comme le front WASM la
/// consomme, sans aucune doublure sur le chemin observé). On DÉCLARE le cycle de fond via le canal
/// d'écriture HTTP (<c>POST /api/canal/definir-cycle</c>), puis on RELIT tous les cycles déclarés via
/// le canal de lecture HTTP (<c>GET /api/foyer/cycles</c>) : ils apparaissent tous (plus invisibles),
/// chacun identifié de façon stable (index de semaine) avec son id de responsable persisté.
/// </summary>
public sealed class CyclesFoyerLectureRuntimeTests
{
    [Fact]
    public async Task Should_Restituer_tous_les_cycles_declares_via_le_canal_de_lecture_HTTP_When_un_cycle_de_fond_a_ete_declare_via_le_canal_d_ecriture()
    {
        using var hote = new ApiHoteFactory();
        var client = hote.CreateClient();

        // Given — aucun cycle déclaré : la lecture renvoie une liste vide (pas d'erreur).
        var avant = await client.GetFromJsonAsync<List<CycleFoyerVue>>("/api/foyer/cycles");
        Assert.NotNull(avant);
        Assert.Empty(avant!);

        // When — un cycle de fond de 2 semaines est déclaré via le canal d'écriture (parent-a / parent-b).
        var definition = await client.PutAsJsonAsync("/api/foyer/cycles", new
        {
            NombreSemaines = 2,
            Affectations = new Dictionary<int, string> { [0] = "parent-a", [1] = "parent-b" },
        });
        Assert.True(definition.IsSuccessStatusCode, $"la définition du cycle doit aboutir, statut {(int)definition.StatusCode}.");

        // Then — le canal de lecture HTTP restitue TOUS les cycles déclarés (plus invisibles).
        var cycles = await client.GetFromJsonAsync<List<CycleFoyerVue>>("/api/foyer/cycles");
        Assert.NotNull(cycles);
        Assert.Equal(2, cycles!.Count);
        Assert.Contains(cycles, c => c.IndexSemaine == 0 && c.ResponsableId == "parent-a");
        Assert.Contains(cycles, c => c.IndexSemaine == 1 && c.ResponsableId == "parent-b");
    }
}
