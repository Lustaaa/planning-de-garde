# Besoins fin d'itération — sprint 14 (`impersonation-bornee`)

> Sortie de `/4-retours`. Ferme la boucle du sprint 14 et **réamorce `/2-make-gherkin`**
> sur **un** sujet prioritaire. Retours produit s14 **vide** (goal 9/9 atteint, livraison
> validée G3 sans dépôt) → pilotage strictement **au catalogue** (`docs/BACKLOG.md`),
> pas de classification de défauts/évolutions. Le cap a été tranché en **porte G2** par
> le PO.

## Contexte de départ

- **Livré au s14** : palier 8 tranche 2 — **impersonation bornée lecture seule**
  (incarner un acteur déjà déclaré, vue selon le rôle effectif, bandeau « Vous incarnez X »,
  retour identité réelle, retour auto sur suppression concurrente, durcissement gating
  config). **6 scénarios verts**, acceptation runtime / G3. Cycle de vie acteurs **C/R/U/D
  complet** + impersonation lecture livrée.
- **Retours produit PO** : aucun (`# Retours produit (PO)`, `# Idée pour la suite`,
  `# Consigne pour la suite` en placeholders). **Aucun bug** : livraison verte.
- **Contraintes Tech** : aucune (bypass Tech tranché).
- **Bornes tenues au s14** : zéro persistance neuve, aucun port/handler d'écriture neuf,
  pas l'auth réelle du palier 16 (règle 30 anti-cliquet).

## Décision de cap (porte G2 — PO)

**Prochain sujet make-gherkin = Calendrier navigable** (backlog rang +2, **palier 9**,
épics É4 + É7). Suite d'**usage** naturelle après la tranche acteurs ; besoin ancien
(retours s02/s03). Confirme l'arbitre de séquencement acté : **l'usage tranche, le
technique/dette séquencé derrière**.

## Besoin prioritaire — réamorce `/2-make-gherkin`

### B1 — Calendrier navigable (palier 9, É4 + É7) · PROCHAIN SUJET

- **Type** : évolution / nouveau besoin (nouveau `/2-make-gherkin`).
- **Quoi** : navigation **passé/futur** dans le calendrier (au-delà de la fenêtre
  glissante 35 jours actuelle) + **vues prédéfinies** (semaine / mois / 4 semaines) +
  **amorce de sélection de plage de cases** pour définir une période.
- **Origine** : backlog rang +2 (G2 PO clôture s14) · palier 9 spec v14 · besoin ancien
  retours s02 (#3 navigation) / s03.
- **Couverture actuelle** : grille agenda **5 semaines lecture seule** livrée (s03,
  fenêtre stricte 35 jours, bornes inf./sup.) ; **navigation absente** (Épic 4 —
  « Navigation dans le mois » ⬜) ; **sélection de plage absente**.
- **Borne ~2h IA** : **périmètre exact à cadrer au make-gherkin** — le plus petit
  incrément d'usage est probablement la **navigation seule** (semaines précédente /
  suivante, ou bascule de vue) ; la **sélection de plage de cases → définir une période**
  est un sujet plein qui peut déborder ~2h et devra sans doute être **séquencé en tranche
  2** (corollaire de découpe). À trancher au cadrage make-gherkin.
- **Arbitre interne** (si tensions au cadrage) : l'**usage observable** prime ; on livre
  d'abord le déplacement dans le temps (valeur immédiate, lecture), la sélection de plage
  (écriture en contexte enrichie) vient derrière si elle ne tient pas dans la borne.

## Reste du backlog — séquencé derrière (non retenu ce tour)

| Rang | Sujet | Pourquoi pas maintenant |
|-----:|-------|-------------------------|
| +3 | **Rétrofit garde déterministe *TempsReel* SignalR** (généraliser `WaitForState` + cibler la **convergence SignalR multi-clients**, distincte de la course d'énumération déjà gardée s13) — É3, dette de test | Dette structurante **sans observable métier** (vigilance « faux sentiment de progrès ») ; renforcée par les notes IA s14 (flake 1/30 sous charge, convergence multi-écrans non couverte). **Prérequis de l'édition concurrente (rang +4)** — à prendre avant elle, mais après l'usage (Calendrier). |
| +4 | **Édition concurrente du même jour sous dialog ouverte** (last-write-wins règle 11) — É7 | Cas limite runtime ; **dépend du +3** (fondation temps-réel stabilisée). Différé jusqu'à stabilisation SignalR. |
| hors-cap | **Impersonation écriture « au nom de »** (extension de l'impersonation lecture livrée s14) — É10 | **Franchit la borne dure du palier 8** (lecture seule explicite) et **amorce l'auth réelle du palier 13/16** (chemin d'écriture neuf, règle 30 anti-cliquet, D3 s14). À ne tirer que sur **décision PO explicite de changer le cap**. |
| +5 | **Cycle de fond riche** (ancre/début, frontière de jour, plage début/fin, sur-cycle vacances, WE-only) — É7, É1 | Sujet plein qui rouvre la décision « ancrage ISO sans ancre » ; retour PO s10, séquencé. |

## Règles d'arbitrage consignées

- **Arbitre de séquencement** : l'**usage tranche**, le technique / la dette / la
  persistance sont séquencés **derrière** l'usage (borne anti-cliquet règle 30, acté
  backlog). Confirmé par le choix G2 (Calendrier navigable devant le rétrofit SignalR).
- **Borne anti-cap** : toute reprise vers l'**écriture « au nom de »** ou l'**auth réelle**
  exige une **décision PO explicite de changer le cap** (ne pas tirer le palier 13/16 en
  avance de phase).
- **Corollaire de découpe** : si le périmètre Calendrier déborde ~2h, **couper la
  sélection de plage** et la re-séquencer sans toucher au cœur navigation.

## Notes

- **Aucun bug à réparer** : suite verte, pas de symptôme PO, pas de défaut localisé dans
  HEAD à confronter → aucun item `bug`, aucune réparation ordonnée.
- Les notes **méthode / IA** du fichier unifié (flake *TempsReel*, réentrance renderer
  bUnit, prédiction early-green) relèvent de **`retro-sprint`**, pas de ce backlog
  produit. Elles sont mentionnées ici **uniquement** au titre du séquencement (impact sur
  le rang +3).
