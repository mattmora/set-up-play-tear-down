using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Cursor : MonoBehaviour
{
    Camera mainCamera;

    private void Awake() 
    {
        Services.cursor = this;
        mainCamera = Camera.main;
    }

    private void Start() 
    {
        UpdatePosition();
    }

    // Update is called once per frame
    void Update()
    {
        UpdatePosition();
    }

    private void UpdatePosition() 
    {
        if (NetworkManager.Singleton.IsServer)
        {
            Vector3 mouse = Input.mousePosition;
            mouse.z = 1;
            transform.position = mainCamera.ScreenToWorldPoint(mouse);
        }
        else 
        {
            transform.position = Vector3.one * -1000;
        }
    }
}
