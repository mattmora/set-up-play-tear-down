using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoSizeCamera : MonoBehaviour
{
   [SerializeField] private float baseSize = 4.5f;

    private Camera cam;

    private void Awake() {
        cam = GetComponent<Camera>();
    }

    // Update is called once per frame
    void Update()
    {
        cam.orthographicSize = Mathf.Max((16f / 9f) * ((float)Screen.height / Screen.width) * baseSize, baseSize);
    }
}
