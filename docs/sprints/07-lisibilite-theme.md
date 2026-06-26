# Sprint 07 — Lisibilité & thème

> Palier 3 « lisibilité & thème » de la spec v07 (règles 16, 17, 20), pris **en
> bloc** par choix PO. Application du garde-fou corollaire de découpe acté au
> /4-retours s06 : seule la **lisibilité** (nom du responsable + légende couleur)
> porte des scénarios testables ; le **thème métier** reste une note d'ergonomie
> de surface **sans scénario** (règle 20 : aucun observable métier vérifiable).
> À ne pas confondre avec la couleur, **déjà livrée** au palier 2 (identifiant
> stable → palette) : l'observable de ce sprint est le **nom + la légende** qui
> disent *qui* garde, pas la teinte.

## Analyse technique

- **Composants impactés** — (1) Projection/modèle de **lecture** de la grille
  (CQRS read) enrichi du **nom du responsable** et de son **identifiant stable**
  (la couleur est déjà résolue depuis le palier 2). (2) Composant Blazor grille
  (front WASM) : affiche le nom dans la case en plus de la teinte. (3) Nouveau
  composant Blazor **« Légende »** dérivé des responsables présents dans la
  fenêtre, dédoublonnés. (4) Consommation du **canal de diffusion SignalR
  existant** (lecture seule, livré palier 1) pour réactualiser cases + légende —
  aucune infra temps réel construite ici.
- **Couches & dépendances** — Tout vit dans la **lecture** : la projection
  (Application) et son rendu (Infrastructure–Blazor/WASM). Aucune écriture
  (règle 12, grille en lecture seule) : le domaine et les agrégats ne bougent
  pas, les dépendances pointent vers l'intérieur. Litmus : la projection enrichie
  est testable sans framework ; le composant Blazor est remplaçable sans toucher
  au domaine.
- **Contrats de données** — Le DTO de lecture d'une période exposé à la grille :
  `{ identifiantStableResponsable, nomResponsable, couleurRésolue | neutre, date,
  créneau }`. La **légende** est une donnée **dérivée** (pas un contrat
  d'écriture) : ensemble `{ identifiantStable, nom, couleur }` **distinct** sur
  la fenêtre 4 semaines affichée.
- **Write vs read (CQRS)** — 100 % **lecture/affichage** → projection dédiée,
  **jamais** de getter de vue sur l'agrégat. Test « cette donnée sert-elle un
  invariant ? » → non : modèle de lecture.
- **Invariants** — Toute case occupée affiche le **nom** du responsable (règle
  16), jamais la couleur seule ; l'info textuelle ne dépend pas de la résolution
  couleur. La couleur se résout sur l'**identifiant stable**, jamais sur le
  libellé (règle 17) : un acteur légitimement hors set retombe en teinte neutre
  **mais conserve son nom**. La légende couvre **exactement** les responsables
  présents dans la fenêtre, **une entrée par personne distincte** (ni doublon ni
  manquant) ; fenêtre sans affectation → légende masquée.
- **Points d'attention TDD** — *Vert qui ment* : asserter le nom sur une grille
  **réellement câblée** (front WASM + API distante, sans doublure sur le chemin
  observé), pas une grille statique. Ne **doubler que les ports** (source des
  périodes) ; le canal de diffusion existant n'est pas reconstruit, seulement
  **asserté** (ajout vivant). Ne **pas redoubler** la garde gris-bug
  (caractérisation s06 Sc.8) : le gris exercé ici est le **gris-ASSUMÉ** (acteur
  hors set, identifiant valide), conforme et permanent.

### Note d'ergonomie de surface — thème métier (règle 20, sans scénario)

Le **thème** cohérent avec le domaine (garde d'enfants) est une **ergonomie de
surface** : couche de présentation/CSS, **aucune règle métier, aucun observable
métier vérifiable** → **aucun scénario Gherkin** (un `Then` « le thème est
joli/cohérent » n'est pas testable). Il est **subordonné à l'usage** par
l'arbitre et **séquençable derrière** la lisibilité si le périmètre déborde
(corollaire de découpe). Validation **visuelle**, hors acceptation scénarisée.

### Risques & questions ouvertes

- **Vert qui ment sur la grille** — le nom doit apparaître sur un câblage **réel**
  (front WASM + API distante), pas sur une doublure. Rempart à tenir en
  acceptation runtime/IHM.
- **Thème = ergonomie de surface** — sans observable métier ; aucun scénario,
  validation visuelle. « Le thème est dégueulasse » est une **absence de
  feature**, jamais un bug.
- **Pas de scénario `@erreur` natif (matrice close sur nominal + limites,
  justifié)** — les surfaces d'erreur sont **détenues par les paliers adjacents** :
  API injoignable / saisie refusée = règle 25 (s04-s05), **gris-bug = s06 Sc.8**.
  Re-scénariser le gris-bug ici re-figerait un défaut déjà éliminé (règle 17).
- **Candidat backlog — « robustesse du chemin de lecture » (règle 25 côté read)** —
  sujet **distinct** à dimensionner avec le PO quand séquencé : un **échec de
  lecture** observable (« planning indisponible, réessayer ») distinct d'une
  grille **vide** silencieuse qui mentirait en suggérant « personne ne garde ».
  **Hors** de ce sujet de rendu.
- **Données du foyer en mémoire (volatiles)** — dette de persistance à terme
  (palier technique 10, derrière l'usage), sans impact sur les scénarios de rendu.

## Scénarios

Feature: Lisibilité de la responsabilité dans la grille — le nom du responsable
s'affiche dans la case et une légende couleur dit *qui* garde. La teinte seule ne
suffit pas (règle 16) ; ce sprint ajoute le **nom** + la **légende** par-dessus la
couleur déjà résolue sur l'identifiant stable (palier 2), sur une grille en
lecture seule et alimentée en temps réel par le canal de diffusion existant.

### Scenario 1 — Une période affectée affiche le nom et entre dans la légende

`@nominal` `🖥️ IHM` `@rouge`

```gherkin
Scenario: Une période affectée à Alice affiche son nom et une entrée de légende
  Given une fenêtre de 4 semaines glissantes affichée à partir du lundi 29/06/2026
  And une période de garde le lundi 29/06/2026 dont le responsable est Alice (Parent A, couleur bleue)
  When la grille est affichée
  Then la case du lundi 29/06/2026 affiche le nom "Alice" sur fond bleu
  And la légende contient exactement une entrée Alice (bleu)
```

### Scenario 2 — Plusieurs responsables : légende dédoublonnée

`@nominal` `🖥️ IHM` `@rouge`

```gherkin
Scenario: Deux responsables distincts donnent deux entrées, Alice n'apparaît qu'une fois
  Given une fenêtre de 4 semaines glissantes affichée à partir du lundi 29/06/2026
  And Alice (Parent A, bleu) est responsable le lundi 29/06/2026 et le mercredi 01/07/2026
  And Bruno (Parent B, vert) est responsable le mardi 30/06/2026
  When la grille est affichée
  Then les cases du lundi 29/06/2026 et du mercredi 01/07/2026 affichent "Alice" sur fond bleu
  And la case du mardi 30/06/2026 affiche "Bruno" sur fond vert
  And la légende contient exactement deux entrées : Alice (bleu) une seule fois et Bruno (vert)
```

### Scenario 3 — Fenêtre sans aucune affectation : légende masquée

`@limite` `🖥️ IHM` `@rouge`

```gherkin
Scenario: Une fenêtre sans aucune période affectée n'affiche aucun nom ni légende
  Given une fenêtre de 4 semaines glissantes affichée à partir du lundi 29/06/2026
  And aucune période de garde n'est affectée dans cette fenêtre
  When la grille est affichée
  Then aucune case ne porte de nom de responsable
  And la légende est masquée, sans aucune entrée affichée
```

### Scenario 4 — Ajout vivant par diffusion temps réel : nom et légende suivent

`@limite` `🖥️ IHM`

```gherkin
Scenario: Une affectation diffusée par un autre acteur fait apparaître le nom et l'entrée de légende sans rechargement
  Given la grille est affichée à partir du lundi 29/06/2026 avec Alice (Parent A, bleu) seule responsable le lundi 29/06/2026
  And la légende contient l'unique entrée Alice (bleu)
  When un autre acteur affecte Bruno (Parent B, vert) comme responsable le jeudi 02/07/2026 et la période est diffusée sur le canal de lecture
  Then sans rechargement de la page, la case du jeudi 02/07/2026 affiche "Bruno" sur fond vert
  And la légende gagne une seconde entrée Bruno (vert)
```

### Scenario 5 — Acteur hors set (gris assumé) : nom conservé, teinte neutre

`@limite` `🖥️ IHM` `@rouge`

```gherkin
Scenario: Un acteur hors set garde son nom et une entrée de légende malgré la couleur neutre
  Given une fenêtre de 4 semaines glissantes affichée à partir du lundi 29/06/2026
  And grand-père (acteur "Autre", identifiant stable valide non encore colorié dans le set) est responsable le samedi 04/07/2026
  When la grille est affichée
  Then la case du samedi 04/07/2026 affiche le nom "grand-père" sur fond gris neutre
  And la légende contient une entrée grand-père (gris)
  And ce gris traduit un acteur non encore colorié, pas un défaut de résolution
```

### Scenario 6 — Nom long : lisibilité de la case préservée

`@limite` `🖥️ IHM`

```gherkin
Scenario: Un responsable au nom long reste lisible dans la case et complet dans la légende
  Given une fenêtre de 4 semaines glissantes affichée à partir du lundi 29/06/2026
  And Marie-Hélène Grand-Dubois (Parent A, bleu) est responsable le vendredi 03/07/2026
  When la grille est affichée
  Then la case du vendredi 03/07/2026 affiche le nom sans déborder, tronqué en "Marie-Hélène…" avec le nom complet accessible au survol
  And la légende porte le nom complet "Marie-Hélène Grand-Dubois" (bleu)
```
