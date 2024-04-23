using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class PlayerSpawner : SimulationBehaviour, IPlayerJoined
{
    [SerializeField] Player _player;

    int _playerCount = 0;

    public void PlayerJoined(PlayerRef player)
    {
        if (player == Runner.LocalPlayer && _playerCount < 4)
        {
            GameManager.instance.players.Add(Runner.Spawn(_player, GameManager.instance.playerSpawns[_playerCount].position, GameManager.instance.playerSpawns[_playerCount].rotation));
            _playerCount++;
        }
    }
}
