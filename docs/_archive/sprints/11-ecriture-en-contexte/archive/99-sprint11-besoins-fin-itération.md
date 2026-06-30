# Besoins de fin d'itération — Sprint 11 (écriture-en-contexte)

> Sortie de `/4-retours` (agent `retours-challenge`). Backlog priorisé issu du challenge des
> retours produit du sprint 11. **Réamorce `/2-make-gherkin` sur UN sujet prioritaire** (P1).
> Le reste est séquencé derrière, rien d'abandonné. À reverser dans les épics du `docs/BACKLOG.md`
> au `/5-consolidation`.

## Contexte du challenge

- **Goal 7/7 atteint**, nettoyage scaffolding fait (pages/routes/liens `poser-slot` +
  `affecter-periode` retirés, menu clic-case couvrant). Suite **179/179** verte après nettoyage.
- **Aucun retours produit frais** : les sections `# Retours produit (PO)`, `## IHM - général`,
  `## IHM - /planning`, `## Tech`, `# Idée pour la suite` et `# Consigne pour la suite` du
  fichier `99-sprint11-retours.md` sont **vides**. Le matériau forward est l'**overflow tranché
  par le CP** (tranche de secours close) + une **alerte de méthode** (flakes SignalR).
- **Arbitre PO (G2)** : achever l'écriture-en-contexte prime sur l'ouverture d'un nouveau cap
  (CRUD acteurs +2, cycle de fond riche +3, survol→résumé) — plus petit incrément cohérent,
  pattern déjà prouvé (risque faible), ferme proprement l'épic avant de changer de surface.

## Prochain sujet `/2-make-gherkin`

**P1 — 3e dialog « Définir un transfert » depuis une case** (+ retrait de la dernière page de
saisie dédiée). Ferme l'épic écriture-en-contexte (É12 / É8).

## Besoins priorisés

### P1 — Dialog « Définir un transfert » en contexte (PROCHAIN SUJET)

- **Type** : évolution (pas un bug ; aucun défaut localisé dans `src/`).
- **Besoin** : compléter l'écriture en contexte pour le **transfert** — ajouter une **3e entrée**
  au **menu clic-case** (pattern prouvé Sc.1/Sc.2), ouvrant une dialog `Définir un transfert`
  pré-remplie sur la date de la case ; puis **retirer** la dernière page de saisie hors-contexte
  `/planning/definir-transfert` (route + page `DefinirTransfert.razor` + code-behind + lien-barre).
  À la livraison, **plus aucun écran de saisie dédié ne subsiste** → l'épic « écriture en contexte »
  est refermé.
- **Réutilise (aucune fondation tirée)** : commande/handler **`DefinirTransfert`** existants, le
  **canal HTTP** (`POST /api/canal/definir-transfert`) et la **diffusion SignalR lecture seule**
  (s04→s10). Le pattern menu est explicitement extensible (décision CP Sc.2 : « la 3e dialog
  Transfert s'ajoute comme une entrée de menu sans retoucher le câblage du clic-case »).
- **Bornes (sous peine de STOP + escalade CP)** :
  - **Aucune règle ni handler neuf** — déplacement de la saisie en contexte, pas une mécanique métier.
  - **Transfert reste InMemory** (borne anti-cliquet, règle 30) — aucune persistance tirée en avant.
  - **Grille lecture seule** (règle 14) — le menu n'écrit jamais, il ouvre une dialog.
  - **Gating Invité mutualisé** sur le déclencheur unique (rendu conditionnel `Session.EstParent`,
    règle 9) — réutilise le contexte rôle acquis s01, ni auth ni impersonation tirées.
  - **Rétroaction par issue** cohérente avec les dialogs livrées : succès ferme + grille relue ;
    refus domaine OU API injoignable → dialog reste ouverte, message dans la dialog, saisie
    conservée, grille inchangée (règle 28). Transfert incomplet → refus clair (règle s01).
  - **Acceptation runtime obligatoire** sur app réellement câblée (front WASM + API distante +
    store réel + SignalR) — rempart anti vert-qui-ment ; bUnit composant = drivers de détail seulement.
  - **Garde-fou nettoyage** : maintenir la **suite complète verte** (Docker actif) ; mettre à jour
    toute nav/test référençant la route retirée ; ne retirer la page **qu'après** que l'acceptation
    runtime de la dialog transfert prouve la couverture intégrale de l'écran supprimé.
- **Épics** : É8 (Transferts), É12 (Écriture en contexte). **Spec** : palier 7 ; règles 9/14/17/28/30.

### P2 — Stabilisation des flakes temps-réel SignalR (action de MÉTHODE)

- **Type** : question ouverte (méthode/tech) — **PAS un goal produit, PAS un sujet make-gherkin**.
- **Constat** : `FrontWasmConfigCycleServiceInjoignableTempsReelTests` et
  `FrontWasmConfigCycleZeroSemaineRefuseTempsReelTests` sont **verts en isolation** mais **flaky
  sous exécution parallèle** (cause = timing SignalR/Docker).
- **Confrontation HEAD** : les flakes vivent dans `tests/PlanningDeGarde.Web.Tests/` — **aucun défaut
  localisable dans `src/`**. Ce n'est donc ni un `bug` produit ni une réparation métier : c'est une
  **fiabilité de test** à porter en **retro-sprint** (méthode).
- **Rôle dans la séquence** : **prérequis de fait de P3** — purger ces flakes **déverrouille** le
  driving de l'édition concurrente sur une fondation temps-réel stable.

### P3 — Édition concurrente du même jour sous dialog ouverte (DIFFÉRÉE)

- **Type** : nouveau besoin (cas limite runtime).
- **Besoin** : prouver le comportement quand **deux acteurs éditent le même jour** alors qu'une
  dialog est ouverte — **dernière-écriture-gagne** (règle 11, acquise s10) à démontrer **sous dialog
  en contexte**. La caractérisation s11 ne couvre que « le rafraîchissement de fond n'interfère pas
  avec une dialog ouverte », pas l'édition concurrente du même jour.
- **Statut** : **DIFFÉRÉE jusqu'à stabilisation SignalR (P2)** — dépendance cachée sur la stabilité
  temps-réel ; driver ce besoin sur une fondation instable produirait des **scénarios flaky par
  construction**. À séquencer **après** P2.
- **Épics** : É7 (Périodes). **Spec** : règle 11.

## Notes pour `/5-consolidation`

- **Garde-fou IDateTimeProvider (à reporter)** : le pré-remplissage des dialogs est
  **DateContexte-exclusif** (le repli horloge est du **code mort** tant qu'aucun chemin de saisie
  hors-contexte n'existe). **Ne PAS supprimer le port `IDateTimeProvider`** (la grille s'en sert pour
  « aujourd'hui »/fenêtre) ; **réintroduire le repli horloge** dans la dialog si un point d'entrée
  hors-contexte réapparaît à un futur palier. La règle 17 composée reste respectée (il n'y a
  simplement plus de chemin hors-contexte ce sprint).
- **Écart de numérotation préexistant** : la *Séquence de livraison* de la spec numérote les dialogs
  **palier 7** tandis que la table *À faire* du backlog les place **palier 8** — substance identique,
  à réaligner au `/5-consolidation`.
- **Reversement backlog** : P1 → épics É12/É8 (à passer ✅ à la clôture de l'increment transfert) ;
  P3 → épic É7 (besoin séquencé après P2) ; P2 → action de méthode (retro-sprint), pas une entrée
  d'épic produit.

## Risque transverse

- **Aucun retours d'usage frais** ce sprint : la priorisation s'appuie sur la mécanique de découpe
  et la dette tech, **pas** sur une douleur d'usage exprimée. À confirmer au **prochain gate visuel**.
