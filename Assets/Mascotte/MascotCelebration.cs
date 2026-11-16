using TMPro;
using UnityEngine;

// À mettre sur la mascotte de FIN (celle placée à l'arrivée).
// - Pendant la partie, elle joue l'état "State Before End" (Golf par défaut) : pas de coucou.
//   Le script vérifie en continu qu'elle reste bien sur cet état.
// - Quand le joueur termine tous les niveaux, elle fait "coucou" en boucle et sa bulle affiche "Bravo !".
//
// Si cette mascotte est une copie de celle du début, le script désactive automatiquement
// son MascotAnimationController (accueil), qui sinon changerait l'animation et le texte.
// Au lancement, un message dans la Console résume ce que le script a trouvé.
public class MascotCelebration : MonoBehaviour
{
    [Tooltip("Animator de la mascotte (vide = tous les Animators de la mascotte)")]
    [SerializeField] private Animator animator;

    [Tooltip("État joué pendant la partie, avant la fin (vide = ne rien changer)")]
    [SerializeField] private string stateBeforeEnd = "Golf";

    [Tooltip("État joué à la fin (le coucou)")]
    [SerializeField] private string waveState = "Coucou";

    [Tooltip("Texte de la bulle (trouvé automatiquement dans les enfants si vide)")]
    [SerializeField] private TMP_Text speechText;

    [TextArea(1, 3)]
    [SerializeField] private string message = "Bravo !";

    [Tooltip("Cacher la bulle de texte jusqu'à la fin")]
    [SerializeField] private bool hideBubbleBeforeEnd = false;

    private Animator[] animators = new Animator[0];
    private GameObject bubble;
    private bool celebrated;

    private void Awake()
    {
        animators = animator != null ? new[] { animator } : GetComponentsInChildren<Animator>(true);

        if (speechText == null)
            speechText = GetComponentInChildren<TMP_Text>(true);

        if (speechText != null)
        {
            var canvas = speechText.GetComponentInParent<Canvas>(true);
            bubble = canvas != null ? canvas.gameObject : speechText.gameObject;
        }
    }

    private void OnEnable()
    {
        VictoryScreen.AllLevelsCompleted += Celebrate;
    }

    private void OnDisable()
    {
        VictoryScreen.AllLevelsCompleted -= Celebrate;
    }

    private void Start()
    {
        if (!string.IsNullOrEmpty(stateBeforeEnd))
        {
            DisableIntroScripts();
            foreach (var a in animators)
                if (HasState(a, stateBeforeEnd))
                    a.Play(stateBeforeEnd, 0, 0f);
        }

        if (hideBubbleBeforeEnd && bubble != null)
            bubble.SetActive(false);

        // Message seulement en cas de problème de configuration
        foreach (var a in animators)
        {
            if (!string.IsNullOrEmpty(stateBeforeEnd) && !HasState(a, stateBeforeEnd))
                Debug.LogWarning("[MascotCelebration] '" + CheminComplet(transform) + "' : l'état '" + stateBeforeEnd +
                                 "' n'existe pas dans le contrôleur d'animation (mets 'Golf' dans State Before End).");
            if (!HasState(a, waveState))
                Debug.LogWarning("[MascotCelebration] '" + CheminComplet(transform) + "' : l'état '" + waveState +
                                 "' n'existe pas dans le contrôleur d'animation.");
        }
    }

    private void Update()
    {
        if (celebrated || string.IsNullOrEmpty(stateBeforeEnd))
            return;

        // Garde la mascotte sur l'état "avant la fin" tant que la partie n'est pas terminée
        foreach (var a in animators)
        {
            if (a == null || !a.isActiveAndEnabled || !HasState(a, stateBeforeEnd))
                continue;
            if (a.IsInTransition(0))
                continue;
            if (!a.GetCurrentAnimatorStateInfo(0).IsName(stateBeforeEnd))
                a.CrossFadeInFixedTime(stateBeforeEnd, 0.25f, 0);
        }
    }

    public void Celebrate()
    {
        celebrated = true;
        DisableIntroScripts();

        foreach (var a in animators)
        {
            if (a == null)
                continue;
            ResetTriggerIfExists(a, "JoueurProche");
            if (HasState(a, waveState))
                a.CrossFadeInFixedTime(waveState, 0.25f, 0);
        }

        if (bubble != null)
            bubble.SetActive(true);

        if (speechText != null)
            speechText.text = message;
    }

    // Désactive les MascotAnimationController de cette mascotte (sur elle, ses enfants ou ses parents)
    private int DisableIntroScripts()
    {
        int count = 0;
        foreach (var intro in GetComponentsInChildren<MascotAnimationController>(true))
        {
            if (intro.enabled) count++;
            intro.enabled = false;
        }
        foreach (var intro in GetComponentsInParent<MascotAnimationController>(true))
        {
            if (intro.enabled) count++;
            intro.enabled = false;
        }
        return count;
    }

    private static bool HasState(Animator a, string stateName)
    {
        return a != null && a.runtimeAnimatorController != null && !string.IsNullOrEmpty(stateName) &&
               a.HasState(0, Animator.StringToHash(stateName));
    }

    private static void ResetTriggerIfExists(Animator a, string triggerName)
    {
        foreach (var p in a.parameters)
        {
            if (p.name == triggerName && p.type == AnimatorControllerParameterType.Trigger)
            {
                a.ResetTrigger(triggerName);
                return;
            }
        }
    }

    private static string CheminComplet(Transform t)
    {
        string chemin = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            chemin = t.name + "/" + chemin;
        }
        return chemin;
    }

    // Pour tester sans finir les 3 niveaux : en mode Play, clic droit sur le composant > "Tester la célébration"
    [ContextMenu("Tester la célébration")]
    private void TestCelebration()
    {
        if (Application.isPlaying)
            Celebrate();
        else
            Debug.Log("[MascotCelebration] Lance le mode Play pour tester.");
    }
}
