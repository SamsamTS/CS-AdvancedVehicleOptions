using UnityEngine;
using ColossalFramework.Plugins;

namespace AdvancedVehicleOptions
{
    public class DebugUtils
    {
        public const string modPrefix = "[Advanced Vehicle Options] ";

        public static void Message(string message)
        {
            Log(message);
            DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, modPrefix + message);
        }

        public static void Warning(string message)
        {
            Debug.LogWarning(modPrefix + message);
            DebugOutputPanel.AddMessage(PluginManager.MessageType.Warning, modPrefix + message);
        }

        public static void Log(string message)
        {
            Debug.Log(modPrefix + message);
        }
    }
}
