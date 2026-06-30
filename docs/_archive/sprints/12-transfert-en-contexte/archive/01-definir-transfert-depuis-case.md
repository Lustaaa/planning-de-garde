# Scenario 1 — Définir un transfert depuis une case via le menu clic-case `@nominal 🖥️ IHM`

[← Retour au suivi](00-sprint12-suivi.md)

> **Routé vers `ihm-builder`.** Niveau d'acceptation = **E2E/runtime** sur l'app réellement
> câblée. Le comportement neuf vit dans le `.razor` (clic-case → menu **3 entrées** → 3ᵉ entrée
> « Définir un transfert » → dialog pré-remplie → validation → fermeture + accusé à part →
> transfert relu depuis le store). **Aucun handler/règle backend neuf** : on réutilise la
> commande `DefinirTransfert` et le canal HTTP `POST /api/canal/definir-transfert` (s01→s05).
> **Driver réel du sprint** (la 3ᵉ dialog n'existe pas encore). La table bUnit ci-dessous est
> **optionnelle** et complémentaire — le détail RED→GREEN est piloté par `ihm-builder`.

## Acceptation (BDD) — test de NIVEAU RUNTIME

**Should_Fermer_la_dialog_afficher_l_accuse_transfert_defini_a_part_et_enregistrer_le_transfert_parent_a_vers_parent_b_ecole_08h30_le_16_06_2026_When_un_parent_le_definit_via_la_3e_entree_du_menu_clic_case** — ✅ GREEN (`FrontWasmDefinirTransfertDepuisCaseTests`, RED→GREEN runtime : front WASM réel + API distante réelle + store réel ; transfert relu depuis `ITransfertRepository`)

Sur l'app **réellement câblée** (front WASM réel + API distante + store réel, façon
`FrontWasmApiDistanteTests` / `ApiDistanteFactory` du s05) : un Parent **clique la case du
mardi 16/06/2026** → le **menu d'actions s'ouvre** et propose la **3ᵉ entrée** « Définir un
transfert » → la dialog « Définir un transfert » s'ouvre → choisit « Parent A » dépositaire,
« Parent B » récupérateur, lieu « École » à 08:30 → **valide**. **Observable runtime** : la
dialog **se ferme** ; un **accusé « Transfert défini » s'affiche à part, sans bloquer** ; et le
transfert « Parent A » → « Parent B » au lieu « École » le mardi 16/06/2026 à 08:30 est
**réellement enregistré** dans le store de l'API distante et **relu depuis ce store** (pas un
accusé du canal seul, pas une grille statique). *Anti vert-qui-ment* : si le clic n'ouvre pas
le menu, si la 3ᵉ entrée manque, si la validation ne transite pas jusqu'au store distant, ou si
le transfert retombe à une autre date, l'observable distant reste vide → rouge.

## Tests bUnit composant (optionnels — pilotés par `ihm-builder`)

| # | Test composant (FLFI) | TPP | Contradiction | Status |
|---|------------------------|-----|---------------|--------|
| 1 | Should_Ouvrir_la_dialog_definir_un_transfert_When_un_parent_choisit_la_3e_entree_du_menu_clic_case | (unconditional → statement) ajouter la 3ᵉ entrée + ouvrir au clic | Tant que le menu ne propose que 2 entrées, aucune dialog transfert n'apparaît dans le markup au clic | ⏳ Pending |
| 2 | Should_Fermer_la_dialog_emettre_la_definition_du_transfert_et_afficher_l_accuse_transfert_defini_a_part_When_un_parent_valide_la_dialog | (statement → conditional) issue succès + accusé à part | La dialog reste affichée / aucune commande émise / aucun accusé tant que la validation ne ferme pas, n'émet pas et ne signale pas à part | ⏳ Pending |

## Fichiers à créer / modifier (par `ihm-builder`)

- **`DefinirTransfertDialog.razor` (+ code-behind)** — composant dialog (modal) réutilisable
  extrait de `DefinirTransfert.razor`, prenant la **date de contexte** en paramètre et exposant
  `OnValide` / `OnAnnule`. Calqué sur `PoserSlotDialog.razor` / `AffecterPeriodeDialog.razor`.
- **`PlanningPartage.razor` / `.razor.cs`** — **3ᵉ entrée** « Définir un transfert » au menu
  clic-case ; ouverture de la dialog pré-remplie sur la date de la case ; gestion de l'état
  d'ouverture ; **accusé « Transfert défini » à part, non bloquant** au succès (`data-testid`
  dédié), réutilisant le mécanisme du bandeau s11 (Sc.7).
- **`Web.Tests`** — bUnit composant (ci-dessus) + **acceptation runtime** réutilisant
  `ApiDistanteFactory` / `ClientCanalEcriture.Construire` et `DefinirTransfertCanalApiTests`
  étendu (relecture du store).

## Design notes

- **Réutilisation pure** : la commande de transfert et le canal HTTP existent
  (`DefinirTransfertCanalApiTests`, vert s01) ; le neuf est **où** on déclenche la saisie (3ᵉ
  entrée de menu sur une case) et le **pré-remplissage** de la date de contexte (cf. Sc.2).
- **Accusé succès = feedback transitoire, PAS un rendu** (décision CP, règle 27 préservée) :
  l'accusé « Transfert défini » s'affiche **à part** (toast/bandeau non bloquant), il ne rend
  **aucun** qui/quand/où en case (le panneau cloche reste palier 14, hors scope). Il se
  déclenche sur le **simple succès HTTP** du canal — **aucun contrat de réponse neuf**.
- **Grille lecture seule** (règle 14) : la case ne fait **qu'ouvrir** le menu/la dialog, elle
  n'écrit jamais. L'enregistrement passe par le canal requête/réponse ; la diffusion SignalR
  reste lecture seule (acquis s10). Aucun composant SignalR neuf ici.
- **Identifiants stables** (règle 19) : les sélecteurs dépose/récupère bindent `Parent A` /
  `Parent B`, jamais le libellé.
- **Geste d'interaction du menu** : la 3ᵉ entrée s'ajoute au **menu d'actions** déjà tranché au
  s11 (décision CP « un menu d'actions au clic-case »). → remonter au CP si une autre ergonomie
  est envisagée.
