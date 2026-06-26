# Scénario 2 — Le front WASM consomme l'API distante : un slot posé apparaît dans sa case

`@nominal @vert` · 🖥️ **scénario IHM** — **Routé vers `ihm-builder`**

[← Retour au suivi](00-sprint05-suivi.md)

> **Axe : IHM / runtime.** Le comportement vit dans le **navigateur** : le front s'exécute en
> **WebAssembly** et émet ses écritures vers une **API distante** (URL configurable, plus
> `nav.BaseUri`), par **HTTP cross-host réel**. Le symptôme PO est un **fait d'usage runtime**
> (« je pose un slot depuis le front WASM et il apparaît dans la grille, l'écriture ayant
> réellement transité par l'API distante »). **JAMAIS** planifié comme un bUnit composant à
> doublures : un bUnit force l'interactivité et stub le transport — il **ment au vert** sur un
> render mode WASM manquant, une URL d'API mal configurée ou un échec du transport HTTP distant.
>
> **Niveau d'acceptation : E2E / runtime** sur l'**app réellement câblée** (front WASM + hôte
> d'API détaché démarré, DI réelle, HTTP distant). Le détail RED→GREEN du `.razor` / de la
> config / du câblage HTTP est piloté par `ihm-builder`.

## Acceptation (BDD)

`Should_Faire_apparaitre_le_slot_ecole_08h30_16h30_dans_la_case_du_mercredi_24_06_2026_When_le_front_WASM_pose_un_slot_via_l_API_distante` — ✅ GREEN

**Tests runtime (Web.Tests, deux hôtes réels câblés)** :
- `Should_Faire_apparaitre_le_slot_..._When_le_front_WASM_pose_un_slot_via_l_API_distante` — ✅ GREEN — pose émise par le **client réel du front** vers l'**hôte d'API détaché réel** ; slot observé dans le **store réel distant** via `GrilleAgendaQuery`.
- `Should_Cibler_l_URL_d_API_distante_configurable_When_le_client_d_ecriture_du_front_WASM_est_construit` — ✅ GREEN — le client du front cible l'URL **configurable** (`Api:BaseUrl`), non plus `nav.BaseUri`.

> Discriminance du rouge : (1) le client ignorait `Api:BaseUrl` (cible `https://localhost/` ≠ URL distante) ; (2) l'hôte d'API détaché ne savait pas résoudre `IHubContext<PlanningHub>` (ni `AddSignalR()` ni `MapHub`) → l'écriture distante échouait à l'activation. Un bUnit à doublures n'aurait vu ni l'un ni l'autre.

**Test de NIVEAU RUNTIME** sur l'app réellement câblée (front exécuté côté navigateur,
**hôte d'API détaché** démarré à `https://api.planning.local`, front configuré pour émettre ses
écritures vers cette URL d'API distante) :
- **Given** l'hôte d'API est démarré **seul** à `https://api.planning.local` ; le front s'exécute
  **dans le navigateur** (WASM) et est **configuré** pour émettre ses écritures vers
  `https://api.planning.local` ; le foyer connaît le lieu « école » ; aucun slot pour le mercredi
  24/06/2026 ;
- **When** le front émet, **vers l'API distante**, une pose de slot pour l'enfant « Léa » au lieu
  « école », le mercredi 24/06/2026 de 08:30 à 16:30 ;
- **Then** l'API distante **confirme l'effet** par une réponse de succès ; **et** dans la grille
  projetée à la semaine du lundi 22/06/2026, la case du mercredi 24/06/2026 porte un slot « école »
  positionné de 08:30 à 16:30 — l'écriture ayant **réellement transité** par l'API distante jusqu'au
  store réel lu par `GrilleAgendaQuery` (pas un accusé du canal ni une grille statique).

## Tests

> Détail RED→GREEN piloté par `ihm-builder` (migration WASM, config URL d'API, émission HTTP
> distante, rendu de la case). L'acceptation runtime ci-dessus est la **boucle externe**. La table
> de tests unitaires backend est **sans objet** ici (les règles métier pose/projection sont déjà
> vertes ; ce scénario prouve le **câblage runtime WASM → API distante**).

## Fichiers à créer / modifier

- **Migration WASM** du front (`PlanningDeGarde.Web` → WebAssembly, ou nouveau projet `.Client`) :
  render mode interactif WASM, exécution navigateur.
- **Config URL d'API** : `HttpClient.BaseAddress` = URL d'API **configurable** (appsettings /
  config WASM), plus `nav.BaseUri`.
- Câblage de la vue `PoserSlot` (et de son `HttpClient`) vers l'API **distante**.
- Hôte d'API détaché `PlanningDeGarde.Api` (créé au Sc.1) démarré comme cible distante.

## Design notes

- **Anti « vert qui ment »** : l'acceptation doit échouer **comme l'utilisateur la voit** si le
  render mode WASM manque, si l'URL d'API est mal configurée, ou si le HTTP distant échoue. Donc
  app réellement câblée (DI réelle), **pas** bUnit.
- **Observable de bout en bout** : la grille reflète un slot **réellement enregistré** via l'API
  distante (store réel lu par `GrilleAgendaQuery`), jamais un accusé du canal ni une doublure.
- **CORS** : l'origine du front doit être autorisée par l'API distante (couvert en propre par Sc.5).
- **Diffusion temps réel** : après WASM, le hub SignalR est consommé côté navigateur (point de
  câblage). L'écriture aboutie **déclenche** la diffusion ; on n'écrit jamais par le canal de
  diffusion.
- **Hors périmètre** : aucune file/rejeu (PWA reportée). Ce scénario couvre le **chemin nominal
  joignable** ; l'échec injoignable est le Sc.6.
