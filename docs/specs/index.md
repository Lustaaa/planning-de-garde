# Spec vivante — index navigable

La spec produit est **éclatée par sujet fonctionnel** et **éditée en diff** (le `scrum-master` ne
touche que le sujet concerné à chaque `/cloture`, jamais une réécriture intégrale — c'est ce qui
faisait enfler la spec ×10 de `01` à `15`).

> **Migration complète.** Tout le contenu du dernier monolithe `docs/15-specification.md` a été
> **migré ici** ; `docs/specs/` se suffit à lui-même et constitue la **source de vérité courante**.
> Les versions monolithiques `docs/NN-specification.md` restent **figées en historique**.

## Sujets

| Sujet | Fichier | Résumé |
|---|---|---|
| Vision & contexte | [`vision-et-contexte.md`](vision-et-contexte.md) | Le hub `/planning`, back découplé en API, vitrine ; récit de l'état livré. |
| Objectif & arbitrage | [`objectif-et-arbitrage.md`](objectif-et-arbitrage.md) | Arbitre d'usage, exceptions bornées (fondation, persistance config foyer), corollaires « durable ICI », borne anti-cliquet, découpe, révisions hors boucle. |
| Séquence de livraison | [`sequence-de-livraison.md`](sequence-de-livraison.md) | Roadmap des paliers 1→18 avec statut livré / non livré + notes de séquencement. |
| Fondations — back découplé en API | [`fondations-api.md`](fondations-api.md) | Palier 1 : canal requête/réponse, hôte d'API détaché, front WASM, CORS, OpenAPI (règles 28-30). |
| Saisie visible, grille lisible & thème | [`saisie-et-grille.md`](saisie-et-grille.md) | Paliers 2-3 : saisie visible, lisibilité (couleur + nom + légende), thème (règles 18-22). |
| Acteurs & configuration du foyer | [`acteurs-et-config-foyer.md`](acteurs-et-config-foyer.md) | Paliers 4/5/8/13 : édition, ajout, persistance Mongo, CRUD complet, gating, impersonation bornée lecture ; **référentiel de lieux éditable + persisté** (s27, pilote validation de pose ET sélecteurs des dialogs) ; **auth / login complet** — fondation identité, connexion, activation, protection routes, mot de passe, libre-service, récupération, OAuth (R1-9 + R11-R14) ; **câblage réel reset E2E + login mot de passe + amorçage démo convergent** (s28, R14bis). |
| Écriture en contexte — dialogs | [`ecriture-en-contexte.md`](ecriture-en-contexte.md) | Palier 7 : menu clic-case, trois dialogs, issues succès/échec/chevauchement, gating (règles 14/16/17/24/25/28). |
| Périodes de garde & cycle de fond | [`periodes-et-cycle-de-fond.md`](periodes-et-cycle-de-fond.md) | Palier 6 : résolution surcharge > fond > neutre, affecter/supprimer/éditer une période (règles 11/12/14/15 + R15bis). |
| Calendrier navigable | [`calendrier-navigable.md`](calendrier-navigable.md) | Palier 9 (**prochain, non livré**) : navigation passé/futur, vues prédéfinies, sélection de plage. |
| Mécaniques de base | [`mecaniques-de-base.md`](mecaniques-de-base.md) | Vue d'ensemble des mécaniques transverses (planning, cycle, lisibilité, écriture, canaux, persistance). |
| Règles de gestion | [`regles-de-gestion.md`](regles-de-gestion.md) | Catalogue numéroté R1→R30 (R11/12/14/15 détaillées dans `periodes-et-cycle-de-fond.md`). |
| Risques & questions ouvertes | [`risques-et-questions-ouvertes.md`](risques-et-questions-ouvertes.md) | Pilotage, frontières/bornes, dettes de test, révisions de règle en attente, risques produit. |

> **Épics** : le monolithe v15 ne portait **pas** de catalogue d'épics, seulement des **codes inline**
> (É2 CRUD acteurs, É4 + É7 calendrier navigable, É10 impersonation bornée). Ils sont **conservés en
> place** dans les sujets concernés ; aucun fichier `epics.md` n'est créé (pas de catalogue à migrer).

## Convention

- Un fichier par sujet : `docs/specs/<slug-sujet>.md`, format maison (Contexte / Objectif & arbitrage /
  Séquence / Mécaniques / Règles de gestion / Risques), **serré et scannable**.
- **Source unique par contenu** : une règle a **un seul** texte canonique. Les règles du cycle de fond
  et du cycle de vie d'une période (R11, R12, R14, R15 + R15bis) vivent dans
  [`periodes-et-cycle-de-fond.md`](periodes-et-cycle-de-fond.md) ; le catalogue
  [`regles-de-gestion.md`](regles-de-gestion.md) les **référence** sans dupliquer leur texte.
- Ajouter chaque nouveau sujet à la table ci-dessus (titre + lien + résumé).
- Les versions monolithiques `docs/NN-specification.md` restent **figées en historique**.
