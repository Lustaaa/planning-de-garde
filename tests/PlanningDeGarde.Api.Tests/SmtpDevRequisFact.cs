using System.Net.Http.Json;
using System.Text.Json;

namespace PlanningDeGarde.Api.Tests;

/// <summary>
/// Accès au <b>serveur SMTP de développement Smtp4dev</b> (conteneur Docker) pour les acceptations
/// runtime du reset mot de passe (s28, volet 1) : le port <c>2525</c> reçoit les mails émis par
/// l'adaptateur SMTP concret, l'<b>API HTTP</b> (port <c>5081</c>) permet de les relire — c'est la
/// preuve RÉELLE (aucune doublure du canal mail : R4, rempart anti « vert-qui-ment »). Skip propre
/// (<see cref="SmtpDevRequisFactAttribute"/>) si Smtp4dev est injoignable (Docker non démarré),
/// jamais un faux vert — miroir de <c>MongoRequisFact</c>.
/// </summary>
internal static class SmtpDev
{
    public const string SmtpHote = "localhost";
    public const int SmtpPort = 2525;
    public const string ApiBase = "http://localhost:5081";

    /// <summary>Vide la boîte du serveur de dev (isolation entre tests).</summary>
    public static async Task ViderMessages()
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        await http.DeleteAsync($"{ApiBase}/api/Messages/*");
    }

    /// <summary>
    /// Cherche (avec quelques relances, la remise étant asynchrone) un message capté adressé au
    /// destinataire donné et renvoie sa source brute (corps + en-têtes), ou <c>null</c> si aucun.
    /// </summary>
    public static async Task<string?> TrouverSourceMailPour(string destinataire)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        for (var essai = 0; essai < 20; essai++)
        {
            using var doc = JsonDocument.Parse(await http.GetStringAsync($"{ApiBase}/api/Messages?pageSize=50"));
            foreach (var msg in doc.RootElement.GetProperty("results").EnumerateArray())
            {
                var pourCeDestinataire = msg.GetProperty("to").EnumerateArray()
                    .Any(t => string.Equals(t.GetString(), destinataire, StringComparison.OrdinalIgnoreCase));
                if (pourCeDestinataire)
                {
                    var id = msg.GetProperty("id").GetString();
                    return await http.GetStringAsync($"{ApiBase}/api/Messages/{id}/source");
                }
            }
            await Task.Delay(150);
        }
        return null;
    }

    /// <summary>Skip propre : vrai (avec motif) si Smtp4dev est injoignable via son API HTTP.</summary>
    public static bool Indisponible(out string motif)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
            var reponse = http.GetAsync($"{ApiBase}/api/Server").GetAwaiter().GetResult();
            reponse.EnsureSuccessStatusCode();
            motif = string.Empty;
            return false;
        }
        catch (Exception ex)
        {
            motif = $"Smtp4dev indisponible (Docker non démarré ? `docker compose up -d smtp4dev`) — acceptation reset mail ignorée : {ex.Message}";
            return true;
        }
    }
}

/// <summary>
/// Collection xunit des tests SMTP : ils partagent l'UNIQUE boîte du serveur Smtp4dev et la vident
/// (<see cref="SmtpDev.ViderMessages"/>) — les faire tourner EN PARALLÈLE se contamine (un vidage
/// efface le mail que l'autre attend). Cette collection les <b>sérialise</b> entre eux, garantissant
/// l'isolation de la boîte partagée (les autres classes Api.Tests ne touchent pas au SMTP).
/// </summary>
[CollectionDefinition("Smtp4dev")]
public sealed class Smtp4devCollection { }

/// <summary>
/// Fait conditionné à la disponibilité du serveur SMTP de dev (Smtp4dev, Docker) : pose
/// <see cref="FactAttribute.Skip"/> à la découverte si le serveur est injoignable — <b>skip propre</b>,
/// jamais un faux vert (miroir de <c>MongoRequisFactAttribute</c>).
/// </summary>
public sealed class SmtpDevRequisFactAttribute : FactAttribute
{
    public SmtpDevRequisFactAttribute()
    {
        if (SmtpDev.Indisponible(out var motif))
            Skip = motif;
    }
}
