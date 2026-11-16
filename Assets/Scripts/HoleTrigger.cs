using UnityEngine;

// À mettre sur chaque trou (Trou, Trou2, Trou3) en indiquant son numéro de niveau.
// Quand une balle arrive sur le trou : la balle disparaît, le son de victoire est joué
// et l'écran de victoire affiche "Niveau X terminé !" avec le nombre de coups.
// Quand tous les trous de la scène sont réussis (dans n'importe quel ordre),
// un message de félicitations avec le score total s'affiche ensuite.
//
// Détection : la balle doit être au-dessus de l'ouverture du trou ET au niveau du sol
// (pas en vol). Elle n'a pas besoin de tomber physiquement au fond : ça marche même
// si le sol de la piste ou le terrain bouche la coupe.
public class HoleTrigger : MonoBehaviour
{
    [Tooltip("Numéro du niveau de ce trou (1, 2, 3...)")]
    [SerializeField] private int levelNumber = 1;

    [Tooltip("Son de victoire (2 s maximum sont joués). Vide = petit jingle généré automatiquement.")]
    [SerializeField] private AudioClip victorySound;

    [Range(0f, 1f)]
    [SerializeField] private float volume = 0.8f;

    [Header("Détection")]
    [Tooltip("Taille de la zone de détection par rapport à l'ouverture du trou (1 = taille du trou)")]
    [SerializeField] private float holeSizeFactor = 1f;

    [Tooltip("Hauteur (m) au-dessus du sol en dessous de laquelle la balle compte (évite les balles en vol)")]
    [SerializeField] private float heightTolerance = 0.1f;

    [Tooltip("Affiche dans la Console la distance de la balle la plus proche (pour régler la détection)")]
    [SerializeField] private bool debugLog = false;

    private Vector3 holeCenter;
    private float holeRadius;
    private float floorY;
    private bool ready;
    private GolfBallPhysics[] balls;
    private float nextLogTime;

    private void Start()
    {
        var rend = GetComponentInChildren<Renderer>();
        if (rend == null)
        {
            Debug.LogWarning("[HoleTrigger] '" + name + "' : aucun modèle (Renderer) trouvé, détection impossible.");
            return;
        }

        Bounds b = rend.bounds;
        holeCenter = b.center;
        holeRadius = Mathf.Min(b.extents.x, b.extents.z) * holeSizeFactor;
        floorY = Mathf.Max(b.max.y, FloorHeightAroundHole(b.max.y));

        // Un Mesh Collider "Convex" bouche la coupe
        foreach (var mc in GetComponentsInChildren<MeshCollider>())
        {
            if (mc.convex)
            {
                mc.convex = false;
                Debug.LogWarning("[HoleTrigger] '" + name + "' : Mesh Collider 'Convex' décoché pour ne pas boucher le trou. " +
                                 "Décoche-le aussi dans l'éditeur.");
            }
        }

        balls = FindObjectsOfType<GolfBallPhysics>(true);
        ready = true;
    }

    // Hauteur du sol de la piste autour du trou : on sonde 8 points autour de l'ouverture
    // et on garde le plus haut, en ignorant ce qui est trop haut (drapeau, barrières).
    private float FloorHeightAroundHole(float rimY)
    {
        float best = float.NegativeInfinity;
        for (int i = 0; i < 8; i++)
        {
            float angle = i * Mathf.PI / 4f;
            Vector3 p = holeCenter + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * (holeRadius * 1.5f + 0.05f);
            Vector3 start = new Vector3(p.x, rimY + 3f, p.z);
            foreach (var hit in Physics.RaycastAll(start, Vector3.down, 6f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.GetComponentInParent<GolfBallPhysics>() != null)
                    continue;
                float y = hit.point.y;
                if (y < rimY - 0.3f || y > rimY + 1f)
                    continue;
                if (y > best)
                    best = y;
            }
        }
        return float.IsNegativeInfinity(best) ? rimY : best;
    }

    private void Update()
    {
        if (!ready)
            return;

        GolfBallPhysics closest = null;
        float closestDistance = float.MaxValue;

        foreach (var ball in balls)
        {
            if (ball == null || !ball.gameObject.activeInHierarchy)
                continue;

            Vector3 p = ball.transform.position;
            float horizontal = new Vector2(p.x - holeCenter.x, p.z - holeCenter.z).magnitude;
            float ballRadius = BallRadius(ball);
            float maxY = floorY + ballRadius + heightTolerance;

            if (horizontal <= holeRadius && p.y <= maxY)
            {
                BallHoled(ball);
                return;
            }

            if (horizontal < closestDistance)
            {
                closestDistance = horizontal;
                closest = ball;
            }
        }

        if (debugLog && closest != null && Time.time >= nextLogTime)
        {
            nextLogTime = Time.time + 0.5f;
            Vector3 p = closest.transform.position;
            Debug.Log("[HoleTrigger " + name + "] balle la plus proche : '" + closest.name + "' à " +
                      closestDistance.ToString("F2") + " m à plat (doit être ≤ " + holeRadius.ToString("F2") +
                      ") | hauteur y = " + p.y.ToString("F2") + " (doit être ≤ " +
                      (floorY + BallRadius(closest) + heightTolerance).ToString("F2") + ")");
        }
    }

    // Si la balle touche directement le modèle du trou, ça compte aussi
    private void OnCollisionEnter(Collision collision)
    {
        if (!ready)
            return;
        var ball = collision.collider.GetComponentInParent<GolfBallPhysics>();
        if (ball != null && ball.gameObject.activeInHierarchy)
            BallHoled(ball);
    }

    private static float BallRadius(GolfBallPhysics ball)
    {
        var sphere = ball.GetComponent<SphereCollider>();
        if (sphere == null)
            return 0.05f;
        Vector3 s = ball.transform.lossyScale;
        return sphere.radius * Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
    }

    private void BallHoled(GolfBallPhysics ball)
    {
        // La balle disparaît (plus de physique, plus de retour automatique)
        ball.gameObject.SetActive(false);

        VictoryScreen.Instance.LevelCompleted(levelNumber, victorySound, volume);
    }

    // Pour tester sans jouer : en mode Play, clic droit sur le composant > "Tester la victoire"
    [ContextMenu("Tester la victoire")]
    private void TestVictory()
    {
        if (Application.isPlaying)
            VictoryScreen.Instance.LevelCompleted(levelNumber, victorySound, volume);
        else
            Debug.Log("[HoleTrigger] Lance le mode Play pour tester la victoire.");
    }

    // Cercle jaune = zone de détection (visible quand le trou est sélectionné)
    private void OnDrawGizmosSelected()
    {
        var rend = GetComponentInChildren<Renderer>();
        if (rend == null)
            return;
        Bounds b = rend.bounds;
        float r = Mathf.Min(b.extents.x, b.extents.z) * holeSizeFactor;
        float y = Application.isPlaying ? floorY : b.max.y;
        Vector3 c = new Vector3(b.center.x, y, b.center.z);

        Gizmos.color = Color.yellow;
        const int segments = 32;
        for (int i = 0; i < segments; i++)
        {
            float a0 = i * Mathf.PI * 2f / segments, a1 = (i + 1) * Mathf.PI * 2f / segments;
            Gizmos.DrawLine(c + new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0)) * r,
                            c + new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1)) * r);
        }
    }
}
