# Calendrier navigable

> Sujet **migré** depuis `docs/15-specification.md` (palier 9, épics É4 + É7) à la migration complète
> des specs. Source de vérité pour la **navigation passé/futur**, les **vues prédéfinies** et la
> **sélection de plage de cases**. **NAVIGATION + VUES LIVRÉES s15** ; **SÉLECTION DE PLAGE (tranche 2)
> LIVRÉE s49 → palier 9 COMPLET.** Édité en diff, jamais réécrit en bloc.

## Contexte

**NAVIGATION + VUES LIVRÉES (s15).** Le hub `/planning` est un **agenda navigable** : déplacement
**passé/futur** (`PlanningPartage.razor` — boutons `nav-semaine-precedente` / `nav-semaine-suivante` /
`nav-aujourdhui`) et **vues prédéfinies** (semaine / 4 semaines glissantes / mois — `selecteur-vue`),
la fenêtre étant résolue par `GrilleAgendaQuery.Projeter(ancre, vue)` sur `SessionPlanning`
(**état de navigation NON persisté** — borne anti-cliquet).

**SÉLECTION DE PLAGE LIVRÉE (s49, tranche 2 — palier 9 COMPLET).** La grille gagne la **sélection d'une
plage de cases par DRAG** pour affecter une période sur l'**intervalle** choisi. **Réemploi STRICT** de la
dialog « Affecter une période » (écriture-en-contexte s06) : elle est **pré-remplie** avec l'intervalle
sélectionné, **aucune mécanique d'écriture / DTO / store neuf** ; le back multi-jours existe déjà (une
période EST un intervalle `[début..fin]`). La sélection est un **état d'interaction client VOLATILE**
(**aucune persistance** — borne anti-cliquet ; effacée au changement de vue / rechargement / Échap).
**Mécanique** : `pointerdown` pose l'**ancre**, `pointermove` résolu au niveau **DOCUMENT** (port
`IEcouteurMouvementPointeur`, `document.elementFromPoint` → `data-date` de la case survolée) met à jour la
surbrillance **`[min..max]`**, `pointerup` **document** (port `IEcouteurRelachementPointeur`) finalise **où
que le bouton soit lâché** et ouvre la dialog ; `user-select:none` / `touch-action:none` / `draggable=false`
neutralisent la sélection de texte native. **Contrat** : seuil **clic simple vs plage** (une case sans
déplacement = menu clic-case inchangé, PAS la dialog plage), **normalisation `[min..max]`** (drag sens
inverse J3→J1 → `début ≤ fin`), **bornage à la vue chargée** (drag débordant → aucune case hors-vue, aucune
navigation), **Échap** annule (port `IEcouteurEchapModal` s33, capture document), **Parent-gated** (Invité =
drag inerte). **Preuve** : @ihm menés RED→GREEN runtime + **projet E2E Playwright** `tests/PlanningDeGarde.Web.E2E`
(**HORS `.slnx`**, Chromium réel) figeant le geste sur l'app servie — bUnit reste **aveugle** au geste souris
natif / `elementFromPoint` (limite honnête, comme l'Échap document s33).

## Objectif & arbitrage

La tranche acteurs étant close (CRUD complet + impersonation bornée lecture livrés), l'**usage** tire ce
sujet **devant** les paliers techniques et la dette de test. Besoin **ancien** (retours s02 #3
navigation / s03), **rang +2** du backlog, tranché en **porte G2** par le PO. Détail :
[`objectif-et-arbitrage.md`](objectif-et-arbitrage.md).

## Séquence

**Palier 9 — COMPLET (navigation + vues s15 ; sélection de plage s49).** La tranche 1
(navigation passé/futur + vues semaine / 4 semaines glissantes / mois + état non persisté) est **livrée
au sprint 15**. La tranche 2 = **sélection de plage par DRAG** (multi-jours pour affecter une période sur
l'intervalle, réemploi s06) est **livrée au sprint 49**. **Restent hors palier 9** (backlog) : plage vide /
chevauchement riches, plage à cheval sur plusieurs vues / mois (navigation pendant le drag), sélection
persistée. Texte complet : [`sequence-de-livraison.md` § palier 9](sequence-de-livraison.md).

## Mécaniques (cibles)

- Le hub `/planning` devient un **calendrier navigable** : déplacement **passé/futur**, **vues
  prédéfinies** (semaine, mois, 4 semaines glissantes), **fenêtre par défaut** = 4 semaines glissantes
  à partir de la semaine en cours.
- **Sélection d'une plage de cases** pour définir une période sur l'intervalle : elle **ouvrira
  l'affectation d'une période** sur l'intervalle choisi (réemploi de l'écriture en contexte, cf.
  [`ecriture-en-contexte.md`](ecriture-en-contexte.md) et
  [`periodes-et-cycle-de-fond.md`](periodes-et-cycle-de-fond.md) §
  « Affecter une période »).

## Règles de gestion (catalogue : `regles-de-gestion.md`)

- **R14** (texte canonique : [`periodes-et-cycle-de-fond.md`](periodes-et-cycle-de-fond.md)) — la
  **sélection d'une plage de cases** pour affecter une période sur l'intervalle est une **capacité du
  palier 9**, **livrée s49** (drag → dialog « Affecter une période » s06 pré-remplie sur `[min..max]`).

## Risques

- **Acceptation runtime obligatoire** : le calendrier navigable se valide sur l'**app câblée** (un test
  de composant à doublures peut afficher une grille alors que le câblage réel échoue).
- **Aucune persistance neuve** ne doit être tirée (borne anti-cliquet).
- **Pilotage au catalogue** : retours produit VIDE au sprint 14 → **confirmer le besoin réel** au
  démarrage du sprint.
- **Dépendances en queue** : rétrofit déterministe SignalR (rang +3) et édition concurrente du même
  jour (rang +4) restent **derrière** ce sujet.

Cf. [`risques-et-questions-ouvertes.md`](risques-et-questions-ouvertes.md).
