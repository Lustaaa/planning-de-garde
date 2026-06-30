# Sc.2 — Renommer un acteur déjà semé met à jour la grille

`@nominal` `🖥️ IHM` `@vert`

↩ Retour : [00-sprint09-suivi.md](00-sprint09-suivi.md)

**Routage** : **caractérisation** — l'édition (renommer) **est déjà livrée @vert au s08**
(`Scenario1_RenommerActeur`, 3 tests GREEN : store `renommer`, handler `EditerActeur`, diffusion
sur succès). La grille qui suit est `GrilleAgendaQuery` **inchangé** (s07). **Aucun driver backend
neuf** ici : la nouveauté du sprint 09 est la **durabilité**, portée par le **pivot Sc.3**. Tranche
**runtime IHM** (`ihm-builder`) : case + légende suivent **sans rechargement** — caractérisation
réutilisant l'acceptation runtime s08.

## Acceptation (BDD) — niveau **runtime/IHM** (routé `ihm-builder`)

> Le symptôme PO est un fait d'usage **runtime** : je renomme Alice → « Alicia » depuis l'écran
> de config ; **sans rechargement**, les cases du 1er au 5 juin et l'entrée de légende affichent
> « Alicia » en bleu, sur l'app **réellement câblée** (front WASM + API distante + store réel +
> SignalR). **Pas** un test bUnit à doublure. Réutilise l'acceptation runtime livrée au s08 Sc.1
> (le store derrière le port est désormais durable — la durabilité elle-même est prouvée au Sc.3).

`Should_Afficher_Alicia_en_bleu_dans_les_cases_du_1er_au_5_juin_et_dans_la_legende_sans_recharger_la_page_When_un_parent_renomme_Alice_en_Alicia_depuis_l_ecran_de_configuration` — ✅ GREEN (caractérisation)
*(caractérisation runtime — `FrontWasmConfigRenommerActeurGrilleTempsReelTests`, filet plus fort que le single-day s08 : les **5 cases** du 1er→5 juin 2026 + légende suivent le renommage en une re-projection, sur l'app réellement câblée — front WASM + API distante réelle + store réel + SignalR ; green attendu, aucun code de prod neuf, la durabilité est portée par Sc.3)*

> **Note non-régression (test-only).** L'énumération async des acteurs ajoutée à `ConfigurationFoyer`
> au Sc.1 re-rend l'écran et invalidait le handler du `select` si l'on interagissait avant la fin du
> chargement. Stabilisé en attendant la liste `acteur-foyer` avant d'interagir — dans **ce** test **et**
> dans `FrontWasmConfigDeuxEcransConvergenceTempsReelTests` (s08), qui échouait sur la baseline propre.
> Aucun changement de code de production ni d'assertion.

## Tests unitaires backend (boucle interne, `tdd-auto`)

| # | Test unitaire (FLFI) | TPP | Contradiction | Status |
|---|----------------------|-----|---------------|--------|
| — | *(aucun nouveau test backend)* | — | **Aucune contradiction backend neuve** — renommer est couvert @vert par s08 `Scenario1_RenommerActeur` (3 tests) et la re-projection case+légende par s07 (`GrilleAgendaQuery` inchangé). Un test backend ici passerait **vert d'emblée sans rouge** (doublon). La durabilité du renommage est portée par le **pivot Sc.3** (intégration Mongo réel). | — |

> **Pourquoi 0 test backend** (méthodo : ne pas gonfler la liste sans contradiction réelle) — Le
> handler `EditerActeurHandler` mute le store via `IEditeurConfigurationFoyer` ; l'adaptateur
> durable réalise ce **même port inchangé**. Aucun geste backend neuf n'est forcé par ce
> scénario : sa valeur de sprint 09 est la **survie au redémarrage**, démontrée au Sc.3.

## Fichiers à créer / modifier

- **Backend** : néant (édition livrée s08). L'adaptateur durable (Sc.3) réalise le port d'écriture
  `IEditeurConfigurationFoyer` **inchangé** — le renommage devient durable **sans toucher** au
  handler ni au read model.
- **Volet runtime IHM (routé `ihm-builder`)** : réutilise l'écran de config et la grille câblés ;
  l'acceptation s08 reste valable, désormais sur store durable.

## Design notes

- **Inchangé = caractérisation.** Le contrat `IEditeurConfigurationFoyer.Renommer` /
  `IReferentielResponsables.NomDe` ne bouge pas ; seule la **réalisation** passe en durable. La
  case/légende « suivent » par **re-projection** de `GrilleAgendaQuery`, pas par un calcul neuf.
- **La durabilité n'est pas ici.** Renommer reste un geste d'écriture déjà vert ; le seul
  observable neuf — « le renommage survit au redémarrage » — est l'objet du **pivot Sc.3**.
