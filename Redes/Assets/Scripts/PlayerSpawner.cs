using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using System.Linq;

public class PlayerSpawner : SimulationBehaviour, IPlayerJoined, IPlayerLeft
{
    [SerializeField] Player _player;

    public Transform[] playerSpawns;

    public void PlayerJoined(PlayerRef player)
    {
        var playerCount = Runner.ActivePlayers.Count() - 1;

        if (player == Runner.LocalPlayer && playerCount < 4 && !GameManager.instance.gameStarted)
        {
            var spawnedPlayer = Runner.Spawn(_player, playerSpawns[playerCount].position, playerSpawns[playerCount].rotation);
            spawnedPlayer.playerRef = player;
            spawnedPlayer.playerNumber = playerCount;
            //GameManager.instance.PreGame();
        }

        if (playerCount > 0)
        {
            Debug.Log("ready to start");
            GameManager.instance.startGameButton.SetActive(true);
        }
    }

    public void PlayerLeft(PlayerRef player)
    {
        var playerCount = Runner.ActivePlayers.Count();

        for (var i = 0; i < playerCount; i++)
        {
            var checkingPlayer = GameManager.instance.players[i];

            if (checkingPlayer.Item2 == player)
            {
                //Destroy(checkingPlayer.Item1.gameObject);

                GameManager.instance.players.Remove(checkingPlayer);

                break;
            }
        }

        if (playerCount < 2)
        {
            GameManager.instance.startGameButton.SetActive(false);
        }
    }
}