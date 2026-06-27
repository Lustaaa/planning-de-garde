# Config foyer · édition des acteurs (volatile) — Analyse & scénarios

## Analyse technique

- **Composants impactés**
  - *Infrastructure* — store mutable en mémoire (singleton) derrière les ports
    `IReferentielResponsables` (noms) et `IPaletteCouleurs` (couleurs), seedé à
    l'init depuis `Foyer` puis éditable ; remplace la lecture des dictionnaires
    `static readonly` de `Foyer` dans `FoyerReferentielResponsables` /
    `FoyerPaletteCouleurs`. Volatile = re-seedé au redémarrage.
  - *Application* — commande d'édition (`EditerActeur` : renommer / recolorier) +
    handler ; validation légère (id stable connu, nom non vide). `GrilleAgendaQuery`
    **inchangé** : il relit nom+couleur via les ports, la légende reste une
    projection dédoublonnée par id.
  - *Web/Blazor* — nouvel écran de configuration du foyer (vue + code-behind),
    consommant l'API distante via le canal requête/réponse (règle 27) ; aucune vue
    n'écrit le domaine en direct. Diffusion temps réel (`INotificateurPlanning` /
    SignalR) déclenchée sur édition aboutie — jamais d'écriture par la diffusion.

- **Couches & dépendances** — ports en *Application* (intérieur), store mutable en
  *Infrastructure* (extérieur, remplaçable par l'adaptateur durable du palier 13
  sans toucher au domaine). *Domaine* inchangé : le nom/couleur d'un acteur est
  config foyer, pas un agrégat à invariant fort. Litmus : store testable sans
  framework ; infra remplaçable sans toucher au domaine.

- **Contrats de données**
  - Commande `EditerActeur` { acteurId (stable, obligatoire, doit exister),
    nom?, couleur? } via HTTP ; sortie = confirmation de l'effet, ou échec clair
    (nom vide / id inconnu / API injoignable).
  - Store config foyer : id stable → (nom, couleur), seedé depuis
    `Foyer.NomsParResponsable` + `Foyer.CouleursParActeur`, muté en session ;
    l'id stable n'est jamais une donnée éditable.

- **Write vs read (CQRS)** — l'édition est une **commande** qui mute le store
  derrière les ports ; la grille et la légende sont des **projections de lecture**
  (`GrilleAgendaQuery`), jamais un getter d'écriture sur un agrégat.

- **Invariants**
  - L'identifiant stable d'un acteur ne change **jamais** lors d'une édition ;
    seuls nom et couleur mutent.
  - Nom et couleur se résolvent toujours sur l'id stable (règle 18), jamais sur le
    libellé ; la légende reste dédoublonnée par id stable.
  - Tout acteur conserve un nom non vide : un nom vide / tout-espaces est refusé,
    l'ancienne valeur est conservée.
  - Un acteur hors set de couleurs garde sa teinte neutre assumée : renommer ne
    crée pas de couleur ; recolorier vers une teinte du set est le seul moyen de
    sortir du neutre.

- **Points d'attention TDD**
  - Garde-fou anti early-green (« vert qui ment sur la grille ») : l'acceptation
    vérifie l'édition sur une grille **réellement câblée** (front WASM + API
    distante, sans doublure sur le chemin observé), pas un test à doublures.
  - Ne doubler que les ports / le canal HTTP / le notificateur ; tester le store
    mutable en unitaire, puis la commande/handler, puis l'acceptation runtime.
  - Concurrence : tester dernière-écriture-gagne + convergence des deux grilles via
    diffusion (pas de version, pas de rejet) ; volatilité : tester la perte au
    redémarrage / re-seed.

## Scénarios

Feature: Édition volatile des acteurs du foyer (noms + couleurs) — un écran de
configuration permet de renommer et recolorier les acteurs déjà semés ; la grille
(case + légende) relit immédiatement la configuration dans la session, sans
persistance durable. L'identifiant stable ne change jamais ; la résolution reste
sur l'id stable (règle 18). Mémoire partagée du foyer : dernière écriture gagne,
propagée par la diffusion temps réel. État volatile assumé, miroir du seed en dur.

### Scenario 1 — Renommer un acteur : la case et la légende suivent `@nominal` `@vert`

```gherkin
Scenario: Renommer un acteur, la case et la légende suivent dans la session
  Given la grille du foyer affiche, à la semaine du 13/07/2026, une période de garde
    affectée à l'acteur « parent-a » (libellé « Alice », couleur bleu) : la case du
    14/07 porte « Alice » et la légende liste « Alice » en bleu
  When depuis l'écran de configuration du foyer, je renomme l'acteur « parent-a »
    de « Alice » en « Alicia » et j'enregistre
  Then sans recharger la page, la case du 14/07 affiche « Alicia » et l'entrée de
    légende affiche « Alicia » (toujours en bleu), et l'identifiant « parent-a »
    est inchangé
```

### Scenario 2 — Recolorier un acteur : la case et la légende changent de couleur `@nominal` `@vert`

```gherkin
Scenario: Recolorier un acteur, la case et la légende changent de couleur
  Given la grille affiche, à la semaine du 13/07/2026, une période affectée à
    « parent-b » (libellé « Bruno », couleur orange) : la case du 15/07 est orange
    et porte « Bruno », la légende liste « Bruno » en orange
  When depuis l'écran de configuration, je recolorie l'acteur « parent-b » de orange
    en violet et j'enregistre
  Then sans recharger, la case du 15/07 devient violet en conservant le libellé
    « Bruno », l'entrée de légende « Bruno » passe au violet, et l'identifiant
    « parent-b » est inchangé
```

### Scenario 3 — Renommer vers un nom long : troncature, complet au survol et en légende `@limite` `@vert`

```gherkin
Scenario: Renommer vers un nom long, tronqué dans la case mais lisible en entier
  Given la grille affiche, à la semaine du 13/07/2026, une période affectée à
    « parent-c » dont le libellé court « Marie » tient entier dans la case du 16/07
  When je renomme « parent-c » de « Marie » en « Marie-Hélène Grand-Dubois »
    (25 caractères) et j'enregistre
  Then la case du 16/07 affiche le nom tronqué (ex. « Marie-Hél… »), son intitulé
    complet « Marie-Hélène Grand-Dubois » reste lisible au survol (attribut natif),
    et l'entrée de légende affiche le nom complet
```

### Scenario 4 — Collision de couleur entre deux acteurs : distingués par le nom `@limite` `@vert`

```gherkin
Scenario: Recolorier deux acteurs vers la même couleur, distingués par le nom
  Given la grille affiche, à la semaine du 13/07/2026, deux périodes distinctes :
    « parent-a » (« Alice », bleu) sur le 14/07 et « parent-b » (« Bruno », orange)
    sur le 15/07, et la légende liste les deux couleurs
  When je recolorie « parent-b » en bleu — la même couleur que « parent-a » — et
    j'enregistre
  Then les cases du 14/07 et du 15/07 sont toutes deux bleues mais restent
    distinguables par leur nom (« Alice » vs « Bruno »), et la légende liste deux
    entrées bleues nommées distinctement (collision assumée, la lisibilité repose
    sur le nom)
```

### Scenario 5 — Éditer un acteur hors set de couleurs : nom suivi, teinte neutre conservée `@limite` `@vert`

```gherkin
Scenario: Renommer un acteur hors set de couleurs sans lui créer de couleur
  Given la grille affiche, à la semaine du 13/07/2026, une période affectée à
    l'acteur hors set de couleurs « grand-pere » (libellé « grand-père », teinte
    neutre grise assumée) : la case du 17/07 est grise et porte « grand-père »
  When je renomme « grand-pere » de « grand-père » en « Papy Jo » et j'enregistre,
    sans lui attribuer de couleur du set
  Then la case du 17/07 et l'entrée de légende affichent « Papy Jo », et la teinte
    reste neutre (grise) car l'acteur n'a pas de couleur dans le set — le renommage
    ne crée pas de couleur
```

### Scenario 6 — Éditer un acteur absent de la fenêtre affichée : pas d'entrée fantôme `@limite` `@vert`

```gherkin
Scenario: Éditer un acteur sans période dans la fenêtre, sans entrée fantôme
  Given la grille affiche la fenêtre de 5 semaines à partir du 13/07/2026, et
    l'acteur « parent-c » (libellé « Marie ») n'a aucune période dans cette fenêtre
  When je renomme « parent-c » de « Marie » en « Mathilde » et j'enregistre
  Then l'édition est confirmée (l'écran de configuration affiche désormais
    « Mathilde » pour « parent-c »), la grille de la fenêtre courante reste
    inchangée, et la légende ne fait apparaître aucune entrée pour « parent-c »
    (pas d'entrée fantôme tant qu'aucune période ne le porte dans la fenêtre)
```

### Scenario 7 — Deux écrans renomment le même acteur : dernière écriture gagne, les grilles convergent `@limite` `@vert`

```gherkin
Scenario: Deux écrans renomment le même acteur, les grilles convergent
  Given deux écrans (deux parents) affichent la même grille partagée du foyer où
    la case du 15/07/2026 est affectée à « parent-b » (libellé « Bruno »)
  When le premier écran renomme « parent-b » en « Bruno M. », puis juste après le
    second écran le renomme en « Bruno Martin », et les deux enregistrent
  Then la dernière écriture gagne : les deux grilles convergent vers « Bruno Martin »
    dans la case du 15/07 et dans la légende, propagé par la diffusion temps réel,
    et aucune édition n'est rejetée (pas de conflit, pas de version)
```

### Scenario 8 — Renommer avec un nom vide : édition refusée, ancien nom conservé `@erreur` `@vert`

```gherkin
Scenario: Renommer avec un nom vide, édition refusée
  Given la grille affiche, à la semaine du 13/07/2026, une période affectée à
    « parent-b » (libellé « Bruno », orange) sur la case du 15/07
  When depuis l'écran de configuration, je tente d'enregistrer « parent-b » avec un
    nom vide (chaîne vide ou uniquement des espaces)
  Then l'édition est refusée avec un message clair à l'écran (« le nom ne peut pas
    être vide »), et la case du 15/07 et l'entrée de légende conservent « Bruno »
    (inchangé)
```

### Scenario 9 — API distante injoignable : échec clair, édition non appliquée `@erreur` `@vert`

```gherkin
Scenario: API injoignable, l'édition n'est pas appliquée et reste à resoumettre
  Given la grille affiche, à la semaine du 13/07/2026, une période affectée à
    « parent-a » (libellé « Alice », bleu) sur la case du 14/07, et l'API distante
    est injoignable
  When depuis l'écran de configuration, je renomme « parent-a » en « Alicia » et
    j'enregistre
  Then l'enregistrement échoue clairement (message à l'écran), l'édition n'est pas
    appliquée (la case du 14/07 et la légende gardent « Alice ») et reste à
    resoumettre, sans mise en file ni rejeu
```

### Scenario 10 — Volatilité : après redémarrage, le seed d'origine réapparaît `@limite`

```gherkin
Scenario: L'édition volatile est perdue au redémarrage, le seed réapparaît
  Given dans la session courante, « parent-b » a été renommé « Bruno » →
    « Bruno Martin » et recolorié orange → violet, et la grille affiche
    « Bruno Martin » en violet sur la case du 15/07/2026 et en légende
  When le serveur redémarre (ou la session est rechargée), réinitialisant la
    mémoire partagée du foyer
  Then l'édition volatile est perdue : la grille réaffiche le seed d'origine
    (« Bruno » en orange) sur la case du 15/07 et dans la légende — volatilité
    assumée et transitoire, miroir du seed (dette à éteindre au palier persistance
    réelle)
```

## Risques & points de vigilance

- **Dette volatile assumée** : le store se re-seede au redémarrage (Sc.10), à
  éteindre au palier 13 (persistance réelle) — ne pas tirer la durabilité en avant
  (corollaire « éditable ≠ durable »).
- **Early-green sur la grille** : un test à doublures peut « voir » l'édition sans
  câblage réel ; l'acceptation runtime (front WASM + API distante, sans doublure
  sur le chemin observé) est le rempart (Sc.1/2/7).
- **Collision de couleur** entre deux acteurs (Sc.4) assumée : la lisibilité repose
  sur le nom + légende, pas la couleur seule (règle 17) ; ce n'est pas un défaut.
- **Acteur édité hors fenêtre** (Sc.6) : n'introduire aucune entrée fantôme en
  légende tant qu'aucune période ne porte l'acteur dans la fenêtre.
- **Diffusion déclenchée par l'écriture** (Sc.7) : garantir que l'édition aboutie
  actualise les autres grilles sans jamais écrire par le canal de diffusion.
