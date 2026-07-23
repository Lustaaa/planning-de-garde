using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.Api.Tests;

/// <summary>
/// Sprint 54 — S2 (scénario limite) — <b>scope défensif de l'id d'item</b>. Depuis s54, l'activité
/// (ponctuelle ou récurrente) est une <b>sous-ressource de l'enfant</b> (<c>/api/enfants/{enfantId}/activites…</c>).
/// Cibler un DELETE par id sous un enfant qui n'en est PAS le propriétaire doit répondre <b>404</b> :
/// l'id d'item doit appartenir à l'enfant de l'URL. Le slot d'origine reste intact (aucun effet de bord).
/// L'idempotence (id absent sous le BON enfant = no-op 200) reste préservée (non testée ici : couverte
/// par SupprimerSlotIdempotente…).
/// </summary>
public sealed class ActiviteScopeDefensifEnfantApiTests
{
    private static readonly object PoseLea = new
    {
        LieuId = "école",
        Debut = new DateTime(2026, 6, 24, 8, 30, 0),
        Fin = new DateTime(2026, 6, 24, 16, 30, 0),
    };

    [Fact]
    public async Task Should_Repondre_404_et_laisser_le_slot_intact_When_on_supprime_par_id_une_activite_de_Lea_sous_un_autre_enfant()
    {
        using var hote = new ApiHoteFactory();
        var client = hote.CreateClient();

        // Given — une activité de Léa posée, dont on relit l'id stable depuis le store réel.
        var pose = await client.PostAsJsonAsync("/api/enfants/Léa/activites", PoseLea);
        Assert.True(pose.IsSuccessStatusCode, $"la pose de Léa doit aboutir, statut {(int)pose.StatusCode}.");
        var idSlotLea = Assert.Single(hote.Services.GetRequiredService<ISlotRepository>().AllSnapshots()).Id;

        // When — on cible ce même id d'activité SOUS un autre enfant (Tom) qui n'en est pas propriétaire.
        var suppression = await client.DeleteAsync($"/api/enfants/Tom/activites/{idSlotLea}");

        // Then — 404 (l'id d'item doit appartenir à l'enfant de l'URL) et le slot de Léa reste intact.
        Assert.Equal(HttpStatusCode.NotFound, suppression.StatusCode);
        Assert.Contains(hote.Services.GetRequiredService<ISlotRepository>().AllSnapshots(), s => s.Id == idSlotLea);
    }
}
