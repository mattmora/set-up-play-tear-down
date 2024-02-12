using System.Linq;
using System.Net;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;

public class NetworkSelector : MonoBehaviour
{
    public GameObject panel;
    public TMP_InputField localIP;
    public TMP_InputField portInput;
    public Button hostButton;
    public TMP_InputField hostIPInput;
    public Button joinButton;

    private bool hidden;

    private void Awake() {
        localIP.text = GetLocalIPv4();
        hostButton.onClick.AddListener(() => {
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(localIP.text, ushort.Parse(portInput.text));
            NetworkManager.Singleton.StartHost();
        });
        joinButton.onClick.AddListener(() => {
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(hostIPInput.text, ushort.Parse(portInput.text));
            NetworkManager.Singleton.StartClient();
        });
    }

    private void Update() {
        if (Input.GetKeyDown(KeyCode.I))
        {
            hidden = !hidden;
        }

        bool lobby = !NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer;
        bool hosting = NetworkManager.Singleton.IsServer;

        panel.SetActive((hosting && !hidden) || lobby);
        portInput.gameObject.SetActive(lobby);
        hostButton.gameObject.SetActive(lobby);
        hostIPInput.gameObject.SetActive(lobby);
        joinButton.gameObject.SetActive(lobby);
    }

    void StartButtons()
    {
        // if (GUILayout.Button("Server")) NetworkManager.Singleton.StartServer();
        // if (GUILayout.Button("Host")) 
        // {
        //     NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(GetLocalIPv4(), ushort.Parse(port));
        //     NetworkManager.Singleton.StartHost();
        // }
        // if (GUILayout.Button("Client")) 
        // {
        //     NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(ip, ushort.Parse(port));
        //     NetworkManager.Singleton.StartClient();
        // }
        // ip = GUILayout.TextField(ip);
    }

    public string GetLocalIPv4()
    {
        try {
            return Dns.GetHostEntry(Dns.GetHostName()).AddressList.First(
                f => f.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                .ToString();
        }
        catch {
            return "Failed to get IP. Enter manually to host.";
        }
    }

    void StatusLabels()
    {
        var mode = NetworkManager.Singleton.IsHost ?
            "Host" : NetworkManager.Singleton.IsServer ? "Server" : "Client";

        GUILayout.Label("Transport: " +
            NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetType().Name);
        GUILayout.Label("Mode: " + mode);
    }

    void SubmitNewPosition()
    {
        if (GUILayout.Button(NetworkManager.Singleton.IsServer ? "Move" : "Request Position Change"))
        {
            if (NetworkManager.Singleton.IsServer && !NetworkManager.Singleton.IsClient )
            {
                foreach (ulong uid in NetworkManager.Singleton.ConnectedClientsIds)
                    NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(uid).GetComponent<Worker>().Place();
            }
            else
            {
                var playerObject = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
                var player = playerObject.GetComponent<Worker>();
                player.Place();
            }
        }
    }
}
