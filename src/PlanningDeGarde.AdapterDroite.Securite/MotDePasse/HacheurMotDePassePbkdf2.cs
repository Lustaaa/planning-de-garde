using System;
using System.Security.Cryptography;
using PlanningDeGarde.Application.Comptes.Ports;

namespace PlanningDeGarde.AdapterDroite.Securite;

/// <summary>
/// Adaptateur de droite RÉEL du facteur mot de passe : réalise
/// <see cref="IHacheurMotDePasse"/> par <b>PBKDF2</b> (SHA-256) avec sel aléatoire par mot de passe.
/// Le condensat est auto-descriptif — <c>iterations.selBase64.hashBase64</c> — de sorte que la
/// vérification n'a besoin d'aucun paramètre externe (le sel et le coût sont relus du condensat).
/// Le mot de passe en clair n'est jamais stocké ni retourné.
/// </summary>
public sealed class HacheurMotDePassePbkdf2 : IHacheurMotDePasse
{
    private const int Iterations = 100_000;
    private const int TailleSel = 16;   // octets
    private const int TailleHash = 32;  // octets (SHA-256)

    public string Hacher(string motDePasse)
    {
        var sel = RandomNumberGenerator.GetBytes(TailleSel);
        var hash = Deriver(motDePasse, sel, Iterations);
        return $"{Iterations}.{Convert.ToBase64String(sel)}.{Convert.ToBase64String(hash)}";
    }

    public bool Verifier(string motDePasse, string condensat)
    {
        var parties = condensat.Split('.');
        if (parties.Length != 3)
            return false;

        var iterations = int.Parse(parties[0]);
        var sel = Convert.FromBase64String(parties[1]);
        var attendu = Convert.FromBase64String(parties[2]);
        var candidat = Deriver(motDePasse, sel, iterations);

        // Comparaison à temps constant : aucune fuite temporelle sur la position du 1er octet divergent.
        return CryptographicOperations.FixedTimeEquals(candidat, attendu);
    }

    private static byte[] Deriver(string motDePasse, byte[] sel, int iterations)
        => Rfc2898DeriveBytes.Pbkdf2(motDePasse, sel, iterations, HashAlgorithmName.SHA256, TailleHash);
}
