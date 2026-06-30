# Sc.2 — Une surcharge ponctuelle prime sur le fond puis le cycle reprend

`@nominal` `🖥️ IHM`

↩ Retour : [00-sprint10-suivi.md](00-sprint10-suivi.md)

**Routage** : tranche **backend = CARACTÉRISATION** (`tdd-auto`, ⚠️ early green **attendu**, **garde-fou
de non-régression** des périodes explicites — **pas** un driver) **+** acceptation **runtime IHM**
(`ihm-builder` : la grille câblée montre la surcharge primer puis le fond reprendre). **Divergence
assumée vs analyse `/2`** : l'analyse annonçait Sc.2 comme driver ; l'inspection de
`GrilleAgendaQuery.CaseJourAu` montre que la **priorité surcharge > fond est STRUCTURELLE** (la branche
`else période` reste intacte quand le fond est ajouté dans la branche `periode is null`) → **early green**.

> **Données** : cycle N=2, pair → `parent-a` (Alice bleu), impair → `parent-b` (Bruno orange). Semaine
> ISO 28 (06–12/07/2026, **paire**) revient par défaut à Parent A (fond). Surcharge explicite : Parent B
> affecté à la **seule** journée du **08/07/2026**.

## Acceptation (BDD) — niveau **runtime/IHM** (routé `ihm-builder`)

> Sur l'app réellement câblée, avec un cycle de fond actif (ISO 28 = Parent A bleu) et une période
> explicite Parent B au seul 08/07 : la case du 08/07 affiche « Parent B » orange (surcharge), les cases
> du 07 et du 09/07 affichent « Parent A » bleu (le fond reprend de part et d'autre). Prouve que la
> couche fond **ne déborde pas** sur la surcharge ni n'altère les jours voisins.

`Should_Afficher_la_surcharge_le_jour_concerne_et_le_fond_de_part_et_d_autre_When_une_journee_isolee_est_affectee_explicitement_sur_une_semaine_couverte_par_le_fond` — ⏳ Pending *(runtime, `ihm-builder`)*

## Tests unitaires backend (boucle interne, `tdd-auto`)

| # | Test unitaire (FLFI) | TPP | Contradiction | Status |
|---|----------------------|-----|---------------|--------|
| 1 | `Should_Afficher_Bruno_orange_le_jour_surcharge_et_Alice_bleu_de_part_et_d_autre_When_une_journee_isolee_est_affectee_explicitement_sur_une_semaine_couverte_par_le_fond` | — | ⚠️ **probablement early green — couvert par Sc.1 + structure « période d'abord » (caractérisation, garde-fou non-régression — pas driver)**. La case du 08/07 a une **période explicite** → branche `else période` (intacte) → Bruno/orange ; les 07 et 09/07 sont sans période → branche `periode is null` → fond Alice/bleu (Sc.1). La primauté surcharge > fond est **structurelle**, aucun code neuf. Filet verrouillant la **non-régression des périodes explicites**. `tdd-auto` marquera ✅ GREEN (caractérisation). *Si un rouge apparaît (impl résolvant le fond avant de tester la période), ce test devient le driver de la primauté.* | ✅ GREEN (caractérisation) |

## Fichiers à créer / modifier (backend uniquement ici)

- **Aucun fichier neuf** — comportement couvert par l'extension `GrilleAgendaQuery` du Sc.1 (branche
  `periode is null` pour le fond, branche `else période` intacte pour la surcharge).
- **Doublures tests** — `FakePeriodeRepository` (période explicite au 08/07) + `Fake` du port cycle
  (cycle N=2 seedé) ; réutilisent les builders existants (`PeriodeBuilder`).
- **Volet runtime IHM (routé `ihm-builder`)** — surcharge posée via la dialog d'affectation existante ;
  grille affichant surcharge + fond.

## Design notes

- **Garde-fou de non-régression (Risques spec).** Ajouter la couche fond ne doit pas altérer le rendu
  des périodes déjà saisies — Sc.2 est précisément ce filet. La structure `periode is null ? fond :
  période` garantit la primauté par construction.
- **Le fond ne déborde jamais sur les jours voisins** (invariant 3) : chaque case résout
  **indépendamment** sa propre date (période d'abord, sinon fond), donc une surcharge isolée n'affecte
  que sa journée.
- **Caractérisation, pas suppression.** Ce scénario n'apporte aucun rouge mais reste un filet de
  non-régression important (priorité surcharge > fond) — conservé, marqué early green, jamais fusionné.
