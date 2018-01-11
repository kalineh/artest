//-----------------------------------------------------------------------
// <copyright file="HelloARController.cs" company="Google">
//
// Copyright 2017 Google Inc. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// </copyright>
//-----------------------------------------------------------------------

namespace GoogleARCore.HelloAR
{
    using System.Collections.Generic;
    using GoogleARCore;
    using UnityEngine;
    using UnityEngine.Rendering;

    public class CowTrippingController
        : MonoBehaviour
    {
        public Camera FirstPersonCamera;
        public GameObject TrackedPlanePrefab;
        public GameObject SearchingForPlaneUI;

        public UnityEngine.UI.Text actionText;

        public GameObject CowPrefab;
        public GameObject ChickenPrefab;
        public GameObject PigPrefab;
        public GameObject BalloonPrefab;
        public GameObject GrassPrefab;

        private GameObject clickPrefab;
        private bool clickPrefabRequireTarget;

        private List<TrackedPlane> m_NewPlanes = new List<TrackedPlane>();
        private List<TrackedPlane> m_AllPlanes = new List<TrackedPlane>();

        private bool m_IsQuitting = false;

        public enum SpawnMode
        {

        }

        public void OnClickClearAll()
        {
            clickPrefab = null;
            clickPrefabRequireTarget = false;

            var anchors = GameObject.FindObjectsOfType<Anchor>();
            foreach (var anchor in anchors)
                Destroy(anchor.gameObject);
        }

        public void OnClickCowPrefab() { clickPrefab = CowPrefab; clickPrefabRequireTarget = false; actionText.text = "TAP PLANE TO SPAWN"; }
        public void OnClickChickenPrefab() { clickPrefab = ChickenPrefab; clickPrefabRequireTarget = false; actionText.text = "TAP PLANE TO SPAWN"; }
        public void OnClickPigPrefab() { clickPrefab = PigPrefab; clickPrefabRequireTarget = false; actionText.text = "TAP PLANE TO SPAWN"; }
        public void OnClickBalloonPrefab() { clickPrefab = BalloonPrefab; clickPrefabRequireTarget = true; actionText.text = "TAP ANIMAL TO ATTACH"; }
        public void OnClickGrassPrefab() { clickPrefab = GrassPrefab; clickPrefabRequireTarget = false; actionText.text = "TAP PLANE TO SPAWN"; }

        public void Update()
        {
#if UNITY_EDITOR
            // just run for mouse emulation mode
            UpdateGameInput();
            return;
#endif

            if (Input.GetKey(KeyCode.Escape))
            {
                Application.Quit();
            }

            _QuitOnConnectionErrors();

            if (Frame.TrackingState != TrackingState.Tracking)
            {
                const int lostTrackingSleepTimeout = 15;
                Screen.sleepTimeout = lostTrackingSleepTimeout;
                return;
            }

            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            Frame.GetPlanes(m_NewPlanes, TrackableQueryFilter.New);
            for (int i = 0; i < m_NewPlanes.Count; i++)
            {
                GameObject planeObject = Instantiate(TrackedPlanePrefab, Vector3.zero, Quaternion.identity, transform);
                planeObject.GetComponent<TrackedPlaneVisualizerWithCollider>().Initialize(m_NewPlanes[i]);
            }

            Frame.GetPlanes(m_AllPlanes);
            bool showSearchingUI = true;
            for (int i = 0; i < m_AllPlanes.Count; i++)
            {
                if (m_AllPlanes[i].TrackingState == TrackingState.Tracking)
                {
                    showSearchingUI = false;
                    break;
                }
            }

            SearchingForPlaneUI.SetActive(showSearchingUI);

            UpdateGameInput();
        }

        public void UpdateGameInput()
        {
            var touchCount = Input.touchCount;
            for (int i = 0; i < touchCount; ++i)
            {
                var touch = Input.GetTouch(i);
                if (touch.phase == TouchPhase.Began)
                    HandleTouchAt(touch.position);
            }

#if UNITY_EDITOR
            if (Input.GetMouseButtonDown(0))
            {
                var touchPosition = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
                HandleTouchAt(touchPosition);
            }

            if (Input.GetMouseButton(1))
            {
                var mouseDelta = Input.mousePosition;

                var cameraSpeed = 1.0f;
                var cameraX = Input.GetAxis("Mouse X");
                var cameraY = Input.GetAxis("Mouse Y");

                if (Input.GetKey(KeyCode.W)) FirstPersonCamera.transform.position += FirstPersonCamera.transform.forward * cameraSpeed * Time.deltaTime;
                if (Input.GetKey(KeyCode.S)) FirstPersonCamera.transform.position -= FirstPersonCamera.transform.forward * cameraSpeed * Time.deltaTime;
                if (Input.GetKey(KeyCode.A)) FirstPersonCamera.transform.position -= FirstPersonCamera.transform.right * cameraSpeed * Time.deltaTime;
                if (Input.GetKey(KeyCode.D)) FirstPersonCamera.transform.position += FirstPersonCamera.transform.right * cameraSpeed * Time.deltaTime;
                if (Input.GetKey(KeyCode.Q)) FirstPersonCamera.transform.position -= FirstPersonCamera.transform.up * cameraSpeed * Time.deltaTime;
                if (Input.GetKey(KeyCode.E)) FirstPersonCamera.transform.position += FirstPersonCamera.transform.up * cameraSpeed * Time.deltaTime;

                var rotationSpeed = 100.0f;
                FirstPersonCamera.transform.Rotate(Vector3.up, cameraX * rotationSpeed * Time.deltaTime, Space.Self);
                FirstPersonCamera.transform.Rotate(Vector3.right, cameraY * -rotationSpeed * Time.deltaTime, Space.Self);
            }
#endif
        }

        public void HandleTouchAt(Vector2 touchPosition)
        {
            var uiRay = FirstPersonCamera.ScreenPointToRay(touchPosition);
            var uiMask = LayerMask.GetMask("UI");
            var uiHit = Physics.Raycast(uiRay, float.PositiveInfinity, uiMask);
            if (uiHit)
                return;

            if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                return;

            var objRay = FirstPersonCamera.ScreenPointToRay(touchPosition);
            var objInfo = new RaycastHit();
            var objMask = LayerMask.GetMask("TrackableAttachedObject");
            var objRange = 100.0f;
            var objHit = Physics.Raycast(objRay, out objInfo, objRange, objMask);

            var planeRay = FirstPersonCamera.ScreenPointToRay(touchPosition);
            var planeFlags = TrackableHitFlags.PlaneWithinBounds | TrackableHitFlags.PlaneWithinPolygon;
            var planeInfo = new TrackableHit();
            var planeHit = Session.Raycast(touchPosition.x, touchPosition.y, planeFlags, out planeInfo);

            if (clickPrefab == null)
            {
                if (objHit)
                {
                    Debug.LogFormat("Bump Object: {0}", objInfo.collider.gameObject);

                    var objBody = objInfo.collider.gameObject.GetComponent<Rigidbody>();
                    if (objBody != null)
                    {
                        var force = objBody.useGravity ? Vector3.up * 125.0f : objRay.direction * 25.0f;

                        objBody.AddTorque(0.0f, 15000.0f, 0.0f, ForceMode.Acceleration);
                        objBody.AddForceAtPosition(force, objInfo.point, ForceMode.Acceleration);
                    }
                }

                return;
            }

            if (clickPrefab != null && !clickPrefabRequireTarget && objHit)
                return;
            if (clickPrefab != null && clickPrefabRequireTarget && !objHit)
                return;

            if (clickPrefab != null && clickPrefabRequireTarget && objHit)
            {
                var spawnPos = objInfo.collider.ClosestPointOnBounds(objInfo.collider.gameObject.transform.position + Vector3.up * 1.0f);
                var spawnRot = objInfo.collider.gameObject.transform.rotation;
                var spawnObj = GameObject.Instantiate(clickPrefab, spawnPos, spawnRot);

                Debug.LogFormat("Click Attach Spawn: {0}", spawnObj.name);

                spawnObj.transform.parent = objInfo.collider.gameObject.transform.parent;

                clickPrefab = null;
                clickPrefabRequireTarget = false;
                actionText.text = "TAP TO SELECT";

                return;
            }

            if (clickPrefab != null && !clickPrefabRequireTarget)
            {
#if UNITY_EDITOR
                var emulatedPlaneInfo = new RaycastHit();
                var emulatedPlaneMask = LayerMask.GetMask("TrackablePlaneEmulated");
                var emulatedPlaneHit = Physics.Raycast(planeRay, out emulatedPlaneInfo, 100.0f, emulatedPlaneMask);

                if (emulatedPlaneHit)
                {
                    var emulatedPlaneObj = emulatedPlaneInfo.collider.gameObject;
                    var emulatedPlaneSpawn = Instantiate(clickPrefab, emulatedPlaneInfo.point + Vector3.up * 0.01f, Quaternion.identity);

                    Debug.LogFormat("Emulated Plane Spawn: {0}", emulatedPlaneSpawn.name);

                    emulatedPlaneSpawn.transform.parent = emulatedPlaneObj.transform;

                    clickPrefab = null;
                    clickPrefabRequireTarget = false;
                    actionText.text = "TAP TO SELECT";

                    return;
                }
#endif

                if (planeHit)
                {
                    var planeObj = planeInfo.Trackable.CreateAnchor(planeInfo.Pose);
                    var planeSpawn = GameObject.Instantiate(clickPrefab, planeInfo.Pose.position, planeInfo.Pose.rotation);

                    Debug.LogFormat("Plane Spawn: {0}", planeSpawn.name);

                    planeSpawn.transform.parent = planeObj.transform;

                    clickPrefab = null;
                    clickPrefabRequireTarget = false;
                    actionText.text = "TAP TO SELECT";

                    return;
                }
            }
        }

        /// <summary>
        /// Quit the application if there was a connection error for the ARCore session.
        /// </summary>
        private void _QuitOnConnectionErrors()
        {
            if (m_IsQuitting)
            {
                return;
            }

            // Quit if ARCore was unable to connect and give Unity some time for the toast to appear.
            if (Session.ConnectionState == SessionConnectionState.UserRejectedNeededPermission)
            {
                _ShowAndroidToastMessage("Camera permission is needed to run this application.");
                m_IsQuitting = true;
                Invoke("DoQuit", 0.5f);
            }
            else if (Session.ConnectionState == SessionConnectionState.ConnectToServiceFailed)
            {
                _ShowAndroidToastMessage("ARCore encountered a problem connecting.  Please start the app again.");
                m_IsQuitting = true;
                Invoke("DoQuit", 0.5f);
            }
        }

        /// <summary>
        /// Actually quit the application.
        /// </summary>
        private void DoQuit()
        {
            Application.Quit();
        }

        /// <summary>
        /// Show an Android toast message.
        /// </summary>
        /// <param name="message">Message string to show in the toast.</param>
        private void _ShowAndroidToastMessage(string message)
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject unityActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

            if (unityActivity != null)
            {
                AndroidJavaClass toastClass = new AndroidJavaClass("android.widget.Toast");
                unityActivity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
                {
                    AndroidJavaObject toastObject = toastClass.CallStatic<AndroidJavaObject>("makeText", unityActivity,
                        message, 0);
                    toastObject.Call("show");
                }));
            }
        }
    }
}
