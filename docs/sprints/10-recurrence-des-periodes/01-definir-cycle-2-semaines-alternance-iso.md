# Sc.1 — Définir un cycle de 2 semaines : le fond alterne par parité ISO

`@nominal` `🖥️ IHM`

↩ Retour : [00-sprint10-suivi.md](00-sprint10-suivi.md)

**Routage** : **scénario fondation** de la couche de résolution du fond. Tranche **backend**
(`tdd-auto`, 3 drivers : résoudre le responsable de fond d'un jour **sans** période, forcer le calcul
**ISO réel** par une semaine de parité opposée, faire **figurer le fond en légende**) **+** acceptation
**runtime IHM** (`ihm-builder` : la grille réellement câblée affiche le responsable de fond — case
nommée + colorée — **sans aucune saisie de période**). L'acceptation backend exerce **`DefinirCycleHandler`
en succès** (définir un cycle de 2 semaines), ce qui établit le **nominal vert** rendant la garde N ≥ 1
du Sc.7 **conditionnelle**.

> **Données** (déterministes, via `Projeter(dateReference)` — jamais `Now`) : `parent-a` = Alice **bleu**,
> `parent-b` = Bruno **orange** (seed `Foyer`). Cycle N=2, mapping **index pair → `parent-a`, index impair
> → `parent-b`**. Date de référence = lundi **29/06/2026** (ISO **27**, impaire). Fenêtre de 5 semaines :
> ISO 27 (29/06–05/07), 28 (06–12/07), 29 (13–19/07), 30 (20–26/07), 31 (27/07–02/08). `index = ISOWeek
> mod 2` : ISO 27 → 1 (impair) → Parent B ; ISO 28 → 0 (pair) → Parent A.

## Acceptation (BDD) — niveau **runtime/IHM** (routé `ihm-builder`)

> Le symptôme PO est un fait d'usage **runtime** : depuis l'écran de config, un parent définit un cycle
> de fond de 2 semaines (pair → Parent A, impair → Parent B) ; **sans aucune saisie de période**, la
> grille **réellement câblée** (front WASM + API distante + SignalR) affiche « Parent B » en orange sur
> la semaine ISO 27 et « Parent A » en bleu sur la semaine ISO 28, **en case comme en légende**. **Pas**
> un test bUnit à doublure (qui ne prouve ni la DI réelle, ni le chemin HTTP d'écriture du cycle, ni la
> diffusion). Si la grille ignore le cycle (jours sans période = gris neutre), aucun fond n'apparaît → rouge.

`Should_Afficher_le_responsable_de_fond_par_parite_ISO_en_case_et_en_legende_sans_aucune_saisie_de_periode_When_un_parent_definit_un_cycle_de_fond_de_deux_semaines_depuis_la_configuration` — ⏳ Pending *(runtime, `ihm-builder`)*

> **Acceptation backend (boucle externe à la frontière Application, menée par `tdd-auto`)** — filet
> sociable traduisant le Gherkin sans IHM : via `DefinirCycleHandler` (définir un cycle de 2 semaines,
> **succès**) puis `GrilleAgendaQuery.Projeter(29/06/2026)`, les jours ISO 27 affichent « Bruno »/orange
> et les jours ISO 28 « Alice »/bleu, en case **et** en légende, résolus sur l'**identifiant stable**.
> `Acceptation_Should_Resoudre_le_fond_Bruno_orange_sur_ISO_27_impaire_et_Alice_bleu_sur_ISO_28_paire_en_case_et_en_legende_When_un_cycle_de_deux_semaines_est_defini` — ✅ GREEN

- **Niveau** : E2E/runtime sur l'app câblée (cycle défini via le **canal HTTP** `POST
  /api/canal/definir-cycle`, grille relisant le port cycle). Backend : frontière Application.
- **Observable** : sans saisie de période, ISO 27 → Parent B orange, ISO 28 → Parent A bleu, case +
  légende, et l'alternance se poursuit sur les semaines suivantes de la fenêtre.

## Tests unitaires backend (boucle interne, `tdd-auto`)

| # | Test unitaire (FLFI) | TPP | Contradiction | Status |
|---|----------------------|-----|---------------|--------|
| 1 | `Should_Afficher_Bruno_en_orange_en_fond_When_la_semaine_ISO_impaire_n_a_aucune_periode_et_le_cycle_mappe_l_index_impair_sur_Parent_B` | constante → résolution conditionnelle au cycle | **Driver** — aujourd'hui un jour **sans période** = couleur **neutre** + nom **vide** (`CaseJourAu` : `periode is null ? CouleurNeutre`). Aucune couche de fond n'existe. Force la résolution du fond dans la branche `periode is null` : `mapping[ISOWeek(jour) mod N]` → Bruno/orange sur ISO 27. *(Impl minimale possible : constante « Bruno ».)* | ✅ GREEN |
| 2 | `Should_Afficher_Alice_en_bleu_en_fond_When_la_semaine_ISO_paire_n_a_aucune_periode_et_le_cycle_mappe_l_index_pair_sur_Parent_A` | constante → calcul ISO réel `mod N` | **Driver** — l'impl minimale du #1 (constante « Bruno » pour tout jour de fond) est **cassée** par une semaine de parité **opposée** : ISO 28 (paire) doit résoudre l'index **pair** → Alice/bleu. Force le **calcul réel** `ISOWeek(jour) mod N` → `mapping[index]`, contredisant la constante. | ✅ GREEN |
| 3 | `Should_Faire_figurer_le_responsable_de_fond_dans_la_legende_de_la_fenetre_When_le_cycle_couvre_des_jours_sans_periode_explicite` | légende des périodes → légende incluant le fond | **Driver** — `LegendeDesPresents` n'agrège **que les périodes** : sans période, la légende est **vide** alors que les cases portent le fond (incohérence « en case comme en légende »). Force l'extension de la légende aux responsables de **fond présents** dans la fenêtre (Alice + Bruno), dédoublonnés par id stable. | ✅ GREEN |
| 4 | `Should_Poursuivre_l_alternance_sur_les_semaines_suivantes_sans_nouvelle_saisie_When_la_fenetre_couvre_plusieurs_semaines_du_cycle` | — | ⚠️ **probablement early green — couvert par #1/#2 (résolution = fonction pure de la date, reproductible) (caractérisation, pas driver)**. ISO 29 (impaire) → Bruno, ISO 30 (paire) → Alice se résolvent par le **même** calcul que #1/#2, sans saisie. Filet verrouillant « l'alternance se poursuit sur la fenêtre ». `tdd-auto` marquera ✅ GREEN (caractérisation). | ✅ GREEN (caractérisation) |

## Fichiers à créer / modifier (backend uniquement ici)

- **Fonction pure de parité ISO + résolution du fond (Domain, NEUF)** — `index = System.Globalization.ISOWeek.GetWeekOfYear(date) mod N`,
  sans framework ; résout `mapping[index] → responsableId` (forme exacte — value object `CycleDeFond`
  portant N + mapping + méthode pure, ou fonction libre — laissée à `tdd-auto`).
- **Port cycle `IReferentielCycleDeFond` (Application, NEUF)** — lecture du cycle courant ; consommé par
  `GrilleAgendaQuery`. Doublure `Fake` en mémoire côté tests (cycle seedé au constructeur).
- **`GrilleAgendaQuery` ÉTENDU (Application)** — injecte le port cycle ; dans `CaseJourAu`, branche
  `periode is null` → résout le fond (au lieu du neutre/nom vide) ; `LegendeDesPresents` étendue aux
  responsables de fond présents dans la fenêtre. La branche `else période` **reste intacte** (priorité
  surcharge > fond — Sc.2). Résolution nom/couleur via `IReferentielResponsables`/`IPaletteCouleurs`
  **inchangés**, sur l'**identifiant stable**.
- **`DefinirCycleHandler` + `DefinirCycleCommand` (Application, NEUF)** — exercé **en succès** par
  l'acceptation (définir un cycle de 2 semaines), établissant le nominal vert (la garde N ≥ 1 du Sc.7 en
  devient conditionnelle). Renvoie `Result` (convention existante).
- **Adaptateur InMemory singleton du port cycle (Infrastructure, NEUF)** + câblage
  `ServiceCollectionExtensions`. **PAS Mongo** (borne anti-cliquet règle 30).
- **Volet runtime IHM (routé `ihm-builder`, hors backend)** — endpoint `POST /api/canal/definir-cycle` ;
  écran config portant l'édition N + mapping index→responsable (sélecteur alimenté par les acteurs du
  foyer) ; grille affichant le fond ; diffusion SignalR existante réutilisée.

## Design notes

- **Couche de résolution orthogonale.** Le fond est une couche **sous** les périodes : priorité
  **surcharge (période) > fond (cycle) > neutre**. La structure actuelle `CaseJourAu` (`periode is null
  ? … : période`) place naturellement le fond dans la branche `periode is null` → la primauté de la
  surcharge est **gratuite** (Sc.2 early green).
- **Parité ISO = fonction pure de la date.** `ISOWeek.GetWeekOfYear` est déterministe ; tester sur ISO
  paire **et** impaire via la date injectée à `Projeter`, **jamais** `Now`. La discontinuité de parité à
  la jonction d'année (53 → 01) est une conséquence **assumée** de l'ancrage ISO (cf. Risques spec), non
  bloquante pour ce palier.
- **Résolution sur l'identifiant stable** (règle 19), jamais le libellé : le mapping porte des
  `responsableId` (`parent-a`/`parent-b`), résolus en nom + couleur comme les périodes.
- **Légende = présents dans la fenêtre, dédoublonnés par id** (patron s07) — étendue au fond : un
  responsable de fond couvrant un jour de la fenêtre figure une fois en légende.
- **Diffusion = effet de l'écriture aboutie** (Spy backend `INotificateurPlanning` ; suivi live réel
  prouvé au runtime).
