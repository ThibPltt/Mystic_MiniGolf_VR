using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class ContinuerQuitter : MonoBehaviour
{
    public ActionBasedSnapTurnProvider Continuer;
    public ActionBasedContinuousTurnProvider Quitter;

    public void SetTypeFromIndex(int index)
    {

        if (index == 0)
        {

            Continuer.enabled = false;
            Quitter.enabled = false;

        }
        else if (index == 1)
        {

            Continuer.enabled = true;
            Quitter.enabled = false;
        }
    }
}