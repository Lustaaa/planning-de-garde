using System.Collections.Generic;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application.Notifications.Ports;

/// <summary>
/// Port du JOURNAL DE CHANGEMENTS (cloche s47) : journal append-only alimenté par les handlers d'écriture
/// existants (délégation s44 / plage s45 / reprise s46 / transfert s31). TRACE DE LECTURE horodatée — il
/// n'est JAMAIS lu par la résolution (la vérité reste les périodes/transferts, surcharge &gt; fond). Donnée
/// derrière un port (2 adaptateurs InMemory + Mongo), jamais figée dans le code.
/// </summary>
public interface IJournalChangements
{
    /// <summary>Consigne un événement (append-only). L'horodatage porté par l'événement fixe la récence.</summary>
    void Consigner(EvenementChangementSnapshot evenement);

    /// <summary>Tous les événements consignés (non filtrés). Le tri / filtrage par utilisateur est un concern de lecture (query).</summary>
    IReadOnlyList<EvenementChangementSnapshot> Tout();
}
