using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

// À mettre sur chaque club (à côté de XR Grab Interactable et GolfClubGrip).
//
// Seule la TÊTE du club frappe la balle : sa forme est une boîte (cadre cyan dans la
// Scene view quand le club est sélectionné). À chaque image, on balaie la boîte entre
// sa position précédente et sa position actuelle, donc même un coup rapide ne
// "traverse" pas la balle.
//
// La vitesse donnée à la balle dépend de la vitesse de la tête (comme un vrai club) :
// un coup doux fait rouler la balle, un coup fort la fait décoller.
[RequireComponent(typeof(XRGrabInteractable))]
public class GolfClubHead : MonoBehaviour
{
    [Header("Tête du club (repère local du modèle)")]
    [SerializeField] private Vector3 centreTete = new Vector3(-0.07f, 0f, -10.1f);
    [SerializeField] private Vector3 tailleTete = new Vector3(2.3f, 1.4f, 2.2f);

    [Header("Frappe")]
    [Tooltip("Vitesse de la balle / vitesse de la tête. ≈ 1,6 pour un vrai putter.")]
    [SerializeField] private float coefficientFrappe = 1.6f;

    [Tooltip("Angle de départ (degrés) pour un coup doux : la balle roule.")]
    [SerializeField] private float angleDepart = 2f;

    [Tooltip("Vitesse de balle (m/s) à partir de laquelle elle commence à décoller.")]
    [SerializeField] private float vitesseDecollage = 6f;

    [Tooltip("Degrés d'angle en plus par m/s au-dessus de la vitesse de décollage.")]
    [SerializeField] private float angleParMetreSeconde = 4f;

    [SerializeField] private float angleMax = 35f;
    [SerializeField] private float vitesseMaxBalle = 40f;

    [Tooltip("Vitesse minimale de la tête (m/s) pour compter un coup au score.")]
    [SerializeField] private float vitesseMinCoup = 0.2f;

    [Header("Options")]
    [Tooltip("Le club suit exactement la main (pas de retard, ne se bloque pas dans le sol).")]
    [SerializeField] private bool mouvementInstantane = true;
    [SerializeField] private bool vibration = true;

    private const int NbEchantillons = 4;
    private readonly Vector3[] positions = new Vector3[NbEchantillons];
    private readonly float[] instants = new float[NbEchantillons];
    private int nbEchantillons;
    private int indexEchantillon;

    private XRGrabInteractable grab;
    private Score score;
    private Vector3 centrePrecedent;
    private bool aCentrePrecedent;
    private float actifApres;

    private readonly RaycastHit[] resultatsBalayage = new RaycastHit[16];
    private readonly Collider[] resultatsChevauchement = new Collider[16];
    private readonly System.Collections.Generic.HashSet<GolfBallPhysics> touchees =
        new System.Collections.Generic.HashSet<GolfBallPhysics>();

    private void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        if (mouvementInstantane)
            grab.movementType = XRBaseInteractable.MovementType.Instantaneous;

        grab.selectEntered.AddListener(OnPrise);
        grab.selectExited.AddListener(OnLacher);
    }

    private void Start()
    {
        score = FindObjectOfType<Score>(true);

        // Le corps du club ne pousse plus les balles : seule la tête frappe, via ce script.
        // (Évite les collisions "à distance" dues aux gros colliders du club.)
        var collidersClub = GetComponentsInChildren<Collider>(true);
        foreach (var balle in FindObjectsOfType<GolfBallPhysics>(true))
        {
            foreach (var cb in balle.GetComponentsInChildren<Collider>(true))
            {
                foreach (var cc in collidersClub)
                    Physics.IgnoreCollision(cc, cb, true);
            }
        }
    }

    private void OnDestroy()
    {
        if (grab != null)
        {
            grab.selectEntered.RemoveListener(OnPrise);
            grab.selectExited.RemoveListener(OnLacher);
        }
    }

    private void OnPrise(SelectEnterEventArgs args) => Reinitialiser();
    private void OnLacher(SelectExitEventArgs args) => Reinitialiser();

    private void Reinitialiser()
    {
        nbEchantillons = 0;
        aCentrePrecedent = false;
        // Petit délai : quand le club "vole" jusqu'à la main, il ne doit rien frapper
        actifApres = Time.time + 0.15f;
    }

    private void Update()
    {
        if (!grab.isSelected)
        {
            aCentrePrecedent = false;
            return;
        }

        Vector3 centre = transform.TransformPoint(centreTete);
        Quaternion rotation = transform.rotation;
        Vector3 demiTaille = DemiTaille();

        positions[indexEchantillon] = centre;
        instants[indexEchantillon] = Time.time;
        indexEchantillon = (indexEchantillon + 1) % NbEchantillons;
        nbEchantillons = Mathf.Min(nbEchantillons + 1, NbEchantillons);

        if (!aCentrePrecedent || Time.time < actifApres)
        {
            centrePrecedent = centre;
            aCentrePrecedent = true;
            return;
        }

        Vector3 vitesseTete = VitesseTete();
        touchees.Clear();

        // 1) Balayage entre la position précédente et la position actuelle
        Vector3 deplacement = centre - centrePrecedent;
        float distance = deplacement.magnitude;
        if (distance > 0.0001f)
        {
            int n = Physics.BoxCastNonAlloc(centrePrecedent, demiTaille, deplacement / distance,
                                            resultatsBalayage, rotation, distance, ~0,
                                            QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
                Toucher(resultatsBalayage[i].collider, centre, rotation, demiTaille, vitesseTete);
        }

        // 2) Ce qui est déjà dans la tête maintenant
        int m = Physics.OverlapBoxNonAlloc(centre, demiTaille, resultatsChevauchement, rotation, ~0,
                                           QueryTriggerInteraction.Ignore);
        for (int i = 0; i < m; i++)
            Toucher(resultatsChevauchement[i], centre, rotation, demiTaille, vitesseTete);

        centrePrecedent = centre;
    }

    private void Toucher(Collider c, Vector3 centre, Quaternion rotation, Vector3 demiTaille, Vector3 vitesseTete)
    {
        if (c == null)
            return;

        var balle = c.GetComponentInParent<GolfBallPhysics>();
        if (balle == null || !touchees.Add(balle) || !balle.PeutEtreFrappee)
            return;

        // Normale de contact : du point de la tête le plus proche vers la balle, à plat
        Vector3 posBalle = balle.transform.position;
        Vector3 local = Quaternion.Inverse(rotation) * (posBalle - centre);
        Vector3 plusProche = new Vector3(
            Mathf.Clamp(local.x, -demiTaille.x, demiTaille.x),
            Mathf.Clamp(local.y, -demiTaille.y, demiTaille.y),
            Mathf.Clamp(local.z, -demiTaille.z, demiTaille.z));
        Vector3 normale = posBalle - (centre + rotation * plusProche);
        normale.y = 0f;
        if (normale.sqrMagnitude < 0.000001f)
            normale = new Vector3(vitesseTete.x, 0f, vitesseTete.z);
        if (normale.sqrMagnitude < 0.000001f)
            return;
        normale.Normalize();

        // Choc : seule la vitesse relative le long de la normale compte
        Vector3 vitesseBalle = balle.Vitesse;
        float approche = Vector3.Dot(vitesseBalle - vitesseTete, normale);
        if (approche >= 0f)
            return; // la balle s'éloigne déjà de la tête

        Vector3 v = vitesseBalle - coefficientFrappe * approche * normale;

        // Angle de départ : roule si coup doux, décolle si coup fort
        Vector3 horizontale = new Vector3(v.x, 0f, v.z);
        float s = horizontale.magnitude;
        float angle = Mathf.Min(angleMax,
                                angleDepart + Mathf.Max(0f, s - vitesseDecollage) * angleParMetreSeconde);
        v = horizontale + Vector3.up * (s * Mathf.Tan(angle * Mathf.Deg2Rad));
        v = Vector3.ClampMagnitude(v, vitesseMaxBalle);

        bool nouveauCoup = balle.Frapper(v);

        float vitesseTeteAPlat = new Vector3(vitesseTete.x, 0f, vitesseTete.z).magnitude;
        if (nouveauCoup && vitesseTeteAPlat >= vitesseMinCoup)
        {
            if (score == null)
                score = FindObjectOfType<Score>(true);
            if (score != null)
                score.OnBallHit();

            if (vibration)
                Vibrer(Mathf.Clamp01(0.2f + vitesseTeteAPlat * 0.15f));
        }
    }

    private Vector3 VitesseTete()
    {
        if (nbEchantillons < 2)
            return Vector3.zero;

        int dernier = (indexEchantillon - 1 + NbEchantillons) % NbEchantillons;
        int premier = (indexEchantillon - nbEchantillons + NbEchantillons) % NbEchantillons;
        float dt = instants[dernier] - instants[premier];
        return dt > 0.0001f ? (positions[dernier] - positions[premier]) / dt : Vector3.zero;
    }

    private Vector3 DemiTaille()
    {
        Vector3 s = transform.lossyScale;
        return new Vector3(Mathf.Abs(tailleTete.x * s.x), Mathf.Abs(tailleTete.y * s.y), Mathf.Abs(tailleTete.z * s.z)) * 0.5f;
    }

    private void Vibrer(float amplitude)
    {
        if (grab.interactorsSelecting.Count == 0)
            return;

        if (grab.interactorsSelecting[0] is XRBaseControllerInteractor interacteur && interacteur.xrController != null)
            interacteur.xrController.SendHapticImpulse(amplitude, 0.08f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.matrix = Matrix4x4.TRS(transform.TransformPoint(centreTete), transform.rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, DemiTaille() * 2f);
        Gizmos.matrix = Matrix4x4.identity;
    }
}
