using UnityEngine;

// Caméra "spectateur" pour filmer le jeu.
// L'écran du PC (vue Game de Unity, ou fenêtre du build) affiche une vue 16:9 qui suit la tête
// du joueur en douceur, horizon droit, pendant que le casque affiche la vue VR normale.
// Il suffit ensuite d'enregistrer la vue Game / la fenêtre du jeu avec OBS.
//
// Utilisation : crée un objet vide "SpectatorCam" dans la scène et ajoute ce composant
// (il ajoute la Camera tout seul). Pour revenir à la vue normale, désactive l'objet.
[RequireComponent(typeof(Camera))]
public class SpectatorCamera : MonoBehaviour
{
    [Tooltip("Tête du joueur (si vide : la caméra principale du casque)")]
    [SerializeField] private Transform head;

    [Tooltip("Décalage par rapport aux yeux (m) : un peu derrière et au-dessus pour voir les mains et le club")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 0.05f, -0.2f);

    [Tooltip("Douceur du suivi de position (plus petit = plus lent / plus lisse)")]
    [SerializeField] private float positionSmoothing = 8f;

    [Tooltip("Douceur du suivi de rotation (plus petit = plus lent / plus lisse)")]
    [SerializeField] private float rotationSmoothing = 3f;

    [Tooltip("Champ de vision de la vidéo (degrés)")]
    [SerializeField] private float fieldOfView = 70f;

    [Tooltip("Garder l'horizon droit (ignore l'inclinaison de la tête sur le côté)")]
    [SerializeField] private bool levelHorizon = true;

    private Camera cam;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        cam.stereoTargetEye = StereoTargetEyeMask.None; // rend sur l'écran du PC, pas dans le casque
        cam.depth = 100;                                  // passe devant la vue miroir du casque
        cam.fieldOfView = fieldOfView;
        cam.nearClipPlane = 0.05f;

        // Le son reste sur la caméra du casque
        var listener = GetComponent<AudioListener>();
        if (listener != null)
            Destroy(listener);
    }

    private void Start()
    {
        TrouverTete();
        if (head != null)
            transform.SetPositionAndRotation(PositionCible(), RotationCible());
    }

    private void LateUpdate()
    {
        if (head == null && !TrouverTete())
            return;

        cam.fieldOfView = fieldOfView;

        float dt = Time.unscaledDeltaTime;
        transform.position = Vector3.Lerp(transform.position, PositionCible(), 1f - Mathf.Exp(-positionSmoothing * dt));
        transform.rotation = Quaternion.Slerp(transform.rotation, RotationCible(), 1f - Mathf.Exp(-rotationSmoothing * dt));
    }

    private bool TrouverTete()
    {
        if (head != null)
            return true;
        var principale = Camera.main;
        if (principale != null && principale != cam)
            head = principale.transform;
        return head != null;
    }

    private Quaternion RotationCible()
    {
        if (!levelHorizon)
            return head.rotation;

        Vector3 avant = head.forward;
        return avant.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(avant, Vector3.up) : transform.rotation;
    }

    private Vector3 PositionCible()
    {
        return head.position + RotationCible() * offset;
    }
}
