using ICities;
using UnityEngine;

using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using ColossalFramework;
using ColossalFramework.Threading;
using ColossalFramework.UI;

namespace AdvancedVehicleOptions
{
    public class ModInfo : IUserMod
    {
        public ModInfo()
        {
            try
            {
                // Creating setting file
                GameSettings.AddSettingsFile(new SettingsFile[] { new SettingsFile() { fileName = AdvancedVehicleOptions.settingsFileName } });
            }
            catch (Exception e)
            {
                DebugUtils.Log("Could load/create the setting file.");
                DebugUtils.LogException(e);
            }
        }

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
                UICheckBox checkBox;
                UIHelperBase group = helper.AddGroup(Name);

                checkBox = (UICheckBox)group.AddCheckbox("Hide the user interface", AdvancedVehicleOptions.hideGUI.value, (b) =>
                {
                    AdvancedVehicleOptions.hideGUI.value = b;
                    AdvancedVehicleOptions.UpdateGUI();

                });
                checkBox.tooltip = "Hide the UI completely if you feel like you are done with it\nand want to save the little bit of memory it takes\nEverything else will still be functional";

                checkBox = (UICheckBox)group.AddCheckbox("Disable warning at map loading", !AdvancedVehicleOptions.onLoadCheck.value, (b) =>
                {
                    AdvancedVehicleOptions.onLoadCheck.value = !b;
                });
                checkBox.tooltip = "Disable service vehicle availability check at the loading of a map";
            }
            catch (Exception e)
            {
                DebugUtils.Log("OnSettingsUI failed");
                DebugUtils.LogException(e);
            }
        }

        public const string version = "1.8.1";
    }

    public class AdvancedVehicleOptionsLoader : LoadingExtensionBase
    {
        private static AdvancedVehicleOptions instance;

        #region LoadingExtensionBase overrides
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
                DebugUtils.Log("Restoring default values");
                DefaultOptions.RestoreAll();
                DefaultOptions.Clear();

                if (instance != null)
                    GameObject.Destroy(instance);

                AdvancedVehicleOptions.isGameLoaded = false;
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
        public const string settingsFileName = "AdvancedVehicleOptions";
 
        public static SavedBool hideGUI = new SavedBool("hideGUI", settingsFileName, false, true);
        public static SavedBool onLoadCheck = new SavedBool("onLoadCheck", settingsFileName, true, true);

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
                AdvancedVehicleOptions.InitConfig();

                if (AdvancedVehicleOptions.onLoadCheck)
                {
                    AdvancedVehicleOptions.CheckAllServicesValidity();
                }

                m_mainPanel = GameObject.FindObjectOfType<GUI.UIMainPanel>();
                UpdateGUI();
            }
            catch (Exception e)
            {
                DebugUtils.Log("UI initialization failed.");
                DebugUtils.LogException(e);

                GameObject.Destroy(gameObject);
            }
        }

        public static void UpdateGUI()
        {
            if(!isGameLoaded) return;
            
            if(!hideGUI && m_mainPanel == null)
            {
                // Creating GUI
                m_mainPanel = UIView.GetAView().AddUIComponent(typeof(GUI.UIMainPanel)) as GUI.UIMainPanel;
            }
            else if (hideGUI && m_mainPanel != null)
            {
                GameObject.Destroy(m_mainPanel);
                m_mainPanel = null;
            }
        }

        /// <summary>
        /// Init the configuration
        /// </summary>
        public static void InitConfig()
        {
            // Store modded values
            DefaultOptions.StoreAllModded();

            if(config.data != null)
            {
                config.DataToOptions();

                // Remove unneeded options
                List<VehicleOptions> optionsList = new List<VehicleOptions>();

                for (uint i = 0; i < config.options.Length; i++)
                {
                    if (config.options[i] != null && config.options[i].prefab != null) optionsList.Add(config.options[i]);
                }

                config.options = optionsList.ToArray();
            }
            else if (File.Exists(m_fileName))
            {
                // Import config
                ImportConfig();
                return;                
            }
            else
            {
                DebugUtils.Log("No configuration found. Default values will be used.");
            }

            // Checking for new vehicles
            CompileVehiclesList();

            // Checking for conflicts
            DefaultOptions.CheckForConflicts();

            // Update existing vehicles
            new EnumerableActionThread(VehicleOptions.UpdateCapacityUnits);
            new EnumerableActionThread(VehicleOptions.UpdateBackEngines);

            DebugUtils.Log("Configuration initialized");
            LogVehicleListSteamID();
        }

        /// <summary>
        /// Import the configuration file
        /// </summary>
        public static void ImportConfig()
        {
            if (!File.Exists(m_fileName))
            {
                DebugUtils.Log("Configuration file not found.");
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
            
            DebugUtils.Log("Configuration imported");
            LogVehicleListSteamID();
        }

        /// <summary>
        /// Export the configuration file
        /// </summary>
        public static void ExportConfig()
        {
            config.Serialize(m_fileName);
        }

        public static void CheckAllServicesValidity()
        {
            string warning = "";

            for (int i = 0; i < (int)VehicleOptions.Category.Natural; i++)
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
