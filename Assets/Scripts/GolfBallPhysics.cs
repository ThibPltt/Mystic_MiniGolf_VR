using UnityEngine;

// Physique "à peu près réaliste" de la balle :
// - freinage de l'air quand elle vole (proportionnel à v², comme une vraie balle),
// - freinage de roulement quand elle roule (comme sur un green / un tapis de minigolf),
// - rebond et frottement réglables,
// - arrêt propre quand elle est presque immobile sur un sol plat.
// Les coups sont donnés par GolfClubHead (sur les clubs), pas par des collisions physiques.
[RequireComponent(typeof(Rigidbody))]
public class GolfBallPhysics : MonoBehaviour
{
    [Header("Air")]
    [Tooltip("Freinage de l'air quand la balle vole : a = k × v². 0,005 ≈ vraie balle de golf.")]
    [SerializeField] private float traineeAir = 0.005f;

    [Header("Roulement")]
    [Tooltip("Freinage quand la balle roule (m/s²). 0,5 ≈ green rapide, 1,5 ≈ herbe.")]
    [SerializeField] private float freinageRoulement = 0.5f;

    [Tooltip("En dessous de cette vitesse (m/s), la balle s'arrête si le sol est presque plat.")]
    [SerializeField] private float vitesseArret = 0.08f;

    [Header("Rebond et frottement")]
    [Range(0f, 1f)] [SerializeField] private float rebond = 0.45f;
    [Range(0f, 1f)] [SerializeField] private float frottement = 0.35f;

    [Tooltip("Masse de la balle (kg). Une vraie balle de golf pèse 0,046 kg.")]
    [SerializeField] private float masse = 0.046f;

    private Rigidbody rb;
    private float rayon = 0.05f;
    private float dernierContact = -10f;
    private float dernierCoup = -10f;

    private const float DelaiEntreContacts = 0.08f; // évite plusieurs impulsions pour un même contact
    private const float DelaiNouveauCoup = 0.5f;    // un "coup" compté au score au maximum toutes les 0,5 s

    public Vector3 Vitesse => rb != null ? rb.velocity : Vector3.zero;
    public bool PeutEtreFrappee => Time.time - dernierContact > DelaiEntreContacts;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.mass = masse;
        rb.drag = 0f;           // l'air est géré à la main (en v²)
        rb.angularDrag = 0.05f;
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.maxAngularVelocity = 100f;

        var sphere = GetComponent<SphereCollider>();
        if (sphere != null)
        {
            sphere.material = new PhysicMaterial("BalleDeGolf")
            {
                bounciness = rebond,
                dynamicFriction = frottement,
                staticFriction = frottement,
                bounceCombine = PhysicMaterialCombine.Average,
                frictionCombine = PhysicMaterialCombine.Average
            };

            Vector3 s = transform.lossyScale;
            rayon = sphere.radius * Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
        }
    }

    private void FixedUpdate()
    {
        Vector3 v = rb.velocity;
        float vitesse = v.magnitude;
        if (vitesse < 0.0001f)
            return;

        bool auSol = Physics.Raycast(rb.position, Vector3.down, out RaycastHit sol,
                                     rayon + 0.03f, ~0, QueryTriggerInteraction.Ignore);

        if (!auSol)
        {
            // En vol : freinage de l'air
            rb.AddForce(-traineeAir * vitesse * v, ForceMode.Acceleration);
            return;
        }

        // Au sol : freinage de roulement (dans le plan du sol)
        Vector3 vSol = Vector3.ProjectOnPlane(v, sol.normal);
        float vs = vSol.magnitude;
        float perte = freinageRoulement * Time.fixedDeltaTime;

        if (vs > perte)
            rb.AddForce(-vSol / vs * freinageRoulement, ForceMode.Acceleration);
        else
            rb.velocity = v - vSol;

        // Arrêt net quand elle est presque immobile sur un sol presque plat
        bool presquePlat = sol.normal.y > 0.997f;
        if (presquePlat && vitesse < vitesseArret && Time.time - dernierContact > 0.3f)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    // Appelé par GolfClubHead. Renvoie true si c'est un nouveau coup (pour le score).
    public bool Frapper(Vector3 nouvelleVitesse)
    {
        bool nouveauCoup = Time.time - dernierCoup > DelaiNouveauCoup;
        dernierCoup = Time.time;
        dernierContact = Time.time;

        rb.isKinematic = false;
        rb.WakeUp();
        rb.velocity = nouvelleVitesse;

        // Rotation cohérente avec le roulement (visuel)
        Vector3 h = new Vector3(nouvelleVitesse.x, 0f, nouvelleVitesse.z);
        if (rayon > 0f)
            rb.angularVelocity = Vector3.Cross(Vector3.up, h) / rayon;

        return nouveauCoup;
    }
}
