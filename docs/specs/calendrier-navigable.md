# Calendrier navigable

> Sujet **migré** depuis `docs/15-specification.md` (palier 9, épics É4 + É7) à la migration complète
> des specs. Source de vérité pour la **navigation passé/futur**, les **vues prédéfinies** et la
> **sélection de plage de cases**. **Prochain sujet — NON LIVRÉ.** Édité en diff, jamais réécrit en
> bloc.

## Contexte

Le **calendrier navigable** reste **à livrer** : la grille actuelle est une **vue posée non encore
navigable**. Le but est de faire du hub `/planning` un **agenda navigable** : se déplacer dans le
**passé et le futur**, choisir des **vues prédéfinies** (semaine, mois, **4 semaines glissantes** par
défaut à partir de la semaine en cours), et **sélectionner une plage de cases** pour affecter une
période sur l'**intervalle** choisi (l'affectation par plage rouvre l'écriture en contexte sur
plusieurs jours d'un coup). Ce palier enrichit la grille **sans toucher aux mécaniques d'écriture déjà
livrées** (dialogs). **Aucune persistance tirée en avant.**

## Objectif & arbitrage

La tranche acteurs étant close (CRUD complet + impersonation bornée lecture livrés), l'**usage** tire ce
sujet **devant** les paliers techniques et la dette de test. Besoin **ancien** (retours s02 #3
navigation / s03), **rang +2** du backlog, tranché en **porte G2** par le PO. Détail :
[`objectif-et-arbitrage.md`](objectif-et-arbitrage.md).

## Séquence

**Palier 9 — NON LIVRÉ, prochain sujet.** **Orientation de découpe (pas une règle, palier gardé
groupé)** : (a) plus petit incrément probable = **navigation seule** (semaines préc. / suiv., bascule
de vue) ; (b) **sélection de plage** = sujet plein **cuttable en tranche 2** si elle déborde ~2h ; (c)
**périmètre exact tranché au make-gherkin**, non pré-arbitré ici (corollaire de découpe). Pas de découpe
9a/9b actée en spec. Sujet `/2-make-gherkin` = `calendrier-navigable`. Texte complet :
[`sequence-de-livraison.md` § palier 9](sequence-de-livraison.md).

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
