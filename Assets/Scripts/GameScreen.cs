using UnityEngine;
using UnityEngine.InputSystem;

public class GameScreenController : MonoBehaviour
{
    public GameObject gameScreen; // Assignez l'objet écran de jeu dans l'inspecteur
    private bool isGameScreenActive = true;
    private InputAction triggerAction;
    private InputAction toggleMenuAction;

    void Start()
    {
        // Créer une nouvelle action d'entrée pour la gâchette
        triggerAction = new InputAction(type: InputActionType.Button, binding: "<XRController>{RightHand}/trigger");
        triggerAction.performed += ctx => ToggleGameScreen();
        triggerAction.Enable();

        // Créer une nouvelle action d'entrée pour la touche "P"
        toggleMenuAction = new InputAction(type: InputActionType.Button, binding: "<Keyboard>/p");
        toggleMenuAction.performed += ctx => ToggleGameScreen();
        toggleMenuAction.Enable();
    }

    private void ToggleGameScreen()
    {
        isGameScreenActive = !isGameScreenActive;
        gameScreen.SetActive(isGameScreenActive);
    }

    private void OnDestroy()
    {
        // Désactiver les actions d'entrée lorsque le script est détruit
        triggerAction.Disable();
        toggleMenuAction.Disable();
    }
}