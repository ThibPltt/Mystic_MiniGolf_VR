using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class ContinuerQuitter : MonoBehaviour
{
    // Références aux composants de rotation
    public ActionBasedSnapTurnProvider Continuer;
    public ActionBasedContinuousTurnProvider Quitter;

    // Méthode pour définir le type de rotation en fonction de l'index
    public void SetTypeFromIndex(int index)
    {
        if (index == 0)
        {
            // Désactiver les deux types de rotation
            Continuer.enabled = false;
            Quitter.enabled = false;
        }
        else if (index == 1)
        {
            // Activer la rotation par "Continuer" et désactiver "Quitter"
            Continuer.enabled = true;
            Quitter.enabled = false;
        }
    }
}