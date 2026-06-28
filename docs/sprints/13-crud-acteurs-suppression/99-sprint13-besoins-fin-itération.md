# Besoins fin d'itération — sprint 13 (CRUD acteurs · suppression)

> Sortie de `/4-retours`. Réamorce `/2-make-gherkin` sur **un** sujet prioritaire.
> Le détail de conception (scénarios, découpe) appartient au make-gherkin, pas ici.

## Contexte de clôture

Le sprint 13 a refermé le **palier 8 — CRUD acteurs, tranche suppression** :
suppression autorisée (règle 6), neutralisation par repli des cases orphelines
(surcharge orpheline → fond → neutre, sans nom fantôme), accusé non bloquant
« Acteur supprimé », acceptation runtime sur store **Mongo réel**. Livraison
**verte 9/9 scénarios** · suite complète **196/196** (Docker actif). Gate G3
validé sans dépôt de retour produit.

**Retours produit (PO) : VIDE** — les 7 sous-sections `## IHM - ...` et `## Tech`
de `99-sprint13-retours.md` sont des placeholders ; aucune contrainte Tech.
C'est le **chemin nominal** (goal 9/9 atteint), pas une anomalie → pilotage au
catalogue `docs/BACKLOG.md`. **Aucun bug** : pas de réparation à ordonner (rien à
confronter à HEAD, livraison verte).

## Décision G2 (PO) — cap du prochain sprint

**Prochain sujet make-gherkin = Impersonation bornée** (rang +2 backlog, É10+É2).
Tranché par le PO en porte G2.

## Prochain sujet (réamorce `/2-make-gherkin`)

### Impersonation bornée

- **Quoi.** L'admin / parent configurateur **incarne un acteur** du foyer
  (convenance d'administration) — voir et agir « comme » cet acteur depuis l'app.
- **Borne dure (NON négociable).** **PAS l'authentification réelle** du palier 13
  (OAuth, landing, comptes, sessions, prise en main par demande, droits par rôle
  après prise en main restent au palier 13). L'impersonation ici est un confort
  d'administration **avant** l'auth, bornée à incarner un acteur déjà déclaré.
- **Pourquoi maintenant.** Suite **directe** de la suppression : ferme la boucle du
  cycle de vie des acteurs (C/R/U/D livrés → impersonation = dernier maillon de la
  tranche acteurs) et **amorce É10** sans tirer l'auth réelle en avant. Continuité
  de contexte maximale : on reste dans le modèle d'acteurs et l'écran de config
  juste touchés au sprint 13.
- **Épics.** É10 (authentification & accès — amorce), É2 (modèle & configuration
  d'acteurs).
- **Make-gherkin séparé** déjà acté à la clôture s12 (tranche distincte de la
  suppression).
- **À cadrer au make-gherkin (pas tranché ici) :** périmètre exact de
  l'impersonation (lecture seule vs écriture « au nom de »), sortie/retour à
  l'identité réelle, ce que l'impersonation **n'ouvre pas** (la frontière avec
  l'auth réelle du palier 13). Borne anti-cliquet : pas de persistance neuve
  tirée en avant (config foyer durable acquise au palier 5, reste du domaine
  InMemory).

## Règle d'arbitrage

**L'usage tranche** (arbitre constant du backlog). Quand deux besoins s'opposent,
le sujet d'usage le plus proche du cycle de vie acteurs déjà ouvert gagne sur les
sujets techniques (séquencés derrière l'usage) et sur les sujets d'usage plus
éloignés. Corollaire de borne anti-cliquet : **aucune persistance ni auth réelle
n'est remontée devant l'usage** par effet de bord de l'impersonation.

## Séquence (le reste, derrière le prochain sujet)

1. **[Prochain] Impersonation bornée** — make-gherkin immédiat (ci-dessus).
2. **Durcissement du gating config (règle 9)** — *cadrage adjacent / candidat de
   l'impersonation*. Angle mort signalé par l'IA au sprint 13 (Sc.7) : l'écran
   `ConfigurationFoyer` ne gate Invité **que** le bouton supprimer ; ajout /
   édition / cycle de la config restent ouverts à un Invité. Décider si **toutes**
   les écritures config doivent être gatées (cohérence règle 9). Naturellement
   embarquable dans le cadrage impersonation (même écran, même notion de
   rôle/identité) — à confirmer au make-gherkin. Décision CP/métier, **pas** un
   retour produit PO.
3. **Rétrofit complet du garde déterministe *TempsReel* SignalR** — *dette
   technique séquencée* (P2 backlog). Le sprint 13 a posé le garde `WaitForState`
   sur les 7 tests *TempsReel* touchés ; le rétrofit de **tous** les tests
   config/grille reste à faire. **Prérequis de l'édition concurrente** (P3). Pas
   un bug `src/` (dette de test). Ne bloque pas l'impersonation.
4. **Calendrier navigable + sélection de plage** (rang +3, palier 9, É4+É7) —
   sujet d'usage suivant, après la tranche acteurs. Besoin ancien (retours
   s02/s03).

## Risques / points de vigilance

- **Frontière impersonation ↔ auth réelle.** Risque de scope creep vers le
  palier 13. Tenir la borne : incarner un acteur déjà déclaré, sans OAuth /
  comptes / sessions / prise en main. À surveiller au make-gherkin (candidat G1
  si un vrai trou métier apparaît).
- **Couplage É2 ↔ É10.** La création d'acteur avec email = amorce du compte
  (idée PO s08) ; l'impersonation l'anticipe partiellement. Expliciter au
  make-gherkin ce qui est dans la tranche vs reporté au palier 13.
- **Fondation SignalR flaky (P2).** Si l'impersonation touche la diffusion temps
  réel (propagation de l'identité incarnée), valider en **acceptation runtime /
  G3**, ne pas en faire un filet de régression automatisé instable.
- **Aucun bug en attente.** Livraison 9/9 verte ; rien à réparer en `/3` ciblé.
