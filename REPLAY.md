# REPLAY — reconstruire planning-de-garde from scratch

> **But de ce fichier** : un **prompt unique, autonome et optimisé** pour rejouer *tout* le travail
> déjà fait sur cette app, en repartant d'un dépôt vide. Cas d'usage : **tester un reboot du projet
> avec un autre modèle** (p. ex. Fable 5) et comparer le résultat au produit actuel (26 sprints,
> suite 458/458).
>
> **Comment s'en servir** : ouvre une session neuve dans un répertoire vierge, colle la section
> « ===== PROMPT ===== » ci-dessous comme message initial, laisse le modèle produire. Ce n'est **pas**
> un replay ligne à ligne du code : c'est un **brief produit + architecture** qui laisse le modèle
> reconstruire librement, pour éprouver sa capacité à re-livrer *le produit*, pas à recopier *le code*.

---

## ===== PROMPT =====

Tu vas construire **planning-de-garde**, une application web de planning de garde d'enfants
partagé entre parents et intervenants (nounou, grands-parents…). Construis-la de façon
**itérative, testée, et pilotée par l'usage**. Voici la vision, le modèle métier, l'architecture
imposée, la barre de qualité et la séquence de livraison. Reformule d'abord ta compréhension, puis
propose un plan de paliers avant de coder.

### 1. Le produit (pitch)

En garde alternée — ou dès que plusieurs personnes s'occupent des enfants — tout se joue sur
l'**anticipation** : « qui prend les enfants la semaine prochaine ? qui récupère jeudi ? ». Les
plannings vivent dans des SMS et des tableurs jamais à jour. L'app offre un **calendrier partagé
et préparé à l'avance** où chacun voit d'un coup d'œil : **qui** garde quel enfant et **quand**, les
**transferts** à venir (qui dépose, qui récupère), et **où** se trouve chaque enfant à l'instant T.

Principe directeur d'IHM : *un parent comprend en 3 secondes qui a les enfants et comment agir, sans
mode d'emploi.* L'esthétique sert l'intuitivité.

**Familles recomposées = cas central, pas une option** : un foyer peut mêler des enfants de parents
différents, chacun avec son cycle propre, tous sur la même vue. Le multi-enfants est dans le v1.

### 2. Modèle métier (domaine)

- **Foyer** : contient les **acteurs**, les **enfants**, un référentiel de **lieux**, un jeu de
  **couleurs**, un **cycle de fond**. À déclarer/persister — jamais figé dans le code.
- **Acteur** : 3 types — **Admin** (configure le foyer), **Parent** (gère le planning, toujours 2),
  **Autre** (nounou, grand-parent… accès limité). Id **stable opaque**. Nom + couleur éditables.
  CRUD complet. Un acteur peut porter un **rôle** issu d'un **référentiel de rôles éditable**
  (créer/renommer/supprimer) ; le rôle est une **caractéristique**, il **n'intervient pas** dans la
  résolution grille/légende ; acteur sans rôle = neutre.
- **Slot** (créneau de localisation) : enfant → **lieu**, **début/fin**, date. Rejet durée nulle,
  rejet lieu inexistant. Un slot **franchissant minuit** est rendu sur **tous les jours calendaires
  qu'il couvre**. Chevauchement **accepté** + **avertissement non bloquant**. Créer / supprimer.
- **Période de garde** : responsabilité d'un acteur sur un intervalle (distincte des slots). Bornes
  paramétrables, **fin > début**, responsable requis. Créer / éditer (re-borner, réaffecter) /
  supprimer. **Rejet sur état périmé** (concurrence optimiste sur l'agrégat).
- **Cycle de fond** : responsabilité **récurrente** définissable/éditable — cycle de **N semaines**,
  **alternance par parité ISO 8601**, mapping index→responsable sur id stable. La grille **résout le
  responsable de fond** sans saisie de période. Priorité de résolution : **surcharge > fond >
  neutre**, sans nom fantôme (référence orpheline → repli).
- **Transfert** : bascule de responsabilité (date, dépositaire, récupérateur, lieu, heure). Rejet si
  incomplet. Saisi en contexte.
- **Compte utilisateur** : lié **1-1** à un acteur déclaré. Email, statut **Actif/Inactif** (Inactif
  par défaut), mot de passe **haché (PBKDF2, jamais en clair)**. Invariant **admin = Parent** (pur
  Domain, cardinal non borné). Suppression d'un acteur → ses comptes se **désassocient**.

### 3. Authentification

- **Connexion locale** email + mot de passe (refus **neutre anti-énumération**). **Session serveur**
  (état d'hôte, pas d'agrégat durable). **Logout** = destruction de session.
- **Activation** de compte Inactif → Actif (idempotent).
- **Inscription libre-service** (email neuf → compte Inactif ; email déjà porteur → rejet sans écriture).
- **Récupération de mot de passe** par **jeton usage-unique + expiration** via un port de droite
  `IEnvoiMail` ; email inconnu → **réponse neutre** (aucun mail).
- **OAuth Google / Microsoft / Apple** via un port `IFournisseurOAuth` : callback → identité externe
  liée à un compte Actif → session (même chemin que la connexion locale) ; inconnu/Inactif → refus.
- **Protection d'accès aux routes** : non connecté → redirection vers `/connexion` (page landing) ;
  `/connexion` libre ; déconnexion → re-verrouillage immédiat.
- **Acteur par défaut = utilisateur connecté**. **Impersonation bornée LECTURE SEULE** (incarner un
  acteur déclaré, vue selon le rôle **effectif**, retour identité réelle, retour auto sur suppression
  concurrente ; jamais d'écriture « au nom de »).
- *(Les adaptateurs concrets SMTP / OAuth providers réels / store de jetons peuvent être prouvés par
  doublure au niveau du port puis câblés dans un second temps.)*

### 4. Architecture imposée (Clean / hexagonale, DDD + CQRS)

Solution `.slnx`. Séparation stricte :

- **Domain** — modèle métier **pur**, aucune dépendance techno.
- **Application** — use cases en **canal requête/réponse** (CQRS), ports gauche/droite. **Écriture =
  requête/réponse ; diffusion = temps réel lecture seule** — jamais confondus.
- **AdapterDroite.InMemory** / **AdapterDroite.Mongo** — dépôts par techno, **derrière des ports**.
  Persistance Mongo de **tout** le domaine, commutée par config `Foyer:Persistance` (Mongo runtime /
  InMemory tests). Démarrage runtime **sans seed** (app vide au 1er lancement, durable ensuite ; seed
  conservé côté InMemory pour les tests).
- **SignalR** — adaptateur de **gauche** de **diffusion temps réel, lecture seule** (jamais d'écriture
  par la diffusion). Toute écriture aboutie déclenche la diffusion.
- **Api** — hôte d'API **détaché** (démarre seul), expose **OpenAPI + UI explorable (Scalar)**, CORS.
- **Web** — front **Blazor WebAssembly**, consomme l'API comme une **API distante** (jamais d'appel
  direct au domaine). Convention **code-behind** (`.razor` + `.razor.cs`, pas de `@code` inline).
- **Infrastructure** — câblage / DI transverse.

Règles d'or : le front n'appelle **jamais** le domaine en direct (tout passe par l'API) ; données
**toujours derrière des ports** ; un seul chemin de lecture du référentiel acteurs.

### 5. IHM

- **Cœur = un calendrier navigable** façon agenda : navigation ±semaine, vues **Semaine / 4 semaines
  glissantes (défaut) / Mois**, retour « Aujourd'hui ». La responsabilité se lit **par code couleur** +
  **nom** + **légende** (dédoublonnée, masquée si vide).
- **Écriture en contexte** : menu au **clic sur une case** → dialogs pré-remplies sur la date de la
  case — « Poser un slot », « Affecter une période », « Définir un transfert », plus suppression/édition
  de période et suppression de slot. Échec → la dialog **reste ouverte** (message dans la dialog) ;
  annulation → aucune écriture. **Aucun écran de saisie dédié** (un seul chemin d'écriture).
- **Affectation par plage** : clic case début + clic case fin (gardé Parent).
- **Config foyer** en **onglets** (Acteurs / Période de garde / Slot récurrent), Acteurs par défaut.
- **Gating par rôle effectif** : Invité en lecture seule, actions d'écriture réservées au Parent.
- **Temps réel** : deux écrans convergent sans rechargement (SignalR).
- **Refonte visuelle « Studio »** : tokens CSS propres (`--pdg-*`, `:root` + `[data-theme]`), **thème
  clair/sombre** (défaut = préférence système, choix **persisté** `localStorage`, `data-theme` sur
  `<html>`, **zéro flash**), typo de titres + corps soignée (self-hosted, offline), parité navigateur
  PC / Safari iOS (tap ≥ 44px, safe-areas, sticky). Les **couleurs de responsabilité** (bleu / orange
  parents) sont **la donnée**, distinctes de l'accent de marque, lisibles en clair **et** en sombre.

### 6. Barre de qualité (non négociable)

- **TDD + BDD** : chaque comportement piloté par un test qui échoue d'abord (RED → GREEN). Scénarios
  Gherkin. Tests **sociables**, **doublures écrites à la main** (jamais de framework de mock), pattern
  snapshot.
- **Backend d'abord** (jusqu'à la frontière Application), **IHM ensuite**.
- **Acceptation runtime obligatoire** pour tout scénario IHM : prouver sur **câblage réel / store réel**
  (app lancée, observation réelle), **jamais** un test bUnit comme preuve d'un scénario IHM.
- **Non-régression = suite complète verte** à chaque incrément (pas de `--no-build`, pas de filtre).
- 3 projets de test : domaine/app, Api, Web.

### 7. Séquence de livraison (l'usage tranche)

Tout compte, mais **livré par étapes** : chaque phase doit être adoptée avant la suivante. Si ça
n'aide pas le quotidien, c'est coupé. Ordre indicatif (chaque palier = une tranche verticale
livrable, ~1 sprint) :

1. **Socle** — poser/afficher slots, périodes, transferts sur une grille (domaine + grille lecture).
2. **Fondation API** — hôte d'API détaché + front WASM réel + Scalar/OpenAPI + CORS + SignalR diffusion.
3. **Saisie visible** — la saisie réapparaît à la bonne date (défaut aujourd'hui) et en couleur du parent.
4. **Lisibilité & thème** — nom du responsable + légende + thème métier.
5. **Config foyer** — édition des acteurs (volatile puis **persistée Mongo**), ajout d'acteurs.
6. **Récurrence** — cycle de fond éditable (parité ISO), résolu dans la grille.
7. **Écriture en contexte** — dialogs au clic-case (slot / période / transfert), écrans dédiés retirés.
8. **CRUD acteurs** — suppression + repli orphelins + impersonation bornée lecture.
9. **Calendrier navigable** — nav passé/futur, vues prédéfinies, sélection de plage, **+ persistance
   Mongo de tout le domaine + démarrage sans seed**.
10. **Modèle de rôles** — référentiel de rôles éditable + affectation bornée.
11. **Authentification complète** — identité compte↔acteur, connexion + session + logout, activation,
    protection des routes, mot de passe haché, inscription, récupération par jeton, OAuth 3 providers.
12. **Refonte graphique** — habillage complet « Studio » + thème clair/sombre persisté, zéro régression.

**Plus tard** (backlog, non v1) : panneau **cloche** (« qui récupère ce soir », transferts à venir),
**imprévu & échange** (malade/retard/échange avec accord), transfert **matérialisé** sur le planning
(case bicolore diagonale), **PWA** hors-ligne (outbox + file rejouée), droits fins par rôle après
prise en main de compte, personnalisation des couleurs par utilisateur.

### 8. Attendu

Reformule ta compréhension, propose la découpe en paliers, puis exécute **palier par palier** en
TDD/BDD, en tenant l'architecture et en prouvant chaque incrément au runtime. À la fin de chaque
palier, montre l'app qui tourne. Documente au fil de l'eau (README produit + spec vivante).

## ===== FIN DU PROMPT =====

---

## Références (produit actuel, pour comparer le reboot)

- Pitch & séquence : [`README.md`](README.md)
- Spec vivante éclatée : [`docs/specs/`](docs/specs/index.md)
- Fait / reste : [`docs/BACKLOG-Done.md`](docs/BACKLOG-Done.md) · [`docs/BACKLOG.md`](docs/BACKLOG.md)
- Sprints clos (26) : [`docs/_archive/sprints/`](docs/_archive/sprints/)
- Pipeline d'agents SCRUM : [`README-claude.md`](README-claude.md)
- Exécution conteneurs : [`README-docker.md`](README-docker.md)

**Repères de conformité du produit actuel** : 8 projets `src/` (Domain, Application,
AdapterDroite.InMemory, AdapterDroite.Mongo, SignalR, Api, Web, Infrastructure) + 3 projets de test ;
suite **458/458** verte ; 26 sprints livrés.
