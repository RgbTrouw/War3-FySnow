using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

 namespace gameData
{
    

    public class gamePlay : MonoBehaviour
    {


        bool gameStarted = false;
        float timer;


        public void Start()
        {
            
        }

        public void startGame(){

            gameStarted = true;
            timer = 560;
        }

        public void Update(){

            if(gameStarted == true){

            timer -= Time.deltaTime;
            

            if(timer <= 0 ){
                timer = 560;
            }


            Debug.Log(timer);
            }

        }


    }


}
