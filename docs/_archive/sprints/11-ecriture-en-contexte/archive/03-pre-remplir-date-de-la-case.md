# Scenario 3 — La dialog se pré-remplit sur la date de la case cliquée `@limite 🖥️ IHM`

[← Retour au suivi](00-sprint11-suivi.md)

> **Routé vers `ihm-builder`.** Niveau d'acceptation = **E2E/runtime**. Le comportement neuf
> vit dans le `.razor` : l'**ancrage case** (date de la case cliquée) **prime** sur le défaut
> `IDateTimeProvider` « aujourd'hui » (règle 17 composée, non révisée). C'est le cœur de
> l'« écriture en contexte » et le **driver de design** du sprint.

## Acceptation (BDD) — test de NIVEAU RUNTIME

**Should_Pre_remplir_la_saisie_sur_le_jeudi_25_06_2026_et_y_faire_apparaitre_le_slot_When_la_dialog_est_ouverte_depuis_la_case_du_25_06_alors_qu_aujourd_hui_est_le_15_06** — ✅ GREEN (`FrontWasmPreRemplirDateCaseTests`)

> **Caractérisation early-green (décision CP)** : l'acceptation passe **sans fix** — le paramètre
> `DateContexte` et l'ancrage qui prime sur `IDateTimeProvider` ont atterri dès le **fix du Sc.1**. Le
> test runtime est **non-vacuous** (slot au 25/06 **et** rien au 15/06 « aujourd'hui ») : il échouerait
> si l'ancrage retombait sur l'horloge → conservé comme **filet anti-régression de la règle 17**. Pas de
> cycle RED→GREEN factice.

Sur l'app **réellement câblée**, avec la date de référence « aujourd'hui » figée au **lundi
15/06/2026** (`IDateTimeProvider` doublé) et la case du **jeudi 25/06/2026** visible dans la
fenêtre : un Parent **clique la case du jeudi 25/06/2026** → la dialog « Poser un slot »
s'ouvre, **la date de la saisie est pré-remplie sur le jeudi 25/06/2026** (et **n'est pas**
le lundi 15/06/2026). Puis choisit « Maison » 17:00→19:00 et valide. **Observable runtime** :
un slot **réellement enregistré** réapparaît dans la **case du jeudi 25/06/2026** (date de
contexte), prouvant que l'ancrage case a primé sur le défaut « aujourd'hui ».

## Tests bUnit composant (optionnels — pilotés par `ihm-builder`)

| # | Test composant (FLFI) | TPP | Contradiction | Status |
|---|------------------------|-----|---------------|--------|
| 1 | Should_Pre_remplir_la_date_de_saisie_sur_la_date_de_la_case_When_la_dialog_est_ouverte_depuis_une_case_future | (statement → conditional) date de contexte prime | Sans transmission de la date de case, la dialog retombe sur le défaut « aujourd'hui » (15/06) → contradiction | ⏳ Pending |
| 2 | Should_Conserver_la_date_de_la_case_dans_la_commande_emise_When_un_parent_valide_la_dialog_ouverte_depuis_cette_case | (conditional) propagation jusqu'à l'émission | Une émission figée sur « aujourd'hui » trahirait l'ancrage → commande au 15/06 au lieu du 25/06 | ⏳ Pending |

## Fichiers à créer / modifier (par `ihm-builder`)

- **`PoserSlotDialog.razor` / `.razor.cs`** — **paramètre `DateContexte`** ; pré-remplit la
  date de saisie depuis ce paramètre quand il est fourni, sinon depuis `IDateTimeProvider`
  (défaut hors-contexte).
- **`PlanningPartage.razor.cs`** — transmet `jourCase.Date` à la dialog à l'ouverture.
- **`Web.Tests`** — bUnit (ci-dessus) + acceptation runtime avec `IDateTimeProvider` figé au
  15/06 (cf. `DateTimeProviderFige`).

## Design notes

- **Règle 17 composée, non révisée** : le défaut `IDateTimeProvider.Aujourdhui` (acquis s06)
  ne vaut **que hors-contexte** ; en contexte, la **date de la case prime**. Ne pas supprimer
  le port d'horloge — il reste le défaut quand aucune case n'ancre la saisie.
- **Pré-remplissage par UNE case ≠ sélection de plage** (intervalle multi-cases) → **hors
  scope** ; la dialog reçoit une **date unique**.
- C'est le scénario qui **force** le paramètre `DateContexte` : Sc.1/Sc.2 pourraient passer
  avec une date « aujourd'hui » par hasard si la case testée tombait aujourd'hui — Sc.3 rend
  l'ancrage **contredisant** (15/06 ≠ 25/06).
