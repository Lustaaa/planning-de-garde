using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Tests.Fakes;

/// <summary>
/// Doublure à la main du port du cycle de fond. Seedée au constructeur avec un cycle courant
/// (ou aucun) ; la dernière définition écrase le cycle (dernière écriture gagne, sans version).
/// </summary>
public sealed class FakeReferentielCycleDeFond : IReferentielCycleDeFond
{
    // Cycle partagé (clé "") + surcharges par enfant (s53) : un enfant sans cycle propre retombe sur le partagé.
    private readonly System.Collections.Generic.Dictionary<string, CycleDeFond> _cycles = new();

    public FakeReferentielCycleDeFond(CycleDeFond? cycle = null)
    {
        if (cycle is not null)
            _cycles[""] = cycle;
    }

    /// <summary>Seedé avec un cycle POUR UN OU PLUSIEURS enfants (s53) : depuis le gate G3 4e passage, la
    /// résolution d'un enfant ne retombe plus sur le bucket "" — un cycle doit être seedé pour CHAQUE enfant à
    /// résoudre. Miroir du foyer réel « chaque enfant a son cycle propre ».</summary>
    public FakeReferentielCycleDeFond(CycleDeFond cycle, params string[] enfantIds)
    {
        // Seedé pour l'enfant listé ET le bucket "" (confort des assertions à projection legacy null des tests).
        // L'isolation reste garantie par CycleCourant SANS repli : un enfant NON listé résout NEUTRE (pas de "").
        _cycles[""] = cycle;
        foreach (var id in enfantIds)
            _cycles[id] = cycle;
    }

    // ISOLATION STRICTE s53 (gate G3 4e passage) : miroir EXACT des adaptateurs réels — un enfant NON-NULL ne
    // voit QUE son cycle (aucun repli sur le bucket partagé ""). Enfant sans cycle propre → null → NEUTRE.
    public CycleDeFond? CycleCourant(string? enfantId = null)
        => _cycles.TryGetValue(enfantId ?? "", out var cycle) ? cycle : null;

    public void DefinirCycle(CycleDeFond cycle, string? enfantId = null) => _cycles[enfantId ?? ""] = cycle;
}
