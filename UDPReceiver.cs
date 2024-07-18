using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using System.Collections.Generic; 
using UnityEngine.Scripting;
public struct ZainabRayCast
{
    public Vector2 ScreenCoordinates;
    public Vector3 Origin;
    public Vector3 Direction;
    public bool ScreenCoordinatesReady;
    public bool RaycastDataReady;
}

public class UDPReceiver : MonoBehaviour
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

    void Start()
    {
        Screen.SetResolution(7680, 2880, false);
        udpClient = new UdpClient(listenPort);
        udpClient.Client.Blocking = false; 
        targetObject.transform.position = Vector3.zero; 
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
        else if (data.StartsWith("COORD"))
        {
            data = data.Substring("COORD:".Length);
            ParseScreenCoordinates(data); 
        }
        else if (data.StartsWith("RAYCAST"))
        {
            data = data.Substring("RAYCAST:".Length);
            ParseRayCast(data); 
        }
        else 
        {
            ParseData(data, false);
        }
    }

    void ParseScreenCoordinates(string data)
    {
        string[] splitData = data.Split(',');
        if (splitData.Length >= 2)
        {
            Vector2 position = new Vector2(
                float.Parse(splitData[0]) + 0.5f,
                1.0f - float.Parse(splitData[1])
            ); 
            Vector2 pixelCoordinates = GetPixelCoordinates(position); 
            Debug.Log("This is the coordinate in metres " + pixelCoordinates); 

            float width = 1.86f; 
            float height = 0.726f;

            Vector3 newPos = new Vector3(pixelCoordinates.x - width, pixelCoordinates.y, targetObject.transform.position.z);
            targetObject.transform.position = newPos; 
            Vector2 toSend = new Vector2(newPos.x, newPos.y); 

            // ZainabRayCast session = new ZainabRayCast
            // {
            //     ScreenCoordinates = toSend,
            //     ScreenCoordinatesReady = true
            // };
            // sessions.Add(session);
            // TryRaycast(ref session);
        }
    }

    void ParseRayCast(string data)
    {
        string[] splitData = data.Split(',');
        if (splitData.Length >= 6)
        {
            Vector3 origin = new Vector3(
                float.Parse(splitData[0]),
                float.Parse(splitData[1]),
                float.Parse(splitData[2])
            ); 
            Vector3 direction = new Vector3(
                float.Parse(splitData[3]),
                float.Parse(splitData[4]),
                float.Parse(splitData[5])
            );
        // for (int i = 0; i < sessions.Count; i++)
        //     {
        //         if (!sessions[i].RaycastDataReady)
        //         {
        //             ZainabRayCast session = sessions[i];
        //             session.Origin = origin;
        //             session.Direction = direction;
        //             session.RaycastDataReady = true;
        //             sessions[i] = session; // Important to reassign the struct in the list
        //             TryRaycast(ref session);
        //             return;
        //         }
        //     }

        //     // No existing session, create a new one
        //     ZainabRayCast newSession = new ZainabRayCast
        //     {
        //         Origin = origin,
        //         Direction = direction,
        //         RaycastDataReady = true
        //     };
        //     sessions.Add(newSession);
        }
    }
   void TryRaycast(ref ZainabRayCast session)
    {
        if (session.ScreenCoordinatesReady && session.RaycastDataReady)
        {
            PerformRaycast(session);
        }
    }

    void PerformRaycast(ZainabRayCast session)
    {
        Ray ray = mainCamera.ViewportPointToRay(session.ScreenCoordinates);
        // Ray ray = 
        
        ray.direction = session.Direction.normalized;

        RaycastHit hit;
        float rayLength = 100f;

        Debug.DrawRay(ray.origin, ray.direction * rayLength, Color.red, 2f);

        if (Physics.Raycast(ray.origin, ray.direction, out hit, rayLength))
        {
            Debug.Log("Hit: " + hit.collider.name);
            HighlightObject(hit.collider.gameObject);
        }
        else
        {
            Debug.Log("No hit");
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
