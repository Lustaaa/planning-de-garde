# Planning de garde — Organisation des semaines de garde

> Version 08 · consolide la v07 + docs/sprints/07-lisibilite-theme/99-sprint07-besoins-fin-itération.md. Remplace la v07, qui reste figée en historique.

## Contexte

Une app où parents et intervenants (nounou, grands-parents…) organisent à
l'avance et partagent les semaines de garde des enfants d'un foyer. Le hub
`/planning` est la mémoire partagée du foyer : un calendrier navigable où l'on
lit qui garde qui, où, quand, et d'où l'on agit. La responsabilité de chaque
garde se lit d'un coup d'œil par un code couleur propre à chaque personne,
**doublé du nom du responsable affiché dans la case et d'une légende**. Les
acteurs réels du foyer s'authentifient pour que le planning reflète la réalité
plutôt que des SMS éparpillés.

Le hub repose sur un **back découplé du front** : l'application expose ses
commandes et ses lectures à travers une **API**, ce qui en fait à la fois un
produit utilisable et une **vitrine** ouverte à d'autres clients (front exécuté
côté navigateur, IHM tierce, agents). Le front consomme cette API plutôt que
d'appeler le back en direct. Le découplage va jusqu'à un **hôte d'API
détachable** : le back **démarre seul**, sans le front, et expose son canal
d'écriture à n'importe quel client. Cette fondation est **posée** : l'hôte d'API
tourne détaché, le front s'exécute **dans le navigateur** (WebAssembly) et
consomme l'API comme une **API distante**, ouverte et explorable.

La saisie est **visible** : une saisie posée réapparaît immédiatement dans la
grille, **à la bonne date** (les formulaires datent par défaut sur
« aujourd'hui ») **et en couleur du parent responsable** (la couleur se résout
sur l'identifiant stable de l'acteur). Et la grille est désormais **lisible d'un
coup d'œil** : la couleur seule ne porte plus l'information à elle seule — le
**nom du responsable** est affiché dans la case, **doublé d'une légende**, et
l'app porte un **thème en accord avec son domaine** (garde d'enfants). Ce palier
de lisibilité est **livré**.

Le foyer, lui, reste encore **figé dans le code** : les acteurs (noms et
couleurs) sont fournis par un seed lu par la grille, sans moyen de les modifier.
Le prochain pas d'usage est l'**appropriation des acteurs** : pouvoir **éditer**
les acteurs du foyer (renommer, recolorier) et voir la grille — case et légende —
suivre immédiatement. Cette édition se livre d'abord **en mémoire, dans la
session** (volatile) ; la **durabilité** de la configuration (survie au
redémarrage) reste un pas technique séquencé derrière l'usage.

## Objectif & arbitrage

L'app poursuit trois buts : être un **outil réellement utilisé**, servir de
**vitrine** technique, et rester un **terrain d'apprentissage**. En cas de
conflit entre les trois, on garde ce qui sert l'usage quotidien et on coupe le
reste.

> **Arbitre principal : l'usage réel tranche.** Entre deux besoins qui
> s'opposent, gagne celui qui rend le hub utilisable au quotidien : les
> **saisies visibles** et la **grille lisible** priment sur le confort
> d'outillage. Un **défaut confirmé** prime sur une simple évolution. Cet arbitre
> est **permanent** et tient la main : la fenêtre d'investissement de fondation
> est refermée (cf. ci-dessous), et les paliers d'usage « saisie visible » puis
> « lisibilité & thème » sont **livrés**.

> **Exception bornée — refermée.** Au début du projet, une **fondation
> structurelle** a primé ponctuellement sur l'usage immédiat, parce que le coût
> de la poser (découpler le back en API, **détacher l'hôte d'API**, ouvrir l'app
> à d'autres clients) était minimal alors et **explose une fois l'app grosse**.
> C'était une **fenêtre d'investissement de début de projet**, pas une nouvelle
> règle générale. Cette fenêtre est **close** : le back démarre seul (hôte d'API
> détaché, front WASM autonome). L'arbitre d'usage **a repris la main** dès le
> palier « saisie visible » et ne la rend plus. Toute nouvelle fondation
> technique (persistance durable, empaquetage, hors-ligne) passe désormais
> **derrière l'usage**, jamais devant — sa séquence est subordonnée, jamais
> remplacée.

> **Corollaire « éditable maintenant ≠ durable ».** Rendre une donnée
> **éditable** n'oblige pas à la rendre **durable** dans le même incrément. Un
> palier d'usage peut livrer l'édition d'une configuration **en mémoire, dans la
> session** (volatile, relue immédiatement par la grille) tout en laissant la
> **persistance durable** au palier technique qui la porte — sans contradiction
> ni dette non maîtrisée. La dette volatile est alors **assumée et explicitement
> transitoire** (miroir du seed aujourd'hui en dur). C'est ce corollaire qui
> autorise à livrer l'édition des acteurs du foyer tôt sans tirer la persistance
> en avant (YAGNI) : le « éditable » se gagne maintenant, le « durable » reste
> séquencé.

> **Corollaire de découpe.** Quand le périmètre d'un sujet déborde, on coupe au
> **plus petit incrément** qui rend la grille lisible et utilisable : on
> séquence, on ne reporte jamais tout en bloc. C'est ce corollaire qui a justifié
> de livrer d'abord un calendrier en lecture seule avant d'y brancher l'écriture,
> de borner la séparation de l'hôte d'API au plus petit pas qui rend le back
> démarrable seul, et de tenir la lisibilité (nom + légende) comme observable
> avant le thème de surface. Il reste le **garde-fou de secours** des sujets pris
> en bloc : on coupe au plus petit incrément qui porte un observable d'usage et
> on séquence l'ergonomie de surface derrière.

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
au titre de l'exception bornée ; il est **refermé**, et les paliers d'usage
« saisie visible » et « lisibilité & thème » qui l'ont suivi sont **livrés**.
L'arbitre d'usage tient l'ordre : les paliers d'usage d'abord, les paliers
**techniques en queue de séquence**, derrière tout l'usage.

1. **Fondations — découplage du back en API, hôte détachable** *(exception
   bornée, valeur d'usage immédiate nulle, assumée — **REFERMÉ**)*. Les commandes
   d'écriture (poser un slot, affecter ou supprimer une période, ajuster un
   transfert) sont confiées à un **canal requête/réponse** côté serveur. L'**hôte
   d'API est détaché** : le back **démarre seul** (sans référencer le front) et le
   front, exécuté **dans le navigateur** (WebAssembly), le consomme comme une
   **API distante**. L'API est **explorable** (document OpenAPI + UI interactive)
   et autorise l'**origine du front** (CORS) ; une API injoignable produit un
   **échec clair** (message à l'écran, saisie non appliquée, **sans file ni
   rejeu**). *Socle d'un produit ouvert, posé au moment où le coût était minimal.
   Refermé ; l'usage reprend la main.*

2. **Saisie visible — la saisie réapparaît à la bonne date et en couleur du
   parent** *(reprise d'usage — **LIVRÉ**)*. Une saisie posée **réapparaît
   immédiatement** dans la grille, **à la bonne date** (les dates pré-remplies des
   formulaires valent « aujourd'hui », pas une date figée) **et en couleur du
   parent responsable** (la couleur se résout sur l'identifiant stable de
   l'acteur, pas sur son libellé d'affichage). *Deux choses distinctes ont été
   levées : la **date par défaut = aujourd'hui** et la **couleur du parent**
   (identifiant stable bindé + seed). Le faux bug « les saisies n'apparaissent
   pas » est refermé.*

3. **Lisibilité & thème — qui garde se lit d'un coup d'œil** *(reprise produit —
   **LIVRÉ**)*. La responsabilité de période est **explicite** dans la grille : le
   **nom du responsable** est affiché dans la case **et** une **légende couleur**
   accompagne la grille (plus seulement une teinte de fond) ; un nom trop long est
   **tronqué** tout en restant lisible en entier au **survol** (attribut natif
   portant le nom complet) ; un acteur hors du set connu reste affiché et
   distingué (gris assumé) sans perdre son nom. L'app porte un **thème en accord
   avec le domaine** (garde d'enfants). *Pris en bloc (lisibilité + thème) ; la
   lisibilité a porté l'observable d'usage, le thème est resté ergonomie de
   surface subordonnée. Livré et accepté en usage réel (front WASM + API
   distante).*

4. **Config foyer — édition des acteurs (en mémoire)** *(reprise d'usage —
   **PROCHAIN SUJET**)*. Un écran pour **éditer les acteurs du foyer** : leurs
   **noms** et leurs **couleurs**. Le seed jusqu'ici figé devient **éditable** et
   la **grille (case + légende) reflète immédiatement** le changement *dans la
   session* : renommer Alice → Alicia ou recolorier Bruno met aussitôt à jour la
   case et la légende. L'édition vit **en mémoire** ; **aucune persistance durable
   n'est construite** à ce stade — la survie au redémarrage relève du palier
   technique « persistance réelle ». *Plus petite tranche d'appropriation des
   acteurs : on gagne le « éditable » sans tirer la durabilité en avant (corollaire
   « éditable ≠ durable »). Dette volatile assumée, miroir du seed en dur.*
   **Prochain sujet pour `/2-make-gherkin`.**

5. **Récurrence des périodes — définir le cycle de fond** *(reprise d'usage)*.
   Définir et éditer une **récurrence** sur les périodes de garde (le cycle :
   qui garde par défaut, semaine paire / impaire…), au-delà du modèle de cycle
   déjà présent. *Vient derrière l'édition des acteurs : on récurre des
   responsabilités une fois les acteurs maîtrisés.*

6. **Survol → résumé de la journée** *(reprise d'usage, évolution)*. Au survol
   prolongé (~1 s) d'une case, afficher un **résumé de la journée**, au-delà du
   seul nom complet déjà rendu au survol simple. *Enrichissement, pas une
   réparation : le survol simple (nom complet) est livré et conforme. Le périmètre
   du résumé (périodes / slots / responsable / transferts) est à cadrer au
   make-gherkin et ne doit pas être sous-estimé.*

7. **Calendrier navigable & écriture en contexte** *(reprise produit)* — naviguer
   dans le **passé/futur**, offrir des **vues prédéfinies** (semaine, mois, 4
   semaines glissantes), et poser un slot / affecter ou supprimer une période /
   ajuster un transfert via des **dialogs ouvertes depuis une case**. *L'utilisateur
   agit là où il lit ; l'écriture passe par le canal requête/réponse et la grille
   s'actualise par la diffusion temps réel. Séquencé **derrière** l'appropriation
   des acteurs et la récurrence, par priorité d'usage actée.*

8. **Alimentation & saisie des utilisateurs — config foyer durable** — déclarer et
   **persister** la configuration du foyer (acteurs, lieux, set de couleurs par
   défaut, cycle de fond) plutôt que de la porter en données figées dans le code.
   *Reprend la part **durable** de ce que le palier 4 a rendu éditable en
   volatile : la configuration devient durable, socle de données du modèle
   d'acteurs. C'est ce palier que **recoupe** la persistance réelle (palier
   technique) : la configuration durable est le premier besoin d'un store réel.*

9. **Modèle d'acteurs & foyer** — déclarer les acteurs réels (Admin, Parents,
   Autres) dans un écran de configuration, qui porte aussi la **responsabilité
   récurrente de fond** (le cycle) et le **set de couleurs par défaut**. *Exploite
   la config persistée du palier précédent ; prérequis de l'ouverture de l'accès.*

10. **Immédiat & événements à venir** — « qui récupère ce soir », où est l'enfant
    maintenant, transferts et changements à venir présentés comme événements dans
    un **panneau cloche**, plus comme un tableau permanent. *Valeur quotidienne ;
    expose enfin les transferts, aujourd'hui invisibles par construction.*

11. **Imprévu & échange** — enfant malade / retard / échange de dernière minute,
    transferts **dérivés automatiquement** par défaut et saisie réservée à
    l'exception. *Le plus délicat ; après que l'usage à deux est acquis. Porte la
    question ouverte du **workflow demande/accord** avant réaffectation d'une
    période à l'autre parent.*

12. **Ouverture de l'accès** — landing page et authentification des acteurs réels
    (email via Gmail / Apple / Microsoft) pour lever le risque mortel d'adoption.
    Débloque aussi la **personnalisation des couleurs par utilisateur**. *Vient
    après le socle et le modèle d'acteurs ; à ne pas laisser glisser indéfiniment.*

13. **Persistance réelle — adaptateurs de droite** *(palier technique, derrière
    l'usage)* — remplacer les dépôts **en mémoire** (volatils) par des
    **adaptateurs de droite** vers un store **durable**, derrière les ports
    existants, sans toucher au domaine. *Débloqué par la fondation (palier 1) mais
    **subordonné à l'usage** : ne passe pas devant un incrément produit observable.
    **Recoupe le palier alimentation** (8) — la config foyer durable est son
    premier client, et c'est lui qui éteint la dette volatile de l'édition des
    acteurs (palier 4).*

14. **Saisie hors-ligne (PWA)** *(palier technique, derrière l'usage)* — au-delà
    de l'**échec clair** déjà livré (palier 1), mettre en cache et **mettre en
    file** les écritures faites hors connexion, **rejouées au retour du réseau**.
    *Débloqué par la fondation mais **subordonné à l'usage**. Piste consignée : une
    **file d'écritures côté client** (type *outbox*, IndexedDB) garantissant un
    rejeu « exactement une fois » comme socle minimal ; l'*event sourcing* n'est
    retenu que si offline / rejeu / audit le justifient — à trancher au moment
    d'ouvrir ce palier, pas un prérequis.*

> **Re-séquencement acté.** L'appropriation des acteurs (config foyer, édition
> **volatile**) **passe devant** le « Calendrier navigable », par priorité d'usage
> du PO. La récurrence des périodes et le survol → résumé de la journée sont placés
> **derrière elle**, le calendrier navigable encore derrière. Les paliers
> techniques (persistance réelle, PWA) restent **en queue**, derrière tout l'usage.

> **Transverse (hors incrément dédié)** : l'ergonomie de surface est absorbée au
> fil des incréments calendrier et reste subordonnée à l'usage par l'arbitre. Le
> **thème sombre + bascule clair/sombre** (avec persistance de la préférence) est
> une évolution additive au thème métier livré : consignée, non priorisée, à
> rattacher au futur écran de préférences utilisateur (cf. Risques).

> **Garde-fous hors-spec** : la convention de code interne (chaque vue adossée à
> son code-behind), l'outillage d'**API explorable et documentée** (document
> OpenAPI **et** UI interactive type Swagger-UI / Scalar) et l'**empaquetage en
> conteneurs** (hôte API + front WASM, montables ensemble façon compose, débloqué
> par l'hôte détaché) sont des **garde-fous de structure et d'outillage sans
> observable métier** : ils n'ouvrent ni règle de gestion ni incrément produit, et
> restent subordonnés à l'usage par l'arbitre.

> **Prochain sujet** : palier 4, **config foyer — édition des acteurs (noms +
> couleurs) en mémoire**, relue immédiatement par la grille (case + légende),
> sans persistance durable.

## Mécaniques de base

- Un créneau de garde a un horaire (début → fin), un responsable unique, un lieu et une activité
- Une journée est une suite de créneaux enchaînés
- Le planning suit un cycle de plusieurs semaines qui se répète automatiquement
- Le hub `/planning` est un calendrier navigable façon agenda ; on s'y déplace dans le **passé et le futur**, avec des **vues prédéfinies** (semaine, mois, 4 semaines glissantes). La **fenêtre par défaut** est **4 semaines glissantes à partir de la semaine en cours** ; les slots y sont positionnés dans les cases jour/horaire
- La responsabilité de chaque garde se lit par un **code couleur par personne**, **doublé du nom du responsable affiché dans la case et d'une légende** : la couleur seule ne suffit pas à identifier qui garde. Un nom trop long est **tronqué** dans la case, son **intitulé complet restant lisible au survol** ; un acteur hors du set de couleurs connu reste **affiché et distingué** (teinte neutre assumée) sans perdre son nom. La couleur d'un responsable se résout sur un **identifiant d'acteur stable**, jamais sur son libellé d'affichage : les sélecteurs de saisie fournissent ce même identifiant que la palette, sinon la case retombe sur la couleur neutre
- L'app porte un **thème en accord avec son domaine** (garde d'enfants), au service de la lisibilité d'usage ; c'est une ergonomie de surface, subordonnée à l'usage
- Les formulaires de saisie pré-remplissent leurs dates sur **« aujourd'hui »** (la date de référence), jamais sur une date figée : une saisie tombe ainsi dans la fenêtre affichée et réapparaît immédiatement dans la grille
- La grille est en **lecture seule** : elle consomme les slots et périodes déjà enregistrés, sans écriture. Toute écriture (poser un slot, affecter, surcharger ou supprimer une période, ajuster un transfert) se fait en contexte via des **dialogs** ouvertes depuis une case, alimentées par les acteurs et lieux du foyer
- Les **acteurs du foyer (noms et couleurs) sont éditables** depuis un écran de configuration : renommer ou recolorier un acteur **met immédiatement à jour la grille** (case **et** légende) qui relit la configuration. Cette édition vit **en mémoire, dans la session** : elle ne survit pas (encore) au redémarrage. La **durabilité** de la configuration est un pas distinct, porté par les ports de droite et leur adaptateur durable — l'édition n'attend pas la persistance pour exister
- **Le front est découplé du back par une API** : il ne manipule pas le domaine en direct, il **émet ses commandes et lit ses données à travers l'API**, ce qui ouvre l'app à d'autres clients (front navigateur, IHM tierce, agents). L'**hôte d'API est détaché** : le back démarre seul, sans référencer le front, et le front — exécuté **dans le navigateur** (WebAssembly) — consomme une **API distante**
- **Toute écriture passe par le canal requête/réponse** : aucune vue n'écrit le domaine en direct. Une saisie qui contournerait le canal (appel direct du back) est une dette à résorber, pas un mode de fonctionnement
- **Deux canaux distincts, jamais confondus** : l'**écriture** est confiée à un **canal requête/réponse** (la commande part, la réponse confirme l'effet) ; la **diffusion temps réel** vers les autres acteurs (notifications, actualisation du planning à l'écran) passe par un **canal de diffusion en lecture seule**. On **n'écrit jamais** par le canal de diffusion : une saisie se propage aux autres écrans parce que l'écriture, une fois aboutie, **déclenche** la diffusion
- **Échec clair si l'API distante est injoignable** : la commande non aboutie produit un message à l'écran et la saisie **n'est pas appliquée** ni perdue de vue (elle reste à resoumettre). Aucune mise en file ni rejeu à ce stade — le hors-ligne rejouable est un palier technique ultérieur
- L'**API est explorable** : elle expose un **document de description** (OpenAPI) et une **UI interactive** pour essayer les endpoints (type Swagger-UI / Scalar), et autorise l'**origine du front** par sa configuration CORS
- La responsabilité récurrente de fond (qui garde selon le cycle) se déclare dans la configuration du foyer ; le calendrier ne porte que les **surcharges ponctuelles** d'une période
- La configuration du foyer (acteurs, lieux, set de couleurs, cycle de fond) est une **donnée éditable** : éditable **en mémoire** dès maintenant, et **durable à terme** une fois l'adaptateur de persistance posé. Elle vit derrière les **ports de droite**, dont l'adaptateur durable remplace le dépôt en mémoire — l'édition est livrable avant la durabilité, jamais figée en constante dans le code
- Les transferts et changements à venir sont présentés comme des événements dans un panneau de notifications (cloche), pas comme un tableau permanent
- Trois types d'acteurs : Admin, Parent, Autre — chacun avec un affichage adapté à son type
- La composition du foyer (enfants, parents, acteurs autres) se déclare dans un écran de configuration distinct du planning

## Règles de gestion

### Foyer & acteurs

1. **Multi-enfants** — Un foyer compte au moins un enfant, et peut en compter plusieurs, chacun avec sa propre organisation de garde

2. **Familles recomposées** — Un foyer peut mélanger des enfants de parents différents, tous visibles dans le même planning

3. **Toujours deux parents** — Un foyer a toujours exactement deux parents ; tant qu'un seul est inscrit, il peut saisir les informations de l'autre

4. **Acteurs « autres »** — Un foyer peut avoir N acteurs « autres » (nounou, grands-parents…), éditables par les parents ou par l'acteur lui-même

5. **Édition des acteurs livrable en mémoire (volatile)** — Les acteurs du foyer (leurs **noms** et leurs **couleurs**) sont **éditables** depuis un écran de configuration, et la **grille (case + légende) relit immédiatement** la configuration éditée : renommer ou recolorier un acteur se voit aussitôt. Cette édition vit **en mémoire, dans la session** ; **aucune persistance durable** n'est requise pour qu'elle existe — la survie au redémarrage relève de la règle de durabilité (cf. règle 28) et du palier technique « persistance réelle ». Cet état volatile est une **dette transitoire assumée**, miroir du seed aujourd'hui figé : le « éditable » se livre tôt, le « durable » reste séquencé derrière l'usage

6. **Responsabilité de fond en config, exception au calendrier** — La responsabilité récurrente de garde (le cycle : qui garde par défaut) se déclare dans l'écran de configuration du foyer, en même temps que les acteurs ; elle est **éditable** (en mémoire d'abord, durable à terme). Le calendrier ne sert qu'aux **surcharges ponctuelles** d'une période donnée. Les dialogs d'affectation et de surcharge sont alimentées par les acteurs du foyer

### Rôles & accès

7. **Trois types d'acteurs** — Trois types d'accès : Admin (gère tout, y compris la configuration du foyer), Parent (gère les créneaux, lieux, acteurs et invitations de son foyer), Autre (consultation et édition limitée à ses propres informations). L'affichage s'adapte au type

8. **Modification réservée aux parents et à l'admin** — Seul un Parent (ou l'Admin) peut créer, éditer ou supprimer un créneau ou une période ; un acteur « Autre » n'édite que ses propres informations

### Planning & créneaux

9. **Responsable unique** — Un créneau a toujours un seul responsable (pas de « X ou Y »)

10. **Cycle récurrent, éditable** — Le planning se répète selon un cycle de plusieurs semaines (ex : semaine paire / impaire). Ce cycle (la **récurrence des périodes**) est **définissable et éditable** depuis la configuration du foyer, et non figé dans le code

11. **Exception ponctuelle** — Un jour précis peut être surchargé sans casser le cycle de fond ; le cycle reprend ensuite

12. **Lieux éditables** — La liste des lieux (domicile A/B, école, nounou, activité…) est librement modifiable et sert de référentiel aux sélecteurs de saisie

13. **Grille en lecture seule** — La grille agenda consomme les slots et périodes déjà enregistrés et les rend (positionnés dans leur case, colorés par responsable, **nommés**) sans jamais écrire ; toute écriture passe par une dialog ouverte depuis une case

14. **Suppression de période** — Un Parent (ou l'Admin) peut supprimer une période de garde ; c'est une action d'écriture menée depuis une dialog contextuelle, hors de la grille en lecture pure

15. **Pose répétée d'un même slot acceptée avec avertissement** — Un slot qui chevauche ou redouble un slot existant est **accepté**, accompagné d'un **avertissement** ; il n'est ni refusé ni dédoublonné. (Une demande d'interdiction/dédoublonnage est en attente comme révision de règle — cf. Risques & questions ouvertes.)

16. **Date de saisie par défaut = aujourd'hui** — Les formulaires de saisie (poser un slot, affecter ou surcharger une période) pré-remplissent leurs dates sur la **date de référence « aujourd'hui »**, jamais sur une date figée : la saisie tombe ainsi dans la fenêtre affichée et **réapparaît immédiatement** dans la grille. Une date par défaut figée hors fenêtre est une non-conformité à corriger, pas un comportement attendu

### Code couleur & lisibilité

17. **Responsabilité lisible : couleur par personne + nom + légende** — La responsabilité de garde se lit dans la grille par une couleur qui distingue chaque **personne** (Parent A ≠ Parent B), pas seulement son type de rôle ; cette couleur est **doublée du nom du responsable affiché dans la case et d'une légende**, car la teinte seule ne suffit pas à dire qui garde. Un **nom trop long** est **tronqué** dans la case tout en restant lisible **en entier au survol** ; un acteur **hors du set de couleurs connu** reste **affiché et distingué** (teinte neutre assumée) **sans perdre son nom**

18. **Couleur résolue sur un identifiant d'acteur stable** — La couleur d'un responsable se résout sur l'**identifiant stable** de l'acteur, jamais sur son libellé d'affichage. Les sélecteurs de saisie (affectation, surcharge) **fournissent ce même identifiant** que la palette : un libellé qui ne correspond pas à un identifiant connu fait retomber la case sur la **couleur neutre**, ce qu'on ne veut pas. Une case grise là où un responsable est affecté trahit un libellé fourni à la place de l'identifiant : c'est le défaut à localiser, pas la résolution elle-même

19. **Set de couleurs par défaut, recoloriable en session** — Un jeu de couleurs par défaut, déclaré avec le foyer, différencie d'emblée les responsables tant qu'aucune personnalisation n'a été faite ; c'est ce set qui s'applique avant l'ouverture de l'accès. Ce set est **recoloriable** depuis l'écran de config (édition en mémoire, cf. règle 5), et la grille suit ; la **personnalisation par utilisateur authentifié** (règle 20) reste un pas distinct lié à l'ouverture de l'accès

20. **Personnalisation des couleurs par utilisateur** — Une fois identifié, chaque acteur peut personnaliser les couleurs qu'il assigne aux acteurs qu'il voit ; cette personnalisation dépend de l'authentification (séquence, ouverture de l'accès) et n'altère pas le set par défaut vu par les autres

21. **Thème en accord avec le domaine** — L'app porte un **thème** cohérent avec son domaine (garde d'enfants), au service de la lisibilité d'usage. C'est une **ergonomie de surface subordonnée à l'usage** par l'arbitre : il n'ouvre aucune règle métier. Un **thème sombre avec bascule clair/sombre** (et persistance de la préférence) en est une **évolution additive**, consignée et non priorisée, à rattacher au futur écran de préférences utilisateur

### Survol & détail de la case

22. **Survol : du nom complet au résumé de la journée** — Au survol d'une case, l'app expose un complément d'information sans quitter la grille. Le **survol simple** affiche le **nom complet** du responsable (utile quand le nom est tronqué) : c'est le comportement **livré et conforme**. Un **survol enrichi** est une **évolution** prévue : après un survol prolongé (~1 s), afficher un **résumé de la journée**. Le périmètre de ce résumé (périodes, slots, responsable, transferts) est à cadrer au moment de le scénariser ; ce n'est pas un correctif du survol simple, qui n'est pas défaillant

### Transferts

23. **Transfert dérivé par défaut** — Le transfert d'un enfant (qui dépose, qui récupère, à quelle heure) est dérivé automatiquement du planning par défaut : c'est le cas nominal, sans saisie

24. **Transfert modifiable et ponctuel** — Un transfert auto-calculé peut être modifié, et des transferts ponctuels peuvent être proposés ou ajoutés à l'exception (urgence, changement d'emploi du temps)

### Modifications & notifications

25. **Modification directe** — Un Parent applique son changement immédiatement ; les autres acteurs concernés sont notifiés (pas de workflow de validation). (Une demande de workflow demande/accord avant réaffectation d'une période à l'autre parent est en attente comme révision de règle, rattachée au palier « imprévu & échange » — cf. Risques & questions ouvertes.)

26. **Notifications & événements à venir** — Les notifications in-app couvrent les changements de planning et les rappels de transfert ; les transferts et changements à venir sont consultables dans un panneau d'événements accessible via une cloche

### Accès aux données & exploitation

27. **Écriture par le canal, échec clair si l'API est injoignable** — Toute écriture passe par le **canal requête/réponse** vers l'API distante ; aucune vue n'écrit le domaine en direct. Si l'API est **injoignable**, la commande échoue **clairement** (message à l'écran, saisie **non appliquée** et conservée à resoumettre), **sans mise en file ni rejeu** à ce stade. Le hors-ligne rejouable (cache + file d'écritures) est un palier technique ultérieur, derrière l'usage

28. **API explorable et origine du front autorisée** — L'API expose un **document de description** (OpenAPI) **et** une **UI interactive** d'exploration des endpoints (type Swagger-UI / Scalar), et autorise l'**origine du front** (CORS). C'est un garde-fou d'outillage, sans observable métier : aucune règle de gestion n'en dépend

29. **Données derrière les ports, durables à terme** — Les slots, périodes, transferts et la configuration du foyer vivent derrière des **ports** ; leur stockage **en mémoire** est un état transitoire à remplacer par un **adaptateur durable** (palier technique « persistance réelle »), sans toucher au domaine ni aux règles. La donnée du foyer n'est pas une constante figée dans le code. Cette règle porte la **durabilité** ; elle est **distincte de l'édition** (règle 5), qui se livre en mémoire avant elle : rendre une donnée éditable n'oblige pas à la rendre durable dans le même incrément

## Risques & questions ouvertes

- **L'usage tient la main — ne pas enchaîner un sprint sans valeur produit.** Deux sprints structurels (fondation, hôte d'API) puis deux sprints d'usage (saisie visible, lisibilité & thème) sont derrière nous. Les prochains sujets (édition des acteurs, récurrence, survol enrichi, calendrier navigable) restent des paliers **d'usage** ; les paliers techniques débloqués (persistance, PWA, Docker) sont tentants mais **doivent rester derrière l'usage**. Les laisser passer devant ferait un sprint sans valeur observable, à l'encontre de l'arbitre.
- **Édition ≠ persistance (corollaire actif).** Le palier « config foyer » livre l'édition des acteurs **en mémoire, dans la session** : la grille suit, mais rien ne survit au redémarrage. C'est **assumé et transitoire** (miroir du seed en dur), pas un oubli. Ne **pas** tirer la persistance durable en avant sous prétexte que l'édition existe : la durabilité reste au palier technique « persistance réelle », qui éteindra cette dette. Inversement, ne pas reprocher à l'édition de ne pas persister — ce n'est pas un défaut, c'est la découpe.
- **Périmètre « résumé de la journée » (survol enrichi) non défini** — périodes ? slots ? responsable ? transferts ? Sujet potentiellement plus gros qu'il n'y paraît, proche du « qui récupère ce soir » (palier immédiat). À **cadrer au make-gherkin** quand le survol (palier 6) sera pris ; ne pas le sous-estimer comme « simple tooltip ».
- **Survol = évolution, PAS un bug** — Le survol simple (attribut natif portant le nom complet sur le nom tronqué) est **conforme et accepté** (palier 3, runtime vert). Malgré le ressenti « tout est ok sauf le survol », il n'y a **rien de cassé** : le résumé de la journée est un comportement **neuf** à scénariser, jamais une réparation à envoyer en `/3` ciblé.
- **Couplage thème sombre + toggle / préférences utilisateur** — La **persistance d'une préférence de thème** rejoint naturellement le futur écran de config / préférences user. À arbitrer quand le thème sombre remontera : l'embarquer avec la gestion utilisateurs ou le garder isolé. Évolution additive au thème métier livré, non priorisée.
- **Idées consignées non prioritaires** — Indicateur de **présence de l'autre parent** (temps réel), **multi-enfants** (déjà règle 1), **familles recomposées** (déjà règle 2) : besoins reconnus, séquencés derrière l'usage prioritaire, sans règle ni palier dédié tant que l'usage ne les appelle pas.
- **Question ouverte — workflow demande/accord (révision de règle 25)** — Le PO veut qu'une période ne puisse être réaffectée à l'autre parent qu'après une **demande explicite acceptée**. C'est une révision de la règle « modification directe », pas un correctif ; elle attend le palier « imprévu & échange » et ne génère aucune règle ni sujet tant qu'il n'est pas ouvert.
- **Question ouverte — interdiction/dédoublonnage de slot (révision de règle 15)** — Le PO veut **refuser ou dédoublonner** la pose répétée d'un même slot. C'est une révision du choix v1 « accepté avec avertissement », hors de la boucle courante.
- **Adoption de l'autre parent (risque mortel)** — L'app n'a de valeur que si l'autre parent l'utilise aussi ; sinon le planning est faux. L'ouverture de l'accès (auth réelle, palier 12) la traite, mais elle vient tard : **à ne pas laisser glisser indéfiniment** derrière la technique. Aucun des paliers techniques en queue ni des prochains incréments d'usage ne lève ce risque.
- **Contraintes du découplage front/API distant** — Émettre les commandes à travers une API **distante** introduit des contraintes (échanges inter-domaines, sérialisation des commandes, configuration de l'URL d'API, future authentification) absentes quand le front parlait au back en direct ; elles s'accentuent avec l'hôte détaché et le front WASM.
- **Diffusion déclenchée par l'écriture** — Garantir qu'une écriture aboutie déclenche bien l'actualisation temps réel des autres écrans, sans jamais écrire par le canal de diffusion ; à valider en usage réel, le hub SignalR étant consommé côté navigateur.
- **Vert qui ment sur la grille** — Un test de composant avec doublures peut afficher la grille alors que le câblage réel échoue. L'acceptation doit vérifier que des slots et périodes **réellement enregistrés** apparaissent positionnés, colorés et **nommés**, pas une grille vide statique. Les scénarios runtime (front WASM + API distante, sans doublure sur le chemin observé) sont le rempart contre l'early-green — il vaudra particulièrement pour le palier « config foyer » : une édition d'acteur doit se voir sur une grille **réellement câblée**, pas seulement dans un test à doublures.
- **Données du foyer en mémoire (dette à terme)** — Slots, périodes, transferts et config foyer vivent dans des dépôts **en mémoire** (volatils) ; l'édition des acteurs (palier 4) **assume** sciemment cette volatilité. À remplacer par un **adaptateur durable** au palier « persistance réelle », derrière l'usage. À ne pas oublier sous prétexte qu'il est décalé.
- **Hors-ligne rejouable — piste à trancher au palier PWA** — Au-delà de l'échec clair livré, une **file d'écritures côté client** (type *outbox*, IndexedDB) garantissant un rejeu « exactement une fois » est la piste minimale ; l'*event sourcing* n'est retenu que si offline / rejeu / audit le justifient. Décision **au moment d'ouvrir le palier**, pas un prérequis.
- **Coût de saisie du cycle** — Saisir un cycle multi-semaines est lourd ; le palier « récurrence des périodes » devra le rendre supportable. À valider seulement si le cycle est stable dans le temps. Le transfert dérivé automatiquement réduit ce coût pour le cas nominal.
- **Différenciation** — Vs Cozi / FamilyWall / agenda partagé : le manque précis qu'on couvre mieux reste à nommer.
