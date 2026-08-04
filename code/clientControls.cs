using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using networking; 

namespace movement {

    public class clientControls : MonoBehaviour
    {

        float mouseX, mouseY;


        TCPServer network;
        // Start is called before the first frame update
        void Start()
        {
            network = FindObjectOfType(typeof(TCPServer)) as TCPServer;
        }

        // Update is called once per frame
        void Update()
        {

            mouseX = Input.GetAxis("Mouse X");
            mouseY = Input.GetAxis("Mouse Y");

            network.SendMessageToServer(network.localClientIndex + " " + "mouse " + mouseX + " " + mouseY);
            
        }
    }

}
