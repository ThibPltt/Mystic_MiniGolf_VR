using UnityEngine;

// À mettre sur la mascotte du début (Mascotte_NPC).
// Place un club de golf DÉCORATIF dans la main de la mascotte : il n'a ni physique ni prise,
// donc il ne gêne pas le jeu. Par défaut, il n'est visible que pendant l'animation "Golf"
// (quand la mascotte montre comment tirer), pas pendant le "coucou".
//
// Réglage : lance Play, regarde la mascotte et ajuste "Rotation Offset" / "Position Offset"
// en direct (le club se met à jour tout de suite). Une fois bon : clic droit sur le composant >
// Copy Component, sors du mode Play, puis clic droit > Paste Component Values.
public class MascotHeldClub : MonoBehaviour
{
    public enum Hand { Right, Left }

    [Header("Main")]
    [SerializeField] private Hand hand = Hand.Right;
    [Tooltip("Nom de l'os de la main (vide = mixamorig:RightHand / mixamorig:LeftHand)")]
    [SerializeField] private string handBoneName = "";

    [Header("Club")]
    [Tooltip("Club dont on copie l'apparence (vide = un GolfClub de la scène)")]
    [SerializeField] private GameObject clubSource;
    [Tooltip("Point de prise sur le manche, en coordonnées locales du modèle du club")]
    [SerializeField] private Vector3 gripPoint = new Vector3(0.9f, 0.1f, 9.5f);
    [Tooltip("Longueur du club par rapport à la taille de la mascotte (un vrai club ≈ 0,5)")]
    [SerializeField] private float lengthRatio = 0.5f;

    [Header("Ajustement (modifiable en direct en mode Play)")]
    [SerializeField] private Vector3 rotationOffset = new Vector3(0f, 90f, 0f);
    [Tooltip("Décalage en mètres, dans le repère de la prise")]
    [SerializeField] private Vector3 positionOffset = Vector3.zero;

    [Header("Affichage")]
    [Tooltip("Le club n'est visible que pendant cet état d'animation (vide = toujours visible)")]
    [SerializeField] private string visibleOnlyDuringState = "Golf";

    private Transform handBone;
    private Transform pivot;
    private Transform clubVisual;
    private Vector3 clubScale;
    private Animator animator;

    private void Start()
    {
        animator = GetComponentInChildren<Animator>();

        string boneName = string.IsNullOrEmpty(handBoneName)
            ? (hand == Hand.Right ? "mixamorig:RightHand" : "mixamorig:LeftHand")
            : handBoneName;
        handBone = TrouverOs(transform, boneName);
        if (handBone == null)
        {
            Debug.LogWarning("[MascotHeldClub] Os '" + boneName + "' introuvable sur '" + name + "'.");
            enabled = false;
            return;
        }

        if (clubSource == null)
        {
            var head = FindObjectOfType<GolfClubHead>();
            if (head != null)
                clubSource = head.gameObject;
        }
        if (clubSource == null)
        {
            Debug.LogWarning("[MascotHeldClub] Aucun club à copier : glisse un GolfClub dans 'Club Source'.");
            enabled = false;
            return;
        }

        // (pas d'opérateur ?? avec les composants Unity : un composant absent n'est pas un vrai null dans l'éditeur)
        MeshFilter sourceFilter = clubSource.GetComponent<MeshFilter>();
        if (sourceFilter == null)
            sourceFilter = clubSource.GetComponentInChildren<MeshFilter>();
        var sourceRenderer = sourceFilter != null ? sourceFilter.GetComponent<MeshRenderer>() : null;
        if (sourceFilter == null || sourceRenderer == null)
        {
            Debug.LogWarning("[MascotHeldClub] Le club source n'a pas de modèle (MeshFilter / MeshRenderer).");
            enabled = false;
            return;
        }

        // Taille : mêmes proportions que les clubs du jeu, longueur adaptée à la mascotte
        float hauteurMascotte = HauteurMascotte();
        Vector3 proportions = Abs(sourceFilter.transform.lossyScale);
        float longueurModele = sourceFilter.sharedMesh.bounds.size.z * proportions.z;
        float facteur = longueurModele > 0.0001f && hauteurMascotte > 0.0001f
            ? (hauteurMascotte * lengthRatio) / longueurModele
            : 1f;
        clubScale = proportions * facteur;

        // Construction : main > pivot (échelle 1, rotation réglable) > club visuel
        pivot = new GameObject("ClubMascotte").transform;
        pivot.SetParent(handBone, false);

        clubVisual = new GameObject("Club").transform;
        clubVisual.SetParent(pivot, false);
        clubVisual.gameObject.AddComponent<MeshFilter>().sharedMesh = sourceFilter.sharedMesh;
        clubVisual.gameObject.AddComponent<MeshRenderer>().sharedMaterials = sourceRenderer.sharedMaterials;

        AppliquerReglages();
    }

    private void LateUpdate()
    {
        if (pivot == null)
            return;

        AppliquerReglages();

        bool visible = string.IsNullOrEmpty(visibleOnlyDuringState) || EtatActif(visibleOnlyDuringState);
        if (clubVisual.gameObject.activeSelf != visible)
            clubVisual.gameObject.SetActive(visible);
    }

    private void AppliquerReglages()
    {
        // Le pivot annule l'échelle de l'os pour travailler en mètres
        Vector3 s = handBone.lossyScale;
        pivot.localPosition = Vector3.zero;
        pivot.localRotation = Quaternion.Euler(rotationOffset);
        pivot.localScale = new Vector3(Inverse(s.x), Inverse(s.y), Inverse(s.z));

        // Le point de prise du manche est placé dans la main
        clubVisual.localScale = clubScale;
        clubVisual.localPosition = -Vector3.Scale(gripPoint, clubScale) + positionOffset;
        clubVisual.localRotation = Quaternion.identity;
    }

    private bool EtatActif(string etat)
    {
        if (animator == null || !animator.isActiveAndEnabled)
            return true;
        if (animator.GetCurrentAnimatorStateInfo(0).IsName(etat))
            return true;
        return animator.IsInTransition(0) && animator.GetNextAnimatorStateInfo(0).IsName(etat);
    }

    private float HauteurMascotte()
    {
        bool trouve = false;
        Bounds b = new Bounds(transform.position, Vector3.zero);
        foreach (var r in GetComponentsInChildren<Renderer>())
        {
            if (!(r is SkinnedMeshRenderer) && !(r is MeshRenderer))
                continue;
            if (r.GetComponentInParent<Canvas>() != null)
                continue; // la bulle de texte ne compte pas
            if (!trouve) { b = r.bounds; trouve = true; }
            else b.Encapsulate(r.bounds);
        }
        return trouve ? b.size.y : 1.7f;
    }

    private static Transform TrouverOs(Transform racine, string nom)
    {
        if (racine.name == nom)
            return racine;
        foreach (Transform enfant in racine)
        {
            var t = TrouverOs(enfant, nom);
            if (t != null)
                return t;
        }
        return null;
    }

    private static float Inverse(float v) => Mathf.Abs(v) > 0.000001f ? 1f / v : 1f;

    private static Vector3 Abs(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
}
