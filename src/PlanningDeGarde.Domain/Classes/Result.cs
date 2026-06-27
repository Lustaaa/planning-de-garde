namespace PlanningDeGarde.Domain;

/// <summary>
/// Convention de refus fermée : un résultat est soit un succès porteur de valeur,
/// soit un échec porteur d'un motif métier. Pas d'exception pour le refus attendu.
/// </summary>
public readonly struct Result<T>
{
    public bool EstSucces { get; }
    public T? Valeur { get; }
    public string? Motif { get; }

    private Result(bool estSucces, T? valeur, string? motif)
    {
        EstSucces = estSucces;
        Valeur = valeur;
        Motif = motif;
    }

    public static Result<T> Succes(T valeur) => new(true, valeur, null);
    public static Result<T> Echec(string motif) => new(false, default, motif);
}
