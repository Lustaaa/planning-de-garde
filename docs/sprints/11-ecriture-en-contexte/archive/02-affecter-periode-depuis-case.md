# Scenario 2 — Affecter une période depuis une case du planning `@nominal 🖥️ IHM`

[← Retour au suivi](00-sprint11-suivi.md)

> **Routé vers `ihm-builder`.** Niveau d'acceptation = **E2E/runtime** sur l'app réellement
> câblée. Le comportement neuf vit dans le `.razor` (clic-case → dialog « Affecter une
> période » → validation → case colorée et nommée + légende agrégée). **Aucun handler/règle
> backend neuf** : on réutilise la commande `AffecterPeriode` et le canal HTTP
> `POST /api/canal/affecter-periode` (s04→s10). La table bUnit est **optionnelle**.

## Acceptation (BDD) — test de NIVEAU RUNTIME

**Should_Colorer_et_nommer_la_case_du_mercredi_17_06_2026_au_responsable_Alice_When_un_parent_affecte_une_periode_via_la_dialog_ouverte_depuis_cette_case** — ✅ GREEN (`FrontWasmAffecterPeriodeDepuisCaseTests`)

Sur l'app **réellement câblée** (front WASM réel + API distante + store réel + projection +
palette couleur réelle du foyer) : le foyer comporte l'acteur « Alice » avec sa couleur
propre ; un Parent **clique la case du mercredi 17/06/2026** → la dialog « Affecter une
période » s'ouvre → choisit « Alice » → **valide**. **Observable runtime** : la dialog se
ferme **et** la case du mercredi 17/06/2026 **affiche le nom « Alice »**, **prend la couleur
propre d'« Alice »**, et la **légende agrège « Alice » avec sa couleur** — l'affectation
étant **réellement enregistrée** dans le store distant et **relue par la projection** (pas
une grille statique).

## Tests bUnit composant (optionnels — pilotés par `ihm-builder`)

| # | Test composant (FLFI) | TPP | Contradiction | Status |
|---|------------------------|-----|---------------|--------|
| 1 | Should_Ouvrir_la_dialog_affecter_une_periode_When_un_parent_clique_une_case_du_planning | (unconditional → statement) ouvrir au clic | Sans câblage du clic, aucune dialog d'affectation dans le markup | ⏳ Pending |
| 2 | Should_Fermer_la_dialog_et_emettre_l_affectation_du_responsable_choisi_When_un_parent_valide_la_dialog | (statement → conditional) issue succès | La dialog reste affichée / aucune commande d'affectation émise sans validation fermante | ⏳ Pending |

## Fichiers à créer / modifier (par `ihm-builder`)

- **`AffecterPeriodeDialog.razor` (+ code-behind)** — dialog réutilisable extraite de
  `AffecterPeriode.razor`, date de contexte en paramètre, `OnValide` / `OnAnnule`.
- **`PlanningPartage.razor` / `.razor.cs`** — le clic sur une case future/sans responsable
  ouvre cette dialog (cf. routage du type de saisie côté case).
- **`Web.Tests`** — bUnit (ci-dessus) + **acceptation runtime** réutilisant l'infra s05.

## Design notes

- **Couleur + nom + légende** sont **déjà** dérivés par la projection
  (`GrilleAgendaQuery` → `IPaletteCouleurs.CouleurDe`, légende agrégée/dédoublonnée, acquis
  s03/s07/s08) : ce scénario **ne rouvre aucune règle de projection**. Le neuf est
  l'**ouverture en contexte** + la **relecture** après succès.
- **Choix de la dialog selon la case** : poser-slot vs affecter-période — convention IHM à
  trancher par `ihm-builder` (p.ex. une case « sans responsable affiché » oriente vers
  l'affectation). Si l'ergonomie du choix devient ambiguë à l'implémentation, remonter au CP.
