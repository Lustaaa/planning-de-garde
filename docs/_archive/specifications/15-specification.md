# Planning de garde — Organisation des semaines de garde

> Version 15 · consolide la v14 + docs/sprints/14-impersonation-bornee/99-sprint14-besoins-fin-itération.md. Remplace la v14, qui reste figée en historique.

## Contexte

Une app où parents et intervenants (nounou, grands-parents…) organisent à
l'avance et partagent les semaines de garde des enfants d'un foyer. Le hub
`/planning` est la mémoire partagée du foyer : un calendrier où l'on lit qui
garde qui, où, quand, et **d'où l'on agit directement**. La responsabilité de
chaque garde se lit d'un coup d'œil par un code couleur propre à chaque personne,
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
grille, **à la bonne date** et **en couleur du parent responsable** (la couleur
se résout sur l'identifiant stable de l'acteur). La grille est **lisible d'un
coup d'œil** : la couleur seule ne porte plus l'information — le **nom du
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

La **récurrence des périodes** est **livrée** : un **cycle de fond** déclaré dans
la configuration du foyer détermine **qui garde par défaut**, semaine après
semaine, sans qu'aucune période n'ait à être saisie. Le cycle compte
**N semaines** (N ≥ 1) et alterne par **parité de la semaine ISO**
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

L'**écriture en contexte par dialogs** est désormais **livrée et complète** :
l'utilisateur **agit là où il lit**. Un **clic sur une case** du planning ouvre
un **menu d'actions** à **trois entrées** (Poser un slot / Affecter une période /
Définir un transfert) ; chaque entrée ouvre une **dialog** pré-remplie sur la
**date de la case**, alimentée par les acteurs et lieux du foyer. **Tous les
écrans de saisie dédiés** (et leurs routes) — slot, période **et transfert** —
ont été **retirés** : il n'existe plus qu'**un seul chemin d'écriture**, en
contexte, et **plus aucun écran de saisie dédié ne subsiste**. L'épic « écriture
en contexte » est **refermé**. La rétroaction suit l'issue de la commande :
**succès** → la dialog se ferme, la grille est relue et, pour le transfert, un
**accusé non bloquant** (« Transfert défini ») s'affiche **à part** ; **échec**
(refus domaine **ou** API injoignable) → la dialog **reste ouverte**, message
**dans la dialog**, saisie **conservée**, grille **inchangée** ;
**chevauchement** (pose de slot) → l'écriture **aboutit**, la dialog se ferme et
un **avertissement non bloquant** s'affiche **à part**. L'accès en écriture est
**gaté** : le menu n'apparaît qu'aux Parents (la consultation seule des Invités
est préservée). Le transfert réutilise la **commande/handler `DefinirTransfert`**,
le **canal HTTP** et la **diffusion SignalR** déjà livrés (**aucun handler
neuf**) ; il **reste InMemory** (borne anti-cliquet). Le **calendrier navigable**
(navigation passé/futur, vues prédéfinies, sélection d'une plage de cases) reste,
lui, **à livrer** : la grille actuelle reste une vue posée, non encore navigable.

L'**appropriation des acteurs est désormais complète côté cycle de vie** : avec
l'ajout et l'édition acquis, la **suppression** (Delete) est **livrée**.
Supprimer un acteur du foyer le **retire du store durable** (config foyer
persistée Mongo) et **neutralise par repli** ses cases orphelines : la
**surcharge orpheline cesse de primer** et la case retombe sur le **fond** (le
cycle reprend) ou sur le **neutre** si l'index n'est ni mappé ni résolu, **sans
nom fantôme** ; si l'acteur supprimé était **mappé au cycle de fond**, son index
devient **non mappé → neutre**. L'acte s'accompagne d'un **accusé non bloquant**
(« Acteur supprimé »), **sans réaffectation automatique**, et se propage en
**temps réel** (diffusion SignalR) sur les autres écrans. Le **CRUD acteurs est
ainsi complet** (Create + Read + Update + Delete).

Le **dernier maillon de la tranche acteurs** — une **impersonation bornée
lecture seule** — est désormais **livré**, fermant la **boucle du cycle de vie
des acteurs**. L'utilisateur principal (Parent configurateur, **identité
réelle**) peut **incarner un acteur déjà déclaré** du foyer (**convenance
d'administration**) : un **bandeau « Vous incarnez X »** signale l'incarnation,
la **vue reflète le rôle de l'identité effective** (l'identité incarnée, ou
**repli sur la réelle**) — un « Autre » incarné ne voit plus le menu d'écriture
ni les écritures de l'écran de config, un Parent/Admin oui — et le **retour à
l'identité réelle** restaure l'état. Si l'acteur incarné est **supprimé de façon
concurrente**, l'identité effective **retombe automatiquement** sur la réelle (le
bandeau disparaît), par **extension de la neutralisation par repli** et en
**temps réel** (SignalR). Le **type d'acteur** (Admin / Parent / Autre) est
surfacé en **lecture seule** depuis la déclaration seed (extension read-only de
l'énumération des acteurs) ; l'identité effective **lit** ce type pour piloter le
droit d'écriture. **Borne dure tenue** : ce n'est **PAS** l'authentification
réelle du palier 16 (ni OAuth, ni comptes, ni sessions, ni prise en main, ni
droits par rôle persistés), il n'y a **pas d'écriture « au nom de »** (les
commandes restent émises sous l'**identité réelle**) et **aucune persistance
neuve** n'est tirée (l'état d'incarnation vit en **session / mémoire**, rien ne
subsiste au redémarrage). Le **prochain sujet d'usage** est désormais le
**calendrier navigable**.

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
> « lisibilité & thème », « édition des acteurs », « config foyer persistante »,
> « récurrence des périodes », « écriture en contexte (dialogs, **transfert
> inclus**) », « suppression d'acteur » **et « impersonation bornée lecture »**
> sont **livrés**. C'est cet arbitre qui tire le **calendrier navigable**
> (prochain sujet d'usage) **devant** les paliers techniques et la dette de test.

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
> remonter devant l'usage à sa suite. La **suppression d'acteur** (livrée) a
> opéré sur ce **même store durable** → l'acceptation runtime a été tenue sur
> **Mongo réel**. L'**impersonation bornée** (livrée) n'a, elle, tiré **aucune
> persistance neuve** (état de session / mémoire).

> **Corollaire « durable ICI, volatile encore ailleurs »** *(reformule l'ancien
> « éditable maintenant ≠ durable »)*. Rendre une donnée **éditable** n'oblige pas
> à la rendre **durable** dans le même incrément — c'est la découpe qui a permis
> de livrer l'édition des acteurs **en mémoire** sans tirer la persistance en
> avant, et de livrer le **cycle de fond en mémoire** sans tirer sa durabilité.
> Mais quand la durabilité porte un **observable d'usage direct** et reste
> **bornée**, elle se gagne : c'est le cas de la config foyer, dont la persistance
> est **livrée ICI** (la volatilité de l'édition des acteurs s'est **éteinte**
> pour la config foyer). **Partout ailleurs** — slots, périodes, transferts,
> **cycle de fond**, **et l'état d'incarnation** — la donnée reste **volatile**,
> sa durabilité **séquencée derrière l'usage** (l'impersonation, par borne, ne
> persiste rien). Le « durable » se gagne là où il porte un observable et reste
> borné ; il reste séquencé partout où ce n'est pas le cas.

> **Borne anti-cliquet.** L'exception de persistance est **bornée à la config
> foyer** et ne doit pas faire **cliquet** : aucun autre dépôt (slots, périodes,
> transferts, **cycle de fond**) n'est tiré devant l'usage au prétexte que la
> config foyer est passée durable. La persistance du **reste du domaine** demeure
> en **queue de séquence** (palier « config foyer durable — reste » puis
> « persistance réelle »), derrière tout l'usage. L'**écriture en contexte** l'a
> confirmé : déplacer la saisie en dialogs — **transfert compris** — n'a tiré
> **aucune** persistance (slots / périodes / transferts restent en mémoire). La
> **suppression d'acteur** a opéré, elle, sur la config foyer **déjà durable**
> (palier 5) : elle n'a **pas** créé de cliquet, elle a **exercé** une persistance
> acquise. L'**impersonation bornée** (livrée) n'a tiré **aucune persistance
> neuve** : l'état d'incarnation est **session / mémoire**, rien ne subsiste au
> redémarrage.

> **Corollaire de découpe.** Quand le périmètre d'un sujet déborde, on coupe au
> **plus petit incrément** qui rend la grille lisible et utilisable : on
> séquence, on ne reporte jamais tout en bloc. C'est ce corollaire qui a justifié
> de livrer d'abord un calendrier en lecture seule avant d'y brancher l'écriture,
> de borner la séparation de l'hôte d'API au plus petit pas qui rend le back
> démarrable seul, de tenir la lisibilité (nom + légende) comme observable avant
> le thème de surface, de livrer l'**édition** des acteurs avant leur **ajout** et
> leur persistance, de livrer un **cycle de fond** (parité ISO en mémoire) avant
> d'en enrichir l'ergonomie, et — au palier « écriture en contexte » — de livrer
> **trois dialogs** en deux temps (Poser un slot + Affecter une période, **puis**
> Définir un transfert) plutôt qu'en bloc. Il a guidé le palier acteurs : **CRUD
> acteurs** s'est découpé en **suppression d'abord (livrée)** puis **impersonation
> bornée lecture (livrée)**. Il **oriente** le prochain sujet, **Calendrier
> navigable** (palier 9, gardé **groupé**) : son plus petit incrément probable est
> la **navigation seule**, la **sélection de plage de cases** étant un sujet plein
> **cuttable en tranche 2** si elle déborde ~2h — périmètre exact **tranché au
> make-gherkin**, non pré-arbitré ici. Il reste le **garde-fou de secours** des
> sujets pris en bloc.

> **Révisions de règle hors boucle.** Une demande qui contredit une règle déjà
> actée n'est **pas** un correctif : c'est une révision de spec, qui n'entre pas
> dans le séquencement courant et attend le palier qui la porte. Trois telles
> demandes sont en attente (cf. Risques & questions ouvertes) : le workflow
> demande/accord avant réaffectation (palier « imprévu & échange »),
> l'interdiction/dédoublonnage de la pose répétée d'un même slot, et le **choix
> explicite d'une ancre/d'un début de cycle** (option « date d'ancrage » jadis
> écartée au profit de l'ancrage ISO sans ancre), qui sera **ré-arbitré au
> make-gherkin du palier « cycle de fond riche »**. *(Le durcissement du gating de
> l'écran de config, jadis quatrième révision en attente, a été **consommé** au
> palier 8 / impersonation : toutes les écritures config sont désormais gatées sur
> l'identité effective — il n'est plus en attente.)*

> **Prochain sujet : calendrier navigable (palier 9).** La tranche acteurs étant
> close (CRUD complet + impersonation bornée lecture livrés), l'usage tire le
> **calendrier navigable** : faire du hub `/planning` un agenda où l'on se déplace
> dans le **passé et le futur**, avec des **vues prédéfinies** (semaine, mois,
> 4 semaines glissantes) et une **amorce de sélection de plage de cases** pour
> définir une période sur l'intervalle. Besoin **ancien** (retours s02 / s03), au
> **rang +2** du backlog, tranché en **porte G2** par le PO : suite d'usage
> naturelle après la tranche acteurs. **Orientation de découpe (pas une règle)** :
> (a) le plus petit incrément probable est la **navigation seule** (semaines
> précédente / suivante, ou bascule de vue) ; (b) la **sélection de plage** est un
> **sujet plein cuttable en tranche 2** si elle déborde la borne ~2h ; (c) le
> **périmètre exact** est **tranché au make-gherkin**, **non pré-arbitré ici**
> (conforme au corollaire de découpe). Le palier reste **groupé** dans la séquence
> (pas de découpe 9a/9b actée en spec). **Aucune persistance neuve** n'est tirée
> en avant. Sujet `/2-make-gherkin` = `calendrier-navigable`. C'est ce sujet qui
> réamorce `/2-make-gherkin` sur cette spec.

## Séquence de livraison

Tous les besoins comptent, mais ils sont **séquencés** (pas livrés en bloc).
Chaque palier doit être adopté avant le suivant ; chacun se borne au plus petit
pas qui apporte une valeur lisible. Le palier de fondations a ouvert la séquence
au titre de l'exception bornée ; il est **refermé**, et les paliers d'usage
« saisie visible », « lisibilité & thème », « édition des acteurs », « config
foyer persistante », « récurrence des périodes », « écriture en contexte
(dialogs, transfert inclus) », « suppression d'acteur » **et « impersonation
bornée lecture »** qui l'ont suivi sont **livrés**. L'arbitre d'usage tient
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
   a été livrée au palier 8 « CRUD acteurs — suppression »), **sans** édition du
   cycle de fond ici.*

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

7. **Écriture en contexte — dialogs depuis le planning** *(reprise produit —
   **LIVRÉ COMPLET, épic refermé**)*. La saisie **se déplace là où on lit** : un
   **clic sur une case** ouvre un **menu d'actions** à **trois entrées** dont
   chacune ouvre une **dialog** pré-remplie sur la **date de la case**, alimentée
   par les acteurs et lieux du foyer. **Trois dialogs livrées** — **Poser un
   slot**, **Affecter une période** et **Définir un transfert** — et **tous les
   écrans/routes dédiés** correspondants (slot, période **et** la dernière page
   `definir-transfert`) **retirés** : il n'existe plus qu'**un seul chemin
   d'écriture** et **plus aucun écran de saisie dédié ne subsiste**. Issues :
   **succès** → dialog fermée + grille relue (transfert : **accusé « Transfert
   défini »** à part, non bloquant) ; **échec** (refus domaine **ou** API
   injoignable) → dialog **reste ouverte**, message **dans la dialog**, saisie
   **conservée**, grille **inchangée** ; **chevauchement** (slot) → écriture
   **aboutie**, dialog fermée, **avertissement non bloquant** affiché **à part**.
   Le **déclencheur** (menu) est **gaté** : seuls les Parents l'ouvrent
   (consultation seule des Invités préservée). *Observable = le **déplacement de
   la saisie en contexte**, pas une règle de gestion neuve ; réutilise les
   commandes / handlers (`PoserSlot`, `AffecterPeriode`, **`DefinirTransfert`**)
   et le canal requête/réponse déjà livrés (**pas de handler neuf**), la
   réapparition immédiate dans la grille et la diffusion SignalR lecture seule ;
   aucune persistance tirée en avant (slots / périodes / **transferts** restent en
   mémoire).* **Épic « écriture en contexte » refermé** ; il n'y a plus de
   reliquat.

8. **CRUD acteurs (complet) & impersonation bornée lecture** *(reprise d'usage —
   **LIVRÉ**, tiré devant le calendrier navigable)*. Le cycle de vie des acteurs
   est **complet** : Create + Read + Update **et Delete** (épic **É2**), plus le
   **dernier maillon — impersonation bornée lecture seule** (épic **É10**). La
   **suppression autorisée** **cadre les cases orphelines par neutralisation par
   repli** : la **surcharge orpheline cesse de primer**, la case retombe sur le
   **fond** (le cycle reprend, cf. priorité surcharge > fond > neutre) ou sur le
   **neutre** si l'index n'est ni mappé ni résolu (sans nom fantôme) ; si l'acteur
   supprimé était **mappé au cycle de fond**, son index devient **non mappé →
   neutre**. Un **accusé non bloquant** (« Acteur supprimé ») accompagne l'acte ;
   **pas de réaffectation automatique** (règle 6). La config foyer étant
   **persistée Mongo** (palier 5), la suppression a touché un **store réel** →
   **acceptation runtime tenue**. L'**impersonation bornée lecture** distingue une
   **identité réelle** (le configurateur, type Parent) d'une **identité
   effective** (l'acteur incarné, ou **repli sur la réelle**) : `Incarner(acteurId)`
   lit le référentiel (**refus silencieux si absent**, identité réelle conservée),
   un **bandeau « Vous incarnez X »** signale l'incarnation, le **droit d'écriture
   dérive du type de l'identité effective** (Parent/Admin → écriture visible,
   Autre → écritures masquées, grille **et** écran de config), et
   `RevenirIdentiteReelle()` restaure l'état. La **suppression concurrente** de
   l'acteur incarné **replie automatiquement** l'identité effective sur la réelle
   (bandeau retiré), en **temps réel** (SignalR). Le **type d'acteur** est surfacé
   en **lecture seule** depuis le seed (extension read-only de l'énumération
   acteurs ; acteurs ajoutés typés **Parent** par défaut, aucune saisie de type).
   **Bornes tenues** : **pas d'écriture « au nom de »** (commandes sous identité
   réelle, canal requête/réponse inchangé), **aucun port/handler d'écriture neuf**,
   **zéro persistance neuve** (état d'incarnation session / mémoire — borne
   anti-cliquet règle 30), et **PAS** l'authentification réelle du palier 16. *Ce
   palier est **clos côté usage** ; le prochain sujet est le calendrier navigable
   (palier 9).*

9. **Calendrier navigable — navigation, vues prédéfinies & sélection de plage**
   *(reprise produit — **NON LIVRÉ**, **PROCHAIN SUJET**)*. Faire du hub
   `/planning` un **agenda navigable** : se déplacer dans le **passé et le futur**,
   choisir des **vues prédéfinies** (semaine, mois, **4 semaines glissantes** par
   défaut à partir de la semaine en cours), et **sélectionner une plage de cases**
   pour affecter une période sur l'**intervalle** choisi (l'affectation par plage
   rouvre l'écriture en contexte sur plusieurs jours d'un coup). *La grille
   actuelle est une **vue posée non encore navigable** ; ce palier l'enrichit sans
   toucher aux mécaniques d'écriture déjà livrées (dialogs). Aucune persistance
   tirée en avant. **Orientation de découpe (pas une règle, palier gardé
   groupé)** : le plus petit incrément probable est la **navigation seule**
   (semaines préc. / suiv., bascule de vue) ; la **sélection de plage** est un
   sujet plein **cuttable en tranche 2** si elle déborde ~2h ; **périmètre exact
   tranché au make-gherkin**, non pré-arbitré ici (corollaire de découpe).*

10. **Cycle de fond riche — ancre, frontière de jour, plages & sur-cycles**
    *(reprise d'usage, sujet plein à découper)*. Enrichir le cycle de fond livré
    (palier 6) pour le rendre **réellement utilisable au quotidien** : **(a)**
    choisir explicitement le **début / l'ancre** du cycle (quelle semaine =
    index 0), **(b)** une **frontière de jour paramétrable** (ex. vendredi →
    vendredi), **(c)** une **plage de validité** début **et** fin, **(d)** un
    **sur-cycle / exception saisonnier** (vacances), **(e)** un cycle **WE-only**
    (1 week-end sur 2). *Ce palier **rouvre explicitement** la décision actée
    « ancrage ISO sans ancre » — le choix d'un début/d'une phase **est** l'option
    « date d'ancrage » jadis écartée : elle est **ré-arbitrée au make-gherkin de ce
    palier**, pas avant (révision de règle hors boucle). Besoin gardé **groupé** ;
    sa **découpe est impérative** au cadrage (corollaire de découpe). La plage
    début/fin et les sur-cycles **chevauchent la durabilité du cycle** (palier
    « config foyer durable — reste ») : n'enrichir que l'**observable**, ne PAS
    tirer Mongo par précaution (borne anti-cliquet).*

11. **Survol → résumé de la journée** *(reprise d'usage, évolution — séquencée,
    skippée faute de demande PO)*. Au survol prolongé (~1 s) d'une case, afficher
    un **résumé de la journée**, au-delà du seul nom complet déjà rendu au survol
    simple. *Enrichissement, pas une réparation : le survol simple (nom complet)
    est livré et conforme. Le périmètre du résumé (périodes / slots / responsable
    / transferts) est à cadrer au make-gherkin et ne doit pas être sous-estimé.
    Skippé tant que le PO ne le réclame pas ; séquencé, pas écarté.*

12. **Config foyer durable — reste de la config** — **persister** la part
    restante de la configuration du foyer (lieux, set de couleurs par défaut,
    **cycle de fond**) plutôt que de la porter en données figées dans le code.
    *Le **volet acteurs** de la config durable a déjà été livré au palier 5
    (premier client du store réel) ; ce palier complète la config durable pour les
    lieux, couleurs et **cycle de fond** (aujourd'hui en mémoire).*

13. **Modèle d'acteurs & foyer** — déclarer les acteurs réels (Admin, Parents,
    Autres) dans un écran de configuration, qui porte aussi la **responsabilité
    récurrente de fond** (le cycle) et le **set de couleurs par défaut**.
    *Exploite la config persistée des paliers précédents ; prérequis de
    l'ouverture de l'accès.*

14. **Immédiat & événements à venir** — « qui récupère ce soir », où est l'enfant
    maintenant, transferts et changements à venir présentés comme événements dans
    un **panneau cloche**, plus comme un tableau permanent. *Valeur quotidienne ;
    expose enfin les transferts, aujourd'hui invisibles par construction.*

15. **Imprévu & échange** — enfant malade / retard / échange de dernière minute,
    transferts **dérivés automatiquement** par défaut et saisie réservée à
    l'exception. *Le plus délicat ; après que l'usage à deux est acquis. Porte la
    question ouverte du **workflow demande/accord** avant réaffectation d'une
    période à l'autre parent.*

16. **Ouverture de l'accès** — landing page et authentification des acteurs réels
    (email via Gmail / Apple / Microsoft) pour lever le risque mortel d'adoption.
    Débloque aussi la **personnalisation des couleurs par utilisateur** et la
    **persistance d'une préférence de thème** (thème sombre), et **transforme
    l'impersonation bornée** (palier 8) en accès réel par acteur. *Vient après le
    socle et le modèle d'acteurs ; à ne pas laisser glisser indéfiniment.*

17. **Persistance réelle — adaptateurs de droite (reste du domaine)** *(palier
    technique, derrière l'usage)* — remplacer les dépôts **en mémoire** (volatils)
    des **slots, périodes et transferts** par des **adaptateurs de droite** vers
    un store **durable**, derrière les ports existants, sans toucher au domaine.
    *Débloqué par la fondation (palier 1) mais **subordonné à l'usage**. La
    **config foyer** en a été le **premier client**, déjà rendu durable au palier
    5 (exception bornée) ; ce palier étend la durabilité au reste du domaine.
    **Borne anti-cliquet** : il ne remonte pas devant l'usage.*

18. **Saisie hors-ligne (PWA)** *(palier technique, derrière l'usage)* — au-delà
    de l'**échec clair** déjà livré (palier 1), mettre en cache et **mettre en
    file** les écritures faites hors connexion, **rejouées au retour du réseau**.
    *Subordonné à l'usage. Piste consignée : une **file d'écritures côté client**
    (type *outbox*, IndexedDB) garantissant un rejeu « exactement une fois » comme
    socle minimal ; l'*event sourcing* n'est retenu que si offline / rejeu / audit
    le justifient — à trancher au moment d'ouvrir ce palier, pas un prérequis.*

> **Re-séquencement acté.** Le **palier 8** est désormais **clos côté usage** :
> la **tranche suppression** (`crud-acteurs-suppression`) **et** la **tranche
> impersonation bornée lecture** (`impersonation-bornee`) sont **livrées** (CRUD
> acteurs complet ; incarner un acteur déclaré avec bandeau, vue selon le rôle
> effectif, retour identité réelle, retour auto sur suppression concurrente,
> durcissement complet du gating config — 6/6 scénarios verts, suite complète
> 214/214, sur **store Mongo réel** et **app câblée / G3**, **zéro persistance
> neuve**). Le **prochain sujet** est le **calendrier navigable** (palier 9,
> **non livré** : navigation passé/futur, vues prédéfinies, amorce de sélection de
> plage), tiré par priorité d'usage devant le **rétrofit déterministe des tests
> temps-réel SignalR** (dette de test), l'**édition concurrente du même jour sous
> dialog** (cas limite, dépend du rétrofit) et l'**impersonation écriture « au nom
> de »** (hors-cap : franchit la borne dure du palier 8, exige une décision PO
> explicite). Derrière le calendrier vient le **cycle de fond riche** (palier 10,
> qui rouvre l'ancrage ISO à son cadrage). Le **survol enrichi** (palier 11) reste
> **skippé** faute de demande PO, séquencé sans être écarté. La persistance du
> **reste du domaine** reste en queue (palier 17), derrière tout l'usage — seule
> la persistance de la config foyer a été tirée devant (borne).

> **Numérotation — v15 référence unique.** La numérotation des paliers est
> **inchangée depuis la v13** (le swap palier 8/9 — tranche acteurs devant
> calendrier navigable — a déjà été acté et répercuté au `docs/BACKLOG.md`). Le
> **palier 8** est désormais **clos côté usage** (suppression **et** impersonation
> bornée lecture livrées) ; les paliers suivants (calendrier navigable 9, cycle de
> fond riche 10, survol enrichi 11, config foyer durable 12, modèle d'acteurs 13,
> immédiat & événements 14, imprévu & échange 15, ouverture de l'accès 16,
> persistance réelle 17, PWA 18) restent inchangés. La séquence ci-dessus est la
> **référence unique** et continue.

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

> **Prochain sujet** : **calendrier navigable** (`calendrier-navigable`,
> palier 9, épics É4 + É7) — faire de `/planning` un **agenda navigable**
> (déplacement passé/futur, vues prédéfinies semaine / mois / 4 semaines
> glissantes, **amorce de sélection de plage de cases** pour définir une période),
> **sans persistance neuve**. **Orientation de découpe (pas une règle, palier
> groupé)** : plus petit incrément probable = navigation seule ; sélection de
> plage = sujet plein cuttable en tranche 2 si débordement ~2h ; périmètre exact
> **tranché au make-gherkin**. C'est ce sujet qui réamorce `/2-make-gherkin` sur
> cette spec.

## Mécaniques de base

- Un créneau de garde a un horaire (début → fin), un responsable unique, un lieu et une activité
- Une journée est une suite de créneaux enchaînés
- Le planning suit un **cycle de fond** de plusieurs semaines qui se répète automatiquement : il compte **N semaines** (N ≥ 1) et alterne par **parité de la semaine ISO** (`index = semaine ISO du jour, modulo N`) ; chaque index est mappé sur un **responsable de fond**. La responsabilité d'une case se résout par **priorité** : **surcharge (période explicitement saisie) > fond (cycle) > neutre** — une période saisie prime toujours sur le cycle, qui reprend ensuite ; un index de cycle sans responsable retombe sur la **teinte neutre** sans nom. *Quand un acteur est **supprimé**, sa surcharge orpheline cesse de primer et la case retombe sur ce même fond (ou le neutre) ; si l'acteur était mappé au cycle, son index devient non mappé → neutre (cf. règle 6, livrée).*
- Le hub `/planning` est une grille agenda où les slots sont positionnés dans les cases jour/horaire. Il a vocation à devenir un **calendrier navigable** façon agenda — déplacement dans le **passé et le futur**, **vues prédéfinies** (semaine, mois, 4 semaines glissantes), **fenêtre par défaut** = 4 semaines glissantes à partir de la semaine en cours, et **sélection d'une plage de cases** pour définir une période sur l'intervalle. *Cette navigation est le **prochain sujet** (palier « Calendrier navigable », palier 9, **non encore livré**) : la grille actuelle est une vue posée. L'**écriture en contexte** par dialogs, elle, est **acquise et complète** (slot, période et transfert ; cf. ci-dessous).*
- La responsabilité de chaque garde se lit par un **code couleur par personne**, **doublé du nom du responsable affiché dans la case et d'une légende** : la couleur seule ne suffit pas à identifier qui garde. La **légende** agrège les responsables présents dans la fenêtre, **y compris les responsables de fond** issus du cycle, dédoublonnés par identifiant. Un nom trop long est **tronqué** dans la case, son **intitulé complet restant lisible au survol** ; un acteur hors du set de couleurs connu reste **affiché et distingué** (teinte neutre assumée) sans perdre son nom. La couleur d'un responsable se résout sur un **identifiant d'acteur stable**, jamais sur son libellé d'affichage : les sélecteurs de saisie **et le mapping du cycle de fond** fournissent ce même identifiant que la palette, sinon la case retombe sur la couleur neutre
- L'app porte un **thème en accord avec son domaine** (garde d'enfants), au service de la lisibilité d'usage ; c'est une ergonomie de surface, subordonnée à l'usage
- **L'écriture se fait en contexte, depuis la grille** : un **clic sur une case** ouvre un **menu d'actions** dont chaque entrée ouvre une **dialog** pré-remplie sur la **date de la case** et alimentée par les acteurs et lieux du foyer. **Trois dialogs sont livrées** (**Poser un slot**, **Affecter une période**, **Définir un transfert**) ; **tous les écrans de saisie dédiés** (slot, période **et transfert**) ont été **retirés** : il n'existe plus qu'**un seul chemin d'écriture** et **plus aucun écran de saisie dédié ne subsiste**. La dialog suit l'issue de la commande (succès → fermeture + grille relue, et pour le transfert un **accusé « Transfert défini » à part, non bloquant** ; échec → reste ouverte avec message et saisie conservée ; chevauchement → fermeture + avertissement non bloquant à part)
- La **date pré-remplie** d'une dialog est celle de la **case cliquée** : cet **ancrage de contexte prime** sur le défaut « aujourd'hui » de l'horloge. Le défaut horloge ne sert que **hors-contexte** ; tant que toute saisie passe par une case, il n'est pas exercé (cf. règle 17)
- La grille est en **lecture seule** : elle consomme les slots et périodes déjà enregistrés **et le cycle de fond résolu**, sans écriture. Toute écriture (poser un slot, affecter, surcharger ou supprimer une période, **définir un transfert**) se fait en contexte via des **dialogs** ouvertes depuis une case ; **annuler** une dialog n'émet aucune commande et laisse la grille inchangée. La **sélection d'une plage de cases** (palier « Calendrier navigable », non encore livré) ouvrira l'affectation d'une période sur l'intervalle choisi
- Les **acteurs du foyer (noms et couleurs) sont éditables** depuis un écran de configuration : renommer ou recolorier un acteur **met immédiatement à jour la grille** (case **et** légende) qui relit la configuration. On peut aussi **ajouter** un acteur (parent / autre / nounou) : l'ajout génère un **identifiant d'acteur stable neuf** (jamais le libellé) et la grille le reflète aussitôt (case + légende dédoublonnée par identifiant). La **suppression** d'un acteur est **livrée** (palier 8 « CRUD acteurs — suppression ») : elle **retire l'acteur du store** (config foyer persistée) et **neutralise les cases orphelines** par repli (cf. règle 6). La configuration du foyer ainsi éditée **survit au redémarrage** : elle est **persistée** derrière les **ports de droite** par leur adaptateur durable. Cette durabilité est **bornée à la config foyer** (référentiel des acteurs : noms, couleurs, acteurs ajoutés) ; le reste du domaine (slots, périodes, transferts, **cycle de fond**) reste en mémoire le temps de son propre palier de persistance
- **Toutes les écritures de l'écran de configuration** (édition d'acteur, ajout d'acteur, édition du cycle de fond, suppression d'acteur) sont **gatées sur l'identité effective** : un acteur « Autre » incarné (ou réel) ne les voit pas, un Parent / Admin oui. Le gating n'est plus restreint au seul bouton supprimer ; il est **complet** (cf. règle 9, durcissement livré)
- La **responsabilité récurrente de fond** (qui garde selon le cycle) se déclare dans la configuration du foyer (section « Cycle de fond » : nombre de semaines + un sélecteur de responsable par index, alimenté par les **acteurs persistés** du foyer, sur leur identifiant stable). Le calendrier ne porte que les **surcharges ponctuelles** d'une période, qui priment sur le fond. Définir **zéro semaine** est refusé ; toute ré-édition du mapping met la grille à jour **sans rechargement** (diffusion temps réel), et sur édition concurrente la **dernière écriture gagne**
- **Le front est découplé du back par une API** : il ne manipule pas le domaine en direct, il **émet ses commandes et lit ses données à travers l'API**, ce qui ouvre l'app à d'autres clients (front navigateur, IHM tierce, agents). L'**hôte d'API est détaché** : le back démarre seul, sans référencer le front, et le front — exécuté **dans le navigateur** (WebAssembly) — consomme une **API distante**
- **Toute écriture passe par le canal requête/réponse** : aucune vue n'écrit le domaine en direct. Une saisie qui contournerait le canal (appel direct du back) est une dette à résorber, pas un mode de fonctionnement
- **Deux canaux distincts, jamais confondus** : l'**écriture** est confiée à un **canal requête/réponse** (la commande part, la réponse confirme l'effet **et porte l'issue de sa propre écriture** — p. ex. un avertissement de chevauchement) ; la **diffusion temps réel** vers les autres acteurs (notifications, actualisation du planning à l'écran) passe par un **canal de diffusion en lecture seule**. On **n'écrit jamais** par le canal de diffusion : une saisie se propage aux autres écrans parce que l'écriture, une fois aboutie, **déclenche** la diffusion
- **Échec clair si l'API distante est injoignable** : la commande non aboutie produit un message à l'écran et la saisie **n'est pas appliquée** ni perdue de vue (elle reste à resoumettre ; dans une dialog, celle-ci reste ouverte avec la saisie conservée). Aucune mise en file ni rejeu à ce stade — le hors-ligne rejouable est un palier technique ultérieur
- L'**API est explorable** : elle expose un **document de description** (OpenAPI) et une **UI interactive** pour essayer les endpoints (type Swagger-UI / Scalar), et autorise l'**origine du front** par sa configuration CORS
- La configuration du foyer est une **donnée éditable** : pour le **référentiel des acteurs** (noms, couleurs, acteurs ajoutés) elle est **éditable ET durable** — elle vit derrière les **ports de droite**, dont l'adaptateur durable la persiste et la fait survivre au redémarrage. Pour le **reste** (lieux, set de couleurs par défaut, **cycle de fond**) elle est **éditable en mémoire** dès maintenant, et **durable à terme** une fois son adaptateur posé. Dans tous les cas, c'est une donnée derrière les ports, jamais une constante figée dans le code
- Les transferts et changements à venir sont présentés comme des événements dans un panneau de notifications (cloche), pas comme un tableau permanent
- **Trois types d'acteurs : Admin, Parent, Autre** — chacun avec un affichage adapté à son type. Le **type** d'un acteur est surfacé en **lecture seule** depuis la déclaration seed du foyer (extension read-only de l'énumération des acteurs ; les acteurs **ajoutés en session sont typés « Parent » par défaut**, aucune saisie de type). Ce type est **lu** par l'**identité effective** pour piloter le droit d'écriture (cf. règles 8 et 9) ; il ne s'**écrit** pas (aucun port/handler d'écriture neuf)
- **Impersonation bornée lecture** : tant que les acteurs ne sont pas authentifiés (palier 16), l'utilisateur principal peut **incarner un acteur déjà déclaré** du foyer. La **session** distingue une **identité réelle** (le configurateur, fixe) d'une **identité effective** (l'acteur incarné, ou **repli sur la réelle**) ; un **bandeau « Vous incarnez X »** signale l'incarnation, la vue **reflète le rôle de l'identité effective** et le **retour à l'identité réelle** restaure l'état. Aucune **écriture « au nom de »** : les commandes restent émises sous l'**identité réelle**. L'état d'incarnation vit en **session / mémoire** (aucune persistance). C'est une **convenance d'administration**, **pas** l'authentification réelle (cf. règle 8)
- La composition du foyer (enfants, parents, acteurs autres) se déclare dans un écran de configuration distinct du planning

## Règles de gestion

### Foyer & acteurs

1. **Multi-enfants** — Un foyer compte au moins un enfant, et peut en compter plusieurs, chacun avec sa propre organisation de garde

2. **Familles recomposées** — Un foyer peut mélanger des enfants de parents différents, tous visibles dans le même planning

3. **Toujours deux parents** — Un foyer a toujours exactement deux parents ; tant qu'un seul est inscrit, il peut saisir les informations de l'autre

4. **Acteurs « autres » ajoutables, éditables et supprimables** — Un foyer peut avoir N acteurs « autres » (nounou, grands-parents…). Ils sont **ajoutables, éditables et supprimables** (par les parents ou par l'acteur lui-même), au-delà du seul renommage/recoloriage du seed initial : ajouter un acteur le fait exister dans le foyer et dans la grille, **supprimer** un acteur l'en retire et **neutralise ses cases orphelines** par repli (cf. règle 6, **livrée** — palier 8 « CRUD acteurs — suppression »)

5. **Édition des acteurs (noms + couleurs)** — Les acteurs du foyer (leurs **noms** et leurs **couleurs**) sont **éditables** depuis un écran de configuration, et la **grille (case + légende) relit immédiatement** la configuration éditée : renommer ou recolorier un acteur se voit aussitôt, **partout où l'acteur apparaît** (case, légende, sélecteurs de saisie et mapping du cycle de fond doivent lire le même référentiel vivant). Cette édition s'est livrée d'abord **en mémoire, dans la session** ; sa **durabilité** (survie au redémarrage) est portée par la règle 6 et la règle 30. La survie au redémarrage **est acquise pour la config foyer** : la dette volatile de l'édition s'éteint là, ailleurs elle subsiste (cf. règle 30). *Tous les sélecteurs de l'écran de config — y compris le sélecteur « Acteur du foyer » — lisent désormais le **store vivant** des acteurs (et non plus une liste statique) : un acteur renommé, ajouté ou supprimé est aussitôt cohérent **partout** (case, légende, sélecteurs, mapping du cycle de fond). Le **type** d'acteur y est surfacé en **lecture seule** (depuis le seed), lu par l'identité effective (cf. règle 8), jamais saisi.*

6. **Ajout & suppression d'acteur, neutralisation par repli (cases ET incarnation), persistance bornée** — On peut **ajouter** un acteur au foyer (parent / autre / nounou) : l'ajout génère un **identifiant d'acteur stable neuf** (jamais dérivé du libellé d'affichage) et la grille (case + **légende dédoublonnée par identifiant**) le reflète immédiatement. La **configuration du foyer** — référentiel des acteurs : **noms, couleurs, acteurs ajoutés** — est **persistée** derrière les ports de droite par un **adaptateur durable** : elle **survit au redémarrage**. Cette persistance est **bornée à la config foyer** : c'est le **premier client** du store durable, tiré **devant l'usage** parce qu'il porte un observable direct. La **suppression d'un acteur** (Delete) est **livrée** (palier 8) et **autorisée** — pas de refus « si références existantes » (qui contredirait l'additivité et le repli neutre). Les **cases orphelines** de l'acteur retiré sont **neutralisées par repli** : sa **surcharge cesse de primer** et la case **retombe sur le fond** (le cycle reprend, cf. règles 12 et 15) ou sur le **neutre** si l'index n'est ni mappé ni résolu, **sans nom fantôme** (règles 15/19) ; si l'acteur supprimé était **mappé au cycle de fond**, son index devient **non mappé → neutre** (règles 11/19). **Extension du repli à l'incarnation (livrée — palier 8)** : si l'acteur supprimé est **incarné** (identité effective courante, cf. règle 8), l'identité effective **retombe automatiquement** sur l'**identité réelle** (bandeau retiré), en **temps réel** (diffusion SignalR) — même logique de neutralisation par repli, appliquée à l'incarnation. La suppression s'accompagne d'un **accusé non bloquant** (« Acteur supprimé », registre avertissement-à-part, règles 16/28). La suppression est **idempotente** : un identifiant absent ou déjà supprimé est un **no-op qui réussit** (jamais un refus). Il n'y a **pas de réaffectation automatique** (ce serait une règle neuve, hors périmètre). La suppression a opéré sur la config foyer **déjà persistée Mongo** (palier 5) → **acceptation runtime tenue** (store réel : l'acteur retiré disparaît du store relu et après redémarrage). *Variantes refus/réaffectation = porte **G1 au make-gherkin uniquement** si un vrai trou émerge (ex. interdire la suppression du dernier responsable d'un enfant). Les transferts dérivés (règle 24) restent invisibles jusqu'au panneau cloche → aucun orphelin de transfert observable séparé ; un transfert ponctuel explicite (règle 25) suit la même neutralisation, à scénariser au make-gherkin (pas un pré-arbitrage de cette spec).* La persistance du **reste du domaine** reste **en queue** (règle 30, borne anti-cliquet) ; l'**état d'incarnation** ne persiste pas (session / mémoire)

7. **Responsabilité de fond en config, exception au calendrier** — La responsabilité récurrente de garde (le **cycle de fond** : qui garde par défaut) se déclare dans l'écran de configuration du foyer, en même temps que les acteurs ; elle est **éditable** (en mémoire d'abord, durable une fois son adaptateur posé — palier « config foyer durable — reste »). Le calendrier ne sert qu'aux **surcharges ponctuelles** d'une période donnée, qui **priment** sur le fond. Les dialogs d'affectation et de surcharge, comme le **sélecteur de responsable du cycle**, sont alimentés par les acteurs du foyer sur leur **identifiant stable**

### Rôles & accès

8. **Trois types d'acteurs & impersonation bornée lecture (livrée)** — Trois types d'accès : Admin (gère tout, y compris la configuration du foyer), Parent (gère les créneaux, lieux, acteurs et invitations de son foyer), Autre (consultation et édition limitée à ses propres informations). L'affichage s'adapte au type, surfacé en **lecture seule** depuis le seed (cf. Mécaniques). Tant que les acteurs ne sont pas des **utilisateurs réels** (authentification au palier « ouverture de l'accès », palier 16), une **impersonation bornée lecture seule** est **livrée** (épic É10, **2ᵉ tranche du palier 8**, après la tranche suppression) : la **session** distingue une **identité réelle** (le configurateur, fixe, type Parent) d'une **identité effective** (l'acteur **incarné**, ou **repli sur la réelle**). `Incarner(acteurId)` lit le référentiel — **refus silencieux si l'identifiant est absent**, identité réelle conservée — et `RevenirIdentiteReelle()` restaure l'état ; un **bandeau « Vous incarnez X »** signale l'incarnation. Le **droit d'écriture `EstParent` dérive du type de l'identité EFFECTIVE** (vrai si Parent ou Admin, faux si Autre — cf. règle 9), résolu sur l'**identifiant stable** de l'acteur (jamais le libellé, règles 5/19). **Bornes dures tenues** : c'est une **convenance d'administration**, **PAS** l'authentification complète (ni OAuth, ni landing, ni comptes, ni sessions, ni prise en main par demande, ni droits par rôle persistés — tout cela reste au palier 16) ; il n'y a **pas d'écriture « au nom de »** (les commandes restent émises sous l'**identité réelle**, canal requête/réponse inchangé — cf. règle 28) ; **aucune persistance neuve** (état d'incarnation **session / mémoire**, rien ne subsiste au redémarrage — règle 30). À la livraison du palier 16, l'impersonation se transforme en **accès réel par acteur**. *L'**impersonation écriture « au nom de »** (agir réellement sous l'identité incarnée) est **hors-cap** : elle franchit cette borne dure et amorce l'auth réelle — elle exige une **décision PO explicite** de changer le cap (cf. Risques).*

9. **Modification réservée aux parents et à l'admin, gating sur l'identité effective (durcissement config livré)** — Seul un Parent (ou l'Admin) peut créer, éditer ou supprimer un créneau, une période **ou le cycle de fond** ; un acteur « Autre » n'édite que ses propres informations. Depuis que l'écriture se fait **en contexte** (palier 7), le **point d'application** de ce droit est le **déclencheur unique** — le **menu ouvert au clic sur une case** — rendu **conditionnel sur le rôle de l'identité effective** (`@if EstParent`, dérivé de l'identité incarnée, cf. règle 8) : un « Autre » incarné (ou un Invité) ne voit pas le menu et n'ouvre aucune dialog, un Parent / Admin l'ouvre. Ce gating est **mutualisé** sur le déclencheur quelle que soit l'entrée (slot, période, **transfert**). **Durcissement de l'écran de config — livré (palier 8) :** **toutes** les écritures de l'écran `ConfigurationFoyer` — **édition d'acteur, ajout d'acteur, édition du cycle de fond, suppression d'acteur** — sont désormais **gatées sur l'identité effective** (`@if Session.EstParent`), et non plus le seul bouton supprimer : l'**angle mort** d'un Invité / « Autre » incarné voyant les écritures config (signalé Sc.7 au palier précédent) est **refermé**. Le gating **lit l'identité effective sans recalcul** et ne tire **ni authentification réelle ni écriture « au nom de »** (séquencées au palier 16)

### Planning & créneaux

10. **Responsable unique** — Un créneau a toujours un seul responsable (pas de « X ou Y »)

11. **Cycle de fond récurrent, éditable** — Le planning se répète selon un **cycle de fond** de **N semaines** (N ≥ 1) : l'index de la semaine est sa **parité ISO** (`index = semaine ISO du jour, modulo N`) et chaque index est mappé sur un **responsable de fond** résolu sur l'**identifiant stable** de l'acteur (jamais le libellé ; un index non mappé = pas de fond → teinte neutre). Ce cycle est **définissable et éditable** depuis la configuration du foyer (nombre de semaines + responsable par index, alimenté par les acteurs persistés) et **non figé dans le code** ; définir **zéro semaine** est **refusé** (« le cycle doit compter au moins une semaine »), le cycle précédent restant inchangé. La ré-édition du mapping met la grille à jour **sans rechargement** ; sur édition concurrente, la **dernière écriture gagne**. L'ouverture d'une **dialog d'écriture en contexte n'interfère pas** avec le rafraîchissement de fond (la grille se rafraîchit sous la dialog ouverte sans la fermer ni perdre la saisie). *La **suppression d'un acteur** mappé au cycle laisse son index **non mappé → neutre**, sans nom fantôme (cf. règle 6, livrée). Note (besoin différé) : l'**édition concurrente du MÊME jour** sous dialog ouverte — dernière-écriture-gagne à démontrer **sous dialog en contexte** — est séquencée derrière la stabilisation temps-réel (cf. Risques) ; aucune règle neuve. Note d'évolution (séquencée) : l'**ancre/le début explicite** du cycle, la **frontière de jour paramétrable**, les **plages de validité**, **sur-cycles saisonniers** et **cycles WE-only** sont un palier d'évolution (« cycle de fond riche », palier 10) ; choisir explicitement une ancre **rouvre la décision actée « ancrage ISO sans ancre »** et sera **tranché au make-gherkin de ce palier**, pas avant (révision de règle hors boucle)*

12. **Exception ponctuelle prime sur le fond** — Un jour précis peut être surchargé sans casser le cycle de fond : une **période explicitement saisie prime** sur le responsable de fond (priorité **surcharge > fond > neutre**), et le cycle **reprend ensuite** automatiquement autour de la surcharge. *Quand la surcharge devient **orpheline** (son acteur supprimé — cf. règle 6, livrée), elle cesse de primer et la case retombe sur le fond, ou le neutre (cf. règle 15).*

13. **Lieux éditables** — La liste des lieux (domicile A/B, école, nounou, activité…) est librement modifiable et sert de référentiel aux sélecteurs de saisie

14. **Grille en lecture seule, écriture en dialog contextuelle** — La grille agenda consomme les slots, périodes et **le cycle de fond résolu** déjà enregistrés et les rend (positionnés dans leur case, colorés par responsable, **nommés**) **sans jamais écrire**. Toute écriture passe par une **dialog ouverte depuis une case** (via le menu d'actions) : c'est le **seul chemin d'écriture** pour le slot, la période **et le transfert** (**tous les écrans dédiés retirés**, plus aucun écran de saisie dédié ne subsiste), et **annuler** une dialog n'émet **aucune commande** et laisse la grille inchangée. La **sélection d'une plage de cases** pour affecter une période sur l'intervalle est une capacité du palier « Calendrier navigable » (palier 9, **prochain sujet**, non encore livré)

15. **Suppression de période** — Un Parent (ou l'Admin) peut supprimer une période de garde ; c'est une action d'écriture menée depuis une dialog contextuelle, hors de la grille en lecture pure. Sous la période supprimée, le **cycle de fond reprend** (la case retombe sur son responsable de fond, ou sur le neutre si l'index n'est pas mappé). *Le même repli s'applique à une période rendue **orpheline** par la suppression de son acteur (cf. règle 6, livrée).*

16. **Pose répétée d'un même slot acceptée avec avertissement** — Un slot qui chevauche ou redouble un slot existant est **accepté**, accompagné d'un **avertissement** ; il n'est ni refusé ni dédoublonné. Côté écriture en contexte, l'issue est donc un **succès** : la dialog **se ferme**, le slot **réapparaît**, et l'**avertissement s'affiche à part** (bandeau/toast), **non bloquant**. Cet avertissement est **un acquis surfacé** (porté par l'**issue de la commande**, dans le contrat de réponse du canal poser-slot) — **jamais** recalculé depuis la grille relue, jamais une règle neuve. *L'**accusé « Acteur supprimé »** de la suppression d'acteur (règle 6) et l'**accusé « Transfert défini »** (règle 25) relèvent du même registre d'avertissement-à-part. (Une demande d'interdiction/dédoublonnage est en attente comme révision de règle — cf. Risques & questions ouvertes.)*

17. **Date de saisie par défaut = aujourd'hui, ancrage de contexte prioritaire** — Les formulaires de saisie (poser un slot, affecter ou surcharger une période, **définir un transfert**) pré-remplissent leur date sur la **date de référence « aujourd'hui »** **uniquement hors-contexte** ; **en contexte** (saisie ouverte depuis une case), la **date de la case cliquée prime** sur ce défaut. Dans les deux cas la date tombe **dans la fenêtre affichée** et la saisie **réapparaît immédiatement** dans la grille. Une date par défaut figée hors fenêtre est une non-conformité à corriger, pas un comportement attendu. *Garde-fou de mise en œuvre : tant que **toute** saisie passe par une case, le pré-remplissage est **exclusivement** la date de contexte et le **repli horloge devient du code mort** ; ce repli **ne doit pas être supprimé du port d'horloge** (la grille s'en sert pour « aujourd'hui »/fenêtre) et **doit être réintroduit dans la dialog** si un point d'entrée de saisie **hors-contexte** réapparaît à un futur palier.*

### Code couleur & lisibilité

18. **Responsabilité lisible : couleur par personne + nom + légende** — La responsabilité de garde se lit dans la grille par une couleur qui distingue chaque **personne** (Parent A ≠ Parent B), pas seulement son type de rôle ; cette couleur est **doublée du nom du responsable affiché dans la case et d'une légende**, car la teinte seule ne suffit pas à dire qui garde. La **légende** agrège les responsables présents dans la fenêtre **y compris les responsables de fond** issus du cycle, dédoublonnés par identifiant ; un acteur **supprimé** quitte aussitôt la légende (plus de pastille ni de nom fantôme). Un **nom trop long** est **tronqué** dans la case tout en restant lisible **en entier au survol** ; un acteur **hors du set de couleurs connu** reste **affiché et distingué** (teinte neutre assumée) **sans perdre son nom**

19. **Couleur résolue sur un identifiant d'acteur stable** — La couleur d'un responsable se résout sur l'**identifiant stable** de l'acteur, jamais sur son libellé d'affichage. Les sélecteurs de saisie (affectation, surcharge, **transfert**) **et le mapping du cycle de fond** **fournissent ce même identifiant** que la palette ; un acteur **ajouté** reçoit un identifiant stable neuf résolu de la même façon, et l'**identité effective** (incarnation, règle 8) se résout sur ce même identifiant. Un libellé qui ne correspond pas à un identifiant connu fait retomber la case sur la **couleur neutre** ; de même, un **index de cycle non mappé** (y compris après **suppression** de l'acteur qui y était mappé) = pas de fond → teinte neutre, **sans nom fantôme**. Une case grise là où un responsable est affecté trahit un libellé fourni à la place de l'identifiant — c'est le défaut à localiser, pas la résolution elle-même

20. **Set de couleurs par défaut, recoloriable** — Un jeu de couleurs par défaut, déclaré avec le foyer, différencie d'emblée les responsables tant qu'aucune personnalisation n'a été faite ; c'est ce set qui s'applique avant l'ouverture de l'accès. Ce set est **recoloriable** depuis l'écran de config et la grille suit ; la **personnalisation par utilisateur authentifié** (règle 21) reste un pas distinct lié à l'ouverture de l'accès

21. **Personnalisation des couleurs par utilisateur** — Une fois identifié, chaque acteur peut personnaliser les couleurs qu'il assigne aux acteurs qu'il voit ; cette personnalisation dépend de l'authentification (séquence, ouverture de l'accès) et n'altère pas le set par défaut vu par les autres

22. **Thème en accord avec le domaine** — L'app porte un **thème** cohérent avec son domaine (garde d'enfants), au service de la lisibilité d'usage. C'est une **ergonomie de surface subordonnée à l'usage** par l'arbitre : il n'ouvre aucune règle métier. Un **thème sombre avec bascule clair/sombre** (et persistance de la préférence) en est une **évolution additive**, consignée et non priorisée, à rattacher au futur écran de préférences utilisateur. L'**harmonisation de teinte** entre la pastille de légende et le fond de case (cf. Risques) relève du même registre d'ergonomie de surface, pas d'une règle métier

### Survol & détail de la case

23. **Survol : du nom complet au résumé de la journée** — Au survol d'une case, l'app expose un complément d'information sans quitter la grille. Le **survol simple** affiche le **nom complet** du responsable (utile quand le nom est tronqué) : c'est le comportement **livré et conforme**. Un **survol enrichi** est une **évolution** prévue (séquencée, skippée tant que le PO ne la réclame pas) : après un survol prolongé (~1 s), afficher un **résumé de la journée**. Le périmètre de ce résumé (périodes, slots, responsable, transferts) est à cadrer au moment de le scénariser ; ce n'est pas un correctif du survol simple, qui n'est pas défaillant

### Transferts

24. **Transfert dérivé par défaut** — Le transfert d'un enfant (qui dépose, qui récupère, à quelle heure) est dérivé automatiquement du planning par défaut : c'est le cas nominal, sans saisie

25. **Transfert modifiable et ponctuel, saisi en contexte** — Un transfert auto-calculé peut être modifié, et des transferts ponctuels peuvent être proposés ou ajoutés à l'exception (urgence, changement d'emploi du temps). La saisie d'un transfert se fait, comme le slot et la période, **en contexte** : la **3ᵉ dialog « Définir un transfert »** ouverte depuis une case (pré-remplie sur la date de la case) est **livrée**, et l'ancienne page de saisie dédiée a été **retirée** (épic « écriture en contexte » refermé). Au **succès**, un **accusé « Transfert défini »** s'affiche **à part, non bloquant** (registre avertissement, règle 16) ; au **refus domaine ou API injoignable**, la dialog **reste ouverte** avec message et saisie conservée (règle 28). Elle réutilise la **commande/handler `DefinirTransfert`** et le **canal HTTP** déjà livrés (**aucun handler neuf**) ; le transfert **reste InMemory** (règle 30, borne anti-cliquet)

### Modifications & notifications

26. **Modification directe** — Un Parent applique son changement immédiatement ; les autres acteurs concernés sont notifiés (pas de workflow de validation). (Une demande de workflow demande/accord avant réaffectation d'une période à l'autre parent est en attente comme révision de règle, rattachée au palier « imprévu & échange » — cf. Risques & questions ouvertes.)

27. **Notifications & événements à venir** — Les notifications in-app couvrent les changements de planning et les rappels de transfert ; les transferts et changements à venir sont consultables dans un panneau d'événements accessible via une cloche

### Accès aux données & exploitation

28. **Écriture par le canal, échec clair si l'API est injoignable** — Toute écriture passe par le **canal requête/réponse** vers l'API distante ; aucune vue n'écrit le domaine en direct, et **l'écriture est toujours émise sous l'identité réelle** (l'impersonation, règle 8, ne touche **pas** ce canal : pas d'écriture « au nom de »). La **réponse du canal porte l'issue de sa propre écriture** (succès, et le cas échéant un avertissement de chevauchement — cf. règle 16 ; surface autorisée = le **contrat de réponse** du canal, **sans** recalcul métier ni nouvel endpoint de lecture). Si l'API est **injoignable**, ou si le domaine **refuse** la commande, l'échec est **clair** : message à l'écran, saisie **non appliquée** et **conservée** à resoumettre — en contexte, la **dialog reste ouverte**, le message s'affiche **dans la dialog**, la **grille reste inchangée**, **sans mise en file ni rejeu** à ce stade. Cette issue d'échec vaut pour les **trois dialogs** (slot, période, transfert) **et pour la suppression d'acteur** (API injoignable → suppression non appliquée, acteur toujours listé, store réel inchangé, aucune fausse confirmation). Le hors-ligne rejouable (cache + file d'écritures) est un palier technique ultérieur, derrière l'usage

29. **API explorable et origine du front autorisée** — L'API expose un **document de description** (OpenAPI) **et** une **UI interactive** d'exploration des endpoints (type Swagger-UI / Scalar), et autorise l'**origine du front** (CORS). C'est un garde-fou d'outillage, sans observable métier : aucune règle de gestion n'en dépend

30. **Données derrière les ports, durables — exception bornée pour la config foyer** — Les slots, périodes, transferts, **cycle de fond** et la configuration du foyer vivent derrière des **ports** ; la donnée du foyer n'est jamais une constante figée dans le code. Leur stockage est durable **à terme**, sans toucher au domaine ni aux règles. **Exception bornée actée et livrée** : la **config foyer** (référentiel des acteurs — noms, couleurs, acteurs ajoutés) est **persistée maintenant**, **devant l'usage**, parce qu'elle porte un observable direct (survie au redémarrage) et qu'elle est le **premier client** du store durable. Le **reste du domaine** (slots, périodes, transferts **et le cycle de fond**) reste **en mémoire** (adaptateur InMemory), sa durabilité **séquencée derrière l'usage** : la **borne anti-cliquet** empêche que cette exception entraîne le reste devant l'usage. L'**écriture en contexte** (palier 7, **transfert compris**) l'a confirmée : déplacer la saisie en dialogs n'a tiré **aucune** persistance (slots / périodes / **transferts** restent InMemory). La **suppression d'acteur** (palier 8) a opéré sur la config foyer **déjà durable** — elle a **exercé** une persistance acquise, **sans créer de cliquet**. L'**impersonation bornée lecture** (palier 8) n'a tiré **aucune persistance neuve** : l'**état d'incarnation** vit en **session / mémoire**, rien ne subsiste au redémarrage. Cette règle porte la **durabilité** ; elle reste **distincte de l'édition** (règle 5) — rendre une donnée éditable (cf. le cycle de fond, éditable mais volatil) n'oblige pas à la rendre durable dans le même incrément, sauf quand, comme pour la config foyer, la durabilité porte un observable direct et reste bornée

## Risques & questions ouvertes

- **L'usage tient la main — ne pas enchaîner un sprint sans valeur produit.** Deux sprints structurels (fondation, hôte d'API) puis neuf sprints d'usage (saisie visible, lisibilité & thème, édition des acteurs, config foyer persistante, récurrence des périodes, écriture en contexte slot/période, écriture en contexte transfert, suppression d'acteur, **impersonation bornée lecture**) sont derrière nous. Les prochains sujets (calendrier navigable, cycle de fond riche, survol enrichi) restent des paliers **d'usage** ; les paliers techniques débloqués (persistance du reste du domaine, PWA, Docker) **et la dette de test** (rétrofit temps-réel SignalR) sont tentants mais **doivent rester derrière l'usage**. La seule persistance tirée devant a été **bornée à la config foyer** (observable direct) : ne pas en faire un cliquet.
- **Prochain sujet — calendrier navigable (`calendrier-navigable`, palier 9, épics É4 + É7).** Faire de `/planning` un **agenda navigable** : déplacement **passé/futur**, **vues prédéfinies** (semaine, mois, 4 semaines glissantes), **amorce de sélection de plage de cases** pour définir une période sur l'intervalle. Besoin **ancien** (retours s02 #3 navigation / s03), **rang +2** du backlog, tranché en **porte G2** par le PO. **Orientation de découpe (pas une règle, palier groupé)** : (a) plus petit incrément probable = **navigation seule** ; (b) **sélection de plage** = sujet plein **cuttable en tranche 2** si débordement ~2h ; (c) **périmètre exact tranché au make-gherkin**, non pré-arbitré (corollaire de découpe). **Aucune persistance neuve.** La grille actuelle est une vue posée non navigable : ne pas la sous-entendre acquise.
- **Impersonation bornée lecture livrée — frontière avec l'auth réelle (palier 16) et l'écriture « au nom de » (hors-cap).** L'impersonation bornée **lecture seule** est **livrée** (palier 8) : incarner un acteur déclaré (bandeau, vue selon le rôle effectif, retour identité réelle, retour auto sur suppression concurrente), **sans** écriture « au nom de », **sans** persistance neuve, **sans** auth réelle. La **borne dure tient** : ce n'est pas l'authentification du palier 16. L'**impersonation écriture « au nom de »** (agir réellement sous l'identité incarnée) est **hors-cap** : elle **franchit la borne dure du palier 8** (lecture seule) et **amorce l'auth réelle** (chemin d'écriture neuf, règle 30 anti-cliquet) — à ne tirer que sur **décision PO explicite de changer le cap** (candidat **G1**). À la livraison du palier 16, l'impersonation se transforme en accès réel par acteur.
- **Rétrofit déterministe des tests temps-réel SignalR (rang +3 — dette de test).** Aux paliers 8, la touche des composants config / grille partagés a exposé une **course latente** (`UnknownEventHandlerId`) dans des tests `*TempsReel*` interagissant avec un `select` sans garde d'énumération → un **garde déterministe `WaitForState`** a été posé localement, mais le **rétrofit reste à généraliser** (et la **convergence SignalR multi-clients** — distincte de la course d'énumération — n'est pas couverte ; flake observé ~1/30 sous charge). C'est une **dette de test sans observable métier** (vigilance « faux sentiment de progrès ») : à porter en **retro-sprint**, **derrière l'usage** (Calendrier navigable). C'est un **prérequis de fait** de l'édition concurrente (ci-dessous) : driver celle-ci sur une fondation instable produirait des scénarios **flaky par construction**.
- **Édition concurrente du même jour sous dialog ouverte (rang +4 — différée).** Prouver le comportement quand **deux acteurs éditent le même jour** alors qu'une dialog est ouverte — **dernière-écriture-gagne** (règle 11, acquise) à démontrer **sous dialog en contexte**. La caractérisation actuelle ne couvre que « le rafraîchissement de fond n'interfère pas avec une dialog ouverte », pas l'édition concurrente du même jour. **Dépend du rétrofit SignalR (rang +3)** : différée jusqu'à stabilisation de la fondation temps-réel ; aucune règle neuve.
- **Cycle de fond riche (palier 10) — sujet plein, deux frontières à surveiller.** (1) L'enrichissement **rouvre** la décision actée « ancrage ISO sans ancre » : choisir explicitement un début/une phase est l'option « date d'ancrage » jadis écartée, à **ré-arbitrer au make-gherkin de ce palier** (révision de règle hors boucle) — la règle 11 n'est **pas** révisée d'ici là. (2) Plage début/fin **+** sur-cycles vacances **chevauchent la durabilité du cycle** (palier « config foyer durable — reste ») : n'enrichir que l'**observable** de cycle, ne PAS tirer Mongo pour le cycle par précaution (borne anti-cliquet). Le besoin est gardé **groupé** ; sa **découpe est impérative** au cadrage. Risque spec « coût de saisie du cycle » exactement ici.
- **Coût de saisie du cycle** — Saisir un cycle multi-semaines est lourd ; le cycle de fond livré (parité ISO, mapping par index) en pose le socle, et le palier « cycle de fond riche » devra le rendre **supportable** sans le complexifier inutilement. Le transfert dérivé automatiquement réduit ce coût pour le cas nominal.
- **Acceptation runtime obligatoire (rempart anti vert-qui-ment).** Les incréments d'usage sont prouvés sur l'**app réellement câblée** (front WASM + API distante + SignalR + store réel), pas par doublures : le cycle de fond a été accepté en affichant le **fond résolu** sans saisie de période ; l'écriture en contexte (slot, période, **transfert**) en prouvant qu'une saisie **réellement enregistrée** via une dialog réapparaît **positionnée, colorée et nommée** à la **date de la case** ; la **suppression d'acteur** sur le **store Mongo réel** (l'acteur retiré disparaît du store **et** après redémarrage, cases retombées sur fond/neutre, légende dédoublonnée, gating Invité, échec API laissant le store inchangé, propagation SignalR) ; l'**impersonation bornée lecture** sur l'**app câblée / G3** (bandeau affiché, menu clic-case visible/masqué selon le rôle effectif, gating complet de l'écran config, **retour auto** sur suppression concurrente par diffusion temps réel). Ce rempart reste la règle pour les prochains incréments : le **calendrier navigable** se valide sur l'app câblée ; un test de composant à doublures peut afficher une grille alors que le câblage réel échoue.
- **Borne anti-cliquet à tracer sans déraper.** La persistance devant l'usage est une **exception bornée à la config foyer**. Risque de **cliquet** : que le reste du domaine (slots / périodes / transferts / **cycle de fond**) suive devant l'usage. Garder cette persistance **en queue** (paliers « config foyer durable — reste » puis « persistance réelle ») ; la borne est écrite noir sur blanc (règle 30, et `docs/BACKLOG.md`). L'écriture en contexte (transfert compris) l'a respectée ; la **suppression d'acteur** a exercé la persistance **déjà acquise** de la config foyer, sans l'étendre ; l'**impersonation bornée lecture** n'a tiré **aucune persistance neuve** (état session / mémoire). Le **calendrier navigable** ne doit en tirer aucune non plus.
- **Édition vs persistance — deux périmètres à ne pas confondre.** La config foyer est **durable** (référentiel des acteurs), mais le **reste du domaine reste volatile** : slots, périodes, transferts **et le cycle de fond** vivent encore en mémoire jusqu'à leur palier de persistance. Ne pas reprocher au reste de ne pas persister (c'est la découpe) ; ne pas tirer leur persistance en avant au prétexte que la config foyer est durable (c'est le cliquet).
- **Pilotage au catalogue — retours produit VIDE au sprint 14.** Le sprint 14 (impersonation bornée lecture) a été livré et validé au gate G3 **sans aucun retour d'usage déposé** (chemin nominal, goal 9/9 atteint, aucune anomalie, livraison verte 6/6 · 214/214) : la priorisation du prochain sujet (calendrier navigable) dérive du **backlog seul** (rang +2, G2 PO), sans signal d'usage frais. Point de vigilance : **confirmer le besoin réel au démarrage du sprint** ; ne pas enchaîner un sprint « à vide » sur du catalogue si un retour d'usage plus pressant émerge.
- **Périmètre « résumé de la journée » (survol enrichi) non défini** — périodes ? slots ? responsable ? transferts ? Sujet potentiellement plus gros qu'il n'y paraît, proche du « qui récupère ce soir » (palier immédiat). À **cadrer au make-gherkin** quand le survol sera pris ; ne pas le sous-estimer comme « simple tooltip ». Le survol simple (nom complet) est **conforme et accepté** : rien de cassé, le résumé est un comportement **neuf**. Skippé ce cycle faute de demande PO.
- **Légende ≠ bug (non-bug, harmonisation de teinte).** Le ressenti « les couleurs de la légende ne sont pas celles des acteurs » a été **confronté au code courant** : la légende et la case-jour résolvent le **même token couleur sur le même singleton**. **Aucun défaut de résolution.** L'écart vient d'une **incohérence de teinte de présentation** : pastille de légende **saturée** vs fond de case **pâle** (choix de design : fond pâle = texte sombre lisible). C'est une **évolution de teinte**, **jamais** un fix ciblé. À regrouper avec l'ergonomie config (palette/picker de couleur) quand elle remontera, pas un sujet seul.
- **Évolutions de surface non priorisées seules** — **sélecteur de couleur (palette / picker)** dans l'écran de config (au lieu d'une saisie libre) ; **onglets** de config par acteur (faible conviction PO : « un seul foyer → tous les acteurs sur le même écran ») ; **harmonisation de teinte** légende ↔ case (ci-dessus). Reconnues, séquencées derrière l'usage, sans règle ni palier dédié tant que l'usage ne les appelle pas.
- **Question ouverte — workflow demande/accord (révision de règle 26)** — Le PO veut qu'une période ne puisse être réaffectée à l'autre parent qu'après une **demande explicite acceptée**. C'est une révision de la règle « modification directe », pas un correctif ; elle attend le palier « imprévu & échange » et ne génère aucune règle ni sujet tant qu'il n'est pas ouvert.
- **Question ouverte — interdiction/dédoublonnage de slot (révision de règle 16)** — Le PO veut **refuser ou dédoublonner** la pose répétée d'un même slot. C'est une révision du choix v1 « accepté avec avertissement », hors de la boucle courante.
- **Question ouverte — ancre/début explicite du cycle (révision de la décision « ancrage ISO sans ancre »)** — Le PO veut **choisir le début / la phase** du cycle (quelle semaine = index 0). C'est une révision de la décision actée d'ancrage ISO, rattachée au palier « cycle de fond riche » (palier 10) et tranchée **à son make-gherkin**, pas avant.
- **Adoption de l'autre parent (risque mortel)** — L'app n'a de valeur que si l'autre parent l'utilise aussi ; sinon le planning est faux. L'ouverture de l'accès (auth réelle, palier 16) la traite, mais elle vient tard : **à ne pas laisser glisser indéfiniment** derrière la technique. Aucun des paliers techniques en queue ni des prochains incréments d'usage ne lève ce risque ; l'impersonation bornée (livrée) est une **convenance admin**, pas une réponse à ce risque.
- **Contraintes du découplage front/API distant** — Émettre les commandes à travers une API **distante** introduit des contraintes (échanges inter-domaines, sérialisation des commandes, configuration de l'URL d'API, future authentification) absentes quand le front parlait au back en direct ; elles s'accentuent avec l'hôte détaché et le front WASM.
- **Diffusion déclenchée par l'écriture** — Garantir qu'une écriture aboutie déclenche bien l'actualisation temps réel des autres écrans, sans jamais écrire par le canal de diffusion ; validé en usage réel pour le cycle de fond (deux écrans convergent sans rechargement), **acquis pour les dialogs en contexte** (l'ouverture d'une dialog n'interfère pas avec le rafraîchissement de fond), **pour la suppression d'acteur** (un second écran voit la case orpheline retomber sur le fond et la légende se dédoublonner) **et pour le retour auto d'incarnation** (la suppression concurrente de l'acteur incarné replie l'identité effective sur la réelle, bandeau retiré, en temps réel). La stabilité sous exécution parallèle des tests temps-réel reste à durcir (rétrofit déterministe, rang +3).
- **Hors-ligne rejouable — piste à trancher au palier PWA** — Au-delà de l'échec clair livré, une **file d'écritures côté client** (type *outbox*, IndexedDB) garantissant un rejeu « exactement une fois » est la piste minimale ; l'*event sourcing* n'est retenu que si offline / rejeu / audit le justifient. Décision **au moment d'ouvrir le palier**, pas un prérequis.
- **Idées consignées non prioritaires** — Indicateur de **présence de l'autre parent** (temps réel) ; **slot imbriqué** (un slot peut en contenir un autre) ; **parents liés via leurs enfants** (graphe foyer) ; **familles recomposées** (déjà règle 2) : besoins reconnus, séquencés derrière l'usage prioritaire, sans règle ni palier dédié tant que l'usage ne les appelle pas.
- **Différenciation** — Vs Cozi / FamilyWall / agenda partagé : le manque précis qu'on couvre mieux reste à nommer.
