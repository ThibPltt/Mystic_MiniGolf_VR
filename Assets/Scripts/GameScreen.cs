using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

// Affiche / masque l'écran de jeu (GameScreen) et le compteur (Count)
// avec la gâchette de la manette droite (ou la touche P au clavier).
// Les écrans sont masqués au lancement de la scène.
//
// Remarque : la classe s'appelle GameScreen pour correspondre au nom du fichier
// (Unity l'exige pour les MonoBehaviour).
public class GameScreen : MonoBehaviour
{
    [Tooltip("Écran de jeu à afficher / masquer")]
    public GameObject gameScreen;

    [Tooltip("Autres éléments à afficher / masquer en même temps (ex. Count)")]
    public GameObject[] autresEcrans;

    [Tooltip("Les écrans sont-ils visibles au lancement de la scène ?")]
    public bool visibleAuDemarrage = false;

    [Tooltip("Ray Interactor de la main droite (optionnel). S'il vise un bouton ou un slider, " +
             "la gâchette l'utilise au lieu de fermer l'écran. Trouvé automatiquement si vide.")]
    public XRRayInteractor rayDroit;

    private InputAction toggleAction;
    private bool visible;

    private void Awake()
    {
        toggleAction = new InputAction("ToggleGameScreen", InputActionType.Button);
        toggleAction.AddBinding("<XRController>{RightHand}/{TriggerButton}");
        toggleAction.AddBinding("<Keyboard>/p");
        toggleAction.performed += OnToggle;
        toggleAction.Enable();
    }

    private void Start()
    {
        if (rayDroit == null)
            rayDroit = TrouverRayDroit();

        SetVisible(visibleAuDemarrage);
    }

    private void OnToggle(InputAction.CallbackContext ctx)
    {
        // Si l'écran est ouvert et que le rayon droit vise un élément cliquable
        // (slider, bouton…), la gâchette sert à l'utiliser au lieu de fermer l'écran.
        // Viser le fond du panneau ne bloque plus la fermeture.
        if (visible && RayOnInteractableUI())
            return;

        SetVisible(!visible);
    }

    private bool RayOnInteractableUI()
    {
        if (rayDroit == null || !rayDroit.isActiveAndEnabled)
            return false;

        if (!rayDroit.TryGetCurrentUIRaycastResult(out RaycastResult result) || result.gameObject == null)
            return false;

        Selectable selectable = result.gameObject.GetComponentInParent<Selectable>();
        return selectable != null && selectable.IsInteractable();
    }

    private void SetVisible(bool v)
    {
        visible = v;

        if (gameScreen != null)
            gameScreen.SetActive(v);

        if (autresEcrans != null)
        {
            foreach (var go in autresEcrans)
            {
                if (go != null)
                    go.SetActive(v);
            }
        }
    }

    private static XRRayInteractor TrouverRayDroit()
    {
        foreach (var ray in FindObjectsOfType<XRRayInteractor>())
        {
            var t = ray.transform;
            while (t != null)
            {
                if (t.name.Contains("Right"))
                    return ray;
                t = t.parent;
            }
        }
        return null;
    }

    private void OnDestroy()
    {
        if (toggleAction != null)
        {
            toggleAction.performed -= OnToggle;
            toggleAction.Disable();
            toggleAction.Dispose();
        }
    }
}
