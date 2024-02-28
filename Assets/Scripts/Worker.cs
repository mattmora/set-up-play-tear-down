using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;

public class Worker : NetworkBehaviour
{
    public Vector2Int readPosition = new(0, 0);
    public NetworkVariable<Vector2Int> Position = new(Vector2Int.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public Vector2Int readSize = new(1, 1);
    public Vector2Int maxSize = new(8, 8);
    public NetworkVariable<Vector2Int> Size = new(Vector2Int.one, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> ColorId = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    float hInput, vInput;

    private GameObject projection;

    public override void OnNetworkSpawn()
    {
        Services.textureManager.workers.Add(this);
        if (IsOwner)
        {
            Position.Value = new(Services.textureManager.width / 2, Services.textureManager.height / 2);
            Size.Value = readSize;
            Services.textureManager.localWorker = this;
            Services.textureManager.UpdatePlayer(this);

            Camera.main.GetComponent<Outliner>().enabled = true;
            projection = GameObject.Find("Projection");
        }
        
        Position.OnValueChanged += (previousValue, newValue) => {
            readPosition = newValue;
            Services.textureManager.UpdatePlayer(this);
        };
        Size.OnValueChanged += (previousValue, newValue) => {
            readSize = newValue;
            Services.textureManager.UpdatePlayer(this);
        };
        ColorId.OnValueChanged += (previousValue, newValue) => {
             if (IsOwner) Services.textureManager.SetGuide(newValue);
            Services.textureManager.UpdatePlayer(this);
        };
    }

    public void Move(int x, int y, bool shift)
    {
        if (shift)
        {
            ShiftRpc(Position.Value.x, Position.Value.y, Size.Value.x, Size.Value.y, x, y);
        }
        Position.Value = new Vector2Int(Position.Value.x + x, Position.Value.y + y);
        if (Input.GetKey(KeyCode.Space))
        {
            PaintRpc(Position.Value.x, Position.Value.y, Size.Value.x, Size.Value.y, ColorId.Value);
        }
        // Services.textureManager.UpdateBlobs();
    }

    private void Grow(int x, int y, bool inverse)
    {
        if (inverse)
        {
            var previous = Size.Value;
            Size.Value = new Vector2Int(Mathf.Clamp(Size.Value.x - x, 1, maxSize.x), Mathf.Clamp(Size.Value.y - y, 1, maxSize.y));
            var delta = Size.Value - previous;
            Position.Value = new Vector2Int(Position.Value.x - delta.x, Position.Value.y - delta.y);
        }
        else
        {
            Size.Value = new Vector2Int(Mathf.Clamp(Size.Value.x + x, 1, maxSize.x), Mathf.Clamp(Size.Value.y + y, 1, maxSize.y));
        }
        if (Input.GetKey(KeyCode.Space))
            PaintRpc(Position.Value.x, Position.Value.y, Size.Value.x, Size.Value.y, ColorId.Value);
        // Services.textureManager.UpdateBlobs();
    }

    [Rpc(SendTo.Everyone)]
    private void PaintRpc(int x0, int y0, int w, int h, int cId)
    {
        var colors = new Color32[w * h];
        int i = 0;
        bool odd = false;
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int xr = odd ? (w - 1) - x : x;
                int p = xr + y * w;
                colors[p] = GetColor(cId, (float)i / (w * h));
                i++;
            }
            // odd = !odd;
        }
        Services.textureManager.PaintCanvasArea(x0, y0, w, h, colors, true);
    }

    [Rpc(SendTo.Everyone)]
    private void ShiftRpc(int bx, int by, int bw, int bh, int dx, int dy)
    {
        RectInt block = new(bx, by, bw, bh);
        int width = Services.textureManager.width;
        int height = Services.textureManager.height;
        List<Color32> colors = new();
        if (dy == 0) 
        {
            int dir = dx > 0 ? block.width : -1;
            int edge = block.x + dir;
            int end = width;
            int inc = Math.Sign(dir);
            for (int y = block.y; y < Mathf.Min(block.y + block.height, height); y++)
            {
                int x;
                if (dir > 0) colors.Add(Services.textureManager.transparent);
                for (x = edge; x >= 0 && x < end; x += inc)
                {
                    int p = x + y * width;
                    if (Services.textureManager.canvasPixels[p].a == 0) break;
                    colors.Add(Services.textureManager.canvasPixels[p]);
                }
                if (dir < 0) 
                {
                    colors.Reverse();
                    colors.Add(Services.textureManager.transparent);
                }
                if (x != edge)
                {
                    int xStart = Mathf.Min(edge, x);
                    int xEnd =  Mathf.Max(edge, x);
                    int w = xEnd - xStart + 1;
                    RectInt rect = new(xStart, y, w, 1);
                    // Debug.Log(rect);
                    Services.textureManager.PaintCanvasArea(rect.x, rect.y, rect.width, rect.height, colors.ToArray(), false);
                }
                colors.Clear();
            }
        }
        else 
        {
            int dir = dy > 0 ? block.height : -1;
            int edge = block.y + dir;
            int end = height;
            int inc = Math.Sign(dir);
            for (int x = block.x; x < Mathf.Min(block.x + block.width, width); x++)
            {
                int y;
                if (dir > 0) colors.Add(Services.textureManager.transparent);
                for (y = edge; y >= 0 && y < end; y += inc)
                {
                    int p = x + y * width;
                    if (Services.textureManager.canvasPixels[p].a == 0) break;
                    colors.Add(Services.textureManager.canvasPixels[p]);
                }
                if (dir < 0) 
                {
                    colors.Reverse();
                    colors.Add(Services.textureManager.transparent);
                }
                if (y != edge)
                {
                    int yStart =  Mathf.Min(edge, y);
                    int yEnd = Mathf.Max(edge, y);
                    int h = yEnd - yStart + 1;
                    RectInt rect = new(x, yStart, 1, h);
                    Services.textureManager.PaintCanvasArea(rect.x, rect.y, rect.width, rect.height, colors.ToArray(), false);
                }
                colors.Clear();
            }
        }
        Services.textureManager.UpdateBlobs();
    }

    private void Update() 
    {
        if (!IsOwner) return;

        if (IsHost)
        {
            if (Input.GetKeyDown(KeyCode.C))
            {
                PaintRpc(0, 0, Services.textureManager.width, Services.textureManager.height, 9);
            }
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            Position.Value = new(Services.textureManager.width / 2, Services.textureManager.height / 2);
        }

        float h = Input.GetAxisRaw("Horizontal") * Time.deltaTime * 5;
        float v = Input.GetAxisRaw("Vertical") * Time.deltaTime * 5;
        hInput = h == 0f ? 0f : hInput + h;
        vInput = v == 0f ? 0f : vInput + v;
        int hI = (int)hInput;
        int vI = (int)vInput;
        hInput -= hI;
        vInput -= vI;

        if (Input.GetKeyDown(KeyCode.O)) 
        {
            var outliner = Camera.main.GetComponent<Outliner>();
            outliner.enabled = !outliner.enabled;
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            projection.SetActive(!projection.activeSelf);
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha0)) ColorId.Value = 9;
        else if (Input.GetKeyDown(KeyCode.Alpha1)) ColorId.Value = 0;
        else if (Input.GetKeyDown(KeyCode.Alpha2)) ColorId.Value = 1;
        else if (Input.GetKeyDown(KeyCode.Alpha3)) ColorId.Value = 2;
        else if (Input.GetKeyDown(KeyCode.Alpha4)) ColorId.Value = 3;
        else if (Input.GetKeyDown(KeyCode.Alpha5)) ColorId.Value = 4;
        else if (Input.GetKeyDown(KeyCode.Alpha6)) ColorId.Value = 5;
        else if (Input.GetKeyDown(KeyCode.Alpha7)) ColorId.Value = 6;
        else if (Input.GetKeyDown(KeyCode.Alpha8)) ColorId.Value = 7;
        else if (Input.GetKeyDown(KeyCode.Alpha9)) ColorId.Value = 8;
        else if (Input.GetKeyDown(KeyCode.Q)) ColorId.Value = 10;

        bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
        {
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                Grow(0, 1, shift);
            }
            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                Grow(0, -1, shift);
            }
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                Grow(-1, 0, shift);
            }
            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                Grow(1, 0, shift);
            }
            if (hI != 0 || vI != 0)
            {
                Grow(hI, vI, shift);
            }
        }
        else 
        {
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                Move(0, 1, shift);
            }
            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                Move(0, -1, shift);
            }
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                Move(-1, 0, shift);
            }
            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                Move(1, 0, shift);
            }
            if (hI != 0 || vI != 0)
            {
                Move(hI, vI, shift);
            }
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            PaintRpc(Position.Value.x, Position.Value.y, Size.Value.x, Size.Value.y, ColorId.Value);
        }
    }

    public Color32 GetColor(float p) => GetColor(ColorId.Value, p);

    public Color32 GetColor(int cId, float p) 
    {
        if (cId == 10) return Color.HSVToRGB(p, 1f, 1f);
        return Services.paletteManager.colors[Mathf.Clamp(cId, 0, Services.paletteManager.colors.Count - 1)];
    }
}
