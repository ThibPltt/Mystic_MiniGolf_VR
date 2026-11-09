using Unity.VisualScripting;
using UnityEngine;

public class MenuFollow : MonoBehaviour
{
    // Référence à la position de la caméra
    [SerializeField] private Transform cameraTransform;

    // Distance entre le menu et la caméra
    [SerializeField] private float distance = 3.0f;

    // Indicateur pour savoir si le menu est centré
    private bool isCentered = false;

    // Méthode appelée lorsque l'objet devient invisible
    private void OnBecameInvisible()
    {
        isCentered = false;
    }

    private void Update()
    {
        if (isCentered)
        {
            // Trouver la position cible du menu
            Vector3 targetPosition = FindTargetPosition();

            // Déplacer le menu vers la position cible
            MoveTowards(targetPosition);

            // Vérifier si le menu a atteint la position cible
            if (ReachedPosition(targetPosition))
            {
                isCentered = true;
            }
        }
    }

    // Calculer la position cible du menu en fonction de la position et de l'orientation de la caméra
    private Vector3 FindTargetPosition()
    {
        return cameraTransform.position + (cameraTransform.forward * distance);
    }

    // Déplacer le menu vers la position cible à une vitesse définie
    private void MoveTowards(Vector3 targetPosition)
    {
        transform.position += (targetPosition - transform.position) * 0.025f;
    }

    // Vérifier si le menu est suffisamment proche de la position cible
    private bool ReachedPosition(Vector3 targetPosition)
    {
        return Vector3.Distance(targetPosition, transform.position) < 0.1f;
    }
}