using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class Worker : NetworkBehaviour
{
    public NetworkVariable<Vector2Int> Position = new NetworkVariable<Vector2Int>();
    public Vector2Int readPosition;
    public Vector2Int size;
    public Color color;

    private void Awake() 
    {
        Position.OnValueChanged += (previousValue, newValue) => {
            readPosition = newValue;
        };
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            StartCoroutine(PlaceRoutine());
        }
    }

    private IEnumerator PlaceRoutine()
    {
        if (!Services.textureManager.IsSpawned)
            yield return new WaitUntil(() => Services.textureManager.IsSpawned);
        Place();
    }

    public void Place()
    {
        var position = GetRandomPosition();
        if (NetworkManager.Singleton.IsServer)
        {
            ExecutePlace(position);
        }
        else
        {
            SubmitPlaceRequestServerRpc(position);
        }
    }

    public void Move(int x, int y, bool shift)
    {
        var move = new Vector2Int(x, y);
        if (NetworkManager.Singleton.IsServer)
        {
            // Debug.Log("Server Move");
            ExecuteMove(move, shift);
        }
        else
        {
            // Debug.Log("Client Move");
            SubmitMoveRequestServerRpc(move, shift);
        }
    }

    private void Update() {
        if (!IsOwner) return;
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

    [Rpc(SendTo.Server)]
    void SubmitPlaceRequestServerRpc(Vector2Int position) => ExecutePlace(position);

    private void ExecutePlace(Vector2Int position)
    {
        // Services.textureManager.ResetArea(Position.Value, Position.Value + size - Vector2Int.one);
        Position.Value = position;
        Services.textureManager.SetArea(Position.Value, Position.Value + size - Vector2Int.one, color);
        Services.textureManager.texture.Apply();
        if (NetworkManager.Singleton.IsServer) Services.textureManager.UpdateBlobs();
    }


    [Rpc(SendTo.Server)]
    void SubmitMoveRequestServerRpc(Vector2Int move, bool shift) => ExecuteMove(move, shift);

    private void ExecuteMove(Vector2Int move, bool shift)
    {
        int width = Services.textureManager.width;
        int height = Services.textureManager.height;
        if (shift)
        {
            if (move.y == 0) 
            {
                int dir = move.x > 0 ? size.x : -1;
                int edge = Position.Value.x + dir;
                int end = width;
                int inc = System.Math.Sign(dir);
                for (int l = 0; l < size.y; l++)
                {
                    int x;
                    int y = l + Position.Value.y;
                    for (x = edge; x >= 0 && x < end; x += inc)
                    {
                        int p = x + y * width;
                        if (Services.textureManager.pixels[p].a == 0) break;
                        Services.textureManager.scratch[p] = Services.textureManager.pixels[p];
                    }
                    if (x != edge)
                    {
                        Services.textureManager.Apply(new Vector2Int(edge + inc, y), new Vector2Int(x, y), (xp, yp, rect, i) => {
                            Color c = Services.textureManager.scratch[(xp - inc) + yp * width];
                            Services.textureManager.SetPixel(xp, yp, c);
                            return c;
                        });
                    }
                }
            }
            else 
            {
                int dir = move.y > 0 ? size.y : -1;
                int edge = Position.Value.y + dir;
                int end = height;
                int inc = System.Math.Sign(dir);
                for (int l = 0; l < size.x; l++)
                {
                    int x = l + Position.Value.x;
                    int y;
                    for (y = edge; y >= 0 && y < end; y += inc)
                    {
                        int p = x + y * width;
                        if (Services.textureManager.pixels[p].a == 0) break;
                        Services.textureManager.scratch[p] = Services.textureManager.pixels[p];
                    }
                    if (y != edge)
                    {
                        Services.textureManager.Apply(new Vector2Int(x, edge + inc), new Vector2Int(x, y), (xp, yp, rect, i) => {
                            Color c = Services.textureManager.scratch[xp + (yp - inc) * width];
                            Services.textureManager.SetPixel(xp, yp, c);
                            return c;
                        });
                    }
                }
            }
        }
        Services.textureManager.ResetArea(Position.Value, Position.Value + size - Vector2Int.one);
        Position.Value = new Vector2Int(Mathf.Clamp(Position.Value.x + move.x, 0, width), Mathf.Clamp(Position.Value.y + move.y, 0, height));
        Services.textureManager.SetArea(Position.Value, Position.Value + size - Vector2Int.one, color);
        Services.textureManager.texture.Apply();
        if (NetworkManager.Singleton.IsServer) Services.textureManager.UpdateBlobs();
    }

    private Vector2Int GetRandomPosition()
    {
        int w = Services.textureManager.width - size.x;
        int h = Services.textureManager.height - size.y;
        return new Vector2Int(Random.Range(0, w), Random.Range(0, h));
    }
}
