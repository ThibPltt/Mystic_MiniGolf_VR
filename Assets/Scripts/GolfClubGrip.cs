using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.XR.Interaction.Toolkit;

// À mettre sur chaque club de golf (à côté du XR Grab Interactable).
// Crée un point de prise sur le manche : quand on attrape le club, il se place
// toujours dans la main avec la même orientation. L'orientation est choisie selon
// la main qui s'approche (main gauche = main droite en miroir par défaut).
//
// Sélectionne un club dans la Scene view : la sphère jaune montre le point de prise,
// le trait bleu la direction du manche, le trait vert le "haut" de la main (main droite).
[RequireComponent(typeof(XRGrabInteractable))]
public class GolfClubGrip : MonoBehaviour
{
    [Tooltip("Position du point de prise, en coordonnées locales du club")]
    [SerializeField] private Vector3 positionPrise = new Vector3(0.9f, 0.1f, 9.5f);

    [Tooltip("Rotation du point de prise pour la main droite (degrés)")]
    [FormerlySerializedAs("rotationPrise")]
    [SerializeField] private Vector3 rotationMainDroite = new Vector3(0f, 180f, 220f);

    [Tooltip("Main gauche = main droite en miroir (Z inversé)")]
    [SerializeField] private bool mainGaucheEnMiroir = true;

    [Tooltip("Utilisé seulement si 'Main Gauche En Miroir' est décoché")]
    [SerializeField] private Vector3 rotationMainGauche = new Vector3(0f, 180f, -220f);

    private XRGrabInteractable grab;
    private Transform pointDePrise;

    private Vector3 RotationGauche => mainGaucheEnMiroir
        ? new Vector3(rotationMainDroite.x, rotationMainDroite.y, -rotationMainDroite.z)
        : rotationMainGauche;

    private void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();

        pointDePrise = new GameObject("PointDePrise").transform;
        pointDePrise.SetParent(transform, false);
        pointDePrise.localPosition = positionPrise;
        pointDePrise.localRotation = Quaternion.Euler(rotationMainDroite);

        grab.attachTransform = pointDePrise;
        grab.useDynamicAttach = false; // sinon le club garde l'angle dans lequel on l'a attrapé

        grab.hoverEntered.AddListener(OnSurvol);
    }

    private void OnDestroy()
    {
        if (grab != null)
            grab.hoverEntered.RemoveListener(OnSurvol);
    }

    // Quand une main s'approche (avant de l'attraper), on règle l'orientation pour cette main.
    private void OnSurvol(HoverEnterEventArgs args)
    {
        if (grab.isSelected || args.interactorObject is XRGazeInteractor)
            return;

        bool gauche = EstMainGauche(args.interactorObject.transform);
        pointDePrise.localRotation = Quaternion.Euler(gauche ? RotationGauche : rotationMainDroite);
    }

    private static bool EstMainGauche(Transform t)
    {
        for (; t != null; t = t.parent)
        {
            if (t.name.Contains("Left"))
                return true;
            if (t.name.Contains("Right"))
                return false;
        }
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 p = transform.TransformPoint(positionPrise);
        Quaternion r = transform.rotation * Quaternion.Euler(rotationMainDroite);

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(p, 0.03f);
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(p, p + r * Vector3.forward * 0.3f);
        Gizmos.color = Color.green;
        Gizmos.DrawLine(p, p + r * Vector3.up * 0.15f);
    }
}
