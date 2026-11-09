using UnityEngine;

public class WaterLevel : MonoBehaviour
{
    // Niveau de l'eau
    public float waterLevel = 0f;

    // Méthode appelée lorsqu'un autre Collider reste en contact avec le Collider attaché à cet objet
    void OnTriggerStay(Collider other)
    {
        // Vérifiez si l'objet entrant en collision est un bateau
        BoatMovement boat = other.GetComponent<BoatMovement>();
        if (boat != null)
        {
            // Si c'est un bateau, mettez à jour le niveau de l'eau dans le script BoatMovement
            boat.waterLevel = waterLevel;
        }
    }
}