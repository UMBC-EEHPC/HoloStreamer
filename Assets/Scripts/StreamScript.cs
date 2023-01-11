using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

#if !UNITY_EDITOR 
using Windows.Networking.Sockets;

#endif

using static ViewerData;

public class StreamScript : MonoBehaviour
{
    private static Mutex mut = new Mutex();
    private RawImage image;
    private byte[] buffer;
    private NetworkStream aideck_stream;
    private BinaryReader aideck_reader;
    private BinaryWriter aideck_writer;
    private int current_buffer_offset;
    private int max_buffer_size;
    private int most_recent_type;

    private Encoding utf8_encoding;

    private Task receiver_task;
    private Task sender_task;

    private EntityHandler entity_handler;

    void Start()
    {
        max_buffer_size = 131072;
        buffer = new byte[max_buffer_size];
        current_buffer_offset = 0;
        
        utf8_encoding = Encoding.UTF8;

        aideck_stream = new NetworkStream(ViewerData.aideck_socket);
        aideck_reader = new BinaryReader(aideck_stream);
        aideck_writer = new BinaryWriter(aideck_stream, utf8_encoding);
        image = GetComponent<RawImage>();

        Debug.Log("\n\n\nSpawning threads!\n\n\n");
        receiver_task = Task.Run(() => receiver_thread());

        sender_task = Task.Run(() => sender_thread());
    }

    void Update() {
        if (mut.WaitOne(10)) {
            if (most_recent_type == 0xBC) {
                Texture2D tex = new Texture2D(2, 2);
                tex.LoadImage(buffer);
                image.texture = tex;
            } else if (most_recent_type == 0xEF) {
                image.GetComponent<Renderer>().enabled = !image.GetComponent<Renderer>().enabled;
            }
            mut.ReleaseMutex();
        }
    }

    void receiver_thread()
    {
        Debug.Log("\n\n\nSpawned thread!\n\n\n");
        while (true) {
            if (mut.WaitOne(10)) {
                int type = aideck_reader.ReadInt32();
                int length = aideck_reader.ReadInt32();
                Debug.Log("Type: " + type + " Length: " + length);
                if (length < max_buffer_size) {
                    receive_buffer(length);

                    most_recent_type = type;

                    switch (type) {
                        case 0xDE:
                            var json_data = utf8_encoding.GetString(buffer, 0, length);
                            Data data = Data.CreateFromJSON(json_data);
                            entity_handler.AddData(data);

                            break;
                        default:
                            break;
                    }

                    current_buffer_offset = 0;
                }
                mut.ReleaseMutex();
            }
        }
    }

    void sender_thread()
    {
        while (true) {
            Data data = Data.CreateFromGameObkect(gameObject, 0000);
            string json_data = Data.CreateFromData(data);
            json_data += '\n';
            aideck_writer.Write(json_data);
        }
    }

    void receive_buffer(int size)
    {
        int total_size_read = 0;
        while (total_size_read < size) {
            int size_read = aideck_reader.Read(buffer, current_buffer_offset, size - total_size_read);
            total_size_read += size_read;
            current_buffer_offset += size_read;
        }
    }

    public void stop_receiver()
    {
        if (receiver_task != null) {
            receiver_task.Dispose();
            receiver_task = null;
        }
    }

    public void stop_sender()
    {
        if (sender_task != null) {
            sender_task.Dispose();
            sender_task = null;
        }
    }

    public void OnDestroy()
    {
        stop_receiver();
        stop_sender();
        mut.Dispose();
    }
}
