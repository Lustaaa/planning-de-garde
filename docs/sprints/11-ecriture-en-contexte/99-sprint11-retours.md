# Retours — Sprint 11 (écriture-en-contexte)

> **Fichier unifié.** Il porte deux choses, consommées par deux étapes différentes :
> - **Retours produit (PO)** ci-dessous → lus par `/4-retours` (challenge + besoins).
> - **Méthode (agents)** + **`## IA`** plus bas → lus par `retro-sprint` en fin de sprint.
>
> Section « Décisions autonomes (chef de projet) » préexistante (tranchée au `/2-make-gherkin`)
> conservée en bas. La partie produit est préparée vide ici et remplie par le PO après le gate
> visuel ; la partie méthode est appendée au fil de l'eau par le thread principal. Lancement de
> l'app : `pwsh .claude/skills/run/scripts/run.ps1`.

# Retours produit (PO)

> Le code et les tests unitaires sont **hors scope** ici (revus en revue de code).
> Ces retours portent sur l'**usage de l'IHM** : ce qui marche, ce qui coince, ce qui
> manque à l'écran. Remplis les puces, puis lance `/4-retours`.

## IHM - général

-

## IHM - /planning

> Surface unique du sprint : les dialogs « Poser un slot » / « Affecter une période » s'ouvrent
> depuis une case cliquée du planning (les routes dédiées `/planning/poser-slot` et
> `/planning/affecter-periode` sont retirées). Le lien/route « Définir un transfert » peut rester
> tant que le transfert est en tranche de secours.

-

## Tech (optionnel)

- (contraintes techniques éventuelles ; laisser vide si aucune → bypass dans `/4-retours`)

# Idée pour la suite

> Idées produit que le PO veut verser au backlog pour de futurs sprints (pas forcément le
> prochain). Consommées par `/4-retours` (classées/séquencées) puis replacées dans les épics
> du BACKLOG. Laisser vide si aucune.

-

# Consigne pour la suite

> Consignes directes du PO sur l'orientation à donner à la suite (priorité, cap, contrainte
> de séquencement). Pèsent sur le choix du prochain sujet en `/4-retours` (G2). Laisser vide
> si aucune.

-

# Méthode (agents) — pour retro-sprint

> Retours à la volée du PO sur la **méthode** (agents/skills/commands), appendés par le
> thread principal pendant le sprint. Ne PAS confondre avec les retours produit ci-dessus.

| Date | Cible (agent/skill/command) | Retour | Décision prise |
|------|-----------------------------|--------|----------------|

## IA

> Observations méthode relevées par l'IA (non demandées par le PO), candidates pour `retro-sprint`.

| Date | Cible (agent/skill/command) | Observation | Recommandation |
|------|-----------------------------|-------------|----------------|

## Notes de contexte (décisions produit, hors méthode)

-

# Décisions autonomes (chef de projet)

> Journal des décisions tranchées **seul** par le `chef-de-projet` pendant le sprint (sans
> déranger le PO). **Le PO le relit en rétro** pour piloter a posteriori. Détail prosé ci-dessous
> (tranché au `/2-make-gherkin`) ; appendé par `chef-de-projet`, lu par `retro-sprint`.

| Date | Question (agent dev) | Décision du CP | Fondement (spec/convention/principe) |
|------|----------------------|----------------|--------------------------------------|
| 2026-06 | Comportement de la dialog selon l'issue (succès / refus / chevauchement) ? | Option A — 3 issues distinctes (cf. détail) | spec v11 règles 16, 28, 14 |
| 2026-06 | Concurrence sous dialog & accès Invité : drivers vs caractérisations ? | Option B — 1 driver Invité, convergence en caractérisation, édition concurrente hors scope | spec v11 règles 9/11/14, Risques l.499 |
| 2026-06 | Autoriser l'écriture du fichier de scénarios (clôture make-gherkin) ? | AUTORISÉE — 7 scénarios fidèles à la spec | spec v11 palier 7, règles 9/14/16/17/28/30 |
| 2026-06-27 | Valider le plan d'implémentation (sortie tdd-analyse : 7 sc / 15 tests, routage 7/7 IHM, 0 backend) ? | VALIDÉ — enchaîner l'implémentation `ihm-builder` | palier 7 = saisie déplacée en contexte (couche Web seule), CQRS préservé, acceptation runtime exigée |
| 2026-06-27 | Sc.2 : même geste (clic case vide) doit router vers 2 dialogs (poser slot / affecter période) — quelle ergonomie ? | Option 1 — **menu d'actions au clic-case** ; adapter l'acceptation Sc.1 en 2 temps (assertion préservée) | palier 7 « agir là où on lit », extensibilité 3ᵉ dialog Transfert, règle 14, gating Invité mutualisé |
| 2026-06-27 | Sc.3 early-green INATTENDU (ancrage date case déjà acquis au fix Sc.1) : doublon / filet / à investiguer ? | **FILET À CONSERVER** — Option 1, acter caractérisation early-green, test non-vacuous gardé, Sc.3 ✅ GREEN, commiter | test discrimine 25/06≠15/06 (≠ Sc.1), règle 17 composée, anti-régression ; early-green expliqué (pas un trou) |
| 2026-06-27 | Sc.4 early-green (échec clair) doublon/filet ? + autoriser batch Sc.5/6/7 ? | **Sc.4 = FILET À CONSERVER** (caractérisation, commit) ; **batch Sc.5/6/7 AUTORISÉ sous garde-fous** (non-vacuité + Sc.7 vrai cycle RED→GREEN) | Sc.4 ≠ Sc.1 (issue échec, transport coupé + store vide) ; early-greens expliqués par design amont (dont gating Sc.6 = ma décision Sc.2) ; Sc.7 = habillage IHM, règle 16 acquis s01 |
| 2026-06-27 | Sc.7 : le fix (surfacer l'avertissement) déborde le scope « Web only » — exception de scope ou re-séquencer ? | **EXCEPTION DE SCOPE BORNÉE — Option 1** : le canal poser-slot renvoie l'avertissement ACQUIS dans son corps de succès ; bornes strictes, STOP si recalcul métier requis | slug Sc.7 prévoit « avertissement renvoyé par le retour de commande » ; règle 16/`AvertissementChevauchement` acquis s01 ; surfacer un acquis ≠ règle neuve ; pas de logique métier dans l'UI |
| 2026-06-28 | Phase IHM finale : faire le nettoyage scaffolding (a) ? engager ou clore la tranche de secours (b) ? | (a) **OUI, nettoyer** : retirer routes/pages/liens poser-slot + affecter-periode, **GARDER definir-transfert** ; (b) **CLORE à 7/7**, re-séquencer la secours au backlog (transfert + concurrence, jamais en bloc) | (a) ménage attendu du palier 7 (complète « écriture en contexte ») ; (b) goal atteint, ~2h consommées, secours = overflow CP-tranchable, palier 0 conservateur |
| 2026-06-28 | /4-retours G2 prochain sujet : séquencement de l'overflow et des flakes SignalR | **Décidé seul (séquencement)** : (ii) édition concurrente **DIFFÉRÉE** derrière la stabilisation des flakes SignalR ; flakes SignalR = **action MÉTHODE retro-sprint** (tests, pas src/), **pas** un goal produit. **Escaladé au PO (G2)** : 2 goals candidats (A = achever écriture en contexte/transfert ; B = CRUD acteurs + amorce impersonation) | dépendance cachée (ii)↔SignalR (driver flaky par construction) ; choix de cap sans douleur d'usage = porte PO |
| 2026-06-28 | /4-retours : autoriser l'écriture du backlog (G2 PO = Option A acté) ? | **AUTORISÉE** — P1 transfert (ferme É12) / P2 flakes SignalR (méthode) / P3 édition concurrente différée ; garde-fou conso DateContexte | priorisation **dérivable** du G2 acté + de mes décisions journalisées (séquencement, garde-fous) ; aucun nouveau point PO |
| 2026-06-28 | /5-consolidation : le palier 7 fond « écriture en contexte » (livrée) + « calendrier navigable » (non livré) — restructurer la Séquence ? | **SCINDER (Option 1)** : palier « Écriture en contexte (dialogs) » = ✅ LIVRÉ + reliquat P1 (3e dialog Transfert) ; palier distinct « Calendrier navigable » = ⬜ séquencé, re-numéroté ; numérotation spec↔backlog réalignée | restructuration déterministe = refléter l'état réel du code + le séquencement acté ; ne perdre aucun besoin (calendrier navigable + sélection de plage restent ⬜) ; pas un conflit de valeur (pas G1) |
| 2026-06-28 | /5-consolidation : autoriser l'écriture de la spec v12 (docs/12-specification.md) ? | **AUTORISÉE** — v12 remplace v11 figée ; split palier 7, règles révisées 9/11/14/16/17/28/30, aucune neuve/supprimée, garde-fous reportés | consolidation **dérivable** : reflète l'état livré + tous mes arbitrages journalisés (split, DateContexte, ajustement Sc.7, borne règle 30, P1/P2/P3) ; aucun besoin perdu ; pas de conflit de valeur (pas G1) |

## Détail des décisions (prose)

### Comportement de la dialog selon l'issue de la commande (Then observables)

**Décision** : Option A — trois issues distinctes, dérivées des règles 16 et 28 de `docs/11-specification.md`.

- **Succès** : écriture aboutie → la dialog se ferme, la case se met à jour à la date cliquée.
- **Refus domaine OU API injoignable (règle 28)** : la commande échoue clairement, saisie **non appliquée et conservée** → la dialog **reste ouverte**, message d'erreur **dans** la dialog, grille inchangée (rien à resoumettre n'est perdu).
- **Chevauchement / pose répétée (règle 16)** : le slot est **accepté** avec avertissement → écriture aboutie → la dialog **se ferme** et le slot réapparaît ; l'avertissement est affiché **à part** (toast/bandeau), **non bloquant**.

**Rationale** :
- Règle 16 qualifie le chevauchement de slot **accepté** (« ni refusé ni dédoublonné »), avertissement informatif. L'issue est donc un **succès d'écriture** : la dialog doit se fermer comme tout succès. L'option B (confirmation bloquante dans la dialog) et l'option C (dialog maintenue ouverte sur avertissement) **contredisent** « accepté » en traitant l'averti comme un quasi-échec.
- Règle 28 impose un **échec clair, saisie conservée à resoumettre** : refus domaine et API injoignable partagent le même observable (dialog ouverte, message, grille inchangée) — pas besoin de les distinguer en deux scénarios.
- L'option D abandonnerait une couverture observable pourtant **dérivable de la spec** ; la caractérisation s01 verte ne dispense pas de fixer le Then IHM du chevauchement (case réapparaît + avertissement non bloquant). À garder, en réutilisant l'acquis, sans re-spécifier la règle métier.

**Sources** : `docs/11-specification.md` règle 16 (l.440), règle 28 (l.474), règle 14 (grille lecture seule, l.436), palier 7 (l.236-242, 371-376).

### Concurrence sous dialog ouverte & accès Invité : drivers vs caractérisations

**Décision** : Option B — un seul **driver neuf** (droit Invité sur le nouveau déclencheur), la convergence temps réel sous dialog en **caractérisation annotée**, l'édition concurrente du même jour **hors scope**.

- **DRIVER (numéroté)** : *Invité ne peut pas ouvrir la dialog depuis une case* — le déclencheur d'écriture se **déplace** de l'écran dédié vers la case (palier 7) ; gater ce déclencheur en consultation seule est du **code IHM neuf** (rendu conditionnel du déclencheur). Réutilise le **contexte Invité/rôle existant** (acquis s01, archive `06-invite-edition-refusee.md` + règle 9) — **aucune** auth ni impersonation tirée devant l'usage (paliers 8/15 intacts). C'est l'application concrète de règle 14 (grille lecture seule) à la nouvelle surface « agir là où on lit ».
- **CARACTÉRISATION ANNOTÉE (hors numérotation des drivers)** : *la grille se rafraîchit sous une dialog ouverte sans la fermer ni perdre la saisie ; à la validation, dernière écriture gagne*. La **diffusion SignalR** est explicitement **acquise pour les dialogs** (Risques l.499 « à retenir comme acquis pour les dialogs en contexte ») et la **dernière-écriture-gagne** est règle 11 (acquise s10). On garde **un guard léger** prouvant que l'ouverture d'une dialog n'interfère pas avec le rafraîchissement de fond — sans re-spécifier la diffusion ni piloter une nouvelle règle.
- **HORS SCOPE ce sprint** : *édition concurrente du MÊME jour pendant dialog ouverte* (option D) — dépasse vraisemblablement ~2h, **tranche de secours séquençable** juste derrière (comme le transfert), **jamais reportée en bloc** (corollaire de découpe).

**Rationale** :
- Le **point d'application** du droit Invité **migre** avec le déclencheur (écran → case) : c'est un observable IHM neuf, donc un driver, mais **borné** — il réutilise l'acquis d'accès sans tirer la fondation auth/impersonation devant l'usage (palier 0 conservateur respecté).
- La convergence sous dialog **ne pilote aucune règle neuve** : la diffusion est acquise (l.499) et last-write-wins est règle 11. La traiter en driver gonflerait le scope sans valeur de design ; en caractérisation annotée, elle **garde le filet** sans re-spécifier.
- L'édition concurrente du même jour est un **cas limite** : le borner protège le cap ~2h, conformément au corollaire de découpe et à la leçon transfert/config foyer (couper au plus petit incrément, séquencer, jamais reporter en bloc).

**Sources** : `docs/11-specification.md` palier 7 (l.236-251, 371-378), règle 9 (l.424), règle 11 (l.430), règle 14 (l.436), Risques diffusion acquise pour dialogs (l.499) & transfert/débordement ~2h (l.483) ; archive s01 `06-invite-edition-refusee.md`.

### Autorisation d'écriture du fichier de scénarios (clôture make-gherkin)

**Décision** : **AUTORISÉE**. Les 7 scénarios (Sc1 poser slot @nominal, Sc2 affecter période @nominal, Sc3 pré-remplissage sur la date de la case @limite, Sc4 échec clair Outline refus domaine|API injoignable @erreur, Sc5 annulation sans écrire @limite, Sc6 Invité ne peut pas ouvrir la dialog @erreur, Sc7 chevauchement accepté+averti non bloquant @limite) **dérivent fidèlement de la spec actée** et des arbitrages déjà tranchés (ancrage case, comportement dialog selon l'issue, concurrence/Invité). Convergence temps réel et validation domaine en hors-numérotation (caractérisation annotée + couverte par Sc4). Tranche de secours = 3e dialog Transfert + édition concurrente même jour.

**Rationale** :
- **Aucune règle ni handler neuf** : palier 7 déplace la saisie en contexte (écran → case), réutilise commandes/canal HTTP/SignalR s04→s10, couche unique = Web (Blazor WASM). Conforme à l'observable « déplacement de la saisie en contexte, pas une règle neuve » (spec l.249-251, 371-378).
- **Mapping scénario→règle vérifié** : Sc1/Sc2 = palier 7 + règle 14 ; Sc3 = ancrage case (arbitrage CP, esprit règle 17 « date dans la fenêtre ») ; Sc4 = règles 28+14 (refus domaine ET API injoignable, même observable, un seul Outline — conforme à la décision « comportement dialog ») ; Sc5 = règle 14 (annulation n'écrit pas) ; Sc6 = règle 9 + driver Invité tranché ; Sc7 = règle 16 (caractérisation, acquis s01).
- **Bornes tenues** : aucune persistance tirée en avant (slots/périodes/transferts restent InMemory — borne anti-cliquet règle 30), grille lecture seule (règle 14), acceptation runtime sur câblage réel exigée (Risques l.488).
- **Note non bloquante** : écart de numérotation préexistant — la *Séquence de livraison* de la spec numérote les dialogs **palier 7** (l.236) tandis que la table *À faire* du backlog les place **palier 8** (l.226). La substance est identique et non ambiguë (prochain sujet = dialogs) ; à réaligner au `/5-consolidation`, ne bloque pas l'écriture.

**Sources** : `docs/11-specification.md` palier 7 (l.236-251, 371-378), règles 9/14/16/17/28/30, Mécaniques de base (l.388-389) ; `docs/BACKLOG.md` (l.40-46, 226) ; décisions journalisées supra (ancrage case, comportement dialog, concurrence/Invité).

### Validation du plan d'implémentation (sortie tdd-analyse → feu vert `ihm-builder`)

**Décision** : **VALIDÉ** — le plan (7 scénarios / 15 tests, routage 7/7 🖥️ IHM → `ihm-builder`, 0 backend, scaffolding 2 dialogs + clic-case + suppression routes/liens dédiés) est cohérent avec la spec, les conventions et DDD/CQRS. Enchaîner l'implémentation.

**Rationale** :
- **Routage backend/IHM correct** : palier 7 déplace la saisie en contexte (écran → case), couche unique = Web (Blazor WASM). Aucun handler ni règle neuve → 0 scénario backend justifié ; réutilise `PoserSlot`/`AffecterPeriode` + canal HTTP + diffusion SignalR (s04→s10). CQRS préservé (write par canal requête/réponse, read+diffusion par `GrilleAgendaQuery`+SignalR, jamais confondus, grille lecture seule règle 14).
- **Acceptation au bon niveau** : 7/7 pilotés par un test d'acceptation **runtime** sur app réellement câblée (front WASM + API distante + store réel + SignalR), rempart anti vert-qui-ment (réutilise `ApiDistanteFactory`/`ClientCanalEcriture` du s05) ; les bUnit composant restent des drivers de détail, jamais preuve seule. Sc.4 (API injoignable) exige transport réellement coupé + store distant resté vide.
- **Sc.3 = vrai driver de design** confirmé : paramètre `DateContexte` ancre la date de la case et **prime** sur le défaut `IDateTimeProvider` « aujourd'hui » (règle 17 composée, non révisée) ; le port d'horloge reste le défaut hors-contexte. Pré-remplissage par UNE case ≠ sélection de plage → hors scope, conforme.
- **Sc.7 = caractérisation** correctement annotée : règle 16 (chevauchement accepté+averti) déjà verte s01 ; test #1 (fermeture) **early-green attendu**, à conserver comme **filet** (pas un défaut, pas une porte) — le seul neuf drivé est l'habillage IHM non bloquant (test #2, avertissement à part après fermeture). Tranchage early-green : doublon assumé/filet conservé, pas de trou métier → **pas de G1**.
- **Bornes tenues** : anti-cliquet (slots/périodes InMemory, config foyer Mongo inchangée), aucune fondation auth/impersonation tirée (Sc.6 réutilise `SessionPlanning`/`EstParent` acquis s01, paliers 8/15 intacts), 3ᵉ dialog Transfert + édition concurrente même jour en tranche de secours séquençable.
- **Note non bloquante** (déjà relevée) : écart de numérotation palier 7 (spec) vs palier 8 (table backlog) — substance identique, à réaligner au `/5-consolidation`, ne bloque pas l'implémentation.

**Sources** : `docs/sprints/11-ecriture-en-contexte/00-sprint11-suivi.md` ; scénarios 01→07 ; `docs/11-specification.md` palier 7 + règles 9/14/16/17/28/30 ; décisions journalisées supra ; `CLAUDE.md` (Clean/DDD/CQRS, acceptation runtime obligatoire).

### Routage du type de saisie au clic-case (Sc.2 : poser slot vs affecter période)

**Décision** : **Option 1 — menu d'actions au clic-case**. Le clic sur une case (`data-testid="jour-case"`) ouvre un **petit menu contextuel** (`data-testid` dédié) à deux entrées — « Poser un slot » / « Affecter une période » — chacune (`data-testid` propre) ouvrant **sa** dialog, pré-remplie sur la date de la case. **Option 3 (heuristique selon l'état de la case) écartée** (fragile, contredit Sc.1 vert, mise en garde explicite de la design note Sc.2). **Option 2 (déclencheurs `+slot`/`+période` persistants dans chaque case) écartée** (encombre la grille et passe mal à l'échelle quand la 3ᵉ dialog Transfert arrive).

**Conséquence assumée sur Sc.1 (déjà vert)** : l'acceptation Sc.1 gagne **une étape d'interaction** (clic case → clic « Poser un slot » dans le menu → dialog). **L'assertion d'acceptation est strictement préservée** (slot « École » 08:30→16:30 réellement enregistré, relu par la projection, positionné dans la case du mardi 16/06/2026) : seul le **prélude de geste** évolue, pas l'observable. Le bUnit composant #1 de Sc.1 (« ouvrir la dialog au clic ») devient « ouvrir le menu au clic » + « l'entrée Poser un slot ouvre la dialog ». **Non-régression à maintenir verte** (suite complète).

**Rationale** :
- **Esprit palier 7 « agir là où on lit »** : les deux actions (poser slot, affecter période — et bientôt définir un transfert) sont contextuelles à la même date ; un menu au point de lecture est l'expression directe de l'écriture en contexte, geste unique → choix d'action.
- **Extensibilité (cap du sprint)** : la **3ᵉ dialog Transfert** (tranche de secours) s'ajoute comme une **entrée de menu** sans retoucher le câblage du clic-case ni recharger la case — là où l'option 2 imposerait un 3ᵉ bouton par cellule. Câblage uniforme, 1 déclencheur → N actions.
- **CQRS/DDD propre** : chaque entrée mappe **1:1** une commande existante (`PoserSlot`, `AffecterPeriode`) via le canal HTTP ; aucune règle ni handler neuf, aucun couplage entre les deux écritures (l'option 3 mélangeait l'état de lecture pour deviner l'écriture — anti-CQRS).
- **Règle 14 préservée** : le menu **n'écrit jamais**, il ne fait qu'ouvrir une dialog ; la grille reste lecture seule.
- **Gating Invité mutualisé (Sc.6)** : le déclencheur unique (case cliquable ouvrant le menu) est rendu conditionnel sur `Session.EstParent` — **un seul point à gater** au lieu de N boutons, ce qui simplifie et fiabilise la garde de la consultation seule.
- **Pas un trou métier (pas de G1)** : décision purement IHM/convention ; les deux écritures et leurs commandes existent déjà dans la spec actée, rien de métier n'est manquant. Adapter une acceptation verte intra-sprint sans toucher son assertion relève de l'évolution de scénario légitime, pas d'une porte PO.

**Sources** : `docs/sprints/11-ecriture-en-contexte/01-poser-slot-depuis-case.md` (acceptation verte + design note), `02-affecter-periode-depuis-case.md` (design note l.45-47 « remonter au CP si ambigu »), `06-invite-ne-peut-pas-ouvrir-dialog.md` (gating déclencheur) ; `docs/11-specification.md` palier 7 + règle 14 ; tranche de secours Transfert (`00-sprint11-suivi.md` l.59-61) ; `CLAUDE.md` (CQRS write/diffusion jamais confondus, grille lecture seule, non-régression suite complète).

### Sc.3 early-green inattendu : ancrage de la date de case (règle 17) acquis dès le fix Sc.1

**Décision** : **FILET À CONSERVER (Option 1)**. Acter Sc.3 comme **caractérisation early-green** : le test d'acceptation runtime non-vacuous (`FrontWasmPreRemplirDateCaseTests`) est **conservé** comme filet anti-régression de la règle 17 composée, Sc.3 passe **✅ GREEN** annoté « design acquis au Sc.1 », tags `@vert`, **commité** avec le reste. **Pas de cycle RED→GREEN factice** (option 2 écartée : artificiel et risque de casser Sc.1/Sc.2). **Filet non supprimé** (option 3 écartée).

**Pourquoi ce n'est PAS un doublon de Sc.1** :
- Sc.1 pose un slot sur la case du **16/06** et prouve seulement qu'il **réapparaît à la date cliquée** — il **ne dissocie pas** la date de la case de « aujourd'hui ». Un ancrage bugué sur l'horloge **passerait quand même** Sc.1 si l'horloge tombait sur la même date.
- Sc.3 est le **seul** test qui rend l'ancrage **contredisant** : horloge figée au **15/06**, case au **25/06**, et il asserte le slot **au 25/06 ET son absence au 15/06**. Il est **non-vacuous** et **discriminant** : il vire au rouge si la dialog s'ancre sur l'horloge. C'est précisément le **driver de design** identifié à la validation du plan — sa valeur de filet est **distincte et irremplaçable**.

**Pourquoi ce n'est PAS un vrai trou à investiguer** :
- L'early-green est **expliqué et légitime** : le fix minimal du Sc.1 a fait pré-remplir `PoserSlotDialog` **exclusivement** depuis `DateContexte`, ce qui satisfait l'ancrage règle 17 par construction. Aucun vert-qui-ment : le test transite jusqu'au store distant et discrimine 25/06≠15/06.
- L'early-green **attendu par essence** : à la validation du plan, Sc.3 était déjà flaggé « driver de design » ; qu'il atterrisse avec le Sc.1 confirme la cohérence du design, pas une anomalie. On est dans le cas « design acquis en amont », pas « câblage suspect ».

**Note non bloquante (à vérifier en revue / `/5-consolidation`)** :
- L'agent signale que le pré-remplissage se fait **« sans repli horloge »** (DateContexte exclusif). C'est **cohérent ce sprint** : palier 7 **retire** les routes dédiées `/planning/poser-slot` — **plus aucun point d'entrée hors-contexte**, donc `DateContexte` est **toujours** fourni et le repli horloge serait du **code mort**. La règle 17 composée (« le défaut horloge ne vaut que hors-contexte ») reste **respectée** : il n'y a simplement plus de chemin hors-contexte. **Garde-fou** : le **port `IDateTimeProvider` ne doit pas être supprimé de l'app** (la grille s'en sert pour « aujourd'hui »/fenêtre) ; si un point d'entrée hors-contexte réapparaît (futur palier), **réintroduire le repli horloge** dans la dialog. À tracer au backlog/consolidation, **ne bloque pas** le commit.

**Pas un trou métier → pas de G1** : pur arbitrage de filet de test (routage early-green dévolu au CP). Aucune règle métier manquante.

**Sources** : `docs/sprints/11-ecriture-en-contexte/03-pre-remplir-date-de-la-case.md` (driver de design, test non-vacuous 15/06≠25/06, règle 17 composée), `01-poser-slot-depuis-case.md` (acceptation Sc.1 verte au 16/06, non discriminante sur l'horloge) ; `tests/PlanningDeGarde.Web.Tests/FrontWasmPreRemplirDateCaseTests.cs` ; `docs/11-specification.md` palier 7 + règle 17 ; décision « validation du plan » supra (Sc.3 = driver de design, Sc.7 caractérisation) ; `CLAUDE.md` (acceptation runtime, anti vert-qui-ment).

### Sc.4 early-green (échec clair, règle 28) + autorisation de batch Sc.5/6/7

**Décision** :
1. **Sc.4 = FILET À CONSERVER** (caractérisation early-green). Acter Sc.4 ✅ GREEN, **commiter** les 2 tests runtime non-vacuous (`FrontWasmEchecDialogResteOuverteTests`). **Pas un doublon de Sc.1** : Sc.1 prouve le **succès** ; Sc.4 prouve l'**issue échec** (dialog reste ouverte, message dans la dialog, saisie conservée, **grille inchangée**) et surtout, sur le cas **API injoignable**, que le **transport réellement coupé** laisse le **store distant vide** — observable distinct et anti-vert-qui-ment. Option 3 (RED artificiel) écartée : casserait `FrontWasmApiInjoignableTests` et contredit le single-observable que j'ai décidé (présence du message, pas son texte exact).
2. **Batch Sc.5/6/7 AUTORISÉ, sous garde-fous** (lever de vélocité borné) :
   - **Sc.5 (annuler)** et **Sc.6 (Invité)** : actés en **caractérisation** **uniquement si** leur test est **non-vacuous et discriminant sur câblage réel**. Sc.6 **doit** porter un **contrôle positif** (un **Parent** ouvre bien le menu/dialog) **en regard** du négatif (l'**Invité** n'ouvre rien) — sinon un test qui ne vérifie que « rien ne s'ouvre » passerait **vacuously** si le clic était cassé pour tous. Sc.5 doit prouver **aucune écriture émise + grille inchangée** (rouge si annuler écrivait/relisait).
   - **Sc.7 (chevauchement)** : **vrai cycle RED→GREEN** attendu — l'habillage **bandeau/toast d'avertissement à part, non bloquant** est l'IHM neuve à driver (le test #1 « fermeture » reste early-green/filet, le test #2 « avertissement à part » est le driver réel). **Aucun handler/règle backend** : règle 16 (accepté + averti) **acquise s01**.
   - **Retour final unique** accepté **sous ces conditions**.
3. **STOP + escalade CP immédiate si** : un early-green s'avère **vacuous** ou **vert pour une mauvaise raison**, **révèle un trou** de couverture, **ou** le RED de Sc.7 implique une **règle backend manquante** (ne devrait pas — règle 16 acquise).

**Rationale** :
- **Tous les early-greens aval sont expliqués par le design amont**, pas des anomalies : l'aiguillage d'issue (succès ferme/relit vs échec garde ouvert) a atterri en bloc au fix Sc.1/Sc.2 ; le `OnAnnule`/`FermerDialog` est de la plomberie de dialog de base ; et surtout **le gating Invité de Sc.6 est la réalisation directe de ma décision Sc.2** (« menu au clic-case, **gating Invité mutualisé**, un seul déclencheur à gater sur `EstParent` »). L'early-green de Sc.6 n'est donc **pas** vraiment inattendu : il **découle** d'un arbitrage CP déjà tranché — ce qui **renforce** son acte en caractérisation plutôt qu'en suspicion.
- **Le cadre « lot borné aux caractérisations anticipées » est respecté** : Sc.7 **rompt le lot** (vrai driver) et reste donc en **cycle TDD normal**, hors caractérisation ; le batch ne couvre que des early-greens **anticipables** (issues déjà câblées en amont), à condition de **non-vacuité** vérifiée pièce par pièce.
- **Conservatisme palier 0 préservé** : la seule zone sensible (accès Invité, sécurité-adjacent) est verrouillée par l'exigence de **contrôle positif** ; aucune fondation auth/impersonation tirée (réutilise `Session.EstParent`, acquis s01).
- **Pas de trou métier → pas de G1** : routage d'early-greens et habillage IHM ; aucune règle métier manquante.

**Sources** : `docs/sprints/11-ecriture-en-contexte/04-echec-clair-dialog-reste-ouverte.md` (issue échec, single-observable règle 28, API injoignable sur câblage réel), `05-annuler-dialog-sans-ecrire.md`, `06-invite-ne-peut-pas-ouvrir-dialog.md` (driver Invité, gating déclencheur), `07-chevauchement-accepte-averti.md` (règle 16 acquise s01, habillage avertissement non bloquant = seul neuf) ; `tests/PlanningDeGarde.Web.Tests/FrontWasmEchecDialogResteOuverteTests.cs`, `FrontWasmApiInjoignableTests` ; décisions supra (Sc.2 gating mutualisé, comportement dialog par issue, Sc.3 early-green) ; `CLAUDE.md` (acceptation runtime, anti vert-qui-ment, non-régression suite complète).

### Sc.7 : exception de scope pour surfacer l'avertissement de chevauchement

**Décision** : **EXCEPTION DE SCOPE BORNÉE — Option 1**. Le canal `POST /api/canal/poser-slot` cesse de renvoyer `Results.Ok()` nu : il renvoie, **dans son corps de succès**, l'**avertissement de chevauchement déjà acquis** (règle 16 / `AvertissementChevauchement` côté Application, vert s01). `PoserSlotDialog` remonte cet avertissement à `PlanningPartage` qui affiche le **bandeau à part, non bloquant, après fermeture** (issue succès). **Options 2 et 3 écartées.**

**Bornes strictes de l'exception** (à respecter sous peine de STOP + escalade) :
- **Aucune règle ni handler neuf, aucune logique métier dans l'UI**. On **surface un acquis**, on ne le recalcule pas. L'UI **ne déduit jamais** le chevauchement depuis la grille relue (anti-règle respectée).
- **Préférence de câblage** : surfacer l'`AvertissementChevauchement` **porté par le résultat de la commande** (le plus propre — zéro nouveau couplage : la réponse du canal requête/réponse transporte l'**outcome de sa propre écriture**, ce n'est ni de la diffusion SignalR ni une query déguisée). **Si** ce résultat ne le porte pas déjà, l'endpoint Api peut, **à titre de plomberie d'adaptateur**, consulter le **read model EXISTANT** (`JourneeEnfantQuery.Chevauchements`) post-écriture — **sans** créer de règle. **MAIS si surfacer l'avertissement exigeait un recalcul métier neuf, un nouveau handler, ou une modif de `GrilleAgendaQuery`/Domain : STOP + escalade CP** (le périmètre changerait de nature).
- **Surface autorisée** : le **DTO/contrat de réponse** du canal poser-slot (Api/Application) + le front (dialog → PlanningPartage). **Interdit** : `GrilleAgendaQuery`, nouveau chemin d'écriture, nouvel endpoint d'écriture, Domain.
- **Acceptation runtime non-vacuous** : chevauchement → bandeau présent **après** fermeture, slots conservés (ni refusé ni dédoublonné) ; **sans** chevauchement → **pas** de bandeau.

**Rationale** :
- **Ce n'est pas un trou métier (pas de G1)** : la règle 16 « accepté + averti » et son `AvertissementChevauchement` sont **verts s01**. Le manque est purement un **contrat de transport** : le canal jette un outcome déjà produit. Le réparer = **surfacer un acquis**, pas spécifier du métier.
- **Le slug Sc.7 prévoyait déjà ce câblage** : « afficher l'avertissement de chevauchement **renvoyé par le retour de commande** ». La déclaration de scaffolding « couche unique = Web » était **exacte pour Sc.1→Sc.6** mais **trop serrée pour Sc.7**, dont l'acceptation a **toujours** supposé une fine plomberie de canal. L'exception **réconcilie** le scaffolding avec le slug, elle ne le contredit pas.
- **CQRS préservé** : l'avertissement est un **attribut de l'outcome de la commande** (réponse du canal requête/réponse), distinct de la **diffusion** (SignalR) et de la **lecture de grille** (`GrilleAgendaQuery`). Le « write » reste le write ; on ne confond pas les canaux. L'option 2 (endpoint MapGet + 2ᵉ aller-retour) est **moins** propre (traite un outcome d'écriture comme une query permanente, risque de course sur « cette » écriture) et ajoute de la surface. L'option 3 (déférer le bandeau) abandonnerait un livrable quasi terminé pour un manque **trivialement** comblable, contre le principe « jamais reporter en bloc ».
- **Conservatisme palier 0** : exception **minimale et bornée**, garde-fou STOP si le fix s'avérait plus large que « surfacer un acquis ».

**Note `/5-consolidation`** : corriger la formule de scaffolding « couche unique = Web » → « **Web + contrat de réponse du canal poser-slot** (surfaçage de l'avertissement acquis), reste inchangé ». Tracer comme ajustement de cadrage, non comme dette.

**Sources** : `docs/sprints/11-ecriture-en-contexte/07-chevauchement-accepte-averti.md` (slug « avertissement renvoyé par le retour de commande », règle 16/`AvertissementChevauchement` acquis s01), `00-sprint11-suivi.md` (déclaration « couche unique = Web », tranche secours) ; `docs/11-specification.md` règle 16 ; décision « comportement dialog par issue » supra (chevauchement = succès, bandeau à part non bloquant) ; `CLAUDE.md` (CQRS write/diffusion/lecture jamais confondus, données derrière des ports, acceptation runtime).

### Phase IHM finale : périmètre (nettoyage scaffolding + sort de la tranche de secours)

**Décision** :
1. **(a) Nettoyage scaffolding — À FAIRE dans cette phase IHM finale.** Retirer les **routes/pages dédiées** `/planning/poser-slot` et `/planning/affecter-periode` (`PoserSlot.razor` / `AffecterPeriode.razor` + code-behind) **et leurs liens de barre**. **GARDER** `/planning/definir-transfert` (route + page + lien) **tel quel** — la 3ᵉ dialog transfert est déférée (cf. point 2), retirer cette page **régresserait** l'accès au transfert. **Garde-fous** : suite **complète verte** maintenue (Docker actif) ; mettre à jour toute nav/test référençant les routes retirées ; s'appuyer sur l'acceptation runtime **Sc.1/Sc.2 verte** qui prouve que le **menu clic-case couvre intégralement** les écrans supprimés (aucune perte d'accès à la pose/affectation).
2. **(b) Tranche de secours — CLORE le sprint à 7/7, re-séquencer au backlog.** Ne **pas** engager la 3ᵉ dialog « Définir un transfert » ni l'édition concurrente dans ce sprint. Les **re-séquencer en increments discrets** (jamais en bloc) : (i) **3ᵉ dialog Transfert depuis une case** — increment qui, une fois livré, **retirera** alors la page `definir-transfert` ; (ii) **édition concurrente du même jour sous dialog ouverte** — cas limite candidat séquençable.

**Rationale** :
- **(a) est du ménage attendu du palier 7, pas du scope creep** : le scaffolding déclarait explicitement la suppression de ces routes/pages/liens (« l'écriture est désormais en contexte via le menu clic-case »). Laisser les écrans dédiés **maintiendrait deux chemins d'écriture concurrents** — contraire à « déplacer la saisie là où on lit ». Le nettoyage **achève** le sprint goal, il ne l'étend pas. La page transfert reste **précisément parce que** (b) est déférée — cohérence (a)↔(b).
- **(b) : le sprint goal est ATTEINT** (7/7 scénarios numérotés verts, suite 192/192). La tranche de secours était **dès le cadrage** de l'**overflow hors-numérotation**, livrable « **si le scope ~2h tient, sinon séquencée juste derrière, jamais reportée en bloc** ». Le budget ~2h a été consommé par les 7 scénarios ; en **palier 0 conservateur**, on ne tire pas un increment supplémentaire (nouvelle surface transfert + concurrence) devant l'usage. La re-séquence respecte le corollaire de découpe (couper au plus petit incrément, séquencer, jamais reporter en bloc).
- **Pas une porte PO (pas de G2)** : le sprint goal n'est pas modifié — il est **rempli**. Le sort de l'overflow est **explicitement dévolu au CP** (« décision CP requise pour l'engager ou clore »). Aucun trou métier (pas de G1) : le transfert reste accessible via sa page existante jusqu'à l'increment dédié.

**À tracer en `/4-retours` / backlog** : (i) dialog Transfert depuis case (+ retrait page `definir-transfert` à ce moment-là), (ii) édition concurrente sous dialog ouverte. À reverser dans les épics du BACKLOG comme prochains candidats séquencés.

**Sources** : `docs/sprints/11-ecriture-en-contexte/00-sprint11-suivi.md` (scaffolding : suppression routes poser-slot/affecter-periode, lien transfert conservé l.16-18 / l.98-101 ; tranche de secours overflow l.59-65) ; `01-poser-slot-depuis-case.md` & `02-affecter-periode-depuis-case.md` (acceptation runtime verte = couverture du menu clic-case) ; décisions supra (concurrence/Invité : édition concurrente hors scope, secours séquençable) ; `docs/11-specification.md` palier 7 ; `CLAUDE.md` (non-régression suite complète, jamais deux chemins d'écriture, palier 0 conservateur).

### /4-retours — choix du prochain sujet (G2) : 2 goals candidats escaladés au PO + règle de séquencement (décidée seul)

**Part décidée seul (séquencement — relève du CP)** :
- **(ii) édition concurrente du même jour sous dialog ouverte = DIFFÉRÉE.** Dépendance cachée **(ii)↔flakes SignalR** : driver une fondation temps-réel **instable** produirait des scénarios **flaky par construction**. À ne séquencer **qu'après** stabilisation. **Pas** candidate G2 maintenant.
- **Flakes temps-réel SignalR = action de MÉTHODE pour `retro-sprint`**, pas un goal produit : ils **vivent dans les tests, pas dans `src/`** (aucun bug produit observable). À traiter en rétro (stabilisation), ce qui **déverrouille** (ii) ensuite.

**Part escaladée au PO (G2 — choix de cap, aucune douleur d'usage à dériver)** : 2 goals candidats ~2h, tirés du BACKLOG existant (pas de re-brainstorm), 3ᵉ injectable.
- **Goal A — Achever l'écriture en contexte : 3ᵉ dialog « Définir un transfert » depuis une case.** Ouvre le transfert via le **menu clic-case** (pattern Sc.1/Sc.2 **prouvé**), réutilise la commande `DefinirTransfert` + canal HTTP + SignalR **existants** ; **retire la dernière page de saisie dédiée** `/planning/definir-transfert` (+ lien). Tranche verticale runtime. **Ferme l'épic É12**, supprime l'incohérence résiduelle « une seule page dédiée restante ». **Risque faible.** (BACKLOG +1 reliquat / É12 / `00-suivi` tranche secours.)
- **Goal B — CRUD acteurs complet + amorce impersonation** (BACKLOG +2, É2/É10). **Suppression d'acteur** avec cadrage des **cases orphelines** (règle 6) + **amorce d'impersonation** (l'utilisateur principal incarne un acteur, convenance admin) réutilisant le **contexte rôle `SessionPlanning`** (acquis s01/s06) — **PAS** l'auth réelle (palier 12 intact). Tranche verticale : config foyer → supprimer un acteur → grille/légende relues (orphelins cadrés) + bascule d'incarnation visible. **Risque modéré** (sémantique suppression + orphelins). Avance l'appropriation des acteurs et **amorce** le chemin auth.

**Recommandation CP** : léger penchant pour **A** — en **palier 0 conservateur** et **sans douleur d'usage exprimée** (retours produit PO vides), **clore proprement l'épic écriture-en-contexte** (plus aucune page de saisie dédiée) avant d'ouvrir un nouvel épic est le mouvement le plus sûr (faible risque, pattern éprouvé). **B** est le bon choix si le PO veut **avancer la surface produit** (acteurs) plutôt que finir le ménage. **3ᵉ injectable** suggéré : palier 7 backlog « **Survol → résumé de la journée** » (⬜, É5/É9), **cycle de fond riche** (+3, É7), ou **calendrier navigable** (palier 8).

**Conséquences** :
- **A** : épic É12 fermé, cohérence d'écriture-en-contexte complète ; mais reste « à l'aveugle d'usage », n'ouvre pas de valeur d'usage majeure neuve.
- **B** : avance acteurs + amorce auth (palier 12) ; mais touche la sémantique des **orphelins** (règle 6, risque modéré) et **laisse la page transfert dédiée orpheline** (incohérence résiduelle É12) jusqu'à reprise.
- **Différer (ii) + flakes en rétro** : évite des scénarios flaky par construction ; la dette temps-réel est purgée d'abord (méthode), déverrouillant (ii) ensuite.

**Sources** : `docs/BACKLOG.md` (Prochains sprints +1/+2/+3 l.44-48 ; À faire paliers 7-8 l.225-226 ; É2/É10/É12 ; dette « suppression de période » l.272 / « cycle de fond riche » l.279) ; `docs/11-specification.md` (règle 6 acteurs/orphelins, palier 7) ; décisions supra (sort de la tranche de secours, gating Invité réutilisant `SessionPlanning`) ; alerte méthode flakes SignalR (ihm-builder) ; `CLAUDE.md` / `README-claude.md` (portes PO G2, goals ~2h tirés du backlog, palier 0 conservateur).

### /5-consolidation (spec v11 → v12) : scinder le palier 7 « calendrier navigable & écriture en contexte »

**Décision** : **SCINDER EN DEUX (Option 1)**. Restructuration **documentaire déterministe** : refléter l'**état réel du code** + le **séquencement déjà acté**, sans rien perdre.
- **Palier « Écriture en contexte (dialogs) » = ✅ LIVRÉ ce sprint** (2 dialogs Poser slot / Affecter période, **menu clic-case**, écrans dédiés slot/période retirés, avertissement de chevauchement surfacé, gating Invité mutualisé). **Reliquat = PROCHAIN SUJET (P1, G2 acté Option A)** : **3ᵉ dialog « Définir un transfert » en contexte + retrait `definir-transfert`** → **referme l'épic É12**.
- **Palier distinct « Calendrier navigable » = ⬜ NON LIVRÉ, séquencé, re-numéroté** : navigation passé/futur, vues prédéfinies (semaine / mois / 4-sem), **+ sélection de plage de cases** (pour définir une période). Vérifié dans le code : `PlanningPartage.razor` = grille 5×7 **statique**, **aucune** navigation/vue/plage → c'est bien une **cible**, pas un acquis.
- **Numérotation spec↔backlog RÉALIGNÉE** : résorber l'écart « palier 7 (spec) vs palier 8 (table backlog) » relevé dès `/2-make-gherkin`. La table *À faire* du BACKLOG et la *Séquence de livraison* de la spec doivent porter la **même** numérotation après split.

**Options 2 et 3 écartées** :
- **Option 2** (un seul palier « partiellement livré ») : **trompeuse** — fond deux capacités sans lien (saisie en contexte vs navigation calendrier) et masque ce qui est réellement acquis ; viole « refléter l'état courant réel ».
- **Option 3** (calendrier navigable rétrogradé en simple question ouverte/Risques) : **perte de traçabilité** d'un besoin **réel** de l'épic É4 (calendrier navigable + sélection de plage) ; un besoin séquencé ne doit pas être dégradé en note.

**Garde-fous d'écriture** (déterministes, à appliquer par `spec-consolidation`) :
- **Ne perdre aucun besoin** : « calendrier navigable », « vues prédéfinies », « **sélection de plage de cases pour définir une période** » restent **⬜ séquencés** (ne pas les fondre dans le palier livré).
- **Mécaniques nuancées** : ce qui décrit le calendrier navigable passe au **futur/cible** ; ce qui décrit l'écriture en contexte passe à l'**acquis/présent** (dialogs, menu clic-case, issue par dialog, ancrage `DateContexte`).
- **Reporter le garde-fou DateContexte** (déjà journalisé) : pré-remplissage **DateContexte-exclusif** (repli horloge = **code mort** tant qu'aucun point d'entrée hors-contexte) — **ne pas supprimer le port `IDateTimeProvider`** (la grille s'en sert) ; **réintroduire le repli** si un point d'entrée hors-contexte réapparaît.
- **Reporter l'ajustement de cadrage Sc.7** : « couche unique = Web » → « **Web + contrat de réponse du canal poser-slot** » (surfaçage de l'avertissement acquis).
- **Borne anti-cliquet préservée** (règle 30) : transfert/slots/périodes restent **InMemory** ; la persistance du reste du domaine reste en queue.

**Rationale** :
- **Résolution déterministe, pas un choix de valeur (pas de G1)** : il s'agit d'**aligner la doc sur la réalité** (code constaté + G2 déjà tranché par le PO). Aucun arbitrage métier ni de cap n'est rouvert — le PO a déjà choisi le prochain sujet (transfert) ; le split **acte** cette réalité, il ne la décide pas.
- **Fidélité + traçabilité** : l'option 1 est la seule qui dise **vrai** (écriture-en-contexte livrée, calendrier navigable à faire) **et** conserve le besoin calendrier comme palier à part entière, prêt à être priorisé après l'épic écriture-en-contexte.

**Sources** : `docs/11-specification.md` (palier 7 « calendrier navigable & écriture en contexte », Séquence de livraison) ; `docs/BACKLOG.md` (À faire paliers 7-8 l.225-226 : « Survol → résumé » vs « Calendrier navigable + écriture en contexte + sélection de plage » ; É4 calendrier, É12 écriture-en-contexte) ; `src/.../PlanningPartage.razor` (grille 5×7 statique, constat agent) ; décisions supra (G2 Option A acté, garde-fou DateContexte, ajustement cadrage Sc.7, borne anti-cliquet règle 30) ; note de numérotation `/2-make-gherkin` (décision « autorisation d'écriture des scénarios », écart palier 7/8).
