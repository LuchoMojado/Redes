using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using System.Linq;

public class PlayerSpawner : SimulationBehaviour, IPlayerJoined, IPlayerLeft
{
    [SerializeField] Player _player;

    public void PlayerJoined(PlayerRef player)
    {
        var playerCount = Runner.ActivePlayers.Count() - 1;

        if (player == Runner.LocalPlayer && playerCount < 4 && !GameManager.instance.gameStarted)
        {
            var spawnedPlayer = Runner.Spawn(_player, GameManager.instance.playerSpawns[playerCount].position, GameManager.instance.playerSpawns[playerCount].rotation);
            spawnedPlayer.playerRef = player;
            spawnedPlayer.playerNumber = playerCount;
        }

        if (playerCount > 0)
        {
            Debug.Log("ready to start");
            GameManager.instance.readyButton.SetActive(true);
        }

        GameManager.instance.RpcReadyCheck();
    }

    public void PlayerLeft(PlayerRef player)
    {
        var playerCount = Runner.ActivePlayers.Count();

        GameManager.instance.RpcReadyCheck();

        if (playerCount < 2)
        {
            GameManager.instance.readyButton.SetActive(false);
        }
    }
}