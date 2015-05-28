using ICities;
using UnityEngine;

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

using ColossalFramework.UI;

namespace AdvancedVehicleOptions
{
    public class AdvancedVehicleOptions : LoadingExtensionBase, IUserMod
    {
        #region IUserMod implementation
        public string Name
        {
            get { return "Advanced Vehicle Options"; }
        }

        public string Description
        {
            get { return "Customize vehicles"; }
        }
        #endregion

        public static bool Initialized
        {
            get { return m_initialized; }
        }

        private static bool m_initialized = false;
        private static FileSystemWatcher m_watcher = new FileSystemWatcher();

        private UIComponent m_mainPanel;
        private UIComponent m_optionsPanel;
                
        #region LoadingExtensionBase overrides
        /// <summary>
        /// Called when the level (game, map editor, asset editor) is loaded
        /// </summary>
        public override void OnLevelLoaded(LoadMode mode)
        {
            // Is it an actual game ?
            if (mode != LoadMode.LoadGame && mode != LoadMode.NewGame)
                return;

            // Creating GUI
            UIView view = UIView.GetAView();
            m_mainPanel = view.AddUIComponent(typeof(GUI.UIMainPanel));


            for (uint i = 0; i < PrefabCollection<VehicleInfo>.PrefabCount(); i++)
            {
                VehicleInfo prefab = PrefabCollection<VehicleInfo>.GetPrefab(i);
                if (prefab != null && prefab.m_vehicleType == VehicleInfo.VehicleType.Train)
                {
                    int n = prefab.m_trailers.Length - 1;
                    if (n > 0)
                    {
                        prefab.m_trailers[n].m_info = prefab;
                        prefab.m_trailers[n].m_invertProbability =prefab.m_trailers[n].m_probability;
                    }
                }
            }

            m_initialized = true;

            return;

            m_initialized = true;

            // Watching configuration file for any changes
            if (!m_watcher.EnableRaisingEvents)
            {
                m_watcher.Filter = "AdvancedVehicleOptions.xml";
                m_watcher.NotifyFilter = NotifyFilters.LastWrite;
                m_watcher.Changed += new FileSystemEventHandler(OnFileChanged);

                m_watcher.EnableRaisingEvents = true;
            }

            // Loading the configuration
            LoadConfig();
        }

        /// <summary>
        /// Called when the level is unloaded
        /// </summary>
        public override void OnLevelUnloading()
        {
             m_initialized = false;

            GameObject.Destroy(m_mainPanel);
            GameObject.Destroy(m_optionsPanel);
        }
        #endregion

        /// <summary>
        /// FileSystemWatcher callback
        /// </summary>
        private static void OnFileChanged(object source, FileSystemEventArgs e)
        {
            // Applying changes
            if (m_initialized) LoadConfig();
        }

        /// <summary>
        /// Load and apply the configuration file
        /// </summary>
        public static void LoadConfig()
        {
            if (!File.Exists("AdvancedVehicleOptions.xml"))
            {
                Debug.Log("Configuration file not found. Creating new configuration file.");
                SaveConfig();
                return;
            }

            XmlSerializer xmlSerializer = new XmlSerializer(typeof(VehicleOptions[]));
            VehicleOptions[] colors = null;

            try
            {
                // Trying to deserialize the configuration file
                using (FileStream stream = new FileStream("AdvancedVehicleOptions.xml", FileMode.Open))
                {
                    colors = xmlSerializer.Deserialize(stream) as VehicleOptions[];
                }
            }
            catch (Exception e)
            {
                // Couldn't deserialize (XML malformed?)
                Debug.LogException(e);
                return;
            }

            if (colors == null) return;

            // Applying new colors to each prefabs
            for (int i = 0; i < colors.Length; i++)
            {
                VehicleInfo prefab = PrefabCollection<VehicleInfo>.FindLoaded(colors[i].name);
                if (prefab != null)
                {
                    prefab.m_useColorVariations = true;
                    prefab.m_color0 = colors[i].color0;
                    prefab.m_color1 = colors[i].color1;
                    prefab.m_color2 = colors[i].color2;
                    prefab.m_color3 = colors[i].color3;
                }
            }

        }

        /// <summary>
        /// Save the configuration file
        /// </summary>
        public static void SaveConfig()
        {
            List<VehicleOptions> list = new List<VehicleOptions>();

            // Compiling each prefab colors into a list
            for (uint i = 0; i < PrefabCollection<VehicleInfo>.PrefabCount(); i++)
            {
                VehicleInfo prefab = PrefabCollection<VehicleInfo>.GetPrefab(i);

                if (prefab != null)
                {
                    VehicleOptions info = new VehicleOptions();
                    info.name = prefab.name;
                    info.color0 = prefab.m_color0;
                    info.color1 = prefab.m_color1;
                    info.color2 = prefab.m_color2;
                    info.color3 = prefab.m_color3;

                    list.Add(info);
                }
            }

            // The list shouldn't be empty
            if (list.Count == 0)
            {
                DebugOutputPanel.AddMessage(ColossalFramework.Plugins.PluginManager.MessageType.Warning,
                    "Couldn't create configuration. PrefabCollection is empty.");
                return;
            }

            // Serializing the list
            try
            {
                using (FileStream stream = new FileStream("AdvancedVehicleOptions.xml", FileMode.OpenOrCreate))
                {
                    stream.SetLength(0); // Emptying the file !!!
                    XmlSerializer xmlSerializer = new XmlSerializer(typeof(VehicleOptions[]));
                    xmlSerializer.Serialize(stream, list.ToArray());
                }
            }
            catch (Exception e)
            {
                DebugOutputPanel.AddMessage(ColossalFramework.Plugins.PluginManager.MessageType.Warning,
                    "Couldn't save configuration at \"" + Directory.GetCurrentDirectory() + "\"");
                Debug.LogException(e);
            }
        }


        public static void ClearVehicles(VehicleOptions options, bool parked)
        {
        }

        public static void ApplyOptions(VehicleOptions options)
        {
        }
    }
}
