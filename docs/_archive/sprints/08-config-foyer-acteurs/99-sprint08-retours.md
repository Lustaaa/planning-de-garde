# Retours — Sprint 08 (config-foyer-acteurs)

> **Fichier unifié.** Il porte les retours produit (PO), la méthode (agents) et les
> décisions autonomes du chef de projet. Amorcé au cadrage `/2-make-gherkin` (section
> « Décisions autonomes » créée ici) ; scaffoldé par `tdd-analyse` au `/3` qui DOIT
> **préserver** la section « Décisions autonomes (chef de projet) ».

# Retours produit (PO)

> Le code et les tests unitaires sont **hors scope** ici (revus en revue de code).
> Ces retours portent sur l'**usage de l'IHM** : ce qui marche, ce qui coince, ce qui
> manque à l'écran. Remplis les puces, puis lance `/4-retours`.
> Lancement de l'app : `pwsh .claude/skills/run/scripts/run.ps1`.

## IHM - général

-

<!-- une sous-section `## IHM - /<route>` par route du sprint ; routes à confirmer au gate -->
## IHM - /configuration

> Écran de configuration du foyer (nouveau) : renommer / recolorier les acteurs semés.

- pour la configuration du foyer, j'aimerai un palette de couleur pour choisir la couleur de l'acteur.
- j'aimerai des onglet pour configurer le foyer par acteur
  - actuellement un seul foyé, donc tous les acteur doivent etre présent dans le meme écran
  - j'aimerai la possibilité d'ajouter des acteur, parent ou autre (ex: nounou) dans la conf du foyer.

## IHM - /planning

- j'ai l'impression que les couleurs de la légende ne sont pas celle des acteur (parent) 

> Grille partagée (case + légende) : suit l'édition sans rechargement, convergence temps réel.

## Idée pour la suite

- faire des dialog a la place de écran d'édition pour pour concentrer ce qui touche a l'enfant sur un seul ecran
- mettre en place la persistance dans une base mongo a travers un adaptateur de droite.
- Pouvoir interagir sur le planning en selectionnant les case du planning (ex: selection du 29/05 au 05/07 pour définir une période)
- IMPORTANT : Mettre en placa la configuration de la récurence au niveau du foyer. 

## Consigne pour le suite

- essayer de faire un sprint pour et creer un foyer de A a Z
- persistance des donnée en base mongo

## Tech (optionnel)

- (contraintes techniques éventuelles ; laisser vide si aucune → bypass dans `/4-retours`)

# Méthode (agents) — pour retro-sprint

> Retours à la volée du PO sur la **méthode** (agents/skills/commands), appendés par le
> thread principal pendant le sprint. Ne PAS confondre avec les retours produit ci-dessus.

| Date | Cible (agent/skill/command) | Retour | Décision prise |
|------|-----------------------------|--------|----------------|

## IA

> Observations méthode relevées par l'IA (non demandées par le PO), candidates pour `retro-sprint`.

| Date | Cible (agent/skill/command) | Observation | Recommandation |
|------|-----------------------------|-------------|----------------|

## Notes de contexte (décisions produit, hors méthode)

- **À compléter en phase IHM finale** : le sélecteur d'acteur de l'écran `ConfigurationFoyer`
  (et celui d'`AffecterPeriode`) ne liste que `parent-a`/`parent-b` (`Web/Foyer.Responsables`)
  — ni `parent-c` ni `grand-pere`. Les tests runtime Sc.3/Sc.5 pilotent la sélection par
  `.Change()` (câblage réel prouvé), mais au **gate visuel** le PO ne pourra pas choisir
  grand-pere/parent-c depuis l'UI. Compléter la liste des acteurs éditables (les 4 du foyer)
  à la phase IHM finale, avant le gate G3.

# Décisions autonomes (chef de projet)

> Journal des décisions tranchées par l'agent `chef-de-projet` sans escalade PO.
> Permet au PO de piloter a posteriori.

## 2026-06-27 — Périmètre du 1er incrément d'édition des acteurs (volatile)

- **Question (make-gherkin)** : quel périmètre d'édition pour ce premier incrément
  VOLATILE, au plus petit pas d'usage (règle 5) ?
- **Décision (CP, sans escalade)** : option 1 — **renommer + recolorier les acteurs déjà
  semés uniquement** (parent-a, parent-b, parent-c, grand-pere). **Pas d'ajout ni de
  suppression d'acteur**, **pas d'édition du cycle de fond**. L'édition ne touche que le
  **nom** et la **couleur** ; l'**identifiant stable ne change jamais** ; la grille (case +
  légende) résout toujours sur l'id stable (règle 18) et reste dédoublonnée par id.
- **Rationale** : règle 5 = « plus petite tranche cohérente » ; le seed devient son miroir
  éditable. L'ajout/suppression d'acteur introduit la gestion d'identifiants stables, des
  cases orphelines et un impact légende → élargit l'incrément au-delà de la règle 5. Le cycle
  de fond est le **palier 5 (récurrence des périodes)**, séquencé derrière → le tirer ici
  casse la découpe (garde-fou découpe). Aucun arbitrage métier neuf.
- **Sources** : spec v08 règle 5 (édition volatile) + règle 18 (résolution sur id stable) ;
  BACKLOG palier 4 (ce sujet) vs palier 5 (cycle) ; besoins /4-retours s07 (plus petite
  tranche cohérente, G2 PO).

## 2026-06-27 — Consolidation v09 : collision règle 29 (durabilité en queue) vs Mongo borné config foyer

- **Question (spec-consolidation)** : le PO a acté en G2 (/4-retours) que Mongo (persistance
  réelle, palier 13) est tiré DEVANT l'usage mais BORNÉ à la config foyer. Or la règle 29
  (durabilité = persistance réelle séquencée derrière l'usage) + le corollaire v08 « éditable
  maintenant ≠ durable » disent l'inverse. Comment l'écrire dans la spec v09 sans contradiction ?
- **Décision (CP, sans escalade)** : option 1 — **exception bornée écrite noir sur blanc**.
  Conserver la règle 29 mais l'amender d'une **clause d'exception** : la **config foyer**
  devient le **premier client de la persistance durable**, tirée devant l'usage car la survie
  au redémarrage est un **observable d'usage direct**. Reformuler le corollaire en « **durable
  ICI (config foyer), volatile encore ailleurs** » : la volatilité du palier 4 s'éteint pour la
  config foyer uniquement. Écrire la **borne anti-cliquet** : slots/périodes/transferts restent
  en queue (palier 13). Palier 4 figé livré ; nouveau palier « Config foyer persistante » inséré
  devant la récurrence.
- **Rationale** : résolution **déterministe** de représentation — la **valeur** est déjà
  tranchée par le PO (G2 acté au /4). Ce n'est pas un renversement de l'arbitre permanent
  (option 2, rejetée : effet de cliquet) ni un statu quo qui ignore la décision PO (option 3,
  rejetée). L'exception bornée est cohérente avec le BACKLOG (config foyer = « premier client de
  la config durable », palier 13↔8). Aucun nouvel arbitrage métier.
- **Sources** : spec v08 règle 29 + corollaire « éditable ≠ durable » ; décision PO G2 (/4-retours,
  Mongo borné config foyer) ; BACKLOG palier 13↔8.

## 2026-06-27 — Concurrence : où vit l'édition volatile et conflit sur le même acteur

- **Question (make-gherkin)** : où vit le store d'édition volatile, et que se passe-t-il quand
  deux écrans éditent le même acteur en même temps ?
- **Décision (CP, sans escalade)** : option 1 — **store partagé côté serveur (singleton
  derrière les ports), dernière écriture gagne, propagée par le canal de diffusion temps réel
  (lecture seule)**. Une édition est visible par toutes les grilles connectées. Conflit sur le
  même acteur = **convergence vers la dernière valeur** (pas de rejet, pas de version). La
  volatilité se mesure au **redémarrage du serveur** (le seed d'origine réapparaît).
- **Rationale** : cohérent avec les **dépôts in-memory actuels** (singletons = mémoire
  **partagée** du foyer) et avec la **règle 25** (modification directe, sans workflow de
  validation). Le **canal de diffusion temps réel** existe déjà (palier 1, SignalR lecture
  seule) → l'édition aboutie le déclenche, aucune infra neuve. L'option 2 (rejet optimiste sur
  état périmé) ajoute une **notion de version** par acteur — surdimensionné pour un simple
  renommage et en tension avec règle 25 (YAGNI). L'option 3 (isolé par session) contredit le
  **foyer partagé** et casse l'observable « mémoire partagée ». Aucun arbitrage métier neuf.
- **Sources** : dépôts `InMemory*Repository` (singletons) ; spec v08 règle 25 (modification
  directe) ; palier 1 / diffusion SignalR lecture seule (s05) ; spec v08 règle 5 (volatilité).
