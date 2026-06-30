# Planning de garde — Organisation des semaines de garde

> Version 04 · consolide la v03 + docs/sprints/03-calendrier-grille-lecture/99-sprint03-besoins-fin-itération.md. Remplace la v03, qui reste figée en historique.

## Contexte

Une app où parents et intervenants (nounou, grands-parents…) organisent à
l'avance et partagent les semaines de garde des enfants d'un foyer. Le hub
`/planning` est la mémoire partagée du foyer : un calendrier navigable où l'on
lit qui garde qui, où, quand, et d'où l'on agit. La responsabilité de chaque
garde se lit d'un coup d'œil par un code couleur propre à chaque personne, doublé
du nom du responsable et d'une légende. Les acteurs réels du foyer s'authentifient
pour que le planning reflète la réalité plutôt que des SMS éparpillés.

Le hub repose sur un **back découplé du front** : l'application expose ses
commandes et ses lectures à travers une **API**, ce qui en fait à la fois un
produit utilisable et une **vitrine** ouverte à d'autres clients (front exécuté
côté navigateur, IHM tierce, agents). Le front consomme cette API plutôt que
d'appeler le back en direct.

## Objectif & arbitrage

L'app poursuit trois buts : être un **outil réellement utilisé**, servir de
**vitrine** technique, et rester un **terrain d'apprentissage**. En cas de
conflit entre les trois, on garde ce qui sert l'usage quotidien et on coupe le
reste.

> **Arbitre principal : l'usage réel tranche.** Entre deux besoins qui
> s'opposent, gagne celui qui rend le hub utilisable au quotidien : les données
> et le câblage qui débloquent l'usage priment sur l'ergonomie de surface, qui
> prime elle-même sur l'ouverture de l'accès. Cet arbitre est **permanent**.

> **Exception bornée : la fondation technique d'abord, au début du projet.**
> Tant que l'app est petite, une **fondation structurelle** prime
> ponctuellement sur l'usage immédiat, parce que le coût de la poser (découpler
> le back en API, ouvrir l'app à d'autres clients) est minimal maintenant et
> **explose une fois l'app grosse**. C'est une **fenêtre d'investissement de
> début de projet**, pas une nouvelle règle générale : l'exception est **bornée
> à un seul palier de fondations** et l'arbitre d'usage **reprend la main juste
> après**. Subordination temporelle, jamais remplacement.

> **Corollaire de découpe.** Quand le périmètre d'un sujet déborde, on coupe au
> **plus petit incrément** qui rend la grille lisible et utilisable : on
> séquence, on ne reporte jamais tout en bloc. C'est ce corollaire qui justifie
> de livrer d'abord un calendrier en lecture seule avant d'y brancher l'écriture.

## Séquence de livraison

Tous les besoins comptent, mais ils sont **séquencés** (pas livrés en bloc).
Chaque palier doit être adopté avant le suivant ; chacun se borne au plus petit
pas qui apporte une valeur lisible. Le palier de fondations ouvre la séquence au
titre de l'exception bornée ; ensuite l'arbitre d'usage reprend la main.

1. **Fondations — découplage du back en API** *(exception bornée, valeur d'usage
   immédiate nulle, assumée)*. Les commandes d'écriture (poser un slot, surcharger
   ou supprimer une période, ajuster un transfert) sont confiées à un **canal
   requête/réponse** côté serveur, et le front est **migré vers une exécution
   côté navigateur** qui consomme cette API plutôt que d'appeler le back en
   direct. *Pose le socle d'un produit ouvert (front navigateur, IHM tierce,
   agents) au moment où le coût est minimal. Aucun incrément produit n'avance ;
   séquence à tenir pour ne pas laisser glisser le grief des saisies invisibles.*
2. **Lisibilité & thème** *(reprise produit — traités ensemble)* — rendre la
   responsabilité de période **explicite** dans la grille (nom du responsable +
   **légende couleur**, pas seulement une teinte de fond) **et** habiller l'app
   d'un **thème en accord avec le domaine** (garde d'enfants). *La grille
   existante colore les cases mais ne dit pas qui garde ; on la rend lisible
   d'un coup d'œil.*
3. **Écriture en contexte** *(reprise produit — traités ensemble)* — poser un
   slot, surcharger ponctuellement une période, supprimer une période se font
   via des **dialogs ouvertes depuis une case** du calendrier, et la saisie
   **réapparaît immédiatement** dans la grille. *L'utilisateur agit là où il
   lit ; le grief « les saisies n'apparaissent pas » est résolu ici, l'écriture
   passant désormais par le canal requête/réponse et la grille s'actualisant par
   la diffusion temps réel. Vérifié en usage réel, pas seulement en code.*
4. **Alimentation & saisie des utilisateurs** — déclarer et **persister** la
   configuration du foyer (acteurs, lieux, set de couleurs par défaut, cycle de
   fond) plutôt que de la porter en données figées dans le code. *La config
   foyer devient éditable et durable ; c'est le socle de données du modèle
   d'acteurs ci-dessous.*
5. **Modèle d'acteurs & foyer** — déclarer les acteurs réels (Admin, Parents,
   Autres) dans un écran de configuration, qui porte aussi la **responsabilité
   récurrente de fond** (le cycle) et le **set de couleurs par défaut**.
   *Exploite la config persistée du palier précédent ; prérequis de l'ouverture
   de l'accès.*
6. **Immédiat & événements à venir** — « qui récupère ce soir », où est l'enfant
   maintenant, transferts et changements à venir présentés comme événements dans
   un **panneau cloche**, plus comme un tableau permanent. *Valeur quotidienne,
   faible coût de saisie ; expose enfin les transferts, aujourd'hui invisibles.*
7. **Imprévu & échange** — enfant malade / retard / échange de dernière minute,
   transferts **dérivés automatiquement** par défaut et saisie réservée à
   l'exception. *Le plus délicat ; après que l'usage à deux est acquis.*
8. **Ouverture de l'accès** — landing page et authentification des acteurs réels
   (email via Gmail / Apple / Microsoft) pour lever le risque mortel d'adoption.
   Débloque aussi la **personnalisation des couleurs par utilisateur**. *Vient
   après le socle et le modèle d'acteurs ; à ne pas laisser glisser
   indéfiniment.*

> **Transverse (hors incrément dédié)** : l'ergonomie de surface est absorbée au
> fil des incréments calendrier et reste subordonnée à l'usage par l'arbitre.

> **Garde-fous hors-spec** : la convention de code interne (chaque vue adossée à
> son code-behind) et l'outillage d'**API explorable/documentée** sont attendus
> dès l'apparition de l'API, mais comme garde-fous de structure **sans observable
> métier** — ils n'ouvrent ni règle de gestion ni incrément produit.

> **Prochain sujet** : palier 1, `controllers-wasm-fondation`.

## Mécaniques de base

- Un créneau de garde a un horaire (début → fin), un responsable unique, un lieu et une activité
- Une journée est une suite de créneaux enchaînés
- Le planning suit un cycle de plusieurs semaines qui se répète automatiquement
- Le hub `/planning` est un calendrier navigable façon agenda (semaine en cours + 4 semaines suivantes) ; les slots y sont positionnés dans les cases jour/horaire
- La responsabilité de chaque garde se lit par un **code couleur par personne**, **doublé du nom du responsable et d'une légende** : la couleur seule ne suffit pas à identifier qui garde
- La grille est en **lecture seule** : elle consomme les slots et périodes déjà enregistrés, sans écriture. Toute écriture (poser un slot, surcharger ou supprimer une période, ajuster un transfert) se fait en contexte via des **dialogs** ouvertes depuis une case, alimentées par les acteurs et lieux du foyer
- **Le front est découplé du back par une API** : il ne manipule pas le domaine en direct, il **émet ses commandes et lit ses données à travers l'API**, ce qui ouvre l'app à d'autres clients (front navigateur, IHM tierce, agents)
- **Deux canaux distincts, jamais confondus** : l'**écriture** est confiée à un **canal requête/réponse** (la commande part, la réponse confirme l'effet) ; la **diffusion temps réel** vers les autres acteurs (notifications, actualisation du planning à l'écran) passe par un **canal de diffusion en lecture seule**. On **n'écrit jamais** par le canal de diffusion : une saisie se propage aux autres écrans parce que l'écriture, une fois aboutie, **déclenche** la diffusion
- La responsabilité récurrente de fond (qui garde selon le cycle) se déclare dans la configuration du foyer ; le calendrier ne porte que les **surcharges ponctuelles** d'une période
- La configuration du foyer (acteurs, lieux, set de couleurs, cycle de fond) est une **donnée persistée et éditable**, pas une constante figée dans le code
- Les transferts et changements à venir sont présentés comme des événements dans un panneau de notifications (cloche), pas comme un tableau permanent
- Trois types d'acteurs : Admin, Parent, Autre — chacun avec un affichage adapté à son type
- La composition du foyer (enfants, parents, acteurs autres) se déclare dans un écran de configuration distinct du planning

## Règles de gestion

### Foyer & acteurs

1. **Multi-enfants** — Un foyer compte au moins un enfant, et peut en compter plusieurs, chacun avec sa propre organisation de garde

2. **Familles recomposées** — Un foyer peut mélanger des enfants de parents différents, tous visibles dans le même planning

3. **Toujours deux parents** — Un foyer a toujours exactement deux parents ; tant qu'un seul est inscrit, il peut saisir les informations de l'autre

4. **Acteurs « autres »** — Un foyer peut avoir N acteurs « autres » (nounou, grands-parents…), éditables par les parents ou par l'acteur lui-même

5. **Responsabilité de fond en config, exception au calendrier** — La responsabilité récurrente de garde (le cycle : qui garde par défaut) se déclare dans l'écran de configuration du foyer, en même temps que les acteurs, et constitue une **donnée du foyer persistée et éditable** (et non une constante figée). Le calendrier ne sert qu'aux **surcharges ponctuelles** d'une période donnée. Les dialogs d'affectation et de surcharge sont alimentées par les acteurs du foyer

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

14. **Responsabilité lisible : couleur par personne + nom + légende** — La responsabilité de garde se lit dans la grille par une couleur qui distingue chaque **personne** (Parent A ≠ Parent B), pas seulement son type de rôle ; cette couleur est **doublée du nom du responsable et d'une légende**, car la teinte seule ne suffit pas à dire qui garde

15. **Set de couleurs par défaut** — Un jeu de couleurs par défaut, déclaré avec le foyer, différencie d'emblée les responsables tant qu'aucune personnalisation n'a été faite ; c'est ce set qui s'applique avant l'ouverture de l'accès

16. **Personnalisation des couleurs par utilisateur** — Une fois identifié, chaque acteur peut personnaliser les couleurs qu'il assigne aux acteurs qu'il voit ; cette personnalisation dépend de l'authentification (séquence, ouverture de l'accès) et n'altère pas le set par défaut vu par les autres

### Transferts

17. **Transfert dérivé par défaut** — Le transfert d'un enfant (qui dépose, qui récupère, à quelle heure) est dérivé automatiquement du planning par défaut : c'est le cas nominal, sans saisie

18. **Transfert modifiable et ponctuel** — Un transfert auto-calculé peut être modifié, et des transferts ponctuels peuvent être proposés ou ajoutés à l'exception (urgence, changement d'emploi du temps)

### Modifications & notifications

19. **Modification directe** — Un Parent applique son changement immédiatement ; les autres acteurs concernés sont notifiés (pas de workflow de validation en v1)

20. **Notifications & événements à venir** — Les notifications in-app couvrent les changements de planning et les rappels de transfert ; les transferts et changements à venir sont consultables dans un panneau d'événements accessible via une cloche

## Risques & questions ouvertes

- **Adoption de l'autre parent (risque mortel)** — L'app n'a de valeur que si l'autre parent l'utilise aussi ; sinon le planning est faux. L'ouverture de l'accès (auth réelle) la traite, mais elle vient tard dans la séquence : ne pas laisser glisser indéfiniment.
- **Sprint de fondations à valeur d'usage nulle (assumé)** — Le palier de découplage du back n'avance aucun incrément produit ; le grief « les saisies n'apparaissent pas » reste entier jusqu'à l'écriture en contexte. Tenir la séquence pour ne pas le laisser glisser (cf. « faux sentiment de progrès »).
- **Faux sentiment de progrès** — Une belle grille reste cosmétique tant que l'écriture n'est pas effectivement rebranchée et visible à l'écran ; tenir la séquence pour que la lisibilité puis l'écriture suivent vite le palier de fondations.
- **Bloc de fondations indivisible** — Le découplage front/back peut être plus gros que les incréments produit restants ; borner le périmètre et surveiller la dérive au make-gherkin.
- **Contraintes du découplage front/API** — Émettre les commandes à travers l'API introduit des contraintes (échanges inter-domaines, format de sérialisation des commandes, future authentification) absentes quand le front parlait au back en direct.
- **Réécriture du flux d'écriture** — Faire passer les écritures par le canal requête/réponse touche tout le câblage de saisie ; risque de régression à vérifier en **usage réel**, pas seulement en test de composant.
- **Diffusion déclenchée par l'écriture** — Garantir qu'une écriture aboutie déclenche bien l'actualisation temps réel des autres écrans, sans jamais écrire par le canal de diffusion ; point à valider en usage réel.
- **Vert qui ment sur la grille** — Un test de composant avec doublures peut afficher la grille alors que le câblage réel échoue. L'acceptation doit vérifier que des slots et périodes **réellement enregistrés** apparaissent positionnés, colorés et **nommés**, pas une grille vide statique.
- **Données du foyer figées dans le code (dette)** — La configuration du foyer est aujourd'hui portée par des constantes plutôt que par une donnée persistée ; dette explicitement signalée, à résorber au palier d'alimentation des utilisateurs, à ne pas oublier sous prétexte qu'elle est décalée.
- **Coût de saisie du cycle** — Saisir un cycle multi-semaines est lourd ; à valider seulement si le cycle est stable dans le temps. Le transfert dérivé automatiquement réduit ce coût pour le cas nominal.
- **Différenciation** — Vs Cozi / FamilyWall / agenda partagé : le manque précis qu'on couvre mieux reste à nommer.
- **Identité visuelle** — Un thème en accord avec le domaine (garde d'enfants) est attendu (palier lisibilité & thème) ; ergonomie de surface, subordonnée à l'usage par l'arbitre.
