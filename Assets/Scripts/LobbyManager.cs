using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using TMPro;

public class LobbyManager : NetworkBehaviour
{
    private List<LobbyPlayerPanel> playerPanels;

    public GameObject playerScrollContent;
    public TMPro.TMP_Text txtPlayerNumber;
    public Button btnStart;
    public Button btnReady;
    public LobbyPlayerPanel playerPanelPrefab;
    public ChatUI chat;

    public void Awake()
    {
        GameData.dbgRun.StartGameWithSceneIfNotStarted();
        playerPanels = new List<LobbyPlayerPanel>();
    }

    public void Start()
    {
        if (IsHost)
        {
            RefreshPlayerPanels();
            btnStart.onClick.AddListener(HostOnBtnStartClick);
        }

        if (IsClient)
        {
            btnReady.onClick.AddListener(ClientOnReadyClicked);
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsHost)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += HostOnClientConnected;
            btnReady.gameObject.SetActive(false);

            int myIndex = GameData.Instance.FindPlayerIndex(NetworkManager.LocalClientId);
            if (myIndex != -1)
            {
                PlayerInfo info = GameData.Instance.allPlayers[myIndex];
                info.isReady = true;
                GameData.Instance.allPlayers[myIndex] = info;
            }
        }
        else
        {
            btnStart.gameObject.SetActive(false);
            Debug.Log("In else block");
            NetworkManager.Singleton.OnClientDisconnectCallback += ClientOnDisconnect;
        }

        txtPlayerNumber.text = $"Player #{NetworkManager.LocalClientId}";
        GameData.Instance.allPlayers.OnListChanged += ClientOnAllPlayersChanged;
        EnableStartIfAllReady();
    }

    // -----------------------
    // Private
    // -----------------------
    private void AddPlayerPanel(PlayerInfo info)
    {
        LobbyPlayerPanel newPanel = Instantiate(playerPanelPrefab);
        newPanel.transform.SetParent(playerScrollContent.transform, false);
        newPanel.SetName($"Player {info.clientId.ToString()}");
        newPanel.SetColor(info.color);
        newPanel.SetReady(info.isReady);
        newPanel.ShowKick(IsHost && info.clientId != NetworkManager.Singleton.LocalClientId);
        newPanel.OnKickPlayer += delegate
        {
            OnPlayerKicked(info.clientId);
        };
        playerPanels.Add(newPanel);
    }

    private void OnPlayerKicked(ulong clientId)
    {
        Debug.Log("In onplayerKicked method");
        NetworkManager.Singleton.DisconnectClient(clientId);
        GameData.Instance.RemovePlayerFromList(clientId);
    }

    private void RefreshPlayerPanels()
    {
        foreach (LobbyPlayerPanel panel in playerPanels)
        {
            Destroy(panel.gameObject);
        }
        playerPanels.Clear();

        foreach (PlayerInfo pi in GameData.Instance.allPlayers)
        {
            AddPlayerPanel(pi);
        }
    }

    private void EnableStartIfAllReady()
    {
        int readyCount = 0;
        foreach (PlayerInfo readyInfo in GameData.Instance.allPlayers)
        {
            if (readyInfo.isReady)
            {
                readyCount += 1;
            }
        }

        btnStart.enabled = readyCount == GameData.Instance.allPlayers.Count;
        if (btnStart.enabled)
        {
            btnStart.GetComponentInChildren<TextMeshProUGUI>().text = "Start";
        }
        else
        {
            btnStart.GetComponentInChildren<TextMeshProUGUI>().text = "<Waiting for Ready>";
        }
    }

    // -----------------------
    // Events
    // -----------------------
    private void ClientOnAllPlayersChanged(NetworkListEvent<PlayerInfo> changeEvent)
    {
        RefreshPlayerPanels();
    }

    private void HostOnBtnStartClick()
    {
        StartGame();
    }

    private void HostOnClientConnected(ulong clientId)
    {
        EnableStartIfAllReady();
    }

    private void ClientOnReadyClicked()
    {
        ToggleReadyServerRpc();
    }

    private void ClientOnDisconnect(ulong clientId)
    {
        SceneManager.LoadScene("Main");
    }

    // -----------------------
    // Public
    // -----------------------

    public override void OnDestroy()
    {
        if (GameData.Instance != null)
        {
            GameData.Instance.allPlayers.OnListChanged -= ClientOnAllPlayersChanged;
        }
        if (IsHost && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= HostOnClientConnected;
        }
    }

    public void StartGame()
    {
        var scene = NetworkManager.SceneManager.LoadScene(
            "Arena1",
            UnityEngine.SceneManagement.LoadSceneMode.Single
        );
        btnStart.enabled = false;
    }

    [ServerRpc(RequireOwnership = false)]
    public void ToggleReadyServerRpc(ServerRpcParams serverRpcParams = default)
    {
        ulong clientId = serverRpcParams.Receive.SenderClientId;
        int playerIndex = GameData.Instance.FindPlayerIndex(clientId);
        PlayerInfo info = GameData.Instance.allPlayers[playerIndex];

        info.isReady = !info.isReady;
        GameData.Instance.allPlayers[playerIndex] = info;

        EnableStartIfAllReady();
    }
}