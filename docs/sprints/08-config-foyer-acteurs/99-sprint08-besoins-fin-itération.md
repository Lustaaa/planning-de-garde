# Besoins priorisés — Config foyer · édition volatile des acteurs (noms + couleurs)

> Source : `99-sprint08-retours.md` (section `# Retours produit (PO)` : `## IHM - général`,
> `## IHM - /configuration`, `## IHM - /planning` + sections forward `## Idée pour la suite`
> et `## Consigne pour le suite`) · produit par `/4-retours` (retours-challenge). Réamorce
> `/2-make-gherkin` sur le **sujet prioritaire** ci-dessous. Ne pas confondre avec le journal
> méthode du même fichier (`# Méthode (agents)`, `## IA`, `## Notes de contexte`,
> `# Décisions autonomes (chef de projet)`) qui relève de `retro-sprint` / du pipeline.

## Classification des retours

> Sprint 08 (config foyer · édition **volatile** des acteurs) clos **@vert 10/10** (palier 4).
> Les retours frais d'usage de CE sprint portent sur l'écran `/configuration` (palette de
> couleur, onglets, **ajout d'acteurs**) et sur `/planning` (couleurs de la légende). Le
> retour `/planning` a été **confronté au code courant (HEAD)** avant classification. Les
> sections `## Idée pour la suite` et `## Consigne pour le suite` portent des intentions de
> séquencement (idées + cap), pas des défauts sur le livré. `## Tech` = placeholder vide →
> **bypass** (aucune contrainte technique à injecter).

| # | Retour (résumé) | Source | Type | Besoin sous-jacent | Destination |
|---|---|---|---|---|---|
| 1 | Ajouter des acteurs (parent / autre, ex. nounou) dans la config du foyer | `## IHM - /configuration` | **nouveau besoin** | AJOUT/édition d'acteurs au-delà du rename/recolor du seed livré au s08 (gestion d'identifiants stables neufs, impact légende). Exclu sciemment de l'incrément volatile s08 (décision CP) | **PROCHAIN `/2`** — rang 1 (volet ajout) |
| 2 | Persistance des données en base Mongo (consigne + idée) | `## Consigne pour le suite` + `## Idée pour la suite` | **consigne actée — bornée** | Adaptateur de droite durable derrière les ports, **borné à la config foyer** (référentiel acteurs : noms, couleurs, acteurs ajoutés). La survie au redémarrage de la config foyer | **PROCHAIN `/2`** — rang 1 (volet persistance) |
| 3 | « Faire un sprint pour créer un foyer de A à Z » | `## Consigne pour le suite` | consigne — **coupée** | Cap : compléter la config du foyer. Coupé au plus petit pas d'usage (ajout d'acteurs + persistance config foyer) ; cycle + lieux restent séquencés (paliers 5/8/9) | rang 1 (tranché) + backlog |
| 4 | « IMPORTANT : configuration de la récurrence au niveau du foyer » | `## Idée pour la suite` | nouveau besoin (flaggé IMPORTANT) | Définir/éditer le cycle de fond (récurrence des périodes), É7/É1 | backlog — **séquencé rang 2** |
| 5 | Dialogs au lieu d'écran d'édition (concentrer ce qui touche à l'enfant) | `## Idée pour la suite` | nouveau besoin | Écriture en contexte via dialogs ouvertes depuis une case (É12) | backlog — **séquencé rang 3** |
| 6 | Sélection de cases du planning pour définir une période (29/05→05/07) | `## Idée pour la suite` | nouveau besoin | Sélection de plage de cases pour affecter une période (É7/É12) | backlog — **séquencé rang 3** |
| 7 | Palette de couleur pour choisir la couleur de l'acteur | `## IHM - /configuration` | évolution | Sélecteur de couleur (picker/palette) dans l'écran config, au lieu d'une saisie libre | backlog — non prioritaire seul |
| 8 | Couleurs de la légende ≠ couleurs des acteurs (parent) | `## IHM - /planning` | **évolution** (harmonisation de teinte) — **NON-bug** | Cohérence visuelle pastille de légende ↔ fond de case-jour du responsable | backlog — non prioritaire seul |
| 9 | Onglets pour configurer le foyer par acteur | `## IHM - /configuration` | évolution mineure (auto-tempérée par le PO) | Agencement de l'écran config. Le PO se contredit : « un seul foyer → tous les acteurs sur le même écran » | backlog — non prioritaire (ne pas prioriser seul) |

> **Note `bug` (anti-règle confrontation HEAD)** — Le retour `/planning` « j'ai l'impression
> que les couleurs de la légende ne sont pas celles des acteurs (parent) » a été confronté au
> **code courant (HEAD)**. Ce n'est **PAS un bug de mapping** : la légende et la case-jour
> résolvent le **même token couleur sur le même singleton** — `ConfigurationFoyerEnMemoire`
> réalise à la fois `IReferentielResponsables` et `IPaletteCouleurs`, enregistré singleton
> (`src/PlanningDeGarde.Infrastructure/ServiceCollectionExtensions.cs:28-30`) ; côté lecture,
> `GrilleAgendaQuery.cs:64` (case) et `:83` (légende) appellent tous deux `_palette.CouleurDe(id)`
> sur l'identifiant stable. **Aucun défaut de résolution localisé.** Le ressenti s'explique par
> une **incohérence de teinte de PRÉSENTATION** : la pastille de légende est **saturée**
> (`src/PlanningDeGarde.Web/Components/Legende.razor:29-37`, bleu = `#2563eb`) tandis que le
> fond de case-jour du responsable est **pâle** (`src/PlanningDeGarde.Web/Components/Pages/PlanningPartage.razor.cs:92-100`,
> `Teinte`, bleu = `#dbeafe`) — choix de design assumé (fond pâle = texte sombre lisible). La
> légende matche la couleur des **slots** (saturée), pas le fond pâle de la case responsable.
> → **évolution** (harmonisation de teinte), **jamais** un `/3` ciblé aveugle : rien n'est cassé.
> `## Tech` placeholder vide → **bypass** (aucune contrainte technique à injecter).

## Arbitrage

- **Objectif de l'itération** — Fermer la boucle du sprint 08 (@vert 10/10, palier 4, édition
  **volatile** des acteurs) en désignant le **prochain incrément**. Le PO a exprimé un cap
  fort (consigne « foyer de A à Z » + « persistance Mongo » + flag IMPORTANT récurrence/ajout
  d'acteurs) ; on en extrait une **tranche bornée**, le reste séquencé et consigné.
- **Arbitre (départage)** — Règle actée pour ce tour, par ordre :
  1. **Arbitre permanent maintenu pour le reste du domaine** : l'usage tranche, la technique
     (persistance Mongo des **slots / périodes / transferts**) reste séquencée **derrière**
     l'usage.
  2. **Exception bornée actée par le PO (G2)** : la **persistance Mongo de la SEULE config
     foyer** est tirée **devant** l'usage, parce qu'elle porte un observable d'usage direct
     (l'ajout/édition d'acteur **survit au redémarrage**) et qu'elle est, par la spec/BACKLOG,
     le **premier client de la config durable** (palier 13 recoupe le palier 8). Ce n'est
     **pas** un renversement de l'arbitre : c'est une borne, à écrire noir sur blanc.
  3. **Garde-fou découpe en vigueur** : si le sprint déborde la fenêtre (~2h IA), on **coupe**
     (cf. risques), on ne reporte jamais tout en bloc.
  4. **Un défaut confirmé primerait sur une évolution** — mais **aucun bug n'a été confirmé**
     ce sprint (le retour légende est une **évolution de teinte**, pas un défaut).
- **Révision d'arbitre actée (G2, noir sur blanc)** — Mongo (palier 13) passe **devant
  l'usage**, **borné à la config foyer** (référentiel acteurs : noms, couleurs, acteurs
  ajoutés). La **volatilité du palier 4 (s08) s'éteint ICI**, pour la config foyer
  **uniquement**. La persistance Mongo du **reste du domaine** (slots / périodes / transferts)
  **reste en queue**, derrière l'usage — la borne doit empêcher tout effet de cliquet.

## Séquence de livraison

| Rang | Besoin | Type | Sujet make-gherkin | Dépend de |
|---|---|---|---|---|
| 1 | **Config foyer persistante** — AJOUT/édition d'acteurs (parent / autre / nounou) **+** persistance Mongo **bornée à la config foyer** (adaptateur de droite, ports inchangés). L'ajout/édition **survit au redémarrage** | nouveau besoin + consigne actée bornée | `config-foyer-persistante` | palier 4 (livré ✅ s08) |
| 2 | **Récurrence des périodes** (cycle de fond, flaggé IMPORTANT) | nouveau besoin | `recurrence-periodes` | rang 1 |
| 3 | **Écriture en contexte** — dialogs depuis une case **+** sélection de plage de cases pour définir une période | nouveau besoin | `ecriture-en-contexte` | rang 1 |
| 4 | **Persistance Mongo du reste du domaine** (slots / périodes / transferts) — complète le palier 13 | nouveau besoin (technique) | `persistance-domaine` | tout l'usage |
| … | *(petites évolutions IHM, non prioritaires seules)* palette/picker de couleur dans l'écran config · harmonisation de teinte légende↔case (NON-bug) · onglets config (faible conviction PO) | évolution | — | usage |

> **Révision d'arbitre actée (à porter au `docs/BACKLOG.md`)** — La **config foyer
> persistante** (rang 1) tire la **persistance Mongo devant l'usage, bornée à la config
> foyer** ; le palier 13 (persistance réelle) commence **par son premier client** (la config
> durable, palier 13↔8). La persistance du **reste du domaine** reste **en queue** (rang 4),
> derrière l'usage. La récurrence (rang 2) et l'écriture en contexte (rang 3) sont placées
> derrière le rang 1.

## Prochain sujet → make-gherkin

- **Sujet** : `config-foyer-persistante` — Config foyer : ajout/édition d'acteurs **+**
  persistance Mongo bornée à la config foyer.
- **Périmètre** : (1) **AJOUT/édition d'acteurs** au foyer (parent / autre / nounou), au-delà
  du rename/recolor du seed déjà livré au s08 ; (2) **persistance Mongo de la SEULE config
  foyer** (référentiel acteurs : noms, couleurs, acteurs ajoutés) via un **adaptateur de
  droite** derrière les ports existants (`IReferentielResponsables` / `IPaletteCouleurs` /
  `IEditeurConfigurationFoyer` inchangés). Observable Gherkin pressenti : « j'ajoute la nounou
  → elle apparaît dans l'écran config et dans la grille (case + légende) ; **après
  redémarrage du serveur, elle est toujours là** » (fin de la volatilité pour la config
  foyer). Le reste du domaine (slots / périodes / transferts) **reste InMemory**.
- **Hors périmètre (reporté)** : la **persistance Mongo du reste du domaine** (slots /
  périodes / transferts) = **rang 4**, derrière l'usage ; la **récurrence des périodes**
  (rang 2) ; l'**écriture en contexte** — dialogs + sélection de plage (rang 3). Backlog non
  prioritaire seul : palette/picker de couleur, harmonisation de teinte légende↔case, onglets
  config.

## Risques & questions encore ouvertes

- **R1 — Débordement ~2h IA (garde-fou découpe)** — Le sujet combine **deux** choses : (a)
  l'ajout/édition d'acteurs et (b) un **adaptateur de persistance Mongo réel** (connexion,
  sérialisation du référentiel, seed-au-démarrage durable, tests d'intégration store). Si
  Mongo déborde la config foyer, ou si l'ajout ouvre la gestion d'identifiants/cases
  orphelines, le sprint dépasse la fenêtre. **Tranche de secours à cadrer au make-gherkin** :
  persister D'ABORD le référentiel acteurs **déjà semé** (rename/recolor livrés) derrière
  Mongo, PUIS l'ajout d'acteurs — ou l'inverse (ajout volatile d'abord, persistance derrière).
  **Couper, ne pas reporter en bloc.**
- **R2 — Ajout d'acteur = nouvel arbitrage de découpe** — Créer un acteur introduit la
  **génération d'identifiants stables neufs**, l'impact sur la **légende** (dédoublonnée par
  id) et le risque de **cases orphelines**. Décision CP du s08 l'avait **exclu** de
  l'incrément volatile pour cette raison. À **borner explicitement au make-gherkin** (ex.
  ajout sans suppression d'abord ; pas d'édition du cycle de fond ici).
- **R3 — Révision d'arbitre à tracer sans déraper** — Mongo devant l'usage est une
  **exception bornée à la config foyer**. Risque de **cliquet** : que le reste du domaine
  suive devant l'usage. Garder la persistance du reste **en queue** (rang 4) ; la borne doit
  être écrite noir sur blanc dans `docs/BACKLOG.md`.
- **R4 — Acceptation runtime obligatoire (rempart anti vert-qui-ment)** — La persistance doit
  être prouvée sur un **store Mongo réel** (redémarrage → l'acteur ajouté/édité réapparaît),
  pas par un test à doublures. Et l'ajout d'acteur doit se voir sur une **grille (case +
  légende) réellement câblée** (front WASM + API distante), conformément au risque spec
  « vert qui ment sur la grille » / « config foyer ».
- **R5 — Légende ≠ bug** — Le retour `/planning` est une **évolution de teinte** (pastille
  saturée vs case pâle, **même token résolu** ; cf. note confrontation HEAD), pas un défaut.
  **Ne jamais** l'envoyer en `/3` ciblé. À regrouper avec l'ergonomie config (palette/picker)
  quand il remontera, pas un sujet seul.
- **R6 — T3 (liste éditable) DÉJÀ levé** — L'écran `ConfigurationFoyer` ne listait que
  `parent-a`/`parent-b` (Notes de contexte du retours), mais l'**IHM finale du s08 liste les 4
  acteurs semés**. **Ne pas reporter** comme reste à faire.
- **R7 — Risque d'adoption du second parent (mortel)** — repoussé au palier 12 (auth) ; aucun
  des sujets ci-dessus ne le lève. À ne pas laisser glisser indéfiniment derrière la technique.
