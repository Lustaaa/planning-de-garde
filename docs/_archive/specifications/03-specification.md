# Planning de garde — Organisation des semaines de garde

> Version 03 · consolide la v02 + docs/sprints/02-reparer-cablage-ihm-actions/99-sprint02-besoins-fin-itération.md. Remplace la v02, qui reste figée en historique.

## Contexte

Une app où parents et intervenants (nounou, grands-parents…) organisent à
l'avance et partagent les semaines de garde des enfants d'un foyer. Le hub
`/planning` est la mémoire partagée du foyer : un calendrier navigable où l'on
lit qui garde qui, où, quand, et d'où l'on agit. La responsabilité de chaque
garde se lit d'un coup d'œil par un code couleur propre à chaque personne. Les
acteurs réels du foyer s'authentifient pour que le planning reflète la réalité
plutôt que des SMS éparpillés.

## Objectif & arbitrage

L'app poursuit trois buts : être un **outil réellement utilisé**, servir de
**vitrine** technique, et rester un **terrain d'apprentissage**. En cas de
conflit entre les trois, on garde ce qui sert l'usage quotidien et on coupe le
reste.

> **Arbitre : l'usage réel tranche.** Entre deux besoins qui s'opposent, gagne
> celui qui rend le hub utilisable au quotidien : les données et le câblage qui
> débloquent l'usage priment sur l'ergonomie de surface, qui prime elle-même
> sur l'ouverture de l'accès.

> **Corollaire de découpe.** Quand le périmètre d'un sujet déborde, on coupe au
> **plus petit incrément** qui rend la grille lisible et utilisable : on
> séquence, on ne reporte jamais tout en bloc. C'est ce corollaire qui justifie
> de livrer d'abord un calendrier en lecture seule avant d'y brancher l'écriture.

## Séquence de livraison

Tous les besoins comptent, mais ils sont **séquencés** (pas livrés en bloc).
Chaque incrément doit être adopté avant le suivant ; chacun se borne au plus
petit pas qui apporte une valeur lisible.

1. **Grille agenda en lecture pure** — le hub `/planning` devient une grille
   calendaire (semaine en cours + 4 semaines suivantes) qui positionne chaque
   slot dans sa case et lit la responsabilité par code couleur, en remplacement
   des tableaux. *Socle de la mémoire partagée : sans une vue lisible, rien
   d'autre ne sert. Aucune écriture à cette étape.*
2. **Navigation dans le mois** — avancer / reculer les semaines comme un agenda.
   *Rend la mémoire partagée projetable sur les semaines et vacances à venir.*
3. **Écriture en contexte** — poser un slot, surcharger ponctuellement une
   période, supprimer une période se font via des **dialogs ouvertes depuis une
   case du calendrier**, en remplacement des routes dédiées. *L'utilisateur agit
   là où il lit.*
4. **Immédiat & événements à venir** — « qui récupère ce soir », où est l'enfant
   maintenant, transferts et changements à venir présentés comme événements dans
   un **panneau cloche**, plus comme un tableau permanent. *Valeur quotidienne,
   faible coût de saisie.*
5. **Imprévu & échange** — enfant malade / retard / échange de dernière minute,
   transferts **dérivés automatiquement** par défaut et saisie réservée à
   l'exception. *Le plus délicat ; après que l'usage à deux est acquis.*
6. **Modèle d'acteurs & foyer** — déclarer les acteurs réels (Admin, Parents,
   Autres) dans un écran de configuration, qui porte aussi la **responsabilité
   récurrente de fond** (le cycle) et le **set de couleurs par défaut**.
   *Prérequis de l'ouverture de l'accès.*
7. **Ouverture de l'accès** — landing page et authentification des acteurs réels
   (email via Gmail / Apple / Microsoft) pour lever le risque mortel d'adoption.
   Débloque aussi la **personnalisation des couleurs par utilisateur**. *Vient
   après le socle et le modèle d'acteurs ; à ne pas laisser glisser
   indéfiniment.*

> **Transverse (hors incrément dédié)** : un thème en accord avec le domaine
> (garde d'enfants) est attendu ; ergonomie de surface, absorbée au fil des
> incréments calendrier, subordonnée à l'usage par l'arbitre.

> **Prochain sujet** : incrément 1, `calendrier-grille-lecture`.

## Mécaniques de base

- Un créneau de garde a un horaire (début → fin), un responsable unique, un lieu et une activité
- Une journée est une suite de créneaux enchaînés
- Le planning suit un cycle de plusieurs semaines qui se répète automatiquement
- Le hub `/planning` est un calendrier navigable façon agenda (semaine en cours + 4 semaines suivantes) ; les slots y sont positionnés dans les cases jour/horaire et la responsabilité de chaque garde se lit par un code couleur
- La grille est en **lecture seule** : elle consomme les slots et périodes déjà enregistrés, sans écriture. Toute écriture (poser un slot, surcharger ou supprimer une période, ajuster un transfert) se fait en contexte via des **dialogs** ouvertes depuis une case, alimentées par les acteurs et lieux du foyer
- La responsabilité récurrente de fond (qui garde selon le cycle) se déclare dans la configuration du foyer ; le calendrier ne porte que les **surcharges ponctuelles** d'une période
- Les transferts et changements à venir sont présentés comme des événements dans un panneau de notifications (cloche), pas comme un tableau permanent
- Trois types d'acteurs : Admin, Parent, Autre — chacun avec un affichage adapté à son type
- La composition du foyer (enfants, parents, acteurs autres) se déclare dans un écran de configuration distinct du planning

## Règles de gestion

### Foyer & acteurs

1. **Multi-enfants** — Un foyer compte au moins un enfant, et peut en compter plusieurs, chacun avec sa propre organisation de garde

2. **Familles recomposées** — Un foyer peut mélanger des enfants de parents différents, tous visibles dans le même planning

3. **Toujours deux parents** — Un foyer a toujours exactement deux parents ; tant qu'un seul est inscrit, il peut saisir les informations de l'autre

4. **Acteurs « autres »** — Un foyer peut avoir N acteurs « autres » (nounou, grands-parents…), éditables par les parents ou par l'acteur lui-même

5. **Responsabilité de fond en config, exception au calendrier** — La responsabilité récurrente de garde (le cycle : qui garde par défaut) se déclare dans l'écran de configuration du foyer, en même temps que les acteurs. Le calendrier ne sert qu'aux **surcharges ponctuelles** d'une période donnée. Les dialogs d'affectation et de surcharge sont alimentées par les acteurs du foyer

### Rôles & accès

6. **Trois types d'acteurs** — Trois types d'accès : Admin (gère tout, y compris la configuration du foyer), Parent (gère les créneaux, lieux, acteurs et invitations de son foyer), Autre (consultation et édition limitée à ses propres informations). L'affichage s'adapte au type

7. **Modification réservée aux parents et à l'admin** — Seul un Parent (ou l'Admin) peut créer, éditer ou supprimer un créneau ou une période ; un acteur « Autre » n'édite que ses propres informations

### Planning & créneaux

8. **Responsable unique** — Un créneau a toujours un seul responsable (pas de « X ou Y »)

9. **Cycle récurrent** — Le planning se répète selon un cycle de plusieurs semaines (ex : semaine paire / impaire)

10. **Exception ponctuelle** — Un jour précis peut être surchargé sans casser le cycle de fond ; le cycle reprend ensuite

11. **Lieux éditables** — La liste des lieux (domicile A/B, école, nounou, activité…) est librement modifiable et sert de référentiel aux sélecteurs de saisie

12. **Grille en lecture seule** — La grille agenda consomme les slots et périodes déjà enregistrés et les rend (positionnés dans leur case, colorés par responsable) sans jamais écrire ; toute écriture passe par une dialog ouverte depuis une case

13. **Suppression de période** — Un Parent (ou l'Admin) peut supprimer une période de garde ; c'est une action d'écriture menée depuis une dialog contextuelle, hors de la grille en lecture pure

### Code couleur

14. **Couleur par personne** — La responsabilité de garde se lit dans la grille par une couleur qui distingue chaque **personne** (Parent A ≠ Parent B), pas seulement son type de rôle

15. **Set de couleurs par défaut** — Un jeu de couleurs par défaut, déclaré avec le foyer, différencie d'emblée les responsables tant qu'aucune personnalisation n'a été faite ; c'est ce set qui s'applique avant l'ouverture de l'accès

16. **Personnalisation des couleurs par utilisateur** — Une fois identifié, chaque acteur peut personnaliser les couleurs qu'il assigne aux acteurs qu'il voit ; cette personnalisation dépend de l'authentification (séquence, incrément 7) et n'altère pas le set par défaut vu par les autres

### Transferts

17. **Transfert dérivé par défaut** — Le transfert d'un enfant (qui dépose, qui récupère, à quelle heure) est dérivé automatiquement du planning par défaut : c'est le cas nominal, sans saisie

18. **Transfert modifiable et ponctuel** — Un transfert auto-calculé peut être modifié, et des transferts ponctuels peuvent être proposés ou ajoutés à l'exception (urgence, changement d'emploi du temps)

### Modifications & notifications

19. **Modification directe** — Un Parent applique son changement immédiatement ; les autres acteurs concernés sont notifiés (pas de workflow de validation en v1)

20. **Notifications & événements à venir** — Les notifications in-app couvrent les changements de planning et les rappels de transfert ; les transferts et changements à venir sont consultables dans un panneau d'événements accessible via une cloche

## Risques & questions ouvertes

- **Adoption de l'autre parent (risque mortel)** — L'app n'a de valeur que si l'autre parent l'utilise aussi ; sinon le planning est faux. L'ouverture de l'accès (auth réelle) la traite, mais elle vient tard dans la séquence : ne pas laisser glisser indéfiniment.
- **Faux sentiment de progrès** — Une belle grille reste cosmétique tant que l'écriture vit dans les anciennes routes ; tenir la séquence pour que les incréments 2-3 suivent vite, sinon le foyer voit un planning qu'il ne peut pas piloter.
- **Vert qui ment sur la grille** — Un test de composant avec doublures peut afficher la grille alors que le câblage réel échoue. L'acceptation du calendrier doit vérifier que des slots et périodes **réellement enregistrés** apparaissent positionnés et colorés, pas une grille vide statique.
- **Refonte calendrier transverse** — Le passage du tableau au calendrier navigable touche slots, responsabilité (code couleur) et transferts ; risque de bloc indivisible, borné en incréments adoptables (lecture pure d'abord, écriture ensuite).
- **Coût de saisie du cycle** — Saisir un cycle multi-semaines est lourd ; à valider seulement si le cycle est stable dans le temps. Le transfert dérivé automatiquement réduit ce coût pour le cas nominal.
- **Différenciation** — Vs Cozi / FamilyWall / agenda partagé : le manque précis qu'on couvre mieux reste à nommer.
- **Identité visuelle** — Un thème en accord avec le domaine (garde d'enfants) est attendu ; ergonomie de surface, subordonnée à l'usage par l'arbitre.
