# Séquence de livraison

> Sujet **migré** depuis `docs/15-specification.md` (section « Séquence de livraison ») à la migration
> complète des specs. **Roadmap canonique** des paliers 1→18 avec leur statut (livré / non livré).
> Édité en diff, jamais réécrit en bloc.

Tous les besoins comptent, mais ils sont **séquencés** (pas livrés en bloc). Chaque palier doit être
adopté avant le suivant ; chacun se borne au plus petit pas qui apporte une valeur lisible. Le palier
de fondations a ouvert la séquence au titre de l'exception bornée ; il est **refermé**, et les paliers
d'usage « saisie visible », « lisibilité & thème », « édition des acteurs », « config foyer
persistante », « récurrence des périodes », « écriture en contexte (dialogs, transfert inclus) »,
« suppression d'acteur » **et « impersonation bornée lecture »** qui l'ont suivi sont **livrés**.
L'arbitre d'usage tient l'ordre : les paliers d'usage d'abord, les paliers **techniques en queue de
séquence**, derrière tout l'usage — à la seule **exception bornée** de la persistance de la config
foyer, qui a été tirée devant parce qu'elle portait un observable d'usage direct, et qui est désormais
livrée. *(Arbitre & corollaires : [`objectif-et-arbitrage.md`](objectif-et-arbitrage.md).)*

## Vue d'ensemble

| # | Palier | Statut | Sujet dédié |
|---|---|---|---|
| 1 | Fondations — découplage du back en API, hôte détachable | ✅ refermé | [`fondations-api.md`](fondations-api.md) |
| 2 | Saisie visible | ✅ livré | [`saisie-et-grille.md`](saisie-et-grille.md) |
| 3 | Lisibilité & thème | ✅ livré | [`saisie-et-grille.md`](saisie-et-grille.md) |
| 4 | Config foyer — édition des acteurs (en mémoire) | ✅ livré | [`acteurs-et-config-foyer.md`](acteurs-et-config-foyer.md) |
| 5 | Config foyer persistante — ajout & survie redémarrage | ✅ livré | [`acteurs-et-config-foyer.md`](acteurs-et-config-foyer.md) |
| 6 | Récurrence des périodes — cycle de fond | ✅ livré | [`periodes-et-cycle-de-fond.md`](periodes-et-cycle-de-fond.md) |
| 7 | Écriture en contexte — dialogs depuis le planning | ✅ livré (épic refermé) | [`ecriture-en-contexte.md`](ecriture-en-contexte.md) |
| 8 | CRUD acteurs complet & impersonation bornée lecture | ✅ livré | [`acteurs-et-config-foyer.md`](acteurs-et-config-foyer.md) |
| 9 | Calendrier navigable — navigation, vues, sélection de plage | ✅ **COMPLET : navigation + vues LIVRÉES s15 ; sélection de plage par DRAG LIVRÉE s49** | [`calendrier-navigable.md`](calendrier-navigable.md) |
| 10 | Cycle de fond riche — ancre, frontière, plages, sur-cycles | ⏳ non livré | (ci-dessous) |
| 11 | Survol → résumé de la journée | ⏳ skippé (faute de demande PO) | (ci-dessous) |
| 12 | Config foyer durable — reste de la config | ⏳ non livré (technique) | (ci-dessous) |
| 13 | Modèle d'acteurs & foyer | ⏳ non livré | (ci-dessous) |
| 14 | Immédiat & événements à venir (panneau cloche) | ✅ **COMPLÉTÉ** : cloche générale s47 ; **digest « immédiat » (qui récupère ce soir + transferts à venir) réintégré DANS la cloche s50** | [`notifications-et-echange.md`](notifications-et-echange.md) |
| 15 | Imprévu & échange | ✅ échange consenti livré s47 ; imprévu dédié (malade / retard, informatif) livré s48 | [`notifications-et-echange.md`](notifications-et-echange.md) |
| 16 | Ouverture de l'accès — landing + auth réelle | ⏳ non livré | (ci-dessous) |
| 17 | Persistance réelle — adaptateurs de droite (reste du domaine) | ⏳ non livré (technique) | (ci-dessous) |
| 18 | Saisie hors-ligne (PWA) | ⏳ non livré (technique) | (ci-dessous) |

## Paliers livrés (détail dans les sujets dédiés)

1. **Fondations — découplage du back en API, hôte détachable** *(exception bornée de fondation,
   valeur d'usage immédiate nulle, assumée — **REFERMÉ**)*. Les commandes d'écriture (poser un slot,
   affecter ou supprimer une période, ajuster un transfert) sont confiées à un **canal
   requête/réponse** côté serveur. L'**hôte d'API est détaché** : le back **démarre seul** (sans
   référencer le front) et le front, exécuté **dans le navigateur** (WebAssembly), le consomme comme
   une **API distante**. L'API est **explorable** (document OpenAPI + UI interactive) et autorise
   l'**origine du front** (CORS) ; une API injoignable produit un **échec clair** (message à l'écran,
   saisie non appliquée, **sans file ni rejeu**). *Socle d'un produit ouvert, posé au moment où le
   coût était minimal. Refermé ; l'usage reprend la main.* → [`fondations-api.md`](fondations-api.md)

2. **Saisie visible — la saisie réapparaît à la bonne date et en couleur du parent** *(reprise
   d'usage — **LIVRÉ**)*. Une saisie posée **réapparaît immédiatement** dans la grille, **à la bonne
   date** (les dates pré-remplies des formulaires valent « aujourd'hui », pas une date figée) **et en
   couleur du parent responsable** (la couleur se résout sur l'identifiant stable de l'acteur, pas sur
   son libellé d'affichage). → [`saisie-et-grille.md`](saisie-et-grille.md)

3. **Lisibilité & thème — qui garde se lit d'un coup d'œil** *(reprise produit — **LIVRÉ**)*. La
   responsabilité de période est **explicite** dans la grille : le **nom du responsable** est affiché
   dans la case **et** une **légende couleur** accompagne la grille ; un nom trop long est **tronqué**
   tout en restant lisible en entier au **survol** ; un acteur hors du set connu reste **affiché et
   distingué** (gris assumé) sans perdre son nom. L'app porte un **thème en accord avec le domaine**
   (garde d'enfants). → [`saisie-et-grille.md`](saisie-et-grille.md)

4. **Config foyer — édition des acteurs (en mémoire)** *(reprise d'usage — **LIVRÉ**)*. Un écran pour
   **éditer les acteurs du foyer** : leurs **noms** et leurs **couleurs**. Le seed jusqu'ici figé est
   devenu **éditable** et la **grille (case + légende) reflète immédiatement** le changement *dans la
   session*. L'édition vivait alors **en mémoire** ; sa **durabilité** a été portée par le palier
   suivant. → [`acteurs-et-config-foyer.md`](acteurs-et-config-foyer.md)

5. **Config foyer persistante — ajout d'acteurs & survie au redémarrage** *(reprise d'usage +
   exception bornée de persistance — **LIVRÉ**)*. Deux choses, fondues en un incrément : **(a)**
   **ajouter** un acteur au foyer (parent / autre / nounou), au-delà du renommage/recoloriage du seed
   — l'ajout génère un **identifiant stable neuf** (jamais le libellé) et la grille (case + légende,
   dédoublonnée par id) le reflète aussitôt ; **(b)** **persister** la config foyer (référentiel des
   acteurs : noms, couleurs, acteurs ajoutés) via un **adaptateur de droite durable**, derrière les
   ports existants (`IReferentielResponsables` / `IPaletteCouleurs` /
   `IEditeurConfigurationFoyer` inchangés). L'observable est tenu : « j'ajoute la nounou → elle
   apparaît dans l'écran de config **et** dans la grille ; **après redémarrage, elle est toujours
   là** ». La **volatilité du palier 4 est éteinte ICI**, pour la config foyer **uniquement** ; le
   reste du domaine (slots / périodes / transferts) **reste en mémoire**. *Livré : ajout **sans
   suppression** d'abord (la suppression d'acteur a été livrée au palier 8 « CRUD acteurs —
   suppression »), **sans** édition du cycle de fond ici.* → [`acteurs-et-config-foyer.md`](acteurs-et-config-foyer.md)

6. **Récurrence des périodes — définir le cycle de fond** *(reprise d'usage — **LIVRÉ**)*. Une
   **couche de résolution du responsable de fond** est posée sous les périodes explicites : le cycle
   compte **N semaines** (N ≥ 1) et alterne par **parité de la semaine ISO** (`index = semaine ISO du
   jour, modulo N`), chaque index étant mappé sur un **responsable de fond** résolu sur son
   **identifiant stable** (jamais le libellé). La résolution d'une case suit une **priorité** :
   **surcharge (période saisie) > fond (cycle) > neutre**. La grille affiche le fond (case **nommée +
   colorée** + **légende** étendue aux responsables de fond présents dans la fenêtre) sans qu'aucune
   période ne soit saisie ; un index sans responsable retombe sur la **teinte neutre** (repli miroir
   de la couleur neutre), sans nom fantôme. Le cycle est **éditable** depuis la configuration du foyer
   (section « Cycle de fond » : nombre de semaines + un sélecteur de responsable par index, **alimenté
   par les acteurs persistés** du foyer) ; définir **zéro semaine** est **refusé** (« le cycle doit
   compter au moins une semaine »), le cycle précédent restant inchangé. Toute ré-édition du mapping
   met à jour la grille **sans rechargement** (diffusion temps réel), et sur édition concurrente la
   **dernière écriture gagne**. *Le cycle vit **en mémoire** (borne anti-cliquet) ; sa durabilité est
   portée par le palier « config foyer durable — reste ». Son **ergonomie riche** (ancre, frontière de
   jour, plages, sur-cycles) est un palier d'évolution séquencé (palier 10).* →
   [`periodes-et-cycle-de-fond.md`](periodes-et-cycle-de-fond.md)

7. **Écriture en contexte — dialogs depuis le planning** *(reprise produit — **LIVRÉ COMPLET, épic
   refermé**)*. La saisie **se déplace là où on lit** : un **clic sur une case** ouvre un **menu
   d'actions** à **trois entrées** dont chacune ouvre une **dialog** pré-remplie sur la **date de la
   case**, alimentée par les acteurs et lieux du foyer. **Trois dialogs livrées** — **Poser un slot**,
   **Affecter une période** et **Définir un transfert** — et **tous les écrans/routes dédiés**
   correspondants (slot, période **et** la dernière page `definir-transfert`) **retirés** : il
   n'existe plus qu'**un seul chemin d'écriture** et **plus aucun écran de saisie dédié ne subsiste**.
   Issues : **succès** → dialog fermée + grille relue (transfert : **accusé « Transfert défini »** à
   part, non bloquant) ; **échec** (refus domaine **ou** API injoignable) → dialog **reste ouverte**,
   message **dans la dialog**, saisie **conservée**, grille **inchangée** ; **chevauchement** (slot) →
   écriture **aboutie**, dialog fermée, **avertissement non bloquant** affiché **à part**. Le
   **déclencheur** (menu) est **gaté** : seuls les Parents l'ouvrent (consultation seule des Invités
   préservée). *Observable = le **déplacement de la saisie en contexte**, pas une règle de gestion
   neuve ; réutilise les commandes / handlers (`PoserSlot`, `AffecterPeriode`, **`DefinirTransfert`**)
   et le canal requête/réponse déjà livrés (**pas de handler neuf**), la réapparition immédiate dans
   la grille et la diffusion SignalR lecture seule ; aucune persistance tirée en avant (slots /
   périodes / **transferts** restent en mémoire).* **Épic « écriture en contexte » refermé** ; il n'y
   a plus de reliquat. → [`ecriture-en-contexte.md`](ecriture-en-contexte.md)

8. **CRUD acteurs (complet) & impersonation bornée lecture** *(reprise d'usage — **LIVRÉ**, tiré
   devant le calendrier navigable)*. Le cycle de vie des acteurs est **complet** : Create + Read +
   Update **et Delete** (épic **É2**), plus le **dernier maillon — impersonation bornée lecture
   seule** (épic **É10**). La **suppression autorisée** **cadre les cases orphelines par
   neutralisation par repli** : la **surcharge orpheline cesse de primer**, la case retombe sur le
   **fond** (le cycle reprend, cf. priorité surcharge > fond > neutre) ou sur le **neutre** si l'index
   n'est ni mappé ni résolu (sans nom fantôme) ; si l'acteur supprimé était **mappé au cycle de
   fond**, son index devient **non mappé → neutre**. Un **accusé non bloquant** (« Acteur supprimé »)
   accompagne l'acte ; **pas de réaffectation automatique** (règle 6). La config foyer étant
   **persistée Mongo** (palier 5), la suppression a touché un **store réel** → **acceptation runtime
   tenue**. L'**impersonation bornée lecture** distingue une **identité réelle** (le configurateur,
   type Parent) d'une **identité effective** (l'acteur incarné, ou **repli sur la réelle**) :
   `Incarner(acteurId)` lit le référentiel (**refus silencieux si absent**, identité réelle
   conservée), un **bandeau « Vous incarnez X »** signale l'incarnation, le **droit d'écriture dérive
   du type de l'identité effective** (Parent/Admin → écriture visible, Autre → écritures masquées,
   grille **et** écran de config), et `RevenirIdentiteReelle()` restaure l'état. La **suppression
   concurrente** de l'acteur incarné **replie automatiquement** l'identité effective sur la réelle
   (bandeau retiré), en **temps réel** (SignalR). Le **type d'acteur** est surfacé en **lecture
   seule** depuis le seed (extension read-only de l'énumération acteurs ; acteurs ajoutés typés
   **Parent** par défaut, aucune saisie de type). **Bornes tenues** : **pas d'écriture « au nom de »**
   (commandes sous identité réelle, canal requête/réponse inchangé), **aucun port/handler d'écriture
   neuf**, **zéro persistance neuve** (état d'incarnation session / mémoire — borne anti-cliquet règle
   30), et **PAS** l'authentification réelle du palier 16. *Ce palier est **clos côté usage** ; le
   prochain sujet est le calendrier navigable (palier 9).* →
   [`acteurs-et-config-foyer.md`](acteurs-et-config-foyer.md)

9. **Calendrier navigable — navigation, vues prédéfinies & sélection de plage** *(reprise produit —
   **NAVIGATION + VUES LIVRÉES s15** ; **sélection de plage restante, séquencée s49**)*. Le hub
   `/planning` est un **agenda navigable** : déplacement **passé/futur** (`PlanningPartage.razor` —
   `nav-semaine-precedente` / `nav-semaine-suivante` / `nav-aujourdhui`) et **vues prédéfinies**
   (semaine, mois, **4 semaines glissantes** — `selecteur-vue`), la fenêtre résolue par
   `GrilleAgendaQuery.Projeter(ancre, vue)` sur `SessionPlanning` (**état de navigation NON persisté**).
   **Reste NON LIVRÉ** = **sélectionner une plage de cases** pour affecter une période sur
   l'**intervalle** choisi (l'affectation par plage rouvre l'écriture en contexte sur plusieurs jours
   d'un coup) — enrichit la grille sans toucher aux mécaniques d'écriture déjà livrées (dialogs),
   **aucune persistance tirée en avant**. → [`calendrier-navigable.md`](calendrier-navigable.md)

## Paliers non livrés (sans sujet dédié à ce stade)

10. **Cycle de fond riche — ancre, frontière de jour, plages & sur-cycles** *(reprise d'usage, sujet
    plein à découper)*. Enrichir le cycle de fond livré (palier 6) pour le rendre **réellement
    utilisable au quotidien** : **(a)** choisir explicitement le **début / l'ancre** du cycle (quelle
    semaine = index 0), **(b)** une **frontière de jour paramétrable** (ex. vendredi → vendredi),
    **(c)** une **plage de validité** début **et** fin, **(d)** un **sur-cycle / exception
    saisonnier** (vacances), **(e)** un cycle **WE-only** (1 week-end sur 2). *Ce palier **rouvre
    explicitement** la décision actée « ancrage ISO sans ancre » — le choix d'un début/d'une phase
    **est** l'option « date d'ancrage » jadis écartée : elle est **ré-arbitrée au make-gherkin de ce
    palier**, pas avant (révision de règle hors boucle). Besoin gardé **groupé** ; sa **découpe est
    impérative** au cadrage (corollaire de découpe). La plage début/fin et les sur-cycles
    **chevauchent la durabilité du cycle** (palier « config foyer durable — reste ») : n'enrichir que
    l'**observable**, ne PAS tirer Mongo par précaution (borne anti-cliquet).*

11. **Survol → résumé de la journée** *(reprise d'usage, évolution — séquencée, skippée faute de
    demande PO)*. Au survol prolongé (~1 s) d'une case, afficher un **résumé de la journée**, au-delà
    du seul nom complet déjà rendu au survol simple. *Enrichissement, pas une réparation : le survol
    simple (nom complet) est livré et conforme. Le périmètre du résumé (périodes / slots / responsable
    / transferts) est à cadrer au make-gherkin et ne doit pas être sous-estimé. Skippé tant que le PO
    ne le réclame pas ; séquencé, pas écarté.*

12. **Config foyer durable — reste de la config** — **persister** la part restante de la
    configuration du foyer (lieux, set de couleurs par défaut, **cycle de fond**) plutôt que de la
    porter en données figées dans le code. *Le **volet acteurs** de la config durable a déjà été livré
    au palier 5 (premier client du store réel) ; ce palier complète la config durable pour les lieux,
    couleurs et **cycle de fond** (aujourd'hui en mémoire).*

13. **Modèle d'acteurs & foyer** — déclarer les acteurs réels (Admin, Parents, Autres) dans un écran
    de configuration, qui porte aussi la **responsabilité récurrente de fond** (le cycle) et le **set
    de couleurs par défaut**. *Exploite la config persistée des paliers précédents ; prérequis de
    l'ouverture de l'accès.*

14. **Immédiat & événements à venir** *(**cloche générale LIVRÉE s47** ; **digest « immédiat »
    COMPLÉTÉ s50** — palier « immédiat & rappels » de la vision refermé)* — transferts et changements
    présentés comme **événements dans une CLOCHE** (barre du haut), plus comme un tableau permanent.
    **Livré s47** : **journal de changements append-only** (`IJournalChangements`) alimenté par chaque
    handler d'écriture = **trace de lecture non-autorité** (jamais lue par la résolution), **lu/non-lu
    par utilisateur** + compteur, badge + panneau, gating connecté && Parent, **temps réel porteur de
    payload** (`INotificateurChangement`, 0 GET). **Complété s50** : un **digest « immédiat » en tête
    du panneau cloche** — « qui récupère aujourd'hui / ce soir » (responsable résolu surcharge > fond >
    neutre + où/slot + transfert) + « transferts à venir » de la fenêtre chargée — **query PURE de
    composition** `DigestImmediatQuery` réemployant `GrilleAgendaQuery` (**0 store neuf, 0 mutation**),
    reprojection client **0 GET** (fenêtre grille + diffusion), lecture stricte, Parent-gated. **Ramène
    DANS la cloche** « qui récupère ce soir » (s42) et « à venir » (s43) **retirés de la grille s44**
    (anti-cliquet s44 tenu : aucune carte/panneau réintroduit sur `/planning`). *Expose enfin les
    transferts, invisibles par construction jusque-là.* **Limitation assumée (backlog)** : digest borné
    à la fenêtre grille chargée (persistance hors-fenêtre non rouverte). →
    [`notifications-et-echange.md`](notifications-et-echange.md)

15. **Imprévu & échange** *(**échange consenti LIVRÉ s47** ; **imprévu dédié LIVRÉ s48**)* — échange de
    dernière minute, transferts **dérivés automatiquement** par défaut. **Livré s47** : le **workflow
    demande / accord** (question longtemps ouverte) — `ProposerEchange` crée un `pending` **sans
    écriture**, `AccepterProposition` **compose la délégation s44** (surcharge + transfert dérivé s31),
    `RefuserProposition` clôt sans écriture ; proposition **actionnable dans la cloche**. **Livré s48** :
    le **signalement d'imprévu dédié** (malade / retard, entrée distincte de l'échange) — cas
    **non-négocié / informatif**, `SignalerImprevu` consigne au journal **sans toucher la résolution**,
    notif cloche **sans action de suivi** (brique C). **Reste** : **action de suivi** (réagir à un
    imprévu par un échange) et l'extension plage / série / multi-enfants. →
    [`notifications-et-echange.md`](notifications-et-echange.md)

16. **Ouverture de l'accès** — landing page et authentification des acteurs réels (email via Gmail /
    Apple / Microsoft) pour lever le risque mortel d'adoption. Débloque aussi la **personnalisation
    des couleurs par utilisateur** et la **persistance d'une préférence de thème** (thème sombre), et
    **transforme l'impersonation bornée** (palier 8) en accès réel par acteur. *Vient après le socle
    et le modèle d'acteurs ; à ne pas laisser glisser indéfiniment.*

17. **Persistance réelle — adaptateurs de droite (reste du domaine)** *(palier technique, derrière
    l'usage)* — remplacer les dépôts **en mémoire** (volatils) des **slots, périodes et transferts**
    par des **adaptateurs de droite** vers un store **durable**, derrière les ports existants, sans
    toucher au domaine. *Débloqué par la fondation (palier 1) mais **subordonné à l'usage**. La
    **config foyer** en a été le **premier client**, déjà rendu durable au palier 5 (exception
    bornée) ; ce palier étend la durabilité au reste du domaine. **Borne anti-cliquet** : il ne
    remonte pas devant l'usage.*

18. **Saisie hors-ligne (PWA)** *(palier technique, derrière l'usage)* — au-delà de l'**échec clair**
    déjà livré (palier 1), mettre en cache et **mettre en file** les écritures faites hors connexion,
    **rejouées au retour du réseau**. *Subordonné à l'usage. Piste consignée : une **file d'écritures
    côté client** (type *outbox*, IndexedDB) garantissant un rejeu « exactement une fois » comme socle
    minimal ; l'*event sourcing* n'est retenu que si offline / rejeu / audit le justifient — à trancher
    au moment d'ouvrir ce palier, pas un prérequis.*

## Notes de séquencement

### Re-séquencement acté

Le **palier 8** est désormais **clos côté usage** : la **tranche suppression**
(`crud-acteurs-suppression`) **et** la **tranche impersonation bornée lecture** (`impersonation-bornee`)
sont **livrées** (CRUD acteurs complet ; incarner un acteur déclaré avec bandeau, vue selon le rôle
effectif, retour identité réelle, retour auto sur suppression concurrente, durcissement complet du
gating config — 6/6 scénarios verts, suite complète 214/214, sur **store Mongo réel** et **app câblée
/ G3**, **zéro persistance neuve**). Le **prochain sujet** est le **calendrier navigable** (palier 9,
**non livré** : navigation passé/futur, vues prédéfinies, amorce de sélection de plage), tiré par
priorité d'usage devant le **rétrofit déterministe des tests temps-réel SignalR** (dette de test),
l'**édition concurrente du même jour sous dialog** (cas limite, dépend du rétrofit) et l'**impersonation
écriture « au nom de »** (hors-cap : franchit la borne dure du palier 8, exige une décision PO
explicite). Derrière le calendrier vient le **cycle de fond riche** (palier 10, qui rouvre l'ancrage
ISO à son cadrage). Le **survol enrichi** (palier 11) reste **skippé** faute de demande PO, séquencé
sans être écarté. La persistance du **reste du domaine** reste en queue (palier 17), derrière tout
l'usage — seule la persistance de la config foyer a été tirée devant (borne).

### Numérotation — v15 référence unique

La numérotation des paliers est **inchangée depuis la v13** (le swap palier 8/9 — tranche acteurs
devant calendrier navigable — a déjà été acté et répercuté au `docs/BACKLOG.md`). Le **palier 8** est
désormais **clos côté usage** (suppression **et** impersonation bornée lecture livrées) ; les paliers
suivants (calendrier navigable 9, cycle de fond riche 10, survol enrichi 11, config foyer durable 12,
modèle d'acteurs 13, immédiat & événements 14, imprévu & échange 15, ouverture de l'accès 16,
persistance réelle 17, PWA 18) restent inchangés. La séquence ci-dessus est la **référence unique** et
continue.

### Transverse (hors incrément dédié)

L'ergonomie de surface est absorbée au fil des incréments calendrier et reste subordonnée à l'usage
par l'arbitre. Le **thème sombre + bascule clair/sombre** (avec persistance de la préférence) est une
évolution additive au thème métier livré : consignée, non priorisée, à rattacher au futur écran de
préférences utilisateur (cf. Risques). De même, un **sélecteur de couleur (palette / picker)** dans
l'écran de config et l'**harmonisation de teinte légende ↔ case** (non-bug, cf. Risques) restent des
évolutions de surface non priorisées seules.

### Garde-fous hors-spec

La convention de code interne (chaque vue adossée à son code-behind), l'outillage d'**API explorable
et documentée** (document OpenAPI **et** UI interactive type Swagger-UI / Scalar) et l'**empaquetage en
conteneurs** (hôte API + front WASM + store, montables ensemble façon compose) sont des **garde-fous
de structure et d'outillage sans observable métier** : ils n'ouvrent ni règle de gestion ni incrément
produit, et restent subordonnés à l'usage par l'arbitre. La **restructuration du code applicatif**
menée hors-pipeline (adaptateurs de droite par techno, SignalR comme adaptateur de gauche, rangement
par type, code-behind systématique) a été conduite **à iso-comportement strict** — aucun observable
métier touché — et sa condition de sortie a été tenue : **suite complète verte** via `dotnet test`
**sans `--no-build` ni filtre, Docker actif** (pivot Mongo de la config foyer inclus).

### Prochain sujet

**Calendrier navigable — SÉLECTION DE PLAGE** (`calendrier-navigable`, palier 9 tranche 2, épics
É4 + É7). La **navigation passé/futur + vues** (tranche 1) sont **livrées s15** ; **reste** la
**sélection de plage de cases** (drag / multi-jours) pour affecter une période sur l'**intervalle**,
**sans persistance neuve**. **Séquencée s49** (après l'imprévu malade/retard s48). Périmètre exact
**tranché au make-gherkin**.
