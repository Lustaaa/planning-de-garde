using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.Api.Tests;

/// <summary>
/// Sprint 29 — Câblage DI runtime du slot récurrent (prérequis IHM) : sur l'hôte d'API RÉEL (DI réelle,
/// store réel InMemory), poser un slot récurrent via le CANAL D'ÉCRITURE (endpoint
/// <c>POST /api/canal/poser-slot-recurrent</c>) doit aboutir, puis ses occurrences doivent apparaître sur
/// CHAQUE case du bon jour de la grille projetée réellement (<c>GET /api/grille/…</c>) — preuve que
/// <see cref="ISlotRecurrentRepository"/> est bien enregistré ET injecté dans
/// <see cref="GrilleAgendaQuery"/>. Aucun accès direct au store : tout transite par le câblage réel.
/// </summary>
public sealed class SlotRecurrentCanalEtGrilleRuntimeTests
{
    private sealed class HoteApi : WebApplicationFactory<ApiProgram>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.UseEnvironment("Testing");
    }

    [Fact]
    public async Task Should_Materialiser_les_occurrences_du_slot_recurrent_sur_chaque_samedi_de_la_grille_When_il_est_pose_via_le_canal_ecriture_reel()
    {
        using var hote = new HoteApi();
        var client = hote.CreateClient();

        // Given — le lieu « piscine » ET l'enfant « lea » existent dans le référentiel du foyer (store réel).
        // La pose valide désormais l'existence de l'enfant (s30 S7) : on l'établit comme le lieu.
        hote.Services.GetRequiredService<IEditeurActivites>().Ajouter("piscine", "piscine");
        hote.Services.GetRequiredService<IEditeurEnfants>().Ajouter("lea", "lea");

        // When — un Parent pose un slot récurrent le samedi de 11h30 à 12h15 au lieu « piscine ».
        var pose = await client.PostAsJsonAsync("/api/canal/poser-slot-recurrent", new
        {
            EnfantId = "lea",
            LieuId = "piscine",
            JourDeSemaine = DayOfWeek.Saturday,
            HeureDebut = TimeSpan.FromHours(11.5),
            HeureFin = new TimeSpan(12, 15, 0),
        });
        Assert.True(pose.IsSuccessStatusCode, $"la pose du slot récurrent doit aboutir, statut {(int)pose.StatusCode}.");

        // Then — la grille projetée (fenêtre 4 semaines dès le 24/06/2026) porte l'occurrence sur chaque samedi.
        var grille = await client.GetFromJsonAsync<GrilleAgenda>("/api/grille/2026/6/24");
        Assert.NotNull(grille);
        var samedis = new[] { new DateOnly(2026, 6, 27), new DateOnly(2026, 7, 4), new DateOnly(2026, 7, 11), new DateOnly(2026, 7, 18) };
        foreach (var samedi in samedis)
        {
            var caseSamedi = grille!.Jours.Single(j => j.Date == samedi);
            var slot = Assert.Single(caseSamedi.Slots, s => s.Libelle == "piscine");
            Assert.Equal(new TimeOnly(11, 30), slot.Debut);
            Assert.Equal(new TimeOnly(12, 15), slot.Fin);
        }

        // And — aucune occurrence hors des samedis (le filtre jour-de-semaine tient au runtime).
        Assert.DoesNotContain(
            grille!.Jours.Where(j => j.Date.DayOfWeek != DayOfWeek.Saturday).SelectMany(j => j.Slots),
            s => s.Libelle == "piscine");
    }
}
