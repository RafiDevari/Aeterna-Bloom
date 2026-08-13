using UnityEngine;

public class CameraDrag : MonoBehaviour
{
    private Vector3 dragOrigin;

    

    void Update()
    {   
        // Zoom dengan scroll mouse
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll != 0)
        {
            Camera.main.orthographicSize -= scroll * 5f;
            Camera.main.orthographicSize = Mathf.Clamp(Camera.main.orthographicSize, 2f, 15f);
        }
        if (Input.GetMouseButtonDown(1))
        {
            dragOrigin = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        }

        if (Input.GetMouseButton(1))
        {
            Vector3 difference = dragOrigin - Camera.main.ScreenToWorldPoint(Input.mousePosition);
            transform.position += difference;
        }
    }
}