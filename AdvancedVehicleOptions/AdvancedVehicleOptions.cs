using ICities;
using UnityEngine;

using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

using ColossalFramework;
using ColossalFramework.Threading;
using ColossalFramework.UI;
using ColossalFramework.Globalization;

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

        public void OnSettingsUI(UIHelperBase helper)
        {
            try
            {
                AdvancedVehicleOptions.LoadConfig();

                UICheckBox checkBox;
                UIHelperBase group = helper.AddGroup(Name);

                checkBox = (UICheckBox)group.AddCheckbox("Hide the user interface", AdvancedVehicleOptions.config.hideGUI, (b) =>
                {
                    if (AdvancedVehicleOptions.config.hideGUI != b)
                    {
                        AdvancedVehicleOptions.hideGUI = b;
                        AdvancedVehicleOptions.SaveConfig();
                    }
                });
                checkBox.tooltip = "Hide the UI completely if you feel like you are done with it\nand want to save the little bit of memory it takes\nEverything else will still be functional";

                checkBox = (UICheckBox)group.AddCheckbox("Disable warning at map loading", !AdvancedVehicleOptions.config.onLoadCheck, (b) =>
                {
                    if (AdvancedVehicleOptions.config.onLoadCheck == b)
                    {
                        AdvancedVehicleOptions.config.onLoadCheck = !b;
                        AdvancedVehicleOptions.SaveConfig();
                    }
                });
                checkBox.tooltip = "Disable service vehicle availability check at the loading of a map";
            }
            catch (Exception e)
            {
                DebugUtils.Log("OnSettingsUI failed");
                DebugUtils.LogException(e);
            }
        }

        public const string version = "1.7.4";
    }

    public class AdvancedVehicleOptionsLoader : LoadingExtensionBase
    {
        private static AdvancedVehicleOptions instance;

        #region LoadingExtensionBase overrides
        public override void OnCreated(ILoading loading)
        {
            try
            {
                // Storing default values ASAP (before any mods have the time to change values)
                DefaultOptions.StoreAll();

                // Creating a backup
                AdvancedVehicleOptions.SaveBackup();
            }
            catch (Exception e)
            {
                DebugUtils.LogException(e);
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

                AdvancedVehicleOptions.isGameLoaded = true;

                if (instance != null)
                {
                    GameObject.DestroyImmediate(instance);
                }

                instance = new GameObject("AdvancedVehicleOptions").AddComponent<AdvancedVehicleOptions>();

                try
                {
                    DefaultOptions.BuildVehicleInfoDictionary();
                    VehicleOptions.Clear();
                    DebugUtils.Log("UIMainPanel created");
                }
                catch
                {
                    DebugUtils.Log("Could not create UIMainPanel");

                    if (instance != null)
                        GameObject.Destroy(instance);

                    return;
                }

                //new EnumerableActionThread(BrokenAssetsFix);
            }
            catch (Exception e)
            {
                if (instance != null)
                    GameObject.Destroy(instance);
                DebugUtils.LogException(e);
            }
        }

        /// <summary>
        /// Called when the level is unloaded
        /// </summary>
        public override void OnLevelUnloading()
        {
            try
            {
                //SaveConfig();

                if (instance != null)
                    GameObject.Destroy(instance);

                AdvancedVehicleOptions.isGameLoaded = false;
            }
            catch (Exception e)
            {
                DebugUtils.LogException(e);
            }
        }

        public override void OnReleased()
        {
            try
            {
                DebugUtils.Log("Restoring default values");
                DefaultOptions.RestoreAll();
                DefaultOptions.Clear();
            }
            catch (Exception e)
            {
                DebugUtils.LogException(e);
            }
        }
        #endregion
    }
    
    public class AdvancedVehicleOptions : MonoBehaviour
    {
        private static GUI.UIMainPanel m_mainPanel;

        private static VehicleInfo m_removeInfo;
        private static VehicleInfo m_removeParkedInfo;

        private const string m_fileName = "AdvancedVehicleOptions.xml";

        public static bool isGameLoaded = false;
        public static Configuration config = new Configuration();

        public void Start()
        {
            try
            {
                // Loading config
                AdvancedVehicleOptions.LoadConfig();
                AdvancedVehicleOptions.CheckAllServicesValidity();

                m_mainPanel = GameObject.FindObjectOfType<GUI.UIMainPanel>();
                if (m_mainPanel == null && !hideGUI)
                {
                    m_mainPanel = UIView.GetAView().AddUIComponent(typeof(GUI.UIMainPanel)) as GUI.UIMainPanel;
                }
            }
            catch (Exception e)
            {
                DebugUtils.Log("UI initialization failed.");
                DebugUtils.LogException(e);

                GameObject.Destroy(gameObject);
            }
        }

        public static bool hideGUI
        {
            get { return config.hideGUI; }

            set
            {
                if(config.hideGUI != value)
                {
                    config.hideGUI = value;
                    if(!value && isGameLoaded)
                    {
                        // Creating GUI
                        m_mainPanel = UIView.GetAView().AddUIComponent(typeof(GUI.UIMainPanel)) as GUI.UIMainPanel;
                    }
                    else if (value && isGameLoaded)
                    {
                        GameObject.Destroy(m_mainPanel);
                    }
                    AdvancedVehicleOptions.SaveConfig();
                }
            }
        }

        public static void RestoreBackup()
        {
            if (!File.Exists(m_fileName + ".bak")) return;

            File.Copy(m_fileName + ".bak", m_fileName, true);
            DebugUtils.Log("Backup configuration file restored");
        }

        public static void SaveBackup()
        {
            if (!File.Exists(m_fileName)) return;

            File.Copy(m_fileName, m_fileName + ".bak", true);
            DebugUtils.Log("Backup configuration file created");
        }

        /// <summary>
        /// Load and apply the configuration file
        /// </summary>
        public static void LoadConfig()
        {
            if(!isGameLoaded)
            {
                if (File.Exists(m_fileName)) config.Deserialize(m_fileName);
                return;
            }

            // Store modded values
            DefaultOptions.StoreAllModded();

            if (!File.Exists(m_fileName))
            {
                DebugUtils.Log("Configuration file not found. Creating new configuration file.");

                CreateConfig();

                return;
            }

            config.Deserialize(m_fileName);

            if (config.options == null)
            {
                DebugUtils.Log("Configuration empty. Default values will be used.");
            }
            else
            {
                // Remove unneeded options
                List<VehicleOptions> optionsList = new List<VehicleOptions>();

                for (uint i = 0; i < config.options.Length; i++)
                {
                    if (config.options[i] != null && config.options[i].prefab != null) optionsList.Add(config.options[i]);
                }

                config.options = optionsList.ToArray();
            }

            // Checking for new vehicles
            CompileVehiclesList();

            // Checking for conflicts
            DefaultOptions.CheckForConflicts();

            // Update existing vehicles
            new EnumerableActionThread(VehicleOptions.UpdateCapacityUnits);
            new EnumerableActionThread(VehicleOptions.UpdateBackEngines);
            
            DebugUtils.Log("Configuration loaded");
            LogVehicleListSteamID();
        }

        /// <summary>
        /// Save the configuration file
        /// </summary>
        public static void SaveConfig()
        {
            config.version = ModInfo.version;
            config.Serialize(m_fileName);
        }

        public static void CheckAllServicesValidity()
        {
            if (config == null || !config.onLoadCheck) return;

            string warning = "";

            for (int i = 0; i <= (int)VehicleOptions.Category.TransportPlane; i++)
                if (!CheckServiceValidity((VehicleOptions.Category)i)) warning += "- " + GUI.UIMainPanel.categoryList[i + 1] + "\n";

            if(warning != "")
            {
                GUI.UIWarningModal.instance.message = "The following services may not work correctly because no vehicles are allowed to spawn :\n\n" + warning;
                UIView.PushModal(GUI.UIWarningModal.instance);
                GUI.UIWarningModal.instance.Show(true);
            }

        }

        public static bool CheckServiceValidity(VehicleOptions.Category service)
        {
            if (config == null || config.options == null) return true;

            int count = 0;

            for (int i = 0; i < config.options.Length; i++)
            {
                if (config.options[i].category == service)
                {
                    if(config.options[i].enabled) return true;
                    count++;
                }
            }

            return count == 0;
        }

        public static void ClearVehicles(VehicleOptions options, bool parked)
        {
            if (parked)
            {
                if(options == null)
                {
                    new EnumerableActionThread(ActionRemoveParkedAll);
                    return;
                }
                
                m_removeParkedInfo = options.prefab;
                new EnumerableActionThread(ActionRemoveParked);
            }
            else
            {
                if (options == null)
                {
                    new EnumerableActionThread(ActionRemoveExistingAll);
                    return;
                }

                m_removeInfo = options.prefab;
                new EnumerableActionThread(ActionRemoveExisting);
            }
        }

        public static IEnumerator ActionRemoveExisting(ThreadBase t)
        {
            VehicleInfo info = m_removeInfo;

            for (ushort i = 0; i < VehicleManager.instance.m_vehicles.m_size; i++)
            {
                if (VehicleManager.instance.m_vehicles.m_buffer[i].Info != null)
                {
                    if (info == VehicleManager.instance.m_vehicles.m_buffer[i].Info)
                        VehicleManager.instance.ReleaseVehicle(i);
                }

                if (i % 256 == 255) yield return i;
            }
        }

        public static IEnumerator ActionRemoveParked(ThreadBase t)
        {
            VehicleInfo info = m_removeParkedInfo;

            for (ushort i = 0; i < VehicleManager.instance.m_parkedVehicles.m_size; i++)
            {
                if (VehicleManager.instance.m_parkedVehicles.m_buffer[i].Info != null)
                {
                    if (info == VehicleManager.instance.m_parkedVehicles.m_buffer[i].Info)
                        VehicleManager.instance.ReleaseParkedVehicle(i);
                }

                if (i % 256 == 255) yield return i;
            }
        }

        public static IEnumerator ActionRemoveExistingAll(ThreadBase t)
        {
            for (ushort i = 0; i < VehicleManager.instance.m_vehicles.m_size; i++)
            {
                VehicleManager.instance.ReleaseVehicle(i);
                if (i % 256 == 255) yield return i;
            }
        }

        public static IEnumerator ActionRemoveParkedAll(ThreadBase t)
        {
            for (ushort i = 0; i < VehicleManager.instance.m_parkedVehicles.m_size; i++)
            {
                VehicleManager.instance.ReleaseParkedVehicle(i);
                if (i % 256 == 255) yield return i;
            }
        }

        private static int ParseVersion(string version)
        {
            if (version.IsNullOrWhiteSpace()) return 0;

            int v = 0;
            string[] t = version.Split('.');

            for (int i = 0; i < t.Length; i++)
            {
                v *= 100;
                int a = 0;
                if(int.TryParse(t[i], out a))
                    v += a;
            }

            return v;
        }

        private static void CreateConfig()
        {
            CompileVehiclesList();

            // Loading old mods saves
            List<OldMods.Vehicle> removerList = OldMods.VehicleRemoverMod.LoadConfig();
            List<OldMods.VehicleColorInfo> colorList = OldMods.VehicleColorChangerMod.LoadConfig();

            if (removerList != null || colorList != null)
            {
                for (int i = 0; i < config.options.Length; i++)
                {
                    if (removerList != null)
                    {
                        OldMods.Vehicle vehicle = removerList.Find((v) => { return v.name == config.options[i].name; });
                        if (vehicle.name == config.options[i].name) config.options[i].enabled = vehicle.enabled;
                    }

                    if (colorList != null)
                    {
                        OldMods.VehicleColorInfo vehicle = colorList.Find((v) => { return v.name == config.options[i].name; });
                        if (vehicle != null && vehicle.name == config.options[i].name)
                        {
                            config.options[i].color0 = vehicle.color0;
                            config.options[i].color1 = vehicle.color1;
                            config.options[i].color2 = vehicle.color2;
                            config.options[i].color3 = vehicle.color3;
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
            if (config.options != null) optionsList.AddRange(config.options);

            for (uint i = 0; i < PrefabCollection<VehicleInfo>.PrefabCount(); i++)
            {
                VehicleInfo prefab = PrefabCollection<VehicleInfo>.GetPrefab(i);

                if (prefab == null || ContainsPrefab(prefab)) continue;

                // New vehicle
                VehicleOptions options = new VehicleOptions();
                options.SetPrefab(prefab);

                optionsList.Add(options);
            }

            if (config.options != null)
                DebugUtils.Log("Found " + (optionsList.Count - config.options.Length) + " new vehicle(s)");
            else
                DebugUtils.Log("Found " + optionsList.Count + " new vehicle(s)");

            config.options = optionsList.ToArray();

        }

        private static bool ContainsPrefab(VehicleInfo prefab)
        {
            if (config.options == null) return false;
            for (int i = 0; i < config.options.Length; i++)
            {
                if (config.options[i].prefab == prefab) return true;
            }
            return false;
        }

        private static void LogVehicleListSteamID()
        {
            StringBuilder steamIDs = new StringBuilder("Vehicle Steam IDs : ");

            for (int i = 0; i < config.options.Length; i++)
            {
                if (config.options[i] != null && config.options[i].name.Contains("."))
                {
                    steamIDs.Append(config.options[i].name.Substring(0, config.options[i].name.IndexOf(".")));
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
    }
}
