using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class GameOver : NetworkBehaviour
{
    public TMPro.TMP_Text txtWinnersList;
    public TMPro.TMP_Text txtLosersList;

    public override void OnNetworkSpawn()
    {
        txtLosersList.text = "";
        txtWinnersList.text = "";
        if (IsHost)
        {
            HostHandleTxtGeneration();
        }
    }

    private void HostHandleTxtGeneration()
    {
        string tempWinners = "";
        string tempLosers = "";
        foreach (var player in NetworkManager.Singleton.ConnectedClientsList)
        {
            var playerInfo = player.PlayerObject.GetComponent<Player>();
            playerInfo.gameOver.Value = true;
            if (playerInfo.Score.Value < 100)
            {
                tempLosers += $"Player: {playerInfo.OwnerClientId}, ";
            }
            else
            {
                tempWinners += $"Player: {playerInfo.OwnerClientId}";
            }
        }
        populateWinLossTxtClientRpc(tempWinners, tempLosers);
    }

    [ClientRpc]
    private void populateWinLossTxtClientRpc(string winners, string losers)
    {
        txtLosersList.text += losers;
        txtWinnersList.text += winners;
    }
}
