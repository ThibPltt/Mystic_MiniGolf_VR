using UnityEngine;

public class GolfBallScore : MonoBehaviour
{
    private Score scoreScript;

    void Start()
    {
        TrouverScore();
    }

    void OnCollisionEnter(Collision collision)
    {
        // Vérifier si la collision est avec le club de golf
        if (collision.gameObject.CompareTag("GolfClub"))
        {
            if (scoreScript == null)
                TrouverScore();

            if (scoreScript != null)
                scoreScript.OnBallHit();
        }
    }

    private void TrouverScore()
    {
        // includeInactive = true : le compteur (Count) est masqué au démarrage,
        // il faut quand même le trouver.
        scoreScript = FindObjectOfType<Score>(true);
    }
}
