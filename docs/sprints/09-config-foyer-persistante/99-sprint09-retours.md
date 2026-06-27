# Retours — Sprint 09 (config-foyer-persistante)

> **Fichier unifié.** Il porte les retours produit (PO), la méthode (agents) et les
> décisions autonomes du chef de projet. Amorcé au cadrage `/2-make-gherkin` (section
> « Décisions autonomes » créée ici) ; scaffoldé par `tdd-analyse` au `/3` qui DOIT
> **préserver** la section « Décisions autonomes (chef de projet) ».

# Décisions autonomes (chef de projet)

> Journal des décisions tranchées par l'agent `chef-de-projet` sans escalade PO.
> Permet au PO de piloter a posteriori.

## 2026-06-27 — Découpe du palier 5 (ajout d'acteur + persistance Mongo) dans ~2h IA

- **Question (make-gherkin)** : le palier 5 fond ajout d'acteur + persistance Mongo réelle
  dans ~2h IA. Quel ordre de bataille borne le périmètre (et donc ce que couvre le pivot
  « survie au redémarrage ») ?
- **Décision (CP, sans escalade)** : option 1 — **FUSIONNÉ**. L'**ajout** d'acteur (id stable
  neuf généré, jamais dérivé du libellé) **et** l'**édition** du seed (rename/recolor s08) sont
  **durables d'emblée sur store Mongo réel**. Le **pivot redémarrage** couvre l'acteur **ajouté
  ET** l'édition (ex. Alice→Alicia + nounou Carla survivent au redémarrage). **Tranche de
  secours** (décidée au `/3` UNIQUEMENT si débordement réel ~2h, pas maintenant) : persister
  **d'abord le référentiel semé** (pivot sur l'édition seule), **reporter l'ajout** en tranche 2.
- **Rationale** : c'est le **cap PO complet** (ajout **et** Mongo, acté G2 au /4-retours s08).
  La découpe-dans-le-budget relève du **garde-fou découpe** (remit CP, ambition ~2h) ; la
  tranche de secours est **déjà documentée** (spec v09 R1, backlog). Couper *maintenant* (option
  2 ou 3) amputerait le cap sans nécessité prouvée — on coupe au débordement réel, pas par
  précaution. L'option 3 (ajout volatile d'abord) **retire l'observable de durabilité** que le
  PO a explicitement voulu → écartée. Aucun arbitrage métier neuf.
- **Guidage de cadrage transmis à make-gherkin (craft, pas un arbitrage PO)** :
  - **Forme de l'id stable neuf** : opaque, généré (GUID ou séquence « autre-N »), **jamais
    dérivé du libellé** (anti-pattern libellé-comme-identité corrigé au s06) ; **unique** (jamais
    de collision avec un id existant).
  - **Ajout → légende** : un acteur **ajouté sans période** est présent **immédiatement dans
    l'écran config** mais **n'apparaît PAS en légende/case** tant qu'aucune période ne le porte
    (pas d'entrée fantôme, cohérent s08 Sc.6). Inclure un scénario « ajout → affecter une période
    → apparaît en légende avec id neuf + couleur » pour prouver que l'id neuf circule.
  - **Then du pivot** : prouver la survie au redémarrage sur **store Mongo RÉEL** via la **grille
    réellement câblée** (case + légende nommée après redémarrage) — preuve la plus forte
    (anti vert-qui-ment, R4) ; lecture via le port sur instance fraîche acceptable en complément.
  - **Borne** : ajout **sans suppression** d'abord ; **pas** d'édition du cycle de fond ; cases
    orphelines hors périmètre. Le **reste du domaine** (slots/périodes/transferts) reste
    **InMemory** (borne anti-cliquet, règle 30).
- **Sources** : spec v09 règles 6 + 30 + R1 ; besoins /4-retours s08 (G2 PO, révision d'arbitre
  bornée) ; garde-fou découpe ; acquis s08 (ConfigurationFoyerEnMemoire, EditerActeur, ports).
