# Scenario 1 — Poser un slot depuis une case du planning `@nominal 🖥️ IHM`

[← Retour au suivi](00-sprint11-suivi.md)

> **Routé vers `ihm-builder`.** Niveau d'acceptation = **E2E/runtime** sur l'app réellement
> câblée. Le comportement neuf vit dans le `.razor` (clic-case → ouverture de dialog →
> validation → réapparition dans la case). **Aucun handler/règle backend neuf** : on réutilise
> la commande `PoserSlot` et le canal HTTP `POST /api/canal/poser-slot` (s04→s10). La table de
> tests ci-dessous (bUnit composant) est **optionnelle** et complémentaire — le détail
> RED→GREEN est piloté par `ihm-builder`.

## Acceptation (BDD) — test de NIVEAU RUNTIME

**Should_Faire_apparaitre_le_slot_ecole_08h30_16h30_dans_la_case_du_mardi_16_06_2026_When_un_parent_pose_un_slot_via_la_dialog_ouverte_depuis_cette_case** — ✅ GREEN (`FrontWasmPoserSlotDepuisCaseTests`)

Sur l'app **réellement câblée** (front WASM réel + API distante + store réel + projection
`GrilleAgendaQuery`, façon `FrontWasmApiDistanteTests` / `ApiDistanteFactory` du s05) : un
Parent **clique la case du mardi 16/06/2026** → la dialog « Poser un slot » s'ouvre →
choisit le lieu « École » de 08:30 à 16:30 → **valide**. **Observable runtime** : la dialog
se ferme **et** un slot « École » 08:30→16:30 est **réellement enregistré** dans le store de
l'API distante, **relu par la projection** et **positionné dans la case du mardi 16/06/2026**
(pas un accusé du canal, pas une grille statique). *Anti vert-qui-ment* : si le clic n'ouvre
rien, si la pose ne transite pas jusqu'au store distant, ou si le slot retombe à une autre
date, l'observable distant reste vide → rouge.

## Tests bUnit composant (optionnels — pilotés par `ihm-builder`)

| # | Test composant (FLFI) | TPP | Contradiction | Status |
|---|------------------------|-----|---------------|--------|
| 1 | Should_Ouvrir_la_dialog_poser_un_slot_When_un_parent_clique_une_case_du_planning | (unconditional → statement) ouvrir au clic | Sans câblage du clic, aucune dialog n'apparaît dans le markup | ⏳ Pending |
| 2 | Should_Fermer_la_dialog_et_emettre_la_pose_du_slot_choisi_When_un_parent_valide_la_dialog | (statement → conditional) issue succès | La dialog reste affichée / aucune commande émise tant que la validation ne ferme pas et n'émet pas | ⏳ Pending |

## Fichiers à créer / modifier (par `ihm-builder`)

- **`PoserSlotDialog.razor` (+ code-behind)** — composant dialog (modal) réutilisable extrait
  de `PoserSlot.razor`, prenant la **date de contexte** en paramètre et exposant
  `OnValide` / `OnAnnule`.
- **`PlanningPartage.razor` / `.razor.cs`** — clic sur `data-testid="jour-case"` ouvre la
  dialog ; gestion de l'état d'ouverture ; relecture de la grille à la fermeture en succès.
- **`Web.Tests`** — bUnit composant (ci-dessus) + **acceptation runtime** réutilisant
  `ApiDistanteFactory` / `ClientCanalEcriture.Construire`.

## Design notes

- **Réutilisation pure** : la commande de pose et le canal HTTP existent (`PoserSlotTests`,
  `FrontWasmApiDistanteTests`) ; le neuf est **où** on déclenche la saisie (case) et le
  **pré-remplissage** de la date de contexte (cf. Sc.3).
- **Grille lecture seule** (règle 14) : la case ne fait **qu'ouvrir** la dialog, elle n'écrit
  jamais. La réapparition passe par la **relecture** de la grille (retour commande /
  diffusion SignalR), jamais par une mutation locale de la grille.
- La **diffusion temps réel** se vérifie côté grille via la relecture après succès ; aucun
  composant SignalR neuf ici (acquis s10).
