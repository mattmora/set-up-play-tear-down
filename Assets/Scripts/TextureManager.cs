using System;
using System.Collections;
using System.Collections.Generic;
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

    public Sample[][] samples;

    public GameObject sequencePrefab;

    private List<Worker> workers;

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
        flat = new Color[width * height];
        samples = new Sample[width][];
        for (int i = 0; i < width; i++)
        {
            samples[i] = new Sample[height];
        }

        ResetTexture();

        GetComponent<Renderer>().material.mainTexture = texture;
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
            var makeSequence = Input.GetKey(KeyCode.S);
            int size = (Mathf.Abs(anchorPixel.x - mousePixel.x) + 1) * (Mathf.Abs(anchorPixel.y - mousePixel.y) + 1);
            SampleSequence seq = null;
            // if (makeSequence) 
            // {
            //     seq = Instantiate(sequencePrefab).GetComponent<SampleSequence>();
            //     seq.Initialize(size);
            // }
            Apply(anchorPixel, mousePixel, (x, y, rect) => {
                int f = x + y * width;
                int i = (x - rect.x) + (y - rect.y) * rect.width;
                // if (makeSequence) 
                // {
                //     Vector3 p = new((x + 0.5f) / texture.width, (y + 0.5f) / texture.height, 1);
                //     seq.line.SetPosition(i, mainCamera.ViewportToWorldPoint(p));
                //     seq[i] = f;
                // }
                // if (Input.GetKey(KeyCode.D)) 
                // {
                    float phase = (float)i / size * 2f * Mathf.PI;
                    SetPixel(x, y, AudioManager.PhaseAmpToColor(phase, 1f));
                // }
            });
            texture.Apply();
        }

        // Vector3 cursorPosition = new((float)x / texture.width, (float)y / texture.height, 1);
        // Debug.Log(cursorPosition);
        // Services.cursor.transform.position = mainCamera.ViewportToWorldPoint(cursorPosition);

        //for (int y = 0; y < texture.height; y++)
        //{
        //    for (int x = 0; x < texture.width; x++)
        //    {
        //        int camX = (int)(((float)x / texture.width) * camTextures[currentCam].width);
        //        int camY = (int)(((float)y / texture.height) * camTextures[currentCam].height);
        //        Vector2Int pos = new Vector2Int(x, y);
        //        if (playerPixels.Contains(pos))
        //        {
        //            texture.SetPixel(x, y, playerColors[pos]);
        //        }
        //        else
        //        {
        //            texture.SetPixel(x, y, camActive ? camTextures[currentCam].GetPixel(camX, camY) : Color.black);
        //        }
        //    }
        //}
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
            SetPixel(x, y, ((x + y) % 2) == 0 ? Color.white : Color.gray); 
        });
    } 
}
