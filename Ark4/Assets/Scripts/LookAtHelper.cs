using UnityEngine;

public class LookAtHelper : MonoBehaviour
{
    public Transform target;

    private void Start()
    {
        transform.LookAt(target);
    }
}