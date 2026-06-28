# Sc.8 — API injoignable : suppression non appliquée, liste et grille inchangées `@erreur` 🖥️ scénario IHM `@caractérisation`

← [Retour au suivi](00-sprint13-suivi.md)

> **Routé vers `ihm-builder`** — **niveau d'acceptation E2E / runtime** (app réellement câblée,
> transport HTTP réel échouant). **PAS** un test backend bUnit-à-doublures. Caractérisation : échec
> transport règle 28, registre acquis. **⚠️ probablement early green (câblage IHM partagé)** : une
> fois l'émission canal + l'issue d'échec posées (Sc.6, mécanique acquise s09 Sc.9), aucun rouge propre.

## Acceptation (BDD)

Test **runtime** : sur l'écran de configuration affiché pour un **Parent** (foyer Parent A / Parent B
/ Nounou), un clic sur le bouton supprimer de Nounou alors que **l'API distante est injoignable** →
- un **message d'échec clair** s'affiche à l'écran ;
- Nounou est **toujours présent** dans la liste ;
- la **grille et la légende restent inchangées** ;
- **aucune mise en file ni rejeu** n'est effectué.

Prouvé sur l'app réellement câblée avec un transport HTTP qui échoue (`HttpRequestException`), pas par
bUnit (transport hors de portée).

**✅ GREEN (caractérisation, early-green confirmé)** — `FrontWasmConfigSupprimerApiInjoignableTempsReelTests`
**vert au 1er coup, aucun code de prod neuf** : l'issue d'échec transport posée au Sc.6
(`catch (HttpRequestException)` → `MessagesEcriture.ServiceInjoignable`, surface `motif-echec-suppression`
distincte de l'accusé succès `accuse-suppression`) couvre le geste de suppression. Le test prouve, sur le
**store réel** de l'API (énumération inchangée — `grand-pere` toujours présent), qu'**aucune écriture n'a
transité** : message d'échec clair + grand-père toujours listé + **aucune fausse confirmation** + liste/store
inchangés, sans mise en file (règle 28). Transport coupé de façon **déterministe** (handler levant
`HttpRequestException` sur le seul `POST /api/canal/supprimer-acteur`, anti-flake proxy loopback Docker).
Suite complète **195/195** (Docker actif, sans `--no-build` ni filtre).

## Tests unitaires (ordonnés)

_Détail piloté par `ihm-builder`._ Aucun driver neuf : réutilise le pattern d'échec transport déjà
livré pour l'ajout/édition de config (s09 Sc.9 — `catch (HttpRequestException)`, message clair, état
inchangé, pas de file).

## Fichiers à créer

- (le cas échéant) issue d'échec dans `src/PlanningDeGarde.Web/CanalEcriture.cs` / `ConfigurationFoyer.razor.cs`
- `tests/PlanningDeGarde.Web.Tests/FrontWasmConfigSupprimerApiInjoignableTempsReelTests.cs`

## Design notes

- **Pas de file ni rejeu** (règle 28) : l'échec est terminal pour cette commande ; la grille reste en
  lecture seule et inchangée. Le message d'échec est distinct de l'accusé succès « Acteur supprimé ».
- **⚠️ Cascade early-green** : tombera vert une fois le câblage Sc.6 + l'issue d'échec acquise en place
  → **batchable** comme caractérisation, pas un early-green inattendu.
