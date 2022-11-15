using System.Collections.Generic;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public Player playerPrefab;
    public GameObject spawnPoints;

    private int spawnIndex = 0;
    private List<Vector3> availableSpawnPositions = new List<Vector3>();

    private List<Player> players = new List<Player>();

    public void Start(){
        GameData.dbgRun.StartGameWithSceneIfNotStarted();
    }
    public void Awake(){
        refreshSpawnPoints();
    }
    public override void OnNetworkSpawn()
    {
        if (IsHost)
        {
            SpawnPlayers();
        }
    }

    private void refreshSpawnPoints()
    {
        Transform[] allPoints = spawnPoints.GetComponentsInChildren<Transform>();
        availableSpawnPositions.Clear();
        foreach (Transform point in allPoints)
        {
            if (point != spawnPoints.transform)
            {
                availableSpawnPositions.Add(point.localPosition);
            }
        }
        { }
    }

    public Vector3 GetNextSpawnLocation(){
        var newPosition = availableSpawnPositions[spawnIndex];
        newPosition.y = 1.5f;
        spawnIndex++;
        if(spawnIndex > availableSpawnPositions.Count -1) {
            spawnIndex = 0;
        }
        return newPosition;
    }

    private void SpawnPlayers()
    {
        foreach (PlayerInfo info in GameData.Instance.allPlayers)
        {
            SpawnPlayer(info);
        }
    }

    private void SpawnPlayer(PlayerInfo info){
        Player playerSpawn = Instantiate(
                playerPrefab,
                GetNextSpawnLocation(),
                Quaternion.identity
            );
            playerSpawn.GetComponent<NetworkObject>().SpawnAsPlayerObject(info.clientId);
            playerSpawn.PlayerColor.Value = info.color;
            players.Add(playerSpawn);
            playerSpawn.Score.OnValueChanged += HostOnPlayerScoreChanged;
    }

    private void HostOnPlayerScoreChanged(int previous, int current){
        if(current >= 100 ){
        NetworkManager.SceneManager.LoadScene(
            "GameOver",
            UnityEngine.SceneManagement.LoadSceneMode.Single
        );
        }
    }

    private void HostOnClientConnected(ulong clientId){
        int playerIndex = GameData.Instance.FindPlayerIndex(clientId);
        if(playerIndex != 1){
            PlayerInfo newPlayerInfo = GameData.Instance.allPlayers[playerIndex];
            SpawnPlayer(newPlayerInfo);
        }
    }
    private void HostOnClientDisconnected(ulong clientId){
        NetworkObject nObject = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
        Player pObject = nObject.GetComponent<Player>();
        players.Remove(pObject);
        Destroy(pObject);
    }
}
