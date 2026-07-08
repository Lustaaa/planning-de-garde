using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;

namespace PlanningDeGarde.Api.Tests;

/// <summary>
/// Sprint 28 — S1 — Reset mot de passe, volet 1 (@back, <b>preuve runtime réelle</b>). L'adaptateur
/// SMTP concret (<c>EnvoiMailSmtp</c>) est câblé en DI derrière le port <see cref="IEnvoiMail"/> et
/// remplace la doublure s25 : une demande de récupération émise via le canal d'écriture
/// (<c>POST /api/canal/demander-recuperation</c>) doit provoquer la remise d'un <b>vrai mail</b> capté
/// par le serveur SMTP de développement (Smtp4dev, Docker), adressé au compte et porteur d'un jeton de
/// réinitialisation — tandis que la <b>réponse au client reste NEUTRE</b> (aucun jeton, aucun indice
/// d'existence : anti-énumération). Aucune doublure sur le chemin observé (le canal mail est réel).
/// <b>Skip propre</b> si Smtp4dev est injoignable.
/// </summary>
[Collection("Smtp4dev")]
public sealed class DemanderRecuperationSmtpApiTests
{
    private const string Email = "papa@foyer.fr";

    /// <summary>Hôte API détaché câblé sur le serveur SMTP de dev (adaptateur SMTP concret réel).</summary>
    private sealed class HoteSmtp : WebApplicationFactory<ApiProgram>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("Mail:Smtp:Hote", SmtpDev.SmtpHote);
            builder.UseSetting("Mail:Smtp:Port", SmtpDev.SmtpPort.ToString());
        }
    }

    [SmtpDevRequisFact]
    public async Task Should_Capter_un_vrai_mail_au_compte_porteur_d_un_jeton_tout_en_repondant_neutre_When_une_demande_de_recuperation_est_emise_via_le_canal_sur_l_adaptateur_SMTP_reel()
    {
        await SmtpDev.ViderMessages();

        using var hote = new HoteSmtp();
        var client = hote.CreateClient();

        // Un compte Actif porte l'email visé (seedé dans le store réel singleton de l'hôte).
        hote.Services.GetRequiredService<ReferentielComptesEnMemoire>()
            .Creer("compte-papa", Email, StatutCompte.Actif, "acteur-papa");

        // When — la demande de récupération est émise via le canal d'écriture.
        var reponse = await client.PostAsJsonAsync("/api/canal/demander-recuperation", new { Email });

        // Then #3 — réponse NEUTRE : succès sec, sans jeton ni indice d'existence dans le corps.
        Assert.True(reponse.IsSuccessStatusCode, $"statut de succès attendu, obtenu {(int)reponse.StatusCode}.");
        var corps = await reponse.Content.ReadAsStringAsync();
        Assert.DoesNotContain("reset-", corps);

        // Then #1 & #2 — un VRAI mail est capté par Smtp4dev, adressé au compte, porteur d'un jeton.
        var source = await SmtpDev.TrouverSourceMailPour(Email);
        Assert.NotNull(source);
        Assert.Contains("reset-", source); // jeton de réinitialisation généré côté serveur (usage unique)
    }
}
