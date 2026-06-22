using System;

namespace RdvMedicaux.Domain;

public enum EtatPlage { Active, Inactive }

public class Plage
{
    public string PraticienId { get; init; } = string.Empty;
    public DateTime Debut { get; init; }
    public DateTime Fin { get; init; }
    public EtatPlage Etat { get; private set; } = EtatPlage.Active;
}
