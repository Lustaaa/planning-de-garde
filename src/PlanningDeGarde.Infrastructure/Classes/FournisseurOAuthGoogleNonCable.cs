using PlanningDeGarde.Application;

namespace PlanningDeGarde.Infrastructure;

/// <summary>
/// Réalisation DI du port <see cref="IFournisseurOAuth"/> (s28, volet 3) rendant
/// <c>ConnexionOAuthHandler</c> RÉSOLVABLE et le callback OAuth routable — <b>SANS</b> réaliser l'échange
/// OAuth Google réel (client_secret / token endpoint / résolution de l'email vérifié). C'est une
/// <b>DETTE DE CÂBLAGE assumée</b> (backlog P0, vérifiée MANUELLEMENT au G3, non testable en runtime
/// local) : <see cref="ResoudreIdentite"/> renvoie <c>null</c> tant que l'adaptateur Google réel n'est
/// pas branché — un vrai callback est donc refusé (aucune identité résolue). La LOGIQUE de rapprochement
/// compte local ↔ identité externe est prouvée par doublure à la frontière Application (s28 S9, s25 Sc.14/15).
/// </summary>
public sealed class FournisseurOAuthGoogleNonCable : IFournisseurOAuth
{
    public IdentiteExterne? ResoudreIdentite(string callback) => null;
}
