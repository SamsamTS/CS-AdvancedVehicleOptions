using ICities;
using UnityEngine;

using System;
using System.Text;
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
    public class ModInfo : IUserMod
    {
        public string Name
        {
            get { return "Advanced Vehicle Options " + version; }
        }

        public string Description
        {
            get { return "Customize your vehicles"; }
        }

        public const string version = "1.2.3";
    }
    
    public class AdvancedVehicleOptions : LoadingExtensionBase
    {
        private static GameObject m_gameObject;
        private static VehicleOptions[] m_options;
        private static GUI.UIMainPanel m_mainPanel;

        private static List<VehicleInfo> m_removeList = new List<VehicleInfo>();
        private static bool m_removeAll = false;
        private static bool m_removeThreadRunning = false;

        private static List<VehicleInfo> m_removeParkedList = new List<VehicleInfo>();
        private static bool m_removeParkedThreadRunning = false;
        private static bool m_removeParkedAll = false;

        private static FieldInfo m_transferVehiclesDirty = typeof(VehicleManager).GetField("m_transferVehiclesDirty", BindingFlags.Instance | BindingFlags.NonPublic);

        private const string m_fileName = "AdvancedVehicleOptions.xml";
                
        #region LoadingExtensionBase overrides
        public override void OnCreated(ILoading loading)
        {
            try
            {
                // Storing default values ASAP (before any mods have the time to change values)
                DefaultOptions.StoreAll();

                // Creating a backup
                if (File.Exists(m_fileName))
                {
                    File.Copy(m_fileName, m_fileName + ".bak", true);
                    DebugUtils.Log("Backup configuration file created");
                }
            }
            catch(Exception e)
            {
                Debug.LogException(e);
            }
        }
        /// <summary>
        /// Called when the level (game, map editor, asset editor) is loaded
        /// </summary>
        public override void OnLevelLoaded(LoadMode mode)
        {
            try
            {
                // Is it an actual game ?
                if (mode != LoadMode.LoadGame && mode != LoadMode.NewGame)
                {
                    DefaultOptions.Clear();
                    return;
                }

                // Creating GUI
                UIView view = UIView.GetAView();
                m_gameObject = new GameObject("AdvancedVehicleOptions");
                m_gameObject.transform.SetParent(view.transform);

                try
                {
                    m_mainPanel = m_gameObject.AddComponent<GUI.UIMainPanel>();
                    DebugUtils.Log("UIMainPanel created");
                }
                catch
                {
                    DebugUtils.Warning("A new version of the mod has been installed." + Environment.NewLine +
                        "The game must be exited completely for changes to take effect." + Environment.NewLine +
                        "Until then the mod is disabled.");
                    return;
                }

                new EnumerableActionThread(BrokenVehicleFix);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        /// <summary>
        /// Called when the level is unloaded
        /// </summary>
        public override void OnLevelUnloading()
        {
            try
            {
                SaveConfig();
                m_options = null;

                GUI.UIUtils.DestroyDeeply(m_mainPanel);
                GameObject.Destroy(m_gameObject);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        public override void OnReleased()
        {
            try
            {
                DebugUtils.Log("Restoring default values");
                DefaultOptions.RestoreAll();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
        #endregion

        /// <summary>
        /// Load and apply the configuration file
        /// </summary>
        public static void LoadConfig(UIComponent source)
        {
            // Clear all options
            m_options = null; 

            // Store modded values
            DefaultOptions.StoreAllModded();

            if (!File.Exists(m_fileName))
            {
                DebugUtils.Log("Configuration file not found. Creating new configuration file.");

                CreateConfig();

                // Update GUI list
                m_mainPanel.optionList = m_options;
                return;
            }

            XmlSerializer xmlSerializer = new XmlSerializer(typeof(OptionsList));
            OptionsList options = null;

            try
            {
                // Trying to Deserialize the configuration file
                using (FileStream stream = new FileStream(m_fileName, FileMode.Open))
                {
                    options = xmlSerializer.Deserialize(stream) as OptionsList;
                }
            }
            catch (Exception e)
            {
                // Couldn't Deserialize (XML malformed?)
                DebugUtils.Warning("Couldn't load configuration (XML malformed?)");
                Debug.LogException(e);

                options = null;
            }

            if (options == null || options.items == null)
            {
                DebugUtils.Warning("Couldn't load configuration (vehicle list is null). Default values will be used.");
            }
            else
            {
                // Remove unneeded options
                List<VehicleOptions> optionsList = new List<VehicleOptions>();

                for (uint i = 0; i < options.items.Length; i++)
                {
                    if (options.items[i] != null && options.items[i].prefab != null) optionsList.Add(options.items[i]);
                }

                m_options = optionsList.ToArray();

                // Update existing vehicles
                new EnumerableActionThread(VehicleOptions.UpdateCapacityUnits);
                new EnumerableActionThread(VehicleOptions.UpdateBackEngines);
            }

            // Checking for new vehicles
            CompileVehiclesList();

            // Checking for conflicts
            DefaultOptions.CheckForConflicts();

            // Update GUI list
            m_mainPanel.optionList = m_options;

            DebugUtils.Log("Configuration loaded");
            LogVehicleListSteamID();
        }

        /// <summary>
        /// Save the configuration file
        /// </summary>
        public static void SaveConfig()
        {
            if (m_options == null) return;

            try
            {
                using (FileStream stream = new FileStream(m_fileName, FileMode.OpenOrCreate))
                {
                    stream.SetLength(0); // Emptying the file !!!
                    XmlSerializer xmlSerializer = new XmlSerializer(typeof(OptionsList));
                    xmlSerializer.Serialize(stream, new OptionsList() { version = ModInfo.version, items = m_options });
                    DebugUtils.Log("Configuration saved");
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
            if (parked)
            {
                if(options == null)
                {
                    m_removeParkedAll = true;
                    m_removeParkedList.Clear();
                    if (!m_removeParkedThreadRunning) new EnumerableActionThread(ActionRemoveParkedAll);
                    return;
                }
                if (!m_removeParkedList.Contains(options.prefab))
                {
                    m_removeParkedList.Add(options.prefab);
                    if (!m_removeParkedThreadRunning) new EnumerableActionThread(ActionRemoveParked);
                }
            }
            else
            {
                if (options == null)
                {
                    m_removeAll = true;
                    m_removeList.Clear();
                    if (!m_removeThreadRunning) new EnumerableActionThread(ActionRemoveExistingAll);
                    return;
                }
                if (!m_removeList.Contains(options.prefab))
                {
                    m_removeList.Add(options.prefab);
                    if (!m_removeThreadRunning) new EnumerableActionThread(ActionRemoveExisting);
                }
            }
        }

        public static IEnumerator ActionRemoveExisting(ThreadBase t)
        {
            m_removeThreadRunning = true;

            VehicleManager vehicleManager =  Singleton<VehicleManager>.instance;
            
            while(m_removeList.Count != 0)
            {
                VehicleInfo[] prefabs = m_removeList.ToArray();

                for (ushort i = 0; i < vehicleManager.m_vehicles.m_size; i++)
                {
                    if (m_removeAll) break;
                    if (vehicleManager.m_vehicles.m_buffer[i].Info != null)
                    {
                        if (prefabs.Contains(vehicleManager.m_vehicles.m_buffer[i].Info))
                            vehicleManager.ReleaseVehicle(i);
                    }

                    if (i % 256 == 255) yield return i;
                }

                if(m_removeAll)
                {
                    new EnumerableActionThread(ActionRemoveExistingAll);
                    break;
                }

                m_removeList.RemoveRange(0, prefabs.Count());
            }

            m_removeThreadRunning = false;
        }

        public static IEnumerator ActionRemoveParked(ThreadBase t)
        {
            m_removeParkedThreadRunning = true;

            VehicleManager vehicleManager = Singleton<VehicleManager>.instance;

            while (m_removeParkedList.Count != 0)
            {
                VehicleInfo[] prefabs = m_removeParkedList.ToArray();

                for (ushort i = 0; i < vehicleManager.m_parkedVehicles.m_size; i++)
                {
                    if (m_removeParkedAll) break;
                    if (vehicleManager.m_parkedVehicles.m_buffer[i].Info != null)
                    {
                        if (prefabs.Contains(vehicleManager.m_parkedVehicles.m_buffer[i].Info))
                            vehicleManager.ReleaseParkedVehicle(i);
                    }

                    if (i % 256 == 255) yield return i;
                }

                if (m_removeParkedAll)
                {
                    new EnumerableActionThread(ActionRemoveParkedAll);
                    break;
                }
                m_removeParkedList.RemoveRange(0, prefabs.Count());
            }

            m_removeParkedThreadRunning = false;
        }

        public static IEnumerator ActionRemoveExistingAll(ThreadBase t)
        {
            m_removeThreadRunning = true;

            VehicleManager vehicleManager = Singleton<VehicleManager>.instance;

            for (ushort i = 0; i < vehicleManager.m_vehicles.m_size; i++)
            {
                if (vehicleManager.m_vehicles.m_buffer[i].Info != null)
                    vehicleManager.ReleaseVehicle(i);

                if (i % 256 == 255) yield return i;
            }

            m_removeThreadRunning = false;
        }

        public static IEnumerator ActionRemoveParkedAll(ThreadBase t)
        {
            m_removeParkedThreadRunning = true;

            VehicleManager vehicleManager = Singleton<VehicleManager>.instance;

            for (ushort i = 0; i < vehicleManager.m_parkedVehicles.m_size; i++)
            {
                if (vehicleManager.m_parkedVehicles.m_buffer[i].Info != null)
                    vehicleManager.ReleaseParkedVehicle(i);

                if (i % 256 == 255) yield return i;
            }

            m_removeParkedThreadRunning = false;
        }

        private static void CreateConfig()
        {
            CompileVehiclesList();

            // Loading old mods saves
            List<OldMods.Vehicle> removerList = OldMods.VehicleRemoverMod.LoadConfig();
            List<OldMods.VehicleColorInfo> colorList = OldMods.VehicleColorChangerMod.LoadConfig();

            if (removerList != null || colorList != null)
            {
                for (int i = 0; i < m_options.Length; i++)
                {
                    if (removerList != null)
                    {
                        OldMods.Vehicle vehicle = removerList.Find((v) => { return v.name == m_options[i].name; });
                        if (vehicle.name == m_options[i].name) m_options[i].enabled = vehicle.enabled;
                    }

                    if (colorList != null)
                    {
                        OldMods.VehicleColorInfo vehicle = colorList.Find((v) => { return v.name == m_options[i].name; });
                        if (vehicle != null && vehicle.name == m_options[i].name)
                        {
                            m_options[i].color0 = vehicle.color0;
                            m_options[i].color1 = vehicle.color1;
                            m_options[i].color2 = vehicle.color2;
                            m_options[i].color3 = vehicle.color3;
                        }
                    }
                }
            }

            SaveConfig();
        }

        /// <summary>
        /// Check if there are new vehicles and add them to the options list
        /// </summary>
        private static void CompileVehiclesList()
        {
            List<VehicleOptions> optionsList = new List<VehicleOptions>();
            if(m_options != null) optionsList.AddRange(m_options);

            for (uint i = 0; i < PrefabCollection<VehicleInfo>.PrefabCount(); i++)
            {
                VehicleInfo prefab = PrefabCollection<VehicleInfo>.GetPrefab(i);

                if (prefab == null || ContainsPrefab(prefab)) continue;

                // New vehicle
                VehicleOptions options = new VehicleOptions();
                options.SetPrefab(prefab);

                optionsList.Add(options);
            }

            if(m_options != null)
                DebugUtils.Log("Found " + (optionsList.Count - m_options.Length) + " new vehicle(s)");
            else
                DebugUtils.Log("Found " + optionsList.Count + " new vehicle(s)");

            m_options = optionsList.ToArray();

        }

        private static bool ContainsPrefab(VehicleInfo prefab)
        {
            if (m_options == null) return false;
            for (int i = 0; i < m_options.Length; i++)
            {
                if (m_options[i].prefab == prefab) return true;
            }
            return false;
        }

        private static void LogVehicleListSteamID()
        {
            StringBuilder steamIDs = new StringBuilder("Vehicle Steam IDs : ");

            for (int i = 0; i < m_options.Length; i++)
            {
                if (m_options[i] != null && m_options[i].name.Contains("."))
                {
                    steamIDs.Append(m_options[i].name.Substring(0, m_options[i].name.IndexOf(".")));
                    steamIDs.Append(",");
                }
            }
            steamIDs.Length--;

            DebugUtils.Log(steamIDs.ToString());
        }

        private static bool IsAICustom(VehicleAI ai)
        {
            Type type = ai.GetType();
            return (type != typeof(AmbulanceAI) ||
                type != typeof(BusAI) ||
                type != typeof(CargoTruckAI) ||
                type != typeof(FireTruckAI) ||
                type != typeof(GarbageTruckAI) ||
                type != typeof(HearseAI) ||
                type != typeof(PassengerCarAI) ||
                type != typeof(PoliceCarAI));
        }

        private IEnumerator BrokenVehicleFix(ThreadBase t)
        {
            // Fix broken vehicles ?
            int count = 0;

            Array16<Vehicle> vehicles = Singleton<VehicleManager>.instance.m_vehicles;
            for (int i = 0; i < vehicles.m_size; i++)
            {
                if (count < 0) break;
                if (vehicles.m_buffer[i].Info == null)
                {
                    try
                    {
                        Singleton<VehicleManager>.instance.ReleaseVehicle((ushort)i);
                        count++;
                    }
                    catch { }
                }
                if (i % 256 == 255) yield return i;
            }

            Array16<VehicleParked> vehiclesParked = Singleton<VehicleManager>.instance.m_parkedVehicles;
            for (int i = 0; i < vehiclesParked.m_size; i++)
            {
                if (count < 0) break;
                if (vehiclesParked.m_buffer[i].Info == null)
                {
                    try
                    {
                        Singleton<VehicleManager>.instance.ReleaseParkedVehicle((ushort)i);
                        count++;
                    }
                    catch { }
                }
                if (i % 256 == 255) yield return i;
            }

            if (count > 0) DebugUtils.Message(count + " broken vehicle(s) detected and removed.");
        }
    }
}
