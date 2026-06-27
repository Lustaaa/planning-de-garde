# Planning de garde — Organisation des semaines de garde

> Version 11 · consolide la v10 + docs/sprints/10-recurrence-des-periodes/99-sprint10-besoins-fin-itération.md. Remplace la v10, qui reste figée en historique.

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

La **récurrence des périodes** est désormais **livrée** : un **cycle de fond**
déclaré dans la configuration du foyer détermine **qui garde par défaut**,
semaine après semaine, sans qu'aucune période n'ait à être saisie. Le cycle
compte **N semaines** (N ≥ 1) et alterne par **parité de la semaine ISO**
(`index = semaine ISO du jour, modulo N`) ; chaque index est mappé sur un
**responsable de fond** résolu sur son **identifiant stable**. La grille affiche
ce fond — case **nommée et colorée** + **légende** — exactement comme une saisie
explicite, et il **suit immédiatement** toute ré-édition du cycle (sans
rechargement, par diffusion temps réel). La responsabilité d'une case se résout
par **priorité** : une **surcharge** (période explicitement saisie) **prime** sur
le **fond** (cycle), qui prime sur le **neutre** ; un index de cycle sans
responsable retombe sur la **teinte neutre** sans nom fantôme. Le cycle vit pour
l'instant **en mémoire** : comme les slots, périodes et transferts, sa
durabilité est un pas séquencé derrière l'usage (cf. config foyer durable).

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
> « lisibilité & thème », « édition des acteurs », « config foyer persistante »
> et « récurrence des périodes » sont **livrés**.

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
> avant, et de livrer le **cycle de fond en mémoire** sans tirer sa durabilité.
> Mais quand la durabilité porte un **observable d'usage direct** et reste
> **bornée**, elle se gagne : c'est le cas de la config foyer, dont la persistance
> est **livrée ICI** (la volatilité de l'édition des acteurs s'est **éteinte**
> pour la config foyer). **Partout ailleurs** — slots, périodes, transferts,
> **cycle de fond** — la donnée reste **volatile**, sa durabilité **séquencée
> derrière l'usage**. Le « durable » se gagne là où il porte un observable et
> reste borné ; il reste séquencé partout où ce n'est pas le cas.

> **Borne anti-cliquet.** L'exception de persistance est **bornée à la config
> foyer** et ne doit pas faire **cliquet** : aucun autre dépôt (slots, périodes,
> transferts, **cycle de fond**) n'est tiré devant l'usage au prétexte que la
> config foyer est passée durable. La persistance du **reste du domaine** demeure
> en **queue de séquence** (palier « config foyer durable — reste » puis
> « persistance réelle »), derrière tout l'usage.

> **Corollaire de découpe.** Quand le périmètre d'un sujet déborde, on coupe au
> **plus petit incrément** qui rend la grille lisible et utilisable : on
> séquence, on ne reporte jamais tout en bloc. C'est ce corollaire qui a justifié
> de livrer d'abord un calendrier en lecture seule avant d'y brancher l'écriture,
> de borner la séparation de l'hôte d'API au plus petit pas qui rend le back
> démarrable seul, de tenir la lisibilité (nom + légende) comme observable avant
> le thème de surface, de livrer l'**édition** des acteurs avant leur **ajout** et
> leur persistance, et de livrer un **cycle de fond** (parité ISO en mémoire)
> avant d'en enrichir l'ergonomie (ancre, frontière de jour, plages, sur-cycles).
> Il reste le **garde-fou de secours** des sujets pris en bloc.

> **Révisions de règle hors boucle.** Une demande qui contredit une règle déjà
> actée n'est **pas** un correctif : c'est une révision de spec, qui n'entre pas
> dans le séquencement courant et attend le palier qui la porte. Trois telles
> demandes sont en attente (cf. Risques & questions ouvertes) : le workflow
> demande/accord avant réaffectation (palier « imprévu & échange »),
> l'interdiction/dédoublonnage de la pose répétée d'un même slot, et le **choix
> explicite d'une ancre/d'un début de cycle** (option « date d'ancrage » jadis
> écartée au profit de l'ancrage ISO sans ancre), qui sera **ré-arbitré au
> make-gherkin du palier « cycle de fond riche »**.

## Séquence de livraison

Tous les besoins comptent, mais ils sont **séquencés** (pas livrés en bloc).
Chaque palier doit être adopté avant le suivant ; chacun se borne au plus petit
pas qui apporte une valeur lisible. Le palier de fondations a ouvert la séquence
au titre de l'exception bornée ; il est **refermé**, et les paliers d'usage
« saisie visible », « lisibilité & thème », « édition des acteurs », « config
foyer persistante » et « récurrence des périodes » qui l'ont suivi sont
**livrés**. L'arbitre d'usage tient l'ordre : les paliers d'usage d'abord, les
paliers **techniques en queue de séquence**, derrière tout l'usage — à la seule
**exception bornée** de la persistance de la config foyer, qui a été tirée devant
parce qu'elle portait un observable d'usage direct, et qui est désormais livrée.

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
   l'acteur, pas sur son libellé d'affichage).

3. **Lisibilité & thème — qui garde se lit d'un coup d'œil** *(reprise produit —
   **LIVRÉ**)*. La responsabilité de période est **explicite** dans la grille : le
   **nom du responsable** est affiché dans la case **et** une **légende couleur**
   accompagne la grille ; un nom trop long est **tronqué** tout en restant lisible
   en entier au **survol** ; un acteur hors du set connu reste **affiché et
   distingué** (gris assumé) sans perdre son nom. L'app porte un **thème en accord
   avec le domaine** (garde d'enfants).

4. **Config foyer — édition des acteurs (en mémoire)** *(reprise d'usage —
   **LIVRÉ**)*. Un écran pour **éditer les acteurs du foyer** : leurs **noms** et
   leurs **couleurs**. Le seed jusqu'ici figé est devenu **éditable** et la
   **grille (case + légende) reflète immédiatement** le changement *dans la
   session*. L'édition vivait alors **en mémoire** ; sa **durabilité** a été
   portée par le palier suivant.

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
   mémoire**. *Livré : ajout **sans suppression** d'abord (la suppression d'acteur
   est séquencée au palier « CRUD acteurs complet »), **sans** édition du cycle de
   fond ici.*

6. **Récurrence des périodes — définir le cycle de fond** *(reprise d'usage —
   **LIVRÉ**)*. Une **couche de résolution du responsable de fond** est posée sous
   les périodes explicites : le cycle compte **N semaines** (N ≥ 1) et alterne par
   **parité de la semaine ISO** (`index = semaine ISO du jour, modulo N`), chaque
   index étant mappé sur un **responsable de fond** résolu sur son **identifiant
   stable** (jamais le libellé). La résolution d'une case suit une **priorité** :
   **surcharge (période saisie) > fond (cycle) > neutre**. La grille affiche le
   fond (case **nommée + colorée** + **légende** étendue aux responsables de fond
   présents dans la fenêtre) sans qu'aucune période ne soit saisie ; un index sans
   responsable retombe sur la **teinte neutre** (repli miroir de la couleur
   neutre), sans nom fantôme. Le cycle est **éditable** depuis la configuration du
   foyer (section « Cycle de fond » : nombre de semaines + un sélecteur de
   responsable par index, **alimenté par les acteurs persistés** du foyer) ;
   définir **zéro semaine** est **refusé** (« le cycle doit compter au moins une
   semaine »), le cycle précédent restant inchangé. Toute ré-édition du mapping
   met à jour la grille **sans rechargement** (diffusion temps réel), et sur
   édition concurrente la **dernière écriture gagne**. *Le cycle vit **en mémoire**
   (borne anti-cliquet) ; sa durabilité est portée par le palier « config foyer
   durable — reste ». Son **ergonomie riche** (ancre, frontière de jour, plages,
   sur-cycles) est un palier d'évolution séquencé (cf. ci-dessous).*

7. **Calendrier navigable & écriture en contexte — dialogs depuis le planning**
   *(reprise produit — **PROCHAIN SUJET**)*. Supprimer les **écrans dédiés** Poser
   un slot / Affecter une période / Définir un transfert et les rouvrir comme
   **dialogs ouvertes depuis le planning** (la case, le bouton) : l'utilisateur
   **agit là où il lit**. Le plus petit incrément observable = **deux dialogs**
   (Poser un slot + Affecter une période) ; le **transfert** est une **tranche de
   secours** (livrée si le scope tient, sinon séquencée juste derrière — jamais
   reportée en bloc). Inclut à terme la navigation **passé/futur**, des **vues
   prédéfinies** (semaine, mois, 4 semaines glissantes) et la **sélection d'une
   plage de cases** pour affecter une période sur l'intervalle. *Réutilise
   l'existant : commandes / handlers d'écriture déjà livrés et le canal
   requête/réponse (jamais de DI directe), réapparition immédiate de la saisie
   dans la grille et diffusion SignalR lecture seule (jamais reconstruite).
   L'observable est le **déplacement de la saisie en contexte**, pas une nouvelle
   règle de gestion ; aucune persistance tirée en avant (slots / périodes /
   transferts restent en mémoire).*

8. **CRUD acteurs complet — suppression & amorce d'impersonation bornée**
   *(reprise d'usage, forward rang 2)*. Compléter le cycle de vie des acteurs par
   la **suppression** (Create + Read + Update livrés ; **Delete** manquant), en
   **cadrant les cases orphelines** (slot/période d'un acteur retiré). Embarque
   une **amorce d'impersonation bornée** : tant que les acteurs ne sont pas des
   **utilisateurs réels** (auth au palier « ouverture de l'accès »), l'utilisateur
   principal peut **incarner un acteur** pour agir en son nom — **convenance
   admin**, **pas** l'authentification complète. *Ne pas tirer la fondation auth
   devant l'usage ; l'impersonation reste une amorce, l'auth réelle vient à son
   palier.*

9. **Cycle de fond riche — ancre, frontière de jour, plages & sur-cycles**
   *(reprise d'usage, forward rang 3, sujet plein à découper)*. Enrichir le cycle
   de fond livré (palier 6) pour le rendre **réellement utilisable au quotidien** :
   **(a)** choisir explicitement le **début / l'ancre** du cycle (quelle semaine =
   index 0), **(b)** une **frontière de jour paramétrable** (ex. vendredi →
   vendredi), **(c)** une **plage de validité** début **et** fin, **(d)** un
   **sur-cycle / exception saisonnier** (vacances), **(e)** un cycle **WE-only**
   (1 week-end sur 2). *Ce palier **rouvre explicitement** la décision actée
   « ancrage ISO sans ancre » — le choix d'un début/d'une phase **est** l'option
   « date d'ancrage » jadis écartée : elle est **ré-arbitrée au make-gherkin de ce
   palier**, pas avant (révision de règle hors boucle). Besoin gardé **groupé** ;
   sa **découpe est impérative** au cadrage (corollaire de découpe). La plage
   début/fin et les sur-cycles **chevauchent la durabilité du cycle** (palier
   suivant) : n'enrichir que l'**observable**, ne PAS tirer Mongo par précaution
   (borne anti-cliquet).*

10. **Survol → résumé de la journée** *(reprise d'usage, évolution — séquencée,
    skippée faute de demande PO)*. Au survol prolongé (~1 s) d'une case, afficher
    un **résumé de la journée**, au-delà du seul nom complet déjà rendu au survol
    simple. *Enrichissement, pas une réparation : le survol simple (nom complet)
    est livré et conforme. Le périmètre du résumé (périodes / slots / responsable
    / transferts) est à cadrer au make-gherkin et ne doit pas être sous-estimé.
    Skippé tant que le PO ne le réclame pas ; séquencé, pas écarté.*

11. **Config foyer durable — reste de la config** — **persister** la part
    restante de la configuration du foyer (lieux, set de couleurs par défaut,
    **cycle de fond**) plutôt que de la porter en données figées dans le code.
    *Le **volet acteurs** de la config durable a déjà été livré au palier 5
    (premier client du store réel) ; ce palier complète la config durable pour les
    lieux, couleurs et **cycle de fond** (aujourd'hui en mémoire).*

12. **Modèle d'acteurs & foyer** — déclarer les acteurs réels (Admin, Parents,
    Autres) dans un écran de configuration, qui porte aussi la **responsabilité
    récurrente de fond** (le cycle) et le **set de couleurs par défaut**.
    *Exploite la config persistée des paliers précédents ; prérequis de
    l'ouverture de l'accès.*

13. **Immédiat & événements à venir** — « qui récupère ce soir », où est l'enfant
    maintenant, transferts et changements à venir présentés comme événements dans
    un **panneau cloche**, plus comme un tableau permanent. *Valeur quotidienne ;
    expose enfin les transferts, aujourd'hui invisibles par construction.*

14. **Imprévu & échange** — enfant malade / retard / échange de dernière minute,
    transferts **dérivés automatiquement** par défaut et saisie réservée à
    l'exception. *Le plus délicat ; après que l'usage à deux est acquis. Porte la
    question ouverte du **workflow demande/accord** avant réaffectation d'une
    période à l'autre parent.*

15. **Ouverture de l'accès** — landing page et authentification des acteurs réels
    (email via Gmail / Apple / Microsoft) pour lever le risque mortel d'adoption.
    Débloque aussi la **personnalisation des couleurs par utilisateur** et la
    **persistance d'une préférence de thème** (thème sombre), et **transforme
    l'impersonation bornée** (palier 8) en accès réel par acteur. *Vient après le
    socle et le modèle d'acteurs ; à ne pas laisser glisser indéfiniment.*

16. **Persistance réelle — adaptateurs de droite (reste du domaine)** *(palier
    technique, derrière l'usage)* — remplacer les dépôts **en mémoire** (volatils)
    des **slots, périodes et transferts** par des **adaptateurs de droite** vers
    un store **durable**, derrière les ports existants, sans toucher au domaine.
    *Débloqué par la fondation (palier 1) mais **subordonné à l'usage**. La
    **config foyer** en a été le **premier client**, déjà rendu durable au palier
    5 (exception bornée) ; ce palier étend la durabilité au reste du domaine.
    **Borne anti-cliquet** : il ne remonte pas devant l'usage.*

17. **Saisie hors-ligne (PWA)** *(palier technique, derrière l'usage)* — au-delà
    de l'**échec clair** déjà livré (palier 1), mettre en cache et **mettre en
    file** les écritures faites hors connexion, **rejouées au retour du réseau**.
    *Subordonné à l'usage. Piste consignée : une **file d'écritures côté client**
    (type *outbox*, IndexedDB) garantissant un rejeu « exactement une fois » comme
    socle minimal ; l'*event sourcing* n'est retenu que si offline / rejeu / audit
    le justifient — à trancher au moment d'ouvrir ce palier, pas un prérequis.*

> **Re-séquencement acté.** La **config foyer persistante** (ajout d'acteurs +
> persistance bornée) a été insérée devant la récurrence des périodes, par
> priorité d'usage, et est **livrée** ; la **récurrence des périodes** (cycle de
> fond par parité ISO, en mémoire) est **livrée** à sa suite. Le **prochain
> sujet** est l'**écriture en contexte par dialogs** depuis le planning (palier
> 7). Derrière elle viennent, par priorité d'usage actée, le **CRUD acteurs
> complet + impersonation bornée** (palier 8) puis le **cycle de fond riche**
> (palier 9, qui rouvre l'ancrage ISO à son cadrage). Le **survol enrichi**
> (palier 10) est **skippé** ce cycle faute de demande PO, séquencé sans être
> écarté. La persistance du **reste du domaine** reste en queue (palier 16),
> derrière tout l'usage — seule la persistance de la config foyer a été tirée
> devant (borne).

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
> l'usage par l'arbitre. La **restructuration du code applicatif** menée
> hors-pipeline (adaptateurs de droite par techno, SignalR comme adaptateur de
> gauche, rangement par type, code-behind systématique) a été conduite **à
> iso-comportement strict** — aucun observable métier touché — et sa condition de
> sortie a été tenue : **suite complète verte** via `dotnet test` **sans
> `--no-build` ni filtre, Docker actif** (pivot Mongo de la config foyer inclus).

> **Prochain sujet** : palier 7, **écriture en contexte par dialogs depuis le
> planning** — supprimer les écrans dédiés Poser un slot / Affecter une période /
> Définir un transfert et les rouvrir comme **dialogs ouvertes depuis une case**.
> Scope minimal = **deux dialogs** (Poser un slot + Affecter une période), le
> **transfert** en **tranche de secours**. Réutilise les commandes / handlers et
> le canal HTTP déjà livrés (pas de handler neuf attendu), la réapparition
> immédiate dans la grille et la diffusion SignalR ; aucune persistance tirée en
> avant. Il redevient le prochain sujet `/2-make-gherkin` sur cette spec.

## Mécaniques de base

- Un créneau de garde a un horaire (début → fin), un responsable unique, un lieu et une activité
- Une journée est une suite de créneaux enchaînés
- Le planning suit un **cycle de fond** de plusieurs semaines qui se répète automatiquement : il compte **N semaines** (N ≥ 1) et alterne par **parité de la semaine ISO** (`index = semaine ISO du jour, modulo N`) ; chaque index est mappé sur un **responsable de fond**. La responsabilité d'une case se résout par **priorité** : **surcharge (période explicitement saisie) > fond (cycle) > neutre** — une période saisie prime toujours sur le cycle, qui reprend ensuite ; un index de cycle sans responsable retombe sur la **teinte neutre** sans nom
- Le hub `/planning` est un calendrier navigable façon agenda ; on s'y déplace dans le **passé et le futur**, avec des **vues prédéfinies** (semaine, mois, 4 semaines glissantes). La **fenêtre par défaut** est **4 semaines glissantes à partir de la semaine en cours** ; les slots y sont positionnés dans les cases jour/horaire. On peut **sélectionner une plage de cases** pour définir une période (affecter un responsable sur l'intervalle)
- La responsabilité de chaque garde se lit par un **code couleur par personne**, **doublé du nom du responsable affiché dans la case et d'une légende** : la couleur seule ne suffit pas à identifier qui garde. La **légende** agrège les responsables présents dans la fenêtre, **y compris les responsables de fond** issus du cycle, dédoublonnés par identifiant. Un nom trop long est **tronqué** dans la case, son **intitulé complet restant lisible au survol** ; un acteur hors du set de couleurs connu reste **affiché et distingué** (teinte neutre assumée) sans perdre son nom. La couleur d'un responsable se résout sur un **identifiant d'acteur stable**, jamais sur son libellé d'affichage : les sélecteurs de saisie **et le mapping du cycle de fond** fournissent ce même identifiant que la palette, sinon la case retombe sur la couleur neutre
- L'app porte un **thème en accord avec son domaine** (garde d'enfants), au service de la lisibilité d'usage ; c'est une ergonomie de surface, subordonnée à l'usage
- Les formulaires de saisie pré-remplissent leurs dates sur **« aujourd'hui »** (la date de référence), jamais sur une date figée : une saisie tombe ainsi dans la fenêtre affichée et réapparaît immédiatement dans la grille
- La grille est en **lecture seule** : elle consomme les slots et périodes déjà enregistrés **et le cycle de fond résolu**, sans écriture. Toute écriture (poser un slot, affecter, surcharger ou supprimer une période, ajuster un transfert) se fait en contexte via des **dialogs** ouvertes depuis une case, alimentées par les acteurs et lieux du foyer ; la **sélection d'une plage de cases** ouvre l'affectation d'une période sur l'intervalle choisi
- Les **acteurs du foyer (noms et couleurs) sont éditables** depuis un écran de configuration : renommer ou recolorier un acteur **met immédiatement à jour la grille** (case **et** légende) qui relit la configuration. On peut aussi **ajouter** un acteur (parent / autre / nounou) : l'ajout génère un **identifiant d'acteur stable neuf** (jamais le libellé) et la grille le reflète aussitôt (case + légende dédoublonnée par identifiant). La **suppression** d'un acteur est séquencée (palier « CRUD acteurs complet »). La configuration du foyer ainsi éditée **survit au redémarrage** : elle est **persistée** derrière les **ports de droite** par leur adaptateur durable. Cette durabilité est **bornée à la config foyer** (référentiel des acteurs : noms, couleurs, acteurs ajoutés) ; le reste du domaine (slots, périodes, transferts, **cycle de fond**) reste en mémoire le temps de son propre palier de persistance
- La **responsabilité récurrente de fond** (qui garde selon le cycle) se déclare dans la configuration du foyer (section « Cycle de fond » : nombre de semaines + un sélecteur de responsable par index, alimenté par les **acteurs persistés** du foyer, sur leur identifiant stable). Le calendrier ne porte que les **surcharges ponctuelles** d'une période, qui priment sur le fond. Définir **zéro semaine** est refusé ; toute ré-édition du mapping met la grille à jour **sans rechargement** (diffusion temps réel), et sur édition concurrente la **dernière écriture gagne**
- **Le front est découplé du back par une API** : il ne manipule pas le domaine en direct, il **émet ses commandes et lit ses données à travers l'API**, ce qui ouvre l'app à d'autres clients (front navigateur, IHM tierce, agents). L'**hôte d'API est détaché** : le back démarre seul, sans référencer le front, et le front — exécuté **dans le navigateur** (WebAssembly) — consomme une **API distante**
- **Toute écriture passe par le canal requête/réponse** : aucune vue n'écrit le domaine en direct. Une saisie qui contournerait le canal (appel direct du back) est une dette à résorber, pas un mode de fonctionnement
- **Deux canaux distincts, jamais confondus** : l'**écriture** est confiée à un **canal requête/réponse** (la commande part, la réponse confirme l'effet) ; la **diffusion temps réel** vers les autres acteurs (notifications, actualisation du planning à l'écran) passe par un **canal de diffusion en lecture seule**. On **n'écrit jamais** par le canal de diffusion : une saisie se propage aux autres écrans parce que l'écriture, une fois aboutie, **déclenche** la diffusion
- **Échec clair si l'API distante est injoignable** : la commande non aboutie produit un message à l'écran et la saisie **n'est pas appliquée** ni perdue de vue (elle reste à resoumettre). Aucune mise en file ni rejeu à ce stade — le hors-ligne rejouable est un palier technique ultérieur
- L'**API est explorable** : elle expose un **document de description** (OpenAPI) et une **UI interactive** pour essayer les endpoints (type Swagger-UI / Scalar), et autorise l'**origine du front** par sa configuration CORS
- La configuration du foyer est une **donnée éditable** : pour le **référentiel des acteurs** (noms, couleurs, acteurs ajoutés) elle est **éditable ET durable** — elle vit derrière les **ports de droite**, dont l'adaptateur durable la persiste et la fait survivre au redémarrage. Pour le **reste** (lieux, set de couleurs par défaut, **cycle de fond**) elle est **éditable en mémoire** dès maintenant, et **durable à terme** une fois son adaptateur posé. Dans tous les cas, c'est une donnée derrière les ports, jamais une constante figée dans le code
- Les transferts et changements à venir sont présentés comme des événements dans un panneau de notifications (cloche), pas comme un tableau permanent
- Trois types d'acteurs : Admin, Parent, Autre — chacun avec un affichage adapté à son type
- La composition du foyer (enfants, parents, acteurs autres) se déclare dans un écran de configuration distinct du planning

## Règles de gestion

### Foyer & acteurs

1. **Multi-enfants** — Un foyer compte au moins un enfant, et peut en compter plusieurs, chacun avec sa propre organisation de garde

2. **Familles recomposées** — Un foyer peut mélanger des enfants de parents différents, tous visibles dans le même planning

3. **Toujours deux parents** — Un foyer a toujours exactement deux parents ; tant qu'un seul est inscrit, il peut saisir les informations de l'autre

4. **Acteurs « autres » ajoutables et éditables** — Un foyer peut avoir N acteurs « autres » (nounou, grands-parents…). Ils sont **ajoutables et éditables** (par les parents ou par l'acteur lui-même), au-delà du seul renommage/recoloriage du seed initial : ajouter un acteur le fait exister dans le foyer et dans la grille (cf. règle 6). Leur **suppression** est séquencée au palier « CRUD acteurs complet » (cf. règle 6)

5. **Édition des acteurs (noms + couleurs)** — Les acteurs du foyer (leurs **noms** et leurs **couleurs**) sont **éditables** depuis un écran de configuration, et la **grille (case + légende) relit immédiatement** la configuration éditée : renommer ou recolorier un acteur se voit aussitôt, **partout où l'acteur apparaît** (case, légende, sélecteurs de saisie et mapping du cycle de fond doivent lire le même référentiel vivant). Cette édition s'est livrée d'abord **en mémoire, dans la session** ; sa **durabilité** (survie au redémarrage) est portée par la règle 6 et la règle 30. La survie au redémarrage **est acquise pour la config foyer** : la dette volatile de l'édition s'éteint là, ailleurs elle subsiste (cf. règle 30)

6. **Ajout d'acteur, persistance bornée & suppression à venir** — On peut **ajouter** un acteur au foyer (parent / autre / nounou) : l'ajout génère un **identifiant d'acteur stable neuf** (jamais dérivé du libellé d'affichage) et la grille (case + **légende dédoublonnée par identifiant**) le reflète immédiatement. La **configuration du foyer** — référentiel des acteurs : **noms, couleurs, acteurs ajoutés** — est **persistée** derrière les ports de droite par un **adaptateur durable** : elle **survit au redémarrage**. Cette persistance est **bornée à la config foyer** : c'est le **premier client** du store durable, tiré **devant l'usage** parce qu'il porte un observable direct. L'ajout s'est livré **sans suppression** d'abord et **sans** édition du cycle de fond. La **suppression d'un acteur** (Delete) est un **besoin séquencé** (palier « CRUD acteurs complet ») qui devra **cadrer les cases orphelines** (slot/période d'un acteur retiré). La persistance du **reste du domaine** reste **en queue** (règle 30, borne anti-cliquet)

7. **Responsabilité de fond en config, exception au calendrier** — La responsabilité récurrente de garde (le **cycle de fond** : qui garde par défaut) se déclare dans l'écran de configuration du foyer, en même temps que les acteurs ; elle est **éditable** (en mémoire d'abord, durable une fois son adaptateur posé — palier « config foyer durable — reste »). Le calendrier ne sert qu'aux **surcharges ponctuelles** d'une période donnée, qui **priment** sur le fond. Les dialogs d'affectation et de surcharge, comme le **sélecteur de responsable du cycle**, sont alimentés par les acteurs du foyer sur leur **identifiant stable**

### Rôles & accès

8. **Trois types d'acteurs** — Trois types d'accès : Admin (gère tout, y compris la configuration du foyer), Parent (gère les créneaux, lieux, acteurs et invitations de son foyer), Autre (consultation et édition limitée à ses propres informations). L'affichage s'adapte au type. Tant que les acteurs ne sont pas des **utilisateurs réels** (authentification au palier « ouverture de l'accès »), une **impersonation bornée** est séquencée (palier « CRUD acteurs complet ») : l'utilisateur principal peut **incarner un acteur** pour agir en son nom — **convenance admin**, **pas** l'authentification complète, qui n'est jamais tirée devant l'usage

9. **Modification réservée aux parents et à l'admin** — Seul un Parent (ou l'Admin) peut créer, éditer ou supprimer un créneau, une période **ou le cycle de fond** ; un acteur « Autre » n'édite que ses propres informations

### Planning & créneaux

10. **Responsable unique** — Un créneau a toujours un seul responsable (pas de « X ou Y »)

11. **Cycle de fond récurrent, éditable** — Le planning se répète selon un **cycle de fond** de **N semaines** (N ≥ 1) : l'index de la semaine est sa **parité ISO** (`index = semaine ISO du jour, modulo N`) et chaque index est mappé sur un **responsable de fond** résolu sur l'**identifiant stable** de l'acteur (jamais le libellé ; un index non mappé = pas de fond → teinte neutre). Ce cycle est **définissable et éditable** depuis la configuration du foyer (nombre de semaines + responsable par index, alimenté par les acteurs persistés) et **non figé dans le code** ; définir **zéro semaine** est **refusé** (« le cycle doit compter au moins une semaine »), le cycle précédent restant inchangé. La ré-édition du mapping met la grille à jour **sans rechargement** ; sur édition concurrente, la **dernière écriture gagne**. *Note d'évolution (séquencée) : l'**ancre/le début explicite** du cycle, la **frontière de jour paramétrable**, les **plages de validité**, **sur-cycles saisonniers** et **cycles WE-only** sont un palier d'évolution (« cycle de fond riche ») ; choisir explicitement une ancre **rouvre la décision actée « ancrage ISO sans ancre »** et sera **tranché au make-gherkin de ce palier**, pas avant (révision de règle hors boucle)*

12. **Exception ponctuelle prime sur le fond** — Un jour précis peut être surchargé sans casser le cycle de fond : une **période explicitement saisie prime** sur le responsable de fond (priorité **surcharge > fond > neutre**), et le cycle **reprend ensuite** automatiquement autour de la surcharge

13. **Lieux éditables** — La liste des lieux (domicile A/B, école, nounou, activité…) est librement modifiable et sert de référentiel aux sélecteurs de saisie

14. **Grille en lecture seule** — La grille agenda consomme les slots, périodes et **le cycle de fond résolu** déjà enregistrés et les rend (positionnés dans leur case, colorés par responsable, **nommés**) sans jamais écrire ; toute écriture passe par une dialog ouverte depuis une case, ou par la **sélection d'une plage de cases** pour affecter une période

15. **Suppression de période** — Un Parent (ou l'Admin) peut supprimer une période de garde ; c'est une action d'écriture menée depuis une dialog contextuelle, hors de la grille en lecture pure. Sous la période supprimée, le **cycle de fond reprend** (la case retombe sur son responsable de fond, ou sur le neutre si l'index n'est pas mappé)

16. **Pose répétée d'un même slot acceptée avec avertissement** — Un slot qui chevauche ou redouble un slot existant est **accepté**, accompagné d'un **avertissement** ; il n'est ni refusé ni dédoublonné. (Une demande d'interdiction/dédoublonnage est en attente comme révision de règle — cf. Risques & questions ouvertes.)

17. **Date de saisie par défaut = aujourd'hui** — Les formulaires de saisie (poser un slot, affecter ou surcharger une période) pré-remplissent leurs dates sur la **date de référence « aujourd'hui »**, jamais sur une date figée : la saisie tombe ainsi dans la fenêtre affichée et **réapparaît immédiatement** dans la grille. Une date par défaut figée hors fenêtre est une non-conformité à corriger, pas un comportement attendu

### Code couleur & lisibilité

18. **Responsabilité lisible : couleur par personne + nom + légende** — La responsabilité de garde se lit dans la grille par une couleur qui distingue chaque **personne** (Parent A ≠ Parent B), pas seulement son type de rôle ; cette couleur est **doublée du nom du responsable affiché dans la case et d'une légende**, car la teinte seule ne suffit pas à dire qui garde. La **légende** agrège les responsables présents dans la fenêtre **y compris les responsables de fond** issus du cycle, dédoublonnés par identifiant. Un **nom trop long** est **tronqué** dans la case tout en restant lisible **en entier au survol** ; un acteur **hors du set de couleurs connu** reste **affiché et distingué** (teinte neutre assumée) **sans perdre son nom**

19. **Couleur résolue sur un identifiant d'acteur stable** — La couleur d'un responsable se résout sur l'**identifiant stable** de l'acteur, jamais sur son libellé d'affichage. Les sélecteurs de saisie (affectation, surcharge) **et le mapping du cycle de fond** **fournissent ce même identifiant** que la palette ; un acteur **ajouté** reçoit un identifiant stable neuf résolu de la même façon. Un libellé qui ne correspond pas à un identifiant connu fait retomber la case sur la **couleur neutre** ; de même, un **index de cycle non mappé** = pas de fond → teinte neutre, sans nom fantôme. Une case grise là où un responsable est affecté trahit un libellé fourni à la place de l'identifiant — c'est le défaut à localiser, pas la résolution elle-même

20. **Set de couleurs par défaut, recoloriable** — Un jeu de couleurs par défaut, déclaré avec le foyer, différencie d'emblée les responsables tant qu'aucune personnalisation n'a été faite ; c'est ce set qui s'applique avant l'ouverture de l'accès. Ce set est **recoloriable** depuis l'écran de config et la grille suit ; la **personnalisation par utilisateur authentifié** (règle 21) reste un pas distinct lié à l'ouverture de l'accès

21. **Personnalisation des couleurs par utilisateur** — Une fois identifié, chaque acteur peut personnaliser les couleurs qu'il assigne aux acteurs qu'il voit ; cette personnalisation dépend de l'authentification (séquence, ouverture de l'accès) et n'altère pas le set par défaut vu par les autres

22. **Thème en accord avec le domaine** — L'app porte un **thème** cohérent avec son domaine (garde d'enfants), au service de la lisibilité d'usage. C'est une **ergonomie de surface subordonnée à l'usage** par l'arbitre : il n'ouvre aucune règle métier. Un **thème sombre avec bascule clair/sombre** (et persistance de la préférence) en est une **évolution additive**, consignée et non priorisée, à rattacher au futur écran de préférences utilisateur. L'**harmonisation de teinte** entre la pastille de légende et le fond de case (cf. Risques) relève du même registre d'ergonomie de surface, pas d'une règle métier

### Survol & détail de la case

23. **Survol : du nom complet au résumé de la journée** — Au survol d'une case, l'app expose un complément d'information sans quitter la grille. Le **survol simple** affiche le **nom complet** du responsable (utile quand le nom est tronqué) : c'est le comportement **livré et conforme**. Un **survol enrichi** est une **évolution** prévue (séquencée, skippée tant que le PO ne la réclame pas) : après un survol prolongé (~1 s), afficher un **résumé de la journée**. Le périmètre de ce résumé (périodes, slots, responsable, transferts) est à cadrer au moment de le scénariser ; ce n'est pas un correctif du survol simple, qui n'est pas défaillant

### Transferts

24. **Transfert dérivé par défaut** — Le transfert d'un enfant (qui dépose, qui récupère, à quelle heure) est dérivé automatiquement du planning par défaut : c'est le cas nominal, sans saisie

25. **Transfert modifiable et ponctuel** — Un transfert auto-calculé peut être modifié, et des transferts ponctuels peuvent être proposés ou ajoutés à l'exception (urgence, changement d'emploi du temps)

### Modifications & notifications

26. **Modification directe** — Un Parent applique son changement immédiatement ; les autres acteurs concernés sont notifiés (pas de workflow de validation). (Une demande de workflow demande/accord avant réaffectation d'une période à l'autre parent est en attente comme révision de règle, rattachée au palier « imprévu & échange » — cf. Risques & questions ouvertes.)

27. **Notifications & événements à venir** — Les notifications in-app couvrent les changements de planning et les rappels de transfert ; les transferts et changements à venir sont consultables dans un panneau d'événements accessible via une cloche

### Accès aux données & exploitation

28. **Écriture par le canal, échec clair si l'API est injoignable** — Toute écriture passe par le **canal requête/réponse** vers l'API distante ; aucune vue n'écrit le domaine en direct. Si l'API est **injoignable**, la commande échoue **clairement** (message à l'écran, saisie **non appliquée** et conservée à resoumettre), **sans mise en file ni rejeu** à ce stade. Le hors-ligne rejouable (cache + file d'écritures) est un palier technique ultérieur, derrière l'usage

29. **API explorable et origine du front autorisée** — L'API expose un **document de description** (OpenAPI) **et** une **UI interactive** d'exploration des endpoints (type Swagger-UI / Scalar), et autorise l'**origine du front** (CORS). C'est un garde-fou d'outillage, sans observable métier : aucune règle de gestion n'en dépend

30. **Données derrière les ports, durables — exception bornée pour la config foyer** — Les slots, périodes, transferts, **cycle de fond** et la configuration du foyer vivent derrière des **ports** ; la donnée du foyer n'est jamais une constante figée dans le code. Leur stockage est durable **à terme**, sans toucher au domaine ni aux règles. **Exception bornée actée et livrée** : la **config foyer** (référentiel des acteurs — noms, couleurs, acteurs ajoutés) est **persistée maintenant**, **devant l'usage**, parce qu'elle porte un observable direct (survie au redémarrage) et qu'elle est le **premier client** du store durable. Le **reste du domaine** (slots, périodes, transferts **et le cycle de fond**) reste **en mémoire** (adaptateur InMemory), sa durabilité **séquencée derrière l'usage** : la **borne anti-cliquet** empêche que cette exception entraîne le reste devant l'usage. Cette règle porte la **durabilité** ; elle reste **distincte de l'édition** (règle 5) — rendre une donnée éditable (cf. le cycle de fond, éditable mais volatil) n'oblige pas à la rendre durable dans le même incrément, sauf quand, comme pour la config foyer, la durabilité porte un observable direct et reste bornée

## Risques & questions ouvertes

- **L'usage tient la main — ne pas enchaîner un sprint sans valeur produit.** Deux sprints structurels (fondation, hôte d'API) puis cinq sprints d'usage (saisie visible, lisibilité & thème, édition des acteurs, config foyer persistante, récurrence des périodes) sont derrière nous. Les prochains sujets (dialogs en contexte, CRUD acteurs complet, cycle de fond riche, survol enrichi) restent des paliers **d'usage** ; les paliers techniques débloqués (persistance du reste du domaine, PWA, Docker) sont tentants mais **doivent rester derrière l'usage**. La seule persistance tirée devant a été **bornée à la config foyer** (observable direct) : ne pas en faire un cliquet.
- **Prochain sujet (dialogs) — transfert en tranche de secours, débordement ~2h IA.** L'écriture en contexte par dialogs livre au minimum **deux dialogs** (Poser un slot + Affecter une période) ; le **transfert** est une **tranche de secours** : si les trois débordent ensemble, **couper** au plus petit incrément observable (2 dialogs livrées) et séquencer le transfert juste derrière — **jamais reporter en bloc** (corollaire de découpe / leçon config foyer). L'observable est le **déplacement de la saisie en contexte**, pas une règle neuve ; réutiliser commandes / canal / SignalR existants, aucun handler neuf attendu.
- **Impersonation bornée (palier 8) vs auth réelle (palier 15).** Le besoin d'une vue / de droits **par acteur** est réel **maintenant**, mais l'authentification réelle (OAuth) vient au palier « ouverture de l'accès ». Cadrer l'impersonation comme **amorce de convenance admin** (l'utilisateur principal incarne un acteur), **jamais** l'authentification complète : ne pas tirer la fondation auth devant l'usage. À la livraison du palier 15, l'impersonation se transforme en accès réel par acteur.
- **Cycle de fond riche (palier 9) — sujet plein, deux frontières à surveiller.** (1) L'enrichissement **rouvre** la décision actée « ancrage ISO sans ancre » : choisir explicitement un début/une phase est l'option « date d'ancrage » jadis écartée, à **ré-arbitrer au make-gherkin de ce palier** (révision de règle hors boucle) — la règle 11 n'est **pas** révisée d'ici là. (2) Plage début/fin **+** sur-cycles vacances **chevauchent la durabilité du cycle** (palier « config foyer durable — reste ») : n'enrichir que l'**observable** de cycle, ne PAS tirer Mongo pour le cycle par précaution (borne anti-cliquet). Le besoin est gardé **groupé** ; sa **découpe est impérative** au cadrage. Risque spec « coût de saisie du cycle » exactement ici.
- **Cohérence de l'écran de config avec la grille (défaut connu, hors-spec).** Un écart de cohérence interne à l'écran de configuration (une lecture pointant une liste statique au lieu du **store vivant des acteurs**) peut faire afficher un libellé **périmé** après renommage. C'est une **dérive vis-à-vis de la règle 5** (« tout lit le même référentiel vivant »), corrigée par un **fix ciblé léger hors make-gherkin** (repointer la lecture sur le store) ; ce n'est **pas** une règle ni un palier, et la cible reste : un acteur renommé/ajouté est aussitôt cohérent **partout** (case, légende, sélecteurs, mapping du cycle).
- **Coût de saisie du cycle** — Saisir un cycle multi-semaines est lourd ; le cycle de fond livré (parité ISO, mapping par index) en pose le socle, et le palier « cycle de fond riche » devra le rendre **supportable** sans le complexifier inutilement. Le transfert dérivé automatiquement réduit ce coût pour le cas nominal.
- **Acceptation runtime obligatoire (rempart anti vert-qui-ment).** Les incréments d'usage sont prouvés sur l'**app réellement câblée** (front WASM + API distante + SignalR), pas par doublures : le cycle de fond a été accepté en affichant le **fond résolu** (case nommée + colorée sur l'identifiant stable) sans saisie de période, et en **convergeant sans rechargement** sur édition concurrente. Ce rempart reste la règle pour les prochains incréments (dialogs, suppression d'acteur) : un test de composant à doublures peut afficher une grille alors que le câblage réel échoue ; l'acceptation vérifie que des données **réellement enregistrées** apparaissent positionnées, colorées et **nommées**.
- **Borne anti-cliquet à tracer sans déraper.** La persistance devant l'usage est une **exception bornée à la config foyer**. Risque de **cliquet** : que le reste du domaine (slots / périodes / transferts / **cycle de fond**) suive devant l'usage. Garder cette persistance **en queue** (paliers « config foyer durable — reste » puis « persistance réelle ») ; la borne est écrite noir sur blanc (règle 30, et `docs/BACKLOG.md`).
- **Édition vs persistance — deux périmètres à ne pas confondre.** La config foyer est devenue **durable** (référentiel des acteurs), mais le **reste du domaine reste volatile** : slots, périodes, transferts **et le cycle de fond** vivent encore en mémoire jusqu'à leur palier de persistance. Ne pas reprocher au reste de ne pas persister (c'est la découpe) ; ne pas tirer leur persistance en avant au prétexte que la config foyer est durable (c'est le cliquet).
- **Périmètre « résumé de la journée » (survol enrichi) non défini** — périodes ? slots ? responsable ? transferts ? Sujet potentiellement plus gros qu'il n'y paraît, proche du « qui récupère ce soir » (palier immédiat). À **cadrer au make-gherkin** quand le survol sera pris ; ne pas le sous-estimer comme « simple tooltip ». Le survol simple (nom complet) est **conforme et accepté** : rien de cassé, le résumé est un comportement **neuf**. Skippé ce cycle faute de demande PO.
- **Légende ≠ bug (non-bug, harmonisation de teinte).** Le ressenti « les couleurs de la légende ne sont pas celles des acteurs » a été **confronté au code courant** : la légende et la case-jour résolvent le **même token couleur sur le même singleton**. **Aucun défaut de résolution.** L'écart vient d'une **incohérence de teinte de présentation** : pastille de légende **saturée** vs fond de case **pâle** (choix de design : fond pâle = texte sombre lisible). C'est une **évolution de teinte**, **jamais** un fix ciblé. À regrouper avec l'ergonomie config (palette/picker de couleur) quand elle remontera, pas un sujet seul.
- **Évolutions de surface non priorisées seules** — **sélecteur de couleur (palette / picker)** dans l'écran de config (au lieu d'une saisie libre) ; **onglets** de config par acteur (faible conviction PO : « un seul foyer → tous les acteurs sur le même écran ») ; **harmonisation de teinte** légende ↔ case (ci-dessus). Reconnues, séquencées derrière l'usage, sans règle ni palier dédié tant que l'usage ne les appelle pas.
- **Question ouverte — workflow demande/accord (révision de règle 26)** — Le PO veut qu'une période ne puisse être réaffectée à l'autre parent qu'après une **demande explicite acceptée**. C'est une révision de la règle « modification directe », pas un correctif ; elle attend le palier « imprévu & échange » et ne génère aucune règle ni sujet tant qu'il n'est pas ouvert.
- **Question ouverte — interdiction/dédoublonnage de slot (révision de règle 16)** — Le PO veut **refuser ou dédoublonner** la pose répétée d'un même slot. C'est une révision du choix v1 « accepté avec avertissement », hors de la boucle courante.
- **Question ouverte — ancre/début explicite du cycle (révision de la décision « ancrage ISO sans ancre »)** — Le PO veut **choisir le début / la phase** du cycle (quelle semaine = index 0). C'est une révision de la décision actée d'ancrage ISO, rattachée au palier « cycle de fond riche » (palier 9) et tranchée **à son make-gherkin**, pas avant.
- **Adoption de l'autre parent (risque mortel)** — L'app n'a de valeur que si l'autre parent l'utilise aussi ; sinon le planning est faux. L'ouverture de l'accès (auth réelle, palier 15) la traite, mais elle vient tard : **à ne pas laisser glisser indéfiniment** derrière la technique. Aucun des paliers techniques en queue ni des prochains incréments d'usage ne lève ce risque.
- **Contraintes du découplage front/API distant** — Émettre les commandes à travers une API **distante** introduit des contraintes (échanges inter-domaines, sérialisation des commandes, configuration de l'URL d'API, future authentification) absentes quand le front parlait au back en direct ; elles s'accentuent avec l'hôte détaché et le front WASM.
- **Diffusion déclenchée par l'écriture** — Garantir qu'une écriture aboutie déclenche bien l'actualisation temps réel des autres écrans, sans jamais écrire par le canal de diffusion ; validé en usage réel pour le cycle de fond (deux écrans convergent sans rechargement sur ré-édition du mapping), à retenir comme acquis pour les dialogs en contexte.
- **Hors-ligne rejouable — piste à trancher au palier PWA** — Au-delà de l'échec clair livré, une **file d'écritures côté client** (type *outbox*, IndexedDB) garantissant un rejeu « exactement une fois » est la piste minimale ; l'*event sourcing* n'est retenu que si offline / rejeu / audit le justifient. Décision **au moment d'ouvrir le palier**, pas un prérequis.
- **Idées consignées non prioritaires** — Indicateur de **présence de l'autre parent** (temps réel) ; **slot imbriqué** (un slot peut en contenir un autre) ; **parents liés via leurs enfants** (graphe foyer) ; **familles recomposées** (déjà règle 2) : besoins reconnus, séquencés derrière l'usage prioritaire, sans règle ni palier dédié tant que l'usage ne les appelle pas.
- **Différenciation** — Vs Cozi / FamilyWall / agenda partagé : le manque précis qu'on couvre mieux reste à nommer.
</content>
</invoke>
