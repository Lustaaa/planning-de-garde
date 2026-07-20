# Acteurs & configuration du foyer

> Sujet **migré** depuis `docs/15-specification.md` (paliers 4/5/8 + règles 1-9) à la migration
> complète des specs. Source de vérité pour le **référentiel des acteurs** (édition, ajout, suppression
> — CRUD complet), sa **persistance Mongo**, le **gating**, l'**impersonation bornée lecture** et
> l'**authentification / login complet** (fondation identité → mot de passe / libre-service / récupération /
> OAuth, R11→R14). Édité en diff, jamais réécrit en bloc.

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

Un **référentiel de lieux éditable + persisté** est **livré** *(s27)* : miroir strict du référentiel
des acteurs / rôles. Les **lieux** — jusque-là **codés en dur en double** (`Foyer.Lieux` static lu par
`ILieuRepository.Existe`, ET la même liste itérée par les sélecteurs des dialogs côté Web) — sont hissés
en **référentiel foyer** (lecture `IEnumerationLieux` + écriture `IEditeurLieux`, **id stable** + libellé),
réalisé par les **mêmes stores** que les acteurs (`ConfigurationFoyerEnMemoire` **seedé** / `ConfigurationFoyerMongo`
**durable sans seed**). Ce **canal vivant unique** pilote **à la fois** la **validation de pose** (poser un
slot sur un lieu **inconnu** est refusé sans écriture) **et** les **sélecteurs** des dialogs `PoserSlot` **et**
`DefinirTransfert` (temps réel SignalR lecture : ajout/suppression convergent **sans rechargement**).
**Rejets sans écriture** : libellé **vide** ou **doublon** de libellé (miroir des rejets acteurs R5/R6 et
rôles R10). **Borne** : un slot **déjà posé** sur un lieu supprimé **conserve** son lieu (aucune réécriture
rétroactive). L'ancien `ILieuRepository` / `FoyerLieuRepository` **en dur est retiré** — **un seul chemin de
lecture** du référentiel des lieux. **Conséquence de parité (asymétrie seed s15)** : en mode **Mongo** le foyer
**part sans lieux** (aucun seed), donc **aucun slot n'est posable tant qu'un lieu n'est pas configuré** ;
l'InMemory des tests conserve son seed pour la non-régression.

Un **référentiel d'enfants éditable + persisté** est **livré** *(s30)* : l'**enfant** est hissé en
**agrégat de config foyer de premier rang**, **miroir strict du hissage des lieux s27**. Jusque-là
l'`EnfantId` (« Léa ») restait **implicite/masqué** : transmis via `Session.EnfantId` au canal
d'écriture **sans jamais être choisi** (fantôme), bloquant dès qu'un foyer a ≥2 enfants. L'agrégat
**`Enfant`** porte un **identifiant stable opaque** (jamais dérivé du prénom) + un **snapshot (prénom)**,
énuméré par un **port d'énumération** de droite et muté par un **port d'édition** (ajouter / éditer),
réalisés par les **mêmes stores** que les acteurs / rôles / lieux (`ConfigurationFoyerEnMemoire` **seedé** /
`ConfigurationFoyerMongo` **durable sans seed**, parité asymétrie seed s15). **Rejets sans écriture** :
prénom **vide** ou **doublon** de prénom (miroir des rejets acteurs R5/R6, rôles R10, lieux). L'enfant
devient **explicite** partout : **onglet « Enfants »** dans la Config du foyer (lister / ajouter / éditer,
rejets visibles sans enregistrer) et **sélecteur d'enfant explicite** dans la dialog de pose (slot
ponctuel comme récurrent) qui **remplace l'`EnfantId` implicite « Léa »** (fantôme `Session.EnfantId`
retiré, choix transmis au canal d'écriture existant). Ce **canal vivant** pilote **la validation de pose**
contre le référentiel : poser un slot référençant un enfant **inconnu** du foyer est **refusé sans
écriture** (ponctuel ET récurrent, miroir de la validation « lieu inconnu » s29). Une **migration de
rétro-affectation idempotente** réattache les slots existants du fantôme « Léa » à l'**identifiant stable
de l'enfant réel** (rejeu = no-op), **prouvée sur store Mongo réel** ; elle reste un **utilitaire ops**
(non auto-câblé au runtime) et l'enfant par défaut du sélecteur reste le seed « Léa ». **Borne R1 (≥1
enfant)** tenue par construction (le fantôme devient le premier enfant réel) ; la **suppression** d'un
enfant et sa borne défensive au Delete sont **hors scope s30** (reportées). Le **vrai multi-enfants** au
sens spec R1 (familles recomposées, graphe de parents, multi-enfants dans le cycle de fond) n'est **pas
encore exercé** au-delà de l'agrégat.

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

- **Onglet Acteurs — patron tableau lecture seule + crayon → modal** *(livré s32, 1er incrément de la
  Refonte Config foyer, cohérent avec le patron dialogs s11-s12)* : l'**édition inline** des acteurs
  (les deux cartes « Modifier » / « Ajouter » + contrôles inline dans la table) est **remplacée** par un
  **tableau en LECTURE SEULE** — une ligne par acteur (pastille couleur + nom, email + statut du compte,
  rôle, état actif/admin **matérialisé en pastille lecture**) — doté d'une colonne **Actions → crayon**
  par ligne et d'un bouton **« Ajouter un acteur »**. Le crayon (ou « Ajouter ») ouvre une **MODAL**
  pré-remplie des **champs COURANTS** (nom, couleur, rôle **borné au référentiel** : les rôles du foyer +
  « sans rôle »), ou **vide** en mode création. L'**identifiant stable** est porté par la modal **sans
  être éditable** (jamais dérivé du libellé). **« Enregistrer » émet les commandes CRUD acteurs
  EXISTANTES** (aucun handler ni query neuf) via le canal HTTP : en **succès** la modal se ferme, le
  tableau est **relu** et la grille/légende partagée suit la nouvelle couleur/le nouveau nom **sans
  rechargement** (renommage = **id stable inchangé**, ajout = **id stable neuf**). **Contrat d'erreur**
  (refus domaine — nom vide/doublon — ou API injoignable) : la **modal RESTE OUVERTE**, le **motif est
  affiché dedans**, la **saisie est CONSERVÉE**, et **aucune écriture partielle** ne touche le tableau ni
  la grille. **Fermer/annuler** n'émet **aucune** commande. **Parent-gated** (identité effective, non-
  régression gating s14/s20) : l'**Invité** (non-Parent) garde le tableau **en lecture seule** mais **ni
  crayon, ni « Ajouter »**, aucune modal d'écriture atteignable. **Temps réel SignalR** : l'édition/l'ajout
  depuis un 2ᵉ écran fait **converger** le tableau (ligne mise à jour ou ajoutée) **sans rechargement**
  (lecture s20 préservée, aucune écriture par la diffusion). **Hors 1er incrément (reporté 2ᵉ incrément)** :
  état actif/admin en **toggle** *dans* la modal (ici pastille lecture seule), **adresse de résidence**
  [champ neuf], **couleur vraie palette** [picker], et harmonisation **Rôles / Cycle / Enfants** sur le
  même patron. **Volet en tension à arbitrer (retour PO gate s32)** : réintroduire une **édition inline au
  clic de la valeur** *en plus* de la modal — **choix de direction non tranché** (cf. backlog).

- **Config foyer — Acteurs enrichis + Rôles & Cycle harmonisés** *(livré s33, 2ᵉ incrément de la Refonte
  Config foyer)* :
  - **Acteurs — modal enrichie.** L'état **actif/admin** passe de **pastille lecture** à **TOGGLE éditable
    DANS la modal**, pré-réglé sur l'état courant. **Sens ON à s33** : « Enregistrer » émet les **commandes
    existantes** de **désignation d'admin** / **d'activation de compte**. *(⚠️ **Sens OFF verrouillé à s33**
    faute de commande inverse — un OFF « no-op silencieux » aurait été un vert-qui-ment ; ce **verrou est
    SUPERSÉDÉ s41** : le toggle est désormais **bi-directionnel**, le OFF émet la vraie commande inverse
    — voir le bloc s41 dédié ci-dessous.)* Le toggle **« actif »** n'est actionnable que si l'acteur **porte
    un compte** (sinon désactivé, motif dedans). Un **champ « adresse de résidence »** [**champ de modèle
    neuf**] est porté par l'agrégat acteur, **persisté Mongo durable**, relu par la query de config, éditable
    dans la modal et **rendu dans le tableau lecture** ; **adresse vide acceptée** (optionnel, sans écriture
    partielle des autres champs). La **couleur** passe en **palette / picker minimal** (choix borné au **set
    de couleurs**, couleur courante pré-sélectionnée, persistée via la commande existante ; **pas** de palette
    custom — créer/renommer/supprimer des couleurs **hors scope**) — **solde la dette « set couleurs par
    défaut »**. **Contrat d'erreur** inchangé (refus → modal RESTE OUVERTE, motif dedans, saisie
    dont adresse/toggle/couleur CONSERVÉE, aucune écriture partielle), **Parent-gated** + **temps réel
    SignalR** convergent sur les champs neufs.
  - **Rôles & Cycle au patron tableau lecture seule + crayon → modal** (miroir Acteurs s32, lot atomique de
    surface). **Rôles** : une ligne par rôle en lecture seule + crayon → modal pré-remplie (édition via les
    commandes de rôle **existantes** s21) + « Ajouter un rôle » (modal vide → id stable neuf). **Cycle** : un
    **TABLEAU en lecture seule rend VISIBLES TOUS les cycles / affectations déclarés** (une ligne par semaine
    {index → responsable}) — **corrige le trou du gate s32** (des affectations déclarées n'apparaissaient pas
    dans la config) ; le crayon « Éditer le cycle » ouvre une **modal hébergeant l'éditeur `definir-cycle`
    existant tel quel** (nombre de semaines N + un select responsable par semaine, pré-remplis). Le
    **write-path `definir-cycle` reste ATOMIQUE** (N + toutes les affectations, pas de découpage per-semaine).
    Libellés d'affichage : **« Semaine paire / impaire »** sur les seuls index 0/1 (cas ISO 2 semaines),
    « Semaine d'index k » au-delà. Invariants réutilisés : refus → modal ouverte, **Parent-gated** (Invité =
    lecture seule sans crayon ni « Ajouter »), **temps réel SignalR** convergent. **Hors scope** : édition
    avancée du cycle (ancre / frontière de jour / plage / sur-cycle vacances / WE-only — cf. « cycle de fond
    riche »).
  - **Fermeture Échap des modals de Config foyer** (Acteurs, Rôles, Cycle) : la touche **Échap** ferme la
    modal **strictement comme « Annuler »** — fermeture **SANS mutation** (aucune commande émise, saisie
    abandonnée), jamais confondue avec « Enregistrer », effective **même en état de refus** (motif affiché,
    saisie conservée). Capture au niveau **document** via le **port `IEcouteurEchapModal`** (adaptateur JS
    `document.addEventListener('keydown')` → rappel .NET), **attaché à l'ouverture / détaché à la
    fermeture** (aucune fuite). *NB méthode : un premier `@onkeydown` bUnit sur le backdrop passait vert en
    test mais **jamais** en navigateur réel (l'événement part de `document`, pas du div non focus) → capture
    document via port ; preuve finale = **gate navigateur PO**.*
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

- **Référentiel de lieux éditable + persisté** *(livré s27)* : ports dédiés (lecture `IEnumerationLieux`,
  écriture `IEditeurLieux` — ajouter / supprimer), **deux adaptateurs** (InMemory **seedé** / Mongo **durable
  sans seed**), **bornés à la config foyer** (miroir acteurs / rôles / comptes). **Canal vivant unique** qui
  pilote **la validation de pose** (slot sur lieu inconnu refusé sans écriture) **et** les **sélecteurs** de
  `PoserSlotDialog` **et** `DefinirTransfertDialog` (temps réel SignalR lecture, convergence sans rechargement).
  **Rejets sans écriture** : libellé vide ou en doublon. **Borne** : un slot déjà posé sur un lieu supprimé
  **conserve** son lieu. `Foyer.Lieux` static + `ILieuRepository` / `FoyerLieuRepository` en dur **retirés** →
  un seul chemin de lecture. **Parité seed s15** : mode Mongo sans lieux au départ ⇒ aucun slot posable tant
  qu'aucun lieu n'est configuré. **⚠️ Concept renommé « Activités » en s35** (Domaine + Application ; enrichi
  d'une adresse + d'un lien enfant↔activité + onglet au patron crayon → modal — cf. bloc s35 ci-dessous) ;
  l'**axe LOCALISATION `LieuId`** du slot reste distinct et **non renommé**.

- **Référentiel d'enfants éditable + persisté** *(livré s30, miroir strict du hissage lieux s27)* :
  agrégat **`Enfant`** (**id stable opaque** + **snapshot prénom**, jamais dérivé du libellé), **port
  d'énumération** (lecture) + **port d'édition** (ajouter / éditer), **deux adaptateurs** (InMemory **seedé** /
  Mongo **durable sans seed**), **bornés à la config foyer**. **Rejets sans écriture** : prénom **vide** ou en
  **doublon**. **IHM onglet « Enfants »** (Config foyer) : lister / ajouter / éditer, rejets visibles sans
  enregistrer. **Sélecteur d'enfant explicite** dans la dialog « Poser un slot » (ponctuel + récurrent) qui
  **remplace le fantôme `Session.EnfantId` (« Léa »)** — l'enfant n'est plus implicite, le choix est transmis
  au canal d'écriture existant. **Validation de pose** : slot référençant un enfant **inconnu** du foyer refusé
  **sans écriture** (miroir lieu inconnu s29). **Migration rétro-affectation idempotente** des slots du fantôme
  vers l'id réel (rejeu = no-op, **prouvée store Mongo réel**) — **utilitaire ops non auto-câblé**. **Parité
  seed s15** : mode Mongo sans enfant seedé au 1er lancement. **Hors scope s30** : suppression d'enfant + borne
  défensive R1 au Delete ; vrai multi-enfants de bout en bout.

- **Lien enfant↔parent + onglet Enfants au patron crayon → modal** *(livré s34, 3ᵉ incrément de la Refonte
  Config foyer)* : l'enfant n'est plus l'agrégat **nu** `EnfantFoyer(Id, Prénom)` sans lien — il porte
  désormais un **lien vers 1..2 parents-acteurs**. Un **parent** = un acteur portant le **rôle Parent**
  (référentiel de rôles s21). *(⚠️ **Éligibilité RÉVISÉE s36** : « rôle Parent » ≡ match du **libellé
  littéral** « Parent », rejeté au gate → remplacé par le **flag « rôle parent »** sur `RoleFoyer` — voir
  le bloc s36 dédié ci-dessous.)* Commandes/handlers **lier / délier** (canal requête/réponse : `enfantId`,
  `acteurId`), lien **persisté Mongo durable** (relu par la query de config avec la **liste des parents
  liés**), **id stable de l'enfant inchangé** (enrichissement, pas recréation), **0 parent accepté** (lien
  optionnel). **Rejets SANS écriture partielle** (motif restitué, store inchangé, les liens existants
  intacts) : **3ᵉ parent** (borne « 2 parents max »), **acteur inexistant** du référentiel, **acteur
  non-Parent**, **parent déjà lié** (neutre / refusé, pas de doublon). **Délier est idempotent** (délier un
  parent déjà non lié = no-op qui réussit, sans écriture partielle), les autres liens éventuels préservés.
  **IHM — onglet « Enfants » harmonisé** au patron **tableau lecture seule + crayon → modal** (miroir Acteurs
  s32 / Rôles-Cycle s33, **lot atomique de surface** : l'édition **inline** préexistante — liste `<ul>`,
  `champ-editer-enfant`, `form-ajouter-enfant` — est **retirée** et remplacée par la modal dans le **même
  refactor**, mêmes commandes d'écriture). Une ligne par enfant (prénom + **colonne « Parents liés »** en
  lecture) + colonne **crayon** + bouton **« Ajouter un enfant »** ; crayon → **modal pré-remplie**, « Ajouter »
  → modal **vide** (id stable neuf), fermeture **Échap** = « Annuler » sans mutation (port `IEcouteurEchapModal`
  s33). La modal porte un **sélecteur des parents** (acteurs de rôle Parent, référentiel acteurs), parents
  déjà liés pré-affichés ; lier/délier puis « Enregistrer » émet les commandes ci-dessus via le canal HTTP,
  le tableau en lecture reflète la nouvelle liste. **Contrat d'erreur** (prénom vide/doublon, 3ᵉ parent,
  non-parent, API injoignable) : **modal RESTE OUVERTE**, motif dedans, **saisie (prénom + sélection)
  CONSERVÉE**, tableau inchangé (aucune écriture partielle). **Parent-gated** (Invité = tableau lecture seule,
  ni crayon ni « Ajouter », aucune modal atteignable) + **temps réel SignalR** (2ᵉ écran converge sur prénom
  et parents liés sans rechargement, diffusion lecture seule). **Hors scope s34** : familles recomposées
  **R2/R3** (le sélecteur borne à 2 parents mais **n'impose pas « exactement 2 »**), **graphe enfant-racine**,
  renommage Lieux → Activités, suppression d'enfant.

- **Référentiel « Lieux » renommé « Activités » + adresse + lien enfant↔activité + onglet au patron
  crayon → modal** *(livré s35, 4ᵉ incrément de la Refonte Config foyer)* : le PO **repense le « lieu »
  comme une « activité » liée à l'enfant** (« lieux n'est pas le bon terme »). Trois volets :
  - **Renommage sémantique iso-comportement.** Le **concept « Lieux »** (référentiel foyer s27) est renommé
    **« Activités »** côté **Domaine + Application** (agrégat, ports `IEnumeration*` / `IEditeur*`, query de
    config, handlers, commandes, **deux adaptateurs** InMemory seedé + Mongo durable). Le **comportement
    reste STRICTEMENT iso** : ajouter / supprimer une activité, **rejets libellé vide / doublon**, **id stable
    + libellé**, et surtout la **validation de pose reste PRÉSERVÉE** (poser un slot sur une activité
    **inconnue** est refusé **sans écriture**, miroir « lieu inconnu » s29) ; une activité déjà référencée par
    un slot posé **conserve** sa référence (aucune réécriture rétroactive). **Seul le nom du concept change.**
    L'**axe LOCALISATION du slot** (`SlotSnapshot.LieuId`, `PoserSlotCommand.LieuId`, grille, transfert) porte
    le **« où » de la garde** — c'est un **axe DISTINCT** du référentiel, **hors périmètre, NON renommé** ce
    sprint (ni back ni HTTP/IHM), préservé iso.
  - **Champ « adresse » sur l'agrégat Activité** (**miroir strict de l'adresse acteur s33**) : **persisté Mongo
    durable**, relu par la query de config, **vide accepté** (optionnel), éditer l'adresse **ne touche aucun
    autre champ** (id + libellé inchangés, aucune écriture partielle ; un refus concomitant laisse le store
    inchangé).
  - **Lien enfant↔activité N-M** : commandes/handlers **lier / délier** (canal requête/réponse : `enfantId`,
    `activiteId`), lien **persisté Mongo durable** (relu par la query de config), **id stables enfant et
    activité inchangés** (enrichissement). Le lien est **N-M** (plusieurs enfants partagent une même activité ;
    un enfant porte plusieurs activités), **optionnel** (0 accepté), **aucune borne de cardinalité** imposée ce
    sprint — **distinct** du lien enfant↔parent s34 (borné 0..2). **Délier est idempotent** (lien déjà absent =
    no-op qui réussit). **Rejets SANS écriture partielle** (motif restitué, liens existants intacts) : **enfant
    OU activité inconnu** du référentiel. La validation de pose reste sur l'**existence de l'activité** (pas sur
    le lien enfant↔activité, non exigé à la pose ce sprint).
  - **IHM — onglet « Activités » harmonisé** au patron **tableau lecture seule + crayon → modal** (miroir
    Acteurs s32 / Rôles-Cycle s33 / Enfants s34, **lot atomique de surface**). L'onglet **« Lieux »**
    préexistant (édition **inline** : `onglet-lieux`/`panneau-lieux`, ajout+suppression inline, `liste-lieux`)
    est **retiré ET** remplacé par le tableau + modal dans le **MÊME commit (SWAP atomique)**, qui **absorbe
    aussi le renommage de surface** non fait en volet back : **routes HTTP `/api/foyer/lieux → activites`**,
    **canal `*-lieu → *-activite`**, **DTOs Api**, **record Web `LieuFoyer → ActiviteFoyer`**, testids/labels
    « Lieux » → « Activités » — les **tests Web de l'inline MIGRANT vers la modal** (même commit). Une ligne par
    activité (**libellé + adresse + colonne « Enfants liés »** en lecture) + colonne **crayon** + bouton
    **« Ajouter une activité »** ; crayon → **modal pré-remplie** (libellé, adresse, enfants liés courants),
    « Ajouter » → modal **vide** (id stable neuf) ; la modal porte le **champ adresse** + un **sélecteur des
    enfants** (référentiel enfants s30, enfants déjà liés pré-affichés) émettant les commandes lier/délier via
    le canal HTTP ; fermeture **Échap** = « Annuler » sans mutation (port `IEcouteurEchapModal` s33).
    **Contrat d'erreur** (libellé vide/doublon, enfant/activité inconnu, API injoignable) : **modal RESTE
    OUVERTE**, motif dedans, **saisie (libellé + adresse + sélection) CONSERVÉE**, tableau inchangé (aucune
    écriture partielle). **Parent-gated** (Invité = tableau lecture seule, ni crayon ni « Ajouter », aucune
    modal atteignable) + **temps réel SignalR** (2ᵉ écran converge sur libellé + adresse + enfants liés sans
    rechargement, diffusion lecture seule).
  > **Carve-out du renommage transverse (2 seams, décision SM).** Le volet back (Sc.1) renomme **Domaine +
  > Application UNIQUEMENT** ; le **nommage HTTP / DTOs / record Web / onglet « Lieux »** reste **inchangé** en
  > Sc.1 — l'adaptateur **Api MAPPE « lieu » HTTP → « Activité » Application** (seam **transitoire, cohérent,
  > vert**, pas une incohérence qui traîne). Ce seam est **remplacé par le vrai nom HTTP « activite »** au
  > **SWAP de surface (Sc.4)**, dans le même lot atomique (pas de churn jetable des testids).
  > **Hors scope s35** : **liste de slots par activité** (récurrents/non — extension du modèle de slots),
  > **lien adresse acteur↔lieu/domicile** de l'enfant en garde, **révision de la validation de pose**
  > (préservée iso ici), familles recomposées **R2/R3** + graphe enfant-racine.

- **Éligibilité « parent » du lien enfant↔parent = flag « rôle parent » sur le rôle** *(livré s36, révision
  d'invariant — retour PO gate s35)* : **qu'est-ce qui rend un acteur liable comme parent** est **redéfini**.
  L'ancienne règle s34 (« l'acteur porte un rôle dont le **libellé** vaut littéralement « Parent » ») est
  **abandonnée** (piège du libellé, rejeté s35). L'**option A** un temps livrée puis **REJETÉE au gate**
  (éligibilité = `TypeActeur.Parent`) est **écartée par construction** : le `TypeActeur` n'est **jamais saisi
  via l'IHM** (`AjouterActeurHandler` ne passe aucun type, `TypeParDefaut = Parent`) → **tout acteur créé par
  l'utilisateur devenait `Parent`** et donc liable à tort (Valérie/nounou, Mamie/grand-parent). Règle
  **retenue (option B1+B2)** :
  - **Flag « est rôle parent » sur `RoleFoyer` (B1)** — chaque rôle du référentiel (s21) porte un **attribut
    booléen** « est un rôle parent », **pilotable** (coche/décoche) et **source de vérité unique** — jamais le
    libellé. Porté par le port de lecture `IEnumerationRoles` (surfacé pour chaque rôle) + écriture
    `IEditeurReferentielRoles.MarquerParent` ; **persisté durablement** sur les **deux adaptateurs** (InMemory
    tests **+** Mongo runtime, round-trip survivant au rechargement ; un rôle antérieur sans flag stocké se
    relit à **false** — défaut neutre, pas de crash). Commande/handler **`MarquerRoleParent`** (`roleId`,
    `estParent`) : bascule le flag, **idempotente** (ré-émettre le même état = neutre, aucun doublon d'écriture),
    **rôle inexistant refusé** sans écriture.
  - **Amorçage au seed (B2)** — à la **création du foyer (seed initial UNIQUEMENT)**, les rôles de libellé
    **Papa / Maman / Parent** démarrent **pré-cochés « rôle parent »** ; les autres (Nounou, Grand-parent…) non.
    Un rôle **créé ensuite** via `CreerRole` démarre **non-parent** (flag à false) **même si son libellé est
    Papa/Maman/Parent** — le pré-cochage ne vaut **que** pour le seed, il ne devient parent que par bascule
    explicite (anti-reconnaissance de libellé à la volée, anti-piège s35). Le **seed démo** affecte désormais un
    **rôle-parent** aux acteurs-parents (Alice→Papa, Bruno→Maman) et Nounou/Grand-parent aux autres, pour qu'ils
    restent liables sous la nouvelle règle.
  - **Règle d'éligibilité** — `LierEnfantParentHandler` accepte un acteur comme parent **ssi il porte un rôle
    marqué « est rôle parent »**. Un acteur à **rôle non marqué** (Nounou, Grand-parent) **ou sans aucun rôle**
    (`RoleId = null`) est **REFUSÉ** (motif restitué, **aucune écriture partielle**, liens existants intacts).
    La **borne 0..2 parents** (s34) reste **inchangée**. Le prédicat `TypeActeur.Parent` **ne qualifie PLUS**
    l'éligibilité (option A retirée) et reste **cantonné au seul gating d'impersonation R8/R9** (`SessionPlanning`,
    droit d'écriture de l'identité effective) — **aucun droit d'écriture n'est gagné/perdu** par cette bascule.
  - **IHM** : le **sélecteur des parents** de la modal Enfants (`ActeursParents()`) énumère **exactement** les
    acteurs à rôle marqué parent (aligné sur la règle back, aucun critère divergent) ; un acteur à rôle non
    marqué ou sans rôle **n'apparaît pas**. Une **case à cocher « rôle parent »** est ajoutée dans la **modal de
    l'onglet Rôles** (patron crayon → modal s33) : reflète l'état courant, émet `MarquerRoleParent` via le canal
    HTTP, **Échap = Annuler** sans mutation (port `IEcouteurEchapModal`), **Parent-gated** (non-Parent =
    lecture seule), **convergence SignalR** temps réel (décocher « parent » retire l'acteur du sélecteur de
    parents en direct, sans rechargement). **Hors scope s36** : champ **père/mère distinct** (distinction par le
    NOM), familles recomposées **R2/R3** + graphe enfant-racine, **saisie/édition du `TypeActeur`** lui-même.

- **Rôle-du-lien père / mère / parent-libre sur le lien enfant↔parent** *(livré s37, 5ᵉ incrément de la
  Refonte Config foyer — suite du lien s34, éligibilité role-flag s36)* : le lien enfant↔parent, jusque-là
  une **collection nue d'`ActeurId`** où les deux parents n'étaient distingués que par leur **NOM**, porte
  désormais un **attribut « rôle-du-lien » ∈ {père, mère, parent-libre}**. Le lien passe d'`ActeurId` à un
  **record `ParentLie`** (`ActeurId` + rôle-du-lien) ; la commande **`LierEnfantParent`** est enrichie d'un
  paramètre **rôle-du-lien** (défaut **« parent-libre »** si absent) et l'enfant porte un **champ additif
  `RolesDesLiens`**. Le rôle-du-lien vit **sur le lien**, jamais sur l'acteur (distinct du `TypeActeur` et du
  flag « rôle parent » s36) ; il est **présentation + distinction**, il **n'intervient PAS** dans la résolution
  grille/légende ni dans le gating (miroir de l'invariant R10 « le rôle est une caractéristique, pas une
  responsabilité »).
  - **Invariant d'exclusivité (Domain pur).** Un même enfant ne peut porter **deux liens « père »** ni **deux
    liens « mère »** : la commande **REFUSE AVANT toute écriture** (motif restitué, store intact, liens
    existants préservés). **« parent-libre » reste répétable** (compat + neutralité). Ce n'est **pas** la
    contrainte R2/R3 « exactement 2 parents » (hors scope) : le seul invariant posé est « pas deux liens de
    même rôle exclusif sur un enfant ». La **borne 0..2 parents** (s34) et l'**éligibilité « rôle parent »**
    (role-flag s36) restent **inchangées** ; modifier le rôle-du-lien d'un parent déjà lié met à jour le lien
    **sans le dupliquer** (id enfant + autres liens intacts). Lier/délier restent **idempotents**.
  - **Compat non destructive + persistance.** Le lien est **persisté Mongo durable** (round-trip du rôle-du-lien
    survivant au rechargement, id enfant inchangé — enrichissement additif). Un lien **déjà persisté par s34 sans
    l'attribut** se relit à **« parent-libre »** (défaut neutre), **jamais** un crash de désérialisation,
    **jamais** de migration destructive du store (comportement s34 strictement préservé : un lien sans rôle
    explicite ≡ ancien lien nu).
  - **IHM — modal Enfants + colonne.** Chaque parent lié porte un **sélecteur rôle-du-lien (père / mère /
    parent)** dans la modal d'édition d'un enfant (patron crayon → modal s34), pré-réglé sur son rôle courant ;
    « Enregistrer » émet `LierEnfantParent` (rôle inclus) via le canal HTTP, la modal se ferme, le tableau est
    relu. **Contrat d'erreur** (deux « père »/« mère », acteur non éligible, API injoignable) : la **modal RESTE
    OUVERTE**, le **motif est affiché dedans**, la **sélection (parents + rôles) est CONSERVÉE**, le tableau
    reste inchangé (aucune écriture partielle) ; **Échap = Annuler** sans mutation (port `IEcouteurEchapModal`
    s33). La colonne **« Parents liés »** du tableau affiche désormais, pour chaque parent, **« nom (rôle) »**.
    **Parent-gated** (Invité = tableau lecture seule, rôles-du-lien visibles, ni crayon ni « Ajouter ») +
    **temps réel SignalR** (2ᵉ écran converge sur nom + rôle-du-lien sans rechargement, diffusion lecture seule).
    **Hors scope s37** : familles recomposées **R2/R3** (« exactement 1 père ET 1 mère » / complétude du couple
    **non exigée**), graphe enfant-racine, **saisie/édition du `TypeActeur`** lui-même.

- **Vue foyer « graphe enfant-racine » (lecture seule) + query agrégée `GrapheFoyerQuery`** *(livré s38, 6ᵉ
  incrément de la Refonte Config foyer — RESTITUTION visuelle du foyer câblé s34/s36/s37)* : à l'arrivée sur la
  Config du foyer, une **vue en LECTURE SEULE** restitue le foyer comme un **graphe avec l'ENFANT en RACINE** et
  ses parents liés en branches — payoff des liens posés au fil des sprints (lien enfant↔parent s34, éligibilité
  role-flag s36, rôle-du-lien père/mère/parent-libre s37).
  - **Query de lecture PURE `GrapheFoyerQuery`.** Une query agrégée restitue, **PAR enfant**, la liste de ses
    **parents liés** avec, pour chacun, son **nom** et son **rôle-du-lien** {père, mère, parent-libre} (s37).
    **Lecture PURE** : aucune mutation, **aucun store neuf**, aucune persistance neuve — elle **compose** les
    données déjà persistées (référentiel enfants s30 + liens enfant↔parent s34/s37 + noms d'acteurs), sur les
    **deux adaptateurs** (InMemory seedé + Mongo durable, même contrat). Un lien s34 sans rôle-du-lien explicite
    est restitué à **« parent-libre »** (défaut neutre s37). Le rôle-du-lien reste **présentation seule** (n'intervient
    ni dans la résolution grille/légende ni dans le gating, R10).
  - **Reflet FIDÈLE, zéro fantôme.** Le graphe résout les branches **exclusivement** depuis le store vivant (id
    stable) : un parent **non lié** est **absent** ; un acteur **supprimé / orphelin** ne laisse **aucun nom
    fantôme** (miroir R5/R6, filtre `Resolvable()` s13) ; un enfant **sans parent** est une **racine isolée**
    légitime (0 parent accepté s34) ; un **store vide** restitue un **graphe vide** (aucune racine, sans erreur).
  - **IHM — surface NEUVE de lecture** (pas un swap, aucune garde lot-atomique : on **ajoute** une vue). Chaque
    **enfant en RACINE**, ses parents en branches « **nom (rôle-du-lien)** » (père / mère / parent) ;
    **STRICTEMENT lecture** (aucun contrôle d'édition, aucune commande émise depuis le graphe — l'écriture reste
    dans la modal Enfants s34/s37) ; **store vide → message neutre** (« Aucun enfant, ajoutez-en. »), zéro nœud
    fantôme. **Familles recomposées VISIBLES par construction** : deux enfants de parents distincts = **deux
    racines** ; un parent partagé apparaît **sous chacun** des enfants qu'il a (reflet fidèle, **aucun nouvel
    invariant** imposé — ni « exactement 2 parents » ni complétude du couple). **Parent-gated lecture** : l'Invité
    **voit** la vue (lecture seule, sans contrôle d'édition). **Convergence SignalR par REPROJECTION CLIENT** :
    lier / délier / changer un rôle-du-lien depuis la modal Enfants fait **converger le graphe d'un 2ᵉ écran sans
    rechargement** — le client **reprojette depuis le payload diffusé** (diffusion lecture seule s20, **aucun GET
    sur push**, canal d'écriture jamais confondu).
  - **R1 exercé en LECTURE, R2 visible, R3 ouvert.** Le multi-enfants / graphe foyer (R1) est désormais **exercé
    en LECTURE** (au-delà de l'agrégat s30) ; les **familles recomposées R2** sont **matérialisées visuellement**.
    **Hors scope s38 (reste ouvert)** : **R3 « exactement 2 parents » / statut de complétude du couple** (le graphe
    restitue 0/1/2 parents tels quels, **n'impose aucune complétude**) ; **édition depuis le graphe** (strictement
    lecture seule) ; **graphe ÉTENDU** (grands-parents, parents liés entre eux via leurs enfants, lien enfant↔activité
    s35 dans le graphe) ; **vue planning centrée couple** (recomposé).

- **Statut de complétude du couple R3 (badge en LECTURE) + onglet « Foyer »** *(livré s40, 7ᵉ incrément de la Refonte
  Config foyer — payoff des liens s34/s36/s37 et du graphe s38)* : le foyer **SIGNALE** désormais, par enfant, la
  complétude de son couple parental — **sans jamais l'imposer**.
  - **Statut PUR par enfant, composé sur la lecture.** Un **enum `StatutCoupleR3`** {complet / incomplet / vide} est
    **composé** par enfant à partir des données **déjà persistées** (liens enfant↔parent s34 + rôle-du-lien père/mère/
    parent-libre s37) et **enrichit la projection `GrapheFoyerQuery` s38** — **aucune query parallèle, aucun store
    neuf, aucune mutation, aucune persistance neuve** (borne anti-cliquet), sur les **deux adaptateurs** (InMemory
    seedé + Mongo durable, même contrat). Le statut est **présentation seule** (n'intervient ni dans la résolution
    grille/légende ni dans le gating, R10).
  - **Règle R3.** **Complet** = l'enfant porte un lien **« père » ET** un lien **« mère »** résolus. **Incomplet** =
    0 ou 1 parent, **OU** 2 parents sans le couple père+mère (ex. deux « parent-libre », père + parent-libre,
    mère + parent-libre). **Vide** = **aucun parent lié** (racine isolée légitime s38, état neutre distinct
    d'« incomplet », sans alarme). **Décompte fidèle, zéro fantôme** : un acteur **supprimé / orphelin** encore
    référencé n'est **PAS compté** (miroir R5/R6, filtre `Resolvable()` s13 — un enfant dont le seul lien pointe un
    orphelin est **incomplet**, pas faussement complet) ; un lien s34 **sans rôle-du-lien explicite** compte comme
    **« parent-libre »** (défaut neutre s37, ne satisfait pas seul « père ET mère »).
  - **R3 SIGNALÉE, JAMAIS IMPOSÉE.** Le calcul est un **chemin de LECTURE greffé sur la projection** : il **ne touche
    à aucun handler d'écriture**. `LierEnfantParent` / délier / la modal Enfants continuent d'**accepter 0, 1 ou 2
    parents** et tout jeu de rôles-du-lien valide s37 **sans nouveau refus** — **aucun invariant « exactement 2 »
    n'est ajouté à la pose ni à l'enregistrement** (un enfant « incomplet » s'enregistre. La **contrainte** R3 reste
    délibérément **non imposée** — choix produit).
  - **IHM — badge en LECTURE + onglet « Foyer ».** Un **badge de complétude** (« couple complet » / « couple
    incomplet » / « aucun parent ») est rendu par enfant sur la vue graphe s38, **STRICTEMENT en lecture** (aucun
    contrôle d'édition, aucune commande émise depuis le badge). **Parent-gated lecture** : l'Invité **voit** le badge.
    **Convergence SignalR par REPROJECTION CLIENT** : lier / délier / changer un rôle-du-lien depuis la modal Enfants
    fait **converger le badge d'un 2ᵉ écran sans rechargement** (0 GET sur push, diffusion lecture seule s20, garde
    conception s38). **Relocalisation (rework gate PO)** : la vue graphe s38 + ses badges est déplacée de la tête de
    page (« prend trop de place ») vers un **onglet « Foyer » placé EN PREMIER** dans la barre d'onglets de la Config
    foyer, **actif par défaut** à l'arrivée — pure présentation (aucun handler / commande / invariant / query neuf),
    comportement strictement préservé. **Hors scope s40** : contrainte R3 « exactement 2 » **imposée à l'écriture**
    (non traitée, choix produit), **édition depuis le graphe**, graphe étendu.

- **Commandes inverses actif/admin — toggle bi-directionnel** *(livré s41, solde la dette « toggle verrouillé
  ON s33 »)* : le sens **OFF** du toggle actif/admin de la modal Acteurs est **débloqué** — le domaine offre
  désormais les **commandes montantes inverses** qui manquaient à s33.
  - **Dé-désigner un admin (Domain pur).** Une commande/handler sur l'agrégat **`AdministrationFoyer`** (s22)
    **retire** la désignation d'admin d'un acteur : **no-op idempotent** si l'acteur n'est **déjà pas** admin
    (réussit sans mutation ni doublon), **acteur inconnu REFUSÉ sans mutation** (motif restitué, store intact),
    comportement identique sur les **deux adaptateurs** (InMemory seedé + Mongo durable). **Borne « dernier
    admin » (défensive neuve)** : dé-désigner le **SEUL** admin du foyer est **REFUSÉ AVANT écriture** (motif
    « le foyer doit garder au moins un admin », store intact) — un foyer garde **toujours ≥1 admin** ; dé-désigner
    un admin **quand il en reste d'autres** réussit. Cohérent avec l'invariant s22 « l'admin est un acteur de
    type Parent » (le sens ON, `DesignerAdmin`, reste inchangé, cardinal non borné par le haut).
  - **Désactiver un compte (`Actif → Inactif`).** Une commande/handler porté par l'agrégat
    **`CompteUtilisateur.Desactiver()`** (Domain pur, **miroir de `Activer()` s24**) réutilise le port d'écriture
    **`IEditeurComptes`** (s22, InMemory + Mongo) — **aucun agrégat neuf, aucun store neuf** : **no-op idempotent**
    si déjà Inactif, **compte inconnu REFUSÉ sans mutation**, nouveau statut **persisté durablement** (round-trip
    survivant au rechargement) sur les deux adaptateurs. Un compte redevenu **Inactif refuse la connexion** (garde
    s23 « compte non activé » tenue) ; le sens ON (`ActiverCompte` s24) reste inchangé.
  - **IHM — toggle bi-directionnel.** Le OFF du toggle admin / actif émet la **vraie commande inverse**
    (dé-désigner admin / désactiver compte) via le canal HTTP, **PAS un no-op silencieux** (anti-vert-qui-ment) ;
    l'effet **survit au rechargement** (round-trip Mongo durable). **Contrat d'erreur** (refus « dernier admin »,
    compte inconnu, API injoignable) : la **modal RESTE OUVERTE**, le **motif est affiché dedans**, la **saisie
    et l'état des toggles sont CONSERVÉS**, **aucune écriture partielle** ne touche le tableau ; **Échap = Annuler**
    sans mutation (port `IEcouteurEchapModal` s33). **Parent-gated** (identité effective : Invité = pas de toggle
    actionnable) + **convergence SignalR** de lecture (un 2ᵉ écran reflète le nouvel état actif/admin sans
    rechargement, 0 GET ajouté). Le **gating impersonation R8/R9** (droit d'écriture dérivé du `TypeActeur`
    cantonné, s36) est **préservé, non modifié** — dé-désigner / désactiver ne fait gagner ni perdre aucun droit.

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
  **R1 — référentiel d'enfants livré s30** : l'enfant est un **agrégat de config foyer de 1er rang**
  (id stable opaque + prénom), énuméré/édité par ports dédiés, persisté **Mongo durable sans seed**
  (miroir lieux s27) ; rejets **prénom vide / doublon** sans écriture ; **onglet « Enfants »** + **sélecteur
  d'enfant explicite** à la pose (fantôme `Session.EnfantId` retiré) ; **validation d'existence de l'enfant
  à la pose** (ponctuel + récurrent, slot sur enfant inconnu refusé sans écriture) ; **migration
  rétro-affectation idempotente** des slots du fantôme « Léa » (utilitaire ops non auto-câblé). **Borne ≥1
  enfant** tenue par construction (fantôme → 1er enfant réel) ; **suppression d'enfant + borne défensive au
  Delete hors scope s30** ; **vrai multi-enfants de bout en bout pas encore exercé** (familles recomposées
  R2 / graphe parents R3 / multi-enfants au cycle de fond restent ouverts). **Lien enfant↔parent livré s34** :
  l'enfant porte un **lien vers 1..2 parents-acteurs** (parent = acteur de rôle Parent), commandes **lier /
  délier** idempotentes, règle **« 2 parents max »**, rejets **acteur inexistant / non-parent / déjà lié
  sans écriture partielle**, persistance **Mongo durable**, id enfant inchangé ; **onglet Enfants** au patron
  **tableau lecture + crayon → modal** (swap inline→modal) avec **colonne « Parents liés »** + **sélecteur de
  parents**. **Éligibilité role-flag livrée s36** (l'acteur liable = porte un rôle marqué « parent »). **Rôle-du-lien
  père/mère/parent-libre livré s37** : le lien porte un attribut `{père, mère, parent-libre}` (record `ParentLie`),
  **invariant d'exclusivité** (pas deux « père » ni deux « mère » sur un enfant), « parent-libre » répétable, compat
  non destructive (lien s34 relu à « parent-libre »), colonne « nom (rôle) ». **Graphe foyer enfant-racine livré
  s38 EN LECTURE** : query PURE `GrapheFoyerQuery` (par enfant → parents liés + rôle-du-lien, deux adaptateurs) +
  **vue lecture seule** (enfant en racine « nom (rôle) », **familles recomposées R2 VISIBLES**, store vide =
  message neutre, Parent-gated, convergence SignalR par **reprojection client**) → **R1 multi-enfants/graphe
  exercé en LECTURE, R2 matérialisée**. **Statut de complétude R3 SIGNALÉ en LECTURE livré s40** : enum
  `StatutCoupleR3` {complet = père ET mère résolus / incomplet = 0-1 parent OU 2 sans le couple père+mère / vide =
  racine sans parent} composé **PUR** en enrichissant `GrapheFoyerQuery` (orphelin **exclu du décompte**, miroir
  `Resolvable()` s13 ; lien sans rôle-du-lien = « parent-libre »), rendu en **badge lecture seule** sur le graphe
  (Parent-gated, convergence SignalR par reprojection client, 0 GET), désormais dans un **onglet « Foyer » (1ᵉʳ, actif
  par défaut)**. **R3 SIGNALÉE, JAMAIS IMPOSÉE** : aucun blocage d'écriture ajouté — `LierEnfantParent` accepte
  toujours 0/1/2 parents (la **contrainte « exactement 2 » reste NON imposée**, choix produit).
  **R1 MULTI-ENFANTS exercé de BOUT EN BOUT — ISOLATION STRICTE livrée s53** : R1 était jusque-là exercé
  en LECTURE (graphe s38) mais **jamais de bout en bout à l'écriture** — toute la série s44→s52 (délégation,
  échange, imprévu, digest) était **MONO-ENFANT** (reliquat s30). s53 peuple le store de **≥2 enfants** et
  prouve l'**isolation stricte** sur **TOUS** les chemins d'écriture ET de lecture. **`EnfantId` porté et
  propagé de bout en bout** — période (`PeriodeSnapshot.EnfantId`), **transfert SAISI** (`Transfert.EnfantId`,
  s29, était dé-scopé), **cycle de fond** (`DefinirCycle` par enfant), **slots « où »**, **reprise/annulation**
  (`AnnulerDelegation`) — via **Option A** : l'`EnfantId` est **hérité de l'enfant courant du sélecteur** (s30),
  **affiché en LECTURE SEULE** dans les dialogs (« Pour : X (sélection courante) »), **jamais un champ de choix**.
  **Résolution STRICTEMENT filtrée par enfant** : `GrilleAgendaQuery.Projeter(ancre, vue, enfantId)` — aucun
  repli global/`''` ; un enfant **sans cycle propre → NEUTRE** (repli s13, plus jamais le cycle partagé legacy
  `''`, désormais **inerte**). La **cloche et le journal restent TRANSVERSES par design** (P3 : ils signalent
  QU'un changement a eu lieu, tous enfants) ; le **digest s50 est FILTRÉ** par l'enfant sélectionné. Le
  **sélecteur d'enfant s30 est câblé** (réemploi, **aucune surface de lecture neuve**) ; l'**onglet Cycle de la
  config a son propre sélecteur d'enfant** (cycle par enfant, familles recomposées). Prouvé **store réel** sur
  **deux adaptateurs InMemory + Mongo durable** *(détail cycle/résolution :
  [`periodes-et-cycle-de-fond.md`](periodes-et-cycle-de-fond.md))*. **CONSÉQUENCE UX** : les docs Mongo cycle
  `EnfantId=''`/`undefined` sont **inertes** ; un enfant dont le cycle n'a jamais été configuré **par enfant**
  affiche NEUTRE → le foyer doit configurer le cycle de **CHAQUE** enfant. **Reste ouvert** : contrainte R3
  imposée à l'écriture (non traitée), **VUE multi-enfants SIMULTANÉE** (lanes/colonnes, surface de lecture
  neuve), **imprévu/échange multi-enfant**, **nettoyage optionnel** des données legacy cycle `''`/`undefined`,
  **graphe ÉTENDU** (grands-parents, parents liés entre eux via leurs enfants) et **édition depuis le graphe**,
  **suppression d'un enfant** + borne défensive R1 au Delete.
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
  compris. **Non-régression couleur reconfirmée s27** : une couleur recoloriée en config reste effective
  sur la case de garde ET la légende (la palette relit la dernière écriture, config → planning tenu).
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
  R30, **zéro persistance neuve**). *(Les volets encore hors scope à la clôture s24 — OAuth, libre-service,
  récupération de mot de passe, protection des routes, bug rôle — sont **tous livrés / corrigés s25**, cf. R14.)*

- **R14 — Login complet : protection des routes, mot de passe local, libre-service, récupération & OAuth**
  *(livré s25, auth tranche 2 — terminaison, entorse G2 de preuve actée)*. Ferme le login d'un seul tenant :
  - **Protection d'accès aux routes** : non connecté → **redirection `/connexion`** (aucun contenu de route
    protégée rendu, pas de flash de grille) ; connecté → accès rétabli (navigation entre routes protégées
    **sans** re-redirection) ; **`/connexion` librement accessible** (pas de boucle), `/` → `/connexion`
    (landing s24 préservée) ; **déconnexion → re-verrouillage immédiat** des routes.
  - **Correction du bug rôle ≠ acteur du compte connecté** (« connecté en Mamie → rôle Parent ») :
    l'**identité réelle de la session est ancrée sur l'acteur du compte connecté** (relation 1-1 s22) au lieu
    du **configurateur en dur** ; le **rôle / gating d'écriture suit le type RÉEL** de cet acteur (Mamie type
    Autre → **pas** les droits Parent), plus un rôle Parent hérité par défaut. **Aucune règle de résolution
    grille/légende touchée** (le rôle n'y intervient pas, R10). **Non-régression impersonation bornée s14** :
    retour à l'identité réelle → acteur du compte (pas le configurateur), repli sur suppression concurrente
    de l'incarné vers l'identité réelle du compte (SignalR).
  - **Facteur mot de passe local** : mot de passe **stocké haché (PBKDF2)**, **jamais en clair** ni exposé par
    le canal, sur `CompteUtilisateur`. Login **email + mot de passe** (bon couple → `SessionOuverte`, identité
    réelle = acteur du compte). **Refus NEUTRE anti-énumération** : mauvais mot de passe et email inconnu
    donnent le **même** motif (ne les distingue jamais). L'**email-only s23** reste couvert pour les comptes
    sans mot de passe / OAuth.
  - **Inscription libre-service** : email **neuf** + mot de passe → `CompteUtilisateur` créé **Inactif** (défaut
    s22) avec mot de passe **haché**, `ActeurId` **nullable** (association / activation ultérieures s22/s24) ;
    email **déjà porteur** → **rejet sans écriture** (invariant email unique s22), motif clair.
  - **Récupération de mot de passe par jeton** : demande sur **email connu** → **jeton usage-unique +
    expiration** généré côté serveur + mail (lien/jeton) remis au **port de droite `IEnvoiMail`** ; **réponse
    NEUTRE** (ne confirme pas l'existence du compte). **Email inconnu** → **aucun jeton, aucun envoi**, **même**
    réponse neutre (anti-énumération, aucune fuite). **Jeton valide** → mot de passe **redéfini (haché)** et
    **jeton consommé** (2ᵉ usage échoue) ; jeton **expiré / inconnu** rejeté sans mutation. Le jeton est porté
    par un **port `IReferentielJetonsReset`**.
  - **OAuth externe Google / Microsoft / Apple** : branché **derrière** `SessionOuverte` s23 via le **port de
    droite `IFournisseurOAuth`**. Callback → identité externe **liée à un compte Actif** → `SessionOuverte`
    (**même chemin** que la connexion locale s23, aucun agrégat durable neuf) ; identité **inconnue** ou compte
    **Inactif** → **refus** (motif clair, cohérent Sc.8 / s23 / s24). **Boutons « Se connecter avec Google /
    Microsoft / Apple »** sur `/connexion`, à côté du login local.

  > **⚠️ Entorse G2 de preuve ACTÉE (PO) — volets non testables en runtime local.** OAuth (providers réels,
  > secrets, callbacks) et **envoi de mail SMTP** ne peuvent pas passer le rempart d'acceptation runtime. La
  > logique Application / frontière est prouvée **verte** contre une **doublure du PORT** (`IEnvoiMail`,
  > `IFournisseurOAuth`, `IReferentielJetonsReset`) — **pas** un faux « runtime vert » de bout en bout — et le
  > câblage réel est vérifié **manuellement** au gate. **DETTE DE CÂBLAGE ASSUMÉE, portée au backlog (P0, tête)** :
  > adaptateurs concrets `IEnvoiMail` (SMTP), `IReferentielJetonsReset` (store durable), `IFournisseurOAuth`
  > (providers réels) + endpoint `api/oauth/{provider}/demarrer` **non câblés** ; handlers `@preuve-doublure`
  > **non enregistrés en DI** ; écrans IHM **mot-de-passe-oublié** et **inscription libre-service** **non
  > construits** ; expiration du jeton reset **à confirmer** (défaut suggéré **60 min**). Mode opératoire OAuth :
  > `docs/guides/auth-social-oauth-mode-operatoire.md`. **Tant que ces adaptateurs/écrans ne sont pas branchés,
  > le login n'est pas opérationnel en runtime réel.**

- **R14bis — Câblage auth réel : reset E2E + mot de passe local opérationnels** *(livré s28, solde la
  moitié de la dette de câblage G2 s25)*. Le **reset de mot de passe** est **opérationnel bout-à-bout en
  runtime réel** : adaptateur **`IEnvoiMail` SMTP concret** (serveur de dev Smtp4dev, Docker) remplaçant la
  doublure ; **`IReferentielJetonsReset` sur store Mongo durable** (émission / relecture / consommation
  usage-unique) ; **expiration 60 min** prouvée contre l'horloge injectée sur store réel ; **DI** des handlers
  `DemanderRecuperationMotDePasse` / `RedefinirMotDePasse` + endpoints du canal ; **écrans IHM** « mot de passe
  oublié » et « redéfinir par jeton » (RED→GREEN runtime, message neutre anti-énumération). Le **login email +
  mot de passe** est câblé de bout en bout : commande de **pose de mot de passe** (haché PBKDF2) + **champ mot
  de passe** sur l'écran de connexion (le refus reste **neutre** — mauvais couple / email inconnu
  indistinguables), l'email-only s23 non régressé. Le **rapprochement Google** (callback d'un email connu →
  session sur le compte local existant, **pas** de double compte) + endpoint `api/oauth/google/demarrer` + DI
  du `ConnexionOAuthHandler` sont **branchés en logique**, prouvés **par doublure du port `IFournisseurOAuth`**
  + vérif manuelle.
  - **Amorçage de démo conditionnel** : un compte de démonstration (`deveaux.cyril@gmail.com`) peut être
    **amorcé par le CHEMIN RÉEL** (acteur → compte → activation → pose de mot de passe PBKDF2), **derrière le
    flag `Demo:SeedCompteDemo`** (désactivé par défaut — **parité de l'asymétrie seed s15** : Mongo démarre
    vide, aucun seed hors flag). L'amorçage est **convergent/idempotent** : s'il trouve un compte email-only
    **préexistant** sur le store durable, il **pose le mot de passe** sur ce compte au lieu d'en recréer un
    (réconciliation d'un état partiel, pas un simple insert-si-absent). Exposé par `run.ps1 -SeedDemo`.
  - **Reliquat de dette (P0, backlog)** : **provider Google OAuth réel** non câblé (placeholder
    `FournisseurOAuthGoogleNonCable` renvoie `null` — échange secret / redirect_uri / callback en env. déployé)
    et **écran consommateur de `definir-mot-de-passe`** (endpoint livré, sans IHM). **Surface / dette assumée** :
    **MS / Apple** OAuth (boutons → 404), **relais SMTP externe réel** (choix PO = rester Smtp4dev), **écran
    d'inscription libre-service** (handler DI, écran non construit).

- **R14ter — Persistance / restauration de session côté client (survie au F5) & bouton œil** *(livré s31,
  V1 — bug P0 F5→login corrigé)*. La **session survit au rechargement** de la page : à la connexion réussie,
  un **jeton de session est persisté côté client** derrière un **port `IPersistanceSession`** (adaptateur
  **JS localStorage**, stockage durable au rechargement) ; **au démarrage du client**, un jeton persisté
  **valide** est **relu** et la session est **restaurée sans repasser par le flux de connexion** (F5 sur une
  route protégée reste connecté, **plus** de re-redirection `/connexion` de la protection des routes s25). Un
  jeton **absent ou invalide** n'ouvre **aucune** session (pas de session fantôme). Le **logout purge le
  persisté** : après déconnexion, un F5 redirige vers `/connexion` et **aucune** session n'est restaurée (le
  **logout s23 reste effectif au rechargement**). **Borne anti-cliquet R30 tenue** : `SessionPlanning` reste
  l'état de session en mémoire (aucune persistance de domaine neuve) ; la persistance est **bornée au jeton de
  session côté client**, distincte de la durabilité de la config foyer. **UX login** : un **bouton œil** sur
  `/connexion` bascule l'affichage du mot de passe (visible ↔ masqué).

## Risques

- **Login COMPLET livré (s22 fondation + s23 session + s24 activation/page login + s25 terminaison)** :
  la boucle auth est **fonctionnelle E2E**, la page `/connexion` est la landing, le menu utilisateur donne
  accès config + logout, et le s25 a fermé les volets restants — **protection des routes**, **mot de passe
  local haché**, **inscription libre-service**, **récupération par jeton (mail)**, **OAuth 3 providers**
  (cf. R14). Le **bug rôle ≠ acteur connecté** est **corrigé** (identité réelle ancrée sur l'acteur du compte).
  **s28 rend le reset + le login mot de passe OPÉRATIONNELS en runtime réel** (SMTP dev + jetons Mongo + PBKDF2,
  amorçage de démo convergent) — cf. R14bis.
- **⚠️ DETTE DE CÂBLAGE (s25, entorse G2 de preuve) — MOITIÉ SOLDÉE s28 (cf. R14bis).** ✅ **Branchés &
  opérationnels en runtime réel** : `IEnvoiMail` **SMTP** (dev Smtp4dev), `IReferentielJetonsReset` **store
  Mongo durable**, **expiration 60 min confirmée**, DI des handlers récup/reset + endpoints, **écrans**
  mot-de-passe-oublié + redéfinir-par-jeton, **login email + mot de passe**. **Reliquat P0** : **provider
  Google OAuth réel** (placeholder renvoie `null`) + **écran consommateur de `definir-mot-de-passe`**.
  **Surface / dette assumée** : **MS / Apple** OAuth (404), **SMTP externe réel** (choix PO = Smtp4dev),
  **écran inscription libre-service**. Mode op OAuth : `docs/guides/auth-social-oauth-mode-operatoire.md`.
  *(Plus tard, non spécifié : le PO envisage un **envoi de mail d'activation** — description à préciser
  ultérieurement.)*
- **Auth tranche 1 livrée (fondation identité)** : la relation identité ↔ acteur posée en s22 a rendu le
  couplage « défaut = moi » trivial (concrétisé s23).
- **Impersonation bornée lecture livrée — frontière avec l'auth réelle (palier 16) ; écriture « au nom
  de » = hors-cap** (décision PO explicite, candidat G1).
- **Borne anti-cliquet** : la persistance reste **bornée à la config foyer** ; le reste du domaine en
  queue (paliers « config foyer durable — reste » puis « persistance réelle »).
- **Variantes refus/réaffectation de suppression** = porte G1 au make-gherkin si un vrai trou émerge.
- **Refonte Config foyer — Acteurs (s32) + Acteurs enrichis & Rôles/Cycle (s33) + Enfants & lien parent (s34)
  livrés** : onglet Acteurs migré au patron **tableau lecture seule + crayon → modal** (s32), puis **enrichi
  s33** (toggle actif/admin **sens ON** + verrou faute de commande inverse, **adresse de résidence** [champ
  neuf persisté], **palette couleur (picker)**), **Rôles & Cycle harmonisés** au même patron avec **tous les
  cycles déclarés désormais visibles** (corrige le trou gate s32) et **fermeture Échap** des modals ; **s34**
  harmonise l'**onglet Enfants** au même patron (swap inline→modal) et pose le **lien enfant↔parent 1..2
  parents** (lier/délier, 2 max, rejets sans écriture partielle, Mongo durable, sélecteur + colonne « Parents
  liés » — traite « lier un enfant à 2 parents » du gate s33) ; **s35** renomme le référentiel **« Lieux » →
  « Activités »** (iso-comportement Domaine + Application, validation de pose préservée, axe LOCALISATION
  `LieuId` distinct non renommé), l'enrichit d'un **champ adresse** + d'un **lien enfant↔activité N-M**, et
  harmonise l'**onglet Activités** au même patron (swap inline→modal, renommage HTTP au SWAP) — **Acteurs /
  Rôles / Cycle / Enfants / Activités sont désormais TOUS harmonisés** (cf. Mécaniques). **Commandes inverses
  actif/admin livrées s41** (dé-désigner admin + désactiver compte + borne « dernier admin » → toggle
  bi-directionnel, dette « verrou ON s33 » SOLDÉE ; cf. bloc s41 dans Mécaniques). **Restent** :
  **liste de slots par activité** (récurrents/non, hors scope s35), **lien adresse acteur↔lieu/domicile**,
  **révision de la validation de pose** (préservée iso s35), **familles
  recomposées R2/R3** (« exactement 2 parents » + graphe enfant-racine), **éligibilité « parent »** du lien
  enfant↔parent (retour PO gate s35, à trancher G2 s36). **Tension
  ouverte à arbitrer G2** : le PO veut **en plus** une **édition inline au clic de la valeur** — direction
  (inline seul / modal seule / cohabitation) **non tranchée**, à décider au prochain `/planning` avant tout
  code (ne pas re-livrer l'inline sans arbitrage).
- **Évolutions de surface config non priorisées** : refonte visuelle profonde du thème, contenu réel de
  l'onglet **Slot récurrent** (placeholder réservé au s20, aucune fonctionnalité neuve).
- **Rôle sans effet fonctionnel encore** *(s21)* : le rôle est **livré comme caractéristique** (pas une
  responsabilité) — il n'a **pas encore** de droits/comportements attachés. Le couplage rôle → droits
  par acteur (nounou / grand-parent / second parent) vit dans **É10 (auth, palier 13)**, après la prise
  en main de compte.
- ~~**Dette de cohérence (s19)** — sélecteur d'édition config encore sur `Foyer.ActeursEditables`~~ —
  **RÉSOLUE s20** : convergé sur le **store vivant unifié** `IEnumerationActeursFoyer` (`Foyer.ActeursEditables`
  retirée) → **un seul chemin de lecture** du référentiel, cohérence stricte config ↔ dialogs ↔ grille ↔
  légende, temps réel SignalR compris.

Cf. [`risques-et-questions-ouvertes.md`](risques-et-questions-ouvertes.md).
