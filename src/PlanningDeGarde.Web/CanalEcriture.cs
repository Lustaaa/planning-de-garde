namespace PlanningDeGarde.Web;

/// <summary>
/// Corps des requêtes d'écriture émises par le front <b>WASM</b> vers le canal d'écriture de
/// l'<b>API distante</b> (endpoints <c>/api/canal/*</c>). Ce sont de simples DTO de transport
/// sérialisés en JSON : le front ne porte plus le mapping d'endpoints (qui vit côté hôte d'API
/// détaché, <c>PlanningDeGarde.Api</c>), il n'en émet que les corps.
/// </summary>
public static class CanalEcriture
{
    /// <summary>Corps de la requête de pose de slot émise via le canal requête/réponse.</summary>
    public sealed record PoserSlotRequete(string EnfantId, string LieuId, DateTime Debut, DateTime Fin);

    /// <summary>Corps de la réponse de succès de la pose : l'avertissement de chevauchement (règle 16,
    /// accepté + averti) porté par l'outcome de la commande. Déserialisé par la dialog pour afficher un
    /// bandeau à part, non bloquant (Sc.7). Aucune logique métier côté front : le front ne fait que lire
    /// ce drapeau résolu côté API (read model existant), il ne recalcule jamais le chevauchement.</summary>
    public sealed record PoserSlotReponse(bool Chevauchement);

    /// <summary>Corps de la requête de pose d'un slot RÉCURRENT hebdo émise via le canal (s29) : enfant +
    /// lieu + jour de semaine + plage horaire (début→fin, sans date). Aucune règle métier côté front : les
    /// refus (lieu inconnu / durée non positive) sont tranchés côté handler.</summary>
    public sealed record PoserSlotRecurrentRequete(
        string EnfantId, string LieuId, DayOfWeek JourDeSemaine, TimeSpan HeureDebut, TimeSpan HeureFin,
        bool ConditionneGarde = false, string PoseurId = "");

    /// <summary>Corps de la requête de suppression d'un slot récurrent (s29) : la clé est l'identifiant
    /// stable du slot récurrent. Idempotente côté handler.</summary>
    public sealed record SupprimerSlotRecurrentRequete(string SlotId);

    /// <summary>Corps de la requête d'affectation de période émise via le canal requête/réponse.</summary>
    public sealed record AffecterPeriodeRequete(string ResponsableId, DateTime Debut, DateTime Fin);

    /// <summary>Corps de la requête de délégation de la récupération d'une PLAGE (s44 → s45) : le jour de
    /// DÉBUT, l'enfant sélectionné, l'identifiant stable de l'acteur RECEVANT et le jour de FIN (INCLUS)
    /// <paramref name="JourFin"/> (champ « jusqu'au »). <paramref name="JourFin"/> absent (null) = plage
    /// réduite à UN jour (fin = début) → parité STRICTE s44. Émis via le canal d'écriture ; le use case
    /// COMPOSE l'écriture surcharge MULTI-JOURS existante (s06). Refus métier (délégataire inconnu / à
    /// soi-même / fin &lt; début) renvoyé avec son motif (dialog restée ouverte, Sc.5).</summary>
    public sealed record DeleguerRecuperationRequete(
        DateOnly Jour, string EnfantId, string VersActeurId, DateOnly? JourFin = null);

    /// <summary>Corps de la requête « reprendre ce jour » (s46, ferme la boucle <i>undo</i> s44/s45) émise via
    /// le canal d'écriture : le jour REPRIS et l'enfant sélectionné. Granularité = UNE occurrence (seul ce jour,
    /// même dans une plage s45). Le use case COMPOSE la SUPPRESSION de surcharge existante (s16) : la case
    /// retombe sur le fond, le transfert dérivé s31 disparaît. Jour sans délégation active = no-op idempotent.</summary>
    public sealed record AnnulerDelegationRequete(DateOnly Jour, string EnfantId);

    /// <summary>Corps de la requête de définition d'un transfert de bascule émise via le canal.</summary>
    public sealed record DefinirTransfertRequete(string DeposeParId, string RecupereParId, string LieuId, TimeSpan Heure, DateTime Date);

    /// <summary>Corps de la requête d'ajout d'une activité au référentiel du foyer (s35, ex-« lieu » s27) : le
    /// front n'émet que le libellé ; l'identifiant stable est posé côté handler. Refus (libellé vide / doublon).</summary>
    public sealed record AjouterActiviteRequete(string Libelle);

    /// <summary>Corps de la requête de suppression d'une activité du référentiel (s35) : la clé est l'identifiant
    /// stable. Idempotente côté handler ; les slots déjà posés sur cette activité conservent leur lieu.</summary>
    public sealed record SupprimerActiviteRequete(string ActiviteId);

    /// <summary>Corps de la requête d'édition d'une activité (s35 Sc.2/Sc.4) : la clé est l'identifiant stable ;
    /// libellé et adresse sont deux champs optionnels indépendants (absent (null) = non appliqué, aucune écriture
    /// partielle croisée). L'adresse vide est acceptée (champ facultatif).</summary>
    public sealed record EditerActiviteRequete(string ActiviteId, string? Libelle = null, string? Adresse = null);

    /// <summary>Corps de la requête de liaison d'un enfant à une activité (s35 Sc.5) : l'id stable de l'enfant et
    /// l'id stable de l'activité. Lien N-M (aucune borne). Refus métier (enfant / activité inexistant) renvoyé ;
    /// déjà lié = neutre.</summary>
    public sealed record LierEnfantActiviteRequete(string EnfantId, string ActiviteId);

    /// <summary>Corps de la requête de retrait du lien d'un enfant vers une activité (s35 Sc.5) : l'id stable de
    /// l'enfant et l'id stable de l'activité. Idempotent côté handler (enfant déjà non lié = no-op qui réussit).</summary>
    public sealed record DelierEnfantActiviteRequete(string EnfantId, string ActiviteId);

    /// <summary>Corps de la requête d'ajout d'un enfant au référentiel du foyer (s30) : le front n'émet que le
    /// prénom ; l'identifiant stable neuf opaque est généré côté handler. Refus métier (prénom vide / doublon)
    /// renvoyé avec son motif.</summary>
    public sealed record AjouterEnfantRequete(string Prenom);

    /// <summary>Corps de la requête d'édition du prénom d'un enfant (s30) : l'identifiant stable (clé, jamais
    /// éditable) et le nouveau prénom. Refus métier (prénom vide / doublon d'un autre enfant) renvoyé.</summary>
    public sealed record EditerEnfantRequete(string EnfantId, string NouveauPrenom);

    /// <summary>Corps de la requête de liaison d'un enfant à un parent-acteur (s34) : l'id stable de l'enfant
    /// et l'id stable de l'acteur. Refus métier (inexistant / non-parent / 2 parents max) renvoyé ; déjà lié = neutre.</summary>
    public sealed record LierEnfantParentRequete(string EnfantId, string ActeurId, PlanningDeGarde.Application.RoleDuLien Role = PlanningDeGarde.Application.RoleDuLien.ParentLibre);

    /// <summary>Corps de la requête de retrait du lien d'un enfant vers un parent-acteur (s34) : l'id stable de
    /// l'enfant et l'id stable de l'acteur. Idempotent côté handler (parent déjà non lié = no-op qui réussit).</summary>
    public sealed record DelierEnfantParentRequete(string EnfantId, string ActeurId);

    /// <summary>Corps de la requête d'édition d'un acteur émise via le canal d'écriture. Le nom et la
    /// couleur sont deux champs optionnels et indépendants : un champ absent (null) n'est pas appliqué
    /// (renommage seul au Sc.1, recoloriage seul au Sc.2). L'identifiant stable n'est jamais éditable.</summary>
    public sealed record EditerActeurRequete(string ActeurId, string? Nom = null, string? Couleur = null, string? Adresse = null);

    /// <summary>Corps de la requête d'ajout d'un acteur neuf au foyer émise via le canal d'écriture.
    /// Le front ne fournit que le nom (+ couleur optionnelle) : l'identifiant stable neuf est généré
    /// côté handler, jamais dérivé du libellé.</summary>
    public sealed record AjouterActeurRequete(string Nom, string? Couleur = null);

    /// <summary>Corps de la requête de suppression d'un acteur du foyer émise via le canal d'écriture.
    /// La clé est l'<b>identifiant stable opaque</b> (jamais le libellé, règle 19) ; la suppression est
    /// idempotente côté handler (id absent / déjà supprimé = no-op qui réussit).</summary>
    public sealed record SupprimerActeurRequete(string ActeurId);

    /// <summary>Corps de la requête de définition / ré-édition du cycle de fond (palier 6) émise via le
    /// canal d'écriture : le nombre de semaines + le mapping index→responsable (identifiant stable bindé
    /// par le sélecteur, jamais le libellé). Une nouvelle définition remplace le cycle courant.</summary>
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
    /// clic-case). La clé est l'<b>identifiant stable</b> de la période (jamais un libellé) ; le nouveau
    /// responsable et les nouvelles bornes décrivent l'état voulu. L'état observé (jeton de concurrence) est
    /// résolu côté API sur cet identifiant — le front n'émet que la cible (aucune règle métier dans l'UI).</summary>
    public sealed record EditerPeriodeRequete(string PeriodeId, string NouveauResponsableId, DateTime NouveauDebut, DateTime NouvelleFin);

    /// <summary>Corps de la requête de création d'un rôle du référentiel (s21) : le front n'émet que le
    /// libellé ; l'identifiant stable neuf est généré côté handler.</summary>
    public sealed record CreerRoleRequete(string Libelle);

    /// <summary>Corps de la requête de renommage d'un rôle : l'identifiant stable (clé) et le nouveau libellé.</summary>
    public sealed record RenommerRoleRequete(string RoleId, string NouveauLibelle);

    /// <summary>Corps de la requête de suppression d'un rôle : l'identifiant stable (clé). Idempotente côté handler.</summary>
    public sealed record SupprimerRoleRequete(string RoleId);

    /// <summary>Corps de la requête de bascule du flag « est rôle parent » d'un rôle (s36, B1) : l'id stable
    /// (clé) et l'état voulu du flag (coche/décoche). Source de vérité de l'éligibilité — jamais le libellé.</summary>
    public sealed record MarquerRoleParentRequete(string RoleId, bool EstParent);

    /// <summary>Corps de la requête d'affectation d'un rôle du référentiel à un acteur (s21) : l'id stable de
    /// l'acteur et l'id stable du rôle du référentiel (jamais un libellé en dur). Un id hors référentiel = rejet.</summary>
    public sealed record AffecterRoleRequete(string ActeurId, string RoleId);

    /// <summary>Corps de la requête de retrait du rôle d'un acteur (s21) : l'id stable de l'acteur ; il retombe
    /// « sans rôle » (repli neutre).</summary>
    public sealed record RetirerRoleRequete(string ActeurId);

    /// <summary>Corps de la requête de création d'un compte utilisateur (s22) : l'id stable de l'acteur et
    /// l'email. L'id stable neuf opaque du compte et le statut « inactif » sont posés côté handler.</summary>
    public sealed record CreerCompteRequete(string ActeurId, string Email);

    /// <summary>Corps de la requête d'activation d'un compte utilisateur (s24) : l'id stable opaque du compte.
    /// Le statut passe Inactif→Actif côté handler (mutation portée par l'agrégat) ; idempotence assumée.</summary>
    public sealed record ActiverCompteRequete(string CompteId);

    /// <summary>Corps de la requête de désactivation d'un compte utilisateur (s41, sens OFF) : l'id stable
    /// opaque du compte. Le statut passe Actif→Inactif côté handler (mutation portée par l'agrégat) ;
    /// idempotence assumée (déjà Inactif = no-op), compte inconnu rejeté.</summary>
    public sealed record DesactiverCompteRequete(string CompteId);

    /// <summary>Corps de la requête de désignation d'un acteur comme admin du foyer (s22) : l'id stable de
    /// l'acteur. L'invariant admin=parent est tranché côté Domain (non-Parent rejeté).</summary>
    public sealed record DesignerAdminRequete(string ActeurId);

    /// <summary>Corps de la requête de dé-désignation d'un admin du foyer (s41, sens OFF) : l'id stable de
    /// l'acteur. La borne « dernier admin » et le refus d'un acteur inconnu sont tranchés côté Domain /
    /// handler (le foyer garde toujours ≥1 admin).</summary>
    public sealed record DeDesignerAdminRequete(string ActeurId);

    /// <summary>Corps de la requête de connexion locale par email (s23) émise via le canal requête/réponse :
    /// l'email saisi dans le bandeau de connexion. Aucune règle métier côté front — l'admission (compte
    /// existant ET Actif) est tranchée côté handler ; le front n'émet que l'email.</summary>
    public sealed record SeConnecterRequete(string Email, string? MotDePasse = null);

    /// <summary>Corps de la requête de demande de récupération de mot de passe (s28, volet 1) émise via le
    /// canal requête/réponse : l'email saisi sur l'écran « mot de passe oublié ». Aucune règle métier côté
    /// front — la réponse est toujours un succès neutre (anti-énumération, tranché côté handler) ; le front
    /// n'émet que l'email et affiche un message neutre fixe.</summary>
    public sealed record DemanderRecuperationRequete(string Email);

    /// <summary>Corps de la requête de redéfinition de mot de passe par jeton (s28, volet 1) émise via le
    /// canal requête/réponse : le jeton reçu par mail (porté par l'URL de l'écran de réinitialisation) et le
    /// nouveau mot de passe saisi. Aucune règle métier côté front — validité du jeton et hachage tranchés
    /// côté handler ; sur refus, le motif est surfacé, sur succès l'utilisateur est invité à se connecter.</summary>
    public sealed record RedefinirMotDePasseRequete(string Jeton, string NouveauMotDePasse);

    /// <summary>Corps de la réponse de succès d'une connexion (s23 ; type ancré s25 Sc.5) : l'id stable de
    /// l'acteur lié au compte connecté, son nom d'affichage résolu côté serveur (« Connecté : &lt;Nom&gt; »
    /// sans recalcul côté UI), et son <b>type</b> (Admin / Parent / Autre) résolu côté serveur — le front
    /// ancre l'identité réelle de la session sur CET acteur et son type (gating suivant le type réel).</summary>
    public sealed record SeConnecterReponse(string ActeurId, string Nom, PlanningDeGarde.Application.TypeActeur Type);
}
