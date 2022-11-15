using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using System.Net;
using Unity.Netcode.Transports.UTP;

public class Main : NetworkBehaviour {

    public Button btnHost;
    public Button btnClient;
    public TMPro.TMP_Text txtStatus;
    public TMPro.TMP_InputField inputPort;
    public TMPro.TMP_InputField inputIp;

    public void Start() {
        btnHost.onClick.AddListener(OnHostClicked);
        btnClient.onClick.AddListener(OnClientClicked);
        NetworkManager.Singleton.OnClientDisconnectCallback += OnDisconnect;
        Application.targetFrameRate = 30;

        DebugStart();
    }

    private void StartHost(string sceneName = "Lobby", string startMessage = "Starting Host")
    {
        bool validSettings = ValidateSettings();
        if(!validSettings){
            btnClient.gameObject.SetActive(true);
            btnHost.gameObject.SetActive(true);
            return;
        }
        txtStatus.text = startMessage;

        NetworkManager.Singleton.StartHost();
        NetworkManager.SceneManager.LoadScene(
            sceneName,
            UnityEngine.SceneManagement.LoadSceneMode.Single
        );
    }

    private void StartClient(string startMessage = "Starting Client"){
        bool validSettings = ValidateSettings();
        if(!validSettings){
            return;
        }
        txtStatus.text = startMessage;
        NetworkManager.Singleton.StartClient();
        txtStatus.text = "Waiting on Host";
    }
    private void OnHostClicked() {
        StartHost();
    }

    private void OnClientClicked() {
        StartClient();
    }

    private void OnDisconnect(ulong clientId){
        txtStatus.text = "Failed to connect to server";
        btnHost.gameObject.SetActive(true);
        btnClient.gameObject.SetActive(true);
        inputIp.enabled = true;
        inputPort.enabled = true;
    }

    private bool ValidateSettings()
    {
        IPAddress ip;
        bool isValidIp = IPAddress.TryParse(inputIp.text, out ip);
        if (!isValidIp)
        {
            txtStatus.text = "Invalid IP";
            return false;
        }
        bool isValidPort = ushort.TryParse(inputPort.text, out ushort port);
        if (!isValidPort)
        {
            txtStatus.text = "Invalid Port";
            return false;
        }

        NetworkManager.Singleton
            .GetComponent<UnityTransport>()
            .SetConnectionData(ip.ToString(), port);
        btnHost.gameObject.SetActive(false);
        btnClient.gameObject.SetActive(false);
        inputIp.enabled = false;
        inputPort.enabled = false;
        return true;
    }

    private void DebugStart(){
        if(GameData.dbgRun.startMode == DebugRunner.StartModes.Host){
            string startMsg = $"Starting as {GameData.dbgRun.startMode} with scene {GameData.dbgRun.startScene} ";
            StartHost(GameData.dbgRun.startScene, startMsg);
        } else if (GameData.dbgRun.startMode == DebugRunner.StartModes.CLIENT){
            StartClient();
        } else {
            if(GameData.cmdArgs.startMode == DebugRunner.StartModes.Host){
                StartHost(GameData.cmdArgs.startScene);
            } else if(GameData.cmdArgs.startMode == DebugRunner.StartModes.CLIENT){
                StartClient();
            }
        }
        GameData.cmdArgs.startMode = DebugRunner.StartModes.CHOOSE;
        GameData.dbgRun.startMode = DebugRunner.StartModes.CHOOSE;
    }
}
