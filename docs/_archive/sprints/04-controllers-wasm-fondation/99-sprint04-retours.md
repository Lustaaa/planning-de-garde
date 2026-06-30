# Retours — Sprint 04 (controllers-wasm-fondation)

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

## IHM - général

- Le thème est toujours degelasse.
- Je peux poser plusieurs fois les meme slots.
- En regle de gestion, une période affecté ne peut etre réafacté a l'autre parent que s'il y a eu une demande et que celle ci a été accepté.

## IHM - /planning

- On ne vois pas la coloration du parent en responsabilité.
- Les affectations de période ne s'affiche pas dans le planning.
- Les transfert ne s'affiche pas dans le planning.
- Je pense que le nombre de semaine par défaut devrait etre 4 et pas 5.
  - Je pense aussi que il pourrait y avoir des vue prédefinit (semaine, mois, 4semain glissante a partir de la semaine en cours)
  - La navigation dans le futur et le passé dans aussi etre implémenté.

## IHM - /planning/poser-slot

- Je pense que les dates par defaut devrait etre la date du jour.

## IHM - /planning/affecter-periode

- Je pense que les dates par defaut devrait etre la date du jour.

## Tech (optionnel)

- Actuellement je ne peux pas démarrer uniquement le BackEnd (Le backend n'expose pas le route d'api, mais c'est le front => PlanningDeGarde.Web.CanalEcriture.cs) J'aimerais que tu me propose un solution
- Est ce que c'est normal que je n'ai pas de vu style SWAGGER pour attaquer directement mes api ?

# Méthode (agents) — pour retro-sprint

> Retours à la volée du PO sur la **méthode** (agents/skills/commands), appendés par le
> thread principal pendant le sprint. Ne PAS confondre avec les retours produit ci-dessus.

| Date | Cible (agent/skill/command) | Retour | Décision prise |
|------|-----------------------------|--------|----------------|
| 2026-06-25 | `/6-cloture-sprint` (retro-sprint) + `/2-make-gherkin` | Le `docs/BACKLOG.md` doit être **consolidé après la rétro et avant la fin du sprint**, pour que la création du sprint suivant (`/2-make-gherkin`) puisse s'appuyer dessus. | À traiter en rétro : ajouter une étape de consolidation du BACKLOG (épics + paliers + statuts) dans `/6` après `retro-sprint`, en amont du handoff vers `/2`. |

## IA

> Observations méthode relevées par l'IA (non demandées par le PO), candidates pour `retro-sprint`.

| Date | Cible (agent/skill/command) | Observation | Recommandation |
|------|-----------------------------|-------------|----------------|
| 2026-06-25 | `ihm-builder` (agent) / `/3-tdd-implement` | Agent `ihm-builder` absent du registre de session → dispatch tombé en fallback `general-purpose` pour la phase IHM finale (déjà signalé au sprint 03). Tous les scénarios étant backend, l'agent n'a pas non plus été sollicité en RED→GREEN. `validation-visuelle` aussi absent → même fallback pour le gate. | Charger/enregistrer `ihm-builder` (et `validation-visuelle`) dans le registre de session, ou documenter explicitement le mode fallback comme régime attendu si l'agent n'est pas chargeable. |
| 2026-06-25 | `.claude/skills/run/scripts/run.ps1` | Le lanceur plantait au gate visuel : `Set-Location (git rev-parse --show-toplevel)` — la sortie UTF-8 de git, avec l'accent du chemin (« privée »), est mal décodée par PowerShell → `Cannot find path 'priv?e'`. De plus une instance Web zombie d'un run précédent verrouillait les DLL (build error MSB3027). | Fix appliqué : racine dérivée de `$PSScriptRoot/../../../..` (encodage-safe, sans git). À envisager en rétro : que le lanceur tue les instances Web résiduelles avant le build (cf. pattern `dev-start` GtaX). |

## Notes de contexte (décisions produit, hors méthode)

-
