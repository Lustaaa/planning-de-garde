# Sprint 41 — Commandes inverses actif/admin — débloquer le sens OFF du toggle (dette s33)

> **Goal G2 (tranché PO — délégation, goal 1 du SM)** : **débloquer le sens OFF** du toggle
> actif/admin de la modal Acteurs, verrouillé en sens ON depuis s33 faute de commandes montantes
> inverses. Une tranche verticale **back d'abord** puis IHM :
> - **@back — dé-désigner un admin.** Commande/handler **Domain pur** (agrégat
>   `AdministrationFoyer` s22) qui retire la désignation d'admin d'un acteur : **no-op idempotent**
>   si l'acteur n'est **déjà pas** admin, **acteur inconnu refusé** sans mutation, **borne « dernier
>   admin »** (cf. point de vigilance).
> - **@back — désactiver un compte.** Commande/handler `Actif → Inactif` **réutilisant
>   `IEditeurComptes`** (s22, aucun store neuf) : **no-op idempotent** si déjà Inactif, **compte
>   inconnu refusé** sans mutation, persisté sur les **deux adaptateurs** (InMemory + Mongo durable).
> - **@ihm — toggle bi-directionnel.** Le toggle actif/admin de la modal Acteurs devient
>   **actionnable dans les deux sens** : le **OFF émet la vraie commande inverse** (fin du verrou ON
>   s33). **PAS de no-op silencieux** = anti-vert-qui-ment (proscrit s33). Refus (dernier admin,
>   compte inconnu) → **modal reste ouverte + motif dedans + saisie conservée** ; **Échap = Annuler**
>   (port `IEcouteurEchapModal` s33). **Parent-gated** + convergence **SignalR** (2ᵉ écran reflète
>   actif/admin sans rechargement).
>
> **Ordre d'attaque (backend d'abord, CLAUDE.md)** : @back (dé-désigner admin + borne dernier admin
> + désactiver compte, deux adaptateurs) → puis @ihm (toggle OFF bi-directionnel + contrat d'erreur
> + gating + SignalR).
>
> **HORS scope explicite (périmètre resserré, décision SM)** :
> - **Suppression d'un acteur / d'un compte** (retrait du store) : **≠ désactivation** (`Actif →
>   Inactif` conserve le compte). Le Delete acteur/compte existant (s18/s22) reste **inchangé**, hors
>   goal.
> - **Inscription libre-service / prise en main de compte** par l'utilisateur réel : hors goal
>   (épic 10, backlog).
> - **Refonte du gating impersonation R8/R9** : **préservé, NON modifié** — dé-désigner un admin /
>   désactiver un compte ne change **ni** la mécanique d'identité effective **ni** les droits
>   d'écriture dérivés du `TypeActeur` (cantonné R8/R9 depuis s36).
> - **Édition inline au clic** (tension s32, à trancher en G2) : hors goal.

## Avancement — 3/6

| # | Scénario | Type | Statut |
|--:|----------|------|:------:|
| 1 | **Dé-désigner un admin** — commande/handler Domain pur (`AdministrationFoyer` s22) : retire la désignation ; **no-op idempotent** si déjà non-admin ; **acteur inconnu refusé** sans mutation ; deux adaptateurs | back | ✅ |
| 2 | **Borne « dernier admin » (limite, tranchée)** — dé-désigner le **DERNIER** admin du foyer est **REFUSÉ AVANT écriture** (motif clair, store intact) : le foyer ne se retrouve **jamais sans admin** (cohérent invariant admin=Parent s22) | back | ✅ |
| 3 | **Désactiver un compte** — commande/handler `Actif → Inactif` réutilisant `IEditeurComptes` (aucun store neuf) : **no-op idempotent** si déjà Inactif ; **compte inconnu refusé** sans mutation ; persisté deux adaptateurs (InMemory + Mongo durable) | back | ✅ |
| 4 | **Toggle OFF bi-directionnel** — le OFF du toggle admin / actif de la modal Acteurs émet la **vraie commande inverse** (dé-désigner admin / désactiver compte) via le canal HTTP ; **fin du verrou ON s33** ; **PAS de no-op silencieux** (anti-vert-qui-ment) | 🖥️ IHM | ⏳ |
| 5 | **Contrat d'erreur du OFF** — refus (dernier admin, compte inconnu, API injoignable) → **modal RESTE OUVERTE + motif dedans + saisie/toggles CONSERVÉS**, aucune écriture partielle ; **Échap = Annuler** sans mutation (port `IEcouteurEchapModal` s33) | 🖥️ IHM | ⏳ |
| 6 | **Parent-gated + convergence SignalR** — le OFF est gaté sur l'identité effective (Invité = pas de toggle actionnable) ; un 2ᵉ écran reflète le nouvel état actif/admin **sans rechargement** (diffusion lecture seule s20) | 🖥️ IHM | ⏳ |

> **⚠️ Point de vigilance — borne « dernier admin » TRANCHÉE (Sc.2, décision SM).** L'invariant s22
> pose que **l'admin du foyer est obligatoirement un acteur de type Parent** (`AdministrationFoyer.
> DesignerAdmin` refuse un non-Parent) et que le **cardinal des admins n'est pas borné** (les deux
> parents peuvent être admins). Le sens OFF introduit un risque neuf : **vider le foyer de tout
> admin**. **Décision : dé-désigner le DERNIER admin est REFUSÉ AVANT écriture** (motif clair, store
> intact) — un foyer garde **toujours ≥1 admin**. Dé-désigner un admin **quand il en reste d'autres**
> réussit. C'est une **borne défensive neuve** (miroir du refus « dernier enfant » R1 à venir), à
> **figer noir sur blanc dans le handler**.

> **⚠️ Anti-vert-qui-ment — le OFF émet une VRAIE commande, jamais un no-op silencieux (Sc.4).** Le
> verrou s33 (toggle déjà ON rendu VERROUILLÉ) existait précisément parce qu'un OFF sans commande
> inverse aurait été un **vert-qui-ment** (proscrit s33). Ce sprint **lève le verrou en câblant la
> vraie commande** (dé-désigner admin / désactiver compte). **Interdit** : un toggle OFF qui
> paraît basculer côté UI sans émettre de commande, ou dont l'effet ne survit pas au rechargement.
> Preuve = round-trip runtime réel (Mongo durable) + gate navigateur PO.

> **⚠️ Non-régression du sens ON + du gating (Sc.1-6).** Le sens ON existant reste **strictement
> inchangé** : désigner un admin (`DesignerAdmin` s22, invariant Parent), activer un compte
> (`ActiverCompte` s24), toggle actionnable actif seulement si l'acteur **porte un compte** (s33).
> On **AJOUTE** le sens OFF, on ne réécrit pas le sens ON. Le **gating impersonation R8/R9** (droit
> d'écriture dérivé de l'identité effective, `TypeActeur` cantonné R8/R9 depuis s36) est **préservé,
> non modifié** — dé-désigner / désactiver ne fait gagner ni perdre aucun droit d'écriture.

---

## Scénarios

### Sc.1 — Dé-désigner un admin (commande/handler Domain pur) @back @vert
```gherkin
Étant donné un foyer où plusieurs acteurs-Parents sont désignés admins (agrégat AdministrationFoyer s22)
Quand j'émets la commande de dé-désignation d'admin ciblant un de ces acteurs par son id stable
Alors sa désignation d'admin est retirée (mutation Domain pure sur AdministrationFoyer)
Et l'invariant s22 « l'admin est un acteur de type Parent » n'est ni contourné ni régressé (le sens ON inchangé)
Et re-émettre la commande sur un acteur DÉJÀ non-admin est un NO-OP idempotent qui RÉUSSIT (aucune mutation, aucun doublon d'écriture)
Et cibler un acteur INCONNU du référentiel est REFUSÉ sans mutation (motif restitué, store intact)
Et le comportement est identique sur les DEUX adaptateurs (InMemory seedé ET Mongo durable)
```

### Sc.2 — Borne « dernier admin » : refus AVANT écriture (limite) @back @vert
```gherkin
Étant donné un foyer où DEUX acteurs-Parents sont désignés admins
Quand je dé-désigne l'un des deux
Alors la dé-désignation RÉUSSIT (il reste au moins un admin)

Étant donné un foyer où UN SEUL acteur est désigné admin (le dernier)
Quand j'émets la dé-désignation de ce dernier admin
Alors la commande est REFUSÉE AVANT toute écriture (motif clair « le foyer doit garder au moins un admin »)
Et le store reste INTACT (l'acteur demeure admin, aucune mutation partielle)
Et le foyer ne se retrouve JAMAIS sans admin (borne défensive neuve, cohérente avec l'invariant admin=Parent s22)
Et cette borne est vérifiée à l'identique sur les DEUX adaptateurs (InMemory ET Mongo durable)
```

### Sc.3 — Désactiver un compte (Actif → Inactif, réutilise IEditeurComptes) @back @vert
```gherkin
Étant donné un CompteUtilisateur au statut Actif (s22/s24), persisté dans la config foyer
Quand j'émets la commande de désactivation ciblant ce compte par son id stable opaque
Alors son statut passe Actif → Inactif via l'agrégat CompteUtilisateur (mutation Domain pure)
Et la mutation réutilise le port d'écriture IEditeurComptes existant (s22) — AUCUN store neuf, aucun agrégat neuf
Et re-émettre la commande sur un compte DÉJÀ Inactif est un NO-OP idempotent qui RÉUSSIT (miroir des suppressions idempotentes s16/s18)
Et cibler un compte INCONNU est REFUSÉ sans mutation (motif restitué, store intact)
Et le nouveau statut Inactif est PERSISTÉ durablement (round-trip survivant au rechargement) sur les DEUX adaptateurs (InMemory ET Mongo durable)
Et un compte redevenu Inactif refuse la connexion (garde s23 « compte non activé »), non-régression du sens ON (activer, s24) préservée
```

### Sc.4 — Toggle OFF bi-directionnel émet la vraie commande inverse (fin du verrou s33) @ihm @pending
```gherkin
Étant donné la modal d'édition d'un acteur (patron crayon → modal s32/s33), ouverte en tant que Parent
Et un acteur actuellement désigné admin, dont le toggle « admin » était VERROUILLÉ en ON (s33)
Quand je bascule le toggle « admin » sur OFF puis « Enregistrer »
Alors la commande de DÉ-DÉSIGNATION d'admin est émise via le canal HTTP (Sc.1), PAS un no-op silencieux
Et en succès la modal se ferme, le tableau est relu, l'état admin de la ligne reflète OFF sans rechargement
Et l'effet SURVIT au rechargement (round-trip Mongo durable, anti-vert-qui-ment)

Étant donné un acteur portant un compte au statut Actif, toggle « actif » verrouillé ON (s33)
Quand je bascule le toggle « actif » sur OFF puis « Enregistrer »
Alors la commande de DÉSACTIVATION de compte est émise via le canal HTTP (Sc.3), PAS un no-op silencieux
Et le verrou ON s33 est LEVÉ pour les deux toggles (le sens OFF est désormais actionnable)
```

### Sc.5 — Contrat d'erreur du OFF : modal ouverte, motif, saisie conservée, Échap=Annuler @ihm @pending
```gherkin
Étant donné la modal d'édition d'un acteur qui est le DERNIER admin du foyer, ouverte en tant que Parent
Quand je bascule le toggle « admin » sur OFF puis « Enregistrer »
Alors le refus domaine (borne dernier admin, Sc.2) revient : la modal RESTE OUVERTE
Et le motif est affiché DEDANS (« le foyer doit garder au moins un admin »)
Et la saisie et l'état des toggles sont CONSERVÉS, AUCUNE écriture partielle ne touche le tableau
Et il en va de même si l'API est injoignable ou le compte inconnu (motif dedans, saisie conservée)
Et « Échap » ferme la modal STRICTEMENT comme « Annuler » : aucune commande émise, aucune mutation (port IEcouteurEchapModal s33)
```

### Sc.6 — Parent-gated + convergence SignalR du sens OFF @ihm @pending
```gherkin
Étant donné une identité EFFECTIVE non-Parent (Invité)
Quand j'ouvre l'onglet Acteurs de la Config du foyer
Alors le tableau reste en lecture seule, aucun crayon ni toggle actionnable atteignable (gating s14/s20/s33 préservé, R8/R9 non modifié)

Étant donné deux écrans Config foyer ouverts, la ligne d'un acteur admin/actif rendue sur les deux
Quand je dé-désigne son admin (ou désactive son compte) via la modal du 1ᵉʳ écran
Alors la ligne du 2ᵉ écran CONVERGE sur le nouvel état actif/admin SANS rechargement
Et la convergence passe par le canal SignalR de LECTURE SEULE (aucune écriture par la diffusion, s20 préservé)
```

---

# Retours produit (PO)
