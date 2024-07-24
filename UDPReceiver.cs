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
    public Quaternion IpadGyro; 
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
    private float initialYAngle = 0f; 
    private float appliedGyroYAngle = 0f;
    private float calibrationYAngle = 0f;
    //  public LineRenderer lineRenderer; 
    void Start()
    {
        if (IsServer)
        {
                
        udpClient = new UdpClient(listenPort);
        udpClient.Client.Blocking = false; 
        targetObject.transform.position = Vector3.zero; 
        initialYAngle = targetObject.transform.eulerAngles.y ; 
        GameObject cameraObject = new GameObject("VirtualCamera");
        virtualCamera = cameraObject.AddComponent<Camera>();
        virtualCamera.enabled = false; // Disable rendering

        //  lineRenderer = gameObject.AddComponent<LineRenderer>();
            // lineRenderer.material = new Material(Shader.Find("Sprites/Default")); // Ensure you use a material that makes the line visible
            // lineRenderer.startColor = Color.blue;
            // lineRenderer.endColor = Color.red;
            // lineRenderer.startWidth = 0.02f;
            // lineRenderer.endWidth = 0.02f;

        }
        Screen.SetResolution(7680, 2880, false);
        // GameObject cameraObject = new GameObject("VirtualCamera");
        // virtualCamera = cameraObject.AddComponent<Camera>();
        // virtualCamera.enabled = false; // Disable rendering
        // initialYAngle = virtualCamera.transform.eulerAngles.y;
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
        else if (data.StartsWith("ANGLE"))
        { 
            data = data.Substring("ANGLE".Length);
            ParseAngle(data); 
        }
        // else if (data.StartsWith("GRYO"))
        // { 
        //     data = data.Substring("GRYO:".Length);
        //     ParseGyroData(data); 
        // }
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

            // Debug.Log("doid ya come here " + splitData.Length); 

            if (splitData.Length >= 18) //Screen coords x, y, Ray origin x, y, z, Ray direction x, y, z, Ipad position x,y,z Ipad quaternion x y z w, input.gyro quaternion
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

                Quaternion iPadGyro = new Quaternion(
                    float.Parse(splitData[15]),
                    float.Parse(splitData[16]),
                    float.Parse(splitData[17]),
                    float.Parse(splitData[18])
                );
                float width = 1.86f; 
                float height = 0.726f;
                
                Vector3 newPos = new Vector3(pixelCoordinates.x - width, pixelCoordinates.y, targetObject.transform.position.z);
                targetObject.transform.position = newPos;
                // Debug.Log("This is my ipad gyro " + cameraRotation.eulerAngles ); 
                
                Vector2 toSend = new Vector2(newPos.x, newPos.y); 
                ZainabRayCast session = new ZainabRayCast
                {
                    ScreenCoordinates = position,
                    Origin = origin,
                    Direction = direction,
                    IpadPosition = cameraPosition,
                    IpadRotation = cameraRotation,
                    IpadGyro = iPadGyro, 
                    ScreenCoordinatesReady = true,
                    RaycastDataReady = true
                };
                sessions.Add(session);
                PerformRaycast(session);
            }
        }
void ParseAngle(string data)
{
    string[] splitData = data.Split(',');
    float angleWithNormal = float.Parse(splitData[0]);
    float angleWithRight = float.Parse(splitData[1]);
    float angleWithDown = float.Parse(splitData[2]);

    float yaw = angleWithRight * Mathf.Deg2Rad;
    float pitch = angleWithDown * Mathf.Deg2Rad;
    float roll = angleWithNormal * Mathf.Deg2Rad; 

    //     Vector3 rayDirection = new Vector3(
    //     Mathf.Sin(yaw) * Mathf.Cos(pitch),
    //     Mathf.Sin(pitch),
    //     Mathf.Cos(yaw) * Mathf.Cos(pitch)
    // );
    // Vector3 rayDirection = new Vector3(pitch,yaw,roll); 

    // Draw the ray
    // Debug.DrawRay(targetObject.transform.position, rayDirection * 10, Color.red, 2f);
}

    // void PerformRaycast(ZainabRayCast session)
    // {
    //     RaycastHit hit ; 
    //     float maxRayDistance = Mathf.Infinity; 
    //     Vector3 rayOrigin = targetObject.transform.position; 
    //     Quaternion gyroAttitude = session.IpadRotation; 
    //     // virtualCamera.transform.position = targetObject.transform.position; 

    //     Quaternion correctedGyro = new Quaternion(session.IpadGyro.x, session.IpadGyro.y, -session.IpadGyro.z, -session.IpadGyro.w);

    //     // Unity's coordinate system adjustment
    //     Quaternion coordinateAdjustment = Quaternion.Euler(90f, 0f, 0f);
    //     correctedGyro *= coordinateAdjustment;
    //     float pitch = correctedGyro.eulerAngles.y; // Nodding "yes" - tilt forward/backward
    //     float yaw = correctedGyro.eulerAngles.z;   // Shaking "no" - rotate left/right
    //     // targetObject.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    //     Vector3 rayDirection = new Vector3(session.Direction.x, session.Direction.z,session.Direction.x);
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



    void PerformRaycast(ZainabRayCast session)
    {
        RaycastHit hit;
        float maxRayDistance = Mathf.Infinity;
        Vector3 rayOrigin = targetObject.transform.position;
        Quaternion initialRotation = originRotation; 
        Quaternion currentRotation = session.IpadRotation;
        Quaternion rotationDelta = Quaternion.Inverse(initialRotation) * currentRotation;
        Quaternion correctedRotation = new Quaternion(rotationDelta.x, -rotationDelta.y, rotationDelta.z, rotationDelta.w);
        // Quaternion adjustment = Quaternion.Euler(90, 0, 0); // Adjust to landscape mode orientation
        // correctedRotation *= adjustment;
        targetObject.transform.rotation = correctedRotation; 
        Vector3 rayDirection = session.Direction; 
        // Vector3 rayDirection = correctedRotation * Vector3.forward;  

        bool didHit = Physics.Raycast(rayOrigin, rayDirection, out hit, 100000f);
        Color rayColor = didHit ? Color.red : Color.blue;
        Debug.DrawRay(rayOrigin, rayDirection * (didHit ? hit.distance : 100f), rayColor, 1f);

        if (didHit)
        {
            NetworkCube cube = hit.collider.GetComponent<NetworkCube>();
            if (cube != null && IsServer)
            {
                Debug.Log("Raycast hit a cube on the server.");
                cube.SetHighlight(true);
            }
            Debug.Log("Raycast hit an object at distance: " + hit.distance);
        }
    }
//  void PerformRaycast(ZainabRayCast session)
//     {

//      foreach (Camera cam in Camera.allCameras)
//                 {
//                     // Check if the camera is enabled and rendering
//                     if (cam.enabled && cam.isActiveAndEnabled)
//                     {
//                         // Debug.Log("Active Camera: " + cam.name + ", Display: " + cam.targetDisplay);
//                         Ray ray = cam.ViewportPointToRay(session.ScreenCoordinates);


//                             Quaternion initialRotation = originRotation; 
//                             Quaternion currentRotation = session.IpadRotation;
//                             Quaternion rotationDelta = Quaternion.Inverse(initialRotation) * currentRotation;
//                             Quaternion correctedRotation = new Quaternion(rotationDelta.x, -rotationDelta.y, rotationDelta.z, rotationDelta.w);
//                             Vector3 rayDirection = correctedRotation * Vector3.forward ;
//                             ray.direction = rayDirection ; 
//                         RaycastHit hit;
//                         float rayLength = 100f;
//                         Debug.DrawRay(ray.origin, ray.direction * rayLength, Color.red, 2f);

//                             if (Physics.Raycast(ray.origin, ray.direction, out hit, rayLength))
//                             {
//                                 Debug.Log("Hit: " + hit.collider.name);
//                                 NetworkCube cube = hit.collider.GetComponent<NetworkCube>();
//                                 if (cube != null)
//                                 {
//                                     cube.SetHighlight(true);
//                                 }
//                             }
//                             else
//                             {
//                                 Debug.Log("No hit");
//                             }
//                     }
//                 }
//     }
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
                originRotation = rotation;
                isOriginSet = true;
                // targetObject.transform.localPosition = originPosition;
                // targetObject.transform.localRotation = originRotation;

                Debug.Log("Origin set at rotation: " + originRotation);
            }
        }

    }


    void OnDestroy()
    {
        udpClient.Close();
    }
}



        // //   if (didHit)
        // // {
        // //     lineRenderer.SetPosition(0, rayOrigin);
        // //     lineRenderer.SetPosition(1, hit.point); // Draw line to where ray hits object
        // // }
        // // else
        // // {
        // //     lineRenderer.SetPosition(0, rayOrigin);
        // //     lineRenderer.SetPosition(1, rayOrigin + rayDirection * 100f); // Or some large number to indicate direction
        // // }

        
// void PerformRaycast(ZainabRayCast session)
// {
//     // Set the position to the target object's position
//     virtualCamera.transform.position = targetObject.transform.position;
    
//     // Get the gyroscope attitude and adjust it to Unity's coordinate system
//     Quaternion gyroAttitude = session.IpadGyro ; 
//     Quaternion adjustedRotation = new Quaternion(gyroAttitude.x, gyroAttitude.y, -gyroAttitude.z, -gyroAttitude.w);
//     virtualCamera.transform.localRotation = adjustedRotation;

//     // Rotate to correct for Unity's coordinate system
//     // virtualCamera.transform.Rotate(0f, 0f, 180f, Space.Self);
//     // virtualCamera.transform.Rotate(90f, 180f, 0f, Space.World);

//     virtualCamera.transform.Rotate(90f, 0f, 0f, Space.World); // Adjust pitch
//     virtualCamera.transform.Rotate(0f, -90f, 0f, Space.World); // Adjust yaw to align forward



//     // Define directional vectors relative to the adjusted rotation
//     Vector3 upVec = virtualCamera.transform.up;
//     Vector3 forwardVec = virtualCamera.transform.forward;

//     // Create a ray from the virtual camera based on the screen coordinates
//     Ray ray = virtualCamera.ViewportPointToRay(session.ScreenCoordinates);
//     // ray.direction = session.Direction; 

//     // Perform the raycast
//     if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
//     {
//         Debug.DrawRay(ray.origin, ray.direction * hit.distance, Color.red, 2f);
//         NetworkCube cube = hit.collider.GetComponent<NetworkCube>();
//         if (cube != null && IsServer)
//         {
//             cube.SetHighlight(true);
//         }
//     }
//     else
//     {
//         Debug.DrawRay(ray.origin, ray.direction * 10000f, Color.blue, 2f);
//     }
// }

// void PerformRaycast(ZainabRayCast session)
// {
//     virtualCamera.transform.position = targetObject.transform.position;
//     Quaternion gyroAttitude = session.IpadGyro ; 
//     virtualCamera.transform.rotation = gyroAttitude;
//     virtualCamera.transform.Rotate(0f, 0f, 180f, Space.Self);
//     virtualCamera.transform.Rotate(90f, 180f, 0f, Space.World);
//     appliedGyroYAngle = virtualCamera.transform.eulerAngles.y ; 
//     virtualCamera.transform.Rotate(0f, -calibrationYAngle, 0f, Space.World); 

//     Vector3 rayDirection =  new Vector3(session.Direction.x, session.Direction.y, -session.Direction.z); // Assuming a simple z-flip might suffice

//     Ray ray = virtualCamera.ViewportPointToRay(session.ScreenCoordinates);
//     ray.direction = rayDirection ;

//     if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
//     {
//         Debug.DrawRay(ray.origin, ray.direction * hit.distance, Color.red, 2f);
//         NetworkCube cube = hit.collider.GetComponent<NetworkCube>();
//         if (cube != null && IsServer)
//         {
//             cube.SetHighlight(true);
//         }
//     }
//     else
//     {
//         Debug.DrawRay(ray.origin, ray.direction * 10000f, Color.blue, 2f);
//     }
// }
