using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Net.Sockets;
using Mirror.Examples.Pong;
using UnityEngine.UI;

public class PlayerObject : NetworkBehaviour
{
    Rigidbody thisRigidbody;

    [SyncVar]
    bool gameInPlay = false;

    static bool gameStillGoing = true;

    //references from the inspector
    public GameObject ballPrefab;
    public GameObject spawner;

    GameObject ball;

    //player and opponent scores
    public static int pScore = 0;
    public static int oScore = 0;

    // Start is called before the first frame update
    void Start()
    {
        if (isLocalPlayer)
        {
            Camera.main.GetComponent<CameraControl>().SetPlayerObject(this);
        }

        thisRigidbody = GetComponent<Rigidbody>();

        //transform.position = transform.position + new Vector3(netId, 0, netId);

        ballAuthority = isServer ? authority.me : authority.other;

        SpawnPlayer();
    }

    bool spawned = false;
    public void SpawnPlayer(bool gamestarting = false)
    {
        if (spawned)
            return;

        //figure out which side to spawn on
        var objs = GameObject.FindObjectsOfType<PlayerObject>();
        if (objs.Any())
        {
            
            var otherPlayerObjects = (from o in objs
                              where o.netId != netId
                              select o);
            if (otherPlayerObjects.Any())
            {
                spawned = true;
                var spawnPoint = GameObject.Find("SpawnPlayer" + (otherPlayerObjects.First().netId > netId ? 2 : 1));

                //we need the spawner to find the default transform
                spawner = spawnPoint;
                transform.position = spawnPoint.transform.position;
                transform.rotation = spawnPoint.transform.rotation;

                //spawn the other paddle
                otherPlayerObjects.First().SpawnPlayer(true);
            }

        }
    }

    [ClientRpc]
    public void RpcInstantiateBall()
    {
        if (!isServer && isLocalPlayer)
        {
            if (GameObject.FindGameObjectWithTag("Ball") != null)
                ball = GameObject.FindGameObjectWithTag("Ball");
            else
                ball = GameObject.Instantiate(ballPrefab);
            Debug.Log("Ball Spawned on Client");
            Debug.Log(ball != null);
        }
            
    }

    [Command]
    public void CmdSetBallDetails(Vector3 position, Vector3 velocity)
    {
        if (ballAuthority == authority.other)
        {
            ball.transform.position = position;
            ball.GetComponent<Rigidbody>().velocity = velocity;
        }
        else
        {
            Debug.Log("Received out of turn");
        }
    }

    [ClientRpc]
    public void RpcSetBallDetails(Vector3 position, Vector3 velocity)
    {
        if (ball == null)
        {
            ball = GameObject.FindGameObjectWithTag("Ball");
            if (ball == null)
                return;
        }
        
        if (ballAuthority == authority.other)
        {
            ball.transform.position = position;
            ball.GetComponent<Rigidbody>().velocity = velocity;
        }
        else
        {
            Debug.Log("Received out of turn");
        }
    }

    [ClientRpc]
    public void RpcSpawnBall(Vector3 position, Vector3 velocity)
    {
        //if the client is spawning the ball, control of the position should go to the server (the server always starts with authority)
        ballAuthority = authority.other;
        ball.transform.position = position;
        ball.GetComponent<Rigidbody>().velocity = velocity;
        gameInPlay = true;
        canScore = true;
    }

    void PlaceBall()
    {
        if (isServer && !gameInPlay && isLocalPlayer)
        {
            //start the ball and give it a random amount of momentum
            var vel = new Vector3(UnityEngine.Random.Range(-1, 1) * 5, 0, (UnityEngine.Random.Range(-1, 1) > 0 ? 2 : -2));
            ball.transform.position = Vector3.zero;
            ball.GetComponent<Rigidbody>().velocity = vel;
            gameInPlay = true;
            ballAuthority = authority.me;

            //tell the clients about the ball
            RpcSpawnBall(ball.transform.position, vel);
            canScore = true;
        }
    }

    static bool canScore = false;

    // Update is called once per frame
    void Update()
    {
        //reset gameStillGoing
        if (gameStillGoing == false && isLocalPlayer)
        {
            gameInPlay = false;
            gameStillGoing = true;
        }
        
        if (ball == null)
        {
            var tryFind = GameObject.FindGameObjectWithTag("Ball");
            if (tryFind != null)
                ball = tryFind;
            else
                ball = GameObject.Instantiate(ballPrefab);
            Debug.Log("Ball Spawned due to no ball");
        }

        

        if (!gameInPlay && ball != null && isLocalPlayer && spawned)
        {
            PlaceBall();
        }


        //Only allow input for the player
        if (isLocalPlayer && (gameInPlay || isClientOnly))
        {
            

            float ix = Input.GetAxis("Horizontal") * 7;
            float iy = Input.GetAxis("Vertical") * 7;
            float rotationDirection = Input.GetAxis("Rotation");

            if (rotationDirection == 0)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(spawner.transform.forward, Vector3.up), 0.15f);
            }else if (rotationDirection > 0)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.FromToRotation(Vector3.right + Vector3.forward, spawner.transform.forward), 0.15f);
            }
            else
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.FromToRotation(Vector3.left + Vector3.forward, spawner.transform.forward), 0.15f);
            }
            

            var qRight = transform.right;
            
            //transform.rotation = spawner.transform.rotation;

            var vHor = ix * qRight;
            var vVert = iy * transform.forward;

            thisRigidbody.velocity = ix * qRight + iy * transform.forward;
            var bVel = Vector3.zero;

            //constrain the paddle to within the bounds
            if (((Mathf.Abs(transform.position.x) >= 7f) && vHor.x * transform.position.x <= 0) || (Mathf.Abs(transform.position.x) < 7f))
                bVel += vHor;
            if ((Mathf.Abs(transform.position.z) >= 15f && vVert.z * transform.position.z <= 0) || (Mathf.Abs(transform.position.z) < 15f))
                if (((Mathf.Abs(transform.position.z) <= 3f && vVert.z * transform.position.z >= 0)) || (Mathf.Abs(transform.position.z) >= 3f))
                    bVel += vVert;

            thisRigidbody.velocity = bVel;

        }

        
        //Ball is out the back, someone has won a round
        if (ball != null && Mathf.Abs(ball.transform.position.z) > 15)
        {
            if (ball.transform.position.z * transform.position.z < 0 && canScore)
            {
                if (isLocalPlayer)
                {
                    Debug.Log("Gained a point");
                    pScore += 1;
                }
                else
                {
                    Debug.Log("Lost a point");
                    oScore += 1;
                }

                //without this, the client's scores can desync hard
                canScore = false;

                if (isServer)
                {
                    //in order to stop the game after the ball goes out, no matter who it goes out for
                    //could just be static bool
                    gameStillGoing = false;
                }
            }
        }

        if (!isLocalPlayer)
            return;

        if (isLocalPlayer && ball != null && ballAuthority == authority.me)
        {
            if (isServer)
            {
                RpcSetBallDetails(ball.transform.position, ball.GetComponent<Rigidbody>().velocity);
            }
            else
            {
                CmdSetBallDetails(ball.transform.position, ball.GetComponent<Rigidbody>().velocity);
            }
        }

    }

    //to control whether or not we listen when the client / server tells us the ball's position
    public static authority ballAuthority;

    public enum authority
    {
        me,
        other
    }
}
