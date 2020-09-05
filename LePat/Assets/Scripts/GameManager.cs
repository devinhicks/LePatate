using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Linq;

public class GameManager : MonoBehaviourPunCallbacks
{
    [Header("Stats")]
    public bool gameEnded = false;          //has the game ended?
    public float timeToLose;                 //time a player needs to hold the hat to lose
    public float invincibleDuration;        //how long after a player gets the hat, they are invincible
    private float hatPickupTime;            //the time the hat was picked up by current holder
    public bool playerDied = false;         //has the player run out of time?
    public int deadPlayers;                 //keep track of how many players have died

    [Header("Player")]
    public string playerPrefabLocation;     //path in Resources folder to Player prefab
    public Transform[] spawnPoints;           //array of all available spawn points
    public PlayerController[] players;      //array of all the players
    public int playerWithHat;               //id of player with the hat
    public int playersInGame;              //number of players in the game

    //instance
    public static GameManager instance;

    public void Awake()
    {
        // instance
        instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        players = new PlayerController[PhotonNetwork.PlayerList.Length];
        photonView.RPC("ImInGame", RpcTarget.AllBuffered);
    }

    [PunRPC]
    void ImInGame()
    {
        playersInGame++;

        if(playersInGame == PhotonNetwork.PlayerList.Length)
        {
            SpawnPlayer();
        }
    }

    // spawns a player and intializes it
    void SpawnPlayer()
    {
        //instantiate the player across the network
        //changed spawnPoints from Transform[] to Vector3[]
        GameObject playerObj = PhotonNetwork.Instantiate(playerPrefabLocation,
            spawnPoints[Random.Range(0, spawnPoints.Length)].position, Quaternion.identity);

        //get the player script
        PlayerController playerScript = playerObj.GetComponent<PlayerController>();

        //intialize the player
        playerScript.photonView.RPC("Initialize", RpcTarget.All, PhotonNetwork.LocalPlayer);
    }

    public PlayerController GetPlayer(int playerId)
    {
        return players.First(x => x.id == playerId);
    }

    public PlayerController GetPlayer(GameObject playerObject)
    {
        return players.First(x => x.gameObject == playerObject);
    }

    // called when a player hits the hatted player - giving them the hat
    [PunRPC]
    public void GiveHat (int playerId, bool initialGive)
    {
        // remove the hat from currently hatted player
        if (!initialGive)
        {
            GetPlayer(playerWithHat).SetHat(false);
        }

        // give the hat to the new player
        playerWithHat = playerId;
        GetPlayer(playerId).SetHat(true);
        hatPickupTime = Time.time;
    }

    // is the player able to take the hat at this time?
    public bool CanGetHat ()
    {
        if(Time.time > hatPickupTime + invincibleDuration)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    [PunRPC]
    void LoseGame (int playerId)
    {
        playerDied = true;
        deadPlayers++;

        PlayerController player = GetPlayer(playerId);
        player.isDead = true;

        // if player has hat, pass it off to another player
        if (playerId == playerWithHat)
        {
            for (int x = 0; x < players.Length; ++x)
            {
                if (players[x] != null)
                {
                    PlayerController p = GetPlayer(players[x].id);
                    if (!p.isDead)
                    {
                        GiveHat(p.id, true);
                    }
                }
            }
        }

        // turn player off
        player.rb.gameObject.SetActive(false);

        // Update player list
        GameUI.instance.UpdatePlayerDiedUI(playerId);
    }

    [PunRPC]
    void WinGame (int playerId)
    {
        gameEnded = true;
        PlayerController player = GetPlayer(playerId);

        // set the UI to show who's won
        GameUI.instance.SetWinText(player.photonPlayer.NickName);

        Invoke("GoBackToMenu", 3.0f);
    }

    void GoBackToMenu()
    {
        PhotonNetwork.LeaveRoom();
        NetworkManager.instance.ChangeScene("Menu");
    }
}
