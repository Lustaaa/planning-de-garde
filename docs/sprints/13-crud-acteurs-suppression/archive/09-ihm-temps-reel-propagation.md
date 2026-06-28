# Sc.9 — Temps réel : la suppression propage grille et légende sans rechargement `@limite` 🖥️ scénario IHM `@caractérisation`

← [Retour au suivi](00-sprint13-suivi.md)

> **Routé vers `ihm-builder`** — **niveau d'acceptation runtime / intégration SignalR** (deux écrans
> réellement câblés sur le même hub). **PAS** un test backend bUnit-à-doublures. Caractérisation :
> diffusion SignalR lecture seule déclenchée par l'écriture aboutie. **⚠️ probablement early green
> (câblage IHM partagé)** : la diffusion est déjà déclenchée par le handler sur succès (Spy backend) ;
> reste à observer la propagation à l'écran, sur câblage acquis (s09/s10).

## Acceptation (BDD)

Test **runtime** : deux écrans affichant le même planning partagé, l'un en configuration pour un
**Parent** (foyer Parent A / Nounou, période saisie attribuant le mardi 16/06/2026 à Nounou avec fond
Parent A). Quand le Parent supprime Nounou depuis l'écran de configuration →
- le **second écran** voit, **sans rechargement**, la case du 16/06 **retomber sur Parent A** ;
- le **second écran** voit la **légende dédoublonnée** ne **plus faire apparaître** Nounou.

Prouvé sur l'app réellement câblée (SignalR réel entre deux écrans), pas par bUnit. **Convention
anti-flake des tests *TempsReel*** à respecter (P2 hors scope — ne pas en faire un filet de régression
automatisé instable ; valider en acceptation runtime / G3).

**✅ GREEN (caractérisation, early-green confirmé)** — `FrontWasmGrilleSuppressionTempsReelTests`
**vert au 1er coup, aucun code de prod neuf** : deux écrans (config Parent + grille) câblés à la MÊME API
distante réelle (store singleton partagé, hub SignalR réel commun). grand-père garde le 16/06 (surcharge),
cycle de fond N=1 → parent-a (« Alice ») sur la fenêtre. Après suppression depuis l'écran de configuration,
le **second écran** voit, **sans rechargement** : la case du 16/06 **retomber sur Parent A** (repli surcharge
orpheline → fond, filtre `Resolvable`) et la **légende dédoublonnée** ne **plus** faire apparaître grand-père.
Compose les acquis (diffusion sur succès Sc.1, repli Sc.2, légende dédoublonnée s07, bouton Sc.6). Convention
anti-flake *TempsReel* appliquée : attente déterministe d'établissement de connexion via pompe de diffusion
idempotente, isolation TestContext/store/hub propres. Stable ≥3× (isolation + suite complète). Suite complète
**196/196** (Docker actif, sans `--no-build` ni filtre).

> **Balayage runtime (composant partagé `ConfigurationFoyer`).** Les changements IHM Sc.6/Sc.7 (bouton
> supprimer + `@inject SessionPlanning` + gating) ont **alourdi le rendu** de l'écran de configuration, ce qui
> a **exposé une course latente** (`UnknownEventHandlerId` sur le `select`) dans **7 tests `*TempsReel*`
> préexistants** qui interagissaient avec le `select` **sans attendre** la fin de l'énumération asynchrone
> (`NomLongEdite`, `RenommerActeur`, `Recolorier`, `HorsSetNeutre`, `HorsFenetrePasDeFantome`, `CollisionCouleur`,
> `NomVideRefuse`) — déterministe en isolation. **Fix ciblé** : ajout du **garde déterministe** standard
> (`WaitForState(acteur-foyer Count > 0)`) déjà utilisé par les tests config frères. Ce n'est **pas** le
> rétrofit P2 de la convergence SignalR (isolation store/hub) — c'est la mise au standard du garde
> d'énumération sur les tests que la touche du composant partagé a destabilisés.

## Tests unitaires (ordonnés)

_Détail piloté par `ihm-builder`._ Aucun driver neuf : la diffusion sur succès est acquise (le handler
`SupprimerActeur` notifie `INotificateurPlanning`, cf. Sc.1) ; la re-projection de la grille intègre le
repli sur fond (Sc.2, backend). Reste l'observation runtime de la propagation.

## Fichiers à créer

- `tests/PlanningDeGarde.Web.Tests/FrontWasmGrilleSuppressionTempsReelTests.cs` (acceptation runtime, convention anti-flake)

## Design notes

- **Composition d'acquis** : diffusion (Sc.1) + repli surcharge orpheline → fond (Sc.2) + légende
  dédoublonnée (s07). Le second écran re-projette et voit le résultat sans rechargement.
- **Anti-flake** : exécution parallèle SignalR connue flaky (P2, Risques) ; suivre la convention
  *TempsReel* existante. **→ remonter au CP si la stabilisation devient un blocage** plutôt que de
  désactiver le test.
