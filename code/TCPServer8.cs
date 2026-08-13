using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using Random = UnityEngine.Random;
using UnityEngine.UI;
using gameData;
using System.Runtime.Serialization;
using System.Runtime.Versioning;


namespace networking {

    public class TCPServer : MonoBehaviour
    {


    

        private TcpListener listener;
        private TcpClient client;
        private NetworkStream clientStream;
        private Thread clientThread;

        private List<TcpClient> clients = new List<TcpClient>(); // Keeps track of connected clients

        public gamePlay gameHost = new gamePlay();
        public gamePlay gamePlayData = new gamePlay();

        private string serverIP;
        private int serverPort = 2026;

        private bool hosting = false;

        [SerializeField] public GameObject startGameObjects, serverPanel, characterSelectionPanel, lobbyCamera, player, playerCamera, startGame, game, sentinelSpawnPosition, scourgeSpawnPosition, scoreBoard, scoreListItem, scoreSentinelList, scoreScourgeList;
        [SerializeField] public GameObject model0, model1, model2, model3, model4, model5, model6, model7;
        [SerializeField] public GameObject playerModel, sentinelPlayersEmpty, scourgePlayersEmpty, miniMapCam;
        
        [SerializeField] private GameObject console, consoleContent;
        [SerializeField] private InputField consolePrompt;
        [SerializeField] private Text consoleLine;
        private string padding = "  ";

        [SerializeField] private GameObject profilePicture0, profilePicture1, profilePicture2, profilePicture3, profilePicture4, profilePicture5, profilePicture6, profilePicture7 ;

        [SerializeField] private Text minutesText, secondsText;

        private GameObject[] players;
        private GameObject[] sentinelPlayers, scourgePlayers;

        CharacterController playerController;

        [SerializeField] private Text characterName;

        [SerializeField] private Button joinServerButton, createServerButton ;
        [SerializeField] private InputField inputPlayerName, inputAddress;
        [SerializeField] private Text localIpText, localIpText2, localIpText3, localIpText4;

        [SerializeField] private GameObject spawnPositionSentinel, spawnPositionScourge;

        public int localClientIndex;
        private int timeInt = 560;

        public bool gameStarted = false;

        public float sentinelSpawnPosX = 306.2691f;
        public float sentinelSpawnPosY = 0.9972534f;
        public float sentinelSpawnPosZ = 246.811f;

        public float scourgeSpawnPosX = 305.5692f;
        public float scourgeSpawnPosY = 0.9972534f;
        public float scourgeSpawnPosZ = 80.25097f;

       

        public void Start()
        {
        
            // Debug.Log(sentinelSpawnPosition.transform.position.x + " " + sentinelSpawnPosition.transform.position.y + " " + sentinelSpawnPosition.transform.position.z );
            // Debug.Log(scourgeSpawnPosition.transform.position.x + " " + scourgeSpawnPosition.transform.position.y + " " + scourgeSpawnPosition.transform.position.z );

            joinServerButton.onClick.AddListener(joinServer);
            createServerButton.onClick.AddListener(createServer);

            startGameObjects.SetActive(true);
            serverPanel.SetActive(true);
            characterSelectionPanel.SetActive(false);


            inputPlayerName.text = PlayerPrefs.GetString("playerName");
            inputPlayerName.onValueChanged.AddListener(delegate {setPlayerName(); });

            consolePrompt.onValueChanged.AddListener(delegate {consolePromptLine(); });

 //           localIpText.text = Dns.GetHostEntry(Dns.GetHostName()).AddressList[0].ToString();
            int i=0;
           
           foreach (IPAddress address in  Dns.GetHostEntry(Dns.GetHostName()).AddressList)
            {
      

                if(i==0){localIpText.text = Dns.GetHostEntry(Dns.GetHostName()).AddressList[i].ToString();}
                else if(i==1){localIpText2.text = Dns.GetHostEntry(Dns.GetHostName()).AddressList[i].ToString();}
                else if(i==2){localIpText3.text = Dns.GetHostEntry(Dns.GetHostName()).AddressList[i].ToString();}
                else if(i==3){localIpText3.text = Dns.GetHostEntry(Dns.GetHostName()).AddressList[i].ToString();}

                i++;
            }

            serverIP = Dns.GetHostEntry(Dns.GetHostName()).AddressList[0].ToString();

            //StartCoroutine(mainThreadTasks);
        
        }

        private void consolePromptLine(){

           GameObject consoleLogLine = Instantiate(consoleLine.gameObject, consoleLine.transform) as GameObject;
                consoleLogLine.transform.name = "logLine";
                consoleLogLine.GetComponentInChildren<Text>().text = padding + consolePrompt.text;
                consoleLogLine.transform.SetParent(consoleContent.transform);
                
                consolePrompt.text = "";
                
        }

         private void log(string logMessage){

           GameObject consoleLogLine = Instantiate(consoleLine.gameObject, consoleLine.transform) as GameObject;
                consoleLogLine.transform.name = "logLine";
                consoleLogLine.GetComponentInChildren<Text>().text = logMessage;
                consoleLogLine.transform.SetParent(consoleContent.transform);
                
                
                
        }

        
        //  IEnumerator mainThreadTasks()
        //  {
        //     while (jobsQue.Count > 0) {
        //         jobsQue.Dequeue().Invoke();

        //     }

        //     yield return new WaitForSeconds(.1f);
        // }

        
        public void Update(){

         if(Input.GetKey(KeyCode.BackQuote)){

            if(console.activeSelf == false){
                console.SetActive(true);
            } else {
                console.SetActive(false);
            }

         }

         if(Input.GetKey(KeyCode.Return)){

            if(console.activeSelf == true && consolePrompt.text.Length > 0){
                GameObject consoleLogLine = Instantiate(consoleLine.gameObject, consoleLine.transform) as GameObject;
                consoleLogLine.transform.name = "logLine";
                consoleLogLine.GetComponentInChildren<Text>().text = padding + consolePrompt.text;
                consoleLogLine.transform.SetParent(consoleContent.transform);
                
                consolePrompt.text = "";
                //consoleSpacer
            } 

         }

          

            if(gameHost.hosting){

            gameHost.timeLeft -= Time.deltaTime;

            if(timeInt > Mathf.RoundToInt(gameHost.timeLeft) ){
                timeInt = Mathf.RoundToInt(gameHost.timeLeft);
            //minutesText.text = Mathf.RoundToInt(gameHost.timeLeft / 60).ToString();
            int minutes = Mathf.RoundToInt(Mathf.RoundToInt(gameHost.timeLeft) / 60);
            int seconds = Mathf.RoundToInt(gameHost.timeLeft) - (minutes * 60);

            if(seconds < 0){
                minutes -= 1;
                seconds = 60 - seconds;
            }
            
            if(seconds < 10) {
            secondsText.text = "0" + seconds.ToString();
            } else {
            secondsText.text = seconds.ToString();
            }

            minutesText.text = "0" + minutes.ToString();

            if(gameHost.timeLeft <= 0){ gameHost.timeLeft = 560;}


            broadcastInstructions("timeleft " + Mathf.RoundToInt(gameHost.timeLeft).ToString());
            //log("broadcast: " + "timeleft " + gameHost.timeLeft.ToString());

            }        }

        }


        private void setPlayerName(){

        PlayerPrefs.SetString("playerName", inputPlayerName.text);

        }

    

        private void joinServer(){

            if(!hosting){
            serverIP = inputAddress.text;
            
            } else {
                serverIP = "127.0.0.1";
            }

            try
            {
                client = new TcpClient(serverIP, serverPort);
                clientStream = client.GetStream();
                Debug.Log("Connected to server.");

                SendMessageToServer("setName " + inputPlayerName.text);

                clientThread = new Thread(new ThreadStart(ClientListenForData));
                clientThread.IsBackground = true;
                clientThread.Start();
            }
            catch (SocketException e)
            {
                Debug.LogError("SocketException: " + e.ToString());
            }
        


        }

        private void createServer(){

            listener = new TcpListener(IPAddress.Any, 2026);
            listener.Start();
            Debug.Log("Server started, waiting for connections...");

            Thread acceptThread = new Thread(AcceptClients);
            acceptThread.Start();

           

            gameHost = new gamePlay();
            gameHost.Start();
            gameHost.hosting = true;

            joinServer();
        }


        private void AcceptClients()
        {
            while (true)
            {
                try
                {
                    TcpClient client = listener.AcceptTcpClient();
                    clients.Add(client);
                    Console.WriteLine("Client connected.");

                    Thread clientThread = new Thread(() => HandleClient(client, clients.Count - 1));
                    clientThread.Start();

                    
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error accepting client: " + ex.Message);
                }
            }
        }

        private void HandleClient(TcpClient client, int clientIndex)
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];
            int bytesRead;

           

            try
            {
                

                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {

                    
                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    Debug.Log("server received <- " + message);

                    string[] messageList = message.Split(' ');
                    byte[] response = Encoding.UTF8.GetBytes(message);

                    message = message.Split('\n')[0];


                    if(message.Split(' ')[0] == "setName"){

                       gameHost.addPlayer(message.Split(' ')[1]);

                        
                        gameHost.playersList[gameHost.playersList.Count - 1 ].playerIndex = gameHost.playersList.Count - 1;
                         

                        response = Encoding.UTF8.GetBytes("chooseCharacter " + (gameHost.playersList.Count - 1).ToString() + " " + message.Split(' ')[1] + "\n");
                        
                        stream.Write(response, 0, response.Length);

                        Debug.Log("server sent -> " + "chooseCharacter " + (gameHost.playersList.Count - 1).ToString() + " " + message.Split(' ')[1] );

                        
                        
                    } else if(message.Split(' ')[1] == "join"){

                        UnityMainThreadDispatcher.Enqueue(() =>
                                        {
                                
                                //if(gameHost.playersList.Count == 1){}

                                gameHost.playersList[int.Parse(message.Split(' ')[0])].team = int.Parse(message.Split(' ')[2]);
                                gameHost.playersList[int.Parse(message.Split(' ')[0])].character = int.Parse(message.Split(' ')[3]);
                            
                                gameHost.playersList[int.Parse(message.Split(' ')[0])].Start();

                                           
                            

                                if(message.Split(' ')[2] == "1"){
                                    
                                gameHost.playersList[int.Parse(message.Split(' ')[0])].spawnPositionX = scourgeSpawnPosX;
                                gameHost.playersList[int.Parse(message.Split(' ')[0])].spawnPositionY = scourgeSpawnPosY;
                                gameHost.playersList[int.Parse(message.Split(' ')[0])].spawnPositionZ = scourgeSpawnPosZ;

                                
                                } else if(message.Split(' ')[2] == "0"){

                                gameHost.playersList[int.Parse(message.Split(' ')[0])].spawnPositionX = sentinelSpawnPosX;
                                gameHost.playersList[int.Parse(message.Split(' ')[0])].spawnPositionY = sentinelSpawnPosY;
                                gameHost.playersList[int.Parse(message.Split(' ')[0])].spawnPositionZ = sentinelSpawnPosZ;
                                
                               
                                }

                                broadcastInstructions((message.Split(' ')[0] + " " + message.Split(' ')[1] + " " + message.Split(' ')[2] + " " + message.Split(' ')[3] + " " + message.Split(' ')[4] + " " + "pos(" + gameHost.playersList[int.Parse(message.Split(' ')[0])].spawnPositionX.ToString() + "," + gameHost.playersList[int.Parse(message.Split(' ')[0])].spawnPositionY.ToString() + "," + gameHost.playersList[int.Parse(message.Split(' ')[0])].spawnPositionZ.ToString() + ")").Replace("\n", "") + "\n");
                                
                                for(int i=0; i<gameHost.playersList.Count; i++){

                                broadcastInstructions("playersList" + " " + i.ToString() + " " + gameHost.playersList[i].playerName + " " + gameHost.playersList[i].team.ToString() + " " + gameHost.playersList[i].character.ToString() );
                                }
                            
                             });
                     
                     } else {

                        UnityMainThreadDispatcher.Enqueue(() =>
                                        {
                        broadcastInstructions(message);
                        
                                        });

                    }
                   

                    //byte[] response = Encoding.UTF8.GetBytes("Echo: " + message);
                    
                    
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error communicating with client: " + ex.Message);
            }
            finally
            {
                stream.Close();
                client.Close();
                clients.Remove(client);
                gameHost.removePlayer(clientIndex);
                
                UnityMainThreadDispatcher.Enqueue(() =>
                                        {
                        broadcastInstructions(clientIndex.ToString() + " disconnected");
                        
                        for(int i=0; i<gameHost.playersList.Count; i++){

                        broadcastInstructions("playersList" + " " + i.ToString() + " " + gameHost.playersList[i].playerName + " " + gameHost.playersList[i].team.ToString() + " " + gameHost.playersList[i].character.ToString() );
                        
                        }
                                        });
            }
        }

        public void broadcastInstructions(string instructions){


        Debug.Log("broadcast to " + clients.Count.ToString() + " client(s)");

        for(int i=0; i< clients.Count; i++){

        NetworkStream stream = clients[i].GetStream();

        instructions += "\n";
        //Debug.Log(Encoding.UTF8.GetBytes(instructions).ToString());
        
        stream.Write(Encoding.UTF8.GetBytes(instructions), 0, instructions.Length);

        //stream = null;

        Debug.Log("server sent -> " + instructions);
        //log("server sent -> " + instructions); 

        }
                        
        }


        private void ClientListenForData()
        {
            try
            {
                byte[] bytes = new byte[1024];
                while (true)
                {
                    // Check if there's any data available on the network stream
                    if (clientStream.DataAvailable)
                    {
                        int length;
                        // Read incoming stream into byte array.
                        while ((length = clientStream.Read(bytes, 0, bytes.Length)) != 0)
                        {
                            var incomingData = new byte[length];
                            Array.Copy(bytes, 0, incomingData, 0, length);
                            // Convert byte array to string message.
                            string serverMessage = Encoding.UTF8.GetString(incomingData);

                            Debug.Log("client received <- " + serverMessage);
                            serverMessage = serverMessage.Split('\n')[0];
                            UnityMainThreadDispatcher.Enqueue(() =>
                                        {
                            log("client received <- " + serverMessage);
                                        });
                            if(serverMessage.Split(' ')[0] == "chooseCharacter"){

                                UnityMainThreadDispatcher.Enqueue(() =>
                                {
                                    log(serverMessage.Split(' ')[1] + " < local index is");
                                    localClientIndex = int.Parse(serverMessage.Split(' ')[1]);

                                    startGameObjects.SetActive(true);
                                    serverPanel.SetActive(false);
                                    characterSelectionPanel.SetActive(true);
                                    characterPanel cp = characterSelectionPanel.GetComponent<characterPanel>();
                                    cp.playerName = serverMessage.Split(' ')[2];
                                    cp.clientIndex = int.Parse(serverMessage.Split(' ')[1]);
                                    //Debug.Log("name: " + cp.playerName);

                                });

                            }
                           
                           else if (serverMessage.Split(' ')[0] == "playersList"){

                             UnityMainThreadDispatcher.Enqueue(() =>
                                {

                                    gamePlayData.addPlayer(serverMessage.Split(' ')[2]);
                                    gamePlayData.playersList[int.Parse(serverMessage.Split(' ')[1])].playerIndex = int.Parse(serverMessage.Split(' ')[1]);
                                    gamePlayData.playersList[int.Parse(serverMessage.Split(' ')[1])].team = int.Parse(serverMessage.Split(' ')[3]);
                                    gamePlayData.playersList[int.Parse(serverMessage.Split(' ')[1])].character = int.Parse(serverMessage.Split(' ')[4]);

                                    if(serverMessage.Split(' ')[1] == "0"){

                                        for(int i =0; i<scoreSentinelList.transform.childCount; i++){
                                        GameObject.Destroy(scoreSentinelList.transform.GetChild(i).gameObject);
                                        }

                                        for(int i =0; i<scoreScourgeList.transform.childCount; i++){
                                        GameObject.Destroy(scoreScourgeList.transform.GetChild(i).gameObject);
                                        }
                                    
                                    }

                                GameObject newListItem = Instantiate(scoreListItem.gameObject, scoreListItem.transform) as GameObject;
                                newListItem.transform.name = "scoreListItem";
                                newListItem.GetComponentInChildren<Text>().text = serverMessage.Split(' ')[2];
                                
                                if(serverMessage.Split(' ')[3] == "0")
                                {
                                
                                 newListItem.transform.SetParent(scoreSentinelList.transform);
                               
                                } else
                                {
                               
                                 newListItem.transform.SetParent(scoreScourgeList.transform);


                                }
                                
                                });
                                
                                        
                            }
                           

                        ///////////////////////////// Personal Instructions //////////////////////////////////////////
                        /// 
                        /// 
                        /// 

                            else if(serverMessage.Split(' ')[0] == "0"){

                                if(serverMessage.Split(' ')[1] == "join"){

                                        UnityMainThreadDispatcher.Enqueue(() =>
                                        {
                                            // Debug.Log(serverMessage.Split(' ')[5]);
                                            // Debug.Log(serverMessage.Split(' ')[5].Substring(4).Remove(serverMessage.Split(' ')[5].Substring(4).Length - 1).Split(',')[0]);

                                            //Debug.Log(serverMessage);

                                            characterSelectionPanel.SetActive(false);
                                            startGame.SetActive(false);
                                            lobbyCamera.SetActive(false);
                                            //player.SetActive(true);
                                        
                                            string positionString = serverMessage.Split(' ')[5].Substring(4);
                                            float posXfloat = float.Parse(positionString.Split(',')[0]);
                                            float posYfloat = float.Parse(positionString.Split(',')[1]);
                                            float posZfloat = float.Parse(positionString.Split(',')[2].Split(')')[0]);
                                           
                                           

                                             //Debug.Log(posXString);
                                             //Debug.Log(posYString);
                                             //Debug.Log(posZString.Split(')')[0]);
   

                                           
                                            //Debug.Log(serverMessage.Split(' ')[5].Substring(4).Split(',')[2]);

                                            player = Instantiate(playerModel, new Vector3(posXfloat,posYfloat,posZfloat ), playerModel.transform.rotation);
                                            player.name = serverMessage.Split(' ')[4];
                                            player.SetActive(true);


                                            // GameObject playerCamera = lP.gameObject.transform.GetChild(0).gameObject;
                                            // playerCamera.SetActive(true);

                                            

                                            if (serverMessage.Split(' ')[3] == "0"){

                                                GameObject character = Instantiate(model0, player.transform.position, player.transform.rotation);
                                                character.name = "model0";
                                                character.transform.SetParent(player.transform);
                                                profilePicture0.SetActive(true);
                                                

                                            } else if (serverMessage.Split(' ')[3] == "1"){

                                                GameObject character = Instantiate(model1, player.transform.position, player.transform.rotation);
                                                character.name = "model1";
                                                character.transform.SetParent(player.transform);
                                                profilePicture1.SetActive(true);

                                                characterName.text = "Drow Ranger";
                                                
                                            
                                            } else if (serverMessage.Split(' ')[3] == "2"){

                                                GameObject character = Instantiate(model2, player.transform.position, player.transform.rotation);
                                                character.name = "model2";
                                                character.transform.SetParent(player.transform);
                                                profilePicture2.SetActive(true);

                                                characterName.text = "Dragon Knight";

                                            } else if (serverMessage.Split(' ')[3] == "3"){

                                                GameObject character = Instantiate(model3, player.transform.position, player.transform.rotation);
                                                character.name = "model3";
                                                character.transform.SetParent(player.transform);
                                                profilePicture3.SetActive(true);

                                                characterName.text = "Omni Knight";

                                            } else if (serverMessage.Split(' ')[3] == "4"){

                                                GameObject character = Instantiate(model4, player.transform.position, player.transform.rotation);
                                                character.name = "model4";
                                                character.transform.SetParent(player.transform);
                                                profilePicture4.SetActive(true);

                                                characterName.text = "Silencer";

                                            } else if (serverMessage.Split(' ')[3] == "5"){

                                                GameObject character = Instantiate(model5, player.transform.position, player.transform.rotation);
                                                character.name = "model5";
                                                character.transform.SetParent(player.transform);
                                                profilePicture5.SetActive(true);

                                                characterName.text = "Pudge";

                                            } else if (serverMessage.Split(' ')[3] == "6"){

                                                GameObject character = Instantiate(model6, player.transform.position, player.transform.rotation);
                                                character.name = "model6";
                                                character.transform.SetParent(player.transform);
                                                profilePicture6.SetActive(true);

                                                characterName.text = "Phantom Assassin";

                                            } else if (serverMessage.Split(' ')[3] == "7"){

                                                GameObject character = Instantiate(model7, player.transform.position, player.transform.rotation);
                                                character.name = "model7";
                                                character.transform.SetParent(player.transform);
                                                profilePicture7.SetActive(true);

                                                characterName.text = "Zeus";

                                            }

                                            playerController = player.GetComponent<CharacterController>();
                                            playerCamera = player.gameObject.transform.GetChild(0).gameObject;
                                            miniMapCam.GetComponent<Minimap>().character = player.transform;

                                            gameHost.playersList[0].anim = player.GetComponentInChildren<Animator>();

                                            game.SetActive(true);
                                            Cursor.lockState = CursorLockMode.Locked;

                                           
                                            gameHost.playersList[0].charLoaded = true;

                                            SendMessageToServer(serverMessage[0] + " joined" + "\n");

                                        });

                                    
                                    } 


                                if(serverMessage.Split(' ')[1] == "mouse"){

                                            
                                            UnityMainThreadDispatcher.Enqueue(() =>
                                            {
                                                player.transform.Rotate(new Vector3(0, float.Parse(serverMessage.Split(' ')[2]) *2,0));
                                                playerCamera.transform.Rotate(new Vector3(-float.Parse(serverMessage.Split(' ')[3]) *2,0,0));

                                            });

                                        } 
                                        
                                else if(serverMessage.Split(' ')[1] == "gravity"){

                                            UnityMainThreadDispatcher.Enqueue(() =>
                                            {
                                                playerController.Move(player.transform.TransformDirection(Vector3.down) * Time.deltaTime);

                                                //Debug.Log("Gravity");
                                            });
                                }

                                

                                        if(serverMessage.Split(' ')[1] == "movefw"){
                                            
                                            UnityMainThreadDispatcher.Enqueue(() =>
                                            {
                                                playerController.Move(player.transform.TransformDirection(Vector3.forward) * 8 * Time.deltaTime);
                                                gameHost.playersList[0].changeState("walking");
                                            
                                            });

                                        }

                                        
                                        else if(serverMessage.Split(' ')[1] == "movebw"){
                                            
                                            UnityMainThreadDispatcher.Enqueue(() =>
                                            {
                                                playerController.Move(player.transform.TransformDirection(-Vector3.forward) * 8 * Time.deltaTime);

                                                gameHost.playersList[0].changeState("walking");
                                            });

                                        }

                                        else if(serverMessage.Split(' ')[1] == "movelf"){
                                            
                                            UnityMainThreadDispatcher.Enqueue(() =>
                                            {
                                                playerController.Move(player.transform.TransformDirection(-Vector3.right) * 8 * Time.deltaTime);
                                                gameHost.playersList[0].changeState("walking");
                                            
                                            });

                                        }

                                        else if(serverMessage.Split(' ')[1] == "moverg"){
                                            
                                            UnityMainThreadDispatcher.Enqueue(() =>
                                            {
                                                playerController.Move(player.transform.TransformDirection(Vector3.right) * 8 * Time.deltaTime);
                                                gameHost.playersList[0].changeState("walking");
                                            
                                            });

                                        }
                                        
                                        
                                         else if(serverMessage.Split(' ')[1] == "idle" && gameHost.playersList[0].charLoaded == true) {
                                            
                                            UnityMainThreadDispatcher.Enqueue(() =>
                                            {  
                                                gameHost.playersList[0].changeState("idle");
                                        
                                            });

                                        }

                                        else if(serverMessage.Split(' ')[1] == "tab"){

                                             UnityMainThreadDispatcher.Enqueue(() =>
                                            {  

                                            scoreBoard.SetActive(true);

                                            });


                                        } else if(serverMessage.Split(' ')[1] == "tabup"){

                                             UnityMainThreadDispatcher.Enqueue(() =>
                                            {  


                                            scoreBoard.SetActive(false);

                                             });
                                        }

                            } else if (serverMessage.Split(' ')[0] != "0")
                            {
                                if(serverMessage.Split(' ')[0] == localClientIndex.ToString())
                                {

                                     if(serverMessage.Split(' ')[1] == "join"){

                                        UnityMainThreadDispatcher.Enqueue(() =>
                                        {

                                            for(int i=0; i< int.Parse(serverMessage.Split(' ')[0]); i++)
                                            {
                                                gamePlayData.addPlayer("");
                                            }

                                            gamePlayData.playersList[gamePlayData.playersList.Count -1].playerName = serverMessage.Split(' ')[4];
                                            gamePlayData.playersList[gamePlayData.playersList.Count -1].team = int.Parse(serverMessage.Split(' ')[2]);
                                            gamePlayData.playersList[gamePlayData.playersList.Count -1].character = int.Parse(serverMessage.Split(' ')[3]);

                                            characterSelectionPanel.SetActive(false);
                                            startGame.SetActive(false);
                                            lobbyCamera.SetActive(false);
                                            //player.SetActive(true);
                                        
                                            string positionString = serverMessage.Split(' ')[5].Substring(4);
                                            float posXfloat = float.Parse(positionString.Split(',')[0]);
                                            float posYfloat = float.Parse(positionString.Split(',')[1]);
                                            float posZfloat = float.Parse(positionString.Split(',')[2].Split(')')[0]);
                                           
                                           

                                             //Debug.Log(posXString);
                                             //Debug.Log(posYString);
                                             //Debug.Log(posZString.Split(')')[0]);
   

                                           
                                            //Debug.Log(serverMessage.Split(' ')[5].Substring(4).Split(',')[2]);

                                            player = Instantiate(playerModel, new Vector3(posXfloat,posYfloat,posZfloat ), playerModel.transform.rotation);
                                            player.name = serverMessage.Split(' ')[4];
                                            player.SetActive(true);


                                            // GameObject playerCamera = lP.gameObject.transform.GetChild(0).gameObject;
                                            // playerCamera.SetActive(true);

                                            

                                            if (serverMessage.Split(' ')[3] == "0"){

                                                GameObject character = Instantiate(model0, player.transform.position, player.transform.rotation);
                                                character.name = "model0";
                                                character.transform.SetParent(player.transform);
                                                profilePicture0.SetActive(true);
                                                

                                            } else if (serverMessage.Split(' ')[3] == "1"){

                                                GameObject character = Instantiate(model1, player.transform.position, player.transform.rotation);
                                                character.name = "model1";
                                                character.transform.SetParent(player.transform);
                                                profilePicture1.SetActive(true);

                                                characterName.text = "Drow Ranger";
                                                
                                            
                                            } else if (serverMessage.Split(' ')[3] == "2"){

                                                GameObject character = Instantiate(model2, player.transform.position, player.transform.rotation);
                                                character.name = "model2";
                                                character.transform.SetParent(player.transform);
                                                profilePicture2.SetActive(true);

                                                characterName.text = "Dragon Knight";

                                            } else if (serverMessage.Split(' ')[3] == "3"){

                                                GameObject character = Instantiate(model3, player.transform.position, player.transform.rotation);
                                                character.name = "model3";
                                                character.transform.SetParent(player.transform);
                                                profilePicture3.SetActive(true);

                                                characterName.text = "Omni Knight";

                                            } else if (serverMessage.Split(' ')[3] == "4"){

                                                GameObject character = Instantiate(model4, player.transform.position, player.transform.rotation);
                                                character.name = "model4";
                                                character.transform.SetParent(player.transform);
                                                profilePicture4.SetActive(true);

                                                characterName.text = "Silencer";

                                            } else if (serverMessage.Split(' ')[3] == "5"){

                                                GameObject character = Instantiate(model5, player.transform.position, player.transform.rotation);
                                                character.name = "model5";
                                                character.transform.SetParent(player.transform);
                                                profilePicture5.SetActive(true);

                                                characterName.text = "Pudge";

                                            } else if (serverMessage.Split(' ')[3] == "6"){

                                                GameObject character = Instantiate(model6, player.transform.position, player.transform.rotation);
                                                character.name = "model6";
                                                character.transform.SetParent(player.transform);
                                                profilePicture6.SetActive(true);

                                                characterName.text = "Phantom Assassin";

                                            } else if (serverMessage.Split(' ')[3] == "7"){

                                                GameObject character = Instantiate(model7, player.transform.position, player.transform.rotation);
                                                character.name = "model7";
                                                character.transform.SetParent(player.transform);
                                                profilePicture7.SetActive(true);

                                                characterName.text = "Zeus";

                                            }

                                            playerController = player.GetComponent<CharacterController>();
                                            playerCamera = player.gameObject.transform.GetChild(0).gameObject;
                                            miniMapCam.GetComponent<Minimap>().character = player.transform;

                                            // for(int i=0; i< gamePlayData.playersList.Count; i++)
                                            // {
                                                
                                            //     if( gamePlayData.playersList[localClientIndex] = )

                                            // }

                                            gamePlayData.playersList[gamePlayData.playersList.Count -1].anim = player.GetComponentInChildren<Animator>();

                                            game.SetActive(true);
                                            Cursor.lockState = CursorLockMode.Locked;

                                           
                                            gamePlayData.playersList[gamePlayData.playersList.Count -1].charLoaded = true;

                                            SendMessageToServer(serverMessage[0] + " joined" + "\n");

                                        });

                                     }

                                    if(serverMessage.Split(' ')[1] == "mouse"){

                                            
                                            UnityMainThreadDispatcher.Enqueue(() =>
                                            {
                                                player.transform.Rotate(new Vector3(0, float.Parse(serverMessage.Split(' ')[2]) *2,0));
                                                playerCamera.transform.Rotate(new Vector3(-float.Parse(serverMessage.Split(' ')[3]) *2,0,0));

                                            });

                                        } 
                                        
                                    else if(serverMessage.Split(' ')[1] == "gravity"){

                                                UnityMainThreadDispatcher.Enqueue(() =>
                                                {
                                                    playerController.Move(player.transform.TransformDirection(Vector3.down) * Time.deltaTime);

                                                    //Debug.Log("Gravity");
                                                });
                                    }

                                

                                        if(serverMessage.Split(' ')[1] == "movefw"){
                                            
                                            UnityMainThreadDispatcher.Enqueue(() =>
                                            {
                                                playerController.Move(player.transform.TransformDirection(Vector3.forward) * 8 * Time.deltaTime);
                                                gamePlayData.playersList[0].changeState("walking");
                                            
                                            });

                                        }

                                        
                                        else if(serverMessage.Split(' ')[1] == "movebw"){
                                            
                                            UnityMainThreadDispatcher.Enqueue(() =>
                                            {
                                                playerController.Move(player.transform.TransformDirection(-Vector3.forward) * 8 * Time.deltaTime);

                                                gamePlayData.playersList[0].changeState("walking");
                                            });

                                        }

                                        else if(serverMessage.Split(' ')[1] == "movelf"){
                                            
                                            UnityMainThreadDispatcher.Enqueue(() =>
                                            {
                                                playerController.Move(player.transform.TransformDirection(-Vector3.right) * 8 * Time.deltaTime);
                                                gamePlayData.playersList[0].changeState("walking");
                                            
                                            });

                                        }

                                        else if(serverMessage.Split(' ')[1] == "moverg"){
                                            
                                            UnityMainThreadDispatcher.Enqueue(() =>
                                            {
                                                playerController.Move(player.transform.TransformDirection(Vector3.right) * 8 * Time.deltaTime);
                                                gamePlayData.playersList[0].changeState("walking");
                                            
                                            });

                                        }
                                        
                                        
                                         else if(serverMessage.Split(' ')[1] == "idle" && gamePlayData.playersList[0].charLoaded == true) {
                                            
                                            UnityMainThreadDispatcher.Enqueue(() =>
                                            {  
                                                gamePlayData.playersList[0].changeState("idle");
                                        
                                            });

                                        }

                                        else if(serverMessage.Split(' ')[1] == "tab"){

                                             UnityMainThreadDispatcher.Enqueue(() =>
                                            {  

                                            scoreBoard.SetActive(true);

                                            });


                                        } else if(serverMessage.Split(' ')[1] == "tabup"){

                                             UnityMainThreadDispatcher.Enqueue(() =>
                                            {  


                                            scoreBoard.SetActive(false);

                                             });
                                        }

                                }
                                
                            }

                            if(serverMessage.Split(' ')[1] == "disconnected")
                            {
                                gamePlayData.removePlayer(int.Parse(serverMessage.Split(' ')[0]));
                            }

                            /////////////////////////////////// Other Players Instrucions ///////////////////////////////////////
                            /// 
                            /// 

                            //  } else if(serverMessage.Split(' ')[0] != localClientIndex.ToString()){


                            //  }

                            //Debug.Log("server message: " + serverMessage);
                        }
                    }
                }
            }
            catch (SocketException socketException)
            {
                Debug.Log("Socket exception: " + socketException);
            }
        }

        public void SendMessageToServer(string message)
        {
            if (client == null || !client.Connected)
            {
                Debug.LogError("Client not connected to server.");
                return;
            }

            byte[] data = Encoding.UTF8.GetBytes(message + "\n");
            clientStream.Write(data, 0, data.Length);
            Debug.Log("client sent -> " + message);
            UnityMainThreadDispatcher.Enqueue(() =>
                                        {
            log("client sent -> " + message);
                                        });
        }

       
    }

}
