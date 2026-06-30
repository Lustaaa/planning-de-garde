# Retours sprint 13 — crud-acteurs-suppression

> Fichier de retours du sprint (méthode + produit) et journal des décisions
> autonomes du chef de projet. Créé au make-gherkin (cadrage périmètre/ordre).

## Retours méthode

_(à compléter au fil du sprint — appendé par le thread principal ; `tdd-auto` ne touche jamais ce fichier)_

## IA

> Observations méthode relevées par l'IA (non demandées par le PO), candidates pour `retro-sprint`.

| Date | Cible (agent/skill/command) | Observation | Recommandation |
|------|-----------------------------|-------------|----------------|
| 2026-06-28 | ihm-builder / tests *TempsReel* | La touche IHM du composant partagé `ConfigurationFoyer` (boutons + `@inject SessionPlanning`) a rendu **déterministe** une course `UnknownEventHandlerId` latente dans 7 tests *TempsReel* préexistants (select manipulé sans attendre l'énumération async). Masquée en suite complète (warmup), révélée seulement en runs isolés. Fix = garde standard `WaitForState(acteur-foyer)` déjà éprouvé par les tests frères, bas risque. | Codifier dans le skill `tdd-implement` / `ihm-builder` : après toute modif d'un composant Razor partagé, **balayer les tests runtime en ISOLATION** (pas seulement la suite complète) — le warmup de la suite masque les courses select-sans-garde. Envisager d'extraire le garde d'énumération en helper bUnit partagé pour éviter la répétition. |
| 2026-06-28 | tdd-auto (caractérisations) | Flake bUnit observé 1× sur un test `…TempsReel…` (Sc.3) puis vert en rerun isolé ; suite finale verte. Symptôme de la même course que ci-dessus, sous charge parallèle (fondation flaky P2 déjà notée Risques/D6). | Rattaché au point ci-dessus : le garde déterministe ajouté au Sc.9 couvre la cause racine. À vérifier en rétro que tous les *TempsReel* config/grille portent désormais le garde. |
| 2026-06-28 | chef-de-projet (gating) | Sc.7 : l'écran `ConfigurationFoyer` n'avait **aucun garde de rôle** (ni ajout, ni édition, ni cycle — `EstParent` n'existait que sur la grille). Sc.7 a gaté **uniquement** le bouton supprimer (périmètre tenu). Un durcissement du gating de **toutes** les écritures config est une décision CP/métier à part. | Signaler au PO/CP : décider si ajout/édition/cycle de la config doivent aussi être gatés Invité (cohérence règle 9). Candidat backlog `/4-retours`. |

## Retours produit (PO)

_(à compléter au gate visuel / clôture — porte sur l'usage de l'IHM, code/tests hors scope ici)_

### IHM - Configuration foyer (bouton supprimer + accusé)

- 

### IHM - Grille planning (case retombe sur fond / neutre)

- 

### IHM - Légende dédoublonnée (sans nom fantôme)

- 

### IHM - Gating Invité (aucun bouton supprimer)

- 

### IHM - API injoignable (message d'échec, store inchangé)

- 

### IHM - Temps réel (propagation multi-écrans sans rechargement)

- 

### IHM - général

- 

### Tech (optionnel)

- 

## Décisions autonomes (chef de projet)

### D1 — Périmètre & ordre des drivers : backend d'abord (option A, discipline RED de C)

**Contexte.** make-gherkin demande comment cadrer le périmètre et l'ordre des
drivers de la tranche suppression, qui touche plusieurs couches (Application :
handler + repli de résolution + persistance Mongo runtime ; IHM : bouton
supprimer, liste relue, légende dédoublonnée, gating Invité, échec API, temps
réel SignalR).

**Décision.** Option **A — backend d'abord, IHM en fin**, cadrée par la
discipline RED de l'option C. Conforme CLAUDE.md (« scénarios s'arrêtent à la
frontière Application ; IHM Blazor + SignalR réel = phase finale `ihm-builder` »).
Option B (mélange frontière Application/IHM par écran) **rejetée** : contredit
backend-d'abord.

Découpe :
- **Drivers cœur (vrai RED, frontière Application, prouvés runtime store Mongo réel)** :
  (1) suppression d'un acteur **autorisée** + acteur disparaît du store ;
  (2) repli de la **surcharge orpheline → fond** (le cycle reprend) ;
  (3) repli **surcharge orpheline → neutre** quand l'index n'est ni mappé ni résolu (sans nom fantôme) ;
  (4) acteur supprimé **mappé au cycle de fond → index non mappé → neutre** (sans nom fantôme).
- **Lot IHM final (`ihm-builder`)** : bouton supprimer + liste relue + message
  non bloquant + légende dédoublonnée + gating Invité + échec API + temps réel
  SignalR. Ces concerns **réexercent des mécaniques déjà livrées** (gating règle 9
  mutualisé sur le déclencheur ; échec API règle 28 ; légende dédoublonnée
  règle 18 ; registre avertissement-à-part règles 16/28) → **caractérisations
  groupées**, pas de nouveaux drivers RED.

**Acceptation runtime obligatoire** (règle 6, Risques) : prouver sur front WASM +
API distante + store Mongo réel (l'acteur retiré disparaît du store **et** ses
cases retombent sur fond/neutre), pas par doublure.

**Fondements.** CLAUDE.md (backend d'abord) ; spec v13 règle 6, règle 30
(borne anti-cliquet : transfert/reste du domaine restent InMemory ; config foyer
déjà persistée Mongo) ; règles 9/16/18/28 (mécaniques acquises).

### D2 — « Dernier responsable d'un enfant » : PAS de porte G1, le repli neutre suffit

**Contexte.** Candidat G1 soulevé par make-gherkin : interdire la suppression du
dernier responsable d'un enfant — vrai trou métier ou repli suffisant ?

**Décision (tranchée, pas d'escalade).** Le **repli de neutralisation suffit** ;
aucune porte G1 ouverte, aucune variante refus/réaffectation.

**Rationale.**
- La liaison **enfant ↔ responsable contraignante** n'existe pas encore dans le
  modèle : la déclaration des enfants/parents (épics É1/É2) est **non livrée** (⬜).
  Il n'y a donc pas de « dernier responsable d'un enfant » à protéger
  aujourd'hui — pas de vrai trou métier au palier 8.
- La **neutralisation par repli** (règle 6) garde la grille **cohérente et
  lisible** : pas de nom fantôme, pas d'état cassé. Rien ne justifie un refus.
- Un refus **contredirait** la règle 6 (« suppression autorisée — pas de refus
  si références existantes, qui contredirait l'additivité et le repli neutre »).
- L'invariant « toujours deux parents » (règle 3) est un palier futur
  (É1/É2 déclaration), **pas** un bloqueur de suppression maintenant.

Une porte G1 ne s'ouvrirait que si le make-gherkin révélait un **vrai** trou
métier neuf ; ce n'est pas le cas ici.

### D3 — Suppression d'un acteur inexistant / déjà supprimé : idempotent silencieux

**Décision.** Idempotent. Supprimer un acteur absent/déjà supprimé est un
**no-op qui réussit** (état final identique : acteur absent), **grille
inchangée**, **aucune dialog d'erreur**. Caractérisation légère, **pas** un
chemin de refus.

**Rationale.** DELETE idempotent (convention DDD/REST) + philosophie non-refus de
la règle 6 (surfacer un refus contredirait l'esprit « suppression autorisée,
issues non bloquantes »).

### D4 — Périmètre : suppression des acteurs seedés ET ajoutés

**Décision.** La suppression vise **tout acteur du référentiel**, seedé comme
ajouté — uniformément derrière `IEditeurConfigurationFoyer` /
`ConfigurationFoyerMongo`.

**Rationale.** Aucun fondement spec pour protéger le seed ; le référentiel est
homogène derrière les ports (règle 6, règle 30). La protection du « dernier
parent » relève de É1/É2 (futur), pas d'un bloqueur ici (cf. D2).

### D5 — Libellé du message non bloquant : « Acteur supprimé »

**Décision.** Accusé **« Acteur supprimé »**, affiché **à part, non bloquant**,
registre avertissement-à-part (règles 16/28), aligné sur l'accusé terse
« Transfert défini » (règle 25).

**Rationale.** Cohérence de registre. Un libellé enrichi (mention du repli sur le
fond/neutre) est une **évolution de surface non bloquante**, hors plus-petit-incrément.

### D6 — Validation du plan Gherkin (9 scénarios) : dérive proprement de la spec v13

**Contexte.** make-gherkin (done:true) soumet le plan d'écriture : 9 scénarios,
sujet `crud-acteurs-suppression` (palier 8). Vérifier qu'il dérive de la spec v13
pour ordonner l'écriture sans déranger le PO (aucun arbitrage résiduel).

**Décision.** Plan **validé**, ordre d'écriture approuvé tel quel. Aucune porte
PO (G1/G2) à ouvrir.

**Vérification spec v13.**
- Sc.1 store relu = règle 6 + acceptation runtime obligatoire (Risques). ✓
- Sc.2 surcharge orpheline → fond = règles 6/12/15. ✓
- Sc.3 → neutre sans nom fantôme = règles 6/15/19. ✓
- Sc.4 acteur mappé au cycle → index non mappé → neutre = règles 6/11/19. ✓
- Sc.5 idempotence no-op qui réussit = D3 (RED légitime à la frontière
  Application : avant `Supprimer`, l'assertion échoue). ✓
- Sc.6 bouton + liste relue + accusé + légende dédoublonnée = règles 6/18 + D5. ✓
- Sc.7 gating Invité = règle 9 (mutualisé sur le déclencheur). ✓
- Sc.8 API injoignable = règle 28. ✓
- Sc.9 temps réel = règle 30/diffusion + règle 11. ✓
- Amorce technique (handler `SupprimerActeur`, port `Supprimer(acteurId)`
  InMemory+Mongo, endpoint canal `POST /api/canal/supprimer-acteur`, CQRS, id
  stable opaque, borne anti-cliquet) = règles 28/30 + Architecture CLAUDE.md
  (écriture = canal requête/réponse). ✓
- Couverture règle 6 nominal (Sc.1/2) / limite (Sc.3/4) / erreur (Sc.5) cohérente ;
  aucune règle de résolution neuve (priorité surcharge>fond>neutre réexercée). ✓
- Fix dropdown « Acteur du foyer » (dette règle 5) **correctement exclu** des
  scénarios (fix ciblé tête de sprint, pas un Gherkin CRUD — Risques). ✓

**Pas de porte PO.** « Dernier responsable » déjà tranché (D2, pas de G1) ; le
sprint goal dérive du backlog/palier 8 (pas de re-cadrage G2).

**Point de vigilance (non bloquant).** Sc.9 (temps réel SignalR) touche la
fondation flaky sous exécution parallèle (P2, Risques) : à valider en
**acceptation runtime / G3**, ne pas en faire un filet de régression automatisé
stable. Aucune incidence sur l'écriture du plan.

**Fondements.** Spec v13 règles 6/9/11/12/15/18/19/28/30 + Mécaniques + Risques ;
CLAUDE.md (backend d'abord, canal requête/réponse, acceptation runtime) ;
décisions D1–D5.

### D7 — Plan d'implémentation (tdd-analyse) validé + contrat d'existence tranché

**Contexte.** tdd-analyse soumet la décomposition d'exécution : 9 scénarios, 5 tests
backend (3 drivers réels Sc.1/2/4, 2 caractérisations early-green Sc.3/5), routage
Sc.1→Sc.5 backend `tdd-auto` / Sc.6→Sc.9 🖥️ IHM `ihm-builder` (cascade câblage partagé).
Point ouvert remonté : forme du **contrat d'existence d'acteur** pour le filtre dans
`GrilleAgendaQuery` (`IEnumerationActeursFoyer` vs nouveau `ActeurExiste`).

**Décision.**
1. **Plan validé tel quel**, enchaînement implémentation approuvé. La décomposition
   dérive proprement de la spec et des décisions D1–D6 (drivers = vrais RED frontière
   Application ; caractérisations Sc.3/5 = early-green **attendu**, filet anti-régression
   pas drivers ; cascade IHM Sc.7/8/9 réutilisant le câblage Sc.6 = batchables). Aucune
   porte PO : pas de trou métier neuf (« dernier responsable » déjà tranché D2).
2. **Contrat d'existence = réutiliser `IEnumerationActeursFoyer` existant** (méthode
   `EnumererActeurs()`, déjà réalisée par les deux stores InMemory + Mongo), **pas** de
   nouvelle méthode `ActeurExiste`. Injection dans `GrilleAgendaQuery` **optionnelle/
   nullable** (sur le modèle de `IReferentielCycleDeFond? cycle = null`) : null → aucun
   filtrage, comportement actuel préservé (rétro-compat tests existants). Le filtre
   neutralise l'id orphelin → `null` **avant** la résolution (`GrilleAgendaQuery.cs` L69
   surcharge `periode?.ResponsableId`, branche fond `ResponsableDeFond`), **en amont** du
   `NomDe` L71 qui retombe sinon sur l'id brut (= nom fantôme à éviter). Sc.2 = branche
   surcharge, Sc.4 = branche fond : deux points de test distincts, conformes à l'analyse.

**Rationale.** Plus-petit-incrément + réutilisation : `IEnumerationActeursFoyer` est le
port de lecture déjà câblé sur les stores ; ajouter `ActeurExiste` dupliquerait une
capacité existante sans gain. L'injection nullable évite de toucher les constructeurs
appelants non concernés (filet de régression 161/161 préservé). Lookup O(1) si l'impl
matérialise un `HashSet` côté query — détail laissé à `tdd-auto`.

**Fondements.** `GrilleAgendaQuery.cs` (L69/L71) ; `IEnumerationActeursFoyer.cs` ;
CLAUDE.md (CQRS lecture seule, ports) ; spec v13 règles 6/11/15/19 ; D1/D6.

### D8 — Consolidation v14 : angle mort gating config (Sc.7) — règle 9 conservée, durcissement séquencé en candidat impersonation

**Contexte.** `spec-consolidation` (v13 → v14) signale une collision avec la
règle 9 (gating écriture) : le sprint 13 (Sc.7) a révélé que l'écran
`ConfigurationFoyer` ne gate l'Invité **que** sur le bouton supprimer ; l'ajout,
l'édition d'acteur et l'édition du cycle de fond y restent ouverts à un Invité.
Trois options : (1) conserver règle 9, signaler l'angle mort en Risques, séquencer
le durcissement complet comme candidat du make-gherkin impersonation ; (2) étendre
règle 9 dès v14 ; (3) neutre v14, trancher en porte.

**Décision (tranchée, pas d'escalade G1) — Option 1.** La v14 **ne révise pas la
règle 9** : elle (a) **signale l'angle mort en Risques** (gating config partiel :
seul le bouton supprimer est gaté Invité ; ajout / édition / cycle restent
ouverts), et (b) **séquence le durcissement complet comme candidat du
make-gherkin `impersonation-bornee`** (même écran, même notion de rôle/identité).
Révision de règle **hors boucle**, décision CP/métier confirmée au make-gherkin.

**Rationale (résolution déterministe, pas de conflit de valeur).**
- Le **backlog l'a déjà séquencé ainsi** : `99-sprint13-besoins-fin-itération.md`
  §Séquence pt 2 — « Durcissement du gating config (règle 9) — cadrage adjacent /
  candidat de l'impersonation… à confirmer au make-gherkin. **Décision CP/métier,
  pas un retour produit PO** ». Aucun re-arbitrage requis : j'exécute la séquence.
- **La spec reflète le livré (anti vert-qui-ment).** La consolidation /5 inscrit
  ce qui est **livré** + les besoins ; le gating config complet n'a **pas** été
  livré (Sc.7 ne gate que supprimer). Écrire en règle « toute écriture config est
  gatée Parent/Admin » (Option 2) ferait **mentir la spec** (règle satisfaite alors
  que l'IHM ne l'est pas), sans scénario ni acceptation runtime — pré-arbitrage
  d'un sujet sans drivers. Rejeté.
- **Convention « Révisions de règle hors boucle »** (spec règle 179-187 / Risques) :
  une demande qui contredit/étend une règle actée attend le palier qui la porte —
  ici le make-gherkin impersonation, naturellement adjacent (même `ConfigurationFoyer`,
  même contexte rôle).
- **Pas d'Option 3 (neutre muet) :** taire l'angle mort perdrait le signal Sc.7 ; le
  backlog demande explicitement de le **porter en Risques** pour pilotage a posteriori.
- **Pas de porte G1 :** aucun trou métier. L'intention métier est **déjà actée** —
  règle 8 (« Autre = consultation et édition limitée à ses propres infos ») et
  règle 9 (Parent/Admin seuls écrivent). Le manque est un **écart d'implémentation /
  de couverture de scénarios** sur l'écran config, pas un conflit de valeur. G1 ne
  s'ouvrirait que si le make-gherkin impersonation révélait un vrai trou métier neuf.

**Fondements.** `99-sprint13-besoins-fin-itération.md` (§Séquence pt 2, §Risques
frontière impersonation) ; spec v13 règles 8/9, convention « Révisions de règle
hors boucle », Risques (impersonation bornée vs auth réelle) ; CLAUDE.md
(consolidation reflète le livré, acceptation runtime anti vert-qui-ment) ; D2/D6
(porte G1 réservée au vrai trou métier).

### D9 — Synthèse de consolidation v13 → v14 validée : cohérente pour ordonner l'écriture, aucun conflit de valeur résiduel

**Contexte.** `spec-consolidation` soumet la synthèse v14 (verbatim) avant
écriture de `docs/14-specification.md`. Vérifier sa cohérence pour ordonner
l'écriture sans déranger le PO, et qu'aucun conflit de valeur (porte G1) ne
subsiste — la seule collision (gating config règle 9) ayant déjà été tranchée en
D8 (Option 1).

**Décision (tranchée, pas d'escalade G1).** Synthèse **validée**, écriture vers
`docs/14-specification.md` **autorisée telle quelle**. Aucune porte PO.

**Vérification de cohérence (chaque assertion adossée à une source livrée).**
- **Suppression LIVRÉE 9/9 · 196/196** — `00-sprint13-suivi.md` (tableau 9/9 ✅,
  « Suite complète : 196/196 verte, stable ≥3× », acceptation runtime IHM 4/4 +
  intégration Mongo réel Sc.1). Repli surcharge→fond→neutre sans nom fantôme,
  acteur mappé cycle→index non mappé→neutre, idempotence, accusé « Acteur
  supprimé » non bloquant, gating Invité bouton supprimer, échec API, temps réel
  SignalR = conformes aux scénarios livrés. ✓
- **Marqueurs « PROCHAIN SUJET » suppression → « LIVRÉ »** — passage cohérent :
  spec v13 portait la suppression en « prochain sujet » (L93/189/315/422/466) ;
  la v14 doit les passer au passé. Reflète le livré (anti vert-qui-ment). ✓
- **PROCHAIN SUJET = impersonation bornée (palier 8 tranche 2, É10+É2, G2 PO)** —
  `99-sprint13-besoins-fin-itération.md` §Décision G2 + §Prochain sujet ; spec v13
  L315-332. Borne dure (pas d'OAuth/comptes/sessions/prise en main, aucune
  persistance neuve) = besoins L32-35/L48-50 + §Risques. Périmètre cadré au
  make-gherkin = besoins L45-50. ✓
- **Collision gating règle 9 → Option 1** — strictement conforme à **D8** : v14 ne
  révise pas règle 9 ; angle mort Sc.7 en Risques ; durcissement séquencé candidat
  make-gherkin impersonation. ✓
- **Séquencés sans règle neuve** — rétrofit garde TempsReel SignalR (P2),
  calendrier navigable (palier 9 rang +3), édition concurrente (P3 différée) =
  besoins §Séquence pt 3/4 + §Risques. ✓
- **Borne anti-cliquet : seule config foyer durable (Mongo)** — règle 30 ;
  `00-sprint13-suivi.md` L101-102. ✓

**Rationale (pas de conflit de valeur).** Toute assertion de la synthèse dérive
d'une source livrée ou d'une décision déjà tranchée (D8) ; aucune n'invente de
règle métier ni ne fait mentir la spec. L'unique collision est résolue de façon
déterministe (D8). Aucun trou métier neuf → G1 fermée : G1 ne s'ouvrirait que si
l'écriture révélait un vrai conflit de valeur, ce qui n'est pas le cas.

**Fondements.** `00-sprint13-suivi.md` (9/9, 196/196, Sc.7 driver réel, borne
anti-cliquet) ; `99-sprint13-besoins-fin-itération.md` (§Décision G2, §Prochain
sujet, §Séquence, §Risques) ; spec v13 (marqueurs PROCHAIN SUJET, règles 9/30) ;
**D8** (Option 1 gating config) ; CLAUDE.md (consolidation reflète le livré).

### D10 — Rétro méthode sprint 13 : 6 actions bas risque auto-appliquées, aucune escalade G1

**Contexte.** Priorisation des actions de la rétrospective méthode du sprint 13.
Palier d'autonomie 0 (conservateur) : appliquer les amendements de pipeline à
faible risque, n'escalader en G1 que les changements structurels/risqués (refonte
d'agent, suppression d'un gate). 6 actions proposées, toutes des amendements
ciblés de fichiers du pipeline.

**Décision (tranchée, pas d'escalade G1).** Les **6 actions sont retenues et
auto-appliquées** (sélection complète `[1,2,3,4,5,6]`). Aucune n'est structurelle :
zéro suppression de gate, zéro refonte d'agent. Toutes sont soit des **garde-fous
additifs** (1, 2, 3, 6), soit des **restrictions réductrices de risque** (4, 5).

1. **`ihm-builder.md`** — amender la puce « Balayage runtime après composant
   partagé » : relancer les tests *TempsReel* **en isolation** (pas seulement la
   suite complète) après modif d'un Razor partagé ; nommer le symptôme
   `UnknownEventHandlerId` (course masquée par warmup de suite). Trace directe :
   §IA L16.
2. **`tdd-implement/SKILL.md`** — codifier l'extraction du garde
   `WaitForState(acteur-foyer)` en helper bUnit partagé + audit que tout test
   *TempsReel* config/grille le porte. Trace : §IA L16-17.
3. **`tdd-analyse.md`** — avant d'étiqueter « early-green câblage partagé »,
   vérifier par exploration code que le câblage prérequis (garde de rôle) existe
   **sur l'écran ciblé** ; sinon `@driver`. Garde-fou anti vert-qui-ment.
4. **`validation-visuelle.md`** — ajouter `Edit` au frontmatter + édits
   ciblés/append, **jamais de full Write** sur `99-sprint<NN>-retours.md` existant.
   **Réduit** le risque actuel (l'agent a `Write` seul → peut écraser les sections
   Méthode/IA/Décisions CP).
5. **`spec-consolidation.md`** — interdiction nommée de Write/Edit sur
   `99-sprint<NN>-retours.md` (seule sortie = nextSpec). Restriction, plus sûre.
6. **`chef-de-projet.md`** — codifier : gating de rôle partiel (un seul déclencheur
   gardé sur un écran à écritures multiples) → consigner l'angle mort en Risques du
   backlog + séquencer en candidat adjacent, **sans G1** si l'intention métier est
   déjà actée. Codifie le précédent **D8**.

**Rationale.** Palier 0 réserve l'escalade aux changements structurels/risqués
(suppression de gate, refonte d'agent) — aucune des 6 n'en est. Les actions 4 et
5 **diminuent** la surface de risque (un agent à `Write` large pouvant détruire le
journal de retours est le vrai danger ; les borner est conforme au conservatisme).
Les actions 1-3 et 6 ajoutent des garde-fous sans modifier de contrat de gate ni de
routage de pipeline. Toutes adossées à des observations terrain du sprint 13 (§IA
L16-18) ou à une décision déjà tranchée (D8).

**Fondements.** §IA L16-18 (course `UnknownEventHandlerId`, garde TempsReel, gating
config partiel) ; **D8** (séquençage de l'angle mort gating en candidat adjacent) ;
frontmatter `validation-visuelle.md` (`tools: …, Write`) et `spec-consolidation.md`
(`tools: …, Write, Edit`) ; CLAUDE.md (2 portes PO G2/G3, reste tranché par le CP).
