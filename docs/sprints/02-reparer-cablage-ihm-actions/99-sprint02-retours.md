# Journal méthode — Sprint 02 (réparer le câblage IHM des actions)

> **But.** Consigner au fil de l'eau les retours du PO sur la **méthode** (agents,
> skills, commands du pipeline) donnés pendant le sprint. Ce fichier n'est PAS le
> backlog produit (`99-sprint02-besoins-fin-itération.md`) ni le retours produit du PO
> (`NN-retours.md`). Il est **appendé par le thread principal** à chaque retour à la
> volée, et **consommé par `retro-sprint`** en fin de sprint pour proposer les éditions
> d'agents/skills/commands en vue du sprint suivant.
>
> Créé à l'analyse `/3` (par `tdd-analyse`). Une ligne par retour.

| Date | Cible (agent/skill/command) | Retour | Décision prise |
|------|-----------------------------|--------|----------------|
| 2026-06-24 | `command 3-tdd-implement` | Retirer le blocage `AskUserQuestion` après chaque scénario pour mener le sprint de façon intégrale. | ✅ Appliqué — étapes 5/6 réécrites : récap sans blocage + boucle automatique jusqu'à tous `✅ GREEN` ; la boucle ne se suspend que sur `type:question` agent ou interruption PO. |
| 2026-06-24 | `agent tdd-auto` | Sur un early green **inattendu**, `tdd-auto` doit poser une question (AskUser) avant tout commit ; il **peut** aussi en poser s'il détecte un problème d'implémentation. | ✅ Appliqué — RED_PHASE distingue early green anticipé (caractérisation, pas de question) vs inattendu (STOP + `type:question`) ; section « Quand poser une question » ajoutée. |
| 2026-06-24 | convention de nommage (18 fichiers de définition) | Nommer les fichiers de suivi `00-sprintXX-suivi.md` et les besoins `99-sprintXX-besoins-fin-itération.md` pour éviter la collision d'onglets éditeur entre sprints. | ✅ Appliqué — convention propagée aux agents/commands/skills/scripts/README/mémoire ; scripts élargis (compat ancien+nouveau nom) ; migration sprint 02 effectuée en fin de sprint. |
| 2026-06-24 | `agent tdd-analyse` + `skill retro-sprint` + `command 3-tdd-implement` + `script find-retours` | Créer le fichier `99` dès la création du sprint, et y consigner les retours méthode à la volée pour traitement en fin de sprint. | ✅ Appliqué — `tdd-analyse` scaffolde `99-sprintXX-besoins-fin-itération.md` + `99-sprintXX-retours.md` (ce fichier) ; `retro-sprint` le consomme ; `find-retours.ps1` exclut `99-sprint*-retours.md` du glob produit. |

## Notes de contexte (décisions produit, hors méthode)

- Sc.3 & Sc.4 : early greens conservés / test #2 Sc.4 retiré — décisions **produit** prises
  pendant la boucle TDD (voir `00-sprint02-suivi.md` et les `NN-slug.md`). Listées ici
  pour mémoire, mais elles ne concernent pas la méthode.
