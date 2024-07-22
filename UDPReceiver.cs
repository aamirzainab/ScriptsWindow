using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using System.Collections.Generic; 
using UnityEngine.Scripting;
using UnityEngine;
using Unity.Netcode;
public struct ZainabRayCast
{
    public Vector2 ScreenCoordinates;
    public Vector3 Origin;
    public Vector3 Direction;
    public Vector3 IpadPosition;
    public Quaternion IpadRotation; 
    public bool ScreenCoordinatesReady;
    public bool RaycastDataReady;
}

public class UDPReceiver : NetworkBehaviour
{
    private UdpClient udpClient;
    public int listenPort = 8081; 
    public GameObject targetObject;  
    private Vector3 originPosition = Vector3.zero;
    private Quaternion originRotation = Quaternion.identity;
    private bool isOriginSet = false;
    public Camera mainCamera; 
    public Material highlightMaterial; 
    private List<ZainabRayCast> sessions = new List<ZainabRayCast>();
    public Camera virtualCamera; 
    

    void Start()
    {
        if (IsServer)
        {
                
        udpClient = new UdpClient(listenPort);
        udpClient.Client.Blocking = false; 
        targetObject.transform.position = Vector3.zero; 
        }
        Screen.SetResolution(7680, 2880, false);
        GameObject cameraObject = new GameObject("VirtualCamera");
        virtualCamera = cameraObject.AddComponent<Camera>();
        virtualCamera.enabled = false; // Disable rendering
    }

    void Update()
    {
        if (udpClient.Available > 0)
        {
            try
            {
                IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] receivedBytes = udpClient.Receive(ref remoteEndPoint); 
                string receivedText = Encoding.ASCII.GetString(receivedBytes);
                ProcessReceivedData(receivedText);
                // foreach (Camera cam in Camera.allCameras)
                // {
                //     // Check if the camera is enabled and rendering
                //     if (cam.enabled && cam.isActiveAndEnabled)
                //     {
                //         Debug.Log("Active Camera: " + cam.name + ", Display: " + cam.targetDisplay);
                //     }
                // }
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error receiving UDP: " + e.Message);
            }
        }
    }

    void ProcessReceivedData(string data)
    {
        if (data.StartsWith("SPAWN one:"))
        {
            data = data.Substring("SPAWN one:".Length);
            ParseData(data, true);
        }
        else if (data.StartsWith("DATA"))
        {
            data = data.Substring("DATA:".Length);
            ParseCastAndCoord(data); 
        }
        // else if (data.StartsWith("COORD"))
        // {
        //     data = data.Substring("COORD:".Length);
        //     ParseScreenCoordinates(data); 
        // }
        // else if (data.StartsWith("RAYCAST"))
        // {
        //     data = data.Substring("RAYCAST:".Length);
        //     ParseRayCast(data); 
        // }
        else 
        {
            ParseData(data, false);
        }
    }
    void ParseCastAndCoord(string data)
        {
            string[] splitData = data.Split(',');

            if (splitData.Length >= 15) //Screen coords x, y, Ray origin x, y, z, Ray direction x, y, z, Ipad position x,y,z Ipad quaternion x y z w 
            {
                // Parse screen coordinates
                Vector2 position = new Vector2(
                    float.Parse(splitData[0]) + 0.5f,
                    1.0f - float.Parse(splitData[1])
                );
                Vector2 pixelCoordinates = GetPixelCoordinates(position);

                // Parse ray origin and direction
                Vector3 origin = new Vector3(
                    float.Parse(splitData[2]),
                    float.Parse(splitData[3]),
                    float.Parse(splitData[4])
                );
                Vector3 direction = new Vector3(
                    float.Parse(splitData[5]),
                    float.Parse(splitData[6]),
                    float.Parse(splitData[7])
                );
                Vector3 cameraPosition = new Vector3(
                    float.Parse(splitData[8]),
                    float.Parse(splitData[9]),
                    float.Parse(splitData[10])
                );

                // Parse AR camera rotation
                Quaternion cameraRotation = new Quaternion(
                    float.Parse(splitData[11]),
                    float.Parse(splitData[12]),
                    float.Parse(splitData[13]),
                    float.Parse(splitData[14])
                );
                float width = 1.86f; 
                float height = 0.726f;
                
                Vector3 newPos = new Vector3(pixelCoordinates.x - width, pixelCoordinates.y, targetObject.transform.position.z);
                targetObject.transform.position = newPos;
                // targetObject.transform.rotation = Quaternion.Inverse(cameraRotation) ; 
                Vector2 toSend = new Vector2(newPos.x, newPos.y); 
                ZainabRayCast session = new ZainabRayCast
                {
                    ScreenCoordinates = position,
                    Origin = origin,
                    Direction = direction,
                    IpadPosition = cameraPosition,
                    IpadRotation = cameraRotation,
                    ScreenCoordinatesReady = true,
                    RaycastDataReady = true
                };
                sessions.Add(session);
                PerformRaycast(session);
            }
        }



    // void PerformRaycast(ZainabRayCast session)
    // {

    //  foreach (Camera cam in Camera.allCameras)
    //             {
    //                 // Check if the camera is enabled and rendering
    //                 if (cam.enabled && cam.isActiveAndEnabled)
    //                 {
    //                     // Debug.Log("Active Camera: " + cam.name + ", Display: " + cam.targetDisplay);
    //                     Ray ray = cam.ViewportPointToRay(session.ScreenCoordinates);
    //                     ray.direction = session.Direction;
    //                     RaycastHit hit;
    //                     float rayLength = 100f;
    //                     Debug.DrawRay(ray.origin, ray.direction * rayLength, Color.red, 2f);

    //                         if (Physics.Raycast(ray.origin, ray.direction, out hit, rayLength))
    //                         {
    //                             Debug.Log("Hit: " + hit.collider.name);
    //                             NetworkCube cube = hit.collider.GetComponent<NetworkCube>();
    //                             if (cube != null)
    //                             {
    //                                 cube.SetHighlight(true);
    //                             }
    //                         }
    //                         else
    //                         {
    //                             Debug.Log("No hit");
    //                         }
    //                 }
    //             }
    // }
// void PerformRaycast(ZainabRayCast session)
// {
//     RaycastHit hit;
//     float maxRayDistance = Mathf.Infinity; 
//     Vector3 rayOrigin = targetObject.transform.position;
//     // Vector3 directionVector = Quaternion.Inverse(session.IpadRotation) *Vector3.forward; 
    
//     Vector3 directionVector = Quaternion.Euler(session.Direction) * targetObject.transform.forward;
//     //     Quaternion rayDirectionRotation = Quaternion.Euler(session.Direction); 
//     // Quaternion combinedRotation = session.CameraRotation * directionRotation;
//     // Vector3 directionVector =  combinedRotation* Vector3.forward;
    
//     // Debug.Log("This is session direction " + session.Direction); 
//     Vector3 rayDirection = -session.Direction.normalized;
//     rayDirection = targetObject.transform.forward; 
//     rayDirection = directionVector; 
//     // Debug.Log("This is ray direction " + rayDirection); 

//     // Always draw the ray. The color is red if it hits an object, blue otherwise.
//     bool didHit = Physics.Raycast(rayOrigin, rayDirection, out hit, maxRayDistance);
//     Color rayColor = didHit ? Color.red : Color.blue;
//     Debug.DrawRay(rayOrigin, rayDirection * (didHit ? hit.distance : 100f), rayColor, 1f); // Draw for 2 seconds

//     if (didHit)
//     {
//         // HighlightObject(hit.collider.gameObject);
//         NetworkCube cube = hit.collider.GetComponent<NetworkCube>();
//         Debug.Log("This is the NetworkCube component: " + cube);

//         if (cube != null && IsServer)
//         {
//             Debug.Log("Raycast hit a cube on the server.");
//             cube.SetHighlight(true);
//         }
//         Debug.Log("Raycast hit an object at distance: " + hit.distance);
//     }
//     else
//     {
//         // Debug.Log("Raycast did not hit any object.");
//     }
// }

// void PerformRaycast(ZainabRayCast session)
//     {
//         virtualCamera.transform.position = targetObject.transform.position;
//         virtualCamera.transform.rotation = Quaternion.Inverse(session.IpadRotation);
//         virtualCamera.transform.forward = targetObject.transform.forward; 

//         Ray ray = virtualCamera.ViewportPointToRay(session.ScreenCoordinates); // Using the center point

//         if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
//         {
//             Debug.DrawRay(ray.origin, ray.direction * hit.distance, Color.red, 2f);
//             NetworkCube cube = hit.collider.GetComponent<NetworkCube>();
//             if (cube != null && IsServer)
//             {
//                 cube.SetHighlight(true);
//             }
//         }
//         else
//         {
//             Debug.DrawRay(ray.origin, ray.direction * 10000f, Color.blue, 2f);
//         }
//     }

void PerformRaycast(ZainabRayCast session)
{
    foreach (Camera cam in Camera.allCameras)
    {
        // Check if the camera is enabled and rendering
        if (cam.enabled && cam.isActiveAndEnabled)
        {
            // Convert viewport coordinates (normalized screen coordinates) to a world point
            Vector3 worldPoint = cam.ViewportToWorldPoint(new Vector3(session.ScreenCoordinates.x, session.ScreenCoordinates.y, cam.nearClipPlane));

            // Create a ray from the camera position through the world point
            Ray ray = new Ray(cam.transform.position, worldPoint - cam.transform.position);

            // Optionally, to extend the ray into the scene, you can calculate a farther end point
            Vector3 farPoint = cam.ViewportToWorldPoint(new Vector3(session.ScreenCoordinates.x, session.ScreenCoordinates.y, cam.farClipPlane));

            // Draw the ray in the scene view for debugging
            Debug.DrawRay(ray.origin, ray.direction * (cam.farClipPlane - cam.nearClipPlane), Color.red, 2f);

            // Perform the raycast
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
            {
                Debug.Log("Hit: " + hit.collider.gameObject.name);
                // Additional logic here to handle the hit object
            }
            else
            {
                Debug.Log("No hit detected.");
            }
        }
    }
}


    void HighlightObject(GameObject obj)
    {
        var renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = highlightMaterial;
        }
    }

    Vector2 GetPixelCoordinates(Vector2 normalizedPosition)
    {
        float xPixel = normalizedPosition.x * 1.86f;
        float yPixel = normalizedPosition.y * 0.726f;
        return new Vector2(xPixel, yPixel);
    }

    void ParseData(string data, bool isSettingOrigin)
    {
        string[] splitData = data.Split(',');
        if (splitData.Length >= 7)
        {
            Vector3 position = new Vector3(
                float.Parse(splitData[0]),
                float.Parse(splitData[1]),
                float.Parse(splitData[2])
            );
            Quaternion rotation = new Quaternion(
                float.Parse(splitData[3]),
                float.Parse(splitData[4]),
                float.Parse(splitData[5]),
                float.Parse(splitData[6])
            );

            if (isSettingOrigin && !isOriginSet)
            {
                // originPosition = position;
                // originRotation = rotation;
                // isOriginSet = true;
                // targetObject.transform.localPosition = originPosition;
                // targetObject.transform.localRotation = originRotation;

                // Debug.Log("Origin set at position: " + originPosition + " and rotation: " + originRotation);
            }
            else if (targetObject != null && isOriginSet)
            {
                // Vector3 positionOffset = (position - originPosition);
                // Quaternion rotationOffset = rotation * Quaternion.Inverse(originRotation);
                // targetObject.transform.localPosition = positionOffset;
                // targetObject.transform.localRotation = rotationOffset;
                // mainCamera.transform.LookAt(targetObject.transform.position);
                // Debug.Log("Updated position and rotation relative to origin.");
            }
        }
    }

    void OnDestroy()
    {
        udpClient.Close();
    }
}

    // void ParseScreenCoordinates(string data)
    // {
    //     string[] splitData = data.Split(',');
    //     if (splitData.Length >= 2)
    //     {
    //         Vector2 position = new Vector2(
    //             // (1.0f - (float.Parse(splitData[0])))+ 0.5f,
    //             float.Parse(splitData[0]) + 0.5f, 
    //             1.0f - float.Parse(splitData[1])
    //         ); 
    //         Vector2 pixelCoordinates = GetPixelCoordinates(position); 
    //         // Debug.Log("This is the coordinate in metres " + pixelCoordinates); 

    //         float width = 1.86f; 
    //         float height = 0.726f;

    //         Vector3 newPos = new Vector3(pixelCoordinates.x - width, pixelCoordinates.y, targetObject.transform.position.z);
    //         targetObject.transform.position = newPos; 
    //         Vector2 toSend = new Vector2(newPos.x, newPos.y); 

    //         ZainabRayCast session = new ZainabRayCast
    //         {
    //             ScreenCoordinates = toSend,
    //             ScreenCoordinatesReady = true
    //         };
    //         sessions.Add(session);
    //         TryRaycast(ref session);
    //     }
    // }

    // void ParseRayCast(string data)
    // {
    //     string[] splitData = data.Split(',');
    //     if (splitData.Length >= 6)
    //     {
    //         Vector3 origin = new Vector3(
    //             float.Parse(splitData[0]),
    //             float.Parse(splitData[1]),
    //             float.Parse(splitData[2])
    //         ); 
    //         Vector3 direction = new Vector3(
    //             float.Parse(splitData[3]),
    //             float.Parse(splitData[4]),
    //             float.Parse(splitData[5])
    //         );
    //     for (int i = 0; i < sessions.Count; i++)
    //         {
    //             if (!sessions[i].RaycastDataReady)
    //             {
    //                 ZainabRayCast session = sessions[i];
    //                 session.Origin = origin;
    //                 session.Direction = direction;
    //                 session.RaycastDataReady = true;
    //                 sessions[i] = session; // Important to reassign the struct in the list
    //                 TryRaycast(ref session);
    //                 return;
    //             }
    //         }

    //         // No existing session, create a new one
    //         ZainabRayCast newSession = new ZainabRayCast
    //         {
    //             Origin = origin,
    //             Direction = direction,
    //             RaycastDataReady = true
    //         };
    //         sessions.Add(newSession);
    //     }
    // }
//    void TryRaycast(ref ZainabRayCast session)
//     {
//         if (session.ScreenCoordinatesReady && session.RaycastDataReady)
//         {
//             PerformRaycast(session);
//         }
//     }
