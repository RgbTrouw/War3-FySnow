using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
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

        [SerializeField] public GameObject startGameObjects, serverPanel, characterSelectionPanel, lobbyCamera, player, startGame, game;

        [SerializeField] private Button joinServerButton, createServerButton ;
        [SerializeField] private InputField inputPlayerName, inputAddress;
        [SerializeField] private Text localIpText;

        public int localClientIndex;

        bool gameStarted = false;
        float timer = 560;

        public void Start()
        {
        
            

            joinServerButton.onClick.AddListener(joinServer);
            createServerButton.onClick.AddListener(createServer);

            startGameObjects.SetActive(true);
            serverPanel.SetActive(true);
            characterSelectionPanel.SetActive(false);


            inputPlayerName.text = PlayerPrefs.GetString("playerName");
            inputPlayerName.onValueChanged.AddListener(delegate {setPlayerName(); });

            localIpText.text = Dns.GetHostEntry(Dns.GetHostName()).AddressList[0].ToString();
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

            if(gameStarted == true){

            timer -= Time.deltaTime;
            
            

            if(timer <= 0 ){
                timer = 560;
            }


            Debug.Log(timer);
            //Debug.Log(localPlayer.selectedCharacter + " " + localPlayer.selectedTeam + " " + timer);
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

            hosting = true;
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

                        playerList.Add(new connectedPlayer());
                        playerList[playerList.Count - 1 ].playerName = message.Substring(8);

                        localIndex = playerList.Count - 1;
                        

                        response = Encoding.UTF8.GetBytes("initializeGame " + localIndex.ToString());
                        stream.Write(response, 0, response.Length);
                    } 
                    
                    else if(message.Split(' ')[1] == "go"){

                        localClientIndex = int.Parse(message.Split(' ')[0]);
                        playerList[localClientIndex].selectedTeam = int.Parse(message.Split(' ')[2]);
                        playerList[localClientIndex].selectedCharacter = int.Parse(message.Split(' ')[3]); 

                        localPlayer.selectedTeam = int.Parse(message.Split(' ')[2]);
                        localPlayer.selectedCharacter = int.Parse(message.Split(' ')[3]); 


                        Debug.Log(message.Split(' ')[1]);
                        
                        gameStarted = true;
                        
                        response = Encoding.UTF8.GetBytes("gameStarted " + localClientIndex.ToString());
                        stream.Write(response, 0, response.Length);
                        
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

                            if(serverMessage.Split(' ')[0] == "initializeGame"){

                                UnityMainThreadDispatcher.Enqueue(() =>
                                {
                                    
                                    localClientIndex = int.Parse(serverMessage.Split(' ')[1]);

                                    startGameObjects.SetActive(true);
                                    serverPanel.SetActive(false);
                                    characterSelectionPanel.SetActive(true);
                                });

                            }
                             if(serverMessage.Split(' ')[0] == "gameStarted"){

                                UnityMainThreadDispatcher.Enqueue(() =>
                                {
                                    characterSelectionPanel.SetActive(false);
                                    startGame.SetActive(false);
                                    lobbyCamera.SetActive(false);
                                    player.SetActive(true);
                                    game.SetActive(true);
                                });

                             }

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

            byte[] data = Encoding.UTF8.GetBytes(message);
            clientStream.Write(data, 0, data.Length);
            //Debug.Log("client sent message -> " + message);
        }

        
    }

}
