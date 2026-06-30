# Mécaniques de base

> Sujet **migré** depuis `docs/15-specification.md` (section « Mécaniques de base ») à la migration
> complète des specs. Vue d'ensemble des **mécaniques structurantes** transverses ; les règles
> numérotées détaillées vivent dans [`regles-de-gestion.md`](regles-de-gestion.md) (et, pour le cycle
> de fond, [`periodes-et-cycle-de-fond.md`](periodes-et-cycle-de-fond.md)). Édité en diff.

## Planning & cycle

- Un créneau de garde a un horaire (début → fin), un responsable unique, un lieu et une activité.
- Une journée est une suite de créneaux enchaînés.
- Le planning suit un **cycle de fond** de plusieurs semaines qui se répète automatiquement : il
  compte **N semaines** (N ≥ 1) et alterne par **parité de la semaine ISO** (`index = semaine ISO du
  jour, modulo N`) ; chaque index est mappé sur un **responsable de fond**. La responsabilité d'une
  case se résout par **priorité** : **surcharge (période explicitement saisie) > fond (cycle) >
  neutre** — une période saisie prime toujours sur le cycle, qui reprend ensuite ; un index de cycle
  sans responsable retombe sur la **teinte neutre** sans nom. *Quand un acteur est **supprimé**, sa
  surcharge orpheline cesse de primer et la case retombe sur ce même fond (ou le neutre) ; si l'acteur
  était mappé au cycle, son index devient non mappé → neutre (cf. règle 6, livrée).* (détail :
  [`periodes-et-cycle-de-fond.md`](periodes-et-cycle-de-fond.md))
- Le hub `/planning` est une grille agenda où les slots sont positionnés dans les cases
  jour/horaire. Il a vocation à devenir un **calendrier navigable** façon agenda — déplacement dans le
  **passé et le futur**, **vues prédéfinies** (semaine, mois, 4 semaines glissantes), **fenêtre par
  défaut** = 4 semaines glissantes à partir de la semaine en cours, et **sélection d'une plage de
  cases** pour définir une période sur l'intervalle. *Cette navigation est le **prochain sujet**
  (palier « Calendrier navigable », palier 9, **non encore livré**) : la grille actuelle est une vue
  posée. L'**écriture en contexte** par dialogs, elle, est **acquise et complète** (slot, période et
  transfert).*

## Lisibilité

- La responsabilité de chaque garde se lit par un **code couleur par personne**, **doublé du nom du
  responsable affiché dans la case et d'une légende** : la couleur seule ne suffit pas à identifier
  qui garde. La **légende** agrège les responsables présents dans la fenêtre, **y compris les
  responsables de fond** issus du cycle, dédoublonnés par identifiant. Un nom trop long est
  **tronqué** dans la case, son **intitulé complet restant lisible au survol** ; un acteur hors du set
  de couleurs connu reste **affiché et distingué** (teinte neutre assumée) sans perdre son nom. La
  couleur d'un responsable se résout sur un **identifiant d'acteur stable**, jamais sur son libellé
  d'affichage : les sélecteurs de saisie **et le mapping du cycle de fond** fournissent ce même
  identifiant que la palette, sinon la case retombe sur la couleur neutre.
- L'app porte un **thème en accord avec son domaine** (garde d'enfants), au service de la lisibilité
  d'usage ; c'est une ergonomie de surface, subordonnée à l'usage.

## Écriture en contexte

- **L'écriture se fait en contexte, depuis la grille** : un **clic sur une case** ouvre un **menu
  d'actions** dont chaque entrée ouvre une **dialog** pré-remplie sur la **date de la case** et
  alimentée par les acteurs et lieux du foyer. **Trois dialogs sont livrées** (**Poser un slot**,
  **Affecter une période**, **Définir un transfert**) ; **tous les écrans de saisie dédiés** (slot,
  période **et transfert**) ont été **retirés** : il n'existe plus qu'**un seul chemin d'écriture** et
  **plus aucun écran de saisie dédié ne subsiste**. La dialog suit l'issue de la commande (succès →
  fermeture + grille relue, et pour le transfert un **accusé « Transfert défini » à part, non
  bloquant** ; échec → reste ouverte avec message et saisie conservée ; chevauchement → fermeture +
  avertissement non bloquant à part). (détail : [`ecriture-en-contexte.md`](ecriture-en-contexte.md))
- La **date pré-remplie** d'une dialog est celle de la **case cliquée** : cet **ancrage de contexte
  prime** sur le défaut « aujourd'hui » de l'horloge. Le défaut horloge ne sert que **hors-contexte** ;
  tant que toute saisie passe par une case, il n'est pas exercé (cf. règle 17).
- La grille est en **lecture seule** : elle consomme les slots et périodes déjà enregistrés **et le
  cycle de fond résolu**, sans écriture. Toute écriture (poser un slot, affecter, surcharger ou
  supprimer une période, **définir un transfert**) se fait en contexte via des **dialogs** ouvertes
  depuis une case ; **annuler** une dialog n'émet aucune commande et laisse la grille inchangée. La
  **sélection d'une plage de cases** (palier « Calendrier navigable », non encore livré) ouvrira
  l'affectation d'une période sur l'intervalle choisi.

## Config foyer & acteurs

- Les **acteurs du foyer (noms et couleurs) sont éditables** depuis un écran de configuration :
  renommer ou recolorier un acteur **met immédiatement à jour la grille** (case **et** légende) qui
  relit la configuration. On peut aussi **ajouter** un acteur (parent / autre / nounou) : l'ajout
  génère un **identifiant d'acteur stable neuf** (jamais le libellé) et la grille le reflète aussitôt
  (case + légende dédoublonnée par identifiant). La **suppression** d'un acteur est **livrée** (palier
  8 « CRUD acteurs — suppression ») : elle **retire l'acteur du store** (config foyer persistée) et
  **neutralise les cases orphelines** par repli (cf. règle 6). La configuration du foyer ainsi éditée
  **survit au redémarrage** : elle est **persistée** derrière les **ports de droite** par leur
  adaptateur durable. Cette durabilité est **bornée à la config foyer** (référentiel des acteurs :
  noms, couleurs, acteurs ajoutés) ; le reste du domaine (slots, périodes, transferts, **cycle de
  fond**) reste en mémoire le temps de son propre palier de persistance.
- **Toutes les écritures de l'écran de configuration** (édition d'acteur, ajout d'acteur, édition du
  cycle de fond, suppression d'acteur) sont **gatées sur l'identité effective** : un acteur « Autre »
  incarné (ou réel) ne les voit pas, un Parent / Admin oui. Le gating n'est plus restreint au seul
  bouton supprimer ; il est **complet** (cf. règle 9, durcissement livré).
- La **responsabilité récurrente de fond** (qui garde selon le cycle) se déclare dans la configuration
  du foyer (section « Cycle de fond » : nombre de semaines + un sélecteur de responsable par index,
  alimenté par les **acteurs persistés** du foyer, sur leur identifiant stable). Le calendrier ne
  porte que les **surcharges ponctuelles** d'une période, qui priment sur le fond. Définir **zéro
  semaine** est refusé ; toute ré-édition du mapping met la grille à jour **sans rechargement**
  (diffusion temps réel), et sur édition concurrente la **dernière écriture gagne**.
- La composition du foyer (enfants, parents, acteurs autres) se déclare dans un écran de configuration
  distinct du planning.

## Back découplé, canaux & persistance

- **Le front est découplé du back par une API** : il ne manipule pas le domaine en direct, il **émet
  ses commandes et lit ses données à travers l'API**, ce qui ouvre l'app à d'autres clients (front
  navigateur, IHM tierce, agents). L'**hôte d'API est détaché** : le back démarre seul, sans référencer
  le front, et le front — exécuté **dans le navigateur** (WebAssembly) — consomme une **API distante**.
- **Toute écriture passe par le canal requête/réponse** : aucune vue n'écrit le domaine en direct. Une
  saisie qui contournerait le canal (appel direct du back) est une dette à résorber, pas un mode de
  fonctionnement.
- **Deux canaux distincts, jamais confondus** : l'**écriture** est confiée à un **canal
  requête/réponse** (la commande part, la réponse confirme l'effet **et porte l'issue de sa propre
  écriture** — p. ex. un avertissement de chevauchement) ; la **diffusion temps réel** vers les autres
  acteurs (notifications, actualisation du planning à l'écran) passe par un **canal de diffusion en
  lecture seule**. On **n'écrit jamais** par le canal de diffusion : une saisie se propage aux autres
  écrans parce que l'écriture, une fois aboutie, **déclenche** la diffusion.
- **Échec clair si l'API distante est injoignable** : la commande non aboutie produit un message à
  l'écran et la saisie **n'est pas appliquée** ni perdue de vue (elle reste à resoumettre ; dans une
  dialog, celle-ci reste ouverte avec la saisie conservée). Aucune mise en file ni rejeu à ce stade —
  le hors-ligne rejouable est un palier technique ultérieur.
- L'**API est explorable** : elle expose un **document de description** (OpenAPI) et une **UI
  interactive** pour essayer les endpoints (type Swagger-UI / Scalar), et autorise l'**origine du
  front** par sa configuration CORS.
- La configuration du foyer est une **donnée éditable** : pour le **référentiel des acteurs** (noms,
  couleurs, acteurs ajoutés) elle est **éditable ET durable** — elle vit derrière les **ports de
  droite**, dont l'adaptateur durable la persiste et la fait survivre au redémarrage. Pour le **reste**
  (lieux, set de couleurs par défaut, **cycle de fond**) elle est **éditable en mémoire** dès
  maintenant, et **durable à terme** une fois son adaptateur posé. Dans tous les cas, c'est une donnée
  derrière les ports, jamais une constante figée dans le code.

## Transferts, acteurs & accès

- Les transferts et changements à venir sont présentés comme des événements dans un panneau de
  notifications (cloche), pas comme un tableau permanent.
- **Trois types d'acteurs : Admin, Parent, Autre** — chacun avec un affichage adapté à son type. Le
  **type** d'un acteur est surfacé en **lecture seule** depuis la déclaration seed du foyer (extension
  read-only de l'énumération des acteurs ; les acteurs **ajoutés en session sont typés « Parent » par
  défaut**, aucune saisie de type). Ce type est **lu** par l'**identité effective** pour piloter le
  droit d'écriture (cf. règles 8 et 9) ; il ne s'**écrit** pas (aucun port/handler d'écriture neuf).
- **Impersonation bornée lecture** : tant que les acteurs ne sont pas authentifiés (palier 16),
  l'utilisateur principal peut **incarner un acteur déjà déclaré** du foyer. La **session** distingue
  une **identité réelle** (le configurateur, fixe) d'une **identité effective** (l'acteur incarné, ou
  **repli sur la réelle**) ; un **bandeau « Vous incarnez X »** signale l'incarnation, la vue
  **reflète le rôle de l'identité effective** et le **retour à l'identité réelle** restaure l'état.
  Aucune **écriture « au nom de »** : les commandes restent émises sous l'**identité réelle**. L'état
  d'incarnation vit en **session / mémoire** (aucune persistance). C'est une **convenance
  d'administration**, **pas** l'authentification réelle (cf. règle 8).
