using UnityEngine;

public class MascotAnimationController : MonoBehaviour
{
    public Animator animator;
    public string defaultAnimation = "armature.001|mixamo.com|Layer0.001";
    public string proximityAnimation = "armature.001|mixamo.com|Layer0";
    public float detectionRadius = 5.0f;
    public Transform player;

    private bool isPlayerNearby = false;

    void Start()
    {
        // Lancer l'animation par défaut en boucle
        animator.Play(defaultAnimation);
    }

    void Update()
    {
        // Vérifier la distance entre la mascotte et le joueur
        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= detectionRadius)
        {
            if (!isPlayerNearby)
            {
                // Lancer l'animation de proximité
                animator.Play(proximityAnimation);
                isPlayerNearby = true;
            }
        }
        else
        {
            if (isPlayerNearby)
            {
                // Revenir à l'animation par défaut
                animator.Play(defaultAnimation);
                isPlayerNearby = false;
            }
        }
    }
}