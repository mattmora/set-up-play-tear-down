using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class Worker : NetworkBehaviour
{
    public NetworkVariable<Vector2Int> Position = new NetworkVariable<Vector2Int>();

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
            Position.Value = position;
        }
        else
        {
            SubmitPositionRequestServerRpc(position);
        }
    }

    public void Move(int x, int y)
    {
        var move = new Vector2Int(x, y);
        if (NetworkManager.Singleton.IsServer)
        {
            Position.Value += move;
        }
        else
        {
            SubmitMoveRequestServerRpc(move);
        }
    }

    [ServerRpc]
    void SubmitPositionRequestServerRpc(Vector2Int position, ServerRpcParams rpcParams = default)
    {
        Position.Value = position;
    }

    [ServerRpc]
    void SubmitMoveRequestServerRpc(Vector2Int move, ServerRpcParams rpcParams = default)
    {
        Position.Value += move;
    }

    static Vector2Int GetRandomPosition()
    {
        int w = Services.textureManager.width;
        int h = Services.textureManager.height;
        return new Vector2Int(Random.Range(0, w), Random.Range(0, h));
    }
}
