using UnityEngine;
using TMPro; // Importer le namespace TextMeshPro

public class Score : MonoBehaviour
{
    public TextMeshProUGUI counterText; // Référence au composant TextMeshProUGUI
    private int hitCount = 0;

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

    void UpdateCounterText()
    {
        counterText.text = "Score: " + hitCount;
    }
}