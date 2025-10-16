using UnityEngine;

public class Rotate : MonoBehaviour
{
    public bool Enable;

    void Update()
    {
        if (Enable)
        {
            transform.RotateAround(Vector3.zero, Vector3.up, -Time.deltaTime * 20.0f);
        }
    }
}
