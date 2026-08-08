using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using Random = UnityEngine.Random;
using UnityEngine.UI;
using gameData;


namespace networking {

    public class TCPServer : MonoBehaviour
    {


    

        private TcpListener listener;
        private TcpClient client;
        private NetworkStream clientStream;
        private Thread clientThread;

        private List<TcpClient> clients = new List<TcpClient>(); // Keeps track of connected clients
        private List<connectedPlayer> playerList = new List<connectedPlayer>();
        private connectedPlayer localPlayer = new connectedPlayer();

        public gamePlay gameHost = new gamePlay();
        public gamePlay gamePlayData = new gamePlay();

        private string serverIP;
        private int serverPort = 2026;

        private bool hosting = false;

        [SerializeField] public GameObject startGameObjects, serverPanel, characterSelectionPanel, lobbyCamera, player, playerCamera, startGame, game, sentinelSpawnPosition, scourgeSpawnPosition;
        [SerializeField] public GameObject model0, model1, model2, model3, model4, model5, model6, model7;
        [SerializeField] public GameObject playerModel, sentinelPlayersEmpty, scourgePlayersEmpty;

        [SerializeField] private Text minutesText, secondsText;

        private GameObject[] players;
        private GameObject[] sentinelPlayers, scourgePlayers;

        CharacterController playerController;

        [SerializeField] private Button joinServerButton, createServerButton ;
        [SerializeField] private InputField inputPlayerName, inputAddress;
        [SerializeField] private Text localIpText, localIpText2, localIpText3, localIpText4;

        [SerializeField] private GameObject spawnPositionSentinel, spawnPositionScourge;

        public int localClientIndex;

        bool gameStarted = false;
        float timer = 560;

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
        
        //  IEnumerator mainThreadTasks()
        //  {
        //     while (jobsQue.Count > 0) {
        //         jobsQue.Dequeue().Invoke();

        //     }

        //     yield return new WaitForSeconds(.1f);
        // }

        
        public void Update(){

         

            if(gameHost.hosting){

            gameHost.timeLeft -= Time.deltaTime;

            //minutesText.text = Mathf.RoundToInt(gameHost.timeLeft / 60).ToString();
            secondsText.text = Mathf.RoundToInt(gameHost.timeLeft).ToString();

            if(gameHost.timeLeft <= 0){ gameHost.timeLeft = 560;}


            broadcastInstructions("timeleft " + gameHost.timeLeft.ToString());

            }        

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

                    Thread clientThread = new Thread(() => HandleClient(client));
                    clientThread.Start();

                    
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error accepting client: " + ex.Message);
                }
            }
        }

        private void HandleClient(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];
            int bytesRead;

            

            try
            {
                

                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {

                    
                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    //Debug.Log(message.Split(' ')[1]);

                    string[] messageList = message.Split(' ');
                    byte[] response = Encoding.UTF8.GetBytes(message);

                    int localIndex;

                    if(message.Split(' ')[0] == "setName"){

                       gameHost.addPlayer(message.Split(' ')[1]);


                        response = Encoding.UTF8.GetBytes("chooseCharacter " + gameHost.Count().ToString() + " " + message.Split(' ')[1] );
                        stream.Write(response, 0, response.Length);
                        //Debug.Log(response);
                    } 
                    
                     else if(message.Split(' ')[1] == "join"){

                            gameHost.playersList[int.Parse(message.Split(' ')[0])].team = int.Parse(message.Split(' ')[2]);
                            gameHost.playersList[int.Parse(message.Split(' ')[0])].character = int.Parse(message.Split(' ')[3]);
                        
                            gameHost.playersList[int.Parse(message.Split(' ')[0])].Start();

                            if(message.Split(' ')[2] == "1"){
                            broadcastInstructions(message + " " + "pos(" + gameHost.playersList[int.Parse(message.Split(' ')[0])].scourgeSpawnPosX.ToString() + "," + gameHost.playersList[int.Parse(message.Split(' ')[0])].scourgeSpawnPosY.ToString() + "," + gameHost.playersList[int.Parse(message.Split(' ')[0])].scourgeSpawnPosZ.ToString() + ")");
                            } else if(message.Split(' ')[2] == "0"){
                            broadcastInstructions(message + " " + "pos(" + gameHost.playersList[int.Parse(message.Split(' ')[0])].sentinelSpawnPosX.ToString() + "," + gameHost.playersList[int.Parse(message.Split(' ')[0])].sentinelSpawnPosY.ToString() + "," + gameHost.playersList[int.Parse(message.Split(' ')[0])].sentinelSpawnPosZ.ToString() + ")");
                            }
                     
                     } else if(message.Split(' ')[1] == "joined"){

                            for(int i=0; i<gameHost.playersList.Count; i++){

                                    // float posx, posy, posz;
                                    // if(int.Parse(gameHost.playersList[i].team) == "0"){
                                    //     pos
                                    // }
                                    // + " " + "pos(" + gameHost.playersList[i].spawnPosX + "," + gameHost.playersList[i].spawnPosY + "," + gameHost.playersList[i].spawnPosZ + ")" 

                                broadcastInstructions("playersList " + i.ToString() + " " + gameHost.playersList[i].playerName + " " + gameHost.playersList[i].team + " " + gameHost.playersList[i].character );
                            }
                     }

                     else {

                        broadcastInstructions(message);

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
                Console.WriteLine("Client disconnected.");
            }
        }

        public void broadcastInstructions(string instructions){

        for(int i=0; i< clients.Count; i++){

        NetworkStream stream = clients[i].GetStream();

        //Debug.Log("client " + i.ToString());
        
        stream.Write(Encoding.UTF8.GetBytes(instructions), 0, instructions.Length);

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

                            

                            if(serverMessage.Split(' ')[0] == "chooseCharacter"){

                                UnityMainThreadDispatcher.Enqueue(() =>
                                {
                                    
                                    localClientIndex = int.Parse(serverMessage.Split(' ')[1]);

                                    startGameObjects.SetActive(true);
                                    serverPanel.SetActive(false);
                                    characterSelectionPanel.SetActive(true);
                                    characterPanel cp = characterSelectionPanel.GetComponent<characterPanel>();
                                    cp.playerName = serverMessage.Split(' ')[2];

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
                                    Debug.Log(serverMessage.Split(' ')[5]);
                                    Debug.Log(serverMessage.Split(' ')[5].Substring(4).Remove(serverMessage.Split(' ')[5].Substring(4).Length - 1).Split(',')[0]);

                                    characterSelectionPanel.SetActive(false);
                                    startGame.SetActive(false);
                                    lobbyCamera.SetActive(false);
                                    //player.SetActive(true);
                                   

                                    player = Instantiate(playerModel, new Vector3(float.Parse(serverMessage.Split(' ')[5].Substring(4).Remove(serverMessage.Split(' ')[5].Substring(4).Length - 1).Split(',')[0]) , float.Parse(serverMessage.Split(' ')[5].Substring(4).Remove(serverMessage.Split(' ')[5].Substring(4).Length - 1).Split(',')[1]) , float.Parse(serverMessage.Split(' ')[5].Substring(4).Remove(serverMessage.Split(' ')[5].Substring(4).Length - 1).Split(',')[2]) ), playerModel.transform.rotation);
                                    player.name = serverMessage.Split(' ')[4];
                                    player.SetActive(true);


                                    // GameObject playerCamera = lP.gameObject.transform.GetChild(0).gameObject;
                                    // playerCamera.SetActive(true);

                                    if (serverMessage.Split(' ')[3] == "0"){

                                        GameObject character = Instantiate(model0, player.transform.position, player.transform.rotation);
                                        character.name = "model0";
                                        character.transform.SetParent(player.transform);
                                        

                                    } else if (serverMessage.Split(' ')[3] == "1"){

                                        GameObject character = Instantiate(model1, player.transform.position, player.transform.rotation);
                                        character.name = "model1";
                                        character.transform.SetParent(player.transform);
                                    
                                    } else if (serverMessage.Split(' ')[3] == "2"){

                                        GameObject character = Instantiate(model2, player.transform.position, player.transform.rotation);
                                        character.name = "model2";
                                        character.transform.SetParent(player.transform);

                                    } else if (serverMessage.Split(' ')[3] == "3"){

                                        GameObject character = Instantiate(model3, player.transform.position, player.transform.rotation);
                                        character.name = "model3";
                                        character.transform.SetParent(player.transform);

                                    } else if (serverMessage.Split(' ')[3] == "4"){

                                        GameObject character = Instantiate(model4, player.transform.position, player.transform.rotation);
                                        character.name = "model4";
                                        character.transform.SetParent(player.transform);

                                    } else if (serverMessage.Split(' ')[3] == "5"){

                                        GameObject character = Instantiate(model5, player.transform.position, player.transform.rotation);
                                        character.name = "model5";
                                        character.transform.SetParent(player.transform);

                                    } else if (serverMessage.Split(' ')[3] == "6"){

                                        GameObject character = Instantiate(model6, player.transform.position, player.transform.rotation);
                                        character.name = "model6";
                                        character.transform.SetParent(player.transform);

                                    } else if (serverMessage.Split(' ')[3] == "7"){

                                        GameObject character = Instantiate(model7, player.transform.position, player.transform.rotation);
                                        character.name = "model7";
                                        character.transform.SetParent(player.transform);

                                    }

                                    playerController = player.GetComponent<CharacterController>();
                                    playerCamera = player.gameObject.transform.GetChild(0).gameObject;

                                    game.SetActive(true);
                                    Cursor.lockState = CursorLockMode.Locked;

                                    SendMessageToServer(serverMessage[0] + " joined");

                                });
                            } else if (serverMessage.Split(' ')[0] == "playersList"){

                                Debug.Log("connected player count");
                            }


                                if(serverMessage.Split(' ')[1] == "mouse"){
                                    UnityMainThreadDispatcher.Enqueue(() =>
                                    {
                                        player.transform.Rotate(new Vector3(0, float.Parse(serverMessage.Split(' ')[2]) *2,0));
                                        playerCamera.transform.Rotate(new Vector3(-float.Parse(serverMessage.Split(' ')[3]) *2,0,0));

                                    });

                                } else if(serverMessage.Split(' ')[1] == "gravity"){

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

                                       
                                    });

                                }

                                
                                if(serverMessage.Split(' ')[1] == "movebw"){
                                    
                                    UnityMainThreadDispatcher.Enqueue(() =>
                                    {
                                        playerController.Move(player.transform.TransformDirection(-Vector3.forward) * 8 * Time.deltaTime);

                                       
                                    });

                                }

                                if(serverMessage.Split(' ')[1] == "movelf"){
                                    
                                    UnityMainThreadDispatcher.Enqueue(() =>
                                    {
                                        playerController.Move(player.transform.TransformDirection(-Vector3.right) * 8 * Time.deltaTime);

                                       
                                    });

                                }

                                 if(serverMessage.Split(' ')[1] == "moverg"){
                                    
                                    UnityMainThreadDispatcher.Enqueue(() =>
                                    {
                                        playerController.Move(player.transform.TransformDirection(Vector3.right) * 8 * Time.deltaTime);

                                       
                                    });

                                }

                            /////////////////////////////////// Other Players Instrucions ///////////////////////////////////////
                            /// 
                            /// 

                             } else if(serverMessage.Split(' ')[0] != localClientIndex.ToString()){


                             }

                            Debug.Log("server message: " + serverMessage);
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

            byte[] data = Encoding.UTF8.GetBytes(message);
            clientStream.Write(data, 0, data.Length);
            Debug.Log("client sent message to server -> " + message);
        }

       
    }

}
