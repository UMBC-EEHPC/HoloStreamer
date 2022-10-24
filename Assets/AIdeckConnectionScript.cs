using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

using static ViewerData;

public class AIdeckConnectionScript : MonoBehaviour
{
    private TMP_InputField aideck_ip_field;

    private const int AIDECK_PORT = 5000;

    void OnEnable()
    {
        var canvas = GetComponentInParent<Canvas>();

        aideck_ip_field = canvas.GetComponent<TMP_InputField>();
    }

    void Update()
    {
    }

    public void connect_to_aideck()
    {
        var canvas = GetComponentInParent<Canvas>().rootCanvas;

        aideck_ip_field = GameObject.Find("AIdeckIPField").GetComponent<TMP_InputField>();

        Socket socket = connect_socket(aideck_ip_field.text, AIDECK_PORT);

        if (socket != null)
        {
            ViewerData.aideck_socket = socket;
            SceneManager.LoadScene("StreamScene", LoadSceneMode.Single);
        }
        else
        {
            Debug.Log("Failed to connect");
        }
    }

    private Socket connect_socket(string ip, int port)
    {
        IPHostEntry host_entry = Dns.GetHostEntry(ip);
        IPEndPoint ip_endpoint = new IPEndPoint(host_entry.AddressList[0], port);

        Socket socket = new Socket(ip_endpoint.AddressFamily,
                                   SocketType.Stream, ProtocolType.Tcp);

        socket.Connect(ip_endpoint);

        if (socket.Connected)
        {
            return socket;
        }
        else
        {
            return null;
        }
    }
}