# Retours — sprint 14 (`impersonation-bornee`)

> Fichier de retours du sprint 14. Recueille les retours méthode/produit du PO et les
> décisions autonomes du chef de projet (CP). Consommé par `/4-retours` et la rétro.

## Retours produit (PO)

> Le code et les tests unitaires sont **hors scope** ici (revus en revue de code). Ces retours portent
> sur l'**usage de l'IHM** : ce qui marche, ce qui coince, ce qui manque à l'écran. Remplis les puces,
> puis lance `/4-retours`. Lancement de l'app : `pwsh .claude/skills/run/scripts/run.ps1`.

### IHM - général

-

### IHM - /planning

-

### IHM - /configuration

-

### Tech (optionnel)

- (contraintes techniques éventuelles ; laisser vide si aucune → bypass dans `/4-retours`)

## Idée pour la suite

-

## Consigne pour la suite

-

# Méthode (agents) — pour retro-sprint

> Retours à la volée du PO sur la **méthode** (agents/skills/commands), appendés par le thread principal
> pendant le sprint. Ne PAS confondre avec les retours produit ci-dessus.

| Date | Cible (agent/skill/command) | Retour | Décision prise |
|------|-----------------------------|--------|----------------|

## IA

> Observations méthode relevées par l'IA (non demandées par le PO), candidates pour `retro-sprint`.

| Date | Cible (agent/skill/command) | Observation | Recommandation |
|------|-----------------------------|-------------|----------------|
| 2026-06-28 | tdd-analyse (prédiction early-green) | La cascade early-green IHM a été **bien prédite** ce sprint : Sc.2/3/4 sont tombés early-green ATTENDUS (socle posé par Sc.1), zéro escalade CP parasite — contraste avec s11 (3 escalades inattendues). Le correctif rétro s13 A2 (vérifier le câblage prérequis avant d'étiqueter) a payé. | Conserver. Point positif à acter au bilan rétro (preuve que A2 fonctionne). |
| 2026-06-28 | ihm-builder (réentrance renderer bUnit) | Sc.5 (retour auto SignalR) a heurté un **stack overflow** par réentrance du renderer bUnit (clic-menu réentrant pendant la pompe de diffusion) ; corrigé en déplaçant le clic APRÈS l'arrêt de la pompe. Piège runtime non documenté dans la convention *TempsReel*. | Ajouter à la convention anti-flake *TempsReel* (`tdd-implement`/`ihm-builder`) : ne pas déclencher d'interaction réentrante (`@onclick`) pendant que la pompe de diffusion tourne — arrêter la pompe d'abord. |
| 2026-06-28 | ihm-builder / dette P2 | Flake *TempsReel* sous charge parallèle persiste (1/30, convergence SignalR, vert en isolation). Le garde `WaitForState` (rétro s13 A1) couvre la course `UnknownEventHandlerId` mais pas la convergence SignalR multi-écrans sous charge. | Le rétrofit P2 (backlog rang +3) doit cibler la **convergence SignalR multi-clients**, distincte de la course d'énumération déjà gardée. |

# Décisions autonomes (chef de projet)

## D1 — Périmètre : embarquer le durcissement du gating config sous condition ~2h (option 3)

**Contexte.** Question de périmètre du make-gherkin `impersonation-bornee` (palier 8, tranche 2).
Sprint goal G2 déjà tranché par le PO = **« Incarner (lecture) »** (impersonation lecture seule,
bandeau « Vous incarnez X », vue selon le rôle de l'incarné, gating règle 9 piloté par l'identité
incarnée, retour identité réelle, zéro persistance, PAS l'auth réelle du palier 16). Candidat
adjacent soumis : embarquer le **durcissement du gating de l'écran config** (angle mort Sc.7 s13 —
aujourd'hui seul le bouton supprimer est gaté `@if EstParent` ; ajout / édition d'acteur / édition
du cycle restent ouverts à un Invité).

**Décision.** **Option 3 — cœur prioritaire + durcissement embarqué SI ≤ ~2h une fois l'identité
effective posée ; sinon couper et re-séquencer.** Le cœur impersonation lecture seule est
non négociable (palier autonomie 0, conservateur) ; le durcissement config n'est embarqué que
s'il tient sous la borne ~2h, avec **son propre scénario + acceptation runtime** (pas de gating
inscrit « règle satisfaite » sans scénario — ce serait le pré-arbitrage interdit pointé en spec).

**Rationale.**
- C'est une **décision CP/métier explicitement déléguée** : la spec v14 (Risques, puce
  « Durcissement du gating… angle mort Sc.7 ») la qualifie « Décision CP/métier (pas un retour
  produit PO) », « candidate du make-gherkin de l'impersonation », « à confirmer au cadrage » —
  pas une porte G1. Aucun trou métier nouveau → pas d'escalade.
- **Synergie réelle, pas scope creep gratuit** : l'impersonation lecture seule pose une
  **identité effective** qui pilote le rôle ; le durcissement config n'est alors que l'extension
  du même `@if EstParent` (lu sur l'identité effective) à ajout/édition/cycle. Infra construite
  PAR le cœur → coût marginal faible.
- **Cohérence exigée par le goal lui-même** : « vue selon le rôle de l'incarné » + « gating
  règle 9 piloté par l'identité incarnée ». Incarner un Invité (lecture seule) tout en laissant
  l'écran config ouvert en écriture serait une **incohérence visible** que l'impersonation rend
  justement testable sur le même écran, même notion de rôle/identité.
- **Borne de découpe respectée** (corollaire de découpe + arbitre d'usage) : on ne pré-engage
  pas un périmètre qui pourrait déborder ~2h ; si le durcissement déborde une fois le cœur posé,
  on coupe et on re-séquence la révision de règle 9, sans toucher au cœur.
- **Borne dure tenue** : aucune persistance neuve, pas l'auth réelle du palier 16 (ni OAuth /
  comptes / sessions / prise en main / droits par rôle).

**Sources.** `docs/14-specification.md` (Risques : puces « Prochain sujet — impersonation bornée »,
« Durcissement du gating de l'écran config (angle mort Sc.7) — révision de règle 9 séquencée » ;
règle 9 ; règle 8 ; § Objectif & arbitrage, « Révisions de règle hors boucle » et « Corollaire de
découpe ») ; `docs/BACKLOG.md` (Prochains sprints, rang +1 P1, candidat adjacent).

## D2 — Concurrence : acteur incarné supprimé en cours d'incarnation → retour forcé identité réelle (option 1)

**Contexte.** Question du make-gherkin (concurrence, PAS un sprint goal) : un acteur peut être
SUPPRIMÉ depuis un autre écran (règle 6, suppression livrée s13 + propagation SignalR) PENDANT
qu'on l'incarne. Que fait la vue quand l'acteur incarné disparaît du référentiel en cours
d'incarnation ? Options : (1) retour forcé identité réelle ; (2) repli consultation neutre avec
bandeau pointant un acteur fantôme ; (3) hors périmètre (mono-utilisateur).

**Décision.** **Option 1 — retour AUTOMATIQUE à l'identité réelle** sur suppression concurrente :
bandeau « Vous incarnez X » retiré, vue restaurée à l'identité réelle de l'utilisateur principal.
Scénarisé comme **cas limite** ; comme il touche la **diffusion temps réel** (propagation de la
suppression vers l'écran qui incarne), **validé en acceptation runtime / G3**, pas en filet de
régression automatisé instable.

**Rationale.**
- **Extension cohérente de la neutralisation par repli (règle 6)** : la suppression d'un acteur
  fait retomber ses cases orphelines sur le **fond** ou le **neutre** « sans nom fantôme ». Le
  « neutre » de l'incarnation, c'est l'**identité réelle** du configurateur (état par défaut hors
  incarnation). L'option 1 applique exactement le même principe « la référence orpheline cesse de
  primer → repli » au contexte impersonation. Aucune règle neuve : c'est la projection d'un
  principe déjà acté.
- **Invariant « sans nom fantôme » (règles 18/19)** : un acteur supprimé quitte aussitôt la
  légende (plus de pastille ni de nom fantôme) ; un mapping orphelin retombe sur le neutre sans
  nom fantôme. L'**option 2 viole frontalement cet invariant** (bandeau pointant un acteur
  fantôme) → écartée.
- **Option 3 (hors périmètre) contredite par l'acquis** : la suppression d'acteur **propage déjà
  en temps réel** vers les autres écrans (règle 6, acceptation runtime tenue s13 : second écran
  voit la case orpheline retomber sans rechargement). Le scénario concurrent est donc **réel et
  observable**, pas une hypothèse mono-utilisateur ; le déclarer hors périmètre laisserait un
  bandeau incohérent à l'écran. Écartée.
- **Borne dure tenue** : zéro persistance neuve (l'incarnation reste en session), pas l'auth
  réelle du palier 16. La validation G3/runtime suit la consigne spec (risque « impersonation vs
  diffusion temps réel » : valider en acceptation runtime/G3, pas en filet automatisé flaky).
- **Pas un trou métier (pas G1)** : décision dérivable de règles déjà actées (6/18/19) → tranchée
  par le CP, pas escaladée.

**Sources.** `docs/14-specification.md` (règle 6 « neutralisation par repli… sans nom fantôme,
propagation SignalR » ; règle 18 « un acteur supprimé quitte aussitôt la légende, plus de nom
fantôme » ; règle 19 « index non mappé après suppression = neutre, sans nom fantôme » ; règle 8
« incarner un acteur déjà déclaré » ; Risques, puce « Impersonation bornée vs auth réelle » :
« si l'impersonation touche la diffusion temps réel, valider en acceptation runtime / G3 ») ;
acceptation runtime suppression d'acteur s13 (propagation SignalR vers un second écran).

## D3 — Source du TYPE de l'acteur incarné : extension READ-ONLY de l'énumération acteurs (option 1)

**Contexte.** Question de scaffolding du `tdd-analyse` (palier 0). Le cœur du sprint (règle 8 :
Admin/Parent incarné → menu écriture visible, Autre → masqué ; `EstParent` dérive sur l'identité
**effective**) exige de connaître le **TYPE** de l'acteur incarné. Or le référentiel d'acteurs
persisté ne porte **aucun type** : `ActeurFoyerVue/ActeurFoyer = (Id, Nom, Couleur)`,
`ActeurDocument` Mongo = Id/Nom/Couleur. La borne du sujet impose « zéro persistance neuve, aucun
port/handler d'écriture neuf ». Options : (1) extension READ-ONLY de l'énumération acteurs ;
(2) convention de préfixe d'identifiant ; (3) persister un champ type par acteur.

**Décision.** **Option 1 — surfacer le type via une extension READ-ONLY de l'énumération des
acteurs** (`IEnumerationActeursFoyer` / projection de lecture), **type issu de la déclaration seed
du foyer** ; les acteurs **ajoutés en session** sont typés **« Parent » par défaut** (pas de saisie
de type → pas de persistance neuve). L'identité effective ne fait que **LIRE** ce type pour piloter
`EstParent`. **Aucun port/handler d'écriture neuf, aucune persistance neuve.** Le **défaut de type
des acteurs ajoutés** (Parent) est laissé au make-gherkin pour scénarisation fine si un cas d'usage
l'exige (capter le type à l'ajout = nouvelle saisie/persistance → hors borne, palier 16).

**Rationale.**
- **Le type est un concept domaine déjà existant, pas une donnée neuve** : la mécanique de base
  « Trois types d'acteurs : Admin, Parent, Autre — chacun avec un affichage adapté à son type »
  et règle 8 posent le type au niveau de la **déclaration du foyer / seed** (règle 3 « toujours
  deux parents » vs règle 4 « acteurs autres »). Le surfacer en lecture n'invente rien : il expose
  un attribut déjà porté par la déclaration, jamais persisté faute de besoin jusqu'ici.
- **Borne anti-cliquet (règle 30) respectée à la lettre** : la spec dit noir sur blanc que
  « l'impersonation bornée ne tire **aucune persistance neuve** ». L'**option 3** (champ type au
  modèle Mongo + saisie à l'ajout) crée un **cliquet** (nouveau chemin d'écriture + persistance
  neuve) et appartient au palier 16 (auth réelle / modèle d'acteurs) → **écartée**.
- **Option 2 (préfixe d'id) écartée** : fragile, casse sur identifiants opaques, fait dériver un
  invariant métier (le type) d'une convention de nommage technique non garantie. Anti-pattern.
- **Cohérence CQRS / lecture seule** : l'impersonation est une **convenance de lecture** (cf. D1,
  goal « Incarner (lecture) ») ; piloter `EstParent` depuis une **projection de lecture** du
  référentiel respecte la séparation canal écriture / lecture (la lecture ne crée pas de port
  d'écriture). Cohérent avec `IEnumerationActeursFoyer` qui est déjà un accès de lecture pur.
- **Pas un trou métier (pas G1)** : la réponse est **dérivable** de la spec (règle 8 + mécanique
  « trois types » + borne règle 30) → tranchée par le CP. L'escalade G1 ne se justifierait que si
  le métier exigeait de **capter/persister** le type à l'ajout dès ce sprint — ce que la borne
  interdit explicitement.

**Sources.** `docs/14-specification.md` (règle 8 « Trois types d'acteurs… impersonation bornée,
incarner un acteur déjà déclaré » ; règle 30 + Borne anti-cliquet « l'impersonation bornée ne tire
aucune persistance neuve » ; mécaniques de base « Trois types d'acteurs : Admin, Parent, Autre » ;
§ Prochain sujet, borne dure « pas l'auth réelle du palier 16 ») ;
`src/PlanningDeGarde.Application/Interfaces/IEnumerationActeursFoyer.cs` (accès lecture pur, point
d'extension) ; `src/PlanningDeGarde.Api/Classes/SeedDonneesDemo.cs` (déclaration seed parent-a /
parent-b) ; `src/PlanningDeGarde.Web/State/SessionPlanning.cs` (`EstParent => Role == Parent`,
à rebrancher sur l'identité effective) ; décision D1 (identité effective pilote le rôle).

## D4 — Validation du plan d'implémentation sprint 14 (synthèse `tdd-analyse`)

**Contexte.** Demande de validation du plan d'implémentation (palier autonomie 0) avant d'enchaîner :
6 scénarios, 9 tests inner-loop ; routage 100% `🖥️ IHM → ihm-builder` (acceptation runtime app
câblée) ; cœur = extension `SessionPlanning` (identité réelle vs effective, `Incarner` /
`RevenirIdentiteReelle`, `EstParent` dérivé de l'effective) + extension READ-ONLY du contrat de
lecture acteurs (type seed, D3) ; drivers vs caractérisations/early-green ; ordre
cœur → concurrence → gating cuttable.

**Décision.** **Plan VALIDÉ tel quel, exécution autorisée.** Aucun ajustement requis.

**Rationale.**
- **Routage 100% IHM justifié, pas un raccourci** : le symptôme PO de chaque scénario est un fait
  d'usage runtime (bandeau, menu visible/masqué, gating effectif, retour auto sur diffusion) que
  bUnit seul ne prouve jamais (render mode, DI, transport, store) → acceptation sur app câblée
  réelle. Conforme au rempart anti vert-qui-ment du CLAUDE.md.
- **Drivers / caractérisations correctement distingués** : les early-green attendus (Sc.1 #1 état
  initial + #4 Admin, Sc.4 écriture sous identité réelle — le canal write ne lit jamais l'effective)
  sont **pré-classés en caractérisations à batcher**, pas en early-green inattendus → pas de
  round-trip CP parasite. Le câblage de cascade (gating grille `@if EstParent` + bouton supprimer
  config s13 suivent l'effective sans code neuf) est correctement identifié comme conséquence du
  cœur, pas comme travail neuf.
- **Ordre cœur → concurrence → cuttable respecte le corollaire de découpe** : Sc.1 (identité
  effective) pose l'infra dont dépendent Sc.2/3/5/6 ; Sc.6 (durcissement gating) reste **CUTTABLE
  ≤ ~2h** sans toucher au cœur (conforme à D1, option 3).
- **Bornes dures préservées** : D1/D2/D3 intégrées sans dérive — zéro persistance neuve, aucun
  port/handler write neuf, type surfacé read-only (D3), retour auto sur suppression concurrente en
  runtime/G3 (D2). Pas d'auth réelle du palier 16 tirée en avant.
- **Pas un trou métier (pas G1)** : décision de cadrage technique/process dérivable de la spec et
  des décisions déjà actées → tranchée par le CP. Aucune ambiguïté métier nouvelle.

**Sources.** `docs/sprints/14-impersonation-bornee/00-sprint14-suivi.md` (cadrage scaffolding,
table des 6 scénarios, routage runtime, cascade early-green) ; `docs/14-specification.md` (palier 8
tranche 2, règles 8/9/6/18/19/30, borne anti-cliquet) ; décisions D1/D2/D3 (ce fichier).
