using Unity.XR.CoreUtils;
using UnityEngine;


public class BoatMovement : MonoBehaviour
{
    public float speed = 0.25f; // Vitesse du bateau
    public float targetDistance = 60f; // Distance cible à parcourir
    public float waterLevel = -5f; // Niveau de l'eau
    public float floatHeight = 2f; // Hauteur de flottabilité
    public float bounceDamp = 0.05f; // Amortissement de la flottabilité
    public float buoyancyForce = 5f; // Force de flottabilité
    public Transform anchorPoint; // Point d'ancrage pour le personnage

    private Vector3 startPosition;
    private Rigidbody rb;
    private bool isCharacterOnBoard = false;
    private Transform characterTransform; // Référence au transform du personnage
    private Rigidbody characterRb; // Référence au Rigidbody du personnage
    private Collider characterCollider; // Référence au Collider du personnage

    void Start()
    {
        // Enregistrez la position de départ
        startPosition = transform.position;

        // Obtenez le Rigidbody attaché au bateau
        rb = GetComponent<Rigidbody>();
    }


    void FixedUpdate()
    {
        if (isCharacterOnBoard)
        {
            // Calculez la distance parcourue
            float distanceTravelled = Vector3.Distance(startPosition, transform.position);

            // Vérifiez si la distance cible est atteinte
            if (distanceTravelled < targetDistance)
            {
                // Déplacez le bateau vers la droite (direction inversée)
                Vector3 rightDirection = -transform.right;
                Debug.Log("Direction du déplacement: " + rightDirection);
                rb.AddForce(rightDirection * speed, ForceMode.Force);
            }
            else
            {
                // Arrêtez le bateau
                rb.velocity = new Vector3(0, rb.velocity.y, 0); // Conservez la vitesse verticale
            }
        }

        // Appliquez la force de flottabilité
        ApplyBuoyancy();
    }

    void ApplyBuoyancy()
    {
        // Calculez la profondeur de l'eau
        float waterDepth = waterLevel - transform.position.y;

        // Calculez la force de flottabilité
        if (waterDepth > 0)
        {
            float forceFactor = Mathf.Clamp01(waterDepth / floatHeight);
            Vector3 uplift = -Physics.gravity * (forceFactor - rb.velocity.y * bounceDamp);
            rb.AddForce(uplift * buoyancyForce, ForceMode.Acceleration);
        }

        // Ajustez la position verticale pour rester au-dessus du niveau de l'eau
        if (transform.position.y < waterLevel + floatHeight)
        {
            Vector3 newPosition = transform.position;
            newPosition.y = Mathf.Lerp(transform.position.y, waterLevel + floatHeight, 0.05f); // Utilisez un facteur de lerp plus petit
            rb.MovePosition(newPosition);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("MainCamera"))
        {
            Debug.LogWarning(other.transform.parent.parent.parent.name + " est monté sur le bateau");
            GameObject player = other.transform.parent.parent.parent.gameObject;
            //characterTransform = player.transform; // Stockez le transform du personnage
            characterRb = player.GetComponent<Rigidbody>(); // Obtenez le Rigidbody du personnage
            characterCollider = player.GetComponent<Collider>(); // Obtenez le Collider du personnage
            if (characterRb != null)
            {
                characterRb.isKinematic = true; // Désactivez le Rigidbody du personnage
            }
            if (characterCollider != null)
            {
                characterCollider.enabled = false; // Désactivez le Collider du personnage
            }
            characterTransform.position = anchorPoint.position; // Déplacez le personnage vers le point d'ancrage
            characterTransform.SetParent(anchorPoint); // Définissez le parent du personnage comme étant le point d'ancrage
            isCharacterOnBoard = true;

        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("MainCamera"))
        {
            Debug.LogWarning(other.transform.parent.parent.parent.name + " est monté sur le bateau");
            isCharacterOnBoard = true;
            characterTransform.SetParent(null); // Réinitialisez le parent du personnage
            if (characterRb != null)
            {
                characterRb.isKinematic = false; // Réactivez le Rigidbody du personnage
                characterRb = null; // Réinitialisez la référence au Rigidbody du personnage
            }
            if (characterCollider != null)
            {
                characterCollider.enabled = true; // Réactivez le Collider du personnage
                characterCollider = null; // Réinitialisez la référence au Collider du personnage
            }
            characterTransform = null; // Réinitialisez la référence au transform du personnage
        }
    }
}