using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using System.Linq;

public class PlayerSpawner : SimulationBehaviour, IPlayerJoined
{
    [SerializeField] Player _player;

    public Transform[] playerSpawns;

    public void PlayerJoined(PlayerRef player)
    {
        var playerCount = Runner.ActivePlayers.Count() - 1;

        if (player == Runner.LocalPlayer && playerCount < 4)
        {
            GameManager.instance.players.Add(Runner.Spawn(_player, playerSpawns[playerCount].position, playerSpawns[playerCount].rotation));
            Debug.Log(GameManager.instance.players.Count);
            GameManager.instance.PreGame();
        }

        if (playerCount > 0)
        {
            Debug.Log("ready to start");
            GameManager.instance.startGameButton.SetActive(true);
        }
    }
}
