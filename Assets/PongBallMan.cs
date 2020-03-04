using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PongBallMan : MonoBehaviour
{

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnCollisionEnter(Collision c)
    {
        var player = c.gameObject.GetComponent<PlayerObject>();
        if (player != null)
        {
            //if the ball hits the player than this instance of the game should get authority
            if (player.isLocalPlayer)
            {
                PlayerObject.ballAuthority = PlayerObject.authority.me;
            }
            else
            {
                PlayerObject.ballAuthority = PlayerObject.authority.other;
            }
        }
    }
}
