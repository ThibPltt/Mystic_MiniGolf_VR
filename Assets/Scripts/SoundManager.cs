using UnityEngine;
using UnityEngine.UI;

public class SoundManager : MonoBehaviour
{
    // Référence au slider de volume dans l'interface utilisateur
    [SerializeField] private Slider volumeSlider;

    // Awake (et non Start) : l'écran de jeu est masqué dès le lancement,
    // le volume enregistré doit quand même être appliqué tout de suite.
    void Awake()
    {
        if (!PlayerPrefs.HasKey("musicVolume"))
            PlayerPrefs.SetFloat("musicVolume", 1);

        Load();
    }

    // Appelé lorsque le slider de volume est modifié
    public void ChangeVolume()
    {
        // Mettre à jour le volume global de l'audio en fonction de la valeur du slider
        AudioListener.volume = volumeSlider.value;
        Save();
    }

    // Charger le volume depuis les PlayerPrefs, l'appliquer et mettre à jour le slider
    private void Load()
    {
        float volume = PlayerPrefs.GetFloat("musicVolume");
        AudioListener.volume = volume;
        if (volumeSlider != null)
            volumeSlider.value = volume;
    }

    // Enregistrer la valeur actuelle du slider dans les PlayerPrefs
    private void Save()
    {
        PlayerPrefs.SetFloat("musicVolume", volumeSlider.value);
    }
}
