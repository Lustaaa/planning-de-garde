namespace PlanningDeGarde.Application.Foyer.Ports;

/// <summary>
/// Port d'écriture de la configuration volatile des acteurs du foyer : renomme un acteur
/// sur son identifiant stable (l'id n'est jamais éditable — il est la clé). Miroir écriture
/// du port de lecture <see cref="IReferentielResponsables"/> ; réalisé par le store mutable
/// en Infrastructure, consommé par le handler d'édition. Dernière écriture gagne (le store
/// écrase la valeur, sans version ni rejet — préparé pour la concurrence Sc.7).
/// </summary>
public interface IEditeurConfigurationFoyer
{
    /// <summary>Ajoute un acteur <b>neuf</b> au foyer sur un identifiant stable neuf (jamais un id
    /// existant) : persiste le nom et, si fournie, la couleur (couleur absente → repli neutre par
    /// le contrat <see cref="IPaletteCouleurs"/>, aucune écriture couleur). Surface d'<b>ajout</b>
    /// distincte de l'édition (<see cref="Renommer"/> / <see cref="Recolorier"/> mutent un acteur
    /// déjà présent sur son id stable).</summary>
    void Ajouter(string acteurId, string nom, string? couleur);

    /// <summary>Affecte un nouveau nom d'affichage à l'acteur identifié de façon stable.</summary>
    void Renommer(string acteurId, string nouveauNom);

    /// <summary>Affecte une nouvelle couleur à l'acteur identifié de façon stable (surface
    /// distincte du nom — recolorier ne touche jamais le nom).</summary>
    void Recolorier(string acteurId, string nouvelleCouleur);

    /// <summary>Affecte une adresse de résidence à l'acteur identifié de façon stable (surface
    /// <b>optionnelle</b> distincte du nom et de la couleur — la changer ne touche jamais les autres
    /// surfaces). Une adresse vide (<see cref="string.Empty"/>) est une valeur licite, écrite telle
    /// quelle (champ facultatif — contrairement au nom, jamais vide).</summary>
    void ChangerAdresse(string acteurId, string adresse);

    /// <summary>Affecte un rôle du référentiel à l'acteur identifié de façon stable (surface distincte
    /// du nom et de la couleur — attribut optionnel d'organisation, réutilise le chemin d'écriture de
    /// la config acteur augmenté d'un id de rôle). La valeur est un <b>id de rôle</b> du référentiel,
    /// jamais un libellé en dur : la validation « rôle présent dans le référentiel » est faite en amont
    /// par le use case (le store écrit ce qu'on lui donne).</summary>
    void AffecterRole(string acteurId, string roleId);

    /// <summary>Retire le rôle porté par l'acteur identifié de façon stable : l'attribut rôle redevient
    /// non renseigné (« sans rôle » = neutre assumé). Tolérant à l'absence (un acteur déjà sans rôle
    /// reste sans rôle) — aucun rôle fantôme. Sert au retrait explicite (Sc.5) et au repli des porteurs
    /// d'un rôle supprimé (Sc.6).</summary>
    void RetirerRole(string acteurId);

    /// <summary>Retire l'acteur identifié de façon stable de la configuration : son <b>nom</b>
    /// ET sa <b>couleur</b> (miroir d'<see cref="Ajouter"/>). Après retrait, l'acteur n'est plus
    /// énuméré ni résolu — <see cref="IReferentielResponsables.NomDe"/> retombe sur l'id brut et la
    /// couleur sur le neutre par contrat. La clé est l'identifiant stable opaque, jamais le libellé.</summary>
    void Supprimer(string acteurId);
}
