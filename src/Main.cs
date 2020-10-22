using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using MelonLoader;
using Harmony;
using System.Collections;

namespace AudicaModding
{
    public class AudicaMod : MelonMod
    {
        public static class BuildInfo
        {
            public const string Name = "FollowCamera";
            public const string Author = "Alternity";
            public const string Company = null;
            public const string Version = "0.1.0";
            public const string DownloadLink = null;
        }

        //The current way of tracking menu state.
        public static MenuState.State menuState;
        public static MenuState.State oldMenuState;

        //Tracking FOV setting while in spectator cam menu
        public static float fov = 0f;
        public static float oldFov = 0f;

        public static SpectatorCam spectatorCam = null;

        public static bool camOK = false;
        public static bool spectatorCamSet = false;

        public static OptionsMenuSlider positionSmoothingSlider = null;
        public static OptionsMenuSlider rotationSmoothingSlider = null;
        public static OptionsMenuSlider camHeightSlider = null;
        public static OptionsMenuSlider camDistanceSlider = null;
        public static OptionsMenuSlider camRotationSlider = null;
        public static OptionsMenuSlider camOffsetSlider = null;

        public static bool activated = true;
        public static float positionSmoothing = 0.005f;
        public static float rotationSmoothing = 0.005f;
        public static float camHeight = 1.0f;
        public static float camDistance = 5.0f;
        public static float camRotation = 0.0f;
        public static float camOffset = 5.0f;

        public static void CreateConfig()
        {
            MelonPrefs.RegisterBool("FollowCamera", "Activated", true);
            MelonPrefs.RegisterFloat("FollowCamera", "PositionSmoothing", 0.005f);
            MelonPrefs.RegisterFloat("FollowCamera", "RotationSmoothing", 0.005f);
            MelonPrefs.RegisterFloat("FollowCamera", "CamHeight", 1.0f);
            MelonPrefs.RegisterFloat("FollowCamera", "CamDistance", 5.0f);
            MelonPrefs.RegisterFloat("FollowCamera", "CamRotation", 0.0f);
            MelonPrefs.RegisterFloat("FollowCamera", "CamOffset", 5.0f);
        }

        public static void LoadConfig()
        {
            activated = MelonPrefs.GetBool("FollowCamera", "Activated");
            positionSmoothing = MelonPrefs.GetFloat("FollowCamera", "PositionSmoothing");
            rotationSmoothing = MelonPrefs.GetFloat("FollowCamera", "RotationSmoothing");
            camHeight = MelonPrefs.GetFloat("FollowCamera", "CamHeight");
            camDistance = MelonPrefs.GetFloat("FollowCamera", "CamDistance");
            camRotation = MelonPrefs.GetFloat("FollowCamera", "CamRotation");
            camOffset = MelonPrefs.GetFloat("FollowCamera", "CamOffset");
        }

        public static void SaveConfig()
        {
            MelonPrefs.SetBool("FollowCamera", "Activated", activated);
            MelonPrefs.SetFloat("FollowCamera", "PositionSmoothing", positionSmoothing);
            MelonPrefs.SetFloat("FollowCamera", "RotationSmoothing", rotationSmoothing);
            MelonPrefs.SetFloat("FollowCamera", "CamHeight", camHeight);
            MelonPrefs.SetFloat("FollowCamera", "CamDistance", camDistance);
            MelonPrefs.SetFloat("FollowCamera", "CamRotation", camRotation);
            MelonPrefs.SetFloat("FollowCamera", "CamOffset", camOffset);
        }

        public override void OnLevelWasLoaded(int level)
        {
            if (!MelonPrefs.HasKey("FollowCamera", "Activated"))
            {
                CreateConfig();
            }
            else
            {
                LoadConfig();
            }
        }

        public static void CheckCamera()
        {
            //If spectator cam is on
            bool camOn = PlayerPreferences.I.SpectatorCam.Get();
            if (camOn)
            {
                //If spectator cam is set to static third person
                float camMode = PlayerPreferences.I.SpectatorCamMode.Get();
                if (camMode == 1)
                {
                    if (activated)
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
            else { camOK = false; }
        }

        public static void AddOptionsButtons(OptionsMenu optionsMenu)
        {
            void UpdateSlider(OptionsMenuSlider slider, string text)
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

            OptionsMenuSlider MakeSlider(string label, OptionsMenuSlider.AdjustActionDelegate action, string helpText)
            {
                OptionsMenuSlider slider = optionsMenu.AddSlider
                    (
                    0,
                    label,
                    "P",
                    action,
                    null,
                    null,
                    helpText
                    );
                return slider;
            }

            string positionSmoothingLabel = "Position Smoothing";
            OptionsMenuSlider.AdjustActionDelegate positionSmoothingAction = new Action<float>((float n) =>
            {
                positionSmoothing = Mathf.Round((positionSmoothing + (n * 0.001f)) * 1000.0f) / 1000.0f;
                UpdateSlider(positionSmoothingSlider, positionSmoothingLabel + " : " + positionSmoothing.ToString());
            });
            positionSmoothingSlider = MakeSlider(positionSmoothingLabel, positionSmoothingAction, "Changes how smooth positioning will be");
            positionSmoothingSlider.label.text = positionSmoothingLabel + " : " + positionSmoothing.ToString();

            string rotationSmoothingLabel = "Rotation Smoothing";
            OptionsMenuSlider.AdjustActionDelegate rotationSmoothingAction = new Action<float>((float n) =>
            {
                rotationSmoothing = Mathf.Round((rotationSmoothing + (n * 0.001f)) * 1000.0f) / 1000.0f;
                UpdateSlider(rotationSmoothingSlider, rotationSmoothingLabel + " : " + rotationSmoothing.ToString());
            });
            rotationSmoothingSlider = MakeSlider(rotationSmoothingLabel, rotationSmoothingAction, "Changes how smooth rotation will be");
            rotationSmoothingSlider.label.text = rotationSmoothingLabel + " : " + rotationSmoothing.ToString();

            string camOffsetLabel = "Horizontal Offset";
            OptionsMenuSlider.AdjustActionDelegate camOffsetAction = new Action<float>((float n) =>
            {
                camOffset = Mathf.Round((camOffset + (n * 0.1f)) * 10.0f) / 10.0f;
                UpdateSlider(camOffsetSlider, camOffsetLabel + " : " + camOffset.ToString());
            });
            camOffsetSlider = MakeSlider(camOffsetLabel, camOffsetAction, "Changes horizontal position");
            camOffsetSlider.label.text = camOffsetLabel + " : " + camOffset.ToString();

            string camHeightLabel = "Vertical Offset";
            OptionsMenuSlider.AdjustActionDelegate camHeightAction = new Action<float>((float n) =>
            {
                camHeight = Mathf.Round((camHeight + (n * 0.1f)) * 10.0f) / 10.0f;
                UpdateSlider(camHeightSlider, camHeightLabel + " : " + camHeight.ToString());
            });
            camHeightSlider = MakeSlider(camHeightLabel, camHeightAction, "Changes vertical position");
            camHeightSlider.label.text = camHeightLabel + " : " + camHeight.ToString();

            string camDistanceLabel = "Distance";
            OptionsMenuSlider.AdjustActionDelegate camDistanceAction = new Action<float>((float n) =>
            {
                camDistance = Mathf.Round((camDistance + (n * 0.1f)) * 10.0f) / 10.0f;
                UpdateSlider(camDistanceSlider, camDistanceLabel + " : " + camDistance.ToString());
            });
            camDistanceSlider = MakeSlider(camDistanceLabel, camDistanceAction, "Changes the distance");
            camDistanceSlider.label.text = camDistanceLabel + " : " + camDistance.ToString();

            string camRotationLabel = "Rotation";
            OptionsMenuSlider.AdjustActionDelegate camRotationAction = new Action<float>((float n) =>
            {
                camRotation = Mathf.Round((camRotation + (n * 0.1f)) * 10.0f) / 10.0f;
                UpdateSlider(camRotationSlider, camRotationLabel + " : " + camRotation.ToString());
            });
            camRotationSlider = MakeSlider(camRotationLabel, camRotationAction, "Changes the rotation");
            camRotationSlider.label.text = camRotationLabel + " : " + camRotation.ToString();

            optionsMenu.scrollable.AddRow(optionsMenu.AddHeader(0, "Follow Camera <size=5>Must be set to 3rd person static</size>"));

            optionsMenu.scrollable.AddRow(positionSmoothingSlider.gameObject);
            optionsMenu.scrollable.AddRow(rotationSmoothingSlider.gameObject);
            optionsMenu.scrollable.AddRow(camOffsetSlider.gameObject);
            optionsMenu.scrollable.AddRow(camHeightSlider.gameObject);
            optionsMenu.scrollable.AddRow(camDistanceSlider.gameObject);
            optionsMenu.scrollable.AddRow(camRotationSlider.gameObject);

            if (activated)
            {
                spectatorCam.previewCam.gameObject.SetActive(true);
                spectatorCam.previewCamDisplay.SetActive(true);
            }
        }

        public static IEnumerator PerformChecks()
        {
            if (!spectatorCamSet) yield break;
            yield return new WaitForSeconds(.5f);
            CheckCamera();
            if (!camOK) yield break;
            MelonLogger.Log("Checking done.");

            MelonLogger.Log("disabling preview stuff");
            spectatorCam.previewCam.gameObject.SetActive(false);
            spectatorCam.previewCamDisplay.SetActive(false);

            MelonLogger.Log("checking fov");
            //fov = PlayerPreferences.I.SpectatorCamFOV;
            fov = spectatorCam.mFov;
            if (fov != oldFov)
            {
                //if (oldFov != 0f)
                //{
                MelonLogger.Log("updating fov");
                spectatorCam.UpdateFOV();
                // }
                oldFov = fov;
            }
        }

        public static void SetSpectatorCam(SpectatorCam cam, bool isSet)
        {
            spectatorCam = cam;
            spectatorCamSet = isSet;
        }

        public static void SpectatorCamUpdate()
        {
            if (camOK)
            {
                Transform head = AvatarSelector.I.customHead.transform;

                Vector3 hmdPos = head.position;
                Vector3 hmdRot = head.rotation.eulerAngles;

                Vector3 hmdOffsetPos = new Vector3(hmdPos.x + (head.right.x * camOffset), hmdPos.y, hmdPos.z + (head.right.z * camOffset));

                Vector3 camPos = spectatorCam.cam.gameObject.transform.position;
                Quaternion camRot = spectatorCam.cam.gameObject.transform.rotation;

                Vector3 destinationPos = new Vector3(hmdOffsetPos.x - (head.forward.x * camDistance), camHeight, hmdOffsetPos.z - (head.forward.z * camDistance));
                Quaternion destinationRot = Quaternion.Euler(camRotation, hmdRot.y, 0);

                spectatorCam.cam.gameObject.transform.position = Vector3.Slerp(camPos, destinationPos, positionSmoothing);
                spectatorCam.cam.gameObject.transform.rotation = Quaternion.Slerp(camRot, destinationRot, rotationSmoothing);
            }
        }
    }
}
