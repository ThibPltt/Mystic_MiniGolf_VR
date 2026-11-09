using UnityEngine;

// Joue le son de pas quand le joueur se déplace avec le joystick.
// On mesure la vitesse du CORPS (CharacterController du XR Origin), pas celle de la tête :
// regarder autour de soi, se pencher ou tourner avec le joystick droit ne déclenche plus le son.
[RequireComponent(typeof(AudioSource))]
public class PlayerMovementSound : MonoBehaviour
{
    [Tooltip("Corps du joueur (trouvé automatiquement dans les parents si vide)")]
    [SerializeField] private CharacterController corps;

    [Tooltip("Vitesse à plat (m/s) à partir de laquelle on joue le son de pas")]
    [SerializeField] private float vitesseMin = 0.3f;

    [Tooltip("Temps (s) sans mouvement avant de couper le son (évite les coupures)")]
    [SerializeField] private float delaiArret = 0.15f;

    private AudioSource audioSource;
    private float dernierMouvement = -10f;
    private Vector3 positionPrecedente;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = true;

        if (corps == null)
            corps = GetComponentInParent<CharacterController>();
    }

    private void Start()
    {
        audioSource.Stop();
        positionPrecedente = transform.position;
    }

    private void Update()
    {
        Vector3 vitesse;
        if (corps != null)
        {
            vitesse = corps.velocity;
        }
        else
        {
            // Secours : vitesse de cet objet
            vitesse = Time.deltaTime > 0f ? (transform.position - positionPrecedente) / Time.deltaTime : Vector3.zero;
            positionPrecedente = transform.position;
        }
        vitesse.y = 0f;

        if (vitesse.magnitude > vitesseMin)
            dernierMouvement = Time.time;

        bool bouge = Time.time - dernierMouvement < delaiArret;

        if (bouge && !audioSource.isPlaying)
            audioSource.Play();
        else if (!bouge && audioSource.isPlaying)
            audioSource.Stop();
    }
}
