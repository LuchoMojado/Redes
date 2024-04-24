using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using System.Linq;

public class PlayerSpawner : SimulationBehaviour, IPlayerJoined
{
    [SerializeField] Player _player;

    public void PlayerJoined(PlayerRef player)
    {
        var playerCount = Runner.ActivePlayers.Count() - 1;

        Debug.Log("player joined");
        if (player == Runner.LocalPlayer && playerCount < 4)
        {
            Debug.Log("player spawned");
            GameManager.instance.players.Add(Runner.Spawn(_player, GameManager.instance.playerSpawns[playerCount].position, GameManager.instance.playerSpawns[playerCount].rotation));

            if (playerCount > 0)
            {
                Debug.Log("ready to start");
                GameManager.instance.startGameButton.SetActive(true);
            }
        }
    }
}
