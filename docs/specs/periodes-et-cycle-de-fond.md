# Périodes de garde & cycle de fond

> Sujet **découpé** depuis `docs/15-specification.md` (règles 11/12/14/15) à la clôture s16.
> Source de vérité pour la **résolution du responsable d'une case** (fond ↔ surcharge) et le
> **cycle de vie d'une période** (affecter / supprimer). Édité en diff, jamais réécrit en bloc.

## Contexte

Le planning se lit case par case. Chaque case résout **un seul responsable** par **priorité
descendante** : une **surcharge** (période explicitement saisie) prime sur le **fond** (cycle
récurrent), qui prime sur le **neutre** (rien à afficher → teinte neutre, sans nom). La grille
est en **lecture seule** ; toute écriture (affecter, supprimer) passe par une **dialog ouverte
depuis une case**. Le store des périodes et du cycle est **durable (Mongo, s15)**.

## Objectif & arbitrage

Donner un cycle récurrent qui couvre le quotidien **sans saisie**, surchargeable au cas par cas,
et permettre de **défaire** une surcharge depuis l'IHM. Arbitrages actés :

- **Ancrage ISO sans ancre** : l'index de semaine = parité ISO (`semaine ISO % N`). Choisir un
  début/ancre explicite **rouvre cette décision** → palier « cycle de fond riche » (tranché à
  son make-gherkin, pas avant).
- **Suppression idempotente** : supprimer une période absente / déjà supprimée = **no-op qui
  réussit** (jamais un refus). Clé = **identifiant stable**, jamais un libellé.
- **Édition de période** (re-borner / réaffecter) = **tranche suivante** (hors s16, qui n'a
  livré que le Delete).

## Séquence (résolution d'une case)

1. Une **surcharge** couvre la date → la case affiche son responsable et sa couleur.
2. Sinon, le **cycle de fond** résout l'index (`semaine ISO % N`) → responsable mappé sur cet
   index, s'il existe.
3. Sinon (index non mappé, ou acteur du fond supprimé) → **neutre** : teinte neutre, **aucun
   nom** (pas de nom fantôme).

Supprimer une surcharge fait **re-jouer cette séquence** : la case retombe sur le fond si le
cycle le résout, sinon sur le neutre.

## Mécaniques

- **Affecter une période** — dialog « Affecter une période » (palier 7) ou **sélection de plage
  de cases** (palier 9) ; écrit par le canal requête/réponse, réapparaît dans la grille.
- **Lister les périodes d'une date** — lecture (`PeriodesDuJourQuery`) renvoyant les périodes
  **couvrant** la date avec **identifiant stable, bornes, responsable** ; alimente la dialog de
  suppression ; ne déclenche **jamais** la diffusion.
- **Supprimer une période** — 4ᵉ usage du **menu clic-case** → dialog listant les périodes de la
  date → bouton supprimer par ligne → commande `POST /api/canal/supprimer-periode` (idempotente) ;
  sur succès, **accusé « Période supprimée » à part** (non bloquant) et **diffusion temps réel**
  (case + légende re-résolues sans rechargement). Échec API → la dialog **reste ouverte**, message
  clair, **rien appliqué** ; annulation → **aucune commande émise**. **Gating Invité** : entrée
  absente, aucune commande émissible.

## Règles de gestion

> Numérotation conservée depuis le monolithe v15 pour la traçabilité.

- **R11 — Cycle de fond récurrent, éditable.** Cycle de **N semaines** (N ≥ 1) ; `index =
  semaine ISO du jour % N`, chaque index mappé sur un responsable de fond résolu sur
  l'**identifiant stable** (jamais le libellé ; index non mappé → neutre). Définissable/éditable
  depuis la config foyer (nombre de semaines + responsable par index, alimenté par les acteurs
  persistés), non figé dans le code. **Zéro semaine refusé** (« le cycle doit compter au moins
  une semaine »), cycle précédent inchangé. Ré-édition → grille à jour **sans rechargement** ;
  édition concurrente → **dernière écriture gagne**. Une dialog d'écriture ouverte **n'interfère
  pas** avec le rafraîchissement de fond. *Suppression d'un acteur mappé → index non mappé →
  neutre, sans nom fantôme (R6). Ancre/début explicite, frontière de jour, plages, sur-cycles,
  WE-only = palier « cycle de fond riche » (rouvre l'ancrage ISO).*
- **R12 — Exception ponctuelle prime sur le fond.** Une **période saisie prime** sur le fond
  (surcharge > fond > neutre) ; le cycle **reprend ensuite** autour de la surcharge. *Une
  surcharge **orpheline** (acteur supprimé, R6) cesse de primer → case retombe sur fond ou
  neutre (R15).*
- **R14 — Grille en lecture seule, écriture en dialog contextuelle.** La grille consomme slots,
  périodes et **fond résolu** déjà enregistrés et les rend **sans jamais écrire**. Toute écriture
  passe par une **dialog ouverte depuis une case** (seul chemin) ; **annuler** n'émet **aucune
  commande**. La **sélection de plage** pour affecter une période est une capacité du palier 9
  (livrée s15).
- **R15 — Suppression de période *(livrée s16)*.** Un **Parent / Admin** supprime une période
  depuis une **dialog contextuelle** (4ᵉ usage du menu clic-case → liste des périodes couvrant la
  date → supprimer). Sous la période supprimée, le **fond reprend** (case → responsable de fond,
  ou neutre si l'index n'est pas mappé), **sans nom fantôme**. La suppression est **idempotente**
  (absente / déjà supprimée = succès no-op), opère sur le **store Mongo durable** (disparaît du
  store relu **et après redémarrage**), porte un **accusé « Période supprimée » à part** (R16,
  registre avertissement-à-part), **propage en temps réel** (case + légende, sans rechargement),
  est **gatée Invité** (R9) et **robuste à l'échec** (API injoignable → dialog ouverte, rien
  appliqué, aucune mise en file ni rejeu). *Le même repli s'applique à une période **orpheline**
  (acteur supprimé, R6).* **Édition** (re-borner / réaffecter) = tranche suivante, **non livrée**.

## Risques

- **Édition concurrente du MÊME jour** sous dialog ouverte (dernière-écriture-gagne à démontrer
  sous dialog) — séquencée derrière la stabilisation temps-réel SignalR (P2/P3) ; aucune règle
  neuve.
- **Cohérence date ↔ index ISO** dans les exemples Gherkin : tout scénario nommant une date ET
  un index/parité doit vérifier `index = ISOWeek(date) % N` (friction s16, Sc.3 — cf. journal
  méthode).
- **Cycle de fond riche** (ancre, frontière, plages, sur-cycles) réclamé par l'usage (gate s10) —
  sujet plein qui rouvre l'ancrage ISO ; séquencé.
