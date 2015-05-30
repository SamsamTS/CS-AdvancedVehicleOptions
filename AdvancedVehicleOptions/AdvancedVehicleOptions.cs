using ICities;
using UnityEngine;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Xml.Serialization;
using System.Reflection;

using ColossalFramework;
using ColossalFramework.Threading;
using ColossalFramework.UI;

namespace AdvancedVehicleOptions
{
    public class AdvancedVehicleOptions : LoadingExtensionBase, IUserMod
    {
        #region IUserMod implementation
        public string Name
        {
            get { return "Advanced Vehicle Options 1.0"; }
        }

        public string Description
        {
            get { return "Customize your vehicles"; }
        }
        #endregion

        private static Dictionary<VehicleOptions, VehicleInfo> m_vehicles = new Dictionary<VehicleOptions,VehicleInfo>();
        private static ItemClass m_emptyItemClass = new ItemClass();
                
        private static GUI.UIMainPanel m_mainPanel;

        private const string m_fileName = "AdvancedVehicleOptions.xml";
                
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
            m_mainPanel = (GUI.UIMainPanel)view.AddUIComponent(typeof(GUI.UIMainPanel));

            m_emptyItemClass.m_service = ItemClass.Service.None;
            m_emptyItemClass.m_subService = ItemClass.SubService.None;
            m_emptyItemClass.m_level = ItemClass.Level.None;

            // Loading the configuration
            LoadConfig();
        }

        /// <summary>
        /// Called when the level is unloaded
        /// </summary>
        public override void OnLevelUnloading()
        {
            RestoreItemClasses();
            GameObject.Destroy(m_mainPanel);
        }
        #endregion

        /// <summary>
        /// Load and apply the configuration file
        /// </summary>
        public static void LoadConfig()
        {
            RestoreItemClasses();
            m_vehicles.Clear();

            if (!File.Exists(m_fileName))
            {
                DebugUtils.Log("Configuration file not found. Creating new configuration file.");
                CheckNewVehicles();

                // Loading old mods saves
                List<OldMods.Vehicle> removerList = OldMods.VehicleRemoverMod.LoadConfig();
                List<OldMods.VehicleColorInfo> colorList = OldMods.VehicleColorChangerMod.LoadConfig();

                if(removerList != null || colorList != null)
                {
                    foreach(VehicleOptions options in m_vehicles.Keys)
                    {
                        if(removerList != null)
                        {
                            OldMods.Vehicle vehicle = removerList.Find((v) => { return v.name == options.name; });
                            if (vehicle.name == options.name) options.enabled = vehicle.enabled;
                        }

                        if (colorList != null)
                        {
                            OldMods.VehicleColorInfo vehicle = colorList.Find((v) => { return v.name == options.name; });
                            if (vehicle != null && vehicle.name == options.name)
                            {
                                options.color0 = vehicle.color0;
                                options.color1 = vehicle.color1;
                                options.color2 = vehicle.color2;
                                options.color3 = vehicle.color3;
                            }
                        }
                    }
                }
                
                SaveConfig();

                // Update GUI list
                ApplyOptions();
                m_mainPanel.optionList = m_vehicles.Keys.ToArray();
                return;
            }

            XmlSerializer xmlSerializer = new XmlSerializer(typeof(VehicleOptions[]));
            VehicleOptions[] optionsList = null;

            try
            {
                // Trying to Deserialize the configuration file
                using (FileStream stream = new FileStream(m_fileName, FileMode.Open))
                {
                    optionsList = xmlSerializer.Deserialize(stream) as VehicleOptions[];
                }
            }
            catch (Exception e)
            {
                // Couldn't Deserialize (XML malformed?)
                DebugUtils.Warning("Couldn't load configuration (XML malformed?)");
                Debug.LogException(e);
                return;
            }

            if (optionsList == null)
            {
                DebugUtils.Warning("Couldn't load configuration (vehicle list is null)");
                return;
            }

            // Filling dictionary
            for (uint i = 0; i < optionsList.Length; i++)
            {
                VehicleInfo prefab = PrefabCollection<VehicleInfo>.FindLoaded(optionsList[i].name);
                if (prefab != null)
                {
                    optionsList[i].vehicleType = prefab.m_vehicleType;
                    optionsList[i].itemClass = prefab.m_class;
                    optionsList[i].hasTrailer = prefab.m_trailers != null && prefab.m_trailers.Length > 0;
                    optionsList[i].localizedName = GetLocalizedName(prefab);

                    m_vehicles.Add(optionsList[i], prefab);
                }
            }

            // Checking for new vehicles
            CheckNewVehicles();

            // Update GUI list
            ApplyOptions();
            m_mainPanel.optionList = m_vehicles.Keys.ToArray();
        }

        public static void ApplyOptions()
        {
            foreach(VehicleOptions options in m_vehicles.Keys)
            {
                ApplyMaxSpeed(options);
                ApplyColors(options);
                ApplySpawning(options);
                ApplyBackEngine(options);
            }
        }

        /// <summary>
        /// Save the configuration file
        /// </summary>
        public static void SaveConfig()
        {
            if (m_vehicles.Count == 0) return;

            try
            {
                using (FileStream stream = new FileStream(m_fileName, FileMode.OpenOrCreate))
                {
                    stream.SetLength(0); // Emptying the file !!!
                    XmlSerializer xmlSerializer = new XmlSerializer(typeof(VehicleOptions[]));
                    xmlSerializer.Serialize(stream, m_vehicles.Keys.ToArray());
                }
            }
            catch (Exception e)
            {
                DebugUtils.Warning("Couldn't save configuration at \"" + Directory.GetCurrentDirectory() + "\"");
                Debug.LogException(e);
            }
        }

        public static void ClearVehicles(VehicleOptions options, bool parked)
        {
            if (m_vehicles.Count == 0) return;

            VehicleInfo prefab = m_vehicles[options];

            if (parked)
            {
                if (!m_removeParkedList.Contains(prefab))
                {
                    m_removeParkedList.Add(prefab);
                    if (!m_removeParkedThreadRunning) new EnumerableActionThread(ActionRemoveParked);
                }
            }
            else
            {
                if (!m_removeList.Contains(prefab))
                {
                    m_removeList.Add(prefab);
                    if (!m_removeThreadRunning) new EnumerableActionThread(ActionRemoveExisting);
                }
            }
        }

        private static List<VehicleInfo> m_removeList = new List<VehicleInfo>();
        private static bool m_removeThreadRunning = false;

        public static IEnumerator ActionRemoveExisting(ThreadBase t)
        {
            m_removeThreadRunning = true;

            VehicleManager vehicleManager =  Singleton<VehicleManager>.instance;
            

            while(m_removeList.Count != 0)
            {
                VehicleInfo[] prefabs = m_removeList.ToArray();

                for (ushort i = 0; i < vehicleManager.m_vehicles.m_size; i++)
                {
                    if (vehicleManager.m_vehicles.m_buffer[i].Info == null) continue;
                    if (prefabs.Contains(vehicleManager.m_vehicles.m_buffer[i].Info))
                        vehicleManager.ReleaseVehicle(i);

                    if (i % 256 == 255) yield return i;
                }

                m_removeList.RemoveRange(0, prefabs.Count());
            }

            m_removeThreadRunning = false;
        }

        private static List<VehicleInfo> m_removeParkedList = new List<VehicleInfo>();
        private static bool m_removeParkedThreadRunning = false;

        public static IEnumerator ActionRemoveParked(ThreadBase t)
        {
            m_removeParkedThreadRunning = true;

            VehicleManager vehicleManager = Singleton<VehicleManager>.instance;


            while (m_removeParkedList.Count != 0)
            {
                VehicleInfo[] prefabs = m_removeParkedList.ToArray();

                for (ushort i = 0; i < vehicleManager.m_parkedVehicles.m_size; i++)
                {
                    if (vehicleManager.m_parkedVehicles.m_buffer[i].Info == null) continue;
                    if (prefabs.Contains(vehicleManager.m_parkedVehicles.m_buffer[i].Info))
                        vehicleManager.ReleaseParkedVehicle(i);

                    if (i % 256 == 255) yield return i;
                }

                m_removeParkedList.RemoveRange(0, prefabs.Count());
            }

            m_removeParkedThreadRunning = false;
        }

        public static void ApplyColors(VehicleOptions options)
        {
            if (m_vehicles.Count == 0) return;

            VehicleInfo prefab = m_vehicles[options];

            prefab.m_color0 = options.color0;
            prefab.m_color1 = options.color1;
            prefab.m_color2 = options.color2;
            prefab.m_color3 = options.color3;
        }

        public static void ApplySpawning(VehicleOptions options)
        {
            if (m_vehicles.Count == 0) return;

            VehicleInfo prefab = m_vehicles[options];

            ItemClass newItemClass = options.enabled ? options.itemClass : m_emptyItemClass;

            if (prefab.m_class == newItemClass) return;
            prefab.m_class = newItemClass;

            // Make transfer vehicle dirty
            typeof(VehicleManager).GetField("m_transferVehiclesDirty", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(Singleton<VehicleManager>.instance, true);
        }

        public static void ApplyMaxSpeed(VehicleOptions options)
        {
            if (m_vehicles.Count == 0) return;

            VehicleInfo prefab = m_vehicles[options];
            prefab.m_maxSpeed = options.maxSpeed;
        }

        public static void ApplyBackEngine(VehicleOptions options)
        {
            if (m_vehicles.Count == 0 || !options.hasTrailer) return;

            VehicleInfo prefab = m_vehicles[options];

            // Is it a train?
            if (prefab.m_trailers == null || prefab.m_trailers.Length == 0 || prefab.m_vehicleType != VehicleInfo.VehicleType.Train) return;

            VehicleInfo newTrailer = options.addBackEngine ? prefab : prefab.m_trailers[0].m_info;
            int last = prefab.m_trailers.Length - 1;

            if (prefab.m_trailers[last].m_info == newTrailer) return;

            prefab.m_trailers[last].m_info = newTrailer;

            if (options.addBackEngine)
                prefab.m_trailers[last].m_invertProbability = prefab.m_trailers[last].m_probability;
            else
                prefab.m_trailers[last].m_invertProbability = prefab.m_trailers[0].m_invertProbability;
            
            // TODO Apply on existing trains
            //EnumerableActionThread thread = new EnumerableActionThread(ActionAddBackEngine);
        }

        private static void RestoreItemClasses()
        {
            if (m_vehicles.Count == 0) return;

            foreach(VehicleOptions options in m_vehicles.Keys)
                m_vehicles[options].m_class = options.itemClass;
        }

        /// <summary>
        /// Check if new there are vehicles and add them to the options list
        /// </summary>
        private static void CheckNewVehicles()
        {
            for (uint i = 0; i < PrefabCollection<VehicleInfo>.PrefabCount(); i++)
            {
                VehicleInfo prefab = PrefabCollection<VehicleInfo>.GetPrefab(i);

                if (prefab == null) continue;
                if (m_vehicles.ContainsValue(prefab)) continue;

                // New vehicle
                VehicleOptions options = new VehicleOptions();

                options.name = prefab.name;
                options.maxSpeed = prefab.m_maxSpeed;

                options.color0 = prefab.m_color0;
                options.color1 = prefab.m_color1;
                options.color2 = prefab.m_color2;
                options.color3 = prefab.m_color3;

                options.enabled = true;
                options.addBackEngine = false;

                options.hasTrailer = prefab.m_trailers != null && prefab.m_trailers.Length > 0;

                if (prefab.m_vehicleType == VehicleInfo.VehicleType.Train && options.hasTrailer)
                {
                    options.addBackEngine = prefab.m_trailers[prefab.m_trailers.Length - 1].m_info == prefab;
                }

                options.vehicleType = prefab.m_vehicleType;
                options.itemClass = prefab.m_class;
                options.localizedName = GetLocalizedName(prefab);

                m_vehicles.Add(options, prefab);
            }
        }

        /// <summary>
        /// Get a better displayable and localized name
        /// </summary>
        private static string GetLocalizedName(VehicleInfo prefab)
        {
            // Custom names
            string name = prefab.name;
            if (name.Contains('.'))
            {
                // Removes the steam ID and trailing data from the name
                return name.Substring(name.IndexOf('.') + 1).Replace("_Data", "");
            }

            // Default names
            //name = Singleton<VehicleManager>.instance.GetDefaultVehicleName((ushort)prefab.m_prefabDataIndex);
            //if (name == "Invalid" || name.StartsWith("VEHICLE_TITLE"))
                return prefab.name;
            //return name;
        }
    }
}
