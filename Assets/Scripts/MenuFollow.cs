using Unity.VisualScripting;
using UnityEngine;

public class MenuFollow : MonoBehaviour

{

    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float distance = 3.0f;

    private bool isCentered = false;

    private void OnBecameInvisible()
    {
        isCentered = false;
    }

    private void Update()
    {
       if(isCentered)
       {

            Vector3 targetPostition = FindTargetPosition();

            MoveTowards(targetPostition);

            if(ReachedPosition(targetPostition))
            {

                isCentered = true;
            }
       }
    }

    private Vector3 FindTargetPosition()
    {
        return cameraTransform.position + (cameraTransform.forward * distance);
    }

    private void MoveTowards(Vector3 targetPosition)
    {
        transform.position += (targetPosition - transform.position) * 0.025f;
    }

    private bool ReachedPosition(Vector3 targetPosition)
    {
        return Vector3.Distance(targetPosition, transform.position) < 0.1f;
    }
}