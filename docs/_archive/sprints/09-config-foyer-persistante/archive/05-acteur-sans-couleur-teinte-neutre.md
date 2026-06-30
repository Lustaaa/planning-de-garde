# Sc.5 — Un acteur ajouté sans couleur retombe sur la teinte neutre

`@limite`

↩ Retour : [00-sprint09-suivi.md](00-sprint09-suivi.md)

**Routage** : **caractérisation backend** (⚠️ early green attendu, **garanti par le contrat du
port** `IPaletteCouleurs.CouleurDe`) **+** runtime IHM. Un acteur ajouté **sans couleur** n'a
**aucune** couleur enregistrée → `CouleurDe(idNeuf)` retombe sur `CouleurNeutre` (gris) **par
contrat**, son nom étant conservé (Sc.1 enregistre le nom).

## Acceptation (BDD) — niveau **runtime/IHM** (routé `ihm-builder`)

> Un parent ajoute « Papy Jo » **sans** choisir de couleur, puis lui affecte la garde de Léa le 10
> juin ; **sans rechargement**, la case du 10 juin affiche « Papy Jo » en **gris**, la légende
> affiche « Papy Jo » en **gris**, et le nom « Papy Jo » est conservé. Sur l'app réellement câblée.

`Should_Afficher_Papy_Jo_en_gris_dans_la_case_du_10_juin_et_dans_la_legende_en_conservant_le_nom_When_un_acteur_est_ajoute_sans_couleur_puis_affecte_a_une_periode` — ⏳ Pending

## Tests unitaires backend (boucle interne, `tdd-auto`)

| # | Test unitaire (FLFI) | TPP | Contradiction | Status |
|---|----------------------|-----|---------------|--------|
| 1 | `Should_Resoudre_la_teinte_neutre_pour_l_acteur_ajoute_en_conservant_son_nom_When_il_a_ete_ajoute_sans_couleur_puis_affecte_a_une_periode` | composition (contrat du port) | ⚠️ **probablement early green — garanti par le contrat `IPaletteCouleurs.CouleurDe` (clé absente → `CouleurNeutre`) + Sc.1 (nom enregistré) (caractérisation, pas driver)**. Leçon s03 (Sc.8 « repli gris ») : le port renvoie **déjà** le neutre sur clé absente — l'ajout sans couleur **n'enregistre rien** côté couleur, donc le gris tombe **sans code neuf**. Le nom reste résolu par `NomDe(idNeuf)`. `tdd-auto` marquera ✅ GREEN (caractérisation). | ✅ GREEN (caractérisation) |

> **Pourquoi une caractérisation** — Le contrat du port garantit le repli neutre ; vérifier le
> **CONTRAT des ports déjà introduits avant de prédire une contradiction** (méthodo). Aucun rouge :
> filet documentant le `@limite` « sans couleur → gris, nom conservé ».

## Fichiers à créer / modifier

- **Backend** : néant de neuf. Test de caractérisation composant `AjouterActeurHandler` (Sc.1, sans
  couleur) + `GrilleAgendaQuery` ; couleur résolue par le repli `CouleurNeutre` existant.
- **Doublures tests** — `FakeConfigurationFoyer` (`CouleurDe` renvoie déjà `Neutre` sur clé absente,
  miroir du contrat réel) + `FakePeriodeRepository`.
- **Volet runtime IHM (routé `ihm-builder`)** — case + légende grises sur l'app câblée.

## Design notes

- **Repli neutre = contrat, pas calcul.** `IPaletteCouleurs.CouleurDe` renvoie `CouleurNeutre` pour
  toute clé absente ; l'ajout sans couleur laisse l'id absent du set couleur → gris **par
  construction**. Surface identique côté `FakeConfigurationFoyer` et adaptateur réel.
- **Nom et couleur indépendants** (acquis s08) : ajouter sans couleur n'efface pas le nom.
