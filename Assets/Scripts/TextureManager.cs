using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

/*
TODO:
Workers
Erasing
Alt patterns
Networking.
*/

public class TextureManager : MonoBehaviour
{
    public int width = 80; 
    public int height = 45;

    Camera mainCamera;
    Texture2D texture;
    Vector2Int anchorPixel;

    public Color[] flat;

    public GameObject sequencePrefab;

    private List<Worker> workers;

    private Color transparentBlack = new(0, 0, 0, 0);

    public GameObject backgroundObject;
    Texture2D background;

    private void Awake() 
    {
        Services.textureManager = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        anchorPixel = new Vector2Int(-1, -1);

        mainCamera = Camera.main;

        texture = new(width, height)
        {
            filterMode = FilterMode.Point
        };
        background = new(width, height)
        {
            filterMode = FilterMode.Point
        };
        flat = new Color[width * height];

        ResetTexture();

        GetComponent<Renderer>().material.mainTexture = texture;
        backgroundObject.GetComponent<Renderer>().material.mainTexture = background;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                background.SetPixel(x, y, ((x + y) % 2) == 0 ? Color.white : Color.gray); 
            }
        }
        background.Apply();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            ResetTexture();
        }

        Vector3 viewportPos = mainCamera.ScreenToViewportPoint(Input.mousePosition);
        Vector2Int mousePixel = new((int)(viewportPos.x * texture.width), (int)(viewportPos.y * texture.height));

        if (Input.GetMouseButtonDown(0)) 
        {
            anchorPixel = mousePixel;
        }

        if (Input.GetMouseButtonUp(0))
        {
            int size = (Mathf.Abs(anchorPixel.x - mousePixel.x) + 1) * (Mathf.Abs(anchorPixel.y - mousePixel.y) + 1);
            Apply(anchorPixel, mousePixel, (x, y, rect) => {
                int f = x + y * width;
                int i = (x - rect.x) + (y - rect.y) * rect.width;

                float phase = (float)i / size * 2f * Mathf.PI;
                SetPixel(x, y, AudioManager.PhaseAmpToColor(phase, 1f));
            });
        }
    }

    private void SetPixel(int x, int y, Color c)
    {
        texture.SetPixel(x, y, c);
        flat[x + y * width] = c;
    }

    private void Apply(Vector2Int from, Vector2Int to, Action<int, int, RectInt> action)
    {
        int xStart = Math.Min(from.x, to.x);
        int xEnd = Math.Max(from.x, to.x);
        int yStart = Math.Min(from.y, to.y);
        int yEnd = Math.Max(from.y, to.y);
        int w = xEnd - xStart + 1;
        int h = yEnd - yStart + 1;
        for (int x = xStart; x <= xEnd; x++)
            for (int y = yStart; y <= yEnd; y++)
                action(x, y, new RectInt(xStart, yStart, w, h));
        texture.Apply();
    }

    // private void SetArea(Vector2Int from, Vector2Int to, Color c, bool apply = true) => SetArea(from, to, (x, y, rect) => c, apply);

    private void ResetTexture() 
    {
        ResetArea(Vector2Int.zero, new Vector2Int(width - 1, height - 1));
        texture.Apply();
    } 

    private void ResetArea(Vector2Int from, Vector2Int to) 
    {
        Apply(from, to, (x, y, rect) => 
        {
            SetPixel(x, y, transparentBlack); 
            // SetPixel(x, y, ((x + y) % 2) == 0 ? Color.white : Color.gray); 
        });
    } 
}
