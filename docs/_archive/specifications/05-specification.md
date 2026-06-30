# Planning de garde — Organisation des semaines de garde

> Version 05 · consolide la v04 + docs/sprints/04-controllers-wasm-fondation/99-sprint04-besoins-fin-itération.md. Remplace la v04, qui reste figée en historique.

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
d'appeler le back en direct. Le découplage va jusqu'à un **hôte d'API
détachable** : le back doit pouvoir **démarrer seul**, sans le front, et exposer
son canal d'écriture à n'importe quel client.

## Objectif & arbitrage

L'app poursuit trois buts : être un **outil réellement utilisé**, servir de
**vitrine** technique, et rester un **terrain d'apprentissage**. En cas de
conflit entre les trois, on garde ce qui sert l'usage quotidien et on coupe le
reste.

> **Arbitre principal : l'usage réel tranche.** Entre deux besoins qui
> s'opposent, gagne celui qui rend le hub utilisable au quotidien : les **saisies
> visibles** priment sur l'ergonomie du calendrier, qui prime sur le confort
> d'outillage. Un **défaut confirmé** prime sur une simple évolution. Cet arbitre
> est **permanent**.

> **Exception bornée : la fondation technique d'abord, au début du projet.**
> Tant que l'app est petite, une **fondation structurelle** prime
> ponctuellement sur l'usage immédiat, parce que le coût de la poser (découpler
> le back en API, **détacher l'hôte d'API**, ouvrir l'app à d'autres clients) est
> minimal maintenant et **explose une fois l'app grosse**. C'est une **fenêtre
> d'investissement de début de projet**, pas une nouvelle règle générale :
> l'exception est **bornée au palier de fondations** et l'arbitre d'usage
> **reprend la main juste après**. Subordination temporelle, jamais remplacement.
> La fondation n'est **refermée** que quand le back démarre seul (hôte d'API
> détaché) ; ensuite, bascule **rapide** sur le lot « saisie visible ».

> **Corollaire de découpe.** Quand le périmètre d'un sujet déborde, on coupe au
> **plus petit incrément** qui rend la grille lisible et utilisable : on
> séquence, on ne reporte jamais tout en bloc. C'est ce corollaire qui justifie
> de livrer d'abord un calendrier en lecture seule avant d'y brancher l'écriture,
> et de borner la séparation de l'hôte d'API au plus petit pas qui rend le back
> démarrable seul.

> **Révisions de règle hors boucle.** Une demande qui contredit une règle déjà
> actée n'est **pas** un correctif : c'est une révision de spec, qui n'entre pas
> dans le séquencement courant et attend le palier qui la porte. Deux telles
> demandes sont en attente (cf. Risques & questions ouvertes) : le workflow
> demande/accord avant réaffectation (palier « imprévu & échange ») et
> l'interdiction/dédoublonnage de la pose répétée d'un même slot.

## Séquence de livraison

Tous les besoins comptent, mais ils sont **séquencés** (pas livrés en bloc).
Chaque palier doit être adopté avant le suivant ; chacun se borne au plus petit
pas qui apporte une valeur lisible. Le palier de fondations ouvre la séquence au
titre de l'exception bornée ; il n'est pas encore refermé. Ensuite l'arbitre
d'usage reprend la main.

1. **Fondations — découplage du back en API, hôte détachable** *(exception
   bornée, valeur d'usage immédiate nulle, assumée ; en cours de fermeture)*. Les
   commandes d'écriture (poser un slot, affecter ou supprimer une période, ajuster
   un transfert) sont confiées à un **canal requête/réponse** côté serveur. Il
   reste à **détacher l'hôte d'API** pour que le **back démarre seul**, le front
   le consommant comme une **API distante** plutôt qu'en partageant son hôte.
   *Pose le socle d'un produit ouvert (front navigateur, IHM tierce, agents) au
   moment où le coût est minimal. Aucun incrément produit n'avance ; séquence à
   tenir, et à fermer vite, pour ne pas laisser glisser le grief des saisies
   invisibles.* **Prochain sujet pour `/2-make-gherkin`.**
2. **Saisie visible — la saisie réapparaît à la bonne date et en couleur du
   parent** *(reprise d'usage, juste après la fondation)* — une saisie posée
   **réapparaît immédiatement** dans la grille, **à la bonne date** (les dates
   pré-remplies des formulaires valent « aujourd'hui », pas une date figée) **et
   en couleur du parent responsable** (la couleur se résout sur l'identifiant
   stable de l'acteur, pas sur son libellé d'affichage). *Lève d'un coup les deux
   défauts liés du faux bug « les saisies n'apparaissent pas » ; distincts mais
   indissociables à l'usage.*
3. **Lisibilité & thème** *(reprise produit — traités ensemble)* — rendre la
   responsabilité de période **explicite** dans la grille (nom du responsable +
   **légende couleur**, pas seulement une teinte de fond) **et** habiller l'app
   d'un **thème en accord avec le domaine** (garde d'enfants). *La grille colore
   les cases mais ne dit pas encore qui garde ; on la rend lisible d'un coup
   d'œil.*
4. **Calendrier navigable & écriture en contexte** *(reprise produit)* — naviguer
   dans le **passé/futur**, offrir des **vues prédéfinies** (semaine, mois, 4
   semaines glissantes), et poser un slot / affecter ou supprimer une période /
   ajuster un transfert via des **dialogs ouvertes depuis une case**. *L'utilisateur
   agit là où il lit ; l'écriture passe par le canal requête/réponse et la grille
   s'actualise par la diffusion temps réel. Vérifié en usage réel, pas seulement
   en code.*
5. **Alimentation & saisie des utilisateurs** — déclarer et **persister** la
   configuration du foyer (acteurs, lieux, set de couleurs par défaut, cycle de
   fond) plutôt que de la porter en données figées dans le code. *La config foyer
   devient éditable et durable ; socle de données du modèle d'acteurs.*
6. **Modèle d'acteurs & foyer** — déclarer les acteurs réels (Admin, Parents,
   Autres) dans un écran de configuration, qui porte aussi la **responsabilité
   récurrente de fond** (le cycle) et le **set de couleurs par défaut**. *Exploite
   la config persistée du palier précédent ; prérequis de l'ouverture de l'accès.*
7. **Immédiat & événements à venir** — « qui récupère ce soir », où est l'enfant
   maintenant, transferts et changements à venir présentés comme événements dans
   un **panneau cloche**, plus comme un tableau permanent. *Valeur quotidienne ;
   expose enfin les transferts, aujourd'hui invisibles par construction.*
8. **Imprévu & échange** — enfant malade / retard / échange de dernière minute,
   transferts **dérivés automatiquement** par défaut et saisie réservée à
   l'exception. *Le plus délicat ; après que l'usage à deux est acquis. Porte la
   question ouverte du **workflow demande/accord** avant réaffectation d'une
   période à l'autre parent.*
9. **Ouverture de l'accès** — landing page et authentification des acteurs réels
   (email via Gmail / Apple / Microsoft) pour lever le risque mortel d'adoption.
   Débloque aussi la **personnalisation des couleurs par utilisateur**. *Vient
   après le socle et le modèle d'acteurs ; à ne pas laisser glisser indéfiniment.*

> **Transverse (hors incrément dédié)** : l'ergonomie de surface est absorbée au
> fil des incréments calendrier et reste subordonnée à l'usage par l'arbitre.

> **Garde-fous hors-spec** : la convention de code interne (chaque vue adossée à
> son code-behind) et l'outillage d'**API explorable et documentée** sont attendus
> dès l'apparition de l'API, comme garde-fous de structure **sans observable
> métier** — ils n'ouvrent ni règle de gestion ni incrément produit. L'API
> explorable couvre **deux choses distinctes** : le **document** de description
> (OpenAPI) **et** une **UI interactive** pour essayer les endpoints
> (type Swagger-UI / Scalar).

> **Prochain sujet** : palier 1, fermeture de la fondation — **hôte d'API
> détachable** (back démarrable seul) + **UI d'exploration interactive** des API.

## Mécaniques de base

- Un créneau de garde a un horaire (début → fin), un responsable unique, un lieu et une activité
- Une journée est une suite de créneaux enchaînés
- Le planning suit un cycle de plusieurs semaines qui se répète automatiquement
- Le hub `/planning` est un calendrier navigable façon agenda ; on s'y déplace dans le **passé et le futur**, avec des **vues prédéfinies** (semaine, mois, 4 semaines glissantes). La **fenêtre par défaut** est **4 semaines glissantes à partir de la semaine en cours** ; les slots y sont positionnés dans les cases jour/horaire
- La responsabilité de chaque garde se lit par un **code couleur par personne**, **doublé du nom du responsable et d'une légende** : la couleur seule ne suffit pas à identifier qui garde. La couleur d'un responsable se résout sur un **identifiant d'acteur stable**, jamais sur son libellé d'affichage : les sélecteurs de saisie fournissent ce même identifiant que la palette, sinon la case retombe sur la couleur neutre
- Les formulaires de saisie pré-remplissent leurs dates sur **« aujourd'hui »** (la date de référence), jamais sur une date figée : une saisie tombe ainsi dans la fenêtre affichée et réapparaît immédiatement dans la grille
- La grille est en **lecture seule** : elle consomme les slots et périodes déjà enregistrés, sans écriture. Toute écriture (poser un slot, affecter, surcharger ou supprimer une période, ajuster un transfert) se fait en contexte via des **dialogs** ouvertes depuis une case, alimentées par les acteurs et lieux du foyer
- **Le front est découplé du back par une API** : il ne manipule pas le domaine en direct, il **émet ses commandes et lit ses données à travers l'API**, ce qui ouvre l'app à d'autres clients (front navigateur, IHM tierce, agents). L'**hôte d'API est détachable** : le back démarre seul et le front consomme une API distante
- **Toute écriture passe par le canal requête/réponse** : aucune vue n'écrit le domaine en direct. Une saisie qui contournerait le canal (appel direct du back) est une dette à résorber, pas un mode de fonctionnement
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

14. **Pose répétée d'un même slot acceptée avec avertissement** — Un slot qui chevauche ou redouble un slot existant est **accepté**, accompagné d'un **avertissement** ; il n'est ni refusé ni dédoublonné. (Une demande d'interdiction/dédoublonnage est en attente comme révision de règle — cf. Risques & questions ouvertes.)

### Code couleur

15. **Responsabilité lisible : couleur par personne + nom + légende** — La responsabilité de garde se lit dans la grille par une couleur qui distingue chaque **personne** (Parent A ≠ Parent B), pas seulement son type de rôle ; cette couleur est **doublée du nom du responsable et d'une légende**, car la teinte seule ne suffit pas à dire qui garde

16. **Couleur résolue sur un identifiant d'acteur stable** — La couleur d'un responsable se résout sur l'**identifiant stable** de l'acteur, jamais sur son libellé d'affichage. Les sélecteurs de saisie (affectation, surcharge) **fournissent ce même identifiant** que la palette : un libellé qui ne correspond pas à un identifiant connu fait retomber la case sur la **couleur neutre**, ce qu'on ne veut pas

17. **Set de couleurs par défaut** — Un jeu de couleurs par défaut, déclaré avec le foyer, différencie d'emblée les responsables tant qu'aucune personnalisation n'a été faite ; c'est ce set qui s'applique avant l'ouverture de l'accès

18. **Personnalisation des couleurs par utilisateur** — Une fois identifié, chaque acteur peut personnaliser les couleurs qu'il assigne aux acteurs qu'il voit ; cette personnalisation dépend de l'authentification (séquence, ouverture de l'accès) et n'altère pas le set par défaut vu par les autres

### Transferts

19. **Transfert dérivé par défaut** — Le transfert d'un enfant (qui dépose, qui récupère, à quelle heure) est dérivé automatiquement du planning par défaut : c'est le cas nominal, sans saisie

20. **Transfert modifiable et ponctuel** — Un transfert auto-calculé peut être modifié, et des transferts ponctuels peuvent être proposés ou ajoutés à l'exception (urgence, changement d'emploi du temps)

### Modifications & notifications

21. **Modification directe** — Un Parent applique son changement immédiatement ; les autres acteurs concernés sont notifiés (pas de workflow de validation). (Une demande de workflow demande/accord avant réaffectation d'une période à l'autre parent est en attente comme révision de règle, rattachée au palier « imprévu & échange » — cf. Risques & questions ouvertes.)

22. **Notifications & événements à venir** — Les notifications in-app couvrent les changements de planning et les rappels de transfert ; les transferts et changements à venir sont consultables dans un panneau d'événements accessible via une cloche

## Risques & questions ouvertes

- **Faux sentiment de progrès** — Le prochain sujet (fermeture de la fondation : hôte d'API détachable) **n'avance aucun usage visible**. Borner au plus petit incrément qui rend le back démarrable seul et **basculer vite** sur le lot « saisie visible » juste après : le grief des saisies invisibles ne doit pas glisser une itération de plus.
- **Bloc de fondation potentiellement gros** — Détacher l'hôte d'API et recâbler le client HTTP peut déborder ; borner le périmètre et surveiller la dérive au make-gherkin.
- **Faux bug « les saisies n'apparaissent pas » désamorcé** — Cause-racine : les **dates par défaut figées sur une année passée**, qui jettent toute saisie hors de la fenêtre affichée. La **projection n'est pas en cause** : elle lit et colore correctement ce qui couvre la fenêtre. Aucune réparation de la projection n'est ordonnée ; le lot « saisie visible » corrige les dates et la couleur.
- **« Le thème est dégueulasse » est une absence de feature** — Le thème métier (garde d'enfants) n'est pas encore fait (palier lisibilité & thème) ; ce n'est pas une régression.
- **« Les transferts ne s'affichent pas » est un trou par construction** — La projection du planning ne lit **aucun transfert** ; ils apparaîtront au palier « immédiat & événements » (panneau cloche). Symptôme réel, mais pas un comportement vert qui casse.
- **Dette : la vue « définir un transfert » est en retrait** — Elle **écrit le back en direct** (hors canal d'écriture), garde de la **logique dans le template** (convention code-behind non respectée) et porte la **même date figée** sur une année passée. À résorber au moment du rebranchement de l'usage.
- **Question ouverte — workflow demande/accord (révision de règle 21)** — Le PO veut qu'une période ne puisse être réaffectée à l'autre parent qu'après une **demande explicite acceptée**. C'est une révision de la règle « modification directe », pas un correctif ; elle attend le palier « imprévu & échange » et ne génère aucune règle ni sujet tant qu'il n'est pas ouvert.
- **Question ouverte — interdiction/dédoublonnage de slot (révision de règle 14)** — Le PO veut **refuser ou dédoublonner** la pose répétée d'un même slot. C'est une révision du choix v1 « accepté avec avertissement », hors de la boucle courante.
- **Adoption de l'autre parent (risque mortel)** — L'app n'a de valeur que si l'autre parent l'utilise aussi ; sinon le planning est faux. L'ouverture de l'accès (auth réelle) la traite, mais elle vient tard : ne pas laisser glisser indéfiniment.
- **Contraintes du découplage front/API** — Émettre les commandes à travers l'API introduit des contraintes (échanges inter-domaines, sérialisation des commandes, future authentification) absentes quand le front parlait au back en direct ; elles s'accentuent quand l'hôte d'API est distant.
- **Diffusion déclenchée par l'écriture** — Garantir qu'une écriture aboutie déclenche bien l'actualisation temps réel des autres écrans, sans jamais écrire par le canal de diffusion ; à valider en usage réel.
- **Vert qui ment sur la grille** — Un test de composant avec doublures peut afficher la grille alors que le câblage réel échoue. L'acceptation doit vérifier que des slots et périodes **réellement enregistrés** apparaissent positionnés, colorés et **nommés**, pas une grille vide statique.
- **Données du foyer figées dans le code (dette)** — La configuration du foyer est portée par des constantes plutôt que par une donnée persistée ; à résorber au palier d'alimentation des utilisateurs, à ne pas oublier sous prétexte qu'elle est décalée.
- **Coût de saisie du cycle** — Saisir un cycle multi-semaines est lourd ; à valider seulement si le cycle est stable dans le temps. Le transfert dérivé automatiquement réduit ce coût pour le cas nominal.
- **Différenciation** — Vs Cozi / FamilyWall / agenda partagé : le manque précis qu'on couvre mieux reste à nommer.
- **Identité visuelle** — Un thème en accord avec le domaine (garde d'enfants) est attendu (palier lisibilité & thème) ; ergonomie de surface, subordonnée à l'usage par l'arbitre.
