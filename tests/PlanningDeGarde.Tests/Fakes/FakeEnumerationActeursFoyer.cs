using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.Tests.Fakes;

/// <summary>
/// Doublure à la main du port d'énumération des acteurs du foyer (<see cref="IEnumerationActeursFoyer"/>).
/// Sert le <b>contrat d'existence</b> à la query : un identifiant présent dans le set fourni existe ;
/// tout autre identifiant (acteur supprimé) est <b>orphelin</b>. Le set explicite passé au constructeur
/// modélise l'état du store après une suppression (l'acteur retiré n'y figure plus).
/// </summary>
public sealed class FakeEnumerationActeursFoyer : IEnumerationActeursFoyer
{
    private readonly IReadOnlyCollection<string> _acteurs;

    public FakeEnumerationActeursFoyer(params string[] acteurs)
    {
        _acteurs = acteurs.ToList();
    }

    public IReadOnlyCollection<string> EnumererActeurs() => _acteurs;
}
