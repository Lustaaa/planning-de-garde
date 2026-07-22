# Sprint 39 — Rétrofit flake SignalR *TempsReel* (solder la dette à la cause)

> **Goal G2 (tranché PO — délégation, goal 1 du SM)** : **SOLDER À LA CAUSE** la dette de test
> *flake SignalR TempsReel* (`FrontWasm*TempsReel*` + blast-radius I/O SMTP/Mongo s29), qui a
> franchi le seuil critique : **baseline mesuré ~40-50% de rouge full-suite EN PARALLÈLE même sur
> `main`** (dev-team, s38). Le vert du gate ne tient **plus que par la béquille `-Serial`**
> (mitigation posée s36, étape gate par défaut s37) qui **masque une infra de test structurellement
> cassée** — ce qui menace le **rempart anti-régression** (une vraie régression pourrait se cacher
> derrière le rouge « flake » toléré). Chaque nouveau client SignalR (config, auth, graphe s38)
> **amplifie** la course. **5ᵉ+ montée chiffrée**, repoussée plusieurs sprints : c'est le moment
> craft de payer, **avant** d'ajouter une tranche temps-réel de plus.
>
> **Sprint de DETTE / INFRA DE TEST** — pas de scénario produit métier @back/@ihm. Les « critères »
> ci-dessous sont des **acceptations VÉRIFIABLES et MESURABLES sur la suite elle-même** (taux de
> rouge full-suite en parallèle), pas des comportements produit. **Zéro `src/` produit ne change**
> (seul le harnais / la config de parallélisme des tests bouge) : si un `src/` produit devait bouger,
> c'est une **régression démasquée** (cf. garde ci-dessous), pas le scope.
>
> **Cause visée (connue, chiffrée backlog s29/s36)** : les tests à **I/O réel** (SignalR temps réel
> + SMTP/Mongo) **s'exécutent en parallèle** et **se courent après** (timing SignalR/Docker) →
> collections xUnit **non isolées**. Le remède ciblé : **collections xUnit non parallèles** pour les
> assemblies/tests à I/O réel + **helper bUnit partagé** convergé + audit de la course de convergence
> multi-clients (distincte de la course d'énumération déjà gardée s13).

## Avancement — 4/4

| # | Critère (vérifiable/mesurable sur la suite) | Type | Statut |
|--:|----------|------|:------:|
| 1 | **Mesure du BASELINE AVANT** : ≥5 runs full-suite EN PARALLÈLE (`test.ps1` **sans** `-Serial`) sur l'état courant `main` ; **chiffrer le taux de rouge** (attendu ~40-50%) et **nommer les tests rougissants** (confirmer qu'ils sont *TempsReel*/I/O, pas déterministes). Mesure **consignée** dans ce fichier (§ Mesures). | infra/mesure | ✅ |
| 2 | **Sérialiser À LA CAUSE** les tests à I/O réel — collections xUnit **non parallèles** ciblées sur `FrontWasm*TempsReel*` + blast-radius SMTP/Mongo (s29). **Périmètre EXPLICITE et MINIMAL** : sérialiser **ce qui se court après**, pas tout le harnais aveuglément (une sérialisation trop large cache au lieu de guérir). Justifier le périmètre retenu dans le fichier. | infra | ✅ |
| 3 | **Converger le helper bUnit partagé** des tests *TempsReel* (un seul point de setup/teardown du hub de test) + **auditer la course de convergence multi-clients** (2ᵉ écran qui reprojette) — **distincte** de la course d'énumération **déjà gardée s13** (ne pas la re-garder, ne pas la casser). | infra | ✅ |
| 4 | **Preuve du résultat — MESURE APRÈS** : **0% rouge sur ≥5 runs full-suite EN PARALLÈLE** (`test.ps1` sans `-Serial`) ; total de tests **inchangé** (aucun test supprimé/désactivé pour verdir). Mesure consignée (§ Mesures). En conséquence : **réduire/retirer la dépendance au `-Serial`** au gate (recommandation portée en clôture). | infra/mesure | ✅ |

> **⚠️ GARDE anti-régression-masquée (rempart, décision SM — le cœur de ce sprint).** La sérialisation
> doit **TUER une course de test**, **jamais** cacher un bug produit. Discriminateur **obligatoire**
> (triage durci rétro s21, **maintenu**) : un rouge est **flake** ssi il est **intermittent** (vert
> ≥1/N en isolation, ou rouge **seulement** sous charge de suite parallèle). Un rouge **N/N
> déterministe en isolation = RÉGRESSION** → **STOP, escalade, on corrige à la cause dans `src/`**,
> **on ne sérialise PAS** pour le faire taire (ce serait un vert-qui-ment, proscrit). Avant tout
> classement « flake » : **re-run EN ISOLATION x2-3**. La sérialisation ne s'applique **qu'aux courses
> avérées intermittentes**.

> **⚠️ GARDE « sérialisation ciblée, pas rideau » (décision SM).** Sérialiser **tout** le harnais
> ferait passer le gate mais **masquerait** la cause et **allongerait** le temps de suite sans rien
> guérir. Le critère 2 exige un **périmètre minimal explicite** : n'isoler que les collections dont
> l'I/O réel se court après. Si, mesure à l'appui, la sérialisation ciblée **ne suffit pas** à
> atteindre 0% (critère 4), la dev-team **remonte au SM** (élargir le périmètre est une **décision**,
> pas un réflexe) plutôt que de tout sérialiser en silence.

> **⚠️ Ordre imposé.** Critère 1 (baseline AVANT) **d'abord** — sans chiffre de départ, le gain du
> critère 4 n'est pas prouvable (« amélioration ou rien » exige la mesure). Puis 2 → 3 → 4 (preuve
> APRÈS). Les critères 2 et 3 peuvent se chevaucher techniquement mais **chacun est asserté** par sa
> propre mesure/vérification.

## Critères d'acceptation détaillés

### 1 — Baseline AVANT (mesure) @infra
```
Étant donné l'état courant de `main` (aucune modification du harnais)
Quand je lance la suite COMPLÈTE ≥5 fois EN PARALLÈLE (`test.ps1` SANS `-Serial`, Docker actif)
Alors je consigne, pour chaque run, vert/rouge + le nom des tests rougissants
Et je chiffre le TAUX DE ROUGE (runs rouges / total runs) — objectivant le point de départ ~40-50%
Et je confirme que les rougissants sont bien des courses *TempsReel*/I/O intermittentes
  (re-run isolé x2-3 vert ≥1/N) — PAS des rouges déterministes (qui seraient des régressions)
```

### 2 — Sérialisation ciblée à la cause @infra
```
Étant donné les collections de tests à I/O réel identifiées au critère 1
Quand je place les tests `FrontWasm*TempsReel*` + le blast-radius SMTP/Mongo (s29) dans des
  collections xUnit NON PARALLÈLES ciblées (périmètre minimal, explicité dans § Décisions)
Alors ces collections ne s'exécutent plus concurremment entre elles (la course est neutralisée)
Et le reste de la suite conserve son parallélisme (aucune sérialisation aveugle du harnais entier)
Et aucun test n'est supprimé, désactivé, ni son assertion affaiblie
```

### 3 — Helper bUnit partagé + audit convergence @infra
```
Étant donné les tests *TempsReel* qui montent chacun un hub/écran de test
Quand je converge leur setup/teardown sur un HELPER bUnit PARTAGÉ (un seul point)
  et j'audite la course de convergence multi-clients (2ᵉ écran reprojetant, s20/s38)
Alors la course de convergence est neutralisée SANS re-garder la course d'énumération s13
  (distincte, déjà gardée — ni cassée, ni dupliquée)
Et le comportement asserté par chaque test *TempsReel* est INCHANGÉ (même contrat de convergence)
```

### 4 — Preuve APRÈS + béquille `-Serial` (mesure) @infra
```
Étant donné le harnais rétrofité (critères 2 + 3 en place)
Quand je relance la suite COMPLÈTE ≥5 fois EN PARALLÈLE (`test.ps1` SANS `-Serial`, Docker actif)
Alors 0% de rouge sur les ≥5 runs (déterministe vert en parallèle)
Et le TOTAL de tests est INCHANGÉ vs baseline (aucun verdissement par retrait/skip)
Et la mesure APRÈS est consignée (§ Mesures) à côté du baseline (gain chiffré, « amélioration ou rien »)
Et je RECOMMANDE en clôture : `-Serial` peut redevenir parallèle au gate (ou rester `-Serial`
  par prudence de ceinture-et-bretelles) — décision de clôture, tracée
```

## Mesures (à remplir par la dev-team)

> Consigner ici, chiffres à l'appui (« amélioration ou rien » — la rétro/clôture s'appuie dessus).

- **AVANT (critère 1)** — état `main` (aucune modif harnais), full-suite EN PARALLÈLE, **11 runs** cumulés
  (2 lots : 5 + 6), Docker actif. Total tests **695** constant à chaque run (jamais un test perdu).
  - **Taux de rouge : 4/11 ≈ 36 %** (lot 1 : 2/5 ; lot 2 : 2/6) — dans la fourchette annoncée ~40-50 %.
  - **Test rougissant (UNIQUE, intermittent) :**
    `FrontWasmConfigEnfantsTempsReelTests.L_onglet_Enfants_liste_ajoute_et_edite_un_enfant_via_la_modal_…`
    — toujours **1 seul** échec dans `PlanningDeGarde.Web.Tests`, jamais dans `Tests`/`Api.Tests`.
  - **Triage flake vs régression (durci s21) :** re-run **EN ISOLATION x3 → 3/3 VERT** ⇒ course de charge
    **intermittente**, PAS un rouge déterministe (donc **pas** une régression produit). Sérialisation légitime.
- **APRÈS (critère 4)** — harnais rétrofité (collection *TempsReel* sérialisée + 2 gardes déterministes),
  full-suite EN PARALLÈLE, **8 runs** + 1 run autoritaire `test.ps1` (parallèle) + 1 run `-Serial`, Docker actif.
  - **Taux de rouge : 0/8 (0 %)** en parallèle ; `test.ps1` parallèle **695/695 vert** ; `-Serial` **695/695 vert**.
  - **Total tests : 695** = baseline (aucun test supprimé / désactivé / skippé pour verdir).
  - **Gain chiffré : 36 % → 0 %** de rouge full-suite parallèle (« amélioration ou rien » : mesuré des deux côtés).

## Décisions / périmètre de sérialisation (dev-team)

### Périmètre retenu (critère 2) — collection `SignalRTempsReel`, non parallèle, ciblée
- **Fichier** : `tests/PlanningDeGarde.Web.Tests/SignalRTempsReelCollection.cs` —
  `[CollectionDefinition("SignalRTempsReel", DisableParallelization = true)]`.
- **Membres** : les **55 classes `FrontWasm*TempsReel*`** (attribut `[Collection("SignalRTempsReel")]` ajouté à
  chacune). Ce sont les tests à **I/O SignalR réel** (front WASM réel + `ApiDistanteFactory` réelle + vrai client
  SignalR long-polling + convergence multi-clients avec pousseur de diffusion continu) — les **plus lourds** et
  les seuls à **tenir des connexions persistantes** + attentes de convergence (10-15 s).
- **Effet** : ces 55 tests s'exécutent **un à un** et **jamais concurremment** au reste (la charge SignalR
  concurrente — cause de la course de charge — est supprimée à la racine).
- **Pas un rideau** : les **~213 autres** tests Web.Tests (composant bUnit pur + runtime plus légers) **gardent
  leur parallélisme** entre eux. On ne sérialise PAS les 152 tests `ApiDistanteFactory` en bloc (ce serait un
  rideau) — seulement les 55 `*TempsReel*` qui « se courent après ». `Tests`/`Api.Tests` inchangés (le blast-radius
  SMTP/Mongo s29 n'a **jamais** rougi dans les 11 runs baseline : l'échec était **toujours** dans Web.Tests).

### Découverte clé (rempart anti-régression-masquée — le cœur du sprint)
> **La sérialisation seule NE SUFFIT PAS et, prise isolément, aggrave le symptôme.** Une fois les 55 `*TempsReel*`
> forcées en séquence, la victime baseline (`ConfigEnfants`) passe de **intermittente (4/11)** à **déterministe
> (5/5 rouge)**. **Triage impératif** : le test reste **VERT 3/3 EN ISOLATION** ⇒ ce n'est **pas** une régression
> produit, mais une **course de convergence de test** que le parallélisme *masquait* et que la sérialisation
> *démasque*. On ne l'a donc **pas cachée** derrière la série : on l'a **soldée à la cause** (critère 3).

### Courses de convergence neutralisées (critère 3) — 2 gardes déterministes, 0 assertion touchée
1. **`FrontWasmConfigEnfantsTempsReelTests`** — course : la **continuation async de la 1ʳᵉ soumission** (qui ferme
   la modal d'ajout) est satisfaite *après* la ré-ouverture de la 2ᵉ modal, qu'elle **ferme** alors → `#form-enfant`
   disparaît **entre `Change` et `Submit`**. **Garde** : attendre la **fermeture observable** de la modal (DOM)
   avant de la rouvrir + `WaitForElement` sur la ré-ouverture (miroir de la 1ʳᵉ ouverture déjà gardée). Aucune
   assertion modifiée. *(Vérifié : collection `*TempsReel*` en séquence 59/59 vert x2.)*
2. **`FrontWasmImpersonationAuDessusDeLaConnexionRuntimeTests`** — course : `Connexion.OnInitializedAsync`
   (GET `/api/foyer/acteurs` **async**) peut se résoudre **tardivement** sous charge et **réécrire**
   `Session.ActeursIncarnables` *après* la surcharge manuelle du test → nounou réapparaît, le repli ne se déclenche
   plus. **Garde** : attendre que le **catalogue incarnable réel soit chargé** (contient nounou) avant de le
   manipuler. Aucune assertion modifiée. *(Intermittent rare — 1/6 baseline retrofit ; vert 3/3 en isolation.)*

> **Course d'énumération s13** (snapshot des abonnés côté diffusion) : **distincte**, **déjà gardée**, **ni
> re-gardée ni cassée** — aucune des 2 gardes ne la touche (elles portent sur la **frontière DOM/session** du test,
> pas sur l'énumération des clients).

### `-Serial` au gate (recommandation critère 4, à trancher en clôture)
Le parallèle est désormais **fiable (0/8 rouge)**. Recommandation : **conserver `-Serial` au gate par prudence
ceinture+bretelles** (coût quasi nul, supprime tout résidu de blast-radius cross-assembly I/O sous machine chargée)
tout en sachant que **le parallèle peut redevenir le défaut du gate** sans risque connu. Décision finale au SM.

## Hors scope (périmètre resserré, décision SM)

- **Refonte du harnais de test au-delà des *TempsReel* / I/O réel** — on cible la course avérée
  (SignalR temps réel + SMTP/Mongo s29), pas une réécriture générale du harnais.
- **Toute feature produit** — R3 complétude du couple, édition depuis le graphe, commandes inverses
  actif/admin (candidats /planning) : **hors sprint**. Zéro `src/` produit ne change.
- **Montée du driver Mongo / vulnérabilités transitives** (dette séparée) : hors scope.
- **Édition concurrente sous dialog (+4 backlog)** : dépend de ce rétrofit mais reste un sprint à part.

## Impact pipeline (à confirmer en clôture)

Si le rétrofit rend le parallèle **fiable** (critère 4 vert), l'étape gate `-Serial` (ajoutée s37,
`test.ps1 -Serial`, `RunConfiguration.MaxCpuCount=1`) **pourra redevenir parallèle** — ou **rester
`-Serial` par prudence** (ceinture + bretelles, coût quasi nul). **Recommandation à trancher en
clôture**, tracée au `JOURNAL-METHODE.md` (rétro conditionnelle : friction réelle = flake chronique,
fix = rétrofit — « amélioration ou rien »).

# Retours produit (PO)

> **NB gate** : sprint de **dette/infra de test** — **peu probable qu'il y ait un gate visuel G3
> classique** (aucune surface produit ne change). La revue portera sur la **stabilité MESURÉE** de la
> suite (taux de rouge AVANT/APRÈS, § Mesures) et sur le respect de la garde anti-régression-masquée.
> _(À remplir après revue.)_
