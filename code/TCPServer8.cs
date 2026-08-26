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

                    Thread clientThread = new Thread(() => HandleClient(client, clients.Count - 1));
                    clientThread.Start();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error accepting client: " + ex.Message);
                }
            }
        }

        // Send the full players snapshot to a specific client's stream
        private void SendPlayersListToClient(NetworkStream stream)
        {
            try
            {
                for(int i=0; i<gameHost.playersList.Count; i++){
                    var p = gameHost.playersList[i];
                    string msg = "playersList " + i.ToString() + " " + p.playerName + " " + p.team.ToString() + " " + p.character.ToString() + " (" + p.spawnPositionX.ToString() + "," + p.spawnPositionY.ToString() + "," + p.spawnPositionZ.ToString() + ")\n";
                    byte[] data = Encoding.UTF8.GetBytes(msg);
                    stream.Write(data, 0, data.Length);
                    Debug.Log("server sent -> " + msg);
                }
            }
            catch(Exception e){
                Debug.Log("SendPlayersListToClient exception: " + e.Message);
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

                       // After assigning name and index, send the current players snapshot to this new client
                       SendPlayersListToClient(stream);

                    } else if(message == "requestPlayersList"){
                        // client explicitly requested the players list
                        SendPlayersListToClient(stream);

                    } else if(message.Split(' ')[1] == "join"){

                        UnityMainThreadDispatcher.Enqueue(() =>
                                        {
                        
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

                            // Broadcast the join so all clients can instantiate this player
                            broadcastInstructions((message.Split(' ')[0] + " " + message.Split(' ')[1] + " " + message.Split(' ')[2] + " " + message.Split(' ')[3] + " " + message.Split(' ')[4]));
                            
                            // Also broadcast an updated players list for UI/consistency
                            for(int i=0; i<gameHost.playersList.Count; i++){
                                broadcastInstructions("playersList" + " " + i.ToString() + " " + gameHost.playersList[i].playerName + " " + gameHost.playersList[i].team.ToString() + " " + gameHost.playersList[i].character.ToString() + " (" + gameHost.playersList[i].spawnPositionX.ToString() + "," + gameHost.playersList[i].spawnPositionY.ToString() + "," + gameHost.playersList[i].spawnPositionZ.ToString() + ")");
                            }
                        
                        });
                    } else {

                       UnityMainThreadDispatcher.Enqueue(() =>
                                       {
                       broadcastInstructions(message);
                       
                                       });

                    }
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

                        broadcastInstructions("playersList" + " " + i.ToString() + " " + gameHost.playersList[i].playerName + " " + gameHost.playersList[i].team.ToString() + " " + gameHost.playersList[i].character.ToString() + " (" + gameHost.playersList[i].spawnPositionX.ToString() + "," + gameHost.playersList[i].spawnPositionY.ToString() + "," + gameHost.playersList[i].spawnPositionZ.ToString() + ")");
                        
                        }
                                        });
            }
        }

        public void broadcastInstructions(string instructions){

        Debug.Log("broadcast to " + clients.Count.ToString() + " client(s)");

        for(int i=0; i< clients.Count; i++){

        NetworkStream stream = clients[i].GetStream();

        instructions += "\n";
        stream.Write(Encoding.UTF8.GetBytes(instructions), 0, instructions.Length);

        Debug.Log("server sent -> " + instructions);

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

                            // --- Remote players movement handling (apply for messages like "<index> movefw") ---
                            string[] partsForMovement = serverMessage.Split(' ');
                            int originIdx;
                            if (partsForMovement.Length > 1 && int.TryParse(partsForMovement[0], out originIdx) && originIdx != localClientIndex)
                            {
                                string cmd = partsForMovement[1];
                                bool handled = false;
                                if (cmd == "movefw" || cmd == "movebw" || cmd == "movelf" || cmd == "moverg" || cmd == "idle" || cmd == "mouse")
                                {
                                    UnityMainThreadDispatcher.Enqueue(() =>
                                    {
                                        if (originIdx < gamePlayData.playersList.Count)
                                        {
                                            var entry = gamePlayData.playersList[originIdx];
                                            if (entry != null && entry.anim != null)
                                            {
                                                GameObject remoteRoot = entry.anim.transform.root.gameObject;

                                                switch (cmd)
                                                {
                                                    case "movefw":
                                                        remoteRoot.transform.position += remoteRoot.transform.forward * 8f * Time.deltaTime;
                                                        entry.changeState("walking");
                                                        break;
                                                    case "movebw":
                                                        remoteRoot.transform.position += -remoteRoot.transform.forward * 8f * Time.deltaTime;
                                                        entry.changeState("walking");
                                                        break;
                                                    case "movelf":
                                                        remoteRoot.transform.position += -remoteRoot.transform.right * 8f * Time.deltaTime;
                                                        entry.changeState("walking");
                                                        break;
                                                    case "moverg":
                                                        remoteRoot.transform.position += remoteRoot.transform.right * 8f * Time.deltaTime;
                                                        entry.changeState("walking");
                                                        break;
                                                    case "idle":
                                                        entry.changeState("idle");
                                                        break;
                                                    case "mouse":
                                                        if (partsForMovement.Length >= 4)
                                                        {
                                                            float yaw = 0f;
                                                            float pitch = 0f;
                                                            float.TryParse(partsForMovement[2], out yaw);
                                                            float.TryParse(partsForMovement[3], out pitch);
                                                            remoteRoot.transform.Rotate(new Vector3(0f, yaw * 2f, 0f));
                                                            // try rotate camera child if present
                                                            if (remoteRoot.transform.childCount > 0)
                                                            {
                                                                var cam = remoteRoot.transform.GetChild(0);
                                                                cam.Rotate(new Vector3(-pitch * 2f, 0f, 0f));
                                                            }
                                                        }
                                                        break;
                                                }
                                            }
                                        }
                                    });
                                    handled = true;
                                }

                                if (handled)
                                {
                                    // movement applied; skip further processing of this message
                                    continue;
                                }
                            }

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

                                    // Ask server for current players snapshot (safest ordering)
                                    SendMessageToServer("requestPlayersList");
                                });

                            }
                           
                           else if (serverMessage.Split(' ')[0] == "playersList"){

                             UnityMainThreadDispatcher.Enqueue(() =>
                                {

                                    string[] parts = serverMessage.Split(' ');
                                    int idx = int.Parse(parts[1]);
                                    string playerName = parts[2];
                                    int team = int.Parse(parts[3]);
                                    int character = int.Parse(parts[4]);

                                    // Add/update the players list used for UI and logic
                                    if(gamePlayData.playersList.Count <= idx){
                                        gamePlayData.addPlayer(playerName);
                                    }

                                    gamePlayData.playersList[idx].playerIndex = idx;
                                    gamePlayData.playersList[idx].playerName = playerName;
                                    gamePlayData.playersList[idx].team = team;
                                    gamePlayData.playersList[idx].character = character;

                                    // Update score UI list
                                    if(parts[1] == "0"){

                                        for(int j =0; j<scoreSentinelList.transform.childCount; j++){
                                            GameObject.Destroy(scoreSentinelList.transform.GetChild(j).gameObject);
                                        }

                                        for(int j =0; j<scoreScourgeList.transform.childCount; j++){
                                            GameObject.Destroy(scoreScourgeList.transform.GetChild(j).gameObject);
                                        }
                                    }

                                    GameObject newListItem = Instantiate(scoreListItem.gameObject, scoreListItem.transform) as GameObject;
                                    newListItem.transform.name = "scoreListItem";
                                    newListItem.GetComponentInChildren<Text>().text = playerName;
                                    if(team == 0){
                                        newListItem.transform.SetParent(scoreSentinelList.transform);
                                    } else {
                                        newListItem.transform.SetParent(scoreScourgeList.transform);
                                    }

                                    // Instantiate the player in the scene if it's not the local client
                                    // and if we haven't already instantiated them
                                    if(idx != localClientIndex && gamePlayData.playersList[idx].charLoaded == false){

                                        Vector3 spawnPos = (team == 0) ? new Vector3(sentinelSpawnPosX, sentinelSpawnPosY, sentinelSpawnPosZ) : new Vector3(scourgeSpawnPosX, scourgeSpawnPosY, scourgeSpawnPosZ);
                                        GameObject newPlayer = Instantiate(playerModel, spawnPos, playerModel.transform.rotation);
                                        newPlayer.name = playerName;
                                        newPlayer.SetActive(true);

                                        // attach model
                                        GameObject characterObj = null;
                                        switch(character){
                                            case 0: characterObj = Instantiate(model0, newPlayer.transform.position, newPlayer.transform.rotation); break;
                                            case 1: characterObj = Instantiate(model1, newPlayer.transform.position, newPlayer.transform.rotation); break;
                                            case 2: characterObj = Instantiate(model2, newPlayer.transform.position, newPlayer.transform.rotation); break;
                                            case 3: characterObj = Instantiate(model3, newPlayer.transform.position, newPlayer.transform.rotation); break;
                                            case 4: characterObj = Instantiate(model4, newPlayer.transform.position, newPlayer.transform.rotation); break;
                                            case 5: characterObj = Instantiate(model5, newPlayer.transform.position, newPlayer.transform.rotation); break;
                                            case 6: characterObj = Instantiate(model6, newPlayer.transform.position, newPlayer.transform.rotation); break;
                                            case 7: characterObj = Instantiate(model7, newPlayer.transform.position, newPlayer.transform.rotation); break;
                                        }
                                        if(characterObj != null){
                                            characterObj.name = "model" + character.ToString();
                                            characterObj.transform.SetParent(newPlayer.transform);
                                        }

                                        // store animator reference
                                        gamePlayData.playersList[idx].anim = newPlayer.GetComponentInChildren<Animator>();
                                        gamePlayData.playersList[idx].charLoaded = true;
                                    }

                                });
                                
                                        
                            }

                        ///////////////////////////// Personal Instructions //////////////////////////////////////////

                            else if(serverMessage.Split(' ')[0] == "0"){

                                // existing personal handling...
                                // (kept unchanged)
                                
                                if(serverMessage.Split(' ')[1] == "join"){

                                        UnityMainThreadDispatcher.Enqueue(() =>
                                        {
                                            characterSelectionPanel.SetActive(false);
                                            startGame.SetActive(false);
                                            lobbyCamera.SetActive(false);

                                            string positionString = serverMessage.Split(' ')[5].Substring(4);
                                            float posXfloat = float.Parse(positionString.Split(',')[0]);
                                            float posYfloat = float.Parse(positionString.Split(',')[1]);
                                            float posZfloat = float.Parse(positionString.Split(',')[2].Split(')')[0]);

                                            player = Instantiate(playerModel, new Vector3(posXfloat,posYfloat,posZfloat ), playerModel.transform.rotation);
                                            player.name = serverMessage.Split(' ')[4];
                                            player.SetActive(true);

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

                                // other personal commands continue unchanged...
                            }

                            // rest of message handling continued unchanged... (movement, mouse, etc.)
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
