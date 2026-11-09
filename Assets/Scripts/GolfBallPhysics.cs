using UnityEngine;

public class GolfBallPhysics : MonoBehaviour
{
    public float force = 10f; // La force appliquée à la balle

    void Start()
    {
        // Assurez-vous que le Rigidbody est configuré correctement
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.drag = 0.5f; // Ajustez la résistance de l'air
            rb.angularDrag = 0.5f; // Ajustez la résistance à la rotation
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Vérifiez si le déclencheur est avec le club de golf
        if (other.gameObject.CompareTag("GolfClub"))
        {
            Debug.Log("Le club de golf a frappé la balle!"); // Message de débogage

            // Désactivez "Is Kinematic" sur le Rigidbody
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb.isKinematic)
            {
                rb.isKinematic = false;
            }

            // Calculez la direction de la force
            Vector3 direction = other.ClosestPoint(transform.position) - transform.position;
            direction = -direction.normalized;

            // Appliquez la force à la balle
            rb.AddForce(direction * force, ForceMode.Impulse);
        }
        else
        {
            // Détecter les déclencheurs avec d'autres objets
            Debug.Log("Déclencheur avec un autre objet: " + other.gameObject.name);
        }
    }
}