# Planning de garde — Organisation des semaines de garde

> Version 07 · consolide la v06 + docs/sprints/06-saisie-visible/99-sprint06-besoins-fin-itération.md. Remplace la v06, qui reste figée en historique.

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
détachable** : le back **démarre seul**, sans le front, et expose son canal
d'écriture à n'importe quel client. Cette fondation est **posée** : l'hôte d'API
tourne détaché, le front s'exécute **dans le navigateur** (WebAssembly) et
consomme l'API comme une **API distante**, ouverte et explorable.

La saisie est désormais **visible** : une saisie posée réapparaît immédiatement
dans la grille, **à la bonne date** (les formulaires datent par défaut sur
« aujourd'hui ») **et en couleur du parent responsable** (la couleur se résout
sur l'identifiant stable de l'acteur). Reste à rendre la grille **lisible d'un
coup d'œil** : la couleur seule ne dit pas encore qui garde — il manque le **nom
du responsable** et une **légende** dans les cases — et l'app attend un **thème en
accord avec son domaine** (garde d'enfants).

## Objectif & arbitrage

L'app poursuit trois buts : être un **outil réellement utilisé**, servir de
**vitrine** technique, et rester un **terrain d'apprentissage**. En cas de
conflit entre les trois, on garde ce qui sert l'usage quotidien et on coupe le
reste.

> **Arbitre principal : l'usage réel tranche.** Entre deux besoins qui
> s'opposent, gagne celui qui rend le hub utilisable au quotidien : les **saisies
> visibles** priment sur l'ergonomie du calendrier, qui prime sur le confort
> d'outillage. Un **défaut confirmé** prime sur une simple évolution. Cet arbitre
> est **permanent** et tient la main : la fenêtre d'investissement de fondation
> est refermée (cf. ci-dessous), et le palier d'usage « saisie visible » est
> **livré**.

> **Exception bornée — refermée.** Au début du projet, une **fondation
> structurelle** a primé ponctuellement sur l'usage immédiat, parce que le coût
> de la poser (découpler le back en API, **détacher l'hôte d'API**, ouvrir l'app
> à d'autres clients) était minimal alors et **explose une fois l'app grosse**.
> C'était une **fenêtre d'investissement de début de projet**, pas une nouvelle
> règle générale. Cette fenêtre est **close** : le back démarre seul (hôte d'API
> détaché, front WASM autonome). L'arbitre d'usage **a repris la main** dès le
> palier « saisie visible » (désormais livré) et ne la rend plus. Toute nouvelle
> fondation technique (persistance durable, empaquetage, hors-ligne) passe
> désormais **derrière l'usage**, jamais devant — sa séquence est subordonnée,
> jamais remplacée.

> **Corollaire de découpe.** Quand le périmètre d'un sujet déborde, on coupe au
> **plus petit incrément** qui rend la grille lisible et utilisable : on
> séquence, on ne reporte jamais tout en bloc. C'est ce corollaire qui a justifié
> de livrer d'abord un calendrier en lecture seule avant d'y brancher l'écriture,
> et de borner la séparation de l'hôte d'API au plus petit pas qui rend le back
> démarrable seul. Le palier « lisibilité & thème » étant pris **en bloc** (nom +
> légende + thème), ce corollaire est le **garde-fou de secours** : si le
> périmètre déborde au découpage des scénarios, on coupe au plus petit incrément
> lisible et on séquence le thème **derrière** la lisibilité — la lisibilité porte
> l'observable d'usage, le thème reste ergonomie de surface subordonnée à l'usage.

> **Révisions de règle hors boucle.** Une demande qui contredit une règle déjà
> actée n'est **pas** un correctif : c'est une révision de spec, qui n'entre pas
> dans le séquencement courant et attend le palier qui la porte. Deux telles
> demandes sont en attente (cf. Risques & questions ouvertes) : le workflow
> demande/accord avant réaffectation (palier « imprévu & échange ») et
> l'interdiction/dédoublonnage de la pose répétée d'un même slot.

## Séquence de livraison

Tous les besoins comptent, mais ils sont **séquencés** (pas livrés en bloc).
Chaque palier doit être adopté avant le suivant ; chacun se borne au plus petit
pas qui apporte une valeur lisible. Le palier de fondations a ouvert la séquence
au titre de l'exception bornée ; il est **refermé**, et le palier d'usage
« saisie visible » qui l'a suivi est **livré**. L'arbitre d'usage tient l'ordre :
les paliers d'usage d'abord, les paliers **techniques en queue de séquence**,
derrière tout l'usage.

1. **Fondations — découplage du back en API, hôte détachable** *(exception
   bornée, valeur d'usage immédiate nulle, assumée — **REFERMÉ**)*. Les commandes
   d'écriture (poser un slot, affecter ou supprimer une période, ajuster un
   transfert) sont confiées à un **canal requête/réponse** côté serveur. L'**hôte
   d'API est détaché** : le back **démarre seul** (sans référencer le front) et le
   front, exécuté **dans le navigateur** (WebAssembly), le consomme comme une
   **API distante**. L'API est **explorable** (document OpenAPI + UI interactive)
   et autorise l'**origine du front** (CORS) ; une API injoignable produit un
   **échec clair** (message à l'écran, saisie non appliquée, **sans file ni
   rejeu**). *Socle d'un produit ouvert (front navigateur, IHM tierce, agents),
   posé au moment où le coût était minimal. Refermé ; l'usage reprend la main.*
2. **Saisie visible — la saisie réapparaît à la bonne date et en couleur du
   parent** *(reprise d'usage — **LIVRÉ**)*. Une saisie posée **réapparaît
   immédiatement** dans la grille, **à la bonne date** (les dates pré-remplies des
   formulaires valent « aujourd'hui », pas une date figée) **et en couleur du
   parent responsable** (la couleur se résout sur l'identifiant stable de
   l'acteur, pas sur son libellé d'affichage). *Deux choses distinctes ont été
   levées : (A) la **date par défaut = aujourd'hui** — les formulaires datent sur
   la date de référence, la saisie tombe dans la fenêtre affichée ; (B) la
   **couleur du parent** — les sélecteurs de saisie et le seed fournissent
   l'**identifiant stable** que la palette attend, plus le libellé qui retombait
   au neutre. Le faux bug « les saisies n'apparaissent pas » est refermé.*
3. **Lisibilité & thème** *(reprise produit — traités **en bloc**, prochain
   sujet)* — rendre la responsabilité de période **explicite** dans la grille
   (**nom du responsable** affiché dans la case + **légende couleur**, pas
   seulement une teinte de fond) **et** habiller l'app d'un **thème en accord avec
   le domaine** (garde d'enfants). *La grille colore déjà les cases (couleur par
   personne sur identifiant stable, palier 2) mais ne dit pas encore qui garde ;
   on la rend lisible d'un coup d'œil. Pris en bloc par choix produit (lisibilité
   + thème indissociables) ; si le périmètre déborde, le corollaire de découpe
   reprend la main — lisibilité d'abord, thème séquencé derrière.* **Prochain
   sujet pour `/2-make-gherkin`.**
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
   devient éditable et durable ; socle de données du modèle d'acteurs. C'est ce
   palier que **recoupe** la persistance réelle (palier technique 10) : la
   configuration durable est le premier besoin d'un store réel.*
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
10. **Persistance réelle — adaptateurs de droite** *(palier technique, derrière
    l'usage)* — remplacer les dépôts **en mémoire** (volatils) par des
    **adaptateurs de droite** vers un store **durable**, derrière les ports
    existants, sans toucher au domaine. *Débloqué par la fondation (palier 1) mais
    **subordonné à l'usage** : ne passe pas devant un incrément produit observable.
    **Recoupe le palier alimentation** (5) — la config foyer durable est son
    premier client.*
11. **Saisie hors-ligne (PWA)** *(palier technique, derrière l'usage)* — au-delà
    de l'**échec clair** déjà livré (palier 1), mettre en cache et **mettre en
    file** les écritures faites hors connexion, **rejouées au retour du réseau**.
    *Débloqué par la fondation mais **subordonné à l'usage**. Piste consignée : une
    **file d'écritures côté client** (type *outbox*, IndexedDB) garantissant un
    rejeu « exactement une fois » comme socle minimal ; l'*event sourcing* n'est
    retenu que si offline / rejeu / audit le justifient — à trancher au moment
    d'ouvrir ce palier, pas un prérequis.*

> **Transverse (hors incrément dédié)** : l'ergonomie de surface est absorbée au
> fil des incréments calendrier et reste subordonnée à l'usage par l'arbitre.

> **Garde-fous hors-spec** : la convention de code interne (chaque vue adossée à
> son code-behind), l'outillage d'**API explorable et documentée** (document
> OpenAPI **et** UI interactive type Swagger-UI / Scalar) et l'**empaquetage en
> conteneurs** (hôte API + front WASM, montables ensemble façon compose, débloqué
> par l'hôte détaché) sont des **garde-fous de structure et d'outillage sans
> observable métier** : ils n'ouvrent ni règle de gestion ni incrément produit, et
> restent subordonnés à l'usage par l'arbitre.

> **Prochain sujet** : palier 3, **lisibilité & thème** — nom du responsable +
> légende couleur dans la grille, et thème métier (garde d'enfants), pris en bloc.

## Mécaniques de base

- Un créneau de garde a un horaire (début → fin), un responsable unique, un lieu et une activité
- Une journée est une suite de créneaux enchaînés
- Le planning suit un cycle de plusieurs semaines qui se répète automatiquement
- Le hub `/planning` est un calendrier navigable façon agenda ; on s'y déplace dans le **passé et le futur**, avec des **vues prédéfinies** (semaine, mois, 4 semaines glissantes). La **fenêtre par défaut** est **4 semaines glissantes à partir de la semaine en cours** ; les slots y sont positionnés dans les cases jour/horaire
- La responsabilité de chaque garde se lit par un **code couleur par personne**, **doublé du nom du responsable et d'une légende** : la couleur seule ne suffit pas à identifier qui garde. La couleur d'un responsable se résout sur un **identifiant d'acteur stable**, jamais sur son libellé d'affichage : les sélecteurs de saisie fournissent ce même identifiant que la palette, sinon la case retombe sur la couleur neutre
- Les formulaires de saisie pré-remplissent leurs dates sur **« aujourd'hui »** (la date de référence), jamais sur une date figée : une saisie tombe ainsi dans la fenêtre affichée et réapparaît immédiatement dans la grille
- La grille est en **lecture seule** : elle consomme les slots et périodes déjà enregistrés, sans écriture. Toute écriture (poser un slot, affecter, surcharger ou supprimer une période, ajuster un transfert) se fait en contexte via des **dialogs** ouvertes depuis une case, alimentées par les acteurs et lieux du foyer
- **Le front est découplé du back par une API** : il ne manipule pas le domaine en direct, il **émet ses commandes et lit ses données à travers l'API**, ce qui ouvre l'app à d'autres clients (front navigateur, IHM tierce, agents). L'**hôte d'API est détaché** : le back démarre seul, sans référencer le front, et le front — exécuté **dans le navigateur** (WebAssembly) — consomme une **API distante**
- **Toute écriture passe par le canal requête/réponse** : aucune vue n'écrit le domaine en direct. Une saisie qui contournerait le canal (appel direct du back) est une dette à résorber, pas un mode de fonctionnement
- **Deux canaux distincts, jamais confondus** : l'**écriture** est confiée à un **canal requête/réponse** (la commande part, la réponse confirme l'effet) ; la **diffusion temps réel** vers les autres acteurs (notifications, actualisation du planning à l'écran) passe par un **canal de diffusion en lecture seule**. On **n'écrit jamais** par le canal de diffusion : une saisie se propage aux autres écrans parce que l'écriture, une fois aboutie, **déclenche** la diffusion
- **Échec clair si l'API distante est injoignable** : la commande non aboutie produit un message à l'écran et la saisie **n'est pas appliquée** ni perdue de vue (elle reste à resoumettre). Aucune mise en file ni rejeu à ce stade — le hors-ligne rejouable est un palier technique ultérieur
- L'**API est explorable** : elle expose un **document de description** (OpenAPI) et une **UI interactive** pour essayer les endpoints (type Swagger-UI / Scalar), et autorise l'**origine du front** par sa configuration CORS
- La responsabilité récurrente de fond (qui garde selon le cycle) se déclare dans la configuration du foyer ; le calendrier ne porte que les **surcharges ponctuelles** d'une période
- La configuration du foyer (acteurs, lieux, set de couleurs, cycle de fond) est une **donnée persistée et éditable**, pas une constante figée dans le code ; elle vit derrière les **ports de droite**, dont l'adaptateur durable remplace à terme le dépôt en mémoire
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

15. **Date de saisie par défaut = aujourd'hui** — Les formulaires de saisie (poser un slot, affecter ou surcharger une période) pré-remplissent leurs dates sur la **date de référence « aujourd'hui »**, jamais sur une date figée : la saisie tombe ainsi dans la fenêtre affichée et **réapparaît immédiatement** dans la grille. Une date par défaut figée hors fenêtre est une non-conformité à corriger, pas un comportement attendu

### Code couleur & lisibilité

16. **Responsabilité lisible : couleur par personne + nom + légende** — La responsabilité de garde se lit dans la grille par une couleur qui distingue chaque **personne** (Parent A ≠ Parent B), pas seulement son type de rôle ; cette couleur est **doublée du nom du responsable affiché dans la case et d'une légende**, car la teinte seule ne suffit pas à dire qui garde

17. **Couleur résolue sur un identifiant d'acteur stable** — La couleur d'un responsable se résout sur l'**identifiant stable** de l'acteur, jamais sur son libellé d'affichage. Les sélecteurs de saisie (affectation, surcharge) **fournissent ce même identifiant** que la palette : un libellé qui ne correspond pas à un identifiant connu fait retomber la case sur la **couleur neutre**, ce qu'on ne veut pas. Une case grise là où un responsable est affecté trahit un libellé fourni à la place de l'identifiant : c'est le défaut à localiser, pas la résolution elle-même

18. **Set de couleurs par défaut** — Un jeu de couleurs par défaut, déclaré avec le foyer, différencie d'emblée les responsables tant qu'aucune personnalisation n'a été faite ; c'est ce set qui s'applique avant l'ouverture de l'accès

19. **Personnalisation des couleurs par utilisateur** — Une fois identifié, chaque acteur peut personnaliser les couleurs qu'il assigne aux acteurs qu'il voit ; cette personnalisation dépend de l'authentification (séquence, ouverture de l'accès) et n'altère pas le set par défaut vu par les autres

20. **Thème en accord avec le domaine** — L'app porte un **thème** cohérent avec son domaine (garde d'enfants), au service de la lisibilité d'usage. C'est une **ergonomie de surface subordonnée à l'usage** par l'arbitre : il n'ouvre aucune règle métier et reste séquençable derrière la lisibilité si le périmètre déborde

### Transferts

21. **Transfert dérivé par défaut** — Le transfert d'un enfant (qui dépose, qui récupère, à quelle heure) est dérivé automatiquement du planning par défaut : c'est le cas nominal, sans saisie

22. **Transfert modifiable et ponctuel** — Un transfert auto-calculé peut être modifié, et des transferts ponctuels peuvent être proposés ou ajoutés à l'exception (urgence, changement d'emploi du temps)

### Modifications & notifications

23. **Modification directe** — Un Parent applique son changement immédiatement ; les autres acteurs concernés sont notifiés (pas de workflow de validation). (Une demande de workflow demande/accord avant réaffectation d'une période à l'autre parent est en attente comme révision de règle, rattachée au palier « imprévu & échange » — cf. Risques & questions ouvertes.)

24. **Notifications & événements à venir** — Les notifications in-app couvrent les changements de planning et les rappels de transfert ; les transferts et changements à venir sont consultables dans un panneau d'événements accessible via une cloche

### Accès aux données & exploitation

25. **Écriture par le canal, échec clair si l'API est injoignable** — Toute écriture passe par le **canal requête/réponse** vers l'API distante ; aucune vue n'écrit le domaine en direct. Si l'API est **injoignable**, la commande échoue **clairement** (message à l'écran, saisie **non appliquée** et conservée à resoumettre), **sans mise en file ni rejeu** à ce stade. Le hors-ligne rejouable (cache + file d'écritures) est un palier technique ultérieur, derrière l'usage

26. **API explorable et origine du front autorisée** — L'API expose un **document de description** (OpenAPI) **et** une **UI interactive** d'exploration des endpoints (type Swagger-UI / Scalar), et autorise l'**origine du front** (CORS). C'est un garde-fou d'outillage, sans observable métier : aucune règle de gestion n'en dépend

27. **Données derrière les ports, durables à terme** — Les slots, périodes, transferts et la configuration du foyer vivent derrière des **ports** ; leur stockage **en mémoire** est un état transitoire à remplacer par un **adaptateur durable** (palier technique « persistance réelle »), sans toucher au domaine ni aux règles. La donnée du foyer n'est pas une constante figée dans le code

## Risques & questions ouvertes

- **L'usage tient la main — ne pas enchaîner un sprint sans valeur produit.** Deux sprints structurels (fondation, hôte d'API) puis un sprint d'usage (saisie visible) sont derrière nous. Le prochain sujet (« lisibilité & thème ») reste un palier **d'usage** ; les paliers techniques débloqués (persistance, PWA, Docker) sont tentants mais **doivent rester derrière l'usage**. Les laisser passer devant ferait un sprint sans valeur observable, à l'encontre de l'arbitre.
- **Lisibilité ≠ couleur déjà livrée.** La couleur (identifiant stable → palette) **fonctionne** depuis le palier « saisie visible » ; ce qui manque encore, c'est le **nom du responsable affiché dans la case** et la **légende** qui disent qui garde. La grille colore les cases mais ne les nomme pas. À ne pas confondre : l'observable du palier « lisibilité & thème » est le nom + la légende, pas la teinte.
- **Débordement du choix « en bloc ».** Nom + légende + thème dans un seul sujet peut gonfler. Activer le **corollaire de découpe** au make-gherkin si besoin : couper au plus petit incrément lisible (lisibilité d'abord), séquencer le thème derrière. L'arbitrage de découpe revient au PO s'il survient.
- **« Le thème est dégueulasse » est une absence de feature** — Le thème métier (garde d'enfants) n'est pas encore fait (palier « lisibilité & thème ») ; ce n'est pas une régression, mais une évolution à livrer, jamais un bug.
- **« Les transferts ne s'affichent pas » est un trou par construction** — La projection du planning ne lit **aucun transfert** ; ils apparaîtront au palier « immédiat & événements » (panneau cloche). Symptôme réel, mais pas un comportement vert qui casse.
- **Dette levée : la vue « définir un transfert »** — Elle portait une écriture en direct, de la logique dans le template et une date figée. Le rebranchement WASM l'a passée par le canal HTTP (`/api/canal/definir-transfert`), et le palier « saisie visible » l'a passée en **code-behind** avec date par défaut « aujourd'hui » et sélecteurs bindant l'identifiant stable. Reste à surveiller sa cohérence au fil des paliers suivants.
- **Question ouverte — workflow demande/accord (révision de règle 23)** — Le PO veut qu'une période ne puisse être réaffectée à l'autre parent qu'après une **demande explicite acceptée**. C'est une révision de la règle « modification directe », pas un correctif ; elle attend le palier « imprévu & échange » et ne génère aucune règle ni sujet tant qu'il n'est pas ouvert.
- **Question ouverte — interdiction/dédoublonnage de slot (révision de règle 14)** — Le PO veut **refuser ou dédoublonner** la pose répétée d'un même slot. C'est une révision du choix v1 « accepté avec avertissement », hors de la boucle courante.
- **Adoption de l'autre parent (risque mortel)** — L'app n'a de valeur que si l'autre parent l'utilise aussi ; sinon le planning est faux. L'ouverture de l'accès (auth réelle, palier 9) la traite, mais elle vient tard : **à ne pas laisser glisser indéfiniment** derrière la technique. Aucun des paliers techniques en queue ne lève ce risque.
- **Contraintes du découplage front/API distant** — Émettre les commandes à travers une API **distante** introduit des contraintes (échanges inter-domaines, sérialisation des commandes, configuration de l'URL d'API, future authentification) absentes quand le front parlait au back en direct ; elles s'accentuent avec l'hôte détaché et le front WASM.
- **Diffusion déclenchée par l'écriture** — Garantir qu'une écriture aboutie déclenche bien l'actualisation temps réel des autres écrans, sans jamais écrire par le canal de diffusion ; à valider en usage réel, le hub SignalR étant désormais consommé côté navigateur.
- **Vert qui ment sur la grille** — Un test de composant avec doublures peut afficher la grille alors que le câblage réel échoue. L'acceptation doit vérifier que des slots et périodes **réellement enregistrés** apparaissent positionnés, colorés et **nommés**, pas une grille vide statique. Les scénarios runtime (front WASM + API distante, sans doublure sur le chemin observé) sont le rempart contre l'early-green — il vaut particulièrement pour le palier « lisibilité » : le nom du responsable doit apparaître sur une grille réellement câblée.
- **Données du foyer en mémoire (dette à terme)** — Slots, périodes, transferts et config foyer vivent dans des dépôts **en mémoire** (volatils) ; à remplacer par un **adaptateur durable** au palier « persistance réelle », derrière l'usage. À ne pas oublier sous prétexte qu'il est décalé.
- **Hors-ligne rejouable — piste à trancher au palier PWA** — Au-delà de l'échec clair livré, une **file d'écritures côté client** (type *outbox*, IndexedDB) garantissant un rejeu « exactement une fois » est la piste minimale ; l'*event sourcing* n'est retenu que si offline / rejeu / audit le justifient. Décision **au moment d'ouvrir le palier**, pas un prérequis.
- **Coût de saisie du cycle** — Saisir un cycle multi-semaines est lourd ; à valider seulement si le cycle est stable dans le temps. Le transfert dérivé automatiquement réduit ce coût pour le cas nominal.
- **Différenciation** — Vs Cozi / FamilyWall / agenda partagé : le manque précis qu'on couvre mieux reste à nommer.
