using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuitGame : MonoBehaviour
{
    // Méthode pour quitter le jeu
    public void Quit()
    {
        // Quitter l'application
        Application.Quit();

        // Afficher un message de débogage pour confirmer l'action
        Debug.Log("Quit!");
    }
}