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

    /// <summary>Corps de la requête de délégation de la récupération d'une PLAGE (s44 → s45) émise via le canal
    /// requête/réponse : le jour de DÉBUT, l'enfant sélectionné, l'identifiant stable de l'acteur RECEVANT et le
    /// jour de FIN (INCLUS) <paramref name="JourFin"/> (champ « jusqu'au »). <paramref name="JourFin"/> absent
    /// (null) = plage réduite à UN jour (fin = début) → parité s44. Le use case COMPOSE l'écriture surcharge
    /// MULTI-JOURS existante (s06) ; refus métier (délégataire inconnu, soi-même, fin &lt; début) renvoyé avec son motif.</summary>
    public sealed record DeleguerRecuperationRequete(
        DateOnly Jour, string EnfantId, string VersActeurId, DateOnly? JourFin = null);

    /// <summary>Corps de la requête « reprendre ce jour » (s46) émise via le canal requête/réponse : le jour
    /// REPRIS et l'enfant sélectionné. Granularité = UNE occurrence. Le use case COMPOSE la SUPPRESSION de
    /// surcharge existante (s16) — la case retombe sur le fond, le transfert dérivé s31 disparaît. Jour sans
    /// délégation active = no-op idempotent (succès).</summary>
    public sealed record AnnulerDelegationRequete(DateOnly Jour, string EnfantId);

    /// <summary>Corps de la requête de définition d'un transfert de bascule émise via le canal.</summary>
    public sealed record DefinirTransfertRequete(string DeposeParId, string RecupereParId, string LieuId, TimeSpan Heure, DateTime Date);

    /// <summary>Corps de la requête d'ajout d'une activité au référentiel du foyer (s35, ex-« lieu » s27) émise
    /// via le canal d'écriture : le front ne fournit que le libellé (l'identifiant stable est posé côté handler).
    /// Refus métier (libellé vide / doublon) renvoyé avec son motif.</summary>
    public sealed record AjouterActiviteRequete(string Libelle);

    /// <summary>Corps de la requête de suppression d'une activité du référentiel du foyer (s35) émise via le
    /// canal d'écriture : la clé est l'identifiant stable de l'activité. Idempotente côté handler (id absent /
    /// déjà supprimé = no-op qui réussit). Borne : les slots déjà posés sur cette activité conservent leur lieu.</summary>
    public sealed record SupprimerActiviteRequete(string ActiviteId);

    /// <summary>Corps de la requête d'édition d'une activité (s35 Sc.2/Sc.4) émise via le canal d'écriture : la
    /// clé est l'identifiant stable ; libellé et adresse sont deux champs OPTIONNELS et indépendants (un champ
    /// absent (null) n'est pas appliqué — aucune écriture partielle croisée). Refus (libellé fourni vide) renvoyé.</summary>
    public sealed record EditerActiviteRequete(string ActiviteId, string? Libelle = null, string? Adresse = null);

    /// <summary>Corps de la requête de liaison d'un enfant à une activité (s35 Sc.3/Sc.5) émise via le canal
    /// d'écriture : l'identifiant stable de l'enfant + l'identifiant stable de l'activité. Lien N-M (aucune borne
    /// de cardinalité). Refus métier (enfant / activité inexistant) renvoyé avec son motif ; déjà lié = neutre.</summary>
    public sealed record LierEnfantActiviteRequete(string EnfantId, string ActiviteId);

    /// <summary>Corps de la requête de retrait du lien d'un enfant vers une activité (s35 Sc.3/Sc.5) émise via
    /// le canal d'écriture : l'identifiant stable de l'enfant + l'identifiant stable de l'activité. Idempotent
    /// côté handler (enfant déjà non lié = no-op qui réussit).</summary>
    public sealed record DelierEnfantActiviteRequete(string EnfantId, string ActiviteId);

    /// <summary>Corps de la requête d'ajout d'un enfant au référentiel du foyer (s30) émise via le canal
    /// d'écriture : le front n'émet que le prénom ; l'identifiant stable neuf opaque est généré côté handler
    /// (jamais dérivé du prénom). Refus métier (prénom vide / doublon) renvoyé avec son motif.</summary>
    public sealed record AjouterEnfantRequete(string Prenom);

    /// <summary>Corps de la requête d'édition du prénom d'un enfant (s30) émise via le canal d'écriture : la
    /// clé est l'<b>identifiant stable</b> de l'enfant (jamais éditable) ; seul le prénom change. Refus métier
    /// (prénom vide / doublon d'un autre enfant) renvoyé avec son motif.</summary>
    public sealed record EditerEnfantRequete(string EnfantId, string NouveauPrenom);

    /// <summary>Corps de la requête de liaison d'un enfant à un parent-acteur (s34) émise via le canal
    /// d'écriture : l'identifiant stable de l'enfant + l'identifiant stable de l'acteur + le <b>rôle-du-lien</b>
    /// (père / mère / parent-libre, s37 — défaut « parent-libre » si absent). Refus métier (acteur inexistant /
    /// non-parent / borne 2 parents max / deux même rôle exclusif) renvoyé avec son motif ; déjà lié = maj du rôle.</summary>
    public sealed record LierEnfantParentRequete(string EnfantId, string ActeurId, RoleDuLien Role = RoleDuLien.ParentLibre);

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

    /// <summary>Corps de la requête de bascule du flag « est rôle parent » d'un rôle (s36, B1) émise via le
    /// canal d'écriture : l'identifiant stable (clé) et l'état voulu du flag (coche/décoche). Le flag est la
    /// source de vérité de l'éligibilité au lien enfant↔parent — jamais le libellé (anti-piège s35).</summary>
    public sealed record MarquerRoleParentRequete(string RoleId, bool EstParent);

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

    /// <summary>Corps de la requête de désactivation d'un compte utilisateur (s41, sens OFF) émise via le canal
    /// d'écriture : l'identifiant stable opaque du compte. Le statut passe Actif→Inactif côté handler (mutation
    /// portée par l'agrégat) ; refus métier (compte introuvable) renvoyé avec son motif, idempotence (déjà
    /// Inactif) assumée.</summary>
    public sealed record DesactiverCompteRequete(string CompteId);

    /// <summary>Corps de la requête de désignation d'un acteur comme admin du foyer (s22) émise via le canal
    /// d'écriture : l'identifiant stable de l'acteur. L'invariant admin=parent est porté par l'agrégat Domain
    /// (un acteur non-Parent est rejeté sans écriture, Sc.4) ; le motif de refus est renvoyé au front.</summary>
    public sealed record DesignerAdminRequete(string ActeurId);

    /// <summary>Corps de la requête de dé-désignation d'un admin du foyer (s41, sens OFF) émise via le canal
    /// d'écriture : l'identifiant stable de l'acteur. La borne « dernier admin » (le foyer garde ≥1 admin) et
    /// le refus d'un acteur inconnu sont portés par l'agrégat Domain / le handler ; le motif de refus est
    /// renvoyé au front.</summary>
    public sealed record DeDesignerAdminRequete(string ActeurId);

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

    /// <summary>Corps de la requête « marquer lu » de la cloche (s47) émise via le canal d'écriture : l'utilisateur
    /// courant (id d'acteur) + l'événement à marquer, ou <c>EvenementId</c> null = marquer TOUTES ses notifications
    /// lues. Idempotent côté handler (re-marquer = aucun doublon, compteur stable). Mute uniquement l'état de
    /// lecture, jamais le planning ; ne déclenche pas la diffusion (état PAR utilisateur, privé).</summary>
    public sealed record MarquerNotificationsLuesRequete(string UtilisateurId, string? EvenementId = null);

    /// <summary>Corps de la requête PROPOSER un échange sur une PLAGE (s47 → s52) émise via le canal d'écriture :
    /// le jour de DÉBUT, l'enfant, l'acteur RECEVANT et le jour de FIN (INCLUS) <paramref name="JourFin"/> (champ
    /// « jusqu'au »). <paramref name="JourFin"/> absent (null) = plage réduite à UN jour (fin = début) → parité
    /// STRICTE s47. N'écrit AUCUNE surcharge (canal de consentement) ; refus métier (recevant inconnu, à soi-même,
    /// fin &lt; début) renvoyé avec son motif. Sur succès, la proposition est diffusée (payload) à la cloche du
    /// recevant.</summary>
    public sealed record ProposerEchangeRequete(
        DateOnly Jour, string EnfantId, string VersActeurId, DateOnly? JourFin = null);

    /// <summary>Corps de la requête ACCEPTER / REFUSER une proposition (s47) émise via le canal d'écriture : la clé
    /// est l'identifiant stable de la proposition. Accepter COMPOSE la délégation s44 (surcharge + transfert dérivé) ;
    /// refuser clôt sans aucune écriture.</summary>
    public sealed record RepondrePropositionRequete(string PropositionId);

    /// <summary>Corps de la requête « action de suivi sur un imprévu : proposer un échange » (s51) émise via le canal
    /// d'écriture : l'identifiant de l'ÉVÉNEMENT d'imprévu journalisé (s48) dont on hérite le jour + l'enfant, et
    /// l'acteur RECEVANT choisi. COMPOSE ProposerEchange s47 (proposition pending, 0 surcharge). Sur succès, la
    /// proposition est diffusée (payload) à la cloche du recevant. Refus métier (recevant inconnu / à soi-même)
    /// renvoyé avec son motif — le mini-dialog reste ouvert côté front.</summary>
    public sealed record ProposerEchangeSuiteImprevuRequete(string ImprevuEvenementId, string VersActeurId);

    /// <summary>Corps de la requête SIGNALER un imprévu (s48) émise via le canal d'écriture : le jour, l'enfant, le
    /// TYPE d'imprévu (malade / retard), l'acteur SIGNALANT, un motif OPTIONNEL. Purement INFORMATIF — consigne une
    /// trace au journal (cloche s47) DIFFUSÉE (payload), N'ÉCRIT AUCUNE surcharge (résolution jamais touchée, invariant
    /// s48). Refus métier (type inconnu) renvoyé avec son motif — le mini-dialog reste ouvert côté front.</summary>
    public sealed record SignalerImprevuRequete(
        DateOnly Jour, string EnfantId, TypeImprevu Type, string SignalantId, string Motif = "");

    public static IEndpointRouteBuilder MapperCanalEcriture(this IEndpointRouteBuilder routes)
    {
        routes.MapPost("/api/canal/marquer-notifications-lues",
            (MarquerNotificationsLuesRequete requete, MarquerNotificationsLuesHandler handler) =>
        {
            // Marquer lu = mutation de l'état de lecture PAR utilisateur (cloche s47), jamais du planning.
            // Idempotent côté handler. Aucune diffusion : l'état lu/non-lu est privé à l'utilisateur (le badge
            // de sa cloche décroît sur sa propre reprojection locale, pas chez les autres).
            var resultat = handler.Handle(new MarquerNotificationsLuesCommand(requete.UtilisateurId, requete.EvenementId));
            return resultat.EstSucces ? Results.Ok(resultat.Valeur) : Results.BadRequest(resultat.Motif);
        });

        routes.MapPost("/api/canal/proposer-echange",
            (ProposerEchangeRequete requete, ProposerEchangeHandler handler, INotificateurChangement notificateur) =>
        {
            var resultat = handler.Handle(new ProposerEchangeCommand(requete.Jour, requete.EnfantId, requete.VersActeurId, requete.JourFin));

            // PROPOSER n'écrit AUCUNE surcharge (canal de consentement). Refus métier (recevant inconnu, à
            // soi-même) renvoyé avec son motif — le mini-dialog reste ouvert côté front (Sc.5). Sur succès, la
            // proposition est DIFFUSÉE (payload) : la cloche du recevant reprojette la notif actionnable, 0 GET.
            if (!resultat.EstSucces)
                return Results.BadRequest(resultat.Motif);

            notificateur.NotifierProposition(resultat.Valeur!);
            return Results.Ok();
        });

        routes.MapPost("/api/canal/proposer-echange-suite-imprevu",
            (ProposerEchangeSuiteImprevuRequete requete, ProposerEchangeSuiteImprevuHandler handler, INotificateurChangement notificateur) =>
        {
            // Action de suivi s51 : COMPOSE ProposerEchange s47 EN RÉACTION à un imprévu journalisé s48. Le jour +
            // l'enfant sont HÉRITÉS de l'imprévu (le proposant n'a choisi que le recevant). N'écrit AUCUNE surcharge
            // (canal de consentement). Refus métier (recevant inconnu, à soi-même) renvoyé avec son motif — le
            // mini-dialog reste ouvert côté front. Sur succès, la proposition est DIFFUSÉE (payload) : la cloche du
            // recevant reprojette la notif actionnable, 0 GET. L'imprévu reste au journal, inchangé (fait informatif).
            var resultat = handler.Handle(new ProposerEchangeSuiteImprevuCommand(requete.ImprevuEvenementId, requete.VersActeurId));
            if (!resultat.EstSucces)
                return Results.BadRequest(resultat.Motif);

            notificateur.NotifierProposition(resultat.Valeur!);
            return Results.Ok();
        });

        routes.MapPost("/api/canal/accepter-proposition",
            (RepondrePropositionRequete requete, AccepterPropositionHandler handler, INotificateurPlanning planning, INotificateurChangement notificateur) =>
        {
            var resultat = handler.Handle(new AccepterPropositionCommand(requete.PropositionId));

            // ACCEPTER COMPOSE la délégation s44 : la surcharge du jour est écrite (le recevant prime), le transfert
            // dérivé s31 sort par construction. La consignation au journal (dans le handler de délégation) DIFFUSE
            // déjà l'événement de changement (payload → cloche) ET on notifie MiseAJour (la grille recharge → case
            // converge). On diffuse aussi la proposition (statut accepté) pour clore la notif actionnable. 0 GET.
            if (!resultat.EstSucces)
                return Results.BadRequest(resultat.Motif);

            planning.NotifierMiseAJour();
            notificateur.NotifierProposition(resultat.Valeur!);
            return Results.Ok();
        });

        routes.MapPost("/api/canal/refuser-proposition",
            (RepondrePropositionRequete requete, RefuserPropositionHandler handler, INotificateurChangement notificateur) =>
        {
            var resultat = handler.Handle(new RefuserPropositionCommand(requete.PropositionId));

            // REFUSER clôt la proposition (refusé) SANS aucune écriture de surcharge (store intact). Sur succès,
            // la proposition (statut refusé) est diffusée (payload) : la cloche des concernés retire la notif, 0 GET.
            if (!resultat.EstSucces)
                return Results.BadRequest(resultat.Motif);

            notificateur.NotifierProposition(resultat.Valeur!);
            return Results.Ok();
        });
        routes.MapPost("/api/canal/signaler-imprevu",
            (SignalerImprevuRequete requete, SignalerImprevuHandler handler) =>
        {
            var resultat = handler.Handle(new SignalerImprevuCommand(
                requete.Jour, requete.EnfantId, requete.Type, requete.SignalantId, requete.Motif));

            // Signalement d'imprévu (s48) : purement INFORMATIF. La consignation au journal (décoré
            // JournalChangementsDiffusant) DIFFUSE déjà l'événement (payload → cloche des concernés), 0 GET sur
            // push. AUCUNE surcharge écrite : la résolution du planning n'est jamais touchée (invariant s48).
            // Refus métier (type inconnu) renvoyé avec son motif — le mini-dialog reste ouvert côté front (Sc.5).
            if (!resultat.EstSucces)
                return Results.BadRequest(resultat.Motif);

            return Results.Ok();
        });

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

        routes.MapPost("/api/canal/deleguer-recuperation",
            (DeleguerRecuperationRequete requete, DeleguerRecuperationHandler handler, INotificateurPlanning notificateur) =>
        {
            var resultat = handler.Handle(new DeleguerRecuperationCommand(
                requete.Jour, requete.EnfantId, requete.VersActeurId, requete.JourFin));

            // Le use case COMPOSE l'écriture surcharge ponctuelle (s06) : succès acquitté (la surcharge du jour
            // fait primer le délégataire, le transfert bicolore sort dérivé de s31), refus métier (délégataire
            // inconnu, délégation à soi-même) renvoyé avec son motif — dialog restée ouverte côté front (Sc.5).
            // Sur succès, l'adaptateur de gauche déclenche la DIFFUSION temps réel (lecture seule) : carte et
            // panneau à-venir des autres écrans reprojettent le nouveau responsable + transfert sans rechargement
            // (Sc.6). Jamais d'écriture par le canal de diffusion.
            if (!resultat.EstSucces)
                return Results.BadRequest(resultat.Motif);

            notificateur.NotifierMiseAJour();
            return Results.Ok();
        });

        routes.MapPost("/api/canal/annuler-delegation",
            (AnnulerDelegationRequete requete, AnnulerDelegationHandler handler, INotificateurPlanning notificateur) =>
        {
            var resultat = handler.Handle(new AnnulerDelegationCommand(requete.Jour, requete.EnfantId));

            // « Reprendre ce jour » COMPOSE la suppression de surcharge existante (s16) : la case retombe sur le
            // fond, le transfert dérivé s31 disparaît. No-op idempotent (jour sans délégation active) = succès.
            // Sur succès, l'adaptateur de gauche déclenche la DIFFUSION temps réel (lecture seule) : les autres
            // écrans reprojettent la case au fond sans rechargement (Sc.6). Jamais d'écriture par la diffusion.
            if (!resultat.EstSucces)
                return Results.BadRequest(resultat.Motif);

            notificateur.NotifierMiseAJour();
            return Results.Ok();
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

        routes.MapPost("/api/canal/ajouter-activite",
            (AjouterActiviteRequete requete, AjouterActiviteHandler handler, INotificateurPlanning notificateur) =>
        {
            var resultat = handler.Handle(new AjouterActiviteCommand(requete.Libelle));

            // Même convention que les autres écritures : succès acquitté (l'activité ajoutée est désormais
            // énumérée depuis le store, disponible à la saisie), refus métier renvoyé avec son motif (libellé
            // vide / doublon). Sur succès, l'adaptateur de gauche déclenche la DIFFUSION temps réel (lecture
            // seule) : la table Activités et les sélecteurs de lieu des autres écrans suivent sans rechargement.
            if (!resultat.EstSucces)
                return Results.BadRequest(resultat.Motif);

            notificateur.NotifierMiseAJour();
            return Results.Ok();
        });

        routes.MapPost("/api/canal/supprimer-activite",
            (SupprimerActiviteRequete requete, SupprimerActiviteHandler handler, INotificateurPlanning notificateur) =>
        {
            var resultat = handler.Handle(new SupprimerActiviteCommand(requete.ActiviteId));

            // Même convention : succès acquitté (l'activité quitte le référentiel, plus proposée à la saisie),
            // refus métier renvoyé avec son motif. Idempotent côté handler. Sur succès, diffusion temps réel :
            // la table et les sélecteurs des dialogs ne proposent plus l'activité supprimée sans rechargement.
            if (!resultat.EstSucces)
                return Results.BadRequest(resultat.Motif);

            notificateur.NotifierMiseAJour();
            return Results.Ok();
        });

        routes.MapPost("/api/canal/editer-activite",
            (EditerActiviteRequete requete, EditerActiviteHandler handler, INotificateurPlanning notificateur) =>
        {
            var resultat = handler.Handle(new EditerActiviteCommand(requete.ActiviteId, requete.Libelle, requete.Adresse));

            // Même convention : succès acquitté (libellé et/ou adresse mis à jour, relus sans rechargement),
            // refus métier renvoyé avec son motif (libellé fourni vide). Sur succès, diffusion temps réel
            // (lecture seule) : la table Activités des autres écrans converge sans rechargement (Sc.6).
            if (!resultat.EstSucces)
                return Results.BadRequest(resultat.Motif);

            notificateur.NotifierMiseAJour();
            return Results.Ok();
        });

        routes.MapPost("/api/canal/lier-enfant-activite",
            (LierEnfantActiviteRequete requete, LierEnfantActiviteHandler handler, INotificateurPlanning notificateur) =>
        {
            var resultat = handler.Handle(new LierEnfantActiviteCommand(requete.EnfantId, requete.ActiviteId));

            // Même convention : succès acquitté (l'enfant lié est relu dans la colonne « Enfants liés » sans
            // rechargement, Sc.5), refus métier renvoyé avec son motif (enfant / activité inexistant, Sc.3).
            // Sur succès, diffusion temps réel (lecture seule) : les autres écrans convergent sans rechargement (Sc.6).
            if (!resultat.EstSucces)
                return Results.BadRequest(resultat.Motif);

            notificateur.NotifierMiseAJour();
            return Results.Ok();
        });

        routes.MapPost("/api/canal/delier-enfant-activite",
            (DelierEnfantActiviteRequete requete, DelierEnfantActiviteHandler handler, INotificateurPlanning notificateur) =>
        {
            var resultat = handler.Handle(new DelierEnfantActiviteCommand(requete.EnfantId, requete.ActiviteId));

            // Même convention : succès acquitté (l'enfant délié disparaît de la colonne « Enfants liés » sans
            // rechargement, Sc.5), idempotent côté handler (enfant déjà non lié = no-op qui réussit). Sur succès,
            // diffusion temps réel (lecture seule) : les autres écrans convergent sans rechargement (Sc.6).
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
            var resultat = handler.Handle(new LierEnfantParentCommand(requete.EnfantId, requete.ActeurId, requete.Role));

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

        routes.MapPost("/api/canal/marquer-role-parent",
            (MarquerRoleParentRequete requete, MarquerRoleParentHandler handler, INotificateurPlanning notificateur) =>
        {
            var resultat = handler.Handle(new MarquerRoleParentCommand(requete.RoleId, requete.EstParent));

            // Succès acquitté (le flag « est rôle parent » est posé/retiré, source de vérité de l'éligibilité)
            // + diffusion temps réel (SignalR lecture seule) : les autres écrans reconvergent le sélecteur des
            // parents et la case « rôle parent » sans rechargement (s36 Sc.6/Sc.7). Refus (rôle inexistant) avec
            // motif. La diffusion suit une écriture aboutie, jamais l'inverse (règle 27).
            if (!resultat.EstSucces)
                return Results.BadRequest(resultat.Motif);

            notificateur.NotifierMiseAJour();
            return Results.Ok();
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

        routes.MapPost("/api/canal/de-designer-admin", (DeDesignerAdminRequete requete, DeDesignerAdminHandler handler, INotificateurPlanning notificateur) =>
        {
            var resultat = handler.Handle(new DeDesignerAdminCommand(requete.ActeurId));

            // Sens OFF (s41, débloque le verrou ON s33) : succès acquitté (l'acteur n'est plus admin), refus
            // métier renvoyé avec son motif (borne « dernier admin », acteur inconnu — Sc.2/Sc.1). Sur succès,
            // l'adaptateur de gauche déclenche la DIFFUSION temps réel (lecture seule) : les autres écrans
            // re-projettent l'état admin sans rechargement (Sc.6). Jamais d'écriture par le canal de diffusion.
            if (!resultat.EstSucces)
                return Results.BadRequest(resultat.Motif);

            notificateur.NotifierMiseAJour();
            return Results.Ok();
        });

        routes.MapPost("/api/canal/desactiver-compte", (DesactiverCompteRequete requete, DesactiverCompteHandler handler, INotificateurPlanning notificateur) =>
        {
            var resultat = handler.Handle(new DesactiverCompteCommand(requete.CompteId));

            // Sens OFF (s41) : succès acquitté (le compte est désormais Inactif, relu du store), refus métier
            // renvoyé avec son motif (compte introuvable — Sc.3). Sur succès, l'adaptateur de gauche déclenche
            // la DIFFUSION temps réel (lecture seule) : les autres écrans ré-énumèrent les comptes sans
            // rechargement (Sc.6). Jamais d'écriture par le canal de diffusion.
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
