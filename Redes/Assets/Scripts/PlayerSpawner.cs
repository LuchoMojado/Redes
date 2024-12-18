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
        StartCoroutine(WaitToSpawn(player));
    }

    IEnumerator WaitToSpawn(PlayerRef player)
    {
        yield return new WaitForSeconds(1);

        var playerCount = Runner.ActivePlayers.Count() - 1;

        if (player == Runner.LocalPlayer)
        {
            if (playerCount < 4 && !GameManager.instance.gameStarted)
            {
                var spawnedPlayer = Runner.Spawn(_player, GameManager.instance.playerSpawns[playerCount].position, GameManager.instance.playerSpawns[playerCount].rotation);
                //spawnedPlayer.playerRef = player;
                spawnedPlayer.playerNumber = playerCount;

                spawnedPlayer.RpcSetPlayerRef(player);
            }
            else
            {
                GameManager.instance.disconnect.onClick.AddListener(() => 
                { 
                    Destroy(Runner.gameObject);
                    ScenesManager.instance.ChangeScene("MainMenu");
                });
            }
        }
        
        
        if (GameManager.instance.gameStarted || playerCount >= 4) yield break;

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

        if (GameManager.instance.gameStarted)
        {
            bool stop = false;
            Debug.Log(player.PlayerId);

            for (int i = 0; i < GameManager.instance.playerIDs.Count(); i++)
            {
                Debug.Log(GameManager.instance.playerIDs.Get(i));
                if (GameManager.instance.playerIDs.Get(i) == player.PlayerId)
                {
                    stop = true;
                }
            }

            if (stop)
            {
                GameManager.instance.RpcRestartGame();

                if (playerCount > 1)
                {
                    GameManager.instance.readyButton.SetActive(true);
                }
            }
        }
        else
        {
            foreach (var item in GameManager.instance.players)
            {
                item.Item1.RpcReset();
            }

            if (playerCount < 2)
            {
                GameManager.instance.readyButton.SetActive(false);
            }
        }
    }
}