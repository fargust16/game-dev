#if TRIPO_BRIDGE
using System.Net;
using System.Net.Sockets;
using Tripo3D.UnityBridge.Editor;
using UnityEditor;
using UnityEngine;

namespace GameDev.Tools.Editor
{
    /// <summary>
    /// The Tripo Bridge package writes its log, errors included, only into its own window. This echoes that log to
    /// the Console and Editor.log, and checks the bridge's port before opening the window, so a server that does not
    /// start says why.
    /// </summary>
    [InitializeOnLoad]
    public static class TripoBridgeDiagnostics
    {
        private const string Prefix = "[Tripo Bridge] ";

        static TripoBridgeDiagnostics()
        {
            LogHelper.OnLog -= Echo;
            LogHelper.OnLog += Echo;
        }

        private static void Echo(string message)
        {
            if (message.Contains("ERROR"))
            {
                Debug.LogError(Prefix + message);
            }
            else
            {
                Debug.Log(Prefix + message);
            }
        }

        [MenuItem("GameDev/Tools/Tripo Bridge - Check Port and Open")]
        public static void CheckPortAndOpen()
        {
            var port = ProtocolConstants.SERVER_PORT;
            var listener = new TcpListener(IPAddress.Parse(ProtocolConstants.SERVER_HOST), port);
            try
            {
                listener.Start();
                listener.Stop();
                Debug.Log($"{Prefix}port {port} is free; opening the window starts the server.");
            }
            catch (SocketException e)
            {
                Debug.LogError(Prefix + Explain(port, e));
            }

            TripoWebSocketWindow.ShowWindow();
        }

        private static string Explain(int port, SocketException e)
        {
            switch (e.SocketErrorCode)
            {
                case SocketError.AddressAlreadyInUse:
                    return $"port {port} is already taken: by this bridge if its window is open, otherwise by another " +
                           "program (find it with: netstat -ano | findstr 60610).";
                case SocketError.AccessDenied:
                    return $"port {port} is refused by Windows, usually because it lies in a range reserved by Hyper-V, " +
                           "WSL or Docker (see: netsh interface ipv4 show excludedportrange protocol=tcp).";
                default:
                    return $"port {port} cannot be opened: {e.SocketErrorCode} ({e.Message}).";
            }
        }
    }
}
#endif
