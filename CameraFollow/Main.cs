using System;
using System.IO;
using MelonLoader;
using NET_SDK;
using NET_SDK.Harmony;
using UnityEngine;

namespace FollowCamera
{
    public static class BuildInfo
    {
        public const string Name = "FollowCamera"; // Name of the Mod.  (MUST BE SET)
        public const string Author = "Alternity"; // Author of the Mod.  (Set as null if none)
        public const string Company = null; // Company that made the Mod.  (Set as null if none)
        public const string Version = "1.0.0"; // Version of the Mod.  (MUST BE SET)
        public const string DownloadLink = null; // Download Link for the Mod.  (Set as null if none)
    }

    public class FollowCamera : MelonMod
    {
        public static Patch SpectatorCam_Update;
        public static Patch OptionsMenu_ShowPage;

        //The current way of tracking menu state.
        //TODO: Hook to the SetMenuState function without breaking the game
        public static MenuState.State menuState;
        public static MenuState.State oldMenuState;

        public static bool camOK = false;

        public static SpectatorCam spectatorCam;

        public static OptionsMenuSlider positionSmoothingSlider = null;
        public static OptionsMenuSlider rotationSmoothingSlider = null;
        public static OptionsMenuSlider camHeightSlider = null;
        public static OptionsMenuSlider camDistanceSlider = null;
        public static OptionsMenuSlider camRotationSlider = null;
        public static OptionsMenuSlider camOffsetSlider = null;

        public static Config config = new Config();
        public static string path = Application.dataPath + "/../Mods/Config/FollowCamera.json";

        public static void SaveConfig()
        {
            Directory.CreateDirectory(Application.dataPath + "/../Mods/Config");
            string contents = Encoder.GetConfig(config);
            File.WriteAllText(path, contents);
        }

        public static void LoadConfig()
        {
            if (!File.Exists(path))
            {
                SaveConfig();
            }
            Encoder.SetConfig(config, File.ReadAllText(path));
        }

        public static void UpdateSlider(OptionsMenuSlider slider, string text)
        {
            if (slider == null)
            {
                return;
            }
            else
            {
                slider.label.text = text;
                SaveConfig();
            }
        }

        public static Vector3 ClampMagnitude(Vector3 input, float minMagnitude, float maxMagnitude)
        {
            float inMagnitude = input.magnitude;
            if (inMagnitude < minMagnitude)
            {
                Vector3 inNormalized = input / inMagnitude; //equivalent to in.normalized, but slightly faster in this case
                return inNormalized * minMagnitude;
            }
            else if (inMagnitude > maxMagnitude)
            {
                Vector3 inNormalized = input / inMagnitude; //equivalent to in.normalized, but slightly faster in this case
                return inNormalized * maxMagnitude;
            }

            // No need to clamp at all
            return input;
        }

        void CheckCamera()
        {
            //If spectator cam is on
            bool camOn = PlayerPreferences.I.SpectatorCam.Get();
            if (camOn)
            {
                //If spectator cam is set to static third person
                float camMode = PlayerPreferences.I.SpectatorCamMode.Get();
                if (camMode == 1)
                {
                    //If camOK is already true at this point we don't need to do anything
                    if (!camOK)
                    {
                        //If it's not, get reference for SpectatorCam class and set camOK to true
                        spectatorCam = UnityEngine.Object.FindObjectOfType<SpectatorCam>();
                        camOK = true;
                    }
                }
                else { camOK = false; }
            }
            else { camOK = false; }
        }

        public override void OnApplicationStart()
        {
            LoadConfig();

            Instance instance = Manager.CreateInstance("CameraScript");

            SpectatorCam_Update = instance.Patch(SDK.GetClass("SpectatorCam").GetMethod("Update"), typeof(FollowCamera).GetMethod("SpectatorCamUpdate"));
            OptionsMenu_ShowPage = instance.Patch(SDK.GetClass("OptionsMenu").GetMethod("ShowPage"), typeof(FollowCamera).GetMethod("ShowPage"));
        }

        public static unsafe void ShowPage(IntPtr @this, OptionsMenu.Page page)
        {
            OptionsMenu_ShowPage.InvokeOriginal(@this, new IntPtr[]
            {
                new IntPtr((void*)(&page))
            });

            if (page == OptionsMenu.Page.SpectatorCam)
            {
                OptionsMenu optionsMenu = new OptionsMenu(@this);

                positionSmoothingSlider = optionsMenu.AddSlider
                    (
                    0, 
                    "Position Smoothing", 
                    "P", 
                    new Action<float>((float n) => {
                        config.positionSmoothing = Mathf.Round((config.positionSmoothing + (n * 0.001f)) * 1000.0f) / 1000.0f; 
                        UpdateSlider(positionSmoothingSlider, "Position Smoothing : " + config.positionSmoothing.ToString()); 
                    }), 
                    null, 
                    null, 
                    "Changes how smooth position will be"
                    );
                positionSmoothingSlider.label.text = "Position Smoothing : " + config.positionSmoothing.ToString();

                rotationSmoothingSlider = optionsMenu.AddSlider
                    (
                    0,
                    "Rotation Smoothing",
                    "P",
                    new Action<float>((float n) => {
                        config.rotationSmoothing = Mathf.Round((config.rotationSmoothing + (n * 0.001f)) * 1000.0f) / 1000.0f;
                        UpdateSlider(rotationSmoothingSlider, "Rotation Smoothing : " + config.rotationSmoothing.ToString());
                    }),
                    null,
                    null,
                    "Changes how smooth rotation will be"
                    );
                rotationSmoothingSlider.label.text = "Rotation Smoothing : " + config.rotationSmoothing.ToString();

                camOffsetSlider = optionsMenu.AddSlider
                    (
                    0,
                    "Horizontal Offset",
                    "P",
                    new Action<float>((float n) => {
                        config.camOffset = Mathf.Round((config.camOffset + (n * 0.1f)) * 10.0f) / 10.0f;
                        UpdateSlider(camOffsetSlider, "Horizontal Offset : " + config.camOffset.ToString());
                    }),
                    null,
                    null,
                    "Changes horizontal position"
                    );
                camOffsetSlider.label.text = "Horizontal Offset : " + config.camOffset.ToString();

                camHeightSlider = optionsMenu.AddSlider
                    (
                    0,
                    "Vertical Offset",
                    "P",
                    new Action<float>((float n) => {
                        config.camHeight = Mathf.Round((config.camHeight + (n * 0.1f)) * 10.0f) / 10.0f;
                        UpdateSlider(camHeightSlider, "Vertical Offset : " + config.camHeight.ToString());
                    }),
                    null,
                    null,
                    "Changes vertical position"
                    );
                camHeightSlider.label.text = "Vertical Offset : " + config.camHeight.ToString();

                camDistanceSlider = optionsMenu.AddSlider
                    (
                    0,
                    "Distance",
                    "P",
                    new Action<float>((float n) => {
                        config.camDistance = Mathf.Round((config.camDistance + (n * 0.1f)) * 10.0f) / 10.0f;
                        UpdateSlider(camDistanceSlider, "Distance : " + config.camDistance.ToString());
                    }),
                    null,
                    null,
                    "Changes the distance"
                    );
                camDistanceSlider.label.text = "Distance : " + config.camDistance.ToString();

                camRotationSlider = optionsMenu.AddSlider
                    (
                    0,
                    "Rotation",
                    "P",
                    new Action<float>((float n) => {
                        config.camRotation = Mathf.Round((config.camRotation + (n * 0.1f)) * 10.0f) / 10.0f;
                        UpdateSlider(camRotationSlider, "Rotation : " + config.camRotation.ToString());
                    }),
                    null,
                    null,
                    "Changes the rotation"
                    );
                camRotationSlider.label.text = "Rotation : " + config.camRotation.ToString();

                optionsMenu.scrollable.AddRow(optionsMenu.AddHeader(0, "Follow Camera <size=5>Must be set to 3rd person static</size>"));

                //UnhollowerBaseLib.IL2CPP.<GameObject> row1 = new UnhollowerBaseLib.Il2CppArrayBase<GameObject>();

                //System.Collections.Generic.List<GameObject> row1 = new System.Collections.Generic.List<GameObject>();

                optionsMenu.scrollable.AddRow(positionSmoothingSlider.gameObject);
                optionsMenu.scrollable.AddRow(rotationSmoothingSlider.gameObject);
                optionsMenu.scrollable.AddRow(camOffsetSlider.gameObject);
                optionsMenu.scrollable.AddRow(camHeightSlider.gameObject);
                optionsMenu.scrollable.AddRow(camDistanceSlider.gameObject);
                optionsMenu.scrollable.AddRow(camRotationSlider.gameObject);
            }


        }

        public static unsafe void SpectatorCamUpdate(IntPtr @this)
        {
            if (camOK)
            {
                Transform head = AvatarSelector.I.customHead.transform;

                Vector3 hmdPos = head.position;
                Vector3 hmdRot = head.rotation.eulerAngles;

                Vector3 hmdOffsetPos = new Vector3(hmdPos.x + (head.right.x * config.camOffset), hmdPos.y, hmdPos.z + (head.right.z * config.camOffset));

                Vector3 camPos = spectatorCam.cam.gameObject.transform.position;
                Quaternion camRot = spectatorCam.cam.gameObject.transform.rotation;

                Vector3 destinationPos = new Vector3(hmdOffsetPos.x - (head.forward.x * config.camDistance), config.camHeight, hmdOffsetPos.z - (head.forward.z * config.camDistance));
                Quaternion destinationRot = Quaternion.Euler(config.camRotation, hmdRot.y, 0);

                spectatorCam.cam.gameObject.transform.position = Vector3.Slerp(camPos, destinationPos, config.positionSmoothing);
                spectatorCam.cam.gameObject.transform.rotation = Quaternion.Slerp(camRot, destinationRot, config.rotationSmoothing);
            }

            else
            {
                SpectatorCam_Update.InvokeOriginal(@this);
            }
        }

        public override void OnUpdate()
        {
            //Tracking menu state
            menuState = MenuState.GetState();

            //If menu changes
            if (menuState != oldMenuState)
            {
                MelonModLogger.Log("Menu: " + menuState.ToString());

                if (menuState == MenuState.State.MainPage)
                {
                    CheckCamera();
                }

                oldMenuState = menuState;
            }
        }

        /*
        public override void OnLevelWasLoaded(int level)
        {
            MelonModLogger.Log("OnLevelWasLoaded: " + level.ToString());
        }

        public override void OnLevelWasInitialized(int level)
        {
            MelonModLogger.Log("OnLevelWasInitialized: " + level.ToString());
        }

        public override void OnFixedUpdate()
        {
            MelonModLogger.Log("OnFixedUpdate");
        }

        public override void OnLateUpdate()
        {
            MelonModLogger.Log("OnLateUpdate");
        }

        public override void OnGUI()
        {
            MelonModLogger.Log("OnGUI");
        }

        public override void OnApplicationQuit()
        {
            MelonModLogger.Log("OnApplicationQuit");
        }

        public override void OnModSettingsApplied()
        {
            MelonModLogger.Log("OnModSettingsApplied");
        }

        public override void VRChat_OnUiManagerInit() // Only works in VRChat
        {
            MelonModLogger.Log("VRChat_OnUiManagerInit");
        }
        */
    }
}
