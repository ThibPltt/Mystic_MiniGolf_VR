using UnityEngine;

public class CameraPlatform : MonoBehaviour
{
    // Référence à la cible suivie par la caméra
    public Transform target;

    // Décalage initial entre la caméra et la cible
    private Vector3 offset;

    // Vitesse de lissage du mouvement de la caméra
    public float smoothSpeed = 0.125f;

    // Amortissement de la rotation de la caméra
    public float rotationDamping = 2.5f;

    private void Start()
    {
        // Vérifie si la cible est définie
        if (target == null)
        {
            // Lancer une exception si la cible est nulle
            throw new System.ArgumentException("TARGET cannot be null", "original");
        }

        // Calculer le décalage initial entre la caméra et la cible
        offset = transform.position - target.transform.position;
    }

    private void LateUpdate()
    {
        try
        {
            // Lisse la position de la caméra pour qu'elle suive la cible
            transform.position = Vector3.Lerp(transform.position, target.transform.position + (-target.transform.forward * offset.magnitude), smoothSpeed);

            // Lisse la rotation de la caméra pour qu'elle suive la rotation de la cible
            transform.rotation = Quaternion.Lerp(transform.rotation, target.rotation, Time.deltaTime * rotationDamping);
        }
        catch (System.Exception)
        {
            throw;
        }
    }
}