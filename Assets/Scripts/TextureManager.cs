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

    public Color32[] canvasPixels;
    public Color32[] playersPixels;
    private Color32[] overlayPixels;
    
    public float[] canvasSamples;
    public float[] playersSamples;

    public GameObject sequencePrefab;

    public Renderer overlay;
    public Renderer players;

    private CCLBlobDetector canvasCCL = new();
    private CCLBlobDetector playersCCL = new();

    public List<int> canvasBlob = new();
    public List<int> playersBlob = new();

    public Worker localWorker;
    public HashSet<Worker> workers;

    public Texture2D image;

    public GameObject projection;

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

    private void Update() 
    {
        if (Input.GetKeyDown(KeyCode.G))
        {
            overlay.gameObject.SetActive(!overlay.gameObject.activeSelf);
        }    
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
        overlayPixels = new Color32[width * height];
    }

    public void UpdatePlayer(Worker w)
    {
        Array.Clear(playersPixels, 0, playersPixels.Length);
        playersCCL.ResetPixels();
        foreach (var worker in workers)
        {
            var position = worker.Position.Value;
            var size = worker.Size.Value;
            
            float i = 0;
            bool odd = false;
            for (int y = position.y; y < position.y + size.y; y++)
            {
                if (y < 0 || height <= y) 
                {
                    i += size.x;
                    continue;
                }
                for (int x = position.x; x < position.x + size.x; x++)
                {
                    int xr = odd ? (position.x + size.x - 1) - (x - position.x) : x;
                    if (xr < 0 || width <= xr) 
                    {
                        i++;
                        continue;
                    }
                    int p = xr + y * width;
                    Color c = worker.GetColor(i / (size.x * size.y));
                    playersPixels[p] = c;
                    playersSamples[p] = AudioManager.ColorToSample(c);
                    playersCCL.SetPixel(p, c.a > 0.8f);
                    i++;
                }
                // odd = !odd;
            }
        }
        playersTexture.SetPixels32(playersPixels);
        playersTexture.Apply();
        UpdateBlobs();
    }

    public void SetGuide(int colorId)
    {
        if (colorId > 9)
        {
            Array.Clear(overlayPixels, 0, overlayPixels.Length);
            overlayTexture.SetPixels32(overlayPixels);
            return;
        }

        int i = 0;
        foreach (Color c in image.GetPixels())
        {
            Color p = Services.paletteManager.colors[colorId];
            if (c.Equals(p))
            {
                Color.RGBToHSV(p, out float H, out float S, out float V);
                Color g = Color.HSVToRGB(H, S, V < 0.5 ? 1 - V : V);
                g.a = 0.5f;
                overlayPixels[i] = g;
            }
            else
            {
                overlayPixels[i] = transparent;
            }
            i++;
        }

        overlayTexture.SetPixels32(overlayPixels);
        overlayTexture.Apply();
    }

    public void PaintCanvasArea(int x0, int y0, int w, int h, Color32[] colors, bool updateBlobs)
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
                canvasCCL.SetPixel(p, canvasPixels[p].a > 200);
                clampedColors[c] = colors[i];
                i++;
                c++;
            }
        }
        texture.SetPixels32(rect.Left, rect.Top, rect.Width, rect.Height, clampedColors);
        texture.Apply();
        if (updateBlobs) UpdateBlobs();
    }

    public void UpdateBlobs()
    {
        var position = localWorker.Position.Value;

        int x = Mathf.Clamp(position.x, 0, width - 1);
        int y = Mathf.Clamp(position.y, 0, height - 1);
        
        int i = x + y * width;
        playersBlob = playersCCL.GetBlob(i);
        canvasBlob = canvasCCL.GetBlob(i);
    }
}
