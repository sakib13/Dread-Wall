using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class UdpDebugLogger : MonoBehaviour
{
    private UdpClient udp;
    private int listenPort = 4211;

    void Start()
    {
        udp = new UdpClient(listenPort);
        udp.BeginReceive(OnUdpData, null);
        Debug.Log("✅ UDP Debug Logger started on port " + listenPort);
    }

    void OnUdpData(System.IAsyncResult result)
    {
        IPEndPoint ip = new IPEndPoint(IPAddress.Any, listenPort);
        byte[] data = udp.EndReceive(result, ref ip);
        string message = Encoding.ASCII.GetString(data);

        Debug.Log("📡 UDP Received: " + message);

        udp.BeginReceive(OnUdpData, null); // Keep listening
    }

    void OnApplicationQuit()
    {
        if (udp != null)
        {
            udp.Close();
            Debug.Log("UDP client closed.");
        }
    }
}