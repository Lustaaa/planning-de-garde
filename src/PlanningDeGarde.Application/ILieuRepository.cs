namespace PlanningDeGarde.Application;

/// <summary>Port répondant à la capacité « ce lieu existe-t-il dans le foyer ? ».</summary>
public interface ILieuRepository
{
    bool Existe(string lieuId);
}
