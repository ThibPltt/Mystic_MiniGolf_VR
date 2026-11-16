using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

// À mettre sur chaque balle.
// Quand la balle sort de sa zone de golf (ou tombe trop bas, ex. dans l'eau),
// elle disparaît puis réapparaît aux pieds du joueur (légèrement devant lui).
// Si le joueur est lui-même hors de la zone de jeu, la balle revient à son emplacement d'origine.
//
// La zone se définit au choix :
// - avec un InvisibleWall fermé (le tracé par points, idéal pour les formes non rectangulaires).
//   Décoche "Creer Collider" dessus si tu veux juste le contour, sans mur ;
// - ou avec un collider simple (Box, Sphere, Capsule, Mesh convexe) qui couvre l'aire de jeu.
// Seule la position à plat compte : une balle qui vole au-dessus de la zone est toujours "dedans".
[RequireComponent(typeof(Rigidbody))]
public class BallRespawn : MonoBehaviour
{
    [Header("Play area (fill ONE of the two)")]
    [Tooltip("Contour de l'aire de jeu : un InvisibleWall fermé (avec ou sans collider)")]
    [FormerlySerializedAs("contour")]
    [SerializeField] private InvisibleWall outline;

    [Tooltip("Ou un collider simple (Box, Sphere, Capsule, Mesh convexe) qui couvre l'aire de jeu")]
    [FormerlySerializedAs("zone")]
    [SerializeField] private Collider area;

    [Header("Rules")]
    [Tooltip("Temps (s) passé hors de la zone avant de faire revenir la balle")]
    [FormerlySerializedAs("delaiHorsZone")]
    [SerializeField] private float outOfAreaDelay = 0.5f;

    [Tooltip("Si la balle descend de plus de X m sous son point de départ, elle revient (eau, chute)")]
    [FormerlySerializedAs("chuteMax")]
    [SerializeField] private float maxFallDistance = 3f;

    [Tooltip("Durée (s) pendant laquelle la balle reste invisible avant de réapparaître")]
    [FormerlySerializedAs("dureeDisparition")]
    [SerializeField] private float hiddenDuration = 0.4f;

    [Header("Respawn at the player's feet")]
    [Tooltip("Tête du joueur (si vide : la caméra principale)")]
    [SerializeField] private Transform player;

    [Tooltip("Distance (m) devant le joueur où la balle réapparaît")]
    [SerializeField] private float distanceInFront = 0.5f;

    [Tooltip("Calques considérés comme le sol (terrain, sol des pistes)")]
    [SerializeField] private LayerMask groundLayers = 1; // Default

    private Rigidbody rb;
    private Renderer[] renderers;
    private Collider[] colliders;
    private Vector3 startPosition;
    private Quaternion startRotation;
    private Vector3 startScale;
    private List<Vector2> polygon;
    private float outOfAreaSince = -1f;
    private bool respawning;
    private bool watchArea = true;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        renderers = GetComponentsInChildren<Renderer>(true);
        colliders = GetComponentsInChildren<Collider>(true);
    }

    private void Start()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;
        startScale = transform.localScale;

        if (outline != null)
        {
            polygon = new List<Vector2>();
            foreach (var p in outline.ContourMonde())
                polygon.Add(new Vector2(p.x, p.z));

            if (polygon.Count < 3)
            {
                Debug.LogWarning(name + " : le contour '" + outline.name + "' a moins de 3 points, il est ignoré.");
                polygon = null;
            }
        }

        if (polygon == null && area == null)
        {
            watchArea = false;
            Debug.LogWarning(name + " : aucune zone de jeu assignée, seule la chute est surveillée.");
        }
        else if (!IsInsideArea(startPosition))
        {
            watchArea = false;
            Debug.LogWarning(name + " : la balle démarre HORS de sa zone de jeu, vérifie le contour. " +
                             "La sortie de zone est désactivée pour cette balle.");
        }
    }

    private void Update()
    {
        if (respawning)
            return;

        Vector3 p = transform.position;

        if (p.y < startPosition.y - maxFallDistance)
        {
            StartCoroutine(RespawnRoutine());
            return;
        }

        if (watchArea && !IsInsideArea(p))
        {
            if (outOfAreaSince < 0f)
                outOfAreaSince = Time.time;
            else if (Time.time - outOfAreaSince >= outOfAreaDelay)
                StartCoroutine(RespawnRoutine());
        }
        else
        {
            outOfAreaSince = -1f; // revenue dans la zone (rebond) : on annule
        }
    }

    // Peut aussi être appelé par un autre script (ex. bouton "replacer la balle")
    public void Respawn()
    {
        if (!respawning)
            StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        respawning = true;
        outOfAreaSince = -1f;

        // Disparition
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;
        SetRenderersVisible(false);
        SetCollidersEnabled(false);

        yield return new WaitForSeconds(hiddenDuration);

        // Réapparition aux pieds du joueur (ou au point de départ si impossible)
        Vector3 target = ComputeRespawnPosition();
        rb.position = target;
        rb.rotation = startRotation;
        transform.SetPositionAndRotation(target, startRotation);

        // Petite apparition en grossissant
        SetRenderersVisible(true);
        const float appearDuration = 0.25f;
        float t = 0f;
        while (t < appearDuration)
        {
            t += Time.deltaTime;
            transform.localScale = startScale * Mathf.SmoothStep(0.2f, 1f, t / appearDuration);
            yield return null;
        }
        transform.localScale = startScale;

        SetCollidersEnabled(true);
        rb.isKinematic = false;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.WakeUp();

        respawning = false;
    }

    // Position aux pieds du joueur, un peu devant lui, posée sur le sol.
    // Retourne le point de départ si le joueur est introuvable, si aucun sol n'est trouvé
    // ou si cet endroit est hors de la zone de jeu.
    private Vector3 ComputeRespawnPosition()
    {
        if (player == null && Camera.main != null)
            player = Camera.main.transform;
        if (player == null)
            return startPosition;

        Vector3 forward = player.forward;
        forward.y = 0f;
        forward = forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;

        Vector3 feet = player.position + forward * distanceInFront;
        Vector3 rayStart = new Vector3(feet.x, player.position.y + 1f, feet.z);

        if (!Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 20f, groundLayers,
                             QueryTriggerInteraction.Ignore))
            return startPosition;

        Vector3 target = hit.point + Vector3.up * (BallRadius() + 0.01f);

        if (watchArea && !IsInsideArea(target))
            return startPosition;

        return target;
    }

    private float BallRadius()
    {
        var sphere = GetComponent<SphereCollider>();
        if (sphere == null)
            return 0.05f;
        Vector3 s = transform.lossyScale;
        return sphere.radius * Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
    }

    private void SetRenderersVisible(bool visible)
    {
        foreach (var r in renderers)
            if (r != null) r.enabled = visible;
    }

    private void SetCollidersEnabled(bool enabledState)
    {
        foreach (var c in colliders)
            if (c != null) c.enabled = enabledState;
    }

    // ---------- Test "dans la zone" (à plat) ----------

    private bool IsInsideArea(Vector3 p)
    {
        if (polygon != null)
            return IsInsidePolygon(new Vector2(p.x, p.z));

        if (area != null)
        {
            // On teste à la hauteur du centre de la zone : seule la position à plat compte
            Vector3 q = new Vector3(p.x, area.bounds.center.y, p.z);
            return (area.ClosestPoint(q) - q).sqrMagnitude < 0.0001f;
        }

        return true;
    }

    private bool IsInsidePolygon(Vector2 pt)
    {
        bool inside = false;
        int n = polygon.Count;
        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            Vector2 a = polygon[i], b = polygon[j];
            if ((a.y > pt.y) != (b.y > pt.y) &&
                pt.x < (b.x - a.x) * (pt.y - a.y) / (b.y - a.y) + a.x)
                inside = !inside;
        }
        return inside;
    }
}
