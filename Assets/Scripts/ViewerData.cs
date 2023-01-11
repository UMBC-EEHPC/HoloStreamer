using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

#if !UNITY_EDITOR 
using Windows.Networking.Sockets;

using static ViewerData;
#endif

public static class ViewerData
{
    public static Socket aideck_socket;
#if !UNITY_EDITOR
    public static StreamSocket socket;
#endif
    public static double x, y, angle;
}
