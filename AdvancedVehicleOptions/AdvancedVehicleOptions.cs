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

        public const string version = "1.7.1";
    }
    
    public class AdvancedVehicleOptions : LoadingExtensionBase
    {
        private static GameObject m_gameObject;
        private static GUI.UIMainPanel m_mainPanel;

        private static VehicleInfo m_removeInfo;
        private static VehicleInfo m_removeParkedInfo;

        private const string m_fileName = "AdvancedVehicleOptions.xml";

        public static bool isGameLoaded = false;
        public static Configuration config = new Configuration();

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
                        UIView view = UIView.GetAView();
                        m_gameObject = new GameObject("AdvancedVehicleOptions");
                        m_gameObject.transform.SetParent(view.transform);

                        m_mainPanel = m_gameObject.AddComponent<GUI.UIMainPanel>();
                    }
                    else if (value && isGameLoaded)
                    {
                        GameObject.Destroy(m_gameObject);
                    }
                    AdvancedVehicleOptions.SaveConfig();
                }
            }
        }
                
        #region LoadingExtensionBase overrides
        public override void OnCreated(ILoading loading)
        {
            try
            {
                // Storing default values ASAP (before any mods have the time to change values)
                DefaultOptions.StoreAll();

                // Creating a backup
                SaveBackup();
            }
            catch(Exception e)
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

                isGameLoaded = true;

                // Creating GUI
                UIView view = UIView.GetAView();
                m_gameObject = new GameObject("AdvancedVehicleOptions");
                m_gameObject.transform.SetParent(view.transform);

                try
                {
                    DefaultOptions.BuildVehicleInfoDictionary();
                    VehicleOptions.Clear();
                    m_mainPanel = m_gameObject.AddComponent<GUI.UIMainPanel>();
                    DebugUtils.Log("UIMainPanel created");
                }
                catch
                {
                    DebugUtils.Log("Could not create UIMainPanel");

                    if (m_gameObject != null)
                        GameObject.Destroy(m_gameObject);

                    return;
                }

                //new EnumerableActionThread(BrokenAssetsFix);
            }
            catch (Exception e)
            {
                if (m_gameObject != null)
                    GameObject.Destroy(m_gameObject);
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

                if (m_gameObject != null)
                    GameObject.Destroy(m_gameObject);

                isGameLoaded = false;
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

        /*private IEnumerator BrokenAssetsFix(ThreadBase t)
        {
            SimulationManager.instance.ForcedSimulationPaused = true;

            try
            {
                uint brokenCount = 0;
                uint confusedCount = 0;

                // Fix broken offers
                TransferManager.TransferOffer[] incomingOffers = typeof(TransferManager).GetField("m_incomingOffers", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(TransferManager.instance) as TransferManager.TransferOffer[];
                TransferManager.TransferOffer[] outgoingOffers = typeof(TransferManager).GetField("m_outgoingOffers", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(TransferManager.instance) as TransferManager.TransferOffer[];

                ushort[] incomingCount = typeof(TransferManager).GetField("m_incomingCount", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(TransferManager.instance) as ushort[];
                ushort[] outgoingCount = typeof(TransferManager).GetField("m_outgoingCount", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(TransferManager.instance) as ushort[];

                int[] incomingAmount = typeof(TransferManager).GetField("m_incomingAmount", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(TransferManager.instance) as int[];
                int[] outgoingAmount = typeof(TransferManager).GetField("m_outgoingAmount", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(TransferManager.instance) as int[];

                // Based on TransferManager.RemoveAllOffers
                for (int i = 0; i < 64; i++)
                {
                    for (int j = 0; j < 8; j++)
                    {
                        int num = i * 8 + j;
                        int num2 = (int)incomingCount[num];
                        for (int k = num2 - 1; k >= 0; k--)
                        {
                            int num3 = num * 256 + k;
                            if (IsInfoNull(incomingOffers[num3]))
                            {
                                incomingAmount[i] -= incomingOffers[num3].Amount;
                                incomingOffers[num3] = incomingOffers[--num2];
                                brokenCount++;
                            }
                        }
                        incomingCount[num] = (ushort)num2;
                        int num4 = (int)outgoingCount[num];
                        for (int l = num4 - 1; l >= 0; l--)
                        {
                            int num5 = num * 256 + l;
                            if (IsInfoNull(outgoingOffers[num5]))
                            {
                                outgoingAmount[i] -= outgoingOffers[num5].Amount;
                                outgoingOffers[num5] = outgoingOffers[--num4];
                                brokenCount++;
                            }
                        }
                        outgoingCount[num] = (ushort)num4;
                    }

                    yield return i;
                }

                if (brokenCount > 0) DebugUtils.Log("Removed " + brokenCount + " broken transfer offers.");

                // Fix broken vehicles
                Array16<Vehicle> vehicles = VehicleManager.instance.m_vehicles;
                for (int i = 0; i < vehicles.m_size; i++)
                {
                    if (vehicles.m_buffer[i].m_flags != Vehicle.Flags.None)
                    {
                        bool exists = (vehicles.m_buffer[i].m_flags & Vehicle.Flags.Spawned) != Vehicle.Flags.None;

                        // Vehicle validity
                        InstanceID target;
                        bool isInfoNull = vehicles.m_buffer[i].Info == null;
                        bool isLeading = vehicles.m_buffer[i].m_leadingVehicle == 0;
                        bool isWaiting = !exists && (vehicles.m_buffer[i].m_flags & Vehicle.Flags.WaitingSpace) != Vehicle.Flags.None;
                        bool isConfused = exists && isLeading && !isInfoNull && vehicles.m_buffer[i].Info.m_vehicleAI.GetLocalizedStatus((ushort)i, ref vehicles.m_buffer[i], out target) == Locale.Get("VEHICLE_STATUS_CONFUSED");
                        bool isSingleTrailer = false;

                        if (exists && !isInfoNull && isLeading && !isConfused && !isWaiting)
                        {
                            VehicleOptions options = new VehicleOptions();
                            options.SetPrefab(vehicles.m_buffer[i].Info);
                            isSingleTrailer = options.isTrailer && vehicles.m_buffer[i].m_trailingVehicle == 0;
                        }

                        if (isInfoNull || isSingleTrailer || isWaiting || isConfused)
                        {
                            try
                            {
                                VehicleManager.instance.ReleaseVehicle((ushort)i);
                                if (isInfoNull) brokenCount++;
                                if (isConfused) confusedCount++;
                            }
                            catch { }
                        }
                    }
                    if (i % 256 == 255) yield return i;
                }

                if (confusedCount > 0) DebugUtils.Log("Removed " + confusedCount + " confused vehicle instances.");

                Array16<VehicleParked> vehiclesParked = VehicleManager.instance.m_parkedVehicles;
                for (int i = 0; i < vehiclesParked.m_size; i++)
                {
                    if (vehiclesParked.m_buffer[i].Info == null)
                    {
                        try
                        {
                            VehicleManager.instance.ReleaseParkedVehicle((ushort)i);
                            brokenCount++;
                        }
                        catch { }
                    }
                    if (i % 256 == 255) yield return i;
                }

                if (brokenCount > 0) DebugUtils.Log("Removed " + brokenCount + " broken vehicle instances.");
                brokenCount = 0;

                // Fix broken buildings
                Array16<Building> buildings = BuildingManager.instance.m_buildings;
                for (int i = 0; i < buildings.m_size; i++)
                {
                    if (buildings.m_buffer[i].Info == null)
                    {
                        try
                        {
                            BuildingManager.instance.ReleaseBuilding((ushort)i);
                            brokenCount++;
                        }
                        catch { }
                    }
                    if (i % 256 == 255) yield return i;
                }

                if (brokenCount > 0) DebugUtils.Log("Removed " + brokenCount + " broken building instances.");
                brokenCount = 0;

                // Fix broken props
                Array16<PropInstance> props = PropManager.instance.m_props;
                for (int i = 0; i < props.m_size; i++)
                {
                    if (props.m_buffer[i].Info == null)
                    {
                        try
                        {
                            PropManager.instance.ReleaseProp((ushort)i);
                            brokenCount++;
                        }
                        catch { }
                    }
                    if (i % 256 == 255) yield return i;
                }

                if (brokenCount > 0) DebugUtils.Log("Removed " + brokenCount + " broken prop instances.");
                brokenCount = 0;

                // Fix broken trees
                Array32<TreeInstance> trees = TreeManager.instance.m_trees;
                for (int i = 0; i < trees.m_size; i++)
                {
                    if (trees.m_buffer[i].Info == null)
                    {
                        try
                        {
                            TreeManager.instance.ReleaseTree((ushort)i);
                            brokenCount++;
                        }
                        catch { }
                    }
                    if (i % 256 == 255) yield return i;
                }

                if (brokenCount > 0) DebugUtils.Log("Removed " + brokenCount + " broken tree instances.");
                brokenCount = 0;
            }
            finally
            {
                SimulationManager.instance.ForcedSimulationPaused = false;
            }
        }

        private static bool IsInfoNull(TransferManager.TransferOffer offer)
        {
            if (!offer.Active) return false;

            if (offer.Vehicle != 0)
                return VehicleManager.instance.m_vehicles.m_buffer[offer.Vehicle].Info == null;
            
            if (offer.Building != 0)
                return BuildingManager.instance.m_buildings.m_buffer[offer.Building].Info == null;

            return false;
        }*/
    }
}
