# Acteurs & configuration du foyer

> Sujet **migré** depuis `docs/15-specification.md` (paliers 4/5/8 + règles 1-9) à la migration
> complète des specs. Source de vérité pour le **référentiel des acteurs** (édition, ajout, suppression
> — CRUD complet), sa **persistance Mongo**, le **gating** et l'**impersonation bornée lecture**. Édité
> en diff, jamais réécrit en bloc.

## Contexte

L'**appropriation des acteurs** est **livrée** : noms et couleurs **éditables** depuis un écran de
configuration, la grille (case et légende) suit immédiatement. La **config foyer persistante** est
**acquise** : on peut **ajouter** des acteurs (parent ou « autre » comme la nounou) au-delà du
renommage du seed — l'ajout génère un **identifiant stable neuf** (jamais le libellé) — et la config
foyer **survit au redémarrage** (persistée derrière un adaptateur de droite durable, Mongo, ports
inchangés). **Volatilité de l'édition éteinte ICI, pour la config foyer uniquement** ; le reste du
domaine (slots, périodes, transferts, cycle de fond) reste en mémoire.

Le **CRUD acteurs est complet** (Create + Read + Update + **Delete**) : supprimer un acteur le **retire
du store durable** et **neutralise par repli** ses cases orphelines. Le dernier maillon, une
**impersonation bornée lecture seule**, ferme la **boucle du cycle de vie des acteurs**.

Un **modèle de rôles éditable** est **livré** *(s21)* : un **référentiel de rôles** que le parent
crée / renomme / supprime, chaque rôle porté par un **identifiant stable opaque** (jamais le libellé),
**persisté Mongo** comme la config foyer. Un rôle est **affectable à un acteur** en le **bornant au
référentiel** ; un acteur peut rester **sans rôle** (**neutre assumé**, `RoleDe = null`). **Invariant
majeur** : le rôle est une **caractéristique d'acteur, PAS une responsabilité** — il **n'intervient pas**
dans la résolution grille / légende (ni teinte, ni nom de case, ni légende ne dépendent du rôle ;
priorité **surcharge > fond > neutre** strictement inchangée).

Une **fondation d'identité** est **livrée** *(s22, auth tranche 1)* : un agrégat **`CompteUtilisateur`**
(identifiant **stable opaque**, **email**, **statut** `Actif`/`Inactif` — défaut **Inactif**, un `ActeurId`
**nullable**) **lié 1-1 à un acteur déclaré**, **persisté Mongo** dans la config foyer (mêmes bornes que
le référentiel des acteurs et des rôles). **Gardes de création** : email **requis**, email **unique**,
**acteur inconnu rejeté sans écriture**, **acteur déjà porteur d'un compte rejeté sans écriture** (borne
1-1). Un **invariant PUR Domain** est posé : **l'admin du foyer est obligatoirement un acteur de type
Parent** (agrégat `AdministrationFoyer`, `DesignerAdmin` **refuse un non-Parent AVANT toute mutation**) ;
le **cardinal des admins n'est pas borné** — quand les deux parents sont utilisateurs, **les deux peuvent
être admins** (l'invariant borne le **type**, pas l'unicité). **Cycle de vie** : la **suppression d'un
acteur désassocie** ses comptes (repli propre, `ActeurId = null`, **jamais de compte fantôme** référençant
un acteur absent) ; la **désassociation est idempotente** (deux fois = no-op qui réussit).

Une **connexion locale réelle** est **livrée** *(s23, auth tranche 2a)* : une **commande applicative**
(canal requête/réponse, `SeConnecterCommand` / `SeConnecterHandler`, canal `POST /api/canal/se-connecter`)
ouvre une **session serveur** par **email** d'un `CompteUtilisateur` **Actif** ; le **nom / l'acteur est
résolu côté serveur** (l'appelant ne fournit que l'email). **Gardes de refus (aucune session, motif
clair)** : **email inconnu** (aucun compte) ; **compte non activé** (statut `Inactif`, défaut de création
s22). La **session serveur** (état d'**hôte / requête**, `SessionOuverte` — **pas** un agrégat durable de
domaine, borne anti-cliquet respectée) ancre l'**identité réelle** de la session sur l'**acteur lié 1-1**
au compte (relation s22) ; l'**impersonation bornée lecture** (s14) reste possible **au-dessus** de cette
identité réelle (non contournée). Le **logout** = **destruction de la session** : l'identité effective
retombe **exactement** sur le comportement **non connecté**, **aucune identité résiduelle**. L'**acteur
par défaut** est résolu côté serveur (`ResoudreActeurParDefautQuery`) = l'**acteur du compte connecté**
tant qu'une session existe ; **sans session**, le défaut retombe sur le **comportement actuel** (jamais
l'acteur d'un compte) — **aucune régression** du chemin non connecté ni de l'impersonation bornée s14.
**Store touché = comptes en lecture seule** (déjà Mongo config foyer s22), **aucune persistance neuve**.

L'**activation de compte** (`Inactif → Actif`) est **livrée** *(s24, auth tranche 2 — prise en main)*,
levant le **prérequis d'usabilité** : une **commande applicative** (`ActiverCompteCommand` /
`ActiverCompteHandler`, canal `POST /api/canal/activer-compte`) cible un compte par son **id stable
opaque** (s22) et fait passer le statut `Inactif → Actif` ; la mutation est portée par l'agrégat
**`CompteUtilisateur.Activer()`** *(Domain pur, no-op idempotent si déjà Actif)* et réutilise le port
d'écriture **`IEditeurComptes`** (s22, InMemory + Mongo) — **aucun nouvel agrégat, aucun store neuf**.
**Gardes** : compte **inconnu** rejeté (motif clair, **aucune mutation**) ; compte **déjà Actif** = **no-op
qui réussit** (miroir des suppressions idempotentes s16/s18). Le chemin d'activation est une **bascule par
l'admin / parent** depuis l'**onglet Acteurs**, **Parent-gated** (identité effective, non-régression gating
s14/s20), avec accusé non bloquant « Compte activé », **gating Invité** et **temps réel SignalR** (2ᵉ écran
reflète le statut sans rechargement). La **boucle auth est désormais fonctionnelle E2E** : créer un compte
(naît Inactif) → connexion refusée → activer → connexion réussit (session ouverte).

Le **parcours de connexion** est **refondu en page dédiée** *(s24, auth tranche 2 — UX)* : la route
**`/connexion`** est la **landing par défaut** (`/` redirige vers `/connexion`) ; non connecté, l'app
atterrit sur la page login, **pas** sur le planning. La page **emballe `SeConnecterCommand`** (s23, reste
**email-only** — elle n'ajoute **aucun** mot de passe) ; en succès elle **pré-positionne le sélecteur
d'acteur** (incarnation bornée s14) et **redirige vers `/planning`** ; en refus (email inconnu / compte
Inactif) elle affiche un **motif clair**, **reste sur `/connexion`**, **aucune session**. Le **bandeau de
connexion inline** de `PlanningPartage` (s23) est **retiré** — **un seul chemin d'entrée**. Une fois
connecté, un **menu utilisateur** (`MenuUtilisateur` dans `MainLayout`) surface **nom / acteur** (résolu
serveur s23), l'**accès à la config foyer** et **« Se déconnecter »** (logout s23 = destruction de session
→ retour `/connexion`). L'**état de connexion est partagé dans `SessionPlanning`** (borne anti-cliquet
règle 30, **zéro persistance neuve**). Le **temps réel SignalR lecture** (s20) reste **préservé**.

**Hors scope explicite** (palier 13, à venir — cf. backlog) : **OAuth Google / Apple / Microsoft** (tranche
2b, 3 intégrations externes, secrets/callbacks, non testables runtime local) ; **création de compte en
libre-service** ; **récupération de mot de passe par email** (adaptateur de droite mail, facteur mot de
passe distinct de l'email-only). **Bugs / besoins ouverts s24** (backlog) : le rôle affiché ne suit pas
toujours l'acteur du compte connecté (« connecté en Mamie → rôle Parent ») ; les **routes restent
accessibles sans session** (protection d'accès non encore posée).

> **NB test (s24)** : 3 tests s23 couvrant les **affordances de connexion inline** ont été **retirés**
> (l'inline est délibérément déplacé vers la page dédiée) ; les comportements sont **re-couverts** par les
> Sc.8/9/11 s24. Refactor de test **légitime**, **pas** une perte de couverture.

Les **acteurs fictifs de démo (« Parent A / Parent B ») sont éliminés partout** *(livré)* : aucune
**constante de domaine** n'expose plus ces libellés, et **tout l'affichage** (sélecteurs des dialogs,
grille, légende) résout les responsables **exclusivement** depuis le **store vivant des acteurs
déclarés**, **clé = identifiant stable** (jamais un libellé en dur). Conséquences observables : sur un
**store vide** (runtime Mongo au 1er lancement), la grille est **entièrement neutre**, la légende
**vide**, et les sélecteurs affichent **« Aucun acteur, ajoutez-en. »** — **zéro** acteur fantôme. Une
**référence orpheline** (id stable absent du store) retombe sur le repli **surcharge > fond > neutre
sans nom fantôme** (priorité palier 6 + filtre `Resolvable()` s13). L'**asymétrie seed s15** est
préservée : Mongo démarre vide, le seed InMemory des tests est **conservé mais renommé** en libellés
neutres (id stables).

## Objectif & arbitrage

La **persistance de la config foyer** a été tirée **devant l'usage** (exception **bornée**, observable
direct : survie au redémarrage ; premier client du store durable). La **suppression d'acteur** a opéré
sur ce **même store Mongo** (a exercé une persistance acquise, sans cliquet). L'**impersonation bornée**
n'a tiré **aucune persistance neuve** (état session / mémoire). Détail :
[`objectif-et-arbitrage.md`](objectif-et-arbitrage.md).

## Séquence

- **Palier 4 — Édition des acteurs (en mémoire) *(livré)*** : noms + couleurs éditables, grille relue
  immédiatement (durabilité portée par le palier 5).
- **Palier 5 — Config foyer persistante *(livré)*** : **(a)** ajouter un acteur (id stable neuf) ;
  **(b)** persister la config foyer derrière les ports (`IReferentielResponsables` / `IPaletteCouleurs`
  / `IEditeurConfigurationFoyer` inchangés). Survie au redémarrage tenue. Ajout **sans** suppression
  d'abord, **sans** édition du cycle de fond ici.
- **Palier 8 — CRUD complet & impersonation bornée lecture *(livré)*** : **Delete** (épic É2) +
  **impersonation bornée lecture** (épic É10). Suppression → neutralisation par repli des cases
  orphelines + repli de l'incarnation ; **type d'acteur** surfacé lecture seule depuis le seed.

Texte complet : [`sequence-de-livraison.md` § paliers 4/5/8](sequence-de-livraison.md).

## Mécaniques

- **Acteurs (noms + couleurs) éditables, ajoutables, supprimables** depuis l'écran de config ; ajout =
  identifiant stable neuf ; suppression = retrait du store + neutralisation des cases orphelines par
  repli. Config foyer **persistée** (durabilité **bornée** au référentiel des acteurs).
- **Écran de config organisé en trois onglets par thème** *(livré s20)* : **Acteurs** (CRUD acteurs),
  **Période de garde** (cycle de fond), **Slot récurrent** (**placeholder réservé** — aucune écriture ni
  persistance, tient la structure sans fonctionnalité neuve). Onglet **Acteurs actif par défaut** ;
  contenu **cloisonné par rendu conditionnel** (le contenu existant est **réparti**, rien perdu ni
  dupliqué) ; le passage d'un onglet à l'autre **ne perd pas** l'état et **ne casse aucune** écriture.
  Réorganisation **iso-fonctionnelle** : **aucun handler neuf**, aucune règle métier ni persistance neuve.
- **Sélecteur d'édition de la config convergé sur le store vivant unifié** *(livré s20)* : l'énumération
  des acteurs éditables de l'écran de config lit **exclusivement** `IEnumerationActeursFoyer` (id stable),
  **même source** que les sélecteurs des dialogs, la grille et la légende. `Foyer.ActeursEditables` est
  **retirée** → **un seul chemin de lecture** du référentiel acteurs, cohérence stricte
  config ↔ dialogs ↔ grille ↔ légende. L'écran de config est **abonné au hub SignalR de lecture** :
  ajout/renommage depuis un 2ᵉ écran **ré-énumère** son sélecteur **sans rechargement**.
- **Toutes les écritures de l'écran de config** (édition, ajout, édition du cycle de fond, suppression)
  sont **gatées sur l'identité effective**, **sur chaque onglet** (durcissement complet, plus seulement
  le bouton supprimer ; non-régression du gating config s14 tenue par onglet).
- **Impersonation bornée lecture** : session = identité **réelle** (configurateur fixe) vs identité
  **effective** (acteur incarné, ou **repli sur la réelle**) ; bandeau « Vous incarnez X » ; droit
  d'écriture dérivé du type de l'identité effective ; retour à l'identité réelle ; repli auto sur
  suppression concurrente ; **aucune écriture « au nom de »**, état en session / mémoire.
- **Référentiel de rôles éditable + affectation** *(livré s21)* : dans l'**onglet Acteurs**
  (**Parent-gated**), le parent **crée / renomme / supprime** des rôles (id stable opaque, persisté
  Mongo). **Rejets sans écriture** : libellé **vide** ou **doublon** de libellé. Un rôle se **affecte à
  un acteur** en étant **borné au référentiel** — un id de rôle **hors référentiel est rejeté sans
  écriture**. Un acteur **sans rôle** est **neutre assumé** (`RoleDe = null`). **Suppression d'un rôle** :
  les acteurs qui le portaient **retombent sans rôle** (repli neutre, aucun rôle fantôme) **puis** le
  rôle est retiré du référentiel. **Temps réel SignalR** : liste des rôles et sélecteurs de rôle d'un
  2ᵉ écran **convergent sans rechargement**. **Invariant** : le rôle **n'intervient pas** dans la
  résolution grille / légende (caractéristique d'acteur, pas responsabilité).

- **Fondation identité — compte utilisateur ↔ acteur** *(livré s22, auth tranche 1)* : agrégat
  **`CompteUtilisateur`** = petit agrégat de config foyer (miroir du CRUD acteurs et du référentiel de
  rôles), doté d'un **id stable opaque** + **email** + **statut** (`Actif`/`Inactif`, défaut **Inactif** —
  l'activation viendra avec la prise en main de compte, palier 13) + un **`ActeurId` nullable**. Ports
  dédiés (lecture `IEnumerationComptes`, écriture `IEditeurComptes` : créer / associer / désigner-admin /
  désassocier), **deux adaptateurs de droite** (InMemory tests **+** Mongo runtime), **bornés à la config
  foyer** (aucune persistance neuve hors config foyer). **Association 1-1** : le compte **référence l'id
  stable d'un acteur déclaré** ; un acteur porte **au plus un** compte. **Gardes de création (rejet sans
  écriture)** : email **vide**, email en **doublon**, **acteur inconnu**, **acteur déjà porteur** d'un
  compte. **Invariant admin = Parent (PUR Domain)** : l'agrégat **`AdministrationFoyer`** refuse via
  `DesignerAdmin` un acteur **non-Parent AVANT toute mutation** (rejet sans écriture, motif clair) ; le
  **cardinal des admins n'est pas borné** (deux parents utilisateurs → deux admins possibles). **Repli à la
  suppression de l'acteur** : les comptes de l'acteur supprimé retombent **désassociés** (`ActeurId = null`,
  pas de compte fantôme), **désassociation idempotente**. **IHM onglet Acteurs** : **création/association
  d'un compte** (email obligatoire, statut inactif affiché ; échec API → formulaire reste ouvert avec motif
  clair) + **désignation de l'admin** ; l'un et l'autre **Parent-gated** et **gatés « Invité »**
  (durcissement gating config s14, gating par onglet s20 — non-régression) ; **temps réel SignalR** (compte
  créé / admin désigné / compte désassocié convergent sur un 2ᵉ écran sans rechargement). **Hors scope
  (auth tranche 2)** : **OAuth 3 providers**, **page de connexion custom**, **sessions HTTP / logout**,
  **acteur par défaut = utilisateur connecté**.

*Texte complet des mécaniques transverses :* [`mecaniques-de-base.md`](mecaniques-de-base.md).

## Règles de gestion (catalogue : `regles-de-gestion.md`)

- **R1 Multi-enfants · R2 Familles recomposées · R3 Toujours deux parents** (composition du foyer).
- **R4 — Acteurs « autres » ajoutables, éditables et supprimables.**
- **R5 — Édition des acteurs (noms + couleurs)** : grille relue immédiatement, store vivant partout,
  type surfacé lecture seule. Distincte de la durabilité (R30). **Précisé s19** : sélecteurs des
  dialogs, grille et légende résolvent **exclusivement** depuis le **store vivant des acteurs déclarés**
  (id stable, jamais un libellé en dur) ; **aucun** acteur fictif « Parent A/B » dans le domaine ni
  l'IHM ; store vide → message **« Aucun acteur, ajoutez-en. »**, grille neutre, légende vide, zéro
  fantôme. Repli des références orphelines : **surcharge > fond > neutre sans nom fantôme**
  (cf. [`periodes-et-cycle-de-fond.md`](periodes-et-cycle-de-fond.md), R15bis). **Convergence achevée
  s20** : le **sélecteur d'édition de l'écran config** lit lui aussi `IEnumerationActeursFoyer`
  (`Foyer.ActeursEditables` retirée) → **un seul chemin de lecture** du référentiel, temps réel SignalR
  compris.
- **R6 — Ajout & suppression d'acteur, neutralisation par repli (cases ET incarnation), persistance
  bornée** : id stable neuf, persistance Mongo bornée, suppression autorisée & idempotente, repli des
  cases orphelines + de l'incarnation, accusé « Acteur supprimé », pas de réaffectation auto.
- **R7 — Responsabilité de fond en config, exception au calendrier** (cycle déclaré en config ;
  sélecteurs sur identifiant stable).
- **R8 — Trois types d'acteurs & impersonation bornée lecture** (identité réelle vs effective, bornes
  dures, hors-cap = écriture « au nom de »).
- **R9 — Modification réservée aux parents/admin, gating sur l'identité effective** (durcissement
  config livré).
- **R10 — Modèle de rôles éditable, borné au référentiel, hors résolution** *(livré s21)* :
  référentiel de rôles créable / renommable / supprimable par le parent (id stable opaque, persisté
  Mongo) ; rejet **sans écriture** du libellé vide ou en doublon ; rôle affecté à un acteur **borné au
  référentiel** (id hors référentiel rejeté sans écriture) ; acteur **sans rôle = neutre** (`RoleDe =
  null`) ; **suppression d'un rôle → acteurs porteurs retombent sans rôle** (repli neutre) puis rôle
  retiré ; gestion + affectation **Parent-gated** (onglet Acteurs) ; temps réel SignalR (2 écrans
  convergent). **Invariant : le rôle est une caractéristique d'acteur, PAS une responsabilité — il
  n'intervient pas dans la résolution grille / légende.**

- **R11 — Fondation identité : compte utilisateur ↔ acteur & admin = Parent** *(livré s22, auth
  tranche 1)* : agrégat `CompteUtilisateur` (id stable opaque, email, statut `Actif`/`Inactif` défaut
  Inactif, `ActeurId` nullable) **lié 1-1 à un acteur déclaré**, **persisté Mongo** (borné config foyer).
  **Création rejetée sans écriture** si email vide, email en doublon, acteur inconnu, ou acteur déjà
  porteur d'un compte. **Invariant PUR Domain** : l'admin du foyer est **obligatoirement de type Parent**
  (`AdministrationFoyer.DesignerAdmin` refuse un non-Parent avant mutation) ; **cardinal admins non borné**
  (deux parents admins possibles). **Suppression d'un acteur → ses comptes se désassocient** (`ActeurId =
  null`, pas de fantôme), désassociation **idempotente**. Gestion + désignation **Parent-gated** (onglet
  Acteurs, gating Invité), temps réel SignalR. **Hors scope (tranche 2)** : OAuth Google/Apple/Microsoft,
  page de connexion custom, sessions HTTP/logout, acteur par défaut = utilisateur connecté.

- **R12 — Connexion locale, session serveur & acteur par défaut = utilisateur connecté** *(livré s23,
  auth tranche 2a)* : **connexion** par **email** d'un `CompteUtilisateur` **Actif** via commande
  applicative (`SeConnecterCommand` / `SeConnecterHandler`, canal `POST /api/canal/se-connecter`) ; le
  **nom / l'acteur est résolu côté serveur** (l'appelant ne fournit que l'email). **Refus (aucune session,
  motif clair)** : **email inconnu** ; **compte non activé** (statut `Inactif`). La **session serveur**
  (`SessionOuverte`, état d'**hôte / requête**, **pas** un agrégat durable) ancre l'**identité réelle** sur
  l'**acteur lié 1-1** au compte (s22) ; l'**impersonation bornée s14** reste possible **au-dessus** (non
  contournée). **Logout** = **destruction de session** → identité effective retombe **exactement** sur le
  comportement **non connecté**, **aucune identité résiduelle**. **Acteur par défaut** résolu serveur
  (`ResoudreActeurParDefautQuery`) = l'**acteur du compte connecté** tant qu'une session existe ; **sans
  session**, défaut = **comportement actuel** (jamais l'acteur d'un compte) — **aucune régression** du
  chemin non connecté ni de l'impersonation bornée s14. **IHM** : bandeau de connexion custom dans
  `PlanningPartage` (email + connexion / déconnexion + état / motif) ; sélecteur d'acteur **pré-positionné**
  sur l'acteur du compte via impersonation bornée s14. **Temps réel SignalR lecture préservé** (s20).
  **Store = comptes en lecture seule** (Mongo config foyer s22), **aucune persistance neuve**. **Hors scope
  (tranche 2b / palier 13)** : OAuth 3 providers ; **création de compte libre-service** ; **récupération de
  mot de passe par email**.

- **R13 — Activation de compte, page de connexion dédiée & menu utilisateur** *(livré s24, auth tranche 2 —
  prise en main + UX)* : **activation** `Inactif → Actif` par **commande applicative** (`ActiverCompteCommand`
  / `ActiverCompteHandler`, canal `POST /api/canal/activer-compte`) ciblant un compte par **id stable opaque**
  (s22) ; mutation portée par **`CompteUtilisateur.Activer()`** (Domain pur, **no-op idempotent** si déjà
  Actif), réutilisant le port **`IEditeurComptes`** (s22, InMemory + Mongo) — **aucun agrégat ni store neuf**.
  **Gardes** : compte **inconnu** → refus motif clair, **aucune mutation** ; **déjà Actif** → **no-op qui
  réussit**. Chemin d'activation = **bascule admin / parent** depuis l'**onglet Acteurs**, **Parent-gated**
  (accusé « Compte activé », gating Invité, temps réel SignalR). **Boucle auth E2E fonctionnelle** (créer
  Inactif → refus → activer → connexion réussit). **Page de connexion dédiée** : route **`/connexion`** =
  **landing par défaut** (`/` redirige vers `/connexion`) ; emballe `SeConnecterCommand` (s23, **email-only**,
  pas de mot de passe) ; succès → pré-positionne le sélecteur (impersonation bornée s14) + redirige
  `/planning` ; refus (email inconnu / compte Inactif) → motif clair, reste sur `/connexion`, **aucune
  session**. **Bandeau login inline de `PlanningPartage` retiré** (un seul chemin d'entrée). **Menu
  utilisateur** (`MenuUtilisateur` dans `MainLayout`) : nom / acteur + accès config foyer + « Se déconnecter »
  (logout s23 → retour `/connexion`). État de connexion partagé dans `SessionPlanning` (borne anti-cliquet
  R30, **zéro persistance neuve**). **Hors scope (palier 13, cf. backlog)** : OAuth 2b, création de compte
  libre-service, récupération de mot de passe par email. **Ouverts s24** : rôle affiché ≠ acteur du compte
  connecté (bug « Mamie → Parent ») ; routes accessibles sans session (protection d'accès non posée).

## Risques

- **Auth utilisable de bout en bout livrée (s22 fondation + s23 session + s24 activation & page login)** :
  la boucle est **fonctionnelle E2E** (créer Inactif → activer → connexion réussit → session), la page
  `/connexion` dédiée est la landing, le menu utilisateur donne accès config + logout. Le **prérequis
  d'usabilité bloquant (activation) est levé** ; l'**UX auth jugée peu naturelle s23 est corrigée** (page
  dédiée + retrait du bandeau inline). **Reste hors scope (palier 13, cf. backlog)** : **OAuth 2b** (Google
  / Apple / Microsoft, 3 intégrations externes, secrets/callbacks, non testables runtime local — la session
  s23 fournit le socle) ; **création de compte libre-service** ; **récupération de mot de passe par email**
  (adaptateur de droite mail, facteur mot de passe distinct de l'email-only). *(Plus tard, non spécifié : le
  PO envisage un **envoi de mail d'activation** — description à préciser ultérieurement.)*
- **Bug ouvert s24 — rôle ≠ acteur du compte connecté** : « connecté en Mamie, j'ai le rôle Parent ».
  L'identité effective / le rôle affiché ne suit pas toujours l'acteur du compte connecté (recoupe la
  cohérence config → planning s21). À investiguer / corriger (backlog).
- **Besoin ouvert s24 — protection d'accès aux routes** : « les pages sont toutes accessibles même sans
  être loggé ». L'app pose la landing `/connexion` mais **ne garde pas** les routes (planning, config)
  contre un accès non authentifié. Protection d'accès par route (guard / redirection) à poser (backlog).
- **Auth tranche 1 livrée (fondation identité)** : la relation identité ↔ acteur posée en s22 a rendu le
  couplage « défaut = moi » trivial (concrétisé s23).
- **Impersonation bornée lecture livrée — frontière avec l'auth réelle (palier 16) ; écriture « au nom
  de » = hors-cap** (décision PO explicite, candidat G1).
- **Borne anti-cliquet** : la persistance reste **bornée à la config foyer** ; le reste du domaine en
  queue (paliers « config foyer durable — reste » puis « persistance réelle »).
- **Variantes refus/réaffectation de suppression** = porte G1 au make-gherkin si un vrai trou émerge.
- **Évolutions de surface config non priorisées** : palette/picker, refonte visuelle profonde du thème,
  contenu réel de l'onglet **Slot récurrent** (placeholder réservé au s20, aucune fonctionnalité neuve).
- **Rôle sans effet fonctionnel encore** *(s21)* : le rôle est **livré comme caractéristique** (pas une
  responsabilité) — il n'a **pas encore** de droits/comportements attachés. Le couplage rôle → droits
  par acteur (nounou / grand-parent / second parent) vit dans **É10 (auth, palier 13)**, après la prise
  en main de compte.
- ~~**Dette de cohérence (s19)** — sélecteur d'édition config encore sur `Foyer.ActeursEditables`~~ —
  **RÉSOLUE s20** : convergé sur le **store vivant unifié** `IEnumerationActeursFoyer` (`Foyer.ActeursEditables`
  retirée) → **un seul chemin de lecture** du référentiel, cohérence stricte config ↔ dialogs ↔ grille ↔
  légende, temps réel SignalR compris.

Cf. [`risques-et-questions-ouvertes.md`](risques-et-questions-ouvertes.md).
