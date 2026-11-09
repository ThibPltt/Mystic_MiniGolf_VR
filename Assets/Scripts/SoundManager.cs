using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SoundManager : MonoBehaviour
{
    // Référence au slider de volume dans l'interface utilisateur
    [SerializeField] private Slider volumeSlider;

    void Start()
    {
        // Vérifiez si la clé "musicVolume" existe dans les PlayerPrefs
        if (!PlayerPrefs.HasKey("musicVolume"))
        {
            // Si la clé n'existe pas, définissez le volume de la musique par défaut à 1
            PlayerPrefs.SetFloat("musicVolume", 1);
            Load();
        }
        else
        {
            // Si la clé existe, chargez le volume enregistré
            Load();
        }
    }

    // Appelé lorsque le slider de volume est modifié
    public void ChangeVolume()
    {
        // Mettre à jour le volume global de l'audio en fonction de la valeur du slider
        AudioListener.volume = volumeSlider.value;
        Save();
    }

    // Charger le volume depuis les PlayerPrefs et définir la valeur du slider
    private void Load()
    {
        volumeSlider.value = PlayerPrefs.GetFloat("musicVolume");
    }

    // Enregistrer la valeur actuelle du slider dans les PlayerPrefs
    private void Save()
    {
        PlayerPrefs.SetFloat("musicVolume", volumeSlider.value);
    }
}