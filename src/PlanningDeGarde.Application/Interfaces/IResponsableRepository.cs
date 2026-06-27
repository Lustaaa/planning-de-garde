namespace PlanningDeGarde.Application;

/// <summary>Port répondant à la capacité « ce responsable existe-t-il dans le foyer ? ».</summary>
public interface IResponsableRepository
{
    bool Existe(string responsableId);
}
