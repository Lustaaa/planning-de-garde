using System;

namespace PlanningDeGarde.Domain;

/// <summary>
/// Snapshot immuable d'une période de garde — frontière publique pour assertions / persistance.
/// <paramref name="Id"/> est l'<b>identifiant stable</b> attribué par le store à l'enregistrement
/// (clé de suppression / d'édition, jamais un libellé) ; vide tant que la période n'a pas été persistée.
/// </summary>
public sealed record PeriodeSnapshot(string ResponsableId, DateTime Debut, DateTime Fin, string Id = "");

/// <summary>
/// Agrégat « qui est responsable » (axe responsabilité, orthogonal à la localisation).
/// Invariants : exactement un responsable (Id non vide), fin > début. Bornes paramétrables.
/// </summary>
public sealed class PeriodeDeGarde
{
    private readonly string _responsableId;
    private readonly DateTime _debut;
    private readonly DateTime _fin;

    private PeriodeDeGarde(string responsableId, DateTime debut, DateTime fin)
    {
        _responsableId = responsableId;
        _debut = debut;
        _fin = fin;
    }

    public static Result<PeriodeDeGarde> Affecter(string responsableId, DateTime debut, DateTime fin)
    {
        if (string.IsNullOrWhiteSpace(responsableId))
            return Result<PeriodeDeGarde>.Echec("Un responsable est requis pour la période de garde.");

        return Result<PeriodeDeGarde>.Succes(new PeriodeDeGarde(responsableId, debut, fin));
    }

    public PeriodeSnapshot ToSnapshot() => new(_responsableId, _debut, _fin);
}
