using System.Collections;
using System.Collections.Generic;
using ConnectedComponentLabeling;
using Unity.Netcode;
using UnityEngine;

/*
TODO:
Workers
Alt patterns
*/

public class TextureManager : NetworkBehaviour
{
    public Color transparent =  new(0, 0, 0, 0);

    public int width = 80; 
    public int height = 45;

    Camera mainCamera;
    public Texture2D texture;
    Vector2Int anchorPixel;

    // [HideInInspector]
    // public NetworkVariable<Color>[] pixels;
    public Color[] pixels;
    public Color[] scratch;
    public float[] samples;
    // public NetworkPixels pixels;
    // public Sample[][] samples;

    public GameObject sequencePrefab;

    public Renderer background;

    private CCLBlobDetector ccl = new();

    public List<List<int>> blobs = new();

    private void Awake() 
    {
        Services.textureManager = this;
        anchorPixel = new Vector2Int(-1, -1);
        mainCamera = Camera.main;

        texture = new(width, height)
        {
            filterMode = FilterMode.Point
        };

        ccl.Initialize(texture);

        GetComponent<Renderer>().material.mainTexture = texture;

        // Background texture setup
        var backgroundTexture = new Texture2D(width, height)
        {
            filterMode = FilterMode.Point
        };
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                backgroundTexture.SetPixel(x, y, ((x + y) % 2) == 0 ? Color.white : Color.gray);
            }
        }
        backgroundTexture.Apply();
        background.material.mainTexture = backgroundTexture;

        InitializePixels();
    }

    public void InitializePixels()
    {
        pixels = new Color[width * height];
        scratch = new Color[width * height];
        samples = new float[width * height];
        // pixels = new NetworkPixels(width * height, this);
        // Debug.Log("Init");
        // pixels = new NetworkVariable<Color>[width * height];

        // for (int i = 0; i < pixels.Length; i++)
        // {
        //     pixels[i] = new NetworkVariable<Color>();
        //     pixels[i].Initialize(this);
        //     if (!NetworkManager.Singleton.IsServer)
        //     {
        //         int p = i;
        //         pixels[i].OnValueChanged += (previousValue, newValue) => {
        //             // Debug.Log(i);
        //             texture.SetPixel(p % width, p / width, newValue);
        //             // Debug.Log($"{p % width}, {p / width}, {newValue}");
        //         };
        //     }
        // }
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log("Texture spawn");
        ResetTexture();
    }

    [Rpc(SendTo.NotServer)]
    public void DrawRpc(Vector2Int from, Vector2Int to, Color[] pixels)
    {
        // Debug.Log("Draw");
        Apply(from, to, (x, y, rect, i) => {
            Color c = pixels[i];
            SetPixel(x, y, c);
            return c;
        });
    }

    // Update is called once per frame
    void Update()
    {
        if (!NetworkManager.Singleton.IsServer) 
        {   
            // for (int x = 0; x < width; x++)
            // {
            //     for (int y = 0; y < height; y++)
            //     {
            //         SetPixel(x, y, pixels[x + y * width].Value);
            //     }
            // }
            texture.Apply();
            return;
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            ResetTexture();
        }

        Vector3 viewportPos = mainCamera.ScreenToViewportPoint(Input.mousePosition);
        int mouseX = Mathf.Clamp((int)(viewportPos.x * texture.width), 0, width - 1);
        int mouseY = Mathf.Clamp((int)(viewportPos.y * texture.height), 0, height - 1);
        Vector2Int mousePixel = new(mouseX, mouseY);

        if (Input.GetMouseButtonDown(0)) 
        {
            anchorPixel = mousePixel;
        }

        if (Input.GetMouseButtonUp(0) && anchorPixel.x >= 0)
        {
            int size = (Mathf.Abs(anchorPixel.x - mousePixel.x) + 1) * (Mathf.Abs(anchorPixel.y - mousePixel.y) + 1);
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                ResetArea(anchorPixel, mousePixel);
                UpdateBlobs();
            }
            else 
            {
                Apply(anchorPixel, mousePixel, (x, y, rect, i) => {
                    int f = x + y * width;
                    float phase = (float)i / size * 2f * Mathf.PI;
                    Color c = AudioManager.PhaseAmpToColor(phase, 1f);
                    SetPixel(x, y, c);
                    return c;
                }, true);
            }
        }
        texture.Apply();
    }

    public void SetPixel(int x, int y, Color c)
    {
        texture.SetPixel(x, y, c);
        if (NetworkManager.Singleton.IsServer) 
        {
            // Debug.Log(pixels != null);
            int i = x + y * width;
            pixels[i] = c;
            samples[i] = AudioManager.ColorToSample(c);
            ccl.SetPixel(x, y, c.a > 0);
        }
    }

    public void Apply(Vector2Int from, Vector2Int to, System.Func<int, int, RectInt, int, Color> action, bool updateBlobs = false)
    {
        // Debug.Log($"{from} {to}");
        int xStart = Mathf.Clamp(Mathf.Min(from.x, to.x), 0, width - 1);
        int xEnd =  Mathf.Clamp(Mathf.Max(from.x, to.x), 0, width - 1);
        int yStart =  Mathf.Clamp(Mathf.Min(from.y, to.y), 0, height - 1);
        int yEnd =  Mathf.Clamp(Mathf.Max(from.y, to.y), 0, height - 1);
        int w = xEnd - xStart + 1;
        int h = yEnd - yStart + 1;
        RectInt rect = new(xStart, yStart, w, h);
        Color[] colors = new Color[w * h];
        int i = 0;

        for (int y = yStart; y <= yEnd; y++)
        {
            for (int x = xStart; x <= xEnd; x++)
            {   
               colors[i] = action(x, y, rect, i);
               i++;
            }
        }

        if (updateBlobs) UpdateBlobs();
        if (NetworkManager.Singleton.IsServer) 
        {
            // Debug.Log("server");
            DrawRpc(from, to, colors);
        }
        // int i = 0;
        // foreach (var blob in blobs)
        // {
        //     foreach (int pixel in blob)
        //     {
        //         Debug.Log($"{i} {pixel}");
        //     }
        //     i++;
        // }
    }

    public void UpdateBlobs()
    {
        blobs = ccl.GetBlobs();
    }

    // private void SetArea(Vector2Int from, Vector2Int to, Color c, bool apply = true) => SetArea(from, to, (x, y, rect) => c, apply);

    private void ResetTexture() 
    {
        ResetArea(Vector2Int.zero, new Vector2Int(width - 1, height - 1));
        texture.Apply();
    } 

    public void ResetArea(Vector2Int from, Vector2Int to) => SetArea(from, to, transparent);

    public void SetArea(Vector2Int from, Vector2Int to, Color c)
    {
        Apply(from, to, (x, y, rect, i) => 
        {
            SetPixel(x, y, c); 
            return c;
        });
    }
}
