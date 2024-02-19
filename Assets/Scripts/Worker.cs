using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;

public class Worker : NetworkBehaviour
{
    public Vector2Int readPosition = new(0, 0);
    public NetworkVariable<Vector2Int> Position = new();
    public Vector2Int readSize = new(1, 1);
    public Vector2Int maxSize = new(8, 8);
    public NetworkVariable<Vector2Int> Size = new();
    public NetworkVariable<int> ColorId = new();

    private void Awake() 
    {

    }

    public override void OnNetworkSpawn()
    {
        Services.textureManager.workers.Add(this);
        if (IsOwner)
        {
            ColorId.Value = 1;
            Position.Value = readPosition;
            Size.Value = readSize;
            Services.textureManager.localWorker = this;
        }
        Position.OnValueChanged += (previousValue, newValue) => {
            readPosition = newValue;
            Services.textureManager.UpdatePlayers();
        };
        Size.OnValueChanged += (previousValue, newValue) => {
            readSize = newValue;
            Services.textureManager.UpdatePlayers();
        };
        ColorId.OnValueChanged += (previousValue, newValue) => {
            Services.textureManager.UpdatePlayers();
        };
        Services.textureManager.UpdatePlayers();
    }

    public void Move(int x, int y, bool shift)
    {
        int width = Services.textureManager.width;
        int height = Services.textureManager.height;
        if (shift)
        {
            // if (move.y == 0) 
            // {
            //     int dir = move.x > 0 ? size.x : -1;
            //     int edge = Position.Value.x + dir;
            //     int end = width;
            //     int inc = System.Math.Sign(dir);
            //     for (int y = Position.Value.y; y < Mathf.Min(Position.Value.y + size.y, height); y++)
            //     {
            //         int x;
            //         for (x = edge; x >= 0 && x < end; x += inc)
            //         {
            //             int p = x + y * width;
            //             if (Services.textureManager.pixels[p].a == 0) break;
            //             Services.textureManager.scratch[p] = Services.textureManager.pixels[p];
            //         }
            //         if (x != edge)
            //         {
            //             Services.textureManager.Apply(new Vector2Int(edge + inc, y), new Vector2Int(x, y), (xp, yp, rect, i) => {
            //                 Color c = Services.textureManager.scratch[(xp - inc) + yp * width];
            //                 Services.textureManager.SetPixel(xp, yp, c);
            //                 return c;
            //             });
            //         }
            //     }
            // }
            // else 
            // {
            //     int dir = move.y > 0 ? size.y : -1;
            //     int edge = Position.Value.y + dir;
            //     int end = height;
            //     int inc = System.Math.Sign(dir);
            //     for (int x = Position.Value.x; x < Mathf.Min(Position.Value.x + size.x, width); x++)
            //     {
            //         int y;
            //         for (y = edge; y >= 0 && y < end; y += inc)
            //         {
            //             int p = x + y * width;
            //             if (Services.textureManager.pixels[p].a == 0) break;
            //             Services.textureManager.scratch[p] = Services.textureManager.pixels[p];
            //         }
            //         if (y != edge)
            //         {
            //             Services.textureManager.Apply(new Vector2Int(x, edge + inc), new Vector2Int(x, y), (xp, yp, rect, i) => {
            //                 Color c = Services.textureManager.scratch[xp + (yp - inc) * width];
            //                 Services.textureManager.SetPixel(xp, yp, c);
            //                 return c;
            //             });
            //         }
            //     }
            // }
        }
        Position.Value = new Vector2Int(Position.Value.x + x, Position.Value.y + y);
        if (Input.GetKey(KeyCode.Space))
            PaintRpc(Position.Value.x, Position.Value.y, Size.Value.x, Size.Value.y, ColorId.Value);
        // Services.textureManager.UpdateBlobs();
    }

    private void Grow(int x, int y)
    {
        Size.Value = new Vector2Int(Mathf.Clamp(Size.Value.x + x, 1, maxSize.x), Mathf.Clamp(Size.Value.y + y, 1, maxSize.y));
        if (Input.GetKey(KeyCode.Space))
            PaintRpc(Position.Value.x, Position.Value.y, Size.Value.x, Size.Value.y, ColorId.Value);
        // Services.textureManager.UpdateBlobs();
    }

    [Rpc(SendTo.Everyone)]
    private void PaintRpc(int x, int y, int w, int h, int cId)
    {
        var colors = new Color32[w * h];
        for (int i = 0; i < w * h; i++)
        {
            colors[i] = GetColor(cId, (float)i / (w * h));
        }
        Services.textureManager.PaintCanvasArea(x, y, w, h, colors);
    }

    private void Update() 
    {
        if (!IsOwner) return;
        
        if (Input.GetKeyDown(KeyCode.Alpha0)) ColorId.Value = 0;
        else if (Input.GetKeyDown(KeyCode.Alpha1)) ColorId.Value = 1;
        else if (Input.GetKeyDown(KeyCode.Alpha2)) ColorId.Value = 2;
        else if (Input.GetKeyDown(KeyCode.Alpha3)) ColorId.Value = 3;
        else if (Input.GetKeyDown(KeyCode.Alpha4)) ColorId.Value = 4;
        else if (Input.GetKeyDown(KeyCode.Alpha5)) ColorId.Value = 5;
        else if (Input.GetKeyDown(KeyCode.Alpha6)) ColorId.Value = 6;
        else if (Input.GetKeyDown(KeyCode.Alpha7)) ColorId.Value = 7;
        else if (Input.GetKeyDown(KeyCode.Alpha8)) ColorId.Value = 8;
        else if (Input.GetKeyDown(KeyCode.Alpha9)) ColorId.Value = 9;
        else if (Input.GetKeyDown(KeyCode.Q)) ColorId.Value = 10;

        if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
        {
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                Grow(0, 1);
            }
            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                Grow(0, -1);
            }
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                Grow(-1, 0);
            }
            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                Grow(1, 0);
            }
        }
        else 
        {
            bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
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
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            PaintRpc(Position.Value.x, Position.Value.y, Size.Value.x, Size.Value.y, ColorId.Value);
        }
    }

    public Color32 GetColor(float p) 
    {
        if (ColorId.Value == 10) return Color.HSVToRGB(p, 1f, 1f);
        return Services.paletteManager.colors[Mathf.Clamp(ColorId.Value, 0, Services.paletteManager.colors.Count - 1)];
    }

    public Color32 GetColor(int cId, float p) 
    {
        if (cId == 10) return Color.HSVToRGB(p, 1f, 1f);
        return Services.paletteManager.colors[Mathf.Clamp(cId, 0, Services.paletteManager.colors.Count - 1)];
    }
}
