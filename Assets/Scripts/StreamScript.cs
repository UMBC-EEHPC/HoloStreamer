using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

#if !UNITY_EDITOR 
using Windows.Networking.Sockets;
using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.Input;
#endif

using static ViewerData;

public class StreamScript : MonoBehaviour
{
    private static Mutex mut = new Mutex();
    private RawImage image;
    private byte[] buffer;
    private NetworkStream server_stream;
    private BinaryReader server_reader;
    private BinaryWriter server_writer;
    private int current_buffer_offset;
    private int max_buffer_size;
    private int most_recent_type;

    private Encoding utf8_encoding;

    private Task receiver_task;

    private TMP_Text debug_text;

    private byte current_gesture_state = 0;

    void Start()
    {
        debug_text = GameObject.Find("DebugMessage").GetComponent<TMP_Text>();
        var buttons = GameObject.Find("ButtonCollection");
        buttons.SetActive(false);

        max_buffer_size = 131072;
        buffer = new byte[max_buffer_size];
        current_buffer_offset = 0;
        
        utf8_encoding = Encoding.UTF8;

        server_stream = new NetworkStream(ViewerData.server_socket);
        server_reader = new BinaryReader(server_stream);
        server_writer = new BinaryWriter(server_stream, utf8_encoding);
        image = GameObject.Find("StreamImage").GetComponent<RawImage>();

        Debug.Log("\n\n\nSpawning threads!\n\n\n");
        receiver_task = Task.Run(() => receiver_thread());
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
#if !UNITY_EDITOR
        MixedRealityPose index_finger_pose;
        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.IndexTip, Handedness.Left, out index_finger_pose)) {
            //debug_text.text = "Euler Angles: " + index_finger_pose.Rotation.eulerAngles + "\nForward: " + (index_finger_pose.Forward * 365.0f) + "\nRight: " + (index_finger_pose.Right * 365.0f);
            //debug_text.text = "Position: " + index_finger_pose.Position + "\nForward: " + (index_finger_pose.Forward) + "\nRight: " + (index_finger_pose.Right);
            Vector3 finger_pos = index_finger_pose.Forward;
            if (finger_pos.z >= 0.90f && finger_pos.z <= 1.0f) {
                //debug_text.text = debug_text.text + "\nForward";
                debug_text.text = "Forward";
                current_gesture_state = 1;
            } else if (finger_pos.x >= 0.0f && finger_pos.x < 1.0f)  {
                //debug_text.text = debug_text.text + "\nRight";
                debug_text.text = "Right";
                current_gesture_state = 2;
            } else if (finger_pos.x < 0.0f && finger_pos.x >= -1.0f) {
                //debug_text.text = debug_text.text + "\nLeft";
                debug_text.text = "Left";
                current_gesture_state = 3;
            } else {
                //debug_text.text = debug_text.text + "\nStop";
                debug_text.text = "Stop";
                current_gesture_state = 0;
            }
            server_writer.Write(current_gesture_state);
            Debug.Log(debug_text.text);
        } else {
            debug_text.text = "Left index finger not detected";
            Debug.Log(debug_text.text);
        }
#endif
    }

    void receiver_thread()
    {
        Debug.Log("\n\n\nSpawned thread!\n\n\n");
        while (true) {
            if (mut.WaitOne(10)) {
                int type = server_reader.ReadInt32();
                int length = server_reader.ReadInt32();
                //Debug.Log("Type: " + type + " Length: " + length);
                if (length < max_buffer_size) {
                    receive_buffer(length);

                    most_recent_type = type;

                    switch (type) {
                        case 0xDE:
                            var json_data = utf8_encoding.GetString(buffer, 0, length);
                            // Stub.

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

    void receive_buffer(int size)
    {
        int total_size_read = 0;
        while (total_size_read < size) {
            int size_read = server_reader.Read(buffer, current_buffer_offset, size - total_size_read);
            total_size_read += size_read;
            current_buffer_offset += size_read;
        }
    }

    bool is_socket_connected()
    {
        bool part1 = ViewerData.server_socket.Poll(1000, SelectMode.SelectRead);
        bool part2 = (ViewerData.server_socket.Available == 0);
        if (part1 && part2) {
            return false;
        } else {
            return true;
        }
    }


    public void stop_receiver()
    {
        if (receiver_task != null) {
            receiver_task.Dispose();
            receiver_task = null;
        }
    }

    public void OnDestroy()
    {
        Debug.Log("Destroying StreamScript!");
        stop_receiver();
        mut.Dispose();
    }
}
