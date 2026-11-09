using UnityEngine;

public class GolfBallScore : MonoBehaviour
{
    private Score scoreScript;

    void Start()
    {
        // Trouver le script Score dans la scène
        scoreScript = FindObjectOfType<Score>();
    }

    void OnCollisionEnter(Collision collision)
    {
        // Vérifier si la collision est avec le club de golf
        if (collision.gameObject.CompareTag("GolfClub"))
        {
            // Appeler la méthode OnBallHit du script Score
            scoreScript.OnBallHit();
        }
    }
}