//Using (ici on importe des bibliothèques utiles)
global using BepInEx;
global using BepInEx.IL2CPP;
global using HarmonyLib;
global using UnityEngine;
global using System;
global using System.IO;
global using UnhollowerRuntimeLib;
global using System.Linq;
global using System.Runtime.InteropServices;
global using System.Collections.Generic;
global using System.Globalization;
global using System.IO.Compression;
global using System.Net.Http;
global using System.Threading.Tasks;

namespace SpectatePOV
{
    [BepInPlugin("PlaceHereGUID", "SpectatePOV", "1.2.0")]
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
            }

            //Ceci mets a jour les données relative au Server
            void BasicUpdateServer()
            {
                Variables.chatBoxInstance = ChatBox.Instance;
                Variables.gameManager = GameData.GetGameManager();
                Variables.lobbyManager = GameData.GetLobbyManager();
            }
        }

        //Une class d'exemple (ici envoie le message "Coucou!" à chaque frame du jeu)

        public class SpecatePOVManager : MonoBehaviour
        {
            GameObject lastSpectatedPlayer, spectatedPlayerObject, moveCamera;
            Vector3 spectatedPlayerPos = Vector3.zero;
            Quaternion spectatedPlayerRot = Quaternion.identity;
            CamBob camBob = null;

            bool displayPOVMessage = true, displaySwitchedPlayerMessage = true, spectatedPlayerNeedUpdate = true;
            float elapsed = 0f;

            void Update()
            {
                if (!Variables.povTrigger)
                {
                    if (displayPOVMessage && camBob != null)
                    {
                        Utility.ForceMessage("|<color=orange><b>POV OFF</b></color>|");
                        RestorePlayerModelSize(lastSpectatedPlayer);
                        displaySwitchedPlayerMessage = true;
                        displayPOVMessage = false;
                        camBob.field_Private_EnumNPublicSealedvaPlSpPlFr5vUnique_0 = CamBob.EnumNPublicSealedvaPlSpPlFr5vUnique.Freecam;
                    }
                    return;
                }

                if (Variables.clientBody != null && Variables.povTrigger)
                {
                    Variables.povTrigger = false;
                    Utility.ForceMessage("|<color=orange><b>POV OFF (client is alive this round)</b></color>|");
                    return;
                }

                elapsed += Time.deltaTime;

                if (elapsed < (1 - Variables.updateFrequency)) return;
                else elapsed = 0f;

                if (camBob == null)
                {
                    FindAndAssignCamera();
                    if (camBob == null) return;
                }

                if (!displayPOVMessage)
                {
                    Utility.ForceMessage("|<color=orange><b>POV ON</b></color>|");
                    camBob.field_Private_EnumNPublicSealedvaPlSpPlFr5vUnique_0 = CamBob.EnumNPublicSealedvaPlSpPlFr5vUnique.Player;
                    displayPOVMessage = true;
                }

                if (moveCamera != null)
                    HandleCameraInput();

                lastSpectatedPlayer = spectatedPlayerObject;
                DisablePlayerModel(lastSpectatedPlayer);
            }

            void FindAndAssignCamera()
            {
                moveCamera = GameObject.Find("MoveCamera(Clone)");
                if (moveCamera != null)
                    camBob = moveCamera.GetComponent<CamBob>();
            }

            void HandleCameraInput()
            {
                if (IsRightMouseButtonClicked() || IsLeftMouseButtonClicked())
                {
                    int direction = IsRightMouseButtonClicked() ? 1 : -1;
                    if (camBob != null)
                    {
                        camBob.Method_Private_Void_Int32_0(direction);
                        camBob.field_Private_EnumNPublicSealedvaPlSpPlFr5vUnique_0 = CamBob.EnumNPublicSealedvaPlSpPlFr5vUnique.Player;
                        displaySwitchedPlayerMessage = true;
                    }
                }

                if (spectatedPlayerNeedUpdate)
                    spectatedPlayerObject = camBob.field_Private_Transform_1.gameObject;

                if (spectatedPlayerObject != null)
                {
                    spectatedPlayerPos = spectatedPlayerObject.transform.position;
                    spectatedPlayerRot = spectatedPlayerObject.GetComponent<PlayerManager>().GetRotation();

                    SmoothCameraMovement(camBob.transform);

                    if (lastSpectatedPlayer != null)
                    {
                        RestorePlayerModelSize(lastSpectatedPlayer);
                    }

                    if (displaySwitchedPlayerMessage)
                    {
                        DisplaySwitchPlayerMessage(spectatedPlayerObject.GetComponent<PlayerManager>());
                        displaySwitchedPlayerMessage = false;
                    }

                    lastSpectatedPlayer = spectatedPlayerObject;
                }
            }

            void SmoothCameraMovement(Transform camTransform)
            {
                camTransform.position = Vector3.Lerp(camTransform.position, spectatedPlayerPos + new Vector3(0, 1.5f, 0), Variables.smoothSpeedPosition * Time.deltaTime);
                camTransform.rotation = Quaternion.Slerp(camTransform.rotation, spectatedPlayerRot, Variables.smoothSpeedRotation * Time.deltaTime);
            }

            void DisplaySwitchPlayerMessage(PlayerManager playerManager)
            {
                Utility.ForceMessage($"<color=orange>Now spectating <b>#{playerManager.playerNumber} {playerManager.username}</b></color>");
            }

            void DisablePlayerModel(GameObject player)
            {
                if (player != null)
                {
                    Transform playerModelTransform = player.transform.Find("PlayerModel");
                    if (playerModelTransform != null)
                    {
                        playerModelTransform.localScale = Vector3.one * 0.0001f;
                    }
                }
            }

            void RestorePlayerModelSize(GameObject player)
            {
                if (player != null)
                {
                    Transform playerModelTransform = player.transform.Find("PlayerModel");
                    if (playerModelTransform != null)
                    {
                        playerModelTransform.localScale = Vector3.one * 0.3411f;
                    }
                }
            }

            bool IsRightMouseButtonClicked()
            {
                return Input.GetMouseButtonDown(1);
            }

            bool IsLeftMouseButtonClicked()
            {
                return Input.GetMouseButtonDown(0);
            }
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