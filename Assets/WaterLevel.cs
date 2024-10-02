using UnityEngine;

public class WaterLevel : MonoBehaviour
{
    public float waterLevel = 0f; // Niveau de l'eau

    void OnTriggerStay(Collider other)
    {
        // Vérifiez si l'objet est un bateau
        BoatMovement boat = other.GetComponent<BoatMovement>();
        if (boat != null)
        {
            // Mettez à jour le niveau de l'eau dans le script BoatMovement
            boat.waterLevel = waterLevel;
        }
    }
}