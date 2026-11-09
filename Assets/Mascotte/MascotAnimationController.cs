using TMPro;
using UnityEngine;

// La mascotte fait "coucou" en boucle tant que le joueur est loin, avec le message d'accueil.
// Dès que le joueur entre dans le rayon de détection, elle passe à l'animation de golf
// (en boucle pour toujours) et la bulle affiche l'explication.
//
// Fonctionne avec le contrôleur Mascotte_Animated2 (états "Coucou" et "Golf",
// paramètre Trigger "JoueurProche").
public class MascotAnimationController : MonoBehaviour
{
    [Tooltip("Animator de la mascotte (trouvé automatiquement si vide)")]
    [SerializeField] private Animator animator;

    [Tooltip("Tête du joueur (si vide : la caméra principale)")]
    [SerializeField] private Transform player;

    [Tooltip("Distance (en mètres, à plat) à partir de laquelle la mascotte passe au golf")]
    [SerializeField] private float detectionRadius = 5.0f;

    [Tooltip("Nom du paramètre Trigger dans l'Animator")]
    [SerializeField] private string triggerName = "JoueurProche";

    [Header("Bulle de texte")]
    [Tooltip("Texte de la bulle (trouvé automatiquement dans les enfants de la mascotte si vide)")]
    [SerializeField] private TMP_Text texteBulle;

    [TextArea(2, 4)]
    [SerializeField] private string messageAccueil = "Salut !\nJe vais te montrer comment jouer :)";

    [TextArea(2, 4)]
    [SerializeField] private string messageGolf = "Prends le club de golf et tire de cette manière sur la balle";

    private int triggerHash;
    private bool golfLance;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (texteBulle == null)
            texteBulle = GetComponentInChildren<TMP_Text>(true);

        triggerHash = Animator.StringToHash(triggerName);
    }

    private void Start()
    {
        AfficherMessage(messageAccueil);
    }

    private void Update()
    {
        if (golfLance || animator == null)
            return;

        if (player == null)
        {
            if (Camera.main == null)
                return;
            player = Camera.main.transform;
        }

        // Distance horizontale : on ignore la différence de hauteur
        Vector3 ecart = player.position - transform.position;
        ecart.y = 0f;

        if (ecart.sqrMagnitude <= detectionRadius * detectionRadius)
        {
            animator.SetTrigger(triggerHash);
            AfficherMessage(messageGolf);
            golfLance = true;
            enabled = false; // plus besoin de surveiller la distance
        }
    }

    private void AfficherMessage(string message)
    {
        if (texteBulle != null)
            texteBulle.text = message;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
