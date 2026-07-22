namespace PlanningDeGarde.AdapterDroite.Mongo.CyclesDeFond.DbModels;

/// <summary>Une affectation du cycle : index de semaine (0..N-1) → identifiant stable de responsable.</summary>
internal sealed class AffectationDocument
{
    public int Index { get; set; }
    public string ResponsableId { get; set; } = default!;
}
