using System;
using System.Collections;
using System.Collections.Generic;
using ConnectedComponentLabeling;
using Unity.Netcode;
using UnityEngine;


public class TextureManager : MonoBehaviour
{
    public Color transparent =  new(0, 0, 0, 0);

    public int width = 80; 
    public int height = 45;

    Camera mainCamera;

    [HideInInspector]
    public Texture2D texture;
    private Texture2D playersTexture;
    private Texture2D overlayTexture;

    Vector2Int anchorPixel;

    // [HideInInspector]
    // public NetworkVariable<Color>[] pixels;
    public Color32[] canvasPixels;
    public Color32[] playersPixels;
    
    public float[] canvasSamples;
    public float[] playersSamples;
    // public NetworkPixels pixels;
    // public Sample[][] samples;

    public GameObject sequencePrefab;

    public Renderer overlay;
    public Renderer players;

    private CCLBlobDetector canvasCCL = new();
    private CCLBlobDetector playersCCL = new();

    public List<int> canvasBlob = new();
    public List<int> playersBlob = new();

    public Worker localWorker;
    public HashSet<Worker> workers;

    private void Awake() 
    {
        Services.textureManager = this;
        anchorPixel = new Vector2Int(-1, -1);
        mainCamera = Camera.main;
        workers = new();

        InitializePixels();

        texture = SetupTexture(GetComponent<Renderer>(), (x, y) => transparent);
        overlayTexture = SetupTexture(overlay, (x, y) => transparent);
        playersTexture = SetupTexture(players, (x, y) => transparent);

        canvasCCL.Initialize(texture);
        playersCCL.Initialize(playersTexture);
        canvasCCL.fallback = playersCCL;
    }

    private Texture2D SetupTexture(Renderer r, Func<int, int, Color> color)
    {
        var texture = new Texture2D(width, height)
        {
            filterMode = FilterMode.Point
        };
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                texture.SetPixel(x, y, color(x, y));
            }
        }
        texture.Apply();
        r.material.mainTexture = texture;
        return texture;
    }

    public void InitializePixels()
    {
        canvasPixels = new Color32[width * height];
        playersPixels = new Color32[width * height];
        canvasSamples = new float[width * height];
        playersSamples = new float[width * height];
    }

    // [Rpc(SendTo.NotServer)]
    // public void DrawRpc(Vector2Int from, Vector2Int to, Color[] pixels)
    // {
    //     // Debug.Log("Draw");
    //     Apply(from, to, (x, y, rect, i) => {
    //         Color c = pixels[i];
    //         SetPixel(x, y, c);
    //         return c;
    //     });
    // }

    // Update is called once per frame
    void Update()
    {
        // if (Input.GetKeyDown(KeyCode.C))
        // {
        //     ResetTexture();
        // }

        // Vector3 viewportPos = mainCamera.ScreenToViewportPoint(Input.mousePosition);
        // int mouseX = Mathf.Clamp((int)(viewportPos.x * texture.width), 0, width - 1);
        // int mouseY = Mathf.Clamp((int)(viewportPos.y * texture.height), 0, height - 1);
        // Vector2Int mousePixel = new(mouseX, mouseY);

        // if (Input.GetMouseButtonDown(0)) 
        // {
        //     anchorPixel = mousePixel;
        // }

        // if (Input.GetMouseButtonUp(0) && anchorPixel.x >= 0)
        // {
        //     int size = (Mathf.Abs(anchorPixel.x - mousePixel.x) + 1) * (Mathf.Abs(anchorPixel.y - mousePixel.y) + 1);
        //     if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        //     {
        //         ResetArea(anchorPixel, mousePixel);
        //         UpdateBlobs();
        //     }
        //     else 
        //     {
        //         Apply(anchorPixel, mousePixel, (x, y, rect, i) => {
        //             int f = x + y * width;
        //             float phase = (float)i / size * 2f * Mathf.PI;
        //             Color c = AudioManager.PhaseAmpToColor(phase, 1f);
        //             SetPixel(x, y, c);
        //             return c;
        //         }, true);
        //     }
        // }
        // texture.Apply();
        // overlayTexture.Apply();
    }

    public void UpdatePlayers()
    {
        Array.Clear(playersPixels, 0, playersPixels.Length);
        playersCCL.ResetPixels();
        foreach (var worker in workers)
        {
            var position = worker.Position.Value;
            var size = worker.Size.Value;
            float i = 0;
            for (int y = position.y; y < position.y + size.y; y++)
            {
                if (y < 0 || height <= y) 
                {
                    i += size.x;
                    continue;
                }
                for (int x = position.x; x < position.x + size.x; x++)
                {
                    if (x < 0 || width <= x) 
                    {
                        i++;
                        continue;
                    }
                    int p = x + y * width;
                    Color c = worker.GetColor(i / (size.x * size.y));
                    playersPixels[p] = c;
                    playersSamples[p] = AudioManager.ColorToSample(c);
                    playersCCL.SetPixel(p, c.a > 0);
                    i++;
                }
            }
        }
        playersTexture.SetPixels32(playersPixels);
        playersTexture.Apply();
        UpdateBlobs();
    }

    public void PaintCanvasArea(int x0, int y0, int w, int h, Color32[] colors)
    {
        System.Drawing.Rectangle rect = new(x0, y0, w, h);
        System.Drawing.Rectangle bounds = new(0, 0, width, height);
        if (!rect.IntersectsWith(bounds)) return;

        rect.Intersect(bounds);

        var clampedColors = new Color32[rect.Width * rect.Height];
        int i = 0;
        int c = 0;
        for (int y = y0; y < y0 + h; y++)
        {
            if (y < 0 || height <= y) 
            {
                i += w;
                continue;
            }
            for (int x = x0; x < x0 + w; x++)
            {
                if (x < 0 || width <= x) 
                {
                    i++;
                    continue;
                }
                int p = x + y * width;
                canvasPixels[p] = colors[i];
                canvasSamples[p] = AudioManager.ColorToSample(colors[i]);
                canvasCCL.SetPixel(p, canvasPixels[p].a > 0);
                clampedColors[c] = colors[i];
                i++;
                c++;
            }
        }
        texture.SetPixels32(rect.Left, rect.Top, rect.Width, rect.Height, clampedColors);
        texture.Apply();
        UpdateBlobs();
    }

    // public void SetPixel(int x, int y, Color c)
    // {
    //     texture.SetPixel(x, y, c);
    //     if (NetworkManager.Singleton.IsServer) 
    //     {
    //         // Debug.Log(pixels != null);
    //         int i = x + y * width;
    //         pixels[i] = c;
    //         samples[i] = AudioManager.ColorToSample(c);
    //         ccl.SetPixel(x, y, c.a > 0);
    //     }
    // }

    // public void Apply(Vector2Int from, Vector2Int to, System.Func<int, int, RectInt, int, Color> action, bool updateBlobs = false)
    // {
    //     // Debug.Log($"{from} {to}");
    //     int xStart = Mathf.Clamp(Mathf.Min(from.x, to.x), 0, width - 1);
    //     int xEnd =  Mathf.Clamp(Mathf.Max(from.x, to.x), 0, width - 1);
    //     int yStart =  Mathf.Clamp(Mathf.Min(from.y, to.y), 0, height - 1);
    //     int yEnd =  Mathf.Clamp(Mathf.Max(from.y, to.y), 0, height - 1);
    //     int w = xEnd - xStart + 1;
    //     int h = yEnd - yStart + 1;
    //     RectInt rect = new(xStart, yStart, w, h);
    //     Color[] colors = new Color[w * h];
    //     int i = 0;

    //     for (int y = yStart; y <= yEnd; y++)
    //     {
    //         for (int x = xStart; x <= xEnd; x++)
    //         {   
    //            colors[i] = action(x, y, rect, i);
    //            i++;
    //         }
    //     }

    //     if (updateBlobs) UpdateBlobs();
    //     if (NetworkManager.Singleton.IsServer) 
    //     {
    //         // Debug.Log("server");
    //         DrawRpc(from, to, colors);
    //     }
    //     // int i = 0;
    //     // foreach (var blob in blobs)
    //     // {
    //     //     foreach (int pixel in blob)
    //     //     {
    //     //         Debug.Log($"{i} {pixel}");
    //     //     }
    //     //     i++;
    //     // }
    // }

    public void UpdateBlobs()
    {
        var position = localWorker.Position.Value;

        int x = Mathf.Clamp(position.x, 0, width - 1);
        int y = Mathf.Clamp(position.y, 0, height - 1);
        
        int i = x + y * width;
        playersBlob = playersCCL.GetBlob(i);
        canvasBlob = canvasCCL.GetBlob(i);
    }

    // private void SetArea(Vector2Int from, Vector2Int to, Color c, bool apply = true) => SetArea(from, to, (x, y, rect) => c, apply);

    // private void ResetTexture() 
    // {
    //     ResetArea(Vector2Int.zero, new Vector2Int(width - 1, height - 1));
    //     texture.Apply();
    // } 

    // public void ResetArea(Vector2Int from, Vector2Int to) => SetArea(from, to, transparent);

    // public void SetArea(Vector2Int from, Vector2Int to, Color c)
    // {
    //     Apply(from, to, (x, y, rect, i) => 
    //     {
    //         SetPixel(x, y, c); 
    //         return c;
    //     });
    // }
}
