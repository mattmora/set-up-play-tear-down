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
        Services.textureManager.workers.Add(this);
        if (IsOwner)
        {
            Place();
        }
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

    public void Move(int x, int y)
    {
        var move = new Vector2Int(x, y);
        if (NetworkManager.Singleton.IsServer)
        {
            ExecuteMove(move);
        }
        else
        {
            SubmitMoveRequestServerRpc(move);
        }
    }

    private void Update() {
        if (!IsOwner) return;
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            Move(0, 1);
        }
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            Move(0, -1);
        }
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            Move(-1, 0);
        }
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            Move(1, 0);
        }
    }

    [ServerRpc]
    void SubmitPlaceRequestServerRpc(Vector2Int position, ServerRpcParams rpcParams = default) => ExecutePlace(position);

    private void ExecutePlace(Vector2Int position)
    {
        Services.textureManager.ResetArea(Position.Value, Position.Value + size - Vector2Int.one);
        Position.Value = position;
        Services.textureManager.SetArea(Position.Value, Position.Value + size - Vector2Int.one, color);
        Services.textureManager.texture.Apply();
        if (NetworkManager.Singleton.IsServer) Services.textureManager.UpdateBlobs();
    }


    [ServerRpc]
    void SubmitMoveRequestServerRpc(Vector2Int move, ServerRpcParams rpcParams = default) => ExecuteMove(move);

    private void ExecuteMove(Vector2Int move)
    {
        Services.textureManager.ResetArea(Position.Value, Position.Value + size - Vector2Int.one);
        Position.Value += move;
        Services.textureManager.SetArea(Position.Value, Position.Value + size - Vector2Int.one, color);
        Services.textureManager.texture.Apply();
        if (NetworkManager.Singleton.IsServer) Services.textureManager.UpdateBlobs();
    }

    static Vector2Int GetRandomPosition()
    {
        int w = Services.textureManager.width;
        int h = Services.textureManager.height;
        return new Vector2Int(Random.Range(0, w), Random.Range(0, h));
    }
}
