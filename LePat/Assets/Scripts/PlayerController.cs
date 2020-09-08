using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class PlayerController : MonoBehaviourPunCallbacks, IPunObservable
{
    [HideInInspector]
    public int id;
    public int totalPlayers;

    [Header("Info")]
    public float moveSpeed;
    public float jumpForce;
    public GameObject hatObject;

    [HideInInspector]
    public float curHatTime;
    public bool isDead = false;

    [Header("Components")]
    public Rigidbody rb;
    public Player photonPlayer;

    // called when the player object is instantiated
    [PunRPC]
    public void Initialize(Player player)
    {
        photonPlayer = player;
        totalPlayers++;
        id = player.ActorNumber;

        GameManager.instance.players[id - 1] = this;

        // give the first player the hat
        if (id == 1)
        {
            GameManager.instance.GiveHat(id, true);
        }

        // if this isn't our local player, disable physics
        if (!photonView.IsMine)
        {
            rb.isKinematic = true;
        }
    }

    // Update is called once per frame
    void Update()
    {
        //Debug.Log(photonView.IsMine);

        if (photonView.IsMine)
        {
            Move();

            if (Input.GetKeyDown(KeyCode.Space))
            {
                TryJump();
            }

            // track the amount of time we're wearing the hat
            if (hatObject.activeInHierarchy)
            {
                curHatTime += Time.deltaTime;
            }
        }

        // the host will check if player has lost
        if (PhotonNetwork.IsMasterClient)
        {
            if (curHatTime >= GameManager.instance.timeToLose && !GameManager.instance.gameEnded)
            {
                GameManager.instance.photonView.RPC("LoseGame", RpcTarget.All, id);
            }
        }

        // the host will check if player has won
        if (PhotonNetwork.IsMasterClient)
        {
            if (GameManager.instance.deadPlayers == (totalPlayers - 1)
                && !GameManager.instance.gameEnded)
            {
                if (photonView.IsMine && !isDead)
                {
                    GameManager.instance.gameEnded = true;
                    GameManager.instance.photonView.RPC("WinGame", RpcTarget.All, id);
                }
            }
        }
    }

    void Move()
    {
        float x = Input.GetAxis("Horizontal") * moveSpeed;
        float z = Input.GetAxis("Vertical") * moveSpeed;

        rb.velocity = new Vector3(x, rb.velocity.y, z);
    }

    void TryJump()
    {
        Ray ray = new Ray(transform.position, Vector3.down);

        if (Physics.Raycast(ray, 0.7f))
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }

    }

    // sets the player's hat active or not
    public void SetHat(bool hasHat)
    {
        hatObject.SetActive(hasHat);
    }

    public void OnCollisionEnter(Collision collision)
    {
        if (!photonView.IsMine)
        {
            return;
        }

        // did we hit another player
        if (collision.gameObject.CompareTag("Player"))
        {
            // do they have the hat?
            if (GameManager.instance.GetPlayer(collision.gameObject).id ==
                GameManager.instance.playerWithHat)
            {
                // can we get the hat?
                if (GameManager.instance.CanGetHat())
                {
                    // give us the hat
                    GameManager.instance.photonView.RPC("GiveHat", RpcTarget.All,
                        id, false);
                }
            }
        }
    }

    public void OnPhotonSerializeView (PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(curHatTime);
        }
        else if (stream.IsReading)
        {
            curHatTime = (float)stream.ReceiveNext();
        }
    }

}
