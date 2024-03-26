namespace SpectatePOV
{
    //Ici on stock les variables "globale" pour la lisibilité du code dans Plugin.cs 
    internal class Variables
    {
        //folder
        public static string assemblyFolderPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        public static string defaultFolderPath = assemblyFolderPath + "\\";
        public static string mainFolderPath = defaultFolderPath + @"SpectatePOV\";

        //file
        public static string logFilePath = mainFolderPath + "log.txt";
        public static string configFilePath = mainFolderPath + "config.txt";


        //Manager
        public static GameManager gameManager;
        public static PlayerMovement clientMovement;
        public static PlayerInventory clientInventory;
        public static PlayerStatus clientStatus;
        public static LobbyManager lobbyManager;
        public static SteamManager steamManager;

        //TextBox
        public static ChatBox chatBoxInstance;

        //Dictionary
        public static Il2CppSystem.Collections.Generic.Dictionary<ulong, PlayerManager> activePlayers;

        //List
        public static List<Il2CppSystem.Collections.Generic.Dictionary<ulong, MonoBehaviourPublicCSstReshTrheObplBojuUnique>.Entry> playersList;

        //Rigidbody
        public static Rigidbody clientBody;

        //GameObject
        public static GameObject clientObject;

        //int
        public static int mapId, modeId, smoothSpeedPosition, smoothSpeedRotation;

        //float
        public static float updateFrequency;

        //ulong
        public static ulong clientId, clientIdSafe;

        //string
        public static string povKey, gameState, lastGameState;

        //bool
        public static bool displayMessageInChat, povTrigger;



    }
}
