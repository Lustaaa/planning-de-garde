# Retours — Sprint 07 (lisibilite-theme)

> **Fichier unifié.** Il porte les retours produit (PO), la méthode (agents) et les
> décisions autonomes du chef de projet. Créé au cadrage `/2-make-gherkin`.

# Décisions autonomes (chef de projet)

> Journal des décisions tranchées par l'agent `chef-de-projet` sans escalade PO.
> Permet au PO de piloter a posteriori.

## 2026-06-26 — Périmètre des scénarios Gherkin du sujet « lisibilité + thème » (en bloc)

- **Question (make-gherkin)** : le thème métier (règle 20) n'ouvre aucune règle de
  gestion ni observable vérifiable, alors que la lisibilité (nom + légende, règle 16)
  porte l'observable d'usage. Comment cadrer les scénarios pour ce sujet pris en bloc ?
- **Décision** : option 1 — **seule la lisibilité (nom + légende) est scénarisée**.
  Tous les scénarios portent sur un Then testable (nom affiché, légende, repli gris
  règle 17). Le thème reste une **note d'ergonomie de surface** dans l'analyse technique,
  sans scénario propre. Un seul fichier, sujet conservé « en bloc », thème subordonné
  derrière la lisibilité.
- **Rationale** : applique le garde-fou corollaire de découpe acté au /4-retours (couper
  au plus petit incrément lisible, séquencer le thème derrière). Pas de scénario à Then
  non vérifiable (règle 20 = aucune règle métier). Le « en bloc » PO est préservé : un
  seul sujet/fichier, pas de re-découpe à arbitrer.
- **Sources** : spec v07 règles 16, 17, 20 ; décisions de cadrage /4-retours sprint 06
  (en bloc + garde-fou découpe) ; BACKLOG palier 3 (É5).

## 2026-06-26 — Périmètre et contenu de la légende couleur (Then testable)

- **Question (make-gherkin)** : la légende est l'autre moitié de l'observable de
  lisibilité ; quel périmètre/contenu exact pour que son Then soit testable ?
- **Décision** : option 1 — **légende = responsables réellement affectés dans la
  fenêtre 4 semaines affichée**. Une seule légende attachée à la grille, une entrée
  nom↔couleur par responsable distinct (dédoublonnée). Fenêtre sans affectation →
  légende vide/masquée. Then testable : « la légende contient exactement Alice (bleu)
  et Bruno (vert) ».
- **Rationale** : la règle 16 fait de la légende le décodeur de « qui garde » sur la
  grille affichée — son contenu doit donc être l'ensemble effectivement présent à
  l'écran, pas le catalogue déclaré. L'option 2 (tous les acteurs du foyer) affiche des
  entrées sans case correspondante, casse le couplage observable↔écran et fait dépendre
  le Then du set déclaré plutôt que de l'affichage. Le set par défaut (règle 18) régit
  l'attribution des couleurs, pas le contenu de la légende → pas d'arbitrage métier. Le
  repli neutre (règle 17) reste une entrée comme une autre si un acteur hors set est
  affecté dans la fenêtre.
- **Sources** : spec v07 règles 16, 17, 18 ; BACKLOG palier 3 (É5).

## 2026-06-26 — Le rendu nom + légende suit-il la diffusion temps réel ?

- **Question (make-gherkin)** : la grille est en lecture seule (règle 12) mais une
  écriture aboutie d'un autre acteur déclenche la diffusion temps réel (palier 1, déjà
  livrée s05) et actualise la grille. À l'arrivée d'une période diffusée, nom + légende
  doivent-ils se mettre à jour dans le périmètre de ce sujet ?
- **Décision** : option 1 — **oui, nom + légende suivent la diffusion**. À l'arrivée
  d'une période diffusée, la case affiche immédiatement nom + couleur et la légende gagne
  l'entrée du responsable si absente. Un scénario @limite couvre cet ajout vivant.
- **Rationale** : l'observable de lisibilité (règle 16) ne se restreint pas au
  chargement ; il doit tenir sur la grille réellement câblée. La diffusion temps réel
  étant déjà construite (palier 1), ce sujet n'ajoute pas d'infra — il assert seulement
  que nom/légende suivent le canal existant, fermant le trou de cohérence avec la saisie
  visible (palier 2) et posant le rempart anti-« vert qui ment » sur mise à jour live.
  Coût marginal (un scénario @limite sur canal existant) faible, pas d'arbitrage métier.
  L'option 2 (rendu au chargement seul) laisserait un angle mort sur la grille live déjà
  en place.
- **Sources** : spec v07 règles 12, 16 ; palier 1 (diffusion temps réel SignalR, s05) ;
  palier 2 (saisie visible, s06) ; BACKLOG palier 3 (É5).

## 2026-06-26 — Cas dégradé gris : rendu case + légende, et QUEL gris exercer

- **Question (make-gherkin)** : pour rendre observable le scénario gris dégradé (règle
  17), que doivent afficher la case et la légende ? Et quel gris : le gris-assumé
  (légitime) ou le gris-bug (libellé au lieu d'identifiant, déjà corrigé au s06) ?
- **Décision (deux volets)** :
  1. **Rendu** = option 1 : **nom affiché + case grise (teinte neutre) + entrée légende
     « <nom> (gris/neutre) »**. Then : « la case affiche 'grand-père' sur fond gris ET la
     légende contient grand-père (gris) ». Option 2 (case grise sans nom) rejetée : elle
     perd l'observable « un responsable est là » et confond avec une case vide → viole
     la règle 16 (nom + légende gardent « qui garde » lisible même couleur effondrée).
  2. **Quel gris** = le **gris-ASSUMÉ** (acteur légitimement hors set, id stable valide
     non encore colorié — Sc.7 s06, tagué `@limite`, conforme et **permanent**). **PAS**
     le **gris-BUG** (libellé fourni à la place de l'identifiant — Sc.8 s06, `@erreur`),
     qui a été **corrigé à la source** (Sc.6 binde/sème l'id stable).
  - **Reclassement de tag** : ce scénario palier 3 est donc `@limite`, pas `@erreur` —
    le repli neutre hors-set est conforme, ce n'est pas une erreur.
- **Rationale** : scénariser le gris-bug comme Then attendu de lisibilité re-figerait un
  défaut déjà éliminé et contredirait la règle 17 (« c'est le défaut à localiser, pas la
  résolution »). La garde anti-régression du gris-bug existe déjà = la caractérisation
  s06 Sc.8 ; pas besoin de la redoubler ici. Le sujet palier 3 exerce le **chemin
  dégradé légitime** (hors-set → neutre) et vérifie que l'observable règle 16 tient
  malgré la couleur neutre. Aucun arbitrage métier ni de cap.
- **Sources** : spec v07 règles 16, 17, 18 ; s06 Sc.6/Sc.7/Sc.8 (gris-assumé `@limite` vs
  gris-bug `@erreur` corrigé à la source) ; BACKLOG palier 3 (É5).

## 2026-06-26 — Case @erreur de la matrice : ne pas la forcer, périmètre tenu

- **Question (make-gherkin)** : repli gris requalifié `@limite`, gris-bug déjà gardé par
  s06 Sc.8 → ce sujet de rendu lecture seule n'a aucun `@erreur` propre. Comment combler
  la case `@erreur` de la matrice sans re-figer un défaut éliminé ?
- **Décision** : option 2 — **pas d'@erreur forcé**. La matrice se ferme sur nominal +
  limites ; l'absence d'`@erreur` natif est **justifiée et consignée**, pas un trou à
  maquiller. Les surfaces d'erreur pertinentes sont détenues par les paliers adjacents :
  API injoignable / saisie refusée = règle 25 (s04-s05) ; gris-bug = s06 Sc.8. Le sujet
  lisibilité **rend** des données déjà validées et n'ouvre aucun nouveau chemin d'écriture
  → aucun `@erreur` natif est l'état honnête.
- **Discipline de périmètre (point tranché)** : l'option 1 (échec de lecture clair,
  « planning indisponible » vs grille vide) introduit un **observable d'échec de lecture**
  = un sujet distinct « robustesse du chemin de lecture (règle 25 côté read) », avec du
  code de prod neuf. Le palier 0 conservateur + le garde-fou découpe imposent de **ne pas
  bolt-on** une feature de robustesse sur un sujet de rendu. La complétude de matrice est
  un moyen, pas une fin : on ne pad pas un `@erreur` artificiel.
- **Séquencé derrière (backlog candidat)** : « échec de lecture clair — règle 25 côté
  read » est consigné comme **risque / candidat backlog** à dimensionner comme sujet
  propre. L'extension de la règle 25 au chemin de lecture sera à **confirmer par le PO**
  au moment où ce sujet sera pris (pas d'arbitrage métier requis ici pour fermer CETTE
  matrice — c'est justement pourquoi on défère).
- **Sources** : spec v07 règles 16, 17, 25 ; règle 25 / s04-s05 (échec clair côté write) ;
  s06 Sc.8 (gris-bug) ; BACKLOG palier 3 (É5).

## 2026-06-26 — Validation de la synthèse make-gherkin → écriture ordonnée

- **Gate** : revue de la synthèse make-gherkin (6 scénarios) avant écriture du fichier de
  scénarios (/2 étape 3). Conformité vérifiée :
  - Q1 (thème hors scénario, couche CSS) ✓ ; Q2 (légende = présents dans la fenêtre,
    dédoublonnée, masquée si vide → Sc.1/2/3) ✓ ; Q3 (suivi temps réel sans rechargement
    → Sc.4) ✓ ; Q4 (gris-assumé : nom conservé + neutre + légende neutre, `@limite` →
    Sc.5) ✓ ; Q5 (pas d'@erreur forcé, matrice close nominal+limites, candidat backlog
    read-robustness consigné) ✓.
  - Sc.6 (nom long : tronqué + survol, légende = nom complet) : ajout non décidé en amont
    mais **dérivé de la règle 16** (nom affiché lisible) — ergonomie de surface, `@limite`,
    aucun cap ni règle métier neuve. **Accepté**.
- **Décision** : aucun arbitrage métier (G1) ni cap (G2) résiduel ; tout dérive de la spec
  v07 actée + des 5 décisions journalisées. **Écriture du fichier de scénarios ordonnée**.
- **Vigilance process** : ce 99-sprint07-retours.md a été amorcé par le CP au /2 ;
  tdd-analyse scaffolde ce fichier unifié au /3 — **préserver la section « Décisions
  autonomes (chef de projet) »** lors du scaffolding (ne pas écraser).

# Retours produit (PO)

-

# Méthode (agents) — pour retro-sprint

| Date | Cible (agent/skill/command) | Retour | Décision prise |
|------|-----------------------------|--------|----------------|

## IA

| Date | Cible (agent/skill/command) | Observation | Recommandation |
|------|-----------------------------|-------------|----------------|
