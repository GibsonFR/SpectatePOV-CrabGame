namespace SpectatePOV
{
    //Ici on stock les fonctions, dans des class pour la lisibilité du code dans Plugin.cs 

    //Cette class regroupe un ensemble de fonction plus ou moins utile
    public class Utility
    {
        //Cette fonction envoie un message dans le chat de la part du client
        public static void SendMessage(string message)
        {
            Variables.chatBoxInstance.SendMessage(message);
        }

        //Cette fonction envoie un message dans le chat de la part du client en mode Force (seul le client peut voir le message)
        public static void ForceMessage(string message)
        {
            if (Variables.displayMessageInChat)
                Variables.chatBoxInstance.ForceMessage(message);
        }

        //Cette fonction envoie un message dans le chat de la part du server, marche uniquement en tant que Host de la partie
        public static void SendServerMessage(string message)
        {
            ServerSend.SendChatMessage(1, message);
        }

        //Cette Fonction permet d'écrire une ligne dans un fichier txt
        public static void Log(string path, string line)
        {
            // Utiliser StreamWriter pour ouvrir le fichier et écrire à la fin
            using (StreamWriter writer = new StreamWriter(path, true))
            {
                writer.WriteLine(line); // Écrire la nouvelle ligne
            }
        }

        //Cette fonction vérifie si une fonction crash sans interrompre le fonctionnement d'une class/fonction, et retourne un booleen
        public static bool DoesFunctionCrash(Action function, string functionName, string logPath)
        {
            try
            {
                function.Invoke();
                return false;
            }
            catch (Exception ex)
            {
                Log(logPath, $"[{GetCurrentTime()}] Erreur [{functionName}]: {ex.Message}");
                return true;
            }
        }
        //Cette fonction créer un dossier si il n'existe pas déjà
        public static void CreateFolder(string path, string logPath)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }
            catch (Exception ex)
            {
                Log(logPath, "Erreur [CreateFolder] : " + ex.Message);
            }
        }
        //Cette fonction créer un fichier si il n'existe pas déjà
        public static void CreateFile(string path, string logPath)
        {
            try
            {
                if (!File.Exists(path))
                {
                    using (StreamWriter sw = File.CreateText(path))
                    {
                        sw.WriteLine("");
                    }
                }
            }
            catch (Exception ex)
            {
                Log(logPath, "Erreur [CreateFile] : " + ex.Message);
            }
        }

        //Cette fonction réinitialise un fichier
        public static void ResetFile(string path, string logPath)
        {
            try
            {
                // Vérifier si le fichier existe
                if (File.Exists(path))
                {
                    using (StreamWriter sw = new StreamWriter(path, false))
                    {
                    }
                }
            }
            catch (Exception ex)
            {
                Log(logPath, "Erreur [ResetFile] : " + ex.Message);
            }
        }

        //Cette fonction  retourne une ligne spécifique prise dans un fichier  
        public static string GetSpecificLine(string filePath, int lineNumber , string logPath)
        {
            try
            {
                // Lire toutes les lignes du fichier
                string[] lines = File.ReadAllLines(filePath);

                // Vérifier si le numéro de ligne est valide
                if (lineNumber > 0 && lineNumber <= lines.Length)
                {
                    // Retourner la ligne spécifique
                    return lines[lineNumber - 1]; // Soustraire 1 car les indices commencent à 0
                }
                else
                {
                    Log(logPath, "ligne invalide.");
                }
            }
            catch (Exception ex)
            {
                Log(logPath, "Erreur [GetSpecificLine] : " + ex.Message);
            }

            return null;
        }
        //Cette fonction retourne l'heure actuelle
        public static string GetCurrentTime()
        {
            return DateTime.Now.ToString("HH:mm:ss");
        }
        //Cette fonction permet de télécharger un fichier zip sur le web, notamment sur github, et de l'extraire directement dans un dossier spécifique
        public static void DownloadZIP(string destinationFolderPath, string exactNameOfTheZipFile, string URL, string extractFolderPath)
        {
            // Check if the directory exists, if not, create it
            string directoryPath = Path.GetDirectoryName(Path.Combine(destinationFolderPath, $"{exactNameOfTheZipFile}.zip"));
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            try
            {
                DownloadAndExtractZipAsync(URL, Path.Combine(destinationFolderPath, $"{exactNameOfTheZipFile}.zip"), extractFolderPath).Wait();
            }
            catch (Exception ex)
            {
                // Handle exceptions here
                Utility.Log(Variables.logFilePath, "Error downloading file: " + ex.Message);
            }
        }
        public static async Task DownloadAndExtractZipAsync(string URL, string destinationFolderPath, string extractFolderPath)
        {
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync(URL);
                response.EnsureSuccessStatusCode();

                using (FileStream fileStream = new FileStream(destinationFolderPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    // Copy the content from the response message to the file stream
                    await response.Content.CopyToAsync(fileStream);
                }
            }

            // Ensure the extract path exists
            Directory.CreateDirectory(extractFolderPath);
            ZipFile.ExtractToDirectory(destinationFolderPath, extractFolderPath, true);
        }

        //Détruit un dossier et tout les fichiers contenus à l'intérieur
        public static void DeleteFolder(string path)
        {
            // Vérifier si le dossier existe
            if (Directory.Exists(path))
            {
                // Supprimer le dossier et tous ses fichiers récursivement
                Directory.Delete(path, true);
            }
        }
        // Créer un fichier de configuration lisible par ReadConfigFile()
        public static void SetConfigFile(string configFilePath)
        {
            // Définition des valeurs par défaut
            Dictionary<string, string> defaultConfig = new Dictionary<string, string>
            {
                {"version", "v0.1.2"},
                {"povKey", "f1"},
                {"smoothSpeedPosition", "20"},
                {"smoothSpeedRotation", "10"},
                {"updateFrequency", "1,0"},
                {"displayMessageInChat", "true"}
            };

            Dictionary<string, string> currentConfig = new Dictionary<string, string>();

            // Si le fichier existe, lire la configuration actuelle
            if (File.Exists(configFilePath))
            {
                string[] lignes = File.ReadAllLines(configFilePath);

                foreach (string ligne in lignes)
                {
                    string[] keyValue = ligne.Split('=');
                    if (keyValue.Length == 2)
                    {
                        string key = keyValue[0].Trim();
                        string value = keyValue[1].Trim();
                        currentConfig[key] = value;
                    }
                }
            }

            // Fusionner la configuration actuelle avec les valeurs par défaut
            foreach (KeyValuePair<string, string> paire in defaultConfig)
            {
                if (!currentConfig.ContainsKey(paire.Key))
                {
                    currentConfig[paire.Key] = paire.Value;
                }
            }

            // Sauvegarder la configuration fusionnée
            using (StreamWriter sw = File.CreateText(configFilePath))
            {
                foreach (KeyValuePair<string, string> paire in currentConfig)
                {
                    sw.WriteLine(paire.Key + "=" + paire.Value);
                }
            }
        }

        // Lit un fichier de config créer par SetConfigFile
        public static void ReadConfigFile(string configFilePath)
        {
            string[] lines = System.IO.File.ReadAllLines(configFilePath);
            Dictionary<string, string> config = new Dictionary<string, string>();
            CultureInfo cultureInfo = new CultureInfo("fr-FR");
            bool resultBool;
            int resultInt;
            float resultFloat;
            bool parseSuccess;

            foreach (string line in lines)
            {
                // Ignore les commentaires sur une ligne
                if (!line.Trim().StartsWith("//"))
                {
                    string[] parts = line.Split('=');
                    if (parts.Length == 2)
                    {
                        string key = parts[0].Trim();
                        string value = parts[1].Trim();
                        config[key] = value;
                    }
                }
            }
            Variables.povKey = config["povKey"];

            parseSuccess = int.TryParse(config["smoothSpeedPosition"], out resultInt);
            Variables.smoothSpeedPosition = parseSuccess ? resultInt : 20;

            parseSuccess = int.TryParse(config["smoothSpeedRotation"], out resultInt);
            Variables.smoothSpeedRotation = parseSuccess ? resultInt : 10;

            parseSuccess = float.TryParse(config["updateFrequency"], out resultFloat);
            Variables.updateFrequency = parseSuccess ? resultInt : 1;

            if (Variables.updateFrequency > 1)
            {
                Variables.updateFrequency = 1;
            }

            parseSuccess = bool.TryParse(config["displayMessageInChat"], out resultBool);
            Variables.displayMessageInChat = parseSuccess ? resultBool : false;
        }
    }

     //Cette class regroupe un ensemble de fonction relative aux données de la partie
     public class GameData
     {
        //Permet de trouver un joueur sur le serveur a partir de son nom ou de son #numéro
        public static string commandPlayerFinder(string identifier)
        {
            if (identifier.Contains("#"))
            {
                if (int.TryParse(identifier.Replace("#", ""), out int playerNumber))
                {
                    return GetPlayerSteamId(playerNumber);
                }
                else
                {
                    Utility.SendServerMessage("Invalid number");
                    return null;
                }
            }
            else
            {
                return GetPlayerSteamId(identifier);
            }
        }

        //Permet de trouver le steamId d'un joueur avec son numéro
        public static string GetPlayerSteamId(int playerNumber)
        {
            for (int u = 0; u <= Variables.playersList.Count; u++)
            {
                try
                {
                    if (Variables.playersList[u].value.playerNumber == playerNumber)
                    {
                        return Variables.playersList[u].value.steamProfile.m_SteamID.ToString();
                    }
                }
                catch (System.Exception ex)
                {
                    Utility.Log(Variables.logFilePath, "Error[GetPlayerSteamId] : " + ex.Message);
                }
            }
            return null;
        }

        //Permet de trouver le steamId d'un joueur avec son nom (surcharge de la méthode GetPlayerSteamId)
        public static string GetPlayerSteamId(string username)
        {
            for (int u = 0; u <= Variables.playersList.Count; u++)
            {
                try
                {
                    if (Variables.playersList[u].value.username.Contains(username, StringComparison.OrdinalIgnoreCase))
                    {
                        return Variables.playersList[u].value.steamProfile.m_SteamID.ToString();
                    }
                }
                catch (System.Exception ex)
                {
                    Utility.Log(Variables.logFilePath, "Error[GetPlayerSteamId] : " + ex.Message);
                    return null;
                }
            }
            return null;
        }

        //Permet d'obtenir le PlayerManager d'un joueur a partir de son steamId
        public static PlayerManager GetPlayer(string steamId)
        {
            for (int u = 0; u <= Variables.playersList.Count; u++)
            {
                try
                {
                    if (Variables.playersList[u].value.steamProfile.m_SteamID.ToString() == steamId)
                    {
                        return Variables.playersList[u].value;
                    }
                }
                catch (System.Exception ex)
                {
                    Utility.Log(Variables.logFilePath, "Error[GetPlayer] : " + ex.Message);
                }
            }
            return null;
        }
        //Permet d'obtenir le nombre de joueur actuellement en vie
        public static int GetPlayerAliveCount()
        {
            int playerAlive = 0;

            // Vérifier si la liste des joueurs est initialisée et non nulle
            if (Variables.playersList != null)
            {
                // Utiliser la propriété Count pour éviter de dépasser la taille de la liste
                for (int u = 0; u < Variables.playersList.Count; u++)
                {
                    try
                    {
                        // Vérifier si l'objet à l'index u est non nul
                        if (Variables.playersList[u] != null && !Variables.playersList[u].value.dead)
                        {
                            playerAlive++;
                        }
                    }
                    catch (System.Exception ex)
                    {
                        // Ajouter une log pour identifier l'erreur
                        Utility.Log(Variables.logFilePath, "Error[GetPlayer] : " + ex.Message);
                    }
                }
            }
            else
            {
                // Ajouter une log si la liste des joueurs est nulle
                Utility.Log(Variables.logFilePath, "Error[GetPlayer] : Variables.playersList is null");
            }

            return playerAlive;
        }

        //Cette fonction récupère la valeur actuelle du Timer in Game
        public static int GetCurrentGameTimer()
        {
            return UnityEngine.Object.FindObjectOfType<TimerUI>().field_Private_TimeSpan_0.Seconds;
        }
        //Cette fonction retourne le GameState de la partie en cours
        public static string GetGameState()
        {
            return UnityEngine.Object.FindObjectOfType<GameManager>().gameMode.modeState.ToString();
        }

        //Cette fonction retourne le LobbyManager
        public static LobbyManager GetLobbyManager()
        {
            return LobbyManager.Instance;
        }

        public static SteamManager GetSteamManager()
        {
            return SteamManager.Instance;
        }

        //Cette fonction retourne l'id de la map en cours
        public static int GetMapId()
        {
            return GetLobbyManager().map.id;
        }

        //Cette fonction retourne l'id du mode en cours
        public static int GetModeId()
        {
            return GetLobbyManager().gameMode.id;
        }

        //Cette fonction retourne le nom de la map en cours
        public static string GetMapName()
        {
            return GetLobbyManager().map.mapName;
        }

        //Cette fonction retourne le nom du mode en cours
        public static string GetModeName()
        {
            return UnityEngine.Object.FindObjectOfType<LobbyManager>().gameMode.modeName;
        }

        //Cette fonction retourne le GameManager
        public static GameManager GetGameManager()
        {
            try
            {
                return GameObject.Find("/GameManager (1)").GetComponent<GameManager>();
            }
            catch
            {
                return GameObject.Find("/GameManager").GetComponent<GameManager>();
            }
        }
    }

    public class PlayersData
    {
        //Cette fonction vérifie si un joueur se trouve au niveau du sol (par défault sur un sol plat, ground = 2f)
        public static bool IsGrounded(Vector3 playerPos, float ground, GameObject player)
        {
            RaycastHit hit;
            Vector3 startPosition = playerPos;
            float distanceToGround;

            // Créez un LayerMask qui ignore le layer 'Player'
            int layerMask = 1 << LayerMask.NameToLayer("Player");
            layerMask = ~layerMask; // Inverse le mask pour ignorer le layer 'Player'

            if (Physics.Raycast(startPosition, Vector3.down, out hit, Mathf.Infinity, layerMask))
            {
                distanceToGround = hit.distance;

                if (hit.distance >= ground)
                    return false;
                else
                    return true;
            }
            return false;
        }
    }

    public class ClientData
    {
        //Cette fonction retourne le steam Id du client sous forme de ulong
        public static ulong GetClientId()
        {
            return (ulong)SteamManager.Instance.field_Private_CSteamID_0;
        }

        //Cette fonction retourne un booleen qui détermine si le client est Host ou non
        public static bool IsClientHost()
        {
            return SteamManager.Instance.IsLobbyOwner() && !LobbyManager.Instance.Method_Public_Boolean_0();
        }

        //Cette fonction retourne le GameObject du client
        public static GameObject GetClientObject()
        {
            return GameObject.Find("/Player");
        }
        //Cette fonction retourne le Rigidbody du client
        public static Rigidbody GetClientBody()
        {
            return GetClientObject() == null ? null : GetClientObject().GetComponent<Rigidbody>();
        }
        //Cette fonction retourne le PlayerManager du client
        public static PlayerManager GetClientManager()
        {
            return GetClientObject() == null ? null : GetClientObject().GetComponent<PlayerManager>();
        }

        //Cette fonction retourne la class Movement qui gère les mouvements du client
        public static PlayerMovement GetClientMovement()
        {
            return GetClientObject() == null ? null : GetClientObject().GetComponent<PlayerMovement>();
        }

        //Cette fonction retourne l'inventaire du client
        public static PlayerInventory GetClientInventory()
        {
            return GetClientObject() == null ? null : PlayerInventory.Instance;
        }

        //Cette fonction retourne le status du client
        public static PlayerStatus GetClientStatus()
        {
            return GetClientObject() == null ? null : PlayerStatus.Instance;
        }

        //Cette fonction retourne la Camera du client
        public static Camera GetClientCamera()
        {
            return GetClientBody() == null ? null : UnityEngine.Object.FindObjectOfType<Camera>();
        }

        //Cette fonction retourne l'username du client
        public static string GetClientUsername()
        {
            return GetClientManager() == null ? null : GetClientManager().username.ToString();
        }
        
        //Cette fonction retourne la rotation du client
        public static Quaternion? GetClientRotation()
        {
            return GetClientObject() == null ? null : GetClientCamera().transform.rotation;
        }

        //Cette fonction retourne la position du client
        public static Vector3? GetClientPosition()
        {
            return GetClientObject() == null ? null : GetClientBody().transform.position;
        }
        //Cette fonction retourne la vitesse du client
        public static Vector3? GetClientSpeed()
        {
            return GetClientObject() == null ? null : Variables.clientBody.velocity;
        }
        //Cette fonction retourne si le client a un item ou non équipé
        public static bool ClientHasItemCheck()
        {
            return PlayerInventory.Instance.currentItem == null ? false : true;
        }

        //Cette fonction désactive les mouvements du client
        public static void DisableClientMovement()
        {
            if (Variables.clientBody != null && Variables.clientBody.position != Vector3.zero)
            {
                Variables.clientBody.isKinematic = true;
                Variables.clientBody.useGravity = false;
            }
        }

        //Cette fonction active les mouvements du client
        public static void EnableClientMovement()
        {
            if (Variables.clientBody != null && Variables.clientBody.position != Vector3.zero)
            {
                Variables.clientBody.isKinematic = false;
                Variables.clientBody.useGravity = true;
            }
        }
    }

}
