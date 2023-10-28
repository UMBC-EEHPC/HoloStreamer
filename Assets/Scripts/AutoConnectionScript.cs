using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

#if !UNITY_EDITOR
using Windows.Networking;
using Windows.Networking.Sockets;
#endif

using static ViewerData;

public class AutoConnectionScript : MonoBehaviour
{
    private const int SERVER_PORT = 25555;
    private const int BROADCAST_PORT = 32222;

    private Canvas canvas;
    private TMP_Text connection_text;

    void OnEnable() 
    {
        canvas = GetComponentInParent<Canvas>().rootCanvas;
        connection_text = GameObject.Find("ConnectionMessage").GetComponent<TMP_Text>();
    }

    void Update() 
    {
        Debug.Log("Connecting to server!");
        connect_to_server(connection_text);
    }

    public void connect_to_server(TMP_Text connection_text)
    {
        Debug.Log("Listening for IP!");
        string received_ip = listen_for_ip();
        Debug.Log("Received IP: " + received_ip);

        IPAddress address;
        if (IPAddress.TryParse(received_ip, out address)) {
            switch (address.AddressFamily) {
                case System.Net.Sockets.AddressFamily.InterNetwork:
                case System.Net.Sockets.AddressFamily.InterNetworkV6:
                    break;
                default:
                    connection_text.text = "Received malformed IP address, restarting server autodiscovery...";
                    Debug.Log(connection_text.text);
                    return;
            }
        }

        Debug.Log(connection_text.text);
        //string received_ip = "jetbot";
        connection_text.text = "Attempting to connect to " + received_ip;
        Socket socket = connect_socket(received_ip, SERVER_PORT);

        if (socket != null) {
            ViewerData.server_socket = socket;
            SceneManager.LoadScene("ControlScene", LoadSceneMode.Single);
        } else {
            connection_text.text = "Failed to connect, restarting server autodiscovery...";
            Debug.Log(connection_text.text);
        }
    }

    private string listen_for_ip()
    {
        using (UdpClient listener = new UdpClient(BROADCAST_PORT))
        {
            IPEndPoint group_endpoint = new IPEndPoint(IPAddress.Any, BROADCAST_PORT);
            while (true)
            {
                try
                {
                    Debug.Log("Listening for IP address...");
                    byte[] bytes = listener.Receive(ref group_endpoint);
                    string received_ip = Encoding.UTF8.GetString(bytes, 0, bytes.Length);
                    Debug.Log("Received " + received_ip);
                    return received_ip;
                }
                catch (Exception e)
                {
                    Debug.Log(e.ToString());
                }
            }
        }
    }

    private Socket connect_socket(string ip, int port)
    {
        while (true)
        {
            try {
                IPHostEntry host_entry = Dns.GetHostEntry(ip);
                IPEndPoint ip_endpoint = new IPEndPoint(host_entry.AddressList[0], port);

                Socket socket = new Socket(ip_endpoint.AddressFamily,
                                           SocketType.Stream, ProtocolType.Tcp);

                Debug.Log("Making connection to: " + ip);

                IAsyncResult result = socket.BeginConnect(ip_endpoint, null, null);

                bool success = result.AsyncWaitHandle.WaitOne(15000, true);

                if (success) { 
                    socket.ReceiveTimeout = 0;
                    socket.SendTimeout = 0;
                    socket.EndConnect(result);

                    Debug.Log("Made connection");
                    return socket;
                } else {
                    Debug.Log("Failed to connect");
                    socket.Close();
                }
            } catch (SocketException e) {
                connection_text.text = "Caught exception, restarting connection attempt: " + e.ToString();
                Debug.Log(connection_text.text);
            }
        }
        return null;
    }
}