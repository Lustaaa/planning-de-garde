namespace PlanningDeGarde.Application;

/// <summary>
/// Résout, côté serveur, l'<b>acteur par défaut</b> à pré-positionner dans le sélecteur d'acteur
/// (config / dialogs d'écriture). Couplage « acteur par défaut = utilisateur connecté » (retour URGENT
/// #2) : quand une <see cref="SessionOuverte"/> est présente, le défaut = l'acteur lié 1-1 au compte
/// connecté (l'identité réelle de la session, relation s22). L'acteur ainsi résolu est un acteur réel du
/// foyer, exposé au sélecteur via la source unique <see cref="IEnumerationActeursFoyer"/> (convergence
/// s20). Le comportement NON connecté (repli sur le défaut actuel) relève du Sc.6.
/// </summary>
public sealed class ResoudreActeurParDefautQuery
{
    /// <summary>Acteur par défaut d'une session ouverte = l'acteur lié 1-1 au compte connecté (identité
    /// réelle de la session).</summary>
    public string Resoudre(SessionOuverte session) => session.IdentiteReelle;
}
