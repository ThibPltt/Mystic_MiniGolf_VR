using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    // Méthode appelée lorsque le bouton de démarrage est cliqué
    public void StartButton()
    {
        // Charger la scène "MainScene"
        SceneManager.LoadScene("MainScene");
    }
}