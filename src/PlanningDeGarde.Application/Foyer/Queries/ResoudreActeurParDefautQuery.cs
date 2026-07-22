namespace PlanningDeGarde.Application.Foyer.Queries;

/// <summary>
/// Résout, côté serveur, l'<b>acteur par défaut</b> à pré-positionner dans le sélecteur d'acteur
/// (config / dialogs d'écriture). Couplage « acteur par défaut = utilisateur connecté »
/// : quand une <see cref="SessionOuverte"/> est présente, le défaut = l'acteur lié 1-1 au compte
/// connecté (l'identité réelle de la session, relation). L'acteur ainsi résolu est un acteur réel du
/// foyer, exposé au sélecteur via la source unique <see cref="IEnumerationActeursFoyer"/>
/// (convergence). Le comportement NON connecté relève du repli sur le défaut actuel.
/// </summary>
public sealed class ResoudreActeurParDefautQuery
{
    /// <summary>Acteur par défaut : quand une <see cref="SessionOuverte"/> est présente, = l'acteur lié
    /// 1-1 au compte connecté (identité réelle de la session). Après un <b>logout</b> (session détruite =
    /// absence de session côté serveur, <c>null</c>), le défaut retombe sur le comportement NON connecté —
    /// AUCUNE identité résiduelle du compte déconnecté.</summary>
    public string? Resoudre(SessionOuverte? session) => session?.IdentiteReelle;
}
