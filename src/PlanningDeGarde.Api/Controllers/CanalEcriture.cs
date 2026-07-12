using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;

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

    /// <summary>Corps de la requête de pose d'un slot RÉCURRENT hebdo émise via le canal requête/réponse
    /// (s29) : enfant + lieu + jour de semaine + plage horaire (début→fin, sans date). Refus miroir de la
    /// pose ponctuelle (lieu inconnu / durée non positive) renvoyé avec son motif.</summary>
    public sealed record PoserSlotRecurrentRequete(
        string EnfantId, string LieuId, DayOfWeek JourDeSemaine, TimeSpan HeureDebut, TimeSpan HeureFin,
        bool ConditionneGarde = false, string PoseurId = "");

    /// <summary>Corps de la requête de suppression d'un slot récurrent (s29) : la clé est l'identifiant
    /// stable du slot récurrent (jamais un libellé) ; la suppression est idempotente côté handler.</summary>
    public sealed record SupprimerSlotRecurrentRequete(string SlotId);

    /// <summary>Corps de la requête d'affectation de période émise via le canal requête/réponse.</summary>
    public sealed record AffecterPeriodeRequete(string ResponsableId, DateTime Debut, DateTime Fin);

    /// <summary>Corps de la requête de définition d'un transfert de bascule émise via le canal.</summary>
    public sealed record DefinirTransfertRequete(string DeposeParId, string RecupereParId, string LieuId, TimeSpan Heure, DateTime Date);

    /// <summary>Corps de la requête d'ajout d'un lieu au référentiel du foyer (s27) émise via le canal
    /// d'écriture : le front ne fournit que le libellé (l'identifiant stable est posé côté handler). Refus
    /// métier (libellé vide / doublon) renvoyé avec son motif.</summary>
    public sealed record AjouterLieuRequete(string Libelle);

    /// <summary>Corps de la requête de suppression d'un lieu du référentiel du foyer (s27) émise via le
    /// canal d'écriture : la clé est l'identifiant stable du lieu. Idempotente côté handler (id absent /
    /// déjà supprimé = no-op qui réussit). Borne : les slots déjà posés sur ce lieu conservent leur lieu.</summary>
    public sealed record SupprimerLieuRequete(string LieuId);

    /// <summary>Corps de la requête d'ajout d'un enfant au référentiel du foyer (s30) émise via le canal
    /// d'écriture : le front n'émet que le prénom ; l'identifiant stable neuf opaque est généré côté handler
    /// (jamais dérivé du prénom). Refus métier (prénom vide / doublon) renvoyé avec son motif.</summary>
    public sealed record AjouterEnfantRequete(string Prenom);

    /// <summary>Corps de la requête d'édition du prénom d'un enfant (s30) émise via le canal d'écriture : la
    /// clé est l'<b>identifiant stable</b> de l'enfant (jamais éditable) ; seul le prénom change. Refus métier
    /// (prénom vide / doublon d'un autre enfant) renvoyé avec son motif.</summary>
    public sealed record EditerEnfantRequete(string EnfantId, string NouveauPrenom);

    /// <summary>Corps de la requête de liaison d'un enfant à un parent-acteur (s34) émise via le canal
    /// d'écriture : l'identifiant stable de l'enfant + l'identifiant stable de l'acteur. Refus métier (acteur
    /// inexistant / non-parent / borne 2 parents max) renvoyé avec son motif ; déjà lié = neutre.</summary>
    public sealed record LierEnfantParentRequete(string EnfantId, string ActeurId);

    /// <summary>Corps de la requête de retrait du lien d'un enfant vers un parent-acteur (s34) émise via le
    /// canal d'écriture : l'identifiant stable de l'enfant + l'identifiant stable de l'acteur. Idempotent côté
    /// handler (parent déjà non lié = no-op qui réussit).</summary>
    public sealed record DelierEnfantParentRequete(string EnfantId, string ActeurId);

    /// <summary>Corps de la requête d'édition d'un acteur émise via le canal d'écriture. Le nom et la
    /// couleur sont deux champs optionnels et indépendants : un champ absent (null) n'est pas appliqué
    /// (renommage seul au Sc.1, recoloriage seul au Sc.2). L'identifiant stable n'est jamais éditable.</summary>
    public sealed record EditerActeurRequete(string ActeurId, string? Nom = null, string? Couleur = null, string? Adresse = null);

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

    /// <summary>Corps de la requête de suppression d'un slot émise via le canal d'écriture (6ᵉ usage du menu
    /// clic-case). La clé est l'<b>identifiant stable</b> du slot (jamais un libellé) ; la suppression est
    /// idempotente côté handler (id absent / déjà supprimé = no-op qui réussit).</summary>
    public sealed record SupprimerSlotRequete(string SlotId);

    /// <summary>Corps de la requête d'édition d'une période émise via le canal d'écriture (5ᵉ usage du menu
    /// clic-case). La clé est l'<b>identifiant stable</b> de la période ; le nouveau responsable et les
    /// nouvelles bornes décrivent l'état voulu. L'état observé (jeton de concurrence optimiste) est résolu
    /// côté API sur cet identifiant avant d'invoquer le handler — le front n'émet que la cible.</summary>
    public sealed record EditerPeriodeRequete(string PeriodeId, string NouveauResponsableId, DateTime NouveauDebut, DateTime NouvelleFin);

    /// <summary>Corps de la requête de création d'un rôle du référentiel du foyer (s21) émise via le canal
    /// d'écriture : le front ne fournit que le libellé ; l'identifiant stable neuf opaque est généré côté
    /// handler (jamais dérivé du libellé). Refus métier (libellé vide / doublon) renvoyé avec son motif.</summary>
    public sealed record CreerRoleRequete(string Libelle);

    /// <summary>Corps de la requête de renommage d'un rôle du référentiel émise via le canal d'écriture.
    /// La clé est l'<b>identifiant stable</b> du rôle (jamais éditable) ; seul le libellé change.</summary>
    public sealed record RenommerRoleRequete(string RoleId, string NouveauLibelle);

    /// <summary>Corps de la requête de suppression d'un rôle du référentiel émise via le canal d'écriture.
    /// La clé est l'<b>identifiant stable</b> du rôle ; la suppression fait retomber « sans rôle » les
    /// acteurs porteurs (repli neutre) et est idempotente côté handler (id absent = no-op qui réussit).</summary>
    public sealed record SupprimerRoleRequete(string RoleId);

    /// <summary>Corps de la requête d'affectation d'un rôle du référentiel à un acteur (s21) émise via le
    /// canal d'écriture : l'identifiant stable de l'acteur et l'identifiant stable du <b>rôle du référentiel</b>
    /// (jamais un libellé en dur). Un id de rôle absent du référentiel = rejet côté handler (champ fermé
    /// sur le référentiel).</summary>
    public sealed record AffecterRoleRequete(string ActeurId, string RoleId);

    /// <summary>Corps de la requête de retrait du rôle d'un acteur (s21) : l'identifiant stable de l'acteur.
    /// L'acteur retombe « sans rôle » (repli neutre, attribut optionnel vidé).</summary>
    public sealed record RetirerRoleRequete(string ActeurId);

    /// <summary>Corps de la requête de création d'un compte utilisateur (s22) associé à un acteur, émise via
    /// le canal d'écriture : l'identifiant stable de l'acteur et l'email. L'identifiant stable neuf opaque du
    /// compte est généré côté handler (jamais dérivé de l'email) ; le statut « inactif » est le défaut métier.
    /// Refus métier (email vide / doublon, acteur inconnu, acteur déjà associé) renvoyé avec son motif.</summary>
    public sealed record CreerCompteRequete(string ActeurId, string Email);

    /// <summary>Corps de la requête d'activation d'un compte utilisateur (s24) émise via le canal d'écriture :
    /// l'identifiant stable opaque du compte. Le statut passe Inactif→Actif côté handler (mutation portée par
    /// l'agrégat) ; refus métier (compte introuvable) renvoyé avec son motif, idempotence (déjà Actif) assumée.</summary>
    public sealed record ActiverCompteRequete(string CompteId);

    /// <summary>Corps de la requête de désignation d'un acteur comme admin du foyer (s22) émise via le canal
    /// d'écriture : l'identifiant stable de l'acteur. L'invariant admin=parent est porté par l'agrégat Domain
    /// (un acteur non-Parent est rejeté sans écriture, Sc.4) ; le motif de refus est renvoyé au front.</summary>
    public sealed record DesignerAdminRequete(string ActeurId);

    /// <summary>Corps de la requête de connexion locale par email (s23) émise via le canal requête/réponse :
    /// l'email d'un compte du référentiel. La connexion réussit ssi un compte de cet email existe ET est
    /// Actif ; sinon refus avec motif clair (email inconnu / compte non activé), aucune session ouverte.</summary>
    public sealed record SeConnecterRequete(string Email, string? MotDePasse = null);

    /// <summary>Corps de la réponse de succès d'une connexion (s23 ; type ancré s25 Sc.5) : l'identité réelle
    /// de la session ouverte (l'acteur lié 1-1 au compte connecté — id stable), son nom d'affichage résolu côté
    /// serveur (bandeau « Connecté : &lt;nom&gt; » sans règle métier côté UI), et son <b>type</b> (Admin /
    /// Parent / Autre) résolu côté serveur : le front ancre l'identité réelle de la session sur CET acteur et
    /// son type — le gating d'écriture suit le type RÉEL, jamais un rôle Parent hérité du configurateur en dur.</summary>
    public sealed record SeConnecterReponse(string ActeurId, string Nom, TypeActeur Type);

    /// <summary>Corps de la requête de demande de récupération de mot de passe (s28, volet 1) émise via le
    /// canal d'écriture : l'email pour lequel on demande la réinitialisation. La réponse est TOUJOURS un
    /// succès NEUTRE (aucun jeton, aucun indice d'existence — anti-énumération) ; si l'email est porté par
    /// un compte, un jeton de réinitialisation est généré côté serveur et remis par mail (canal réel SMTP).</summary>
    public sealed record DemanderRecuperationRequete(string Email);

    /// <summary>Corps de la requête de redéfinition de mot de passe par jeton (s28, volet 1) émise via le
    /// canal d'écriture : le jeton de réinitialisation reçu par mail et le nouveau mot de passe (clair, haché
    /// côté serveur). Le jeton doit être VALIDE (connu, non consommé, non expiré) — sinon refus avec motif,
    /// sans mutation ; sur succès, le mot de passe est redéfini (haché PBKDF2) et le jeton consommé (usage unique).</summary>
    public sealed record RedefinirMotDePasseRequete(string Jeton, string NouveauMotDePasse);

    /// <summary>Corps de la requête de définition d'un mot de passe sur un compte (s28, volet 2) émise via
    /// le canal d'écriture : l'identifiant stable du compte et le mot de passe (clair, haché côté serveur).
    /// Pose le condensat sur le compte (jamais le clair) → le compte devient connectable email + mot de passe.</summary>
    public sealed record DefinirMotDePasseRequete(string CompteId, string MotDePasse);

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

        routes.MapPost("/api/canal/poser-slot-recurrent", (PoserSlotRecurrentRequete requete, PoserSlotRecurrentHandler handler) =>
        {
            var resultat = handler.Handle(new PoserSlotRecurrentCommand(
                requete.EnfantId, requete.LieuId, requete.JourDeSemaine, requete.HeureDebut, requete.HeureFin,
                requete.ConditionneGarde, requete.PoseurId));

            // Même convention que les autres écritures : succès acquitté (le slot récurrent est enregistré,
            // ses occurrences apparaissent sur la grille ; le handler a déclenché la diffusion temps réel),
            // refus métier (lieu inconnu / durée non positive) renvoyé avec son motif.
            return resultat.EstSucces
                ? Results.Ok()
                : Results.BadRequest(resultat.Motif);
        });

        routes.MapPost("/api/canal/supprimer-slot-recurrent", (SupprimerSlotRecurrentRequete requete, SupprimerSlotRecurrentHandler handler) =>
        {
            var resultat = handler.Handle(new SupprimerSlotRecurrentCommand(requete.SlotId));

            // Même convention : succès acquitté (le slot récurrent quitte le store, ses occurrences
            // disparaissent des cases ; le handler a déclenché la diffusion temps réel). Idempotent côté
            // handler (id absent / déjà supprimé = no-op qui réussit).
            return resultat.EstSucces
                ? Results.Ok()
                : Results.BadRequest(resultat.Motif);
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

        routes.MapPost("/api/canal/ajouter-lieu",
            (AjouterLieuRequete requete, AjouterLieuHandler handler, INotificateurPlanning notificateur) =>
        {
            var resultat = handler.Handle(new AjouterLieuCommand(requete.Libelle));

            // Même convention que les autres écritures : succès acquitté (le lieu ajouté est désormais énuméré
            // depuis le store, disponible à la saisie), refus métier renvoyé avec son motif (libellé vide /
            // doublon, S3). Sur succès, l'adaptateur de gauche déclenche la DIFFUSION temps réel (lecture
            // seule) : les sélecteurs de lieu des dialogs des autres écrans suivent sans rechargement (S6).
            if (!resultat.EstSucces)
                return Results.BadRequest(resultat.Motif);

            notificateur.NotifierMiseAJour();
            return Results.Ok();
        });

        routes.MapPost("/api/canal/supprimer-lieu",
            (SupprimerLieuRequete requete, SupprimerLieuHandler handler, INotificateurPlanning notificateur) =>
        {
            var resultat = handler.Handle(new SupprimerLieuCommand(requete.LieuId));

            // Même convention : succès acquitté (le lieu quitte le référentiel, plus proposé à la saisie),
            // refus métier renvoyé avec son motif. Idempotent côté handler. Sur succès, diffusion temps réel :
            // les sélecteurs de lieu des dialogs ne proposent plus le lieu supprimé sans rechargement (S6).
            if (!resultat.EstSucces)
                return Results.BadRequest(resultat.Motif);

            notificateur.NotifierMiseAJour();
            return Results.Ok();
        });

        routes.MapPost("/api/canal/ajouter-enfant", (AjouterEnfantRequete requete, AjouterEnfantHandler handler) =>
        {
            var resultat = handler.Handle(new AjouterEnfantCommand(requete.Prenom));

            // Même convention que les autres écritures : succès acquitté (l'enfant ajouté est désormais énuméré
            // depuis le store, disponible au sélecteur de pose, S9/S10), refus métier renvoyé avec son motif
            // (prénom vide / doublon, S2/S3). La DIFFUSION temps réel est déclenchée PAR LE HANDLER sur succès
            // (les sélecteurs d'enfant des dialogs suivent sans rechargement) — jamais par le canal de diffusion.
            return resultat.EstSucces
                ? Results.Ok()
                : Results.BadRequest(resultat.Motif);
        });

        routes.MapPost("/api/canal/editer-enfant", (EditerEnfantRequete requete, EditerEnfantHandler handler) =>
        {
            var resultat = handler.Handle(new EditerEnfantCommand(requete.EnfantId, requete.NouveauPrenom));

            // Succès acquitté (même id, prénom mis à jour, relu sans rechargement, S9), refus métier
            // (prénom vide / doublon d'un autre enfant) renvoyé avec son motif. Diffusion temps réel par le handler.
            return resultat.EstSucces
                ? Results.Ok()
                : Results.BadRequest(resultat.Motif);
        });

        routes.MapPost("/api/canal/lier-enfant-parent",
            (LierEnfantParentRequete requete, LierEnfantParentHandler handler, INotificateurPlanning notificateur) =>
        {
            var resultat = handler.Handle(new LierEnfantParentCommand(requete.EnfantId, requete.ActeurId));

            // Même convention que les autres écritures : succès acquitté (le parent lié est relu dans la colonne
            // « Parents liés » sans rechargement, s34 Sc.5), refus métier renvoyé avec son motif (acteur
            // inexistant / non-parent / 2 parents max, Sc.2). Sur succès, l'adaptateur de gauche déclenche la
            // DIFFUSION temps réel (lecture seule) : les autres écrans convergent sans rechargement (Sc.6).
            if (!resultat.EstSucces)
                return Results.BadRequest(resultat.Motif);

            notificateur.NotifierMiseAJour();
            return Results.Ok();
        });

        routes.MapPost("/api/canal/delier-enfant-parent",
            (DelierEnfantParentRequete requete, DelierEnfantParentHandler handler, INotificateurPlanning notificateur) =>
        {
            var resultat = handler.Handle(new DelierEnfantParentCommand(requete.EnfantId, requete.ActeurId));

            // Même convention : succès acquitté (le parent délié disparaît de la colonne « Parents liés » sans
            // rechargement, Sc.5), idempotent côté handler (parent déjà non lié = no-op qui réussit). Sur succès,
            // diffusion temps réel (lecture seule) : les autres écrans convergent sans rechargement (Sc.6).
            if (!resultat.EstSucces)
                return Results.BadRequest(resultat.Motif);

            notificateur.NotifierMiseAJour();
            return Results.Ok();
        });

        routes.MapPost("/api/canal/editer-acteur", (EditerActeurRequete requete, EditerActeurHandler handler) =>
        {
            var resultat = handler.Handle(new EditerActeurCommand(requete.ActeurId, requete.Nom, requete.Couleur, requete.Adresse));

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

        routes.MapPost("/api/canal/supprimer-periode",
            (SupprimerPeriodeRequete requete, SupprimerPeriodeHandler handler, INotificateurPlanning notificateur) =>
        {
            var resultat = handler.Handle(new SupprimerPeriodeCommand(requete.PeriodeId));

            // Même convention que les autres écritures : succès acquitté (la période ne sera plus relue
            // depuis le store, la case se re-résout), refus métier renvoyé avec son motif. Idempotent :
            // un identifiant absent / déjà supprimé réussit sans effet (Sc.5). Sur succès, l'adaptateur de
            // gauche déclenche la DIFFUSION temps réel (lecture seule) : les autres écrans re-projettent la
            // grille et la légende sans rechargement (Sc.10). Jamais d'écriture par le canal de diffusion.
            if (!resultat.EstSucces)
                return Results.BadRequest(resultat.Motif);

            notificateur.NotifierMiseAJour();
            return Results.Ok();
        });

        routes.MapPost("/api/canal/supprimer-slot",
            (SupprimerSlotRequete requete, SupprimerSlotHandler handler, INotificateurPlanning notificateur) =>
        {
            var resultat = handler.Handle(new SupprimerSlotCommand(requete.SlotId));

            // Même convention que les autres écritures : succès acquitté (le slot ne sera plus relu depuis
            // le store, la case ne le rend plus), refus métier renvoyé avec son motif. Idempotent : un
            // identifiant absent / déjà supprimé réussit sans effet (Sc.5). Sur succès, l'adaptateur de
            // gauche déclenche la DIFFUSION temps réel (lecture seule) : les autres écrans re-projettent la
            // grille sans rechargement (Sc.10). Jamais d'écriture par le canal de diffusion.
            if (!resultat.EstSucces)
                return Results.BadRequest(resultat.Motif);

            notificateur.NotifierMiseAJour();
            return Results.Ok();
        });

        routes.MapPost("/api/canal/editer-periode",
            (EditerPeriodeRequete requete, IPeriodeRepository periodes, EditerPeriodeHandler handler, INotificateurPlanning notificateur) =>
        {
            // L'état observé (jeton de concurrence optimiste de l'agrégat période) est résolu côté API sur
            // l'identifiant stable : load-then-act. Un identifiant absent (période supprimée entre-temps) →
            // refus métier clair, jamais une écriture aveugle.
            var etatObserve = periodes.AllSnapshots().FirstOrDefault(p => p.Id == requete.PeriodeId);
            if (etatObserve is null)
                return Results.BadRequest("Période introuvable : elle a peut-être été supprimée.");

            var resultat = handler.Handle(new EditerPeriodeCommand(
                etatObserve, requete.NouveauResponsableId, requete.NouveauDebut, requete.NouvelleFin));

            // Même convention que les autres écritures : succès acquitté, refus métier (bornes invalides,
            // état périmé) renvoyé avec son motif. Sur succès, l'adaptateur de gauche déclenche la DIFFUSION
            // temps réel (lecture seule) : les autres écrans re-projettent grille et légende sans rechargement
            // (Sc.11). Jamais d'écriture par le canal de diffusion.
            if (!resultat.EstSucces)
                return Results.BadRequest(resultat.Motif);

            notificateur.NotifierMiseAJour();
            return Results.Ok();
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

        routes.MapPost("/api/canal/creer-role", (CreerRoleRequete requete, CreerRoleHandler handler) =>
        {
            var resultat = handler.Handle(new CreerRoleCommand(requete.Libelle));

            // Même convention que les autres écritures : succès acquitté (le rôle est désormais énuméré
            // depuis le store, Sc.7), refus métier renvoyé avec son motif (libellé vide / doublon, Sc.3).
            return resultat.EstSucces
                ? Results.Ok()
                : Results.BadRequest(resultat.Motif);
        });

        routes.MapPost("/api/canal/renommer-role", (RenommerRoleRequete requete, RenommerRoleHandler handler) =>
        {
            var resultat = handler.Handle(new RenommerRoleCommand(requete.RoleId, requete.NouveauLibelle));

            // Succès acquitté (même id, libellé mis à jour), refus métier (libellé vide / doublon) avec motif.
            return resultat.EstSucces
                ? Results.Ok()
                : Results.BadRequest(resultat.Motif);
        });

        routes.MapPost("/api/canal/supprimer-role", (SupprimerRoleRequete requete, SupprimerRoleHandler handler) =>
        {
            var resultat = handler.Handle(new SupprimerRoleCommand(requete.RoleId));

            // Succès acquitté (le rôle quitte le référentiel, ses porteurs retombent « sans rôle »).
            // Idempotent : un identifiant absent / déjà supprimé réussit sans effet (Sc.6).
            return resultat.EstSucces
                ? Results.Ok()
                : Results.BadRequest(resultat.Motif);
        });

        routes.MapPost("/api/canal/affecter-role", (AffecterRoleRequete requete, AffecterRoleActeurHandler handler) =>
        {
            var resultat = handler.Handle(new AffecterRoleActeurCommand(requete.ActeurId, requete.RoleId));

            // Succès acquitté (l'acteur porte le rôle du référentiel, relu depuis le store côté écran, Sc.8),
            // refus métier renvoyé avec son motif (id de rôle hors référentiel, Sc.4 — jamais de rôle en dur).
            return resultat.EstSucces
                ? Results.Ok()
                : Results.BadRequest(resultat.Motif);
        });

        routes.MapPost("/api/canal/retirer-role", (RetirerRoleRequete requete, RetirerRoleActeurHandler handler) =>
        {
            var resultat = handler.Handle(new RetirerRoleActeurCommand(requete.ActeurId));

            // Succès acquitté (l'acteur retombe « sans rôle », repli neutre, Sc.5).
            return resultat.EstSucces
                ? Results.Ok()
                : Results.BadRequest(resultat.Motif);
        });

        routes.MapPost("/api/canal/creer-compte", (CreerCompteRequete requete, CreerCompteHandler handler, INotificateurPlanning notificateur) =>
        {
            var resultat = handler.Handle(new CreerCompteCommand(requete.Email, requete.ActeurId));

            // Même convention que les autres écritures : succès acquitté (le compte est désormais énuméré
            // depuis le store, associé à l'acteur, statut « inactif », Sc.7), refus métier renvoyé avec son
            // motif (email vide / doublon, acteur inconnu, acteur déjà associé, Sc.2/Sc.3). Sur succès,
            // l'adaptateur de gauche déclenche la DIFFUSION temps réel (lecture seule) : les autres écrans
            // ré-énumèrent les comptes sans rechargement (Sc.9). Jamais d'écriture par le canal de diffusion.
            if (!resultat.EstSucces)
                return Results.BadRequest(resultat.Motif);

            notificateur.NotifierMiseAJour();
            return Results.Ok();
        });

        routes.MapPost("/api/canal/activer-compte", (ActiverCompteRequete requete, ActiverCompteHandler handler, INotificateurPlanning notificateur) =>
        {
            var resultat = handler.Handle(new ActiverCompteCommand(requete.CompteId));

            // Même convention que les autres écritures : succès acquitté (le compte est désormais Actif,
            // relu depuis le store, Sc.5), refus métier renvoyé avec son motif (compte introuvable, Sc.3).
            // Sur succès, l'adaptateur de gauche déclenche la DIFFUSION temps réel (lecture seule) : les
            // autres écrans ré-énumèrent les comptes sans rechargement (Sc.7). Jamais d'écriture par diffusion.
            if (!resultat.EstSucces)
                return Results.BadRequest(resultat.Motif);

            notificateur.NotifierMiseAJour();
            return Results.Ok();
        });

        routes.MapPost("/api/canal/designer-admin", (DesignerAdminRequete requete, DesignerAdminHandler handler, INotificateurPlanning notificateur) =>
        {
            var resultat = handler.Handle(new DesignerAdminCommand(requete.ActeurId));

            // Même convention que les autres écritures : succès acquitté (l'acteur est désormais admin du
            // foyer, Sc.4), refus métier renvoyé avec son motif (l'admin doit être un parent, Sc.4). Sur
            // succès, l'adaptateur de gauche déclenche la DIFFUSION temps réel (lecture seule) : les autres
            // écrans re-projettent l'admin sans rechargement (Sc.9). Jamais d'écriture par le canal de diffusion.
            if (!resultat.EstSucces)
                return Results.BadRequest(resultat.Motif);

            notificateur.NotifierMiseAJour();
            return Results.Ok();
        });

        routes.MapPost("/api/canal/se-connecter", (SeConnecterRequete requete, SeConnecterHandler handler, IReferentielResponsables referentiel, IEnumerationActeursFoyer acteurs) =>
        {
            var resultat = handler.Handle(new SeConnecterCommand(requete.Email, requete.MotDePasse));

            // Connexion = commande applicative (canal requête/réponse) : réussit ssi un compte de cet email
            // existe ET est Actif. Sur refus (email inconnu / compte non activé), aucune session — le motif
            // clair est renvoyé au front. Sur succès, l'identité réelle de la session est l'acteur lié au
            // compte ; son nom est résolu côté serveur (référentiel) pour le bandeau « Connecté : … ». La
            // session est un état d'hôte/requête, PAS un agrégat durable (aucune persistance neuve, règle 30).
            if (!resultat.EstSucces)
                return Results.BadRequest(resultat.Motif);

            // Type de l'acteur du compte résolu côté serveur (D3, lecture seule) : le front ancre l'identité
            // réelle de la session sur CET acteur ET son type, de sorte que le gating d'écriture suive le type
            // RÉEL (Autre → pas les droits Parent), et non un rôle Parent hérité du configurateur en dur (Sc.5).
            var acteurId = resultat.Valeur!.IdentiteReelle;
            return Results.Ok(new SeConnecterReponse(acteurId, referentiel.NomDe(acteurId), acteurs.TypeDe(acteurId)));
        });

        routes.MapPost("/api/canal/demander-recuperation", (DemanderRecuperationRequete requete, DemanderRecuperationMotDePasseHandler handler) =>
        {
            // Demande de récupération = commande applicative (canal requête/réponse). Le handler résout le
            // compte sur l'email ; s'il existe, un jeton de réinitialisation est généré côté serveur et remis
            // au canal mail RÉEL (adaptateur SMTP). La RÉPONSE au client est TOUJOURS un succès NEUTRE (le
            // handler renvoie systématiquement un succès), qu'un compte existe ou non : aucun jeton, aucun
            // indice d'existence ne transite par la réponse (anti-énumération, S4). Le jeton ne voyage QUE
            // par le mail. Aucune session, aucune diffusion : lecture d'existence + envoi, rien d'autre.
            handler.Handle(new DemanderRecuperationMotDePasseCommand(requete.Email));
            return Results.Ok();
        });

        routes.MapPost("/api/canal/redefinir-mot-de-passe", (RedefinirMotDePasseRequete requete, RedefinirMotDePasseHandler handler) =>
        {
            var resultat = handler.Handle(new RedefinirMotDePasseCommand(requete.Jeton, requete.NouveauMotDePasse));

            // Même convention que les autres écritures : succès acquitté (le mot de passe est redéfini haché
            // et le jeton consommé — usage unique), refus métier (jeton inconnu / consommé / expiré) renvoyé
            // avec son motif, sans mutation. Aucune session ni diffusion : c'est une écriture de compte.
            return resultat.EstSucces
                ? Results.Ok()
                : Results.BadRequest(resultat.Motif);
        });

        routes.MapPost("/api/canal/definir-mot-de-passe", (DefinirMotDePasseRequete requete, DefinirMotDePasseHandler handler) =>
        {
            var resultat = handler.Handle(new DefinirMotDePasseCommand(requete.CompteId, requete.MotDePasse));

            // Même convention que les autres écritures : succès acquitté (le mot de passe haché est posé sur
            // le compte, qui devient connectable email + mot de passe), refus métier renvoyé avec son motif.
            return resultat.EstSucces
                ? Results.Ok()
                : Results.BadRequest(resultat.Motif);
        });

        return routes;
    }
}
