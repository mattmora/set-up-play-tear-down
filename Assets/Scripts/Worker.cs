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

    public void Move(int x, int y)
    {
        var move = new Vector2Int(x, y);
        if (NetworkManager.Singleton.IsServer)
        {
            // Debug.Log("Server Move");
            ExecuteMove(move);
        }
        else
        {
            // Debug.Log("Client Move");
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
    void SubmitMoveRequestServerRpc(Vector2Int move) => ExecuteMove(move);

    private void ExecuteMove(Vector2Int move)
    {
        Services.textureManager.ResetArea(Position.Value, Position.Value + size - Vector2Int.one);
        Position.Value = new Vector2Int(Mathf.Clamp(Position.Value.x + move.x, 0, 100), Mathf.Clamp(Position.Value.y + move.y, 0, 100));
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
