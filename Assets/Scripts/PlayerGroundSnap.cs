using System.Collections;
using UnityEngine;

// Pose le joueur directement sur le sol au lancement de la scène,
// pour ne plus apparaître en l'air.
// À mettre sur "XR Origin (XR Rig)" ou sur son parent "XR Interaction Setup" :
// le script retrouve tout seul l'objet qui porte le Character Controller.
//
// Astuce : hors Play, clic droit sur le composant > "Poser le joueur sur le sol"
// descend le joueur dans la scène elle-même (Ctrl+Z possible, puis Ctrl+S).
[DefaultExecutionOrder(-50)]
public class PlayerGroundSnap : MonoBehaviour
{
    [Tooltip("Hauteur au-dessus du joueur d'où part la recherche du sol (m)")]
    public float searchHeight = 1f;

    [Tooltip("Distance maximale de recherche du sol vers le bas (m)")]
    public float maxDistance = 200f;

    [Tooltip("Couches considérées comme sol")]
    public LayerMask groundLayers = ~0;

    [Tooltip("Nombre d'images après le chargement pendant lesquelles on recolle le joueur au sol")]
    public int extraFrames = 3;

    private CharacterController controller;
    private Transform rig;

    private void Awake()
    {
        FindRig();
    }

    private IEnumerator Start()
    {
        Snap();

        for (int i = 0; i < extraFrames; i++)
        {
            yield return null;
            Snap();
        }
    }

    private void FindRig()
    {
        controller = GetComponentInChildren<CharacterController>(true);
        rig = controller != null ? controller.transform : transform;
    }

    [ContextMenu("Poser le joueur sur le sol")]
    public void Snap()
    {
        if (rig == null)
            FindRig();

        if (!Application.isPlaying)
            Physics.SyncTransforms();

        int playerLayer = rig.gameObject.layer;
        Vector3 start = rig.position + Vector3.up * searchHeight;
        RaycastHit[] hits = Physics.RaycastAll(start, Vector3.down, maxDistance + searchHeight,
                                               groundLayers, QueryTriggerInteraction.Ignore);

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            Transform t = hit.collider.transform;

            // Le joueur lui-même (Character Controller, caméra, mains…)
            if (t.IsChildOf(rig) || t.IsChildOf(transform))
                continue;

            // Objets qui bougent (balles, clubs…) : ce n'est pas du sol
            if (hit.rigidbody != null && !hit.rigidbody.isKinematic)
                continue;

            // Couches qui ne bloquent pas le joueur (matrice de collision) : on passerait au travers
            if (Physics.GetIgnoreLayerCollision(playerLayer, hit.collider.gameObject.layer))
                continue;

            SetHeight(hit.point.y);
            return;
        }
    }

    private void SetHeight(float y)
    {
        Vector3 position = rig.position;
        if (Mathf.Abs(position.y - y) < 0.001f)
            return;

        position.y = y;

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UnityEditor.Undo.RecordObject(rig, "Poser le joueur sur le sol");
            rig.position = position;
            UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(rig);
            return;
        }
#endif

        // Le Character Controller écrase les déplacements directs du Transform :
        // on le coupe le temps de déplacer le joueur.
        bool controllerWasEnabled = controller != null && controller.enabled;
        if (controllerWasEnabled)
            controller.enabled = false;

        rig.position = position;

        if (controllerWasEnabled)
            controller.enabled = true;

        Physics.SyncTransforms();
    }
}
