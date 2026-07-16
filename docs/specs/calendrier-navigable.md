# Calendrier navigable

> Sujet **migré** depuis `docs/15-specification.md` (palier 9, épics É4 + É7) à la migration complète
> des specs. Source de vérité pour la **navigation passé/futur**, les **vues prédéfinies** et la
> **sélection de plage de cases**. **NAVIGATION + VUES LIVRÉES s15** ; **seule la sélection de plage
> reste non livrée (tranche 2).** Édité en diff, jamais réécrit en bloc.

## Contexte

**NAVIGATION + VUES LIVRÉES (s15).** Le hub `/planning` est un **agenda navigable** : déplacement
**passé/futur** (`PlanningPartage.razor` — boutons `nav-semaine-precedente` / `nav-semaine-suivante` /
`nav-aujourdhui`) et **vues prédéfinies** (semaine / 4 semaines glissantes / mois — `selecteur-vue`),
la fenêtre étant résolue par `GrilleAgendaQuery.Projeter(ancre, vue)` sur `SessionPlanning`
(**état de navigation NON persisté** — borne anti-cliquet). **Reste NON LIVRÉ = la SÉLECTION DE PLAGE
de cases** (tranche 2) pour affecter une période sur l'**intervalle** choisi (l'affectation par plage
rouvre l'écriture en contexte sur plusieurs jours d'un coup) — enrichit la grille **sans toucher aux
mécaniques d'écriture déjà livrées** (dialogs), **aucune persistance tirée en avant**.

## Objectif & arbitrage

La tranche acteurs étant close (CRUD complet + impersonation bornée lecture livrés), l'**usage** tire ce
sujet **devant** les paliers techniques et la dette de test. Besoin **ancien** (retours s02 #3
navigation / s03), **rang +2** du backlog, tranché en **porte G2** par le PO. Détail :
[`objectif-et-arbitrage.md`](objectif-et-arbitrage.md).

## Séquence

**Palier 9 — NAVIGATION + VUES LIVRÉES s15 ; SÉLECTION DE PLAGE = tranche 2 restante.** La tranche 1
(navigation passé/futur + vues semaine / 4 semaines glissantes / mois + état non persisté) est **livrée
au sprint 15**. La tranche 2 = **sélection de plage** (drag / sélection multi-jours pour affecter une
période sur l'intervalle) **reste non livrée** (séquencée s49). **Périmètre exact tranché au
make-gherkin** (corollaire de découpe). Sujet `/2-make-gherkin` = `calendrier-navigable`. Texte
complet : [`sequence-de-livraison.md` § palier 9](sequence-de-livraison.md).

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
  palier 9**, non encore livrée.

## Risques

- **Acceptation runtime obligatoire** : le calendrier navigable se valide sur l'**app câblée** (un test
  de composant à doublures peut afficher une grille alors que le câblage réel échoue).
- **Aucune persistance neuve** ne doit être tirée (borne anti-cliquet).
- **Pilotage au catalogue** : retours produit VIDE au sprint 14 → **confirmer le besoin réel** au
  démarrage du sprint.
- **Dépendances en queue** : rétrofit déterministe SignalR (rang +3) et édition concurrente du même
  jour (rang +4) restent **derrière** ce sujet.

Cf. [`risques-et-questions-ouvertes.md`](risques-et-questions-ouvertes.md).
