namespace PlanningDeGarde.Application.Comptes.Ports;

/// <summary>
/// Port de droite d'un <b>fournisseur OAuth externe</b> (Google / Microsoft / Apple, volet 4 s25) :
/// résout le retour de callback du provider en une <b>identité externe</b> (au minimum l'email vérifié
/// par le provider). Réalisé par un adaptateur concret en runtime (secrets, redirections, callbacks —
/// <b>non testable en runtime local</b>, vérifié MANUELLEMENT au G3) et par une doublure dans les tests
/// (preuve de la logique Application/frontière). L'Application n'en dépend que par ce port : elle ne
/// connaît ni les secrets, ni le protocole OAuth ; elle reçoit une identité externe déjà résolue.
/// </summary>
public interface IFournisseurOAuth
{
    /// <summary>Résout le retour de callback du provider en une identité externe (email vérifié), ou
    /// <c>null</c> si le callback ne restitue aucune identité exploitable.</summary>
    IdentiteExterne? ResoudreIdentite(string callback);
}

/// <summary>Une identité externe restituée par un fournisseur OAuth : l'email vérifié par le provider,
/// qui sert de clé de résolution vers un compte du foyer (même résolution que la connexion locale s23).</summary>
public sealed record IdentiteExterne(string Email);
