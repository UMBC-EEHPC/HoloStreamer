using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

using static ViewerData;

public class StreamScript : MonoBehaviour
{
    private RawImage image;
    private byte[] buffer;
    private NetworkStream aideck_stream;
    private BinaryReader aideck_reader;
    private int current_buffer_offset;

    // Start is called before the first frame update
    void Start()
    {
        buffer = new byte[131072];
        current_buffer_offset = 0;
        aideck_stream = new NetworkStream(ViewerData.aideck_socket);
        aideck_reader = new BinaryReader(aideck_stream);
        image = GetComponent<RawImage>();
    }

    // Update is called once per frame
    void Update()
    {
        CPXHeaderPacked packet = new CPXHeaderPacked();
        packet.length = aideck_reader.ReadUInt16();
        packet.ignored = aideck_reader.ReadUInt16();

        JPEGPStartHeader jpeg_header = new JPEGPStartHeader();
        jpeg_header.magic = aideck_reader.ReadByte();
        jpeg_header.width = aideck_reader.ReadUInt16();
        jpeg_header.height = aideck_reader.ReadUInt16();
        jpeg_header.depth = aideck_reader.ReadByte();
        jpeg_header.format = aideck_reader.ReadByte();
        jpeg_header.size = aideck_reader.ReadUInt32();

        if (jpeg_header.magic == 0xBC)
        {
            while (current_buffer_offset < jpeg_header.size) {
                packet.length = aideck_reader.ReadUInt16();
                packet.ignored = aideck_reader.ReadUInt16();
                receive_buffer(packet.length - 2);
            }
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(buffer);
            image.texture = tex;
        }
        current_buffer_offset = 0;
    }

    void receive_buffer(int size)
    {
        int total_size_read = 0;
        while (total_size_read < size)
        {
            int size_read = aideck_reader.Read(buffer, current_buffer_offset, size - total_size_read);
            total_size_read += size_read;
            current_buffer_offset += size_read;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct CPXHeaderPacked
    {
        [MarshalAs(UnmanagedType.U2)]
        public ushort length;
        [MarshalAs(UnmanagedType.U2)]
        public ushort ignored;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct JPEGPStartHeader
    {
        [MarshalAs(UnmanagedType.U1)]
        public byte magic;
        [MarshalAs(UnmanagedType.U2)]
        public ushort width;
        [MarshalAs(UnmanagedType.U2)]
        public ushort height;
        [MarshalAs(UnmanagedType.U1)]
        public byte depth;
        [MarshalAs(UnmanagedType.U1)]
        public byte format;
        [MarshalAs(UnmanagedType.U4)]
        public uint size;
    }
}
