using UnityEngine;
using TMPro; // Importer le namespace TextMeshPro

public class Score : MonoBehaviour
{
    public TextMeshProUGUI counterText; // Référence au composant TextMeshProUGUI
    private int hitCount = 0;

    // Nombre de coups actuel (lu par l'écran de victoire)
    public int HitCount => hitCount;

    void Start()
    {
        UpdateCounterText();
    }

    // Appeler cette méthode lorsque la balle est touchée
    public void OnBallHit()
    {
        hitCount++;
        UpdateCounterText();
    }

    // Remise à zéro (après chaque niveau terminé)
    public void ResetScore()
    {
        hitCount = 0;
        UpdateCounterText();
    }

    void UpdateCounterText()
    {
        if (counterText != null)
            counterText.text = "Score: " + hitCount;
    }
}
