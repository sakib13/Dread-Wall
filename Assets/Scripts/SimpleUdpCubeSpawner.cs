using PimDeWitte.UnityMainThreadDispatcher;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class CubeVisibilityController : MonoBehaviour
{
    public GameObject cubeA;
    public GameObject cubeB;
    public GameObject cubeC;

    private UdpClient udp;
    private int port = 4211;

    void Start()
    {
        udp = new UdpClient(port);
        udp.BeginReceive(OnUdpData, null);
        Debug.Log("✅ Cube Visibility Controller listening on port " + port);
    }

    void OnUdpData(System.IAsyncResult ar)
    {
        IPEndPoint ip = new IPEndPoint(IPAddress.Any, port);
        byte[] data = udp.EndReceive(ar, ref ip);
        string message = Encoding.ASCII.GetString(data);

        Debug.Log("📩 Received: " + message);

        UnityMainThreadDispatcher.Instance().Enqueue(() => HandleMessage(message));

        udp.BeginReceive(OnUdpData, null);
    }

    void HandleMessage(string msg)
    {
        switch (msg)
        {
            case "A_appear":
                cubeA.SetActive(true);
                break;
            case "A_disappear":
                cubeA.SetActive(false);
                break;

            case "B_appear":
                cubeB.SetActive(true);
                break;
            case "B_disappear":
                cubeB.SetActive(false);
                break;

            case "C_appear":
                cubeC.SetActive(true);
                break;
            case "C_disappear":
                cubeC.SetActive(false);
                break;
        }
    }

    void OnApplicationQuit()
    {
        if (udp != null) udp.Close();
    }
}
