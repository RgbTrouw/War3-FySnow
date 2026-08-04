using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using networking; 

public class characterPanel : MonoBehaviour
{

    [SerializeField] private Button char1Button, char2Button, char3Button, char4Button, char5Button, char6Button, char7Button, char8Button, scourgeButton, sentinelButton, goButton;

    private int teamSelection = 0;
    private int characterSelection = 0;

    TCPServer network;


    // Start is called
    //  before the first frame update
    void Start()
    {
        
        network = FindObjectOfType(typeof(TCPServer)) as TCPServer;

        char1Button.onClick.AddListener(delegate{selectChar(0);});
        char2Button.onClick.AddListener(delegate{selectChar(1);});
        char3Button.onClick.AddListener(delegate{selectChar(2);});
        char4Button.onClick.AddListener(delegate{selectChar(3);});
        char5Button.onClick.AddListener(delegate{selectChar(4);});
        char6Button.onClick.AddListener(delegate{selectChar(5);});
        char7Button.onClick.AddListener(delegate{selectChar(6);});
        char8Button.onClick.AddListener(delegate{selectChar(7);});

        
        sentinelButton.onClick.AddListener(delegate{selectTeam(0);});
        scourgeButton.onClick.AddListener(delegate{selectTeam(1);});

        goButton.onClick.AddListener(delegate{go();});

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void selectChar(int index){

       
        characterSelection = index;

        

    }

    void selectTeam(int index){

        teamSelection = index;
    }

    void go(){


        network.SendMessageToServer(network.localClientIndex.ToString() + " " + "go " + teamSelection.ToString() + " " + characterSelection.ToString());

    }
}
