//Using (ici on importe des bibliothèques utiles)
global using BepInEx;
global using BepInEx.IL2CPP;
global using HarmonyLib;
global using UnityEngine;
global using System;
global using System.IO;
global using UnhollowerRuntimeLib;
global using System.Linq;
global using UnityEngine.UI;
using System.Runtime.InteropServices;

namespace SpectatePOV
{
    [BepInPlugin("PlaceHereGUID", "SpectatePOV", "1.0.0")]
    public class Plugin : BasePlugin
    {
        public override void Load()
        {
            ClassInjector.RegisterTypeInIl2Cpp<Basics>();

            //Ajouter ici toute vos class MonoBehaviour pour quelle soit active dans le jeu
            //Format: ClassInjector.RegisterTypeInIl2Cpp<NomDeLaClass>(); 
            ClassInjector.RegisterTypeInIl2Cpp<SpecatePOVManager>();

            Harmony.CreateAndPatchAll(typeof(Plugin));

            //Ici on créer un fichier log.txt situé dans le dossier GibsonTemplateMod
            Utility.CreateFolder(Variables.mainFolderPath, Variables.logFilePath);
            Utility.CreateFile(Variables.logFilePath, Variables.logFilePath);
            Utility.ResetFile(Variables.logFilePath, Variables.logFilePath);
            Utility.SetConfigFile(Variables.configFilePath);
        }

        //Cette class permet de récupérer des variables de base ne pas toucher sauf pour rajouter d'autres variables a Update
        public class Basics : MonoBehaviour
        {
            float elapsedServerUpdate, elapsedClientUpdate;

            void Update()
            {
                float elapsedTime = Time.deltaTime;
                elapsedServerUpdate += elapsedTime;
                elapsedClientUpdate += elapsedTime;

                if (elapsedServerUpdate > 1f)
                {
                    Variables.camera = FindObjectOfType<Camera>();
                    BasicUpdateServer();
                    Utility.ReadConfigFile(Variables.configFilePath);
                    elapsedServerUpdate = 0f;
                }
                    
                if (elapsedClientUpdate > 1f)
                {
                    BasicUpdateClient();
                    elapsedClientUpdate = 0f;
                }

                if (Input.GetKeyDown(Variables.povKey))
                {
                    Variables.povTrigger = !Variables.povTrigger;
                }

            }

            //Ceci mets a jour les données relative au Client(fonctionne uniquement si le client a un Rigidbody (en vie))
            void BasicUpdateClient()
            {
                Variables.clientBody = ClientData.GetClientBody();
                if (Variables.clientBody == null) return;

                Variables.clientObject = ClientData.GetClientObject();
                Variables.clientMovement = ClientData.GetClientMovement();
                Variables.clientInventory = ClientData.GetClientInventory();
                Variables.clientStatus = PlayerStatus.Instance;
            }

            //Ceci mets a jour les données relative au Server
            void BasicUpdateServer()
            {
                Variables.chatBoxInstance = ChatBox.Instance;
                Variables.gameManager = GameData.GetGameManager();
                Variables.lobbyManager = GameData.GetLobbyManager();
                Variables.steamManager = GameData.GetSteamManager();
                Variables.mapId = GameData.GetMapId();
                Variables.modeId = GameData.GetModeId();
                Variables.gameState = GameData.GetGameState();
                Variables.activePlayers = Variables.gameManager.activePlayers;
                Variables.playersList = Variables.gameManager.activePlayers.entries.ToList();
                if (Variables.gameState != Variables.lastGameState)
                    Variables.lastGameState = Variables.gameState;
            }
        }

        //Une class d'exemple (ici envoie le message "Coucou!" à chaque frame du jeu)

        public class SpecatePOVManager : MonoBehaviour
        {
            GameObject lastSpectatedPlayer, targetPlayerObject;
            Vector3 targetPlayerPos = Vector3.zero;
            Vector3 targetPlayerForward = Vector3.zero;
            Quaternion targetPlayerRot = Quaternion.identity;

            bool messageDisplayed, switchPlayerMessageDisplayed = true;

            CamBob currentCamBob = null;

            void Update()
            {
                if (!Variables.povTrigger)
                {
                    if (messageDisplayed)
                    {
                        Utility.ForceMessage("|<color=orange><b>POV OFF</b></color>|");
                        switchPlayerMessageDisplayed = true;
                        messageDisplayed = false;
                    }
                    currentCamBob.field_Private_EnumNPublicSealedvaPlSpPlFr5vUnique_0 = CamBob.EnumNPublicSealedvaPlSpPlFr5vUnique.Spectate;
                    return;
                }

                if (!messageDisplayed)
                {
                    Utility.ForceMessage("|<color=orange><b>POV ON</b></color>|");
                    messageDisplayed = true;
                }

                GameObject cam = FindGameObject("Camera");
                GameObject moveCam = FindGameObject("MoveCamera(Clone)");

                if (cam != null && moveCam == null)
                {
                    HandleCameraInput(cam.GetComponent<CamBob>());
                }
                else if (moveCam != null)
                {
                    HandleCameraInput(moveCam.GetComponent<CamBob>());
                }
                else
                {
                    return;
                }

                lastSpectatedPlayer = targetPlayerObject;
                DisablePlayerModel(lastSpectatedPlayer);
            }

            void HandleCameraInput(CamBob camBob)
            {
                currentCamBob = camBob;
                if (IsRightMouseButtonClicked())
                {
                    camBob.Method_Private_Void_Int32_0(1); 
                    switchPlayerMessageDisplayed = true;
                }
                else if (IsLeftMouseButtonClicked())
                {
                    camBob.Method_Private_Void_Int32_0(-1); 
                    switchPlayerMessageDisplayed = true;
                }

                targetPlayerObject = camBob.field_Private_Transform_1.gameObject;
                camBob.field_Private_EnumNPublicSealedvaPlSpPlFr5vUnique_0 = CamBob.EnumNPublicSealedvaPlSpPlFr5vUnique.Player;
                targetPlayerPos = targetPlayerObject.transform.position;
                targetPlayerRot = targetPlayerObject.GetComponent<PlayerManager>().GetRotation();
                targetPlayerForward = targetPlayerObject.transform.forward;

                // Lisser le mouvement de la caméra
                SmoothCameraMovement(camBob.transform);

                if (lastSpectatedPlayer != null)
                {
                    RestorePlayerModelSize(lastSpectatedPlayer);
                }

                if (switchPlayerMessageDisplayed)
                {
                    DisplaySwitchPlayerMessage(targetPlayerObject.GetComponent<PlayerManager>());
                    switchPlayerMessageDisplayed = false;
                }

                lastSpectatedPlayer = targetPlayerObject;
            }

            void SmoothCameraMovement(Transform camTransform)
            {
                camTransform.position = Vector3.Lerp(camTransform.position, targetPlayerPos + new Vector3(0, 1.5f, 0), Variables.smoothSpeedPosition * Time.deltaTime);
                camTransform.rotation = Quaternion.Slerp(camTransform.rotation, targetPlayerRot, Variables.smoothSpeedRotation * Time.deltaTime);
            }

            void DisplaySwitchPlayerMessage(PlayerManager playerManager)
            {
                Utility.ForceMessage($"<color=orange>Now spectating <b>#{playerManager.playerNumber} {playerManager.username}</b></color>");
            }

            void DisablePlayerModel(GameObject player)
            {
                Transform playerModelTransform = player.transform.Find("PlayerModel");
                if (playerModelTransform != null)
                {
                    playerModelTransform.localScale = Vector3.one * 0.0001f;
                }
            }

            void RestorePlayerModelSize(GameObject player)
            {
                Transform playerModelTransform = player.transform.Find("PlayerModel");
                if (playerModelTransform != null)
                {
                    playerModelTransform.localScale = Vector3.one * 0.3411f; 
                }
            }

            GameObject FindGameObject(string name)
            {
                return GameObject.Find(name);
            }

            bool IsRightMouseButtonClicked()
            {
                short state = GetAsyncKeyState(VK_RBUTTON);
                return (state & 0x8000) != 0 && (state & 0x0001) != 0;
            }

            bool IsLeftMouseButtonClicked()
            {
                short state = GetAsyncKeyState(VK_LBUTTON);
                return (state & 0x8000) != 0 && (state & 0x0001) != 0;
            }

            [DllImport("user32.dll")]
            private static extern short GetAsyncKeyState(int vKey);
            const int VK_RBUTTON = 0x02;
            const int VK_LBUTTON = 0x01;
        }

        [HarmonyPatch(typeof(SteamManager), nameof(SteamManager.Update))]
        [HarmonyPostfix]
        public static void OnSteamManagerUpdate(SteamManager __instance)
        {
            //Mets a jour le steamId du client des le lancement du jeu
            if (Variables.clientIdSafe == 0)
            {
                Variables.clientId = (ulong)__instance.field_Private_CSteamID_0;
                Variables.clientIdSafe = Variables.clientId;
            }
        }

        [HarmonyPatch(typeof(GameUI), "Awake")]
        [HarmonyPostfix]
        public static void UIAwakePatch(GameUI __instance)
        {
            GameObject menuObject = new GameObject();
            Basics basics = menuObject.AddComponent<Basics>();

            //Ici aussi ajouter toute vos class MonoBehaviour pour quelle soit active dans le jeu
            //Format: NomDeLaClass nomDeLaClass = menuObject.AddComponent<NomDeLaClass>();
            SpecatePOVManager exemple = menuObject.AddComponent<SpecatePOVManager>();

            menuObject.transform.SetParent(__instance.transform);
        }

        //Anticheat Bypass 
        [HarmonyPatch(typeof(EffectManager), "Method_Private_Void_GameObject_Boolean_Vector3_Quaternion_0")]
        [HarmonyPatch(typeof(LobbyManager), "Method_Private_Void_0")]
        [HarmonyPatch(typeof(MonoBehaviourPublicVesnUnique), "Method_Private_Void_0")]
        [HarmonyPatch(typeof(LobbySettings), "Method_Public_Void_PDM_2")]
        [HarmonyPatch(typeof(MonoBehaviourPublicTeplUnique), "Method_Private_Void_PDM_32")]
        [HarmonyPrefix]
        public static bool Prefix(System.Reflection.MethodBase __originalMethod)
        {
            return false;
        }
    }
}