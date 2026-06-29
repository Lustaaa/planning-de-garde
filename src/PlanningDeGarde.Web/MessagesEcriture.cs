namespace PlanningDeGarde.Web;

/// <summary>
/// Messages d'écriture partagés par les composants du front WASM (dialogs d'écriture en contexte,
/// page de configuration, page de transfert). Relocalisé depuis l'ancienne page dédiée
/// <c>PoserSlot.razor</c> (retirée au palier 7, « écriture en contexte ») pour rester accessible aux
/// composants conservés sans dépendre d'un écran supprimé.
/// </summary>
public static class MessagesEcriture
{
    /// <summary>
    /// Affiché quand l'API distante est <b>injoignable</b> (échec de transport : connexion refusée,
    /// timeout) — distinct d'un refus métier 4xx (motif propagé par le canal). La saisie n'est alors pas
    /// appliquée et reste à resoumettre : aucune navigation, aucune mise en file (PWA hors périmètre).
    /// </summary>
    public const string ServiceInjoignable =
        "Enregistrement impossible : le service est injoignable, réessayez.";

    /// <summary>
    /// Affiché quand une <b>navigation du calendrier</b> échoue parce que l'API distante est
    /// <b>injoignable</b> (Sc.6, règle 28) : la re-requête de la date naviguée n'a pas abouti. La fenêtre
    /// affichée est <b>conservée</b> (l'ancre ne diverge pas) ; la navigation n'est <b>ni mise en file ni
    /// rejouée</b> (le hors-ligne rejouable est un palier technique ultérieur, hors périmètre).
    /// </summary>
    public const string NavigationInjoignable =
        "Navigation impossible : le service est injoignable, la fenêtre affichée est conservée.";
}
