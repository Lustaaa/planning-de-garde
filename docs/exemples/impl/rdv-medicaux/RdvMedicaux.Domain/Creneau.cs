using System;

namespace RdvMedicaux.Domain;

public enum EtatCreneau { Libre, Reserve, Indisponible, Passe }

public class Creneau
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string PraticienId { get; init; } = string.Empty;
    public DateTime Debut { get; init; }
    public DateTime Fin { get; init; }
    public EtatCreneau Etat { get; private set; } = EtatCreneau.Libre;
}
