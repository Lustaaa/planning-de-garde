using System;

namespace PlanningDeGarde.Domain;

/// <summary>
/// Snapshot immuable d'un slot — frontière publique pour les assertions / la persistance.
/// <paramref name="Id"/> est l'<b>identifiant stable</b> attribué par le store à l'enregistrement
/// (clé de suppression, jamais un libellé) ; vide tant que le slot n'a pas été persisté.
/// </summary>
public sealed record SlotSnapshot(string EnfantId, string LieuId, DateTime Debut, DateTime Fin, string Id = "");

/// <summary>
/// Agrégat « où est l'enfant » (axe localisation, orthogonal à la responsabilité).
/// Sans responsable. Invariant porté : fin > début.
/// </summary>
public sealed class SlotDeLocalisation
{
    private readonly string _enfantId;
    private readonly string _lieuId;
    private readonly DateTime _debut;
    private readonly DateTime _fin;

    private SlotDeLocalisation(string enfantId, string lieuId, DateTime debut, DateTime fin)
    {
        _enfantId = enfantId;
        _lieuId = lieuId;
        _debut = debut;
        _fin = fin;
    }

    public static Result<SlotDeLocalisation> Poser(string enfantId, string lieuId, DateTime debut, DateTime fin)
    {
        if (fin <= debut)
            return Result<SlotDeLocalisation>.Echec("La durée du slot doit être strictement positive.");

        return Result<SlotDeLocalisation>.Succes(new SlotDeLocalisation(enfantId, lieuId, debut, fin));
    }

    public SlotSnapshot ToSnapshot() => new(_enfantId, _lieuId, _debut, _fin);
}
