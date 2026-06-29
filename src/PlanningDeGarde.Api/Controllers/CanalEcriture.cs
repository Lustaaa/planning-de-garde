using PlanningDeGarde.Application;

namespace PlanningDeGarde.Api;

/// <summary>
/// Adaptateur de gauche : le canal requête/réponse d'écriture du planning, porté sur l'hôte
/// d'API détaché (<see cref="ApiProgram"/>). Expose les commandes d'écriture comme endpoints
/// HTTP qui invoquent les handlers <b>inchangés</b> (Application/write) et renvoient un accusé
/// succès/échec. Le front (WASM, agent tiers) émet ses commandes ici à distance plutôt qu'en
/// appelant les handlers en direct. N'écrit jamais par le canal de diffusion (lecture seule).
/// </summary>
public static class CanalEcriture
{
    /// <summary>Corps de la requête de pose de slot émise via le canal requête/réponse.</summary>
    public sealed record PoserSlotRequete(string EnfantId, string LieuId, DateTime Debut, DateTime Fin);

    /// <summary>Corps de la réponse de succès de la pose de slot : porte l'<b>avertissement de
    /// chevauchement</b> (règle 16, accepté + averti) comme attribut de l'outcome de la commande
    /// (CQRS — distinct de la diffusion SignalR et de la lecture <c>GrilleAgendaQuery</c>). L'avertissement
    /// provient du read model EXISTANT <c>JourneeEnfantQuery</c> (vert s01) : aucune règle ni recalcul neuf.</summary>
    public sealed record PoserSlotReponse(bool Chevauchement);

    /// <summary>Corps de la requête d'affectation de période émise via le canal requête/réponse.</summary>
    public sealed record AffecterPeriodeRequete(string ResponsableId, DateTime Debut, DateTime Fin);

    /// <summary>Corps de la requête de définition d'un transfert de bascule émise via le canal.</summary>
    public sealed record DefinirTransfertRequete(string DeposeParId, string RecupereParId, string LieuId, TimeSpan Heure, DateTime Date);

    /// <summary>Corps de la requête d'édition d'un acteur émise via le canal d'écriture. Le nom et la
    /// couleur sont deux champs optionnels et indépendants : un champ absent (null) n'est pas appliqué
    /// (renommage seul au Sc.1, recoloriage seul au Sc.2). L'identifiant stable n'est jamais éditable.</summary>
    public sealed record EditerActeurRequete(string ActeurId, string? Nom = null, string? Couleur = null);

    /// <summary>Corps de la requête d'ajout d'un acteur neuf au foyer émise via le canal d'écriture.
    /// Le handler génère l'identifiant stable neuf opaque (jamais fourni par le front). La couleur est
    /// optionnelle (absente → repli neutre par contrat de palette, Sc.5).</summary>
    public sealed record AjouterActeurRequete(string Nom, string? Couleur = null);

    /// <summary>Corps de la requête de suppression d'un acteur du foyer émise via le canal d'écriture.
    /// L'identifiant stable opaque est la clé (jamais le libellé). La suppression est autorisée sans
    /// condition de références et idempotente (id absent / déjà supprimé = succès sans effet, Sc.5).</summary>
    public sealed record SupprimerActeurRequete(string ActeurId);

    /// <summary>Corps de la requête de définition / ré-édition du cycle de fond (palier 6) émise via le
    /// canal d'écriture : le nombre de semaines + le mapping index→responsable (identifiant stable, jamais
    /// le libellé). Une nouvelle définition remplace intégralement le cycle courant (dernière écriture gagne).</summary>
    public sealed record DefinirCycleRequete(int NombreSemaines, IReadOnlyDictionary<int, string> Affectations);

    /// <summary>Corps de la requête de suppression d'une période émise via le canal d'écriture. La clé est
    /// l'<b>identifiant stable</b> de la période (jamais un libellé) ; la suppression est idempotente côté
    /// handler (id absent / déjà supprimé = no-op qui réussit).</summary>
    public sealed record SupprimerPeriodeRequete(string PeriodeId);

    public static IEndpointRouteBuilder MapperCanalEcriture(this IEndpointRouteBuilder routes)
    {
        routes.MapPost("/api/canal/poser-slot", (PoserSlotRequete requete, PoserSlotHandler handler, JourneeEnfantQuery journee) =>
        {
            var resultat = handler.Handle(new PoserSlotCommand(
                requete.EnfantId, requete.LieuId, requete.Debut, requete.Fin));

            if (!resultat.EstSucces)
                return Results.BadRequest(resultat.Motif);

            // Succès acquitté. La pose chevauchante est ACCEPTÉE (règle 16) ; on porte l'avertissement
            // de chevauchement dans l'outcome de la commande, lu depuis le read model EXISTANT
            // JourneeEnfantQuery (aucune règle ni recalcul neuf, aucun nouvel endpoint). CQRS préservé :
            // c'est un attribut de la réponse du canal requête/réponse, pas la diffusion ni la projection.
            var chevauchement = journee.Chevauchements(requete.EnfantId, requete.Debut).Count > 0;
            return Results.Ok(new PoserSlotReponse(chevauchement));
        });

        routes.MapPost("/api/canal/affecter-periode", (AffecterPeriodeRequete requete, AffecterPeriodeHandler handler) =>
        {
            var resultat = handler.Handle(new AffecterPeriodeCommand(
                requete.ResponsableId, requete.Debut, requete.Fin));

            // Même convention que la pose : succès acquitté, refus métier renvoyé avec son motif.
            return resultat.EstSucces
                ? Results.Ok()
                : Results.BadRequest(resultat.Motif);
        });

        routes.MapPost("/api/canal/definir-transfert", (DefinirTransfertRequete requete, DefinirTransfertHandler handler) =>
        {
            var resultat = handler.Handle(new DefinirTransfertCommand(
                requete.DeposeParId, requete.RecupereParId, requete.LieuId, requete.Heure, requete.Date));

            // Même convention que les autres écritures : succès acquitté, refus métier renvoyé avec son motif.
            return resultat.EstSucces
                ? Results.Ok()
                : Results.BadRequest(resultat.Motif);
        });

        routes.MapPost("/api/canal/editer-acteur", (EditerActeurRequete requete, EditerActeurHandler handler) =>
        {
            var resultat = handler.Handle(new EditerActeurCommand(requete.ActeurId, requete.Nom, requete.Couleur));

            // Même convention que les autres écritures : succès acquitté, refus métier renvoyé avec son motif.
            // Sur succès, le handler a muté le store ET déclenché la diffusion temps réel (les grilles suivent).
            return resultat.EstSucces
                ? Results.Ok()
                : Results.BadRequest(resultat.Motif);
        });

        routes.MapPost("/api/canal/ajouter-acteur", (AjouterActeurRequete requete, AjouterActeurHandler handler) =>
        {
            var resultat = handler.Handle(new AjouterActeurCommand(requete.Nom, requete.Couleur));

            // Même convention que les autres écritures : succès acquitté (l'acteur ajouté est désormais
            // énuméré depuis le store, Sc.1), refus métier renvoyé avec son motif (nom vide, Sc.8).
            return resultat.EstSucces
                ? Results.Ok()
                : Results.BadRequest(resultat.Motif);
        });

        routes.MapPost("/api/canal/supprimer-acteur", (SupprimerActeurRequete requete, SupprimerActeurHandler handler) =>
        {
            var resultat = handler.Handle(new SupprimerActeurCommand(requete.ActeurId));

            // Même convention que les autres écritures : succès acquitté (l'acteur ne sera plus énuméré
            // depuis le store, Sc.1), refus métier renvoyé avec son motif. Sur succès, le handler a muté
            // le store ET déclenché la diffusion temps réel (grilles et légende suivent).
            return resultat.EstSucces
                ? Results.Ok()
                : Results.BadRequest(resultat.Motif);
        });

        routes.MapPost("/api/canal/supprimer-periode", (SupprimerPeriodeRequete requete, SupprimerPeriodeHandler handler) =>
        {
            var resultat = handler.Handle(new SupprimerPeriodeCommand(requete.PeriodeId));

            // Même convention que les autres écritures : succès acquitté (la période ne sera plus relue
            // depuis le store, la case se re-résout), refus métier renvoyé avec son motif. Idempotent :
            // un identifiant absent / déjà supprimé réussit sans effet (Sc.5).
            return resultat.EstSucces
                ? Results.Ok()
                : Results.BadRequest(resultat.Motif);
        });

        routes.MapPost("/api/canal/definir-cycle", (DefinirCycleRequete requete, DefinirCycleHandler handler) =>
        {
            var resultat = handler.Handle(new DefinirCycleCommand(requete.NombreSemaines, requete.Affectations));

            // Même convention que les autres écritures : succès acquitté (le cycle est défini, les grilles
            // suivent via la diffusion temps réel déclenchée par le handler), refus métier renvoyé avec son
            // motif (« le cycle doit compter au moins une semaine », N < 1, Sc.7).
            return resultat.EstSucces
                ? Results.Ok()
                : Results.BadRequest(resultat.Motif);
        });

        return routes;
    }
}
