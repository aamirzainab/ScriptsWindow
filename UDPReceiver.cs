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

    private Vector3 angleCorrection ; 
    private Vector3 planeNormal ; 
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
        float angleOfIntersection = float.Parse(splitData[0]);
        float angleWithNormal = float.Parse(splitData[1]); 

        float radiansIntersection = angleOfIntersection * Mathf.Deg2Rad;
        float radiansNormal = angleWithNormal * Mathf.Deg2Rad; 

        // Vector3 direction = new Vector3(Mathf.Sin(radiansIntersection), 0, -Mathf.Cos(radiansIntersection));

        // Vector3 planeNormal = Vector3.forward; 
        // Quaternion rotation = Quaternion.FromToRotation(Vector3.forward, planeNormal.normalized);
        // Vector3 alignedDirection = rotation * direction;

        // Quaternion normalRotation = Quaternion.AngleAxis(angleWithNormal, Vector3.up);
        // alignedDirection = normalRotation * alignedDirection;

        // Vector3 rayOrigin = targetObject.transform.position;

        // Debug.DrawRay(rayOrigin, alignedDirection * 10, Color.red, 5);  
        // RaycastHit hit;
        // if (Physics.Raycast(rayOrigin, alignedDirection, out hit, 10))  
        // {
        //     Debug.Log("Ray hit " + hit.collider.name);
        // }
    }





    // void PerformRaycast(ZainabRayCast session)
    // {
    //     RaycastHit hit;
    //     float maxRayDistance = Mathf.Infinity;
    //     Vector3 rayOrigin = targetObject.transform.position;
    //     Quaternion initialRotation = originRotation; 
    //     Quaternion currentRotation = session.IpadRotation;
    //     Quaternion rotationDelta = Quaternion.Inverse(initialRotation) * currentRotation;
    //     Quaternion correctedRotation = new Quaternion(rotationDelta.x, rotationDelta.y, rotationDelta.z, rotationDelta.w);


    //     targetObject.transform.rotation = correctedRotation; 

    //     Vector3 rayDirection = new Vector3(session.Direction.x,session.Direction.y,session.Direction.z); 

    //     bool didHit = Physics.Raycast(rayOrigin, rayDirection, out hit, 100000f);
    //     Color rayColor = didHit ? Color.red : Color.blue;
    //     Debug.DrawRay(rayOrigin, rayDirection * (didHit ? hit.distance : 100f), rayColor, 1f);

    //     if (didHit)
    //     {
    //         NetworkCube cube = hit.collider.GetComponent<NetworkCube>();
    //         if (cube != null && IsServer)
    //         {
    //             Debug.Log("Raycast hit a cube on the server.");
    //             cube.SetHighlight(true);
    //         }
    //         Debug.Log("Raycast hit an object at distance: " + hit.distance);
    //     }
    // }
void PerformRaycast(ZainabRayCast session)
{
    // Normalize the direction vector coming from the iPad
    Vector3 normalizedDirection = session.Direction.normalized;

    // Apply the iPad's current rotation to the direction to orient it correctly in the world space
    Quaternion newRotation = session.IpadRotation;
    Quaternion adjustment = Quaternion.Euler(0, 90, 0); // Adjust 90 degrees on Y-axis if needed
    Vector3 worldSpaceDirection = newRotation * adjustment * normalizedDirection;
    worldSpaceDirection.Normalize();

    // Determine the start position of the ray, typically the position of the object or camera
    Vector3 startPosition = targetObject.transform.position; // You might need to adjust this based on your specific setup

    // Create the ray using the corrected world space direction
    Ray ray = new Ray(startPosition, worldSpaceDirection);
    float maxRayDistance = 1000f; // Maximum distance the ray should check

    // Perform the raycast
    if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance))
    {
        // Visualize the ray extending from the hit point in the world space direction
        Debug.DrawRay(hit.point, worldSpaceDirection * 100, Color.green, 2f); // Extend the ray by 100 units

        // Check if the hit object is a NetworkCube and if it is on the server
        NetworkCube cube = hit.collider.GetComponent<NetworkCube>();
        if (cube != null && IsServer)
        {
            // Debug.Log("Raycast hit a cube on the server.");
            cube.SetHighlight(true);
        }
    }
    else
    {
        // Visualize the ray if no hit occurs
        Debug.DrawRay(ray.origin, ray.direction * 10000f, Color.blue, 2f);
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
                originRotation = rotation;
                isOriginSet = true;

                Debug.Log("Origin set at rotation: " + originRotation);
                Vector3 forward = originRotation * Vector3.forward; // Forward vector - iPad's looking direction
                Vector3 right = originRotation * Vector3.right;     // Right vector
                Vector3 up = originRotation * Vector3.up;           // Up vector

                planeNormal = forward; 
                angleCorrection = new Vector3(
                Vector3.Angle(rotation * Vector3.forward, Vector3.forward),
                Vector3.Angle(rotation * Vector3.right, Vector3.right),
                Vector3.Angle(rotation * Vector3.up, Vector3.up)
            );
                Debug.Log("DRAWINGGGGG");
                Debug.DrawRay(virtualCamera.transform.position, forward * 2, Color.blue,10000f);  
                Debug.DrawRay(virtualCamera.transform.position, right * 2, Color.red, 100000f);    
                Debug.DrawRay(virtualCamera.transform.position, up * 2, Color.green, 1000f);   
                // Debug.DrawRay(virtualCamera.transform.position, Vector3.forward * 2, Color.cyan,10000f); 
                // Debug.DrawRay(virtualCamera.transform.position, Vector3.right * 2, Color.magenta, 100000f);     
                // Debug.DrawRay(virtualCamera.transform.position, Vector3.up * 2, Color.yellow, 1000f);   
            }
        }

    }


    void OnDestroy()
    {
        udpClient.Close();
    }
}



 
