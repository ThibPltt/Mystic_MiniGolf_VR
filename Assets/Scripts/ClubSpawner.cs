using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

// Bouton A (manette droite) : fait apparaître un nouveau club de golf devant le joueur.
// - Un club au maximum toutes les "Cooldown" secondes (anti-spam).
// - Au-delà de "Max Spawned" clubs créés, le plus ancien (s'il n'est pas tenu) disparaît.
// Touche C au clavier pour tester sans casque.
//
// À mettre sur n'importe quel objet actif de MainScene (ex. un objet vide "ClubSpawner").
public class ClubSpawner : MonoBehaviour
{
    [Tooltip("Club à copier : un des GolfClub de la scène (trouvé automatiquement si vide)")]
    [SerializeField] private GameObject clubTemplate;

    [Tooltip("Temps minimum (s) entre deux apparitions")]
    [SerializeField] private float cooldown = 5f;

    [Tooltip("Distance devant le joueur (m)")]
    [SerializeField] private float distanceInFront = 0.6f;

    [Tooltip("Hauteur sous les yeux (m) où le club apparaît")]
    [SerializeField] private float heightBelowEyes = 0.6f;

    [Tooltip("Nombre maximum de clubs créés en même temps")]
    [SerializeField] private int maxSpawned = 5;

    [Tooltip("Son optionnel joué à l'apparition")]
    [SerializeField] private AudioClip spawnSound;

    private InputAction spawnAction;
    private float lastSpawnTime = -999f;
    private readonly Queue<GameObject> spawnedClubs = new Queue<GameObject>();

    private void Awake()
    {
        spawnAction = new InputAction("SpawnClub", InputActionType.Button);
        spawnAction.AddBinding("<XRController>{RightHand}/primaryButton"); // bouton A
        spawnAction.AddBinding("<Keyboard>/c");
        spawnAction.performed += OnSpawnPressed;
        spawnAction.Enable();
    }

    private void Start()
    {
        if (clubTemplate == null)
        {
            var head = FindObjectOfType<GolfClubHead>();
            if (head != null)
                clubTemplate = head.gameObject;
        }

        if (clubTemplate == null)
            Debug.LogWarning("[ClubSpawner] Aucun club modèle trouvé : glisse un GolfClub dans 'Club Template'.");
    }

    private void OnDestroy()
    {
        if (spawnAction != null)
        {
            spawnAction.performed -= OnSpawnPressed;
            spawnAction.Disable();
            spawnAction.Dispose();
        }
    }

    private void OnSpawnPressed(InputAction.CallbackContext ctx)
    {
        if (Time.time - lastSpawnTime < cooldown)
            return; // anti-spam

        if (clubTemplate == null || Camera.main == null)
            return;

        lastSpawnTime = Time.time;
        SpawnClub(Camera.main.transform);
    }

    private void SpawnClub(Transform head)
    {
        Vector3 forward = head.forward;
        forward.y = 0f;
        forward = forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;

        Vector3 position = head.position + forward * distanceInFront + Vector3.down * heightBelowEyes;

        // Club debout, tête en bas (le manche du modèle est sur son axe Z, la tête côté -Z)
        Quaternion rotation = Quaternion.LookRotation(Vector3.up, forward);

        GameObject club = Instantiate(clubTemplate, position, rotation);
        club.name = "GolfClub (nouveau)";
        club.SetActive(true);

        // Le club ne doit pas hériter de l'état du modèle (ex. modèle tenu en main = cinématique)
        var rb = club.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (spawnSound != null)
            AudioSource.PlayClipAtPoint(spawnSound, position);

        spawnedClubs.Enqueue(club);
        RemoveOldClubs();
    }

    private void RemoveOldClubs()
    {
        int checks = spawnedClubs.Count;
        while (spawnedClubs.Count > maxSpawned && checks-- > 0)
        {
            GameObject oldest = spawnedClubs.Dequeue();
            if (oldest == null)
                continue;

            var grab = oldest.GetComponent<XRGrabInteractable>();
            if (grab != null && grab.isSelected)
            {
                spawnedClubs.Enqueue(oldest); // tenu en main : on le garde
                continue;
            }

            Destroy(oldest);
        }
    }
}
