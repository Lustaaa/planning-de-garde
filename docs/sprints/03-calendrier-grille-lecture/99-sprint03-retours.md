# Retours — Sprint 03 (calendrier, grille de lecture)

> **Fichier unifié.** Il porte deux choses, consommées par deux étapes différentes :
> - **Retours produit (PO)** ci-dessous → lus par `/4-retours` (challenge + besoins).
> - **Méthode (agents)** + **`## IA`** plus bas → lus par `retro-sprint` en fin de sprint.
>
> Créé à l'analyse `/3` (par `tdd-analyse`). La partie produit est préparée vide ici et
> remplie par le PO après le gate visuel ; la partie méthode est appendée au fil de l'eau
> par le thread principal. Lancement de l'app : `pwsh .claude/skills/run/scripts/run.ps1`.

# Retours produit (PO)

> Le code et les tests unitaires sont **hors scope** ici (revus en revue de code).
> Ces retours portent sur l'**usage de l'IHM** : ce qui marche, ce qui coince, ce qui
> manque à l'écran. Remplis les puces, puis lance `/4-retours`.

Retour humain, il serai peut etre pertinent de faire un sprint sur les point tech uniquement (refacto des choses qui me parraissent essentiel) et d'enchainer sur le sprint de lecture et ecriture de l'alimentation des utilisateurs puis de l'alimentation des données de garde.

## IHM - général

- Le theme est toujours très moche.
- Les périodes de responsabilités des parents n'est pas représenté ou invisible.
- Comment les transferts sont notifié ou affiché ? Pour l'instant, j'ai l'impression que rien ne les exposent.

## IHM - /planning

- Les périodes de responsabilités des parents n'est pas représenté ou invisible
- Toutes les saisie dans les différents écrans n'apparaissent pas dans l'écran.

## Tech (Urgent avant que l'app ne soit trop grosse)

- J'aimerai que l'application soit un WASM (on est pas a l'abris que j'ai envie de faire un IHM en VueJs ou que je fasse un MCP qui attaque les API).
- J'aimerai que les fichier .razor soit automatiquement adossé a un .razor.cs et pas de @code
- J'aimerai un swagger sur la partie backend 
- J'aimerai que les commandes soit appelé a travers des controllers 
- Remaque général, mais j'ai pas l'impression qu'il y ai d'adaptateur de gauche ou d'adaptateur de droite (littérature archi port adapter - hexagonal)
- Je suis pas un super fan de mettre des donnés dans les calsses, a l'image de Foyer.cs. Je prefere que ce soit en base (ici, j'espere que c'est le temps davancer sur la configuration des utilisateurs)

# Méthode (agents) — pour retro-sprint

> Retours à la volée du PO sur la **méthode** (agents/skills/commands), appendés par le
> thread principal pendant le sprint. Ne PAS confondre avec les retours produit ci-dessus.

| Date | Cible (agent/skill/command) | Retour | Décision prise |
|------|-----------------------------|--------|----------------|
| 2026-06-24 | `/2-make-gherkin` (déroulé pipeline) | Faire le `/clear` **après** la rédaction du plan Gherkin du sprint, pas avant. | Adopté dès le sprint 03 : ne plus clearer en fin de `/2` ; le clear intervient une fois `docs/sprints/NN-<sujet>.md` écrit. À porter dans le déroulé `/2`/`/3` par retro-sprint. |

## IA

> Observations méthode relevées par l'IA (non demandées par le PO), candidates pour `retro-sprint`.

| Date | Cible (agent/skill/command) | Observation | Recommandation |
|------|-----------------------------|-------------|----------------|
| 2026-06-24 | `/2-make-gherkin` (entrée spec) | L'agent a démarré sur `docs/02-specification.md` alors qu'une `/5-consolidation` produisait `docs/03-specification.md` en tâche de fond (course) ; redirigé à chaud vers v03, challenge repris à zéro. | `/2` devrait résoudre la spec courante = plus grand `NN-specification.md` au dispatch, pas `01-`/`02-` en dur, pour éviter la course avec `/5`. |
| 2026-06-25 | `tdd-analyse` (annotation FLFI) | Sc.3 test #2 (injectivité couleur) a fait early-green sans l'annotation `⚠️ probablement early green` dans sa cellule Contradiction, alors que le couplage au #1 était prévisible (même mapping). Garde-fou `tdd-auto` a quand même suspendu → PO a supprimé #2 (doublon). | `tdd-analyse` devrait annoter systématiquement « probablement early green » tout test qui n'est qu'une branche/non-régression d'un mapping déjà introduit par un test antérieur, pour éviter une suspension de boucle évitable. |
| 2026-06-25 | `tdd-auto` (tag @vert / flux git) | À chaque scénario (1→5), le commentaire de hash dans le tag `@vert` du fichier source pointe un commit d'une génération antérieure à HEAD — effet du `--amend` auto-référentiel (on amende le commit qui contient déjà le tag). L'agent y consacre de l'effort à chaque run pour le constater « insoluble ». | Revoir le flux de tag dans `tdd-auto` : taguer `@vert` SANS hash (ou en 2 temps : commit puis edit du tag sans ré-amender), pour supprimer la boucle et le bruit récurrent. |
| 2026-06-25 | `tdd-analyse` (désignation drivers vs caractérisation) | Sc.6 (période à cheval) : tests #1 (intersection partielle) et #2 (coexistence) désignés DRIVERS, mais déjà acquis par construction (Sc.1 fenêtre + Sc.3 mapping par-jour) → tout vert sans rouge, garde-fou `tdd-auto` a suspendu, PO a supprimé Sc.6 comme doublon. | `tdd-analyse` devrait reconnaître qu'un scénario dont l'observable découle entièrement d'invariants déjà introduits (fenêtre + mapping par-jour) est de la CARACTÉRISATION, pas un driver — l'annoter comme tel d'emblée, ou ne pas en faire un scénario codant. |
| 2026-06-25 | `tdd-analyse` (prédiction de contradiction) | Sc.8 (repli gris) : la cellule Contradiction prédisait un échec « couleur nulle/vide/exception », hypothèse FAUSSE — le contrat du port `IPaletteCouleurs.CouleurDe` renvoie déjà Neutre si acteur absent (réalisé pareil par impl réelle ET doublure). Early-green non anticipé → suspension, PO a supprimé Sc.8 (doublon du contrat de port). | `tdd-analyse` devrait vérifier le CONTRAT des ports déjà introduits avant de prédire une contradiction : un comportement déjà garanti par un port existant n'est pas un driver. Récurrent avec Sc.6 — 2 scénarios sur 8 retirés pour early-green non vu. |
| 2026-06-25 | `README-claude.md` (schéma de workflow) — **demande PO** | Le PO veut que `README-claude.md` porte **en permanence un schéma du workflow tenu à jour**, mais **concis** : prendre le minimum de place tout en restant **très parlant pour un humain NON informaticien**. Le schéma mermaid actuel est verbeux et orienté dev (noms d'agents, fichiers techniques), peu lisible pour un non-tech. | `retro-sprint` : remplacer/compléter le schéma par une version compacte et non technique (étapes en langage métier : Idée → Cadrage → Scénarios → Construction+contrôle visuel → Retours → Nouvelle version → reboucle), tenue à jour **à chaque évolution du pipeline** (responsabilité de `retro-sprint`). Garder le détail technique en table/annexe sous le schéma simple. |
| 2026-06-25 | `/5-consolidation` (étape propagation) | Le pointeur « Spec courante » de `README.md` était périmé de DEUX versions (encore `02-specification.md` alors que v03 puis v04 existaient) → l'étape 6 « Propagation » de `/5` a été sautée au sprint précédent. Corrigé à la main vers v04 ce sprint. | Fiabiliser l'étape Propagation de `/5-consolidation` : la rendre mécanique (script qui réécrit le pointeur `README.md` vers `currentSpec` issu de `find-spec.ps1`), pour qu'elle ne soit jamais oubliée. |
| 2026-06-25 | Pipeline — artefact **product backlog** manquant — **demande PO** | Il n'existe AUCUN backlog produit cumulé unique « fait / reste à faire » pour planifier les sprints. Ce qui existe est éparpillé et non cumulatif : `99-sprint<NN>-besoins-fin-itération.md` (photo de fin d'itération, par sprint), la section *Séquence de livraison* de la spec vivante (forward, réécrite à chaque version, ne trace pas le fait), et le « fait » seulement via git + `archive/`. Le PO note que ce serait utile pour définir les prochains sprints. | `retro-sprint` : introduire un **product backlog permanent** tenu par le pipeline (p.ex. `docs/BACKLOG.md` ou `docs/ROADMAP.md`) — liste cumulée des besoins avec statut (fait / en cours / à faire) et le sprint de rattachement, mis à jour par `/4-retours` (ajout des besoins) et `/6-cloture-sprint` (passage à « fait »). C'est l'artefact **product backlog** du mapping SCRUM (ligne suivante). |
| 2026-06-25 | Workflow global / vocabulaire — **demande PO** | Le PO veut **consolider le workflow avec les concepts de la méthode SCRUM** : nommer explicitement les étapes du pipeline avec le vocabulaire SCRUM pour ancrer la méthode (sprint, sprint goal, product backlog, increment, Definition of Done, sprint review, sprint retrospective…). Aujourd'hui le pipeline EST agile mais ne nomme pas ses rituels. | `retro-sprint` : établir et documenter (dans `README-claude.md` + les commands) le mapping pipeline ↔ SCRUM, p.ex. `/1-spec`+`/5-consolidation` = raffinement/product backlog ; un sujet = **sprint goal** ; `/2`+`/3` = sprint (planning + développement) ; gate visuel = **sprint review** + **Definition of Done** ; `/4-retours` = retours produit (≠ rétro) ; `99-…-besoins` = **product backlog** ; `retro-sprint` = **sprint retrospective** (sur la méthode) ; `99-…-retours` (Méthode+IA) = matière de la rétro. Le schéma concis (ligne ci-dessus) doit refléter ce vocabulaire. |

## Notes de contexte (décisions produit, hors méthode)

- 2026-06-25 — La refonte de /planning en grille **lecture seule** (incrément 1, règle 12) a retiré de l'écran les fonctions d'**écriture** livrées au sprint-02 qui n'existaient que sur /planning : édition inline d'une période, déplacement de slot, avertissement de chevauchement, affichage du responsable actuel. Décision PO : on tient le sprint goal (lecture seule) ; ces actions d'écriture seront **recâblées dans un sprint d'écriture ultérieur** (non encore planifié). 5 tests bUnit obsolètes (`PlanningPartageTests.cs`) supprimés. **Trou fonctionnel assumé jusque-là : aucune édition de période dans l'app.** → à reprendre dans le backlog `/4-retours`.
