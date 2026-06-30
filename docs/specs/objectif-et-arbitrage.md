# Objectif & arbitrage

> Sujet **migré** depuis `docs/15-specification.md` (section « Objectif & arbitrage ») à la migration
> complète des specs. Source de vérité pour l'**arbitre d'usage**, les **exceptions bornées** et les
> **corollaires** qui régissent le séquencement. Édité en diff, jamais réécrit en bloc.

## Trois buts

L'app poursuit trois buts : être un **outil réellement utilisé**, servir de **vitrine** technique, et
rester un **terrain d'apprentissage**. En cas de conflit entre les trois, on garde ce qui sert
l'usage quotidien et on coupe le reste.

## Arbitre principal — l'usage réel tranche

Entre deux besoins qui s'opposent, gagne celui qui rend le hub utilisable au quotidien : les
**saisies visibles** et la **grille lisible** priment sur le confort d'outillage. Un **défaut
confirmé** prime sur une simple évolution. Cet arbitre est **permanent** et tient la main : la
fenêtre d'investissement de fondation est refermée (cf. ci-dessous), et les paliers d'usage « saisie
visible », « lisibilité & thème », « édition des acteurs », « config foyer persistante »,
« récurrence des périodes », « écriture en contexte (dialogs, **transfert inclus**) », « suppression
d'acteur » **et « impersonation bornée lecture »** sont **livrés**. C'est cet arbitre qui tire le
**calendrier navigable** (prochain sujet d'usage) **devant** les paliers techniques et la dette de
test.

## Exception bornée de fondation — refermée

Au début du projet, une **fondation structurelle** a primé ponctuellement sur l'usage immédiat, parce
que le coût de la poser (découpler le back en API, **détacher l'hôte d'API**, ouvrir l'app à d'autres
clients) était minimal alors et **explose une fois l'app grosse**. C'était une **fenêtre
d'investissement de début de projet**, pas une nouvelle règle générale. Cette fenêtre est **close** :
le back démarre seul (hôte d'API détaché, front WASM autonome). L'arbitre d'usage **a repris la main**
dès le palier « saisie visible » et ne la rend plus. Toute nouvelle fondation technique passe
désormais **derrière l'usage**, jamais devant — sa séquence est subordonnée, jamais remplacée. La
seule exception qui l'a précédée était **bornée à la config foyer** (cf. ci-dessous), et n'a pas fait
cliquet.

## Exception bornée — persistance de la config foyer, tirée devant l'usage (réalisée)

La **persistance durable de la SEULE config foyer** (le référentiel des acteurs : noms, couleurs,
acteurs ajoutés) a été tirée **devant l'usage**, parce qu'elle porte un **observable d'usage direct**
— l'ajout ou l'édition d'un acteur **survit au redémarrage** — et qu'elle est, par construction, le
**premier client** de la persistance durable (le palier technique « persistance réelle » s'amorce sur
son premier client). Elle est désormais **livrée**. Ce n'**était pas un renversement** de l'arbitre :
c'est une **borne**, écrite noir sur blanc. Le corollaire qui suit en fixe le périmètre exact, et la
**borne anti-cliquet** empêche le reste du domaine de remonter devant l'usage à sa suite. La
**suppression d'acteur** (livrée) a opéré sur ce **même store durable** → l'acceptation runtime a été
tenue sur **Mongo réel**. L'**impersonation bornée** (livrée) n'a, elle, tiré **aucune persistance
neuve** (état de session / mémoire).

## Corollaire « durable ICI, volatile encore ailleurs »

*(reformule l'ancien « éditable maintenant ≠ durable »)*. Rendre une donnée **éditable** n'oblige pas
à la rendre **durable** dans le même incrément — c'est la découpe qui a permis de livrer l'édition des
acteurs **en mémoire** sans tirer la persistance en avant, et de livrer le **cycle de fond en
mémoire** sans tirer sa durabilité. Mais quand la durabilité porte un **observable d'usage direct** et
reste **bornée**, elle se gagne : c'est le cas de la config foyer, dont la persistance est **livrée
ICI** (la volatilité de l'édition des acteurs s'est **éteinte** pour la config foyer). **Partout
ailleurs** — slots, périodes, transferts, **cycle de fond**, **et l'état d'incarnation** — la donnée
reste **volatile**, sa durabilité **séquencée derrière l'usage** (l'impersonation, par borne, ne
persiste rien). Le « durable » se gagne là où il porte un observable et reste borné ; il reste
séquencé partout où ce n'est pas le cas.

## Borne anti-cliquet

L'exception de persistance est **bornée à la config foyer** et ne doit pas faire **cliquet** : aucun
autre dépôt (slots, périodes, transferts, **cycle de fond**) n'est tiré devant l'usage au prétexte
que la config foyer est passée durable. La persistance du **reste du domaine** demeure en **queue de
séquence** (palier « config foyer durable — reste » puis « persistance réelle »), derrière tout
l'usage. L'**écriture en contexte** l'a confirmé : déplacer la saisie en dialogs — **transfert
compris** — n'a tiré **aucune** persistance (slots / périodes / transferts restent en mémoire). La
**suppression d'acteur** a opéré, elle, sur la config foyer **déjà durable** (palier 5) : elle n'a
**pas** créé de cliquet, elle a **exercé** une persistance acquise. L'**impersonation bornée**
(livrée) n'a tiré **aucune persistance neuve** : l'état d'incarnation est **session / mémoire**, rien
ne subsiste au redémarrage.

## Corollaire de découpe

Quand le périmètre d'un sujet déborde, on coupe au **plus petit incrément** qui rend la grille lisible
et utilisable : on séquence, on ne reporte jamais tout en bloc. C'est ce corollaire qui a justifié de
livrer d'abord un calendrier en lecture seule avant d'y brancher l'écriture, de borner la séparation
de l'hôte d'API au plus petit pas qui rend le back démarrable seul, de tenir la lisibilité (nom +
légende) comme observable avant le thème de surface, de livrer l'**édition** des acteurs avant leur
**ajout** et leur persistance, de livrer un **cycle de fond** (parité ISO en mémoire) avant d'en
enrichir l'ergonomie, et — au palier « écriture en contexte » — de livrer **trois dialogs** en deux
temps (Poser un slot + Affecter une période, **puis** Définir un transfert) plutôt qu'en bloc. Il a
guidé le palier acteurs : **CRUD acteurs** s'est découpé en **suppression d'abord (livrée)** puis
**impersonation bornée lecture (livrée)**. Il **oriente** le prochain sujet, **Calendrier navigable**
(palier 9, gardé **groupé**) : son plus petit incrément probable est la **navigation seule**, la
**sélection de plage de cases** étant un sujet plein **cuttable en tranche 2** si elle déborde ~2h —
périmètre exact **tranché au make-gherkin**, non pré-arbitré ici. Il reste le **garde-fou de secours**
des sujets pris en bloc.

## Révisions de règle hors boucle

Une demande qui contredit une règle déjà actée n'est **pas** un correctif : c'est une révision de
spec, qui n'entre pas dans le séquencement courant et attend le palier qui la porte. Trois telles
demandes sont en attente (cf. [`risques-et-questions-ouvertes.md`](risques-et-questions-ouvertes.md)) :
le workflow demande/accord avant réaffectation (palier « imprévu & échange »),
l'interdiction/dédoublonnage de la pose répétée d'un même slot, et le **choix explicite d'une
ancre/d'un début de cycle** (option « date d'ancrage » jadis écartée au profit de l'ancrage ISO sans
ancre), qui sera **ré-arbitré au make-gherkin du palier « cycle de fond riche »**. *(Le durcissement
du gating de l'écran de config, jadis quatrième révision en attente, a été **consommé** au palier 8 /
impersonation : toutes les écritures config sont désormais gatées sur l'identité effective — il n'est
plus en attente.)*

## Prochain sujet — calendrier navigable (palier 9)

La tranche acteurs étant close (CRUD complet + impersonation bornée lecture livrés), l'usage tire le
**calendrier navigable** : faire du hub `/planning` un agenda où l'on se déplace dans le **passé et le
futur**, avec des **vues prédéfinies** (semaine, mois, 4 semaines glissantes) et une **amorce de
sélection de plage de cases** pour définir une période sur l'intervalle. Besoin **ancien** (retours
s02 / s03), au **rang +2** du backlog, tranché en **porte G2** par le PO : suite d'usage naturelle
après la tranche acteurs. **Orientation de découpe (pas une règle)** : (a) le plus petit incrément
probable est la **navigation seule** (semaines précédente / suivante, ou bascule de vue) ; (b) la
**sélection de plage** est un **sujet plein cuttable en tranche 2** si elle déborde la borne ~2h ;
(c) le **périmètre exact** est **tranché au make-gherkin**, **non pré-arbitré ici** (conforme au
corollaire de découpe). Le palier reste **groupé** dans la séquence (pas de découpe 9a/9b actée en
spec). **Aucune persistance neuve** n'est tirée en avant. Sujet `/2-make-gherkin` =
`calendrier-navigable`. Détail : [`calendrier-navigable.md`](calendrier-navigable.md).
