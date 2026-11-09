using UnityEngine;

public class BoatMovement : MonoBehaviour
{
    public float speed = 5f; // Vitesse du bateau
    public float targetDistance = 100f; // Distance cible à parcourir

    private Vector3 startPosition;
    private Rigidbody rb;

    void Start()
    {
        // Enregistrez la position de départ
        startPosition = transform.position;

        // Obtenez le Rigidbody attaché au bateau
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // Calculez la distance parcourue
        float distanceTravelled = Vector3.Distance(startPosition, transform.position);

        // Vérifiez si la distance cible est atteinte
        if (distanceTravelled < targetDistance)
        {
            // Déplacez le bateau tout droit
            rb.MovePosition(transform.position + transform.forward * speed * Time.deltaTime);
        }
        else
        {
            // Arrêtez le bateau
            rb.velocity = Vector3.zero;
        }
    }
}