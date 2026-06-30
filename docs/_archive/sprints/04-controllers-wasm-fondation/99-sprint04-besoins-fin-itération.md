# Besoins fin d'itération — Sprint 04 (controllers-wasm-fondation)

> Produit par `/4-retours` à partir des retours produit du PO
> (`99-sprint04-retours.md`, section `# Retours produit (PO)`), confrontés au code
> courant (HEAD). Ce fichier **réamorce `/2-make-gherkin`** sur **un seul** prochain
> sujet ; le reste est séquencé derrière sous l'arbitre d'usage. Il **ne contient ni
> code, ni Gherkin, ni spec** — seulement la priorisation.

## Objectif de la boucle

Refermer le **palier de fondation** (le back doit être démarrable seul, l'API détachée
du front), puis **rebrancher l'usage réel** : qu'une saisie réapparaisse dans la grille,
**à la bonne date et avec la bonne couleur de parent**.

## Arbitre

**L'usage réel tranche** (arbitre permanent de la spec vivante). Tant que la fondation
n'est pas refermée, elle prime **ponctuellement** (exception bornée de début de projet) ;
dès qu'elle l'est, l'usage **reprend la main**. Ordre de priorité quand deux besoins
s'opposent :

1. les **saisies visibles** priment sur l'ergonomie du calendrier ;
2. l'**ergonomie du calendrier** prime sur le confort d'outillage ;
3. un **défaut confirmé** prime sur une simple évolution ;
4. toute **révision d'une règle déjà actée en v1** est écartée du séquencement.

## Prochain sujet pour `/2-make-gherkin`

### Host API séparable — démarrer le back seul

Détacher l'**API d'écriture** du front pour que le **back démarre seul**. Aujourd'hui le
canal d'écriture est porté par le projet Web (front et hôte couplés), donc impossible de
lancer une API headless. Le sujet pose un hôte d'API détaché qui expose le canal
d'écriture, le front consommant cette API distante.

Au passage (garde-fou d'outillage, à caser dans le même sujet) : exposer une **UI
d'exploration interactive** des API. Le **document** de description OpenAPI existe déjà
(livré ce sprint), mais l'**UI pour essayer les endpoints** (type Swagger-UI / Scalar)
n'a pas été livrée.

**Pourquoi celui-ci d'abord** : choix du PO au titre de l'**exception bornée de
fondation** — on referme la fondation avant de revenir à l'usage. C'est le palier en
cours ; le coût de séparer l'hôte est minimal maintenant et explose une fois l'app grosse.

> **Risque à tenir** : ce sujet n'avance **aucun usage visible** (faux sentiment de
> progrès). Borner au plus petit incrément qui rend le back démarrable seul, et basculer
> **vite** sur le lot « saisie visible » juste après — le grief des saisies invisibles ne
> doit pas glisser une itération de plus.

## Séquence du reste (derrière le prochain sujet, sous l'arbitre d'usage)

### 1. Une saisie réapparaît dans la grille — à la bonne date ET en couleur du parent

Un **seul lot** qui répare deux défauts liés, pour que le PO en constate l'effet d'un
coup :

- **Dates de saisie par défaut = aujourd'hui.** Les dates pré-remplies des formulaires
  (poser un slot, affecter une période, définir un transfert) sont **figées sur une année
  passée**. Toute saisie tombe donc **hors de la fenêtre affichée** et semble invisible.
  C'est la **cause-racine du faux bug « les saisies n'apparaissent pas »**, reconnue par
  le PO lui-même. La projection du planning n'est **pas** en cause : elle lit et colore
  correctement ce qui couvre la fenêtre.
- **Couleur du parent responsable.** Une période affectée s'affiche en **gris neutre** au
  lieu de la couleur du parent, parce que le **nom de parent** choisi dans le formulaire ne
  correspond pas à l'**identifiant** attendu par la palette de couleurs. C'est le **seul
  vrai défaut confirmé** dans le code courant ; un correctif ciblé suffit.

> Les deux sont **distincts mais indissociables à l'usage** : sans la date, la couleur
> corrigée reste invisible ; sans la couleur, l'affectation remise dans la fenêtre reste
> grise. À traiter ensemble.

### 2. Navigation et vues du calendrier

Naviguer dans le **passé/futur** et offrir des **vues prédéfinies** (semaine, mois,
4 semaines glissantes à partir de la semaine en cours). À trancher au passage : la
**fenêtre par défaut** — le PO veut **4 semaines**, la spec en décrit **5** (semaine en
cours + 4 suivantes). Lever cette incohérence avant de scénariser.

### 3. UI d'exploration des API (si non absorbée par le prochain sujet)

Confort d'outillage : l'UI interactive pour essayer les endpoints. Subordonnée à l'usage ;
à absorber dans le sujet « host API séparable » si possible, sinon ici.

## Tracé mais NON séquencé (révisions de règle v1)

Ces deux demandes **contredisent des choix explicites de la v1** : elles ne sont pas des
correctifs mais des **révisions de spec**, à rouvrir plus tard, hors de cette boucle.

- **Interdire / dédoublonner la pose répétée d'un même slot.** Aujourd'hui un slot qui
  chevauche est **accepté avec avertissement** (choix produit v1). Le PO veut le **refuser
  ou le dédoublonner** → révision de règle.
- **Demande acceptée avant de réaffecter une période à l'autre parent.** La v1 acte la
  **modification directe sans workflow de validation**. Le PO veut un **workflow
  demande/accord** → révision de règle (relève du palier « imprévu & échange »).

## Confirmé NON-bug (faux bug désamorcé)

- **« Les saisies n'apparaissent pas »** : défaut des **dates par défaut périmées**, pas
  de la projection. Aucune réparation de la projection n'est ordonnée.
- **« Le thème est dégueulasse »** : **absence de feature** (thème métier non encore fait,
  palier lisibilité & thème), pas une régression.
- **« Les transferts ne s'affichent pas »** : **trou fonctionnel par construction** — la
  projection du planning ne lit **aucun transfert** (le sujet « transferts au panneau
  cloche » viendra plus tard). Symptôme réel, mais pas un comportement vert qui casse.

## Dette à ne pas oublier (non signalée par le PO, confirmée en HEAD)

- **La vue « définir un transfert » est restée en retrait** de la migration de ce sprint :
  elle **écrit le back en direct** (pas via le canal d'écriture), garde de la **logique
  dans le template** (convention code-behind non respectée) et porte la **même date figée
  sur une année passée**. À résorber au moment du rebranchement de l'usage.

## Risques de la séquence

- **Faux sentiment de progrès** : le prochain sujet (fondation) n'avance aucun usage
  visible. Tenir la séquence pour basculer vite sur « saisie visible ».
- **Bloc de fondation potentiellement gros** : détacher l'hôte d'API, recâbler le client
  HTTP — borner au plus petit incrément, surveiller la dérive au make-gherkin.
- **Fenêtre du calendrier en dur (5 semaines)**, incohérente avec la demande PO (4) et la
  spec — trancher la cible avant de scénariser la navigation.
