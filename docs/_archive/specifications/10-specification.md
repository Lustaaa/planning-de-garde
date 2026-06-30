# Planning de garde — Organisation des semaines de garde

> Version 10 · consolide la v09 + docs/sprints/09-config-foyer-persistante/99-sprint09-besoins-fin-itération.md. Remplace la v09, qui reste figée en historique.

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
sur l'identifiant stable de l'acteur). La grille est **lisible d'un coup
d'œil** : la couleur seule ne porte plus l'information — le **nom du
responsable** est affiché dans la case, **doublé d'une légende**, et l'app porte
un **thème en accord avec son domaine** (garde d'enfants). Ce palier de
lisibilité est **livré**.

L'**appropriation des acteurs** est, elle aussi, **livrée** : les acteurs du
foyer (leurs **noms** et leurs **couleurs**) sont **éditables** depuis un écran
de configuration, et la grille — case et légende — suit immédiatement le
changement. Renommer Alice → Alicia ou recolorier Bruno met aussitôt à jour la
case et la légende.

La **config foyer persistante** est désormais **acquise**. On peut **ajouter**
des acteurs (un parent, ou un « autre » comme la nounou) au-delà du seul
renommage/recoloriage du seed semé : l'ajout génère un **identifiant stable
neuf** (jamais le libellé) et la grille (case + légende, dédoublonnée par
identifiant) le reflète aussitôt. Et la config foyer **survit au redémarrage** :
la configuration du foyer (le référentiel des acteurs — leurs noms, leurs
couleurs, les acteurs ajoutés) est **persistée** derrière un adaptateur de droite
durable, ports inchangés. L'observable est tenu : « j'ajoute la nounou → elle
apparaît dans l'écran de config **et** dans la grille ; **après redémarrage du
serveur, elle est toujours là** ». La **volatilité de l'édition est éteinte ICI,
pour la config foyer uniquement** : le reste du domaine (slots, périodes,
transferts) demeure en mémoire, sa durabilité restant un pas technique séquencé
derrière l'usage.

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
> est refermée (cf. ci-dessous), et les paliers d'usage « saisie visible »,
> « lisibilité & thème », « édition des acteurs » et « config foyer persistante »
> sont **livrés**.

> **Exception bornée de fondation — refermée.** Au début du projet, une
> **fondation structurelle** a primé ponctuellement sur l'usage immédiat, parce
> que le coût de la poser (découpler le back en API, **détacher l'hôte d'API**,
> ouvrir l'app à d'autres clients) était minimal alors et **explose une fois
> l'app grosse**. C'était une **fenêtre d'investissement de début de projet**,
> pas une nouvelle règle générale. Cette fenêtre est **close** : le back démarre
> seul (hôte d'API détaché, front WASM autonome). L'arbitre d'usage **a repris la
> main** dès le palier « saisie visible » et ne la rend plus. Toute nouvelle
> fondation technique passe désormais **derrière l'usage**, jamais devant — sa
> séquence est subordonnée, jamais remplacée. La seule exception qui l'a précédée
> était **bornée à la config foyer** (cf. ci-dessous), et n'a pas fait cliquet.

> **Exception bornée — persistance de la config foyer, tirée devant l'usage
> (réalisée).** La **persistance durable de la SEULE config foyer** (le
> référentiel des acteurs : noms, couleurs, acteurs ajoutés) a été tirée
> **devant l'usage**, parce qu'elle porte un **observable d'usage direct** —
> l'ajout ou l'édition d'un acteur **survit au redémarrage** — et qu'elle est,
> par construction, le **premier client** de la persistance durable (le palier
> technique « persistance réelle » s'amorce sur son premier client). Elle est
> désormais **livrée**. Ce n'**était pas un renversement** de l'arbitre : c'est
> une **borne**, écrite noir sur blanc. Le corollaire qui suit en fixe le
> périmètre exact, et la **borne anti-cliquet** empêche le reste du domaine de
> remonter devant l'usage à sa suite.

> **Corollaire « durable ICI, volatile encore ailleurs »** *(reformule l'ancien
> « éditable maintenant ≠ durable »)*. Rendre une donnée **éditable** n'oblige pas
> à la rendre **durable** dans le même incrément — c'est la découpe qui a permis
> de livrer l'édition des acteurs **en mémoire** sans tirer la persistance en
> avant. Mais quand la durabilité porte un **observable d'usage direct** et reste
> **bornée**, elle se gagne : c'est le cas de la config foyer, dont la persistance
> est **livrée ICI** (la volatilité de l'édition des acteurs s'est **éteinte**
> pour la config foyer). **Partout ailleurs** — slots, périodes, transferts — la
> donnée reste **volatile**, sa durabilité **séquencée derrière l'usage**. Le
> « durable » se gagne là où il porte un observable et reste borné ; il reste
> séquencé partout où ce n'est pas le cas.

> **Borne anti-cliquet.** L'exception de persistance est **bornée à la config
> foyer** et ne doit pas faire **cliquet** : aucun autre dépôt (slots, périodes,
> transferts) n'est tiré devant l'usage au prétexte que la config foyer est passée
> durable. La persistance du **reste du domaine** demeure en **queue de séquence**
> (palier « persistance réelle »), derrière tout l'usage.

> **Corollaire de découpe.** Quand le périmètre d'un sujet déborde, on coupe au
> **plus petit incrément** qui rend la grille lisible et utilisable : on
> séquence, on ne reporte jamais tout en bloc. C'est ce corollaire qui a justifié
> de livrer d'abord un calendrier en lecture seule avant d'y brancher l'écriture,
> de borner la séparation de l'hôte d'API au plus petit pas qui rend le back
> démarrable seul, de tenir la lisibilité (nom + légende) comme observable avant
> le thème de surface, et de livrer l'**édition** des acteurs avant leur **ajout**
> et leur persistance. Il reste le **garde-fou de secours** des sujets pris en
> bloc.

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
« saisie visible », « lisibilité & thème », « édition des acteurs » et « config
foyer persistante » qui l'ont suivi sont **livrés**. L'arbitre d'usage tient
l'ordre : les paliers d'usage d'abord, les paliers **techniques en queue de
séquence**, derrière tout l'usage — à la seule **exception bornée** de la
persistance de la config foyer, qui a été tirée devant parce qu'elle portait un
observable d'usage direct, et qui est désormais livrée.

1. **Fondations — découplage du back en API, hôte détachable** *(exception
   bornée de fondation, valeur d'usage immédiate nulle, assumée — **REFERMÉ**)*.
   Les commandes d'écriture (poser un slot, affecter ou supprimer une période,
   ajuster un transfert) sont confiées à un **canal requête/réponse** côté
   serveur. L'**hôte d'API est détaché** : le back **démarre seul** (sans
   référencer le front) et le front, exécuté **dans le navigateur** (WebAssembly),
   le consomme comme une **API distante**. L'API est **explorable** (document
   OpenAPI + UI interactive) et autorise l'**origine du front** (CORS) ; une API
   injoignable produit un **échec clair** (message à l'écran, saisie non
   appliquée, **sans file ni rejeu**). *Socle d'un produit ouvert, posé au moment
   où le coût était minimal. Refermé ; l'usage reprend la main.*

2. **Saisie visible — la saisie réapparaît à la bonne date et en couleur du
   parent** *(reprise d'usage — **LIVRÉ**)*. Une saisie posée **réapparaît
   immédiatement** dans la grille, **à la bonne date** (les dates pré-remplies des
   formulaires valent « aujourd'hui », pas une date figée) **et en couleur du
   parent responsable** (la couleur se résout sur l'identifiant stable de
   l'acteur, pas sur son libellé d'affichage). *La date par défaut = aujourd'hui
   et la couleur du parent (identifiant stable bindé + seed) ont été levées ; le
   faux bug « les saisies n'apparaissent pas » est refermé.*

3. **Lisibilité & thème — qui garde se lit d'un coup d'œil** *(reprise produit —
   **LIVRÉ**)*. La responsabilité de période est **explicite** dans la grille : le
   **nom du responsable** est affiché dans la case **et** une **légende couleur**
   accompagne la grille ; un nom trop long est **tronqué** tout en restant lisible
   en entier au **survol** ; un acteur hors du set connu reste **affiché et
   distingué** (gris assumé) sans perdre son nom. L'app porte un **thème en accord
   avec le domaine** (garde d'enfants). *Pris en bloc ; la lisibilité a porté
   l'observable, le thème est resté ergonomie de surface subordonnée. Livré et
   accepté en usage réel (front WASM + API distante).*

4. **Config foyer — édition des acteurs (en mémoire)** *(reprise d'usage —
   **LIVRÉ**)*. Un écran pour **éditer les acteurs du foyer** : leurs **noms** et
   leurs **couleurs**. Le seed jusqu'ici figé est devenu **éditable** et la
   **grille (case + légende) reflète immédiatement** le changement *dans la
   session* : renommer Alice → Alicia ou recolorier Bruno met aussitôt à jour la
   case et la légende. L'édition vivait alors **en mémoire** ; sa **durabilité**
   (survie au redémarrage) a été portée par le palier suivant. *Plus petite
   tranche d'appropriation des acteurs : le « éditable » s'est gagné sans tirer la
   durabilité en avant. Dette volatile assumée, éteinte au palier 5 pour la config
   foyer.*

5. **Config foyer persistante — ajout d'acteurs & survie au redémarrage**
   *(reprise d'usage + exception bornée de persistance — **LIVRÉ**)*. Deux choses,
   fondues en un incrément : **(a)** **ajouter** un acteur au foyer (parent /
   autre / nounou), au-delà du renommage/recoloriage du seed — l'ajout génère un
   **identifiant stable neuf** (jamais le libellé) et la grille (case + légende,
   dédoublonnée par id) le reflète aussitôt ; **(b)** **persister** la config
   foyer (référentiel des acteurs : noms, couleurs, acteurs ajoutés) via un
   **adaptateur de droite durable**, derrière les ports existants
   (`IReferentielResponsables` / `IPaletteCouleurs` / `IEditeurConfigurationFoyer`
   inchangés). L'observable est tenu : « j'ajoute la nounou → elle apparaît dans
   l'écran de config **et** dans la grille ; **après redémarrage, elle est toujours
   là** ». La **volatilité du palier 4 est éteinte ICI**, pour la config foyer
   **uniquement** ; le reste du domaine (slots / périodes / transferts) **reste en
   mémoire**. *Exception bornée : la persistance a été tirée devant l'usage parce
   qu'elle porte un observable direct et qu'elle est le premier client du store
   durable. Livré : ajout **sans suppression** d'abord, **sans** édition du cycle
   de fond ici ; la durabilité est prouvée sur store réel (survie au redémarrage),
   pas par doublure.*

6. **Récurrence des périodes — définir le cycle de fond** *(reprise d'usage,
   IMPORTANT)*. Définir et éditer une **récurrence** sur les périodes de garde (le
   cycle : qui garde par défaut, semaine paire / impaire…), au-delà du modèle de
   cycle déjà présent. *Vient derrière l'appropriation des acteurs : on récurre
   des responsabilités une fois les acteurs maîtrisés et durables.*

7. **Survol → résumé de la journée** *(reprise d'usage, évolution)*. Au survol
   prolongé (~1 s) d'une case, afficher un **résumé de la journée**, au-delà du
   seul nom complet déjà rendu au survol simple. *Enrichissement, pas une
   réparation : le survol simple (nom complet) est livré et conforme. Le périmètre
   du résumé (périodes / slots / responsable / transferts) est à cadrer au
   make-gherkin et ne doit pas être sous-estimé.*

8. **Calendrier navigable & écriture en contexte** *(reprise produit)* — naviguer
   dans le **passé/futur**, offrir des **vues prédéfinies** (semaine, mois, 4
   semaines glissantes), et poser un slot / affecter ou supprimer une période /
   ajuster un transfert via des **dialogs ouvertes depuis une case**. Inclut la
   **sélection d'une plage de cases** du planning pour définir une période (ex.
   sélectionner 29/05 → 05/07 et affecter un responsable). *L'utilisateur agit là
   où il lit ; l'écriture passe par le canal requête/réponse et la grille
   s'actualise par la diffusion temps réel. Séquencé **derrière** l'appropriation
   des acteurs et la récurrence, par priorité d'usage actée.*

9. **Alimentation & saisie — config foyer durable (reste de la config)** —
   **persister** la part restante de la configuration du foyer (lieux, set de
   couleurs par défaut, cycle de fond) plutôt que de la porter en données figées
   dans le code. *Le **volet acteurs** de la config durable a déjà été livré au
   palier 5 (premier client du store réel) ; ce palier complète la config durable
   pour les lieux, couleurs et cycle de fond.*

10. **Modèle d'acteurs & foyer** — déclarer les acteurs réels (Admin, Parents,
    Autres) dans un écran de configuration, qui porte aussi la **responsabilité
    récurrente de fond** (le cycle) et le **set de couleurs par défaut**.
    *Exploite la config persistée des paliers précédents ; prérequis de
    l'ouverture de l'accès.*

11. **Immédiat & événements à venir** — « qui récupère ce soir », où est l'enfant
    maintenant, transferts et changements à venir présentés comme événements dans
    un **panneau cloche**, plus comme un tableau permanent. *Valeur quotidienne ;
    expose enfin les transferts, aujourd'hui invisibles par construction.*

12. **Imprévu & échange** — enfant malade / retard / échange de dernière minute,
    transferts **dérivés automatiquement** par défaut et saisie réservée à
    l'exception. *Le plus délicat ; après que l'usage à deux est acquis. Porte la
    question ouverte du **workflow demande/accord** avant réaffectation d'une
    période à l'autre parent.*

13. **Ouverture de l'accès** — landing page et authentification des acteurs réels
    (email via Gmail / Apple / Microsoft) pour lever le risque mortel d'adoption.
    Débloque aussi la **personnalisation des couleurs par utilisateur** et la
    **persistance d'une préférence de thème** (thème sombre). *Vient après le
    socle et le modèle d'acteurs ; à ne pas laisser glisser indéfiniment.*

14. **Persistance réelle — adaptateurs de droite (reste du domaine)** *(palier
    technique, derrière l'usage)* — remplacer les dépôts **en mémoire** (volatils)
    des **slots, périodes et transferts** par des **adaptateurs de droite** vers
    un store **durable**, derrière les ports existants, sans toucher au domaine.
    *Débloqué par la fondation (palier 1) mais **subordonné à l'usage** : ne passe
    pas devant un incrément produit observable. La **config foyer** en a été le
    **premier client**, déjà rendu durable au palier 5 (exception bornée) ; ce
    palier étend la durabilité au reste du domaine. **Borne anti-cliquet** : il ne
    remonte pas devant l'usage.*

15. **Saisie hors-ligne (PWA)** *(palier technique, derrière l'usage)* — au-delà
    de l'**échec clair** déjà livré (palier 1), mettre en cache et **mettre en
    file** les écritures faites hors connexion, **rejouées au retour du réseau**.
    *Subordonné à l'usage. Piste consignée : une **file d'écritures côté client**
    (type *outbox*, IndexedDB) garantissant un rejeu « exactement une fois » comme
    socle minimal ; l'*event sourcing* n'est retenu que si offline / rejeu / audit
    le justifient — à trancher au moment d'ouvrir ce palier, pas un prérequis.*

> **Re-séquencement acté.** La **config foyer persistante** (ajout d'acteurs +
> persistance bornée) a été insérée **devant la récurrence des périodes**, par
> priorité d'usage du PO, et est désormais **livrée** : on tient d'abord des
> acteurs ajoutables et durables, puis on récurre leurs responsabilités. Le
> calendrier navigable & l'écriture en contexte sont placés **derrière** la
> récurrence et le survol enrichi. La persistance du **reste du domaine** reste
> **en queue** (palier 14), derrière tout l'usage — seule la persistance de la
> config foyer a été tirée devant (borne).

> **Transverse (hors incrément dédié)** : l'ergonomie de surface est absorbée au
> fil des incréments calendrier et reste subordonnée à l'usage par l'arbitre. Le
> **thème sombre + bascule clair/sombre** (avec persistance de la préférence) est
> une évolution additive au thème métier livré : consignée, non priorisée, à
> rattacher au futur écran de préférences utilisateur (cf. Risques). De même, un
> **sélecteur de couleur (palette / picker)** dans l'écran de config et
> l'**harmonisation de teinte légende ↔ case** (non-bug, cf. Risques) restent des
> évolutions de surface non priorisées seules.

> **Garde-fous hors-spec** : la convention de code interne (chaque vue adossée à
> son code-behind), l'outillage d'**API explorable et documentée** (document
> OpenAPI **et** UI interactive type Swagger-UI / Scalar) et l'**empaquetage en
> conteneurs** (hôte API + front WASM + store, montables ensemble façon compose)
> sont des **garde-fous de structure et d'outillage sans observable métier** : ils
> n'ouvrent ni règle de gestion ni incrément produit, et restent subordonnés à
> l'usage par l'arbitre. Dans ce même registre, une **restructuration du code
> applicatif** (dette de structure menée **hors du processus BDD/TDD piloté**) est
> conduite **à iso-comportement strict** — aucun observable métier modifié,
> aucun palier d'usage ouvert : code-behind systématique (éliminer le `@code`
> inline restant), frontières hexagonales gauche/droite homogènes, séparation des
> projets (Domaine / Application / Infrastructure / Api / Web) clarifiée. Son
> **critère de sortie est non négociable** : la **suite COMPLÈTE reste verte —
> 161/161 — avant ET après**, via `dotnet test` **sans `--no-build` ni filtre**,
> **Docker actif** (pivot Mongo de la config foyer inclus).

> **Prochain sujet** : palier 6, **récurrence des périodes — définir et éditer le
> cycle de fond** (qui garde par défaut, semaine paire / impaire…), au-delà du
> modèle de cycle déjà présent. Il redevient le prochain sujet `/2-make-gherkin`
> une fois la **restructuration du code applicatif** refermée : cette refacto est
> menée **hors-pipeline** (elle ne réamorce pas `/2-make-gherkin`) et **précède**
> la récurrence par simple séquencement, sans être un incrément produit.

## Mécaniques de base

- Un créneau de garde a un horaire (début → fin), un responsable unique, un lieu et une activité
- Une journée est une suite de créneaux enchaînés
- Le planning suit un cycle de plusieurs semaines qui se répète automatiquement
- Le hub `/planning` est un calendrier navigable façon agenda ; on s'y déplace dans le **passé et le futur**, avec des **vues prédéfinies** (semaine, mois, 4 semaines glissantes). La **fenêtre par défaut** est **4 semaines glissantes à partir de la semaine en cours** ; les slots y sont positionnés dans les cases jour/horaire. On peut **sélectionner une plage de cases** pour définir une période (affecter un responsable sur l'intervalle)
- La responsabilité de chaque garde se lit par un **code couleur par personne**, **doublé du nom du responsable affiché dans la case et d'une légende** : la couleur seule ne suffit pas à identifier qui garde. Un nom trop long est **tronqué** dans la case, son **intitulé complet restant lisible au survol** ; un acteur hors du set de couleurs connu reste **affiché et distingué** (teinte neutre assumée) sans perdre son nom. La couleur d'un responsable se résout sur un **identifiant d'acteur stable**, jamais sur son libellé d'affichage : les sélecteurs de saisie fournissent ce même identifiant que la palette, sinon la case retombe sur la couleur neutre
- L'app porte un **thème en accord avec son domaine** (garde d'enfants), au service de la lisibilité d'usage ; c'est une ergonomie de surface, subordonnée à l'usage
- Les formulaires de saisie pré-remplissent leurs dates sur **« aujourd'hui »** (la date de référence), jamais sur une date figée : une saisie tombe ainsi dans la fenêtre affichée et réapparaît immédiatement dans la grille
- La grille est en **lecture seule** : elle consomme les slots et périodes déjà enregistrés, sans écriture. Toute écriture (poser un slot, affecter, surcharger ou supprimer une période, ajuster un transfert) se fait en contexte via des **dialogs** ouvertes depuis une case, alimentées par les acteurs et lieux du foyer ; la **sélection d'une plage de cases** ouvre l'affectation d'une période sur l'intervalle choisi
- Les **acteurs du foyer (noms et couleurs) sont éditables** depuis un écran de configuration : renommer ou recolorier un acteur **met immédiatement à jour la grille** (case **et** légende) qui relit la configuration. On peut aussi **ajouter** un acteur (parent / autre / nounou) : l'ajout génère un **identifiant d'acteur stable neuf** (jamais le libellé) et la grille le reflète aussitôt (case + légende dédoublonnée par identifiant). La configuration du foyer ainsi éditée **survit au redémarrage** : elle est **persistée** derrière les **ports de droite** par leur adaptateur durable. Cette durabilité est **bornée à la config foyer** (référentiel des acteurs : noms, couleurs, acteurs ajoutés) ; le reste du domaine (slots, périodes, transferts) reste, lui, en mémoire le temps de son propre palier de persistance
- **Le front est découplé du back par une API** : il ne manipule pas le domaine en direct, il **émet ses commandes et lit ses données à travers l'API**, ce qui ouvre l'app à d'autres clients (front navigateur, IHM tierce, agents). L'**hôte d'API est détaché** : le back démarre seul, sans référencer le front, et le front — exécuté **dans le navigateur** (WebAssembly) — consomme une **API distante**
- **Toute écriture passe par le canal requête/réponse** : aucune vue n'écrit le domaine en direct. Une saisie qui contournerait le canal (appel direct du back) est une dette à résorber, pas un mode de fonctionnement
- **Deux canaux distincts, jamais confondus** : l'**écriture** est confiée à un **canal requête/réponse** (la commande part, la réponse confirme l'effet) ; la **diffusion temps réel** vers les autres acteurs (notifications, actualisation du planning à l'écran) passe par un **canal de diffusion en lecture seule**. On **n'écrit jamais** par le canal de diffusion : une saisie se propage aux autres écrans parce que l'écriture, une fois aboutie, **déclenche** la diffusion
- **Échec clair si l'API distante est injoignable** : la commande non aboutie produit un message à l'écran et la saisie **n'est pas appliquée** ni perdue de vue (elle reste à resoumettre). Aucune mise en file ni rejeu à ce stade — le hors-ligne rejouable est un palier technique ultérieur
- L'**API est explorable** : elle expose un **document de description** (OpenAPI) et une **UI interactive** pour essayer les endpoints (type Swagger-UI / Scalar), et autorise l'**origine du front** par sa configuration CORS
- La responsabilité récurrente de fond (qui garde selon le cycle) se déclare dans la configuration du foyer ; le calendrier ne porte que les **surcharges ponctuelles** d'une période
- La configuration du foyer est une **donnée éditable** : pour le **référentiel des acteurs** (noms, couleurs, acteurs ajoutés) elle est **éditable ET durable** — elle vit derrière les **ports de droite**, dont l'adaptateur durable la persiste et la fait survivre au redémarrage. Pour le **reste** (lieux, set de couleurs par défaut, cycle de fond) elle est **éditable en mémoire** dès maintenant, et **durable à terme** une fois son adaptateur posé. Dans tous les cas, c'est une donnée derrière les ports, jamais une constante figée dans le code
- Les transferts et changements à venir sont présentés comme des événements dans un panneau de notifications (cloche), pas comme un tableau permanent
- Trois types d'acteurs : Admin, Parent, Autre — chacun avec un affichage adapté à son type
- La composition du foyer (enfants, parents, acteurs autres) se déclare dans un écran de configuration distinct du planning

## Règles de gestion

### Foyer & acteurs

1. **Multi-enfants** — Un foyer compte au moins un enfant, et peut en compter plusieurs, chacun avec sa propre organisation de garde

2. **Familles recomposées** — Un foyer peut mélanger des enfants de parents différents, tous visibles dans le même planning

3. **Toujours deux parents** — Un foyer a toujours exactement deux parents ; tant qu'un seul est inscrit, il peut saisir les informations de l'autre

4. **Acteurs « autres » ajoutables et éditables** — Un foyer peut avoir N acteurs « autres » (nounou, grands-parents…). Ils sont **ajoutables et éditables** (par les parents ou par l'acteur lui-même), au-delà du seul renommage/recoloriage du seed initial : ajouter un acteur le fait exister dans le foyer et dans la grille (cf. règle 6)

5. **Édition des acteurs (noms + couleurs)** — Les acteurs du foyer (leurs **noms** et leurs **couleurs**) sont **éditables** depuis un écran de configuration, et la **grille (case + légende) relit immédiatement** la configuration éditée : renommer ou recolorier un acteur se voit aussitôt. Cette édition s'est livrée d'abord **en mémoire, dans la session** ; sa **durabilité** (survie au redémarrage) est portée par la règle 6 et la règle 30. La survie au redémarrage **est acquise pour la config foyer** : la dette volatile de l'édition s'éteint là, ailleurs elle subsiste (cf. règle 30)

6. **Ajout d'acteur & persistance bornée de la config foyer** — On peut **ajouter** un acteur au foyer (parent / autre / nounou) : l'ajout génère un **identifiant d'acteur stable neuf** (jamais dérivé du libellé d'affichage) et la grille (case + **légende dédoublonnée par identifiant**) le reflète immédiatement. La **configuration du foyer** — référentiel des acteurs : **noms, couleurs, acteurs ajoutés** — est **persistée** derrière les ports de droite par un **adaptateur durable** : elle **survit au redémarrage**. Cette persistance est **bornée à la config foyer** : c'est le **premier client** du store durable, tiré **devant l'usage** parce qu'il porte un observable direct (l'acteur ajouté/édité réapparaît après redémarrage). L'ajout s'est livré **sans suppression** d'abord et **sans** édition du cycle de fond ; les **cases orphelines** (slot d'un acteur retiré) restent un point de découpe à cadrer le moment venu. La persistance du **reste du domaine** reste **en queue** (règle 30, borne anti-cliquet)

7. **Responsabilité de fond en config, exception au calendrier** — La responsabilité récurrente de garde (le cycle : qui garde par défaut) se déclare dans l'écran de configuration du foyer, en même temps que les acteurs ; elle est **éditable** (en mémoire d'abord, durable une fois son adaptateur posé). Le calendrier ne sert qu'aux **surcharges ponctuelles** d'une période donnée. Les dialogs d'affectation et de surcharge sont alimentées par les acteurs du foyer

### Rôles & accès

8. **Trois types d'acteurs** — Trois types d'accès : Admin (gère tout, y compris la configuration du foyer), Parent (gère les créneaux, lieux, acteurs et invitations de son foyer), Autre (consultation et édition limitée à ses propres informations). L'affichage s'adapte au type

9. **Modification réservée aux parents et à l'admin** — Seul un Parent (ou l'Admin) peut créer, éditer ou supprimer un créneau ou une période ; un acteur « Autre » n'édite que ses propres informations

### Planning & créneaux

10. **Responsable unique** — Un créneau a toujours un seul responsable (pas de « X ou Y »)

11. **Cycle récurrent, éditable** — Le planning se répète selon un cycle de plusieurs semaines (ex : semaine paire / impaire). Ce cycle (la **récurrence des périodes**) est **définissable et éditable** depuis la configuration du foyer, et non figé dans le code

12. **Exception ponctuelle** — Un jour précis peut être surchargé sans casser le cycle de fond ; le cycle reprend ensuite

13. **Lieux éditables** — La liste des lieux (domicile A/B, école, nounou, activité…) est librement modifiable et sert de référentiel aux sélecteurs de saisie

14. **Grille en lecture seule** — La grille agenda consomme les slots et périodes déjà enregistrés et les rend (positionnés dans leur case, colorés par responsable, **nommés**) sans jamais écrire ; toute écriture passe par une dialog ouverte depuis une case, ou par la **sélection d'une plage de cases** pour affecter une période

15. **Suppression de période** — Un Parent (ou l'Admin) peut supprimer une période de garde ; c'est une action d'écriture menée depuis une dialog contextuelle, hors de la grille en lecture pure

16. **Pose répétée d'un même slot acceptée avec avertissement** — Un slot qui chevauche ou redouble un slot existant est **accepté**, accompagné d'un **avertissement** ; il n'est ni refusé ni dédoublonné. (Une demande d'interdiction/dédoublonnage est en attente comme révision de règle — cf. Risques & questions ouvertes.)

17. **Date de saisie par défaut = aujourd'hui** — Les formulaires de saisie (poser un slot, affecter ou surcharger une période) pré-remplissent leurs dates sur la **date de référence « aujourd'hui »**, jamais sur une date figée : la saisie tombe ainsi dans la fenêtre affichée et **réapparaît immédiatement** dans la grille. Une date par défaut figée hors fenêtre est une non-conformité à corriger, pas un comportement attendu

### Code couleur & lisibilité

18. **Responsabilité lisible : couleur par personne + nom + légende** — La responsabilité de garde se lit dans la grille par une couleur qui distingue chaque **personne** (Parent A ≠ Parent B), pas seulement son type de rôle ; cette couleur est **doublée du nom du responsable affiché dans la case et d'une légende**, car la teinte seule ne suffit pas à dire qui garde. Un **nom trop long** est **tronqué** dans la case tout en restant lisible **en entier au survol** ; un acteur **hors du set de couleurs connu** reste **affiché et distingué** (teinte neutre assumée) **sans perdre son nom**

19. **Couleur résolue sur un identifiant d'acteur stable** — La couleur d'un responsable se résout sur l'**identifiant stable** de l'acteur, jamais sur son libellé d'affichage. Les sélecteurs de saisie (affectation, surcharge) **fournissent ce même identifiant** que la palette ; un acteur **ajouté** reçoit un identifiant stable neuf résolu de la même façon. Un libellé qui ne correspond pas à un identifiant connu fait retomber la case sur la **couleur neutre**, ce qu'on ne veut pas : une case grise là où un responsable est affecté trahit un libellé fourni à la place de l'identifiant — c'est le défaut à localiser, pas la résolution elle-même

20. **Set de couleurs par défaut, recoloriable** — Un jeu de couleurs par défaut, déclaré avec le foyer, différencie d'emblée les responsables tant qu'aucune personnalisation n'a été faite ; c'est ce set qui s'applique avant l'ouverture de l'accès. Ce set est **recoloriable** depuis l'écran de config et la grille suit ; la **personnalisation par utilisateur authentifié** (règle 21) reste un pas distinct lié à l'ouverture de l'accès

21. **Personnalisation des couleurs par utilisateur** — Une fois identifié, chaque acteur peut personnaliser les couleurs qu'il assigne aux acteurs qu'il voit ; cette personnalisation dépend de l'authentification (séquence, ouverture de l'accès) et n'altère pas le set par défaut vu par les autres

22. **Thème en accord avec le domaine** — L'app porte un **thème** cohérent avec son domaine (garde d'enfants), au service de la lisibilité d'usage. C'est une **ergonomie de surface subordonnée à l'usage** par l'arbitre : il n'ouvre aucune règle métier. Un **thème sombre avec bascule clair/sombre** (et persistance de la préférence) en est une **évolution additive**, consignée et non priorisée, à rattacher au futur écran de préférences utilisateur. L'**harmonisation de teinte** entre la pastille de légende et le fond de case (cf. Risques) relève du même registre d'ergonomie de surface, pas d'une règle métier

### Survol & détail de la case

23. **Survol : du nom complet au résumé de la journée** — Au survol d'une case, l'app expose un complément d'information sans quitter la grille. Le **survol simple** affiche le **nom complet** du responsable (utile quand le nom est tronqué) : c'est le comportement **livré et conforme**. Un **survol enrichi** est une **évolution** prévue : après un survol prolongé (~1 s), afficher un **résumé de la journée**. Le périmètre de ce résumé (périodes, slots, responsable, transferts) est à cadrer au moment de le scénariser ; ce n'est pas un correctif du survol simple, qui n'est pas défaillant

### Transferts

24. **Transfert dérivé par défaut** — Le transfert d'un enfant (qui dépose, qui récupère, à quelle heure) est dérivé automatiquement du planning par défaut : c'est le cas nominal, sans saisie

25. **Transfert modifiable et ponctuel** — Un transfert auto-calculé peut être modifié, et des transferts ponctuels peuvent être proposés ou ajoutés à l'exception (urgence, changement d'emploi du temps)

### Modifications & notifications

26. **Modification directe** — Un Parent applique son changement immédiatement ; les autres acteurs concernés sont notifiés (pas de workflow de validation). (Une demande de workflow demande/accord avant réaffectation d'une période à l'autre parent est en attente comme révision de règle, rattachée au palier « imprévu & échange » — cf. Risques & questions ouvertes.)

27. **Notifications & événements à venir** — Les notifications in-app couvrent les changements de planning et les rappels de transfert ; les transferts et changements à venir sont consultables dans un panneau d'événements accessible via une cloche

### Accès aux données & exploitation

28. **Écriture par le canal, échec clair si l'API est injoignable** — Toute écriture passe par le **canal requête/réponse** vers l'API distante ; aucune vue n'écrit le domaine en direct. Si l'API est **injoignable**, la commande échoue **clairement** (message à l'écran, saisie **non appliquée** et conservée à resoumettre), **sans mise en file ni rejeu** à ce stade. Le hors-ligne rejouable (cache + file d'écritures) est un palier technique ultérieur, derrière l'usage

29. **API explorable et origine du front autorisée** — L'API expose un **document de description** (OpenAPI) **et** une **UI interactive** d'exploration des endpoints (type Swagger-UI / Scalar), et autorise l'**origine du front** (CORS). C'est un garde-fou d'outillage, sans observable métier : aucune règle de gestion n'en dépend

30. **Données derrière les ports, durables — exception bornée pour la config foyer** — Les slots, périodes, transferts et la configuration du foyer vivent derrière des **ports** ; la donnée du foyer n'est jamais une constante figée dans le code. Leur stockage est durable **à terme**, sans toucher au domaine ni aux règles. **Exception bornée actée et livrée** : la **config foyer** (référentiel des acteurs — noms, couleurs, acteurs ajoutés) est **persistée maintenant**, **devant l'usage**, parce qu'elle porte un observable direct (survie au redémarrage) et qu'elle est le **premier client** du store durable. Le **reste du domaine** (slots, périodes, transferts) reste **en mémoire**, sa durabilité **séquencée derrière l'usage** (palier « persistance réelle ») : la **borne anti-cliquet** empêche que cette exception entraîne le reste devant l'usage. Cette règle porte la **durabilité** ; elle reste **distincte de l'édition** (règle 5) — rendre une donnée éditable n'oblige pas à la rendre durable dans le même incrément, sauf quand, comme ici, la durabilité porte un observable direct et reste bornée

## Risques & questions ouvertes

- **L'usage tient la main — ne pas enchaîner un sprint sans valeur produit.** Deux sprints structurels (fondation, hôte d'API) puis quatre sprints d'usage (saisie visible, lisibilité & thème, édition des acteurs, config foyer persistante) sont derrière nous. Les prochains sujets (récurrence, survol enrichi, calendrier navigable) restent des paliers **d'usage** ; les paliers techniques débloqués (persistance du reste du domaine, PWA, Docker) sont tentants mais **doivent rester derrière l'usage**. La seule persistance tirée devant a été **bornée à la config foyer** (observable direct) : ne pas en faire un cliquet.
- **Refacto hors gate TDD piloté → régression invisible.** Le prochain chantier — la **restructuration du code applicatif** (dette de structure : code-behind, frontières hexagonales, séparation des projets) — est mené **hors du processus BDD/TDD piloté** : pas de scénarios, pas de passage make-gherkin, donc pas de gate TDD qui borde chaque pas. Le risque est qu'une régression passe **invisible**. Mitigation = **condition de fin non négociable** : la **suite COMPLÈTE reste verte — 161/161 — avant ET après**, via `dotnet test` **sans `--no-build` ni filtre**, **Docker actif** (pivot Mongo de la config foyer inclus). Toute restructuration qui ne prouve pas ce vert complet est refusée. **Borne anti-débordement** : refacto **à iso-comportement strict** (aucun observable métier touché), et la persistance du **reste du domaine n'est PAS tirée en avant** (slots / périodes / transferts restent en mémoire — cf. règle 30 / borne anti-cliquet). Le chantier précède la récurrence par simple séquencement, sans ouvrir de palier d'usage.
- **Débordement ~2h IA — découpe de secours des sujets pris en bloc.** La leçon de la config foyer persistante (ajout d'acteurs **et** adaptateur durable réel) tient pour les sujets composites à venir : si un sujet combine deux choses qui débordent ensemble, **couper, ne pas reporter en bloc** — livrer d'abord la tranche qui porte l'observable, puis l'autre.
- **Borne anti-cliquet à tracer sans déraper.** La persistance devant l'usage est une **exception bornée à la config foyer**, désormais livrée. Risque de **cliquet** : que le reste du domaine (slots / périodes / transferts) suive devant l'usage. Garder cette persistance **en queue** (palier 14) ; la borne est écrite noir sur blanc (règle 30, et `docs/BACKLOG.md`).
- **Acceptation runtime obligatoire (rempart anti vert-qui-ment).** La persistance de la config foyer a été prouvée sur un **store durable réel** (redémarrage → l'acteur ajouté/édité réapparaît), pas par un test à doublures, et l'ajout d'acteur sur une **grille (case + légende) réellement câblée** (front WASM + API distante). Ce rempart reste la règle pour les prochains incréments : un test de composant avec doublures peut afficher une grille alors que le câblage réel échoue ; l'acceptation vérifie que des données **réellement enregistrées** apparaissent positionnées, colorées et **nommées**, pas une grille vide statique.
- **Légende ≠ bug (non-bug, harmonisation de teinte).** Le ressenti « les couleurs de la légende ne sont pas celles des acteurs » a été **confronté au code courant** : la légende et la case-jour résolvent le **même token couleur sur le même singleton** (config foyer réalisant lecture nom + couleur). **Aucun défaut de résolution.** L'écart vient d'une **incohérence de teinte de présentation** : pastille de légende **saturée** vs fond de case **pâle** (choix de design : fond pâle = texte sombre lisible). C'est une **évolution de teinte**, **jamais** un `/3` ciblé. À regrouper avec l'ergonomie config (palette/picker de couleur) quand elle remontera, pas un sujet seul.
- **Évolutions de surface non priorisées seules** — **sélecteur de couleur (palette / picker)** dans l'écran de config (au lieu d'une saisie libre) ; **onglets** de config par acteur (faible conviction PO : « un seul foyer → tous les acteurs sur le même écran ») ; **harmonisation de teinte** légende ↔ case (ci-dessus). Reconnues, séquencées derrière l'usage, sans règle ni palier dédié tant que l'usage ne les appelle pas.
- **Édition vs persistance — deux périmètres à ne pas confondre.** La config foyer est devenue **durable** (référentiel des acteurs), mais le **reste du domaine reste volatile** : slots, périodes, transferts vivent encore en mémoire jusqu'à leur palier de persistance. Ne pas reprocher au reste de ne pas persister (c'est la découpe) ; ne pas tirer leur persistance en avant au prétexte que la config foyer est durable (c'est le cliquet).
- **Périmètre « résumé de la journée » (survol enrichi) non défini** — périodes ? slots ? responsable ? transferts ? Sujet potentiellement plus gros qu'il n'y paraît, proche du « qui récupère ce soir » (palier immédiat). À **cadrer au make-gherkin** quand le survol (palier 7) sera pris ; ne pas le sous-estimer comme « simple tooltip ». Le survol simple (nom complet) est **conforme et accepté** : rien de cassé, le résumé est un comportement **neuf**, jamais une réparation à envoyer en `/3` ciblé.
- **Couplage thème sombre + toggle / préférences utilisateur** — La **persistance d'une préférence de thème** rejoint naturellement le futur écran de config / préférences user (palier 13, ouverture de l'accès). À arbitrer quand le thème sombre remontera : l'embarquer avec la gestion utilisateurs ou le garder isolé. Évolution additive au thème métier livré, non priorisée.
- **Idées consignées non prioritaires** — Indicateur de **présence de l'autre parent** (temps réel) ; **slot imbriqué** (un slot peut en contenir un autre) ; **parents liés via leurs enfants** (graphe foyer) ; **familles recomposées** (déjà règle 2) : besoins reconnus, séquencés derrière l'usage prioritaire, sans règle ni palier dédié tant que l'usage ne les appelle pas.
- **Question ouverte — workflow demande/accord (révision de règle 26)** — Le PO veut qu'une période ne puisse être réaffectée à l'autre parent qu'après une **demande explicite acceptée**. C'est une révision de la règle « modification directe », pas un correctif ; elle attend le palier « imprévu & échange » et ne génère aucune règle ni sujet tant qu'il n'est pas ouvert.
- **Question ouverte — interdiction/dédoublonnage de slot (révision de règle 16)** — Le PO veut **refuser ou dédoublonner** la pose répétée d'un même slot. C'est une révision du choix v1 « accepté avec avertissement », hors de la boucle courante.
- **Adoption de l'autre parent (risque mortel)** — L'app n'a de valeur que si l'autre parent l'utilise aussi ; sinon le planning est faux. L'ouverture de l'accès (auth réelle, palier 13) la traite, mais elle vient tard : **à ne pas laisser glisser indéfiniment** derrière la technique. Aucun des paliers techniques en queue ni des prochains incréments d'usage ne lève ce risque.
- **Contraintes du découplage front/API distant** — Émettre les commandes à travers une API **distante** introduit des contraintes (échanges inter-domaines, sérialisation des commandes, configuration de l'URL d'API, future authentification) absentes quand le front parlait au back en direct ; elles s'accentuent avec l'hôte détaché et le front WASM.
- **Diffusion déclenchée par l'écriture** — Garantir qu'une écriture aboutie déclenche bien l'actualisation temps réel des autres écrans, sans jamais écrire par le canal de diffusion ; à valider en usage réel, le hub SignalR étant consommé côté navigateur.
- **Hors-ligne rejouable — piste à trancher au palier PWA** — Au-delà de l'échec clair livré, une **file d'écritures côté client** (type *outbox*, IndexedDB) garantissant un rejeu « exactement une fois » est la piste minimale ; l'*event sourcing* n'est retenu que si offline / rejeu / audit le justifient. Décision **au moment d'ouvrir le palier**, pas un prérequis.
- **Coût de saisie du cycle** — Saisir un cycle multi-semaines est lourd ; le palier « récurrence des périodes » devra le rendre supportable. À valider seulement si le cycle est stable dans le temps. Le transfert dérivé automatiquement réduit ce coût pour le cas nominal.
- **Différenciation** — Vs Cozi / FamilyWall / agenda partagé : le manque précis qu'on couvre mieux reste à nommer.
