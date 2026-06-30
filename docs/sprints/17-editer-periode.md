# Sprint 17 — Éditer une période depuis la dialog (`editer-periode`)

> **Avancement : 4/11 ⏳**

| # | Scénario | Type | Statut |
|--:|----------|:----:|:------:|
| 1 | Re-borner une période la met à jour dans le store durable relu (+ redémarrage) | @back | ✅ |
| 2 | Réaffecter le responsable : la case affiche le nouveau, plus l'ancien | @back | ✅ |
| 3 | Re-bornage : la portion libérée retombe sur le responsable de fond (le cycle reprend) | @back | ✅ |
| 4 | Re-bornage : portion libérée sur index non mappé → neutre sans nom fantôme | @back | ✅ |
| 5 | Bornes invalides (fin ≤ début) refusées, période inchangée | @back | ⏳ |
| 6 | Édition concurrente sur état périmé → rejet, rien appliqué | @back | ⏳ |
| 7 | « Éditer » dans la dialog → formulaire pré-rempli → enregistrer → grille relue + accusé | 🖥️ @ihm | ⏳ |
| 8 | Annulation : fermer le formulaire sans enregistrer ne change rien | 🖥️ @ihm | ⏳ |
| 9 | Gating Invité : aucun bouton « Éditer » ni commande émissible | 🖥️ @ihm | ⏳ |
| 10 | API injoignable : la dialog reste ouverte, message d'échec, rien n'est appliqué | 🖥️ @ihm | ⏳ |
| 11 | Temps réel : l'édition propage grille + légende sans rechargement | 🖥️ @ihm | ⏳ |

---

> Sujet `/2-make-gherkin` = `editer-periode` (épics É7 + É12), goal tranché **G2 (PO)**. **Suite
> directe de s16** : la dialog listant les périodes d'une date (`PeriodesDuJourQuery`) et le menu
> clic-case sont déjà livrés ; on **ajoute un 5ᵉ usage** (bouton « Éditer » par ligne), pas un écran
> dédié. Referme la **moitié restante** de la dette « édition/suppression de période depuis l'IHM »
> (la suppression a été livrée s16). Store du domaine **durable (Mongo, s15)** → **acceptation
> runtime obligatoire**.
>
> **Décisions CP (déterministes).** Périmètre = **backend d'abord, IHM en fin**. Le chemin
> d'écriture réutilise les **dialogs en contexte** (s11/s12/s16). Édition = **re-borner** (début
> et/ou fin) **et/ou réaffecter** le responsable d'une période **existante**, clé = **identifiant
> stable** (jamais un libellé). Re-résolution après ré-bornage = priorité **surcharge > fond >
> neutre** déjà acquise (palier 6) : la **portion libérée** retombe sur le fond (ou neutre **sans
> nom fantôme**), la **portion couverte** affiche le nouveau responsable. Accusé **non bloquant
> « Période modifiée »** à part (registre avertissement-à-part R16). **Hors scope** : suppression de
> slot/transfert (s16 a couvert la suppression de période) ; édition concurrente sous **dialog
> ouverte** simultanée (P3, derrière la stabilisation des flakes SignalR P2) ; cycle de fond riche.
>
> **Décision CP — concurrence (chapeau DÉCISION, journalisée).** Édition d'une période sur **état
> périmé** ⇒ **REJET sur état périmé** (et non last-write-wins). Rationale : l'**agrégat période**
> porte **déjà** ce comportement (Sc.10 s01, épic É7 ✅ livré) ; le **last-write-wins de R11** vise
> le **mapping du cycle de fond** (autre agrégat, écriture de remplacement idempotente), **pas**
> l'édition d'une période existante. Craft DDD : **un seul modèle de concurrence par agrégat** →
> on réutilise le contrôle de version/état déjà en place, **aucune règle neuve**, rien appliqué en
> cas de rejet. Échec clair surfacé dans la dialog (registre R28). *Sources : `docs/specs/
> periodes-et-cycle-de-fond.md` (R11/R12/R14/R15), backlog É7 « Édition concurrente — rejet sur
> état périmé ✅ s01 », `CLAUDE.md` (DDD/CQRS).*

**Feature: Éditer une période de garde depuis le planning.** Depuis la **dialog** listant les
périodes couvrant une date (4ᵉ usage menu clic-case, s16), un **Parent / Admin** ouvre un
formulaire d'**édition** pré-rempli (bornes + responsable), **re-borne** la période et/ou
**réaffecte** son responsable. L'enregistrement met à jour le **store durable** et fait
**re-résoudre** les cases concernées : la portion **libérée** retombe sur le **fond** (ou le
**neutre**, sans nom fantôme), la portion **couverte** affiche le **nouveau** responsable. La
grille reste en **lecture seule** (rétroaction par store relu + diffusion temps réel). L'édition
est **gatée** (Invité interdit), **robuste à l'échec** (API injoignable → dialog ouverte, rien
appliqué) et **rejetée sur état périmé** (concurrence).

## Analyse technique

Légère côté **résolution** (aucune règle neuve : priorité surcharge>fond>neutre acquise palier 6,
durabilité s15, contrôle de concurrence de l'agrégat période acquis s01). Les RED neufs sont la
**mutation** d'une période existante (re-borner / réaffecter) et son **endpoint canal**.

- **Application — commande neuve.** `EditerPeriodeCommand(string PeriodeId, DateOnly Debut, DateOnly
  Fin, string ResponsableId, version/état attendu)` → `EditerPeriodeHandler` renvoyant un `Result`
  succès/échec. **Rejet** si bornes invalides (fin ≤ début), responsable requis manquant, **ou état
  périmé** (concurrence). Clé = **identifiant stable**, jamais un libellé.
- **Port d'écriture.** Méthode `Editer(...)` (ou `Remplacer`) sur le dépôt de périodes existant,
  miroir d'`Affecter`/`Supprimer` (s16). Réalisée **InMemory** **ET** `AdapterDroite.Mongo`.
- **Re-résolution.** Réutilise `GrilleAgendaQuery` + la priorité surcharge>fond>neutre (palier 6) et
  le filtre d'existence `Resolvable()` (s13) pour le repli sans nom fantôme. **Aucune** logique de
  résolution neuve.
- **Api (adaptateur gauche).** Endpoint canal `POST /api/canal/editer-periode`
  (`EditerPeriodeRequete(...)`), même convention succès/échec que les autres écritures ; sur succès,
  déclenche la **diffusion temps réel**.
- **CQRS préservé.** Write par le canal requête/réponse ; lecture (`PeriodesDuJourQuery`, s16) +
  diffusion SignalR lecture seule à part — **jamais d'écriture par la diffusion**, grille en lecture
  seule (R14).
- **Web (IHM, lot final `ihm-builder`).** **5ᵉ usage** : bouton **« Éditer »** par ligne de
  `SupprimerPeriodeDialog` (ou dialog d'édition dédiée réutilisant le pattern) → formulaire
  **pré-rempli** (bornes + sélecteur responsable alimenté par les acteurs persistés) → enregistrer.
  Accusé **« Période modifiée »** à part + **gating Invité** (R9) + **échec API** (R28 : dialog reste
  ouverte, message clair, rien appliqué) + **annulation** sans écriture.
- **Bornes anti-cliquet.** Aucune persistance neuve tirée : l'édition **exerce** la durabilité
  acquise (s09/s15). Respecter la convention anti-flake sur les tests *TempsReel* (rétrofit complet
  = P2, **hors scope**).
- **Tests.** `PlanningDeGarde.Tests` + `Api.Tests` pour les drivers backend (handler + re-résolution
  après ré-bornage + rejets bornes/concurrence), **prouvés au runtime sur store Mongo réel**. `Web.
  Tests` (bUnit) pour le lot IHM final.

### Matrice de couverture

- **Nominal** : Sc.1 (re-borner persisté, store relu) · Sc.2 (réaffecter, case relue) · Sc.7 (dialog → formulaire → grille relue + accusé).
- **Limite** : Sc.3 (portion libérée → fond) · Sc.4 (portion libérée → neutre sans nom fantôme) · Sc.8 (annulation) · Sc.11 (temps réel).
- **Erreur** : Sc.5 (bornes invalides) · Sc.6 (concurrence, rejet sur état périmé) · Sc.9 (gating Invité, R9) · Sc.10 (API injoignable, R28).

## Scénarios

11 scénarios. **Drivers backend `@back`** (1→6) = RED neuf à la frontière Application, prouvés au
runtime sur store Mongo réel. **Lot IHM `@ihm`** (7→11) = mené en RED→GREEN runtime par
l'`ihm-builder` (front WASM + API distante + Mongo réel). Chaque scénario est **autonome** (son
`Given` complet, **pas de `Background`**).

> **Garde de cohérence date↔index appliquée.** Mardi 16 juin 2026 = **semaine ISO 25** ; toute la
> semaine **lundi 15 → dimanche 21 juin 2026 = ISO 25**. Pour un cycle **N=2**, `25 % 2 = 1` →
> **index 1**. Les scénarios fond (Sc.3) et neutre (Sc.4) ancrent leur attendu sur cet index 1.

### Scenario 1 — Re-borner une période la met à jour dans le store durable relu `@back` `@vert`

```gherkin
Scenario: Re-borner une période existante persiste les nouvelles bornes (store relu et après redémarrage)
  Étant donné un foyer dont le store durable comporte les acteurs "Parent A" et "Nounou"
  Et une période durable attribue du lundi 15 au mercredi 17 juin 2026 la garde à "Nounou", d'identifiant stable connu
  Quand je re-borne cette période par son identifiant stable pour qu'elle couvre du mardi 16 au mercredi 17 juin 2026
  Alors l'édition réussit
  Et le store relu comporte la période avec les bornes mardi 16 → mercredi 17 juin 2026
  Et le lundi 15 juin 2026 n'est plus couvert par cette période
  Et le store relu après redémarrage comporte toujours les bornes mardi 16 → mercredi 17 juin 2026
```

### Scenario 2 — Réaffecter le responsable : la case affiche le nouveau, plus l'ancien `@back` `@vert`

```gherkin
Scenario: Réaffecter le responsable d'une période fait afficher le nouveau responsable dans la case
  Étant donné un foyer dont le store durable comporte les acteurs "Parent A" et "Nounou"
  Et une période durable attribue le mardi 16 juin 2026 la garde à "Nounou", d'identifiant stable connu
  Et la case du mardi 16 juin 2026 affiche "Nounou"
  Quand je réaffecte cette période à "Parent A" par son identifiant stable
  Alors l'édition réussit
  Et la case du mardi 16 juin 2026 affiche "Parent A" et sa couleur
  Et la case du mardi 16 juin 2026 n'affiche plus "Nounou"
```

### Scenario 3 — Re-bornage : la portion libérée retombe sur le responsable de fond `@back` `@vert`

```gherkin
Scenario: Re-borner une période libère une journée qui retombe sur le responsable de fond
  Étant donné un foyer dont le store durable comporte les acteurs "Parent A" et "Nounou"
  Et un cycle de fond de 2 semaines mappant l'index 1 sur "Parent A"
  Et une période durable attribue du lundi 15 au mercredi 17 juin 2026 la garde à "Nounou" (surcharge sur le fond)
  Et la case du lundi 15 juin 2026 (semaine ISO 25, index 1) affiche "Nounou"
  Quand je re-borne la période pour qu'elle couvre du mardi 16 au mercredi 17 juin 2026
  Alors l'édition réussit
  Et la surcharge du lundi 15 juin 2026 cesse de primer
  Et la case du lundi 15 juin 2026 retombe sur le responsable de fond "Parent A"
  Et la case du lundi 15 juin 2026 affiche "Parent A" et sa couleur
```

### Scenario 4 — Re-bornage : portion libérée sur index non mappé → neutre sans nom fantôme `@back` `@vert`

```gherkin
Scenario: Re-borner une période libère une journée sur un index de fond non mappé qui retombe sur le neutre
  Étant donné un foyer dont le store durable comporte les acteurs "Parent A" et "Nounou"
  Et un cycle de fond de 2 semaines mappant l'index 0 sur "Parent A" et laissant l'index 1 non mappé
  Et une période durable attribue du lundi 15 au mercredi 17 juin 2026 la garde à "Nounou"
  Et la case du lundi 15 juin 2026 (semaine ISO 25, index 1) affiche "Nounou"
  Quand je re-borne la période pour qu'elle couvre du mardi 16 au mercredi 17 juin 2026
  Alors l'édition réussit
  Et l'index 1 du cycle n'étant ni mappé ni résolu, la case du lundi 15 juin 2026 retombe sur la teinte neutre
  Et la case du lundi 15 juin 2026 n'affiche aucun nom de responsable
```

### Scenario 5 — Bornes invalides (fin ≤ début) refusées, période inchangée `@back` `@pending`

```gherkin
Scenario: Éditer une période avec une fin antérieure ou égale au début est refusé sans rien changer
  Étant donné un foyer dont le store durable comporte les acteurs "Parent A" et "Nounou"
  Et une période durable attribue du lundi 15 au mercredi 17 juin 2026 la garde à "Nounou", d'identifiant stable connu
  Quand je tente de re-borner cette période pour qu'elle finisse avant son début (fin = dimanche 14 juin 2026)
  Alors l'édition est refusée avec un message clair sur les bornes
  Et le store relu comporte toujours la période d'origine du lundi 15 au mercredi 17 juin 2026
```

### Scenario 6 — Édition concurrente sur état périmé → rejet, rien appliqué `@back` `@pending`

> Décision CP : **rejet sur état périmé** (agrégat période, Sc.10 s01), **pas** last-write-wins.

```gherkin
Scenario: Une seconde édition fondée sur un état périmé est rejetée sans rien appliquer
  Étant donné un foyer dont le store durable comporte les acteurs "Parent A" et "Nounou"
  Et une période durable attribue le mardi 16 juin 2026 la garde à "Nounou", à l'état (version) connu
  Et une première édition réaffecte cette période à "Parent A" et aboutit
  Quand une seconde édition tente de re-borner la même période en se fondant sur l'état (version) initial désormais périmé
  Alors la seconde édition est rejetée pour état périmé
  Et le store relu reflète uniquement la première édition (responsable "Parent A")
  Et la seconde édition n'a rien appliqué
```

### Scenario 7 — « Éditer » dans la dialog → formulaire pré-rempli → enregistrer → grille relue + accusé `@ihm` `@pending`

> Lot IHM final (`ihm-builder`), mené en RED→GREEN runtime ; groupable avec Sc.8–Sc.11.
> Acceptation runtime : front WASM + API distante + Mongo réel.

```gherkin
Scenario: Éditer une période depuis sa dialog met à jour la grille avec un accusé non bloquant
  Étant donné le planning affiché pour un Parent
  Et une période attribue le mardi 16 juin 2026 à "Nounou", surfacée dans la case et la légende
  Quand j'ouvre le menu de la case du mardi 16 juin 2026 et choisis l'édition de la période de "Nounou"
  Alors un formulaire d'édition s'ouvre, pré-rempli avec les bornes et le responsable "Nounou"
  Quand je réaffecte la période à "Parent A" et j'enregistre
  Alors un accusé "Période modifiée" s'affiche à part, sans bloquer
  Et la case du mardi 16 juin 2026 affiche "Parent A" et sa couleur
  Et la légende dédoublonnée ne fait plus apparaître "Nounou" si plus aucune période ne le porte
  Et la période modifiée est reflétée dans le store Mongo relu
```

### Scenario 8 — Annulation : fermer le formulaire sans enregistrer ne change rien `@ihm` `@pending`

```gherkin
Scenario: Fermer le formulaire d'édition sans enregistrer laisse période et grille inchangées
  Étant donné le planning affiché pour un Parent
  Et une période attribue le mardi 16 juin 2026 à "Nounou"
  Quand j'ouvre le formulaire d'édition de la période de "Nounou"
  Et je modifie le responsable puis je ferme sans enregistrer
  Alors aucune commande d'édition n'est émise
  Et la période de "Nounou" est inchangée
  Et la case du mardi 16 juin 2026 affiche toujours "Nounou"
```

### Scenario 9 — Gating Invité : aucun bouton « Éditer » ni commande émissible `@ihm` `@pending`

> Gating règle 9, déclencheur rôle mutualisé sur le contexte existant.

```gherkin
Scenario: En consultation seule, aucune édition de période n'est proposée
  Étant donné le planning affiché pour un Invité en consultation seule
  Et une période attribue le mardi 16 juin 2026 à "Nounou"
  Quand j'ouvre la dialog des périodes du mardi 16 juin 2026
  Alors aucun bouton "Éditer" n'est proposé sur les lignes de périodes
  Et aucune commande d'édition ne peut être émise
  Et la période de "Nounou" reste inchangée
```

### Scenario 10 — API injoignable : la dialog reste ouverte, rien n'est appliqué `@ihm` `@pending`

> Échec clair règle 28, registre acquis ; aucune mise en file ni rejeu (PWA = palier ultérieur).

```gherkin
Scenario: Une édition qui n'atteint pas l'API laisse la dialog ouverte et le planning inchangé
  Étant donné le planning affiché pour un Parent
  Et une période attribue le mardi 16 juin 2026 à "Nounou"
  Quand je réaffecte la période de "Nounou" à "Parent A" et j'enregistre
  Et la commande échoue car l'API distante est injoignable
  Alors un message d'échec clair s'affiche dans la dialog
  Et la dialog reste ouverte
  Et la période du mardi 16 juin 2026 affiche toujours "Nounou"
  Et la case et la légende du mardi 16 juin 2026 restent inchangées
  Et aucune mise en file ni rejeu n'est effectué
```

### Scenario 11 — Temps réel : l'édition propage grille et légende sans rechargement `@ihm` `@pending`

> Diffusion SignalR lecture seule, déclenchée par l'écriture aboutie. Respecter la convention
> anti-flake des tests *TempsReel* (rétrofit complet P2 hors scope).

```gherkin
Scenario: Éditer une période sur un écran rafraîchit l'autre écran sans rechargement
  Étant donné deux écrans affichant le même planning partagé, l'un piloté par un Parent
  Et une période attribue le mardi 16 juin 2026 à "Nounou"
  Quand le Parent réaffecte la période de "Nounou" à "Parent A" depuis le formulaire d'édition
  Alors le second écran voit, sans rechargement, la case du mardi 16 juin 2026 afficher "Parent A" et sa couleur
  Et le second écran voit la légende dédoublonnée ne plus faire apparaître "Nounou" si plus aucune période ne le porte
```

# Retours produit (PO)

_(à remplir au gate G3 / clôture)_
