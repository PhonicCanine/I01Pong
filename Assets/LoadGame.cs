using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadGame : MonoBehaviour
{
    DateTime firstLoad;

    // Start is called before the first frame update
    void Start()
    {
        firstLoad = DateTime.Now;
    }

    int frames = 0;

    // Update is called once per frame
    void Update()
    {
        if ((DateTime.Now - firstLoad).TotalSeconds > 5)
        {
            SceneManager.LoadScene("SampleScene", LoadSceneMode.Single);
        }
    }
}
