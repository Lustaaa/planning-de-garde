# Retours — Sprint 08 (config-foyer-acteurs)

> **Fichier unifié.** Il porte les retours produit (PO), la méthode (agents) et les
> décisions autonomes du chef de projet. Amorcé au cadrage `/2-make-gherkin` (section
> « Décisions autonomes » créée ici) ; scaffoldé par `tdd-analyse` au `/3` qui DOIT
> **préserver** la section « Décisions autonomes (chef de projet) ».

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
