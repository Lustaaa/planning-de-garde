# Retours — Sprint 02 (réparer le câblage IHM des actions)

> **Fichier unifié.** Il porte deux choses, consommées par deux étapes différentes :
> - **Retours produit (PO)** ci-dessous → lus par `/4-retours` (challenge + besoins).
> - **Méthode (agents)** + **`## IA`** plus bas → lus par `retro-sprint` en fin de sprint.
>
> Créé à l'analyse `/3` (par `tdd-analyse`). La partie produit est préparée par le gate
> visuel et remplie par le PO ; la partie méthode est appendée au fil de l'eau par le
> thread principal. Lancement de l'app : `pwsh .claude/skills/run/scripts/run.ps1`.

# Retours produit (PO)

> Le code et les tests unitaires sont **hors scope** ici (revus en revue de code).
> Ces retours portent sur l'**usage de l'IHM** : ce qui marche, ce qui coince, ce qui
> manque à l'écran. Remplis les puces, puis lance `/4-retours`.
>
> **Sprint 02 — réparer le câblage IHM des actions d'écriture.** 4/4 scénarios verts.
> Routes : `/planning`, `/planning/poser-slot`, `/planning/affecter-periode`,
> `/planning/definir-transfert`.

## IHM - général

- Subjectif : J'aime pas le theme que tu as mis. 
  - Est ce qu'on peut faire un truc en accord avec le sujet ?
  - J'aimerai une landing page et la possibilité d'avoir mes users via connection email
    - Gmail - Apple (pour connection mobile) - microsoft 

## IHM - /planning

- Je ne comprend pas a quoi correspond la dropdown dans le tableau "Localisation — slots de Léa"
  - J'aurai plutot imaginé cette vu comme un genre de tableau de bord comme un calendrier qui affiche la semaine en cours et les 4 semaines suivante avec la possibilité de naviguer dans le mois comme on peut le faire dans un agendat.
- Je ne comprend pas le tableau "Responsabilité — périodes de garde" 
  - Est ce que ca ne peux pas etre affiché par un code couleur dans le genre de calendrier ?
  - Il manque de quoi les supprimer
  - Est ce que ce tableau ne dois pas donner lieu a un page de paramétrage des parents ?
    - Je trouve meme que ca pourrai etre pertinent que l'admin puisse configurer les 2 parents. Ce qui implique que sur un planning il y a :
      - Au moins 1 enfant
      - Toujours 2 parents, mais si un seul inscrit il peux saisir les infos de l'autre parent
      - N acteurs autres, que les parents peuvent éditer ou que l'acteur lui même peut éditer
- Le tableau "Transferts de bascule" pourrai être mis dans un dialog des événements à venir avec un bouton d'ouverture a coté d'une cloche pour les notifications

## IHM - /planning/poser-slot

- Est ce que cette page ne pourrait pas être un composant dans une dialog de "/planning"

## IHM - /planning/affecter-periode

- Est ce que cette page ne pourrait pas faire partie d'un workflow de configuration et d'information sur les différents acteur ? 

## IHM - /planning/definir-transfert

- Les transferts ne devrait pas être quelque chose de ponctuel (urgence - changement d'emploi du temps - ...) et calculé automatiquement dans la majorité des cas.
  - Si oui, est ce que ca ne devrait pas être un composant d'une dialog de "/planning" 
  

## Tech (optionnel)

- (contraintes techniques éventuelles ; laisser vide si aucune → bypass dans `/4-retours`)

# Méthode (agents) — pour retro-sprint

> Retours à la volée du PO sur la **méthode** (agents/skills/commands), appendés par le
> thread principal pendant le sprint. Ne PAS confondre avec les retours produit ci-dessus.

| Date | Cible (agent/skill/command) | Retour | Décision prise |
|------|-----------------------------|--------|----------------|
| 2026-06-24 | `command 3-tdd-implement` | Retirer le blocage `AskUserQuestion` après chaque scénario pour mener le sprint de façon intégrale. | ✅ Appliqué — étapes 5/6 réécrites : récap sans blocage + boucle automatique jusqu'à tous `✅ GREEN` ; la boucle ne se suspend que sur `type:question` agent ou interruption PO. |
| 2026-06-24 | `agent tdd-auto` | Sur un early green **inattendu**, `tdd-auto` doit poser une question (AskUser) avant tout commit ; il **peut** aussi en poser s'il détecte un problème d'implémentation. | ✅ Appliqué — RED_PHASE distingue early green anticipé (caractérisation, pas de question) vs inattendu (STOP + `type:question`) ; section « Quand poser une question » ajoutée. |
| 2026-06-24 | convention de nommage (18 fichiers de définition) | Nommer les fichiers de suivi `00-sprintXX-suivi.md` et les besoins `99-sprintXX-besoins-fin-itération.md` pour éviter la collision d'onglets éditeur entre sprints. | ✅ Appliqué — convention propagée aux agents/commands/skills/scripts/README/mémoire ; scripts élargis (compat ancien+nouveau nom) ; migration sprint 02 effectuée en fin de sprint. |
| 2026-06-24 | `agent tdd-analyse` + `skill retro-sprint` + `command 3-tdd-implement` + `script find-retours` | Créer le fichier `99` dès la création du sprint, et y consigner les retours méthode à la volée pour traitement en fin de sprint. | ✅ Appliqué — `tdd-analyse` scaffolde le fichier ; `retro-sprint` le consomme ; `find-retours.ps1` cible le bon fichier. |
| 2026-06-24 | `gate validation-visuelle` + `find-retours` + `retours-challenge` | Unifier retours produit et journal méthode dans un seul `99-sprintXX-retours.md` (au lieu d'un `NN-retours.md` séparé). | ✅ Appliqué — fichier unifié (produit PO + méthode + `## IA`) ; gate prépare la section produit dedans ; `find-retours` le lit comme produit ; `05-retours.md` migré/supprimé. |

## IA

> Observations méthode relevées par l'IA (non demandées par le PO), candidates pour `retro-sprint`.

| Date | Cible (agent/skill/command) | Observation | Recommandation |
|------|-----------------------------|-------------|----------------|
| 2026-06-24 | `/4-retours` (retours-challenge) / `/5-consolidation` | **Cause A (donnée).** Le besoin « réparer le câblage IHM » (`01-semaine-de-garde/99-besoins-fin-itération.md:34`) vient de retours sprint 1 (`13-retours.md:31-42`) testés sur du code **périmé** (avant l'IHM Blazor `47d4915`). Le besoin a été classé « bug » sans être confronté au code courant. | Avant de classer un retour en « bug à réparer », **confronter le retour au code courant** (grep du message d'erreur + inspection du composant). Un besoin de réparation doit citer le défaut dans le code HEAD, pas seulement un symptôme observé. |
| 2026-06-24 | `tdd-auto` / `tdd-analyse` / `/3` | **Cause B (process/structure).** `tdd-auto` a pour règle « **jamais l'IHM Blazor** » (repoussée à `ihm-builder`), mais `tdd-analyse` a produit des tests **bUnit de composant** (`RenderComponent<PoserSlot>`, `PoserSlotTests.cs:32`) pour un sprint dont l'objet EST l'IHM. Aucun agent ne pilote l'IHM dans la boucle TDD ; `ihm-builder` ne tourne qu'en phase finale et n'est pas piloté par des tests rouges. | Pour un sprint touchant l'IHM, **router la boucle vers un agent autorisé à écrire le `.razor`** (un `ihm-builder` piloté par tests rouges), ou lever la contradiction « jamais IHM » de `tdd-auto`/`tdd-analyse` quand le sprint est un fix IHM. |
| 2026-06-24 | `tdd-analyse` / `tdd-auto` (niveau des tests) | **Cause C (niveau de test — à confirmer).** Les tests d'acceptation sont des bUnit composant avec **doublures** (`FoyerLieuRepository`, `InMemorySlotRepository`). Ils passent même si l'app réelle (DI réel, repos réels, SignalR) échoue → le bug d'usage rapporté en runtime au sprint 1 n'est **pas reproductible** par ces tests. Risque de « vert qui ment ». | Pour un bug d'**usage** rapporté en runtime, l'acceptation doit s'exécuter sur le **chemin réel** (test d'intégration / E2E sur l'app câblée), pas un composant isolé avec fakes. Le rouge doit reproduire le symptôme PO. |

## Notes de contexte (décisions produit, hors méthode)

- Sc.3 & Sc.4 : early greens conservés / test #2 Sc.4 retiré — décisions **produit** prises
  pendant la boucle TDD (voir `00-sprint02-suivi.md` et les `NN-slug.md`). Listées ici
  pour mémoire, mais elles ne concernent pas la méthode.
