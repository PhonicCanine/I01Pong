using Mirror.Examples.Basic;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CameraControl : MonoBehaviour
{
    private PlayerObject playerObject;
    public Text scoreTextField;

    public void SetPlayerObject(PlayerObject g)
    {
        playerObject = g;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (playerObject == null)
            return;
        transform.position = playerObject.transform.position - playerObject.transform.forward * (10) + Vector3.up * 4;
        transform.LookAt(playerObject.transform, Vector3.up);

        scoreTextField.text = $"Score: {PlayerObject.pScore} - {PlayerObject.oScore}";
    }
}
