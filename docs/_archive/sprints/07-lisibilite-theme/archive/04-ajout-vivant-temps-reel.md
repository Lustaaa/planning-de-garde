# Sc.4 — Ajout vivant par diffusion temps réel : nom et légende suivent

`@limite` `🖥️ IHM`

↩ Retour : [00-sprint07-suivi.md](00-sprint07-suivi.md)

**Routage** : **100 % runtime IHM** (`ihm-builder`). **Aucun driver backend** — la
re-projection couvre mécaniquement le nouvel état (caractérisation triviale des Sc.1/Sc.2).

## Acceptation (BDD) — niveau **runtime/IHM** (routé `ihm-builder`)

> Le symptôme PO est un fait **runtime SignalR** : une écriture d'un autre acteur, diffusée
> sur le canal de **lecture existant** (palier 1, hub réel), fait **suivre** nom + légende
> **sans rechargement**. bUnit seul **ne prouve jamais** ce câblage (render mode, hub réel,
> re-render) — acceptation sur l'app **réellement câblée** obligatoire.

`Should_Faire_apparaitre_le_nom_Bruno_dans_la_case_du_jeudi_02_07_2026_et_une_seconde_entree_de_legende_Bruno_sans_rechargement_When_une_periode_affectee_a_Bruno_est_diffusee_sur_le_canal_de_lecture`

- **Niveau** : E2E/runtime sur l'app câblée + **hub SignalR réel existant** (palier 1) —
  **asserté, pas reconstruit** (la diffusion temps réel n'est pas une infra de ce sprint).
- **Observable** : sans rechargement de page, la case du jeudi 02/07 affiche « Bruno » et
  la légende passe d'une à **deux** entrées (Alice + Bruno).

## Tests unitaires backend

*(Néant.)* La projection re-projetée après l'arrivée d'une période rend mécaniquement le
nouvel état (nom + légende) — déjà couvert par Sc.1 #1/#2 et Sc.2 #1. Aucun rouge backend
à attendre : inscrire un test ici ne ferait que recaractériser les Sc.1/Sc.2.

## Fichiers à créer / modifier

- *(Aucun fichier backend.)*
- **Câblage IHM** (routé `ihm-builder`) — le suivi temps réel existe déjà
  (`PlanningPartage.razor.cs` : `_hub.On(...) → ChargerAsync → StateHasChanged`) ;
  `ihm-builder` **assert** que nom + légende suivent ce canal (ajout vivant), il ne
  reconstruit pas le hub.

## Design notes

- **Ajout vivant = ré-exécution de la projection** déclenchée par l'évènement de diffusion.
  La cohérence nom/légende au rechargement live découle de la projection déterministe — la
  seule chose à prouver est que le **câblage** réagit (anti « vert qui ment » sur grille
  live).
- Ne **pas** redoubler l'infra temps réel (livrée s05) : on **assert** sur le canal
  existant (Spy/réel), conformément à l'analyse technique.
