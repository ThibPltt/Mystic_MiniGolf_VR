using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerMovementSound : MonoBehaviour
{
    private Vector3 previousPosition;
    private AudioSource audioSource;

    // Start est appelé avant la première mise à jour de la frame
    void Start()
    {
        // On stocke la position initiale du joueur
        previousPosition = transform.position;

        // On récupère le composant AudioSource
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            Debug.LogError("Le composant AudioSource est manquant");
        }
    }

    // Update est appelé une fois par frame
    void Update()
    {
        // Vérifie si la position du joueur a changé (le joueur a bougé)
        float distance = Vector3.Distance(transform.position, previousPosition);
        Debug.Log("Distance: " + distance);

        if (distance > 0.01f)
        {
            // Si le joueur bouge et que le son n'est pas en train de jouer, on le joue
            if (!audioSource.isPlaying)
            {
                Debug.Log("Le joueur bouge, on joue le son");
                audioSource.Play();
            }
        }
        else
        {
            // Si le joueur ne bouge pas, on arrête le son
            if (audioSource.isPlaying)
            {
                Debug.Log("Le joueur ne bouge pas, on arrête le son");
                audioSource.Stop();
            }
        }

        // Met à jour la position précédente
        previousPosition = transform.position;
    }
}