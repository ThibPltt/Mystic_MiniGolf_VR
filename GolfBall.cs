using UnityEngine;

public class GolfBall : MonoBehaviour
{
    public float force = 10f; // La force appliquée à la balle

    void OnCollisionEnter(Collision collision)
    {
        // Vérifiez si la collision est avec le club de golf
        if (collision.gameObject.CompareTag("GolfClub"))
        {
            // Calculez la direction de la force
            Vector3 direction = collision.contacts[0].point - transform.position;
            direction = -direction.normalized;

            // Appliquez la force à la balle
            GetComponent<Rigidbody>().AddForce(direction * force, ForceMode.Impulse);
        }
    }
}
