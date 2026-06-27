# Retours sprint 11 — écriture en contexte (dialogs)

## Décisions autonomes (chef de projet)

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
