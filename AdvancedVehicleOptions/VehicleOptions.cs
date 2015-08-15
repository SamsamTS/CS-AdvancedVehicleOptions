using UnityEngine;
using ColossalFramework;
using ColossalFramework.Threading;
using ColossalFramework.Globalization;

using System;
using System.Text;
using System.Xml.Serialization;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace AdvancedVehicleOptions
{
    public class VehicleOptions : IComparable
    {
        public enum Category
        {
            None = -1,
            Citizen,
            Forestry,
            Farming,
            Ore,
            Oil,
            IndustryGeneric,
            Police,
            FireSafety,
            Healthcare,
            Deathcare,
            Garbage,
            TransportBus,
            TransportMetro,
            CargoTrain,
            TransportTrain,
            CargoShip,
            TransportShip,
            TransportPlane
        }

        #region serialized
        [XmlAttribute("name")]
        public string name
        {
            get { return m_prefab.name; }
            set
            {
                VehicleInfo prefab = PrefabCollection<VehicleInfo>.FindLoaded(value);
                if (prefab == null)
                    DebugUtils.Log("Couldn't find " + value);
                else
                    SetPrefab(prefab);
            }
        }
        // enabled
        public bool enabled
        {
            get { return m_prefab.m_placementStyle != ItemClass.Placement.Manual; }
            set
            {
                if (m_prefab == null) return;

                if (value)
                {
                    ItemClass.Placement placement = DefaultOptions.GetPlacementStyle(m_prefab);

                    m_prefab.m_placementStyle = (int)placement != -1 ? placement : m_placementStyle;
                }
                else
                    m_prefab.m_placementStyle = ItemClass.Placement.Manual;
                // Make transfer vehicles dirty
                m_transferVehiclesDirty.SetValue(Singleton<VehicleManager>.instance, true);
            }
        }
        // addBackEngine
        public bool addBackEngine
        {
            get
            {
                if (!hasTrailer) return false;
                return m_prefab.m_trailers[m_prefab.m_trailers.Length - 1].m_info == m_prefab;
            }
            set
            {
                if (m_prefab == null || !hasTrailer || m_prefab.m_vehicleType != VehicleInfo.VehicleType.Train) return;

                VehicleInfo newTrailer = value ? m_prefab : DefaultOptions.GetLastTrailer(m_prefab);
                int last = m_prefab.m_trailers.Length - 1;

                if (m_prefab.m_trailers[last].m_info == newTrailer) return;

                m_prefab.m_trailers[last].m_info = newTrailer;

                if (value)
                    m_prefab.m_trailers[last].m_invertProbability = m_prefab.m_trailers[last].m_probability;
                else
                    m_prefab.m_trailers[last].m_invertProbability = DefaultOptions.GetProbability(prefab);
            }
        }
        // maxSpeed
        public float maxSpeed
        {
            get { return m_prefab.m_maxSpeed; }
            set
            {
                if (m_prefab == null) return;
                m_prefab.m_maxSpeed = value;
            }
        }
        // acceleration
        public float acceleration
        {
            get { return m_prefab.m_acceleration; }
            set
            {
                if (m_prefab == null) return;
                m_prefab.m_acceleration = value;
            }
        }
        // braking
        public float braking
        {
            get { return m_prefab.m_braking; }
            set
            {
                if (m_prefab == null) return;
                m_prefab.m_braking = value;
            }
        }
        // useColorVariations
        public bool useColorVariations
        {
            get { return m_prefab.m_useColorVariations; }
            set
            {
                if (m_prefab == null) return;
                m_prefab.m_useColorVariations = value;
            }
        }
        // colors
        public HexaColor color0
        {
            get { return m_prefab.m_color0; }
            set
            {
                if (m_prefab == null) return;
                m_prefab.m_color0 = value;
            }
        }
        public HexaColor color1
        {
            get { return m_prefab.m_color1; }
            set
            {
                if (m_prefab == null) return;
                m_prefab.m_color1 = value;
            }
        }
        public HexaColor color2
        {
            get { return m_prefab.m_color2; }
            set
            {
                if (m_prefab == null) return;
                m_prefab.m_color2 = value;
            }
        }
        public HexaColor color3
        {
            get { return m_prefab.m_color3; }
            set
            {
                if (m_prefab == null) return;
                m_prefab.m_color3 = value;
            }
        }
        // capacity
        [DefaultValue(-1)]
        public int capacity
        {
            get
            {
                VehicleAI ai;

                ai = m_vehicleAI as AmbulanceAI;
                if (ai != null) return ((AmbulanceAI)ai).m_patientCapacity;

                ai = m_vehicleAI as BusAI;
                if (ai != null) return ((BusAI)ai).m_passengerCapacity;

                ai = m_vehicleAI as CargoShipAI;
                if (ai != null) return ((CargoShipAI)ai).m_cargoCapacity;

                ai = m_vehicleAI as CargoTrainAI;
                if (ai != null) return ((CargoTrainAI)ai).m_cargoCapacity;

                ai = m_vehicleAI as CargoTruckAI;
                if (ai != null) return ((CargoTruckAI)ai).m_cargoCapacity;

                ai = m_vehicleAI as GarbageTruckAI;
                if (ai != null) return ((GarbageTruckAI)ai).m_cargoCapacity;

                ai = m_vehicleAI as FireTruckAI;
                if (ai != null) return ((FireTruckAI)ai).m_fireFightingRate;

                ai = m_vehicleAI as HearseAI;
                if (ai != null) return ((HearseAI)ai).m_corpseCapacity;

                ai = m_vehicleAI as PassengerPlaneAI;
                if (ai != null) return ((PassengerPlaneAI)ai).m_passengerCapacity;

                ai = m_vehicleAI as PassengerShipAI;
                if (ai != null) return ((PassengerShipAI)ai).m_passengerCapacity;

                ai = m_vehicleAI as PassengerTrainAI;
                if (ai != null) return ((PassengerTrainAI)ai).m_passengerCapacity;

                ai = m_vehicleAI as PoliceCarAI;
                if (ai != null) return ((PoliceCarAI)ai).m_crimeCapacity;

                return -1;
            }
            set
            {
                if (m_prefab == null || capacity == -1 || value <= 0) return;

                VehicleAI ai;

                ai = m_vehicleAI as AmbulanceAI;
                if (ai != null) { ((AmbulanceAI)ai).m_patientCapacity = value; return; }

                ai = m_vehicleAI as BusAI;
                if (ai != null) { ((BusAI)ai).m_passengerCapacity = value; return; }

                ai = m_vehicleAI as CargoShipAI;
                if (ai != null) { ((CargoShipAI)ai).m_cargoCapacity = value; return; }

                ai = m_vehicleAI as CargoTrainAI;
                if (ai != null) { ((CargoTrainAI)ai).m_cargoCapacity = value; return; }

                ai = m_vehicleAI as CargoTruckAI;
                if (ai != null) { ((CargoTruckAI)ai).m_cargoCapacity = value; return; }

                ai = m_vehicleAI as GarbageTruckAI;
                if (ai != null) { ((GarbageTruckAI)ai).m_cargoCapacity = value; return; }

                ai = m_vehicleAI as FireTruckAI;
                if (ai != null) { ((FireTruckAI)ai).m_fireFightingRate = value; return; }

                ai = m_vehicleAI as HearseAI;
                if (ai != null) { ((HearseAI)ai).m_corpseCapacity = value; return; }

                ai = m_vehicleAI as PassengerPlaneAI;
                if (ai != null) { ((PassengerPlaneAI)ai).m_passengerCapacity = value; return; }

                ai = m_vehicleAI as PassengerShipAI;
                if (ai != null) { ((PassengerShipAI)ai).m_passengerCapacity = value; return; }

                ai = m_vehicleAI as PassengerTrainAI;
                if (ai != null) { ((PassengerTrainAI)ai).m_passengerCapacity = value; return; }

                ai = m_vehicleAI as PoliceCarAI;
                if (ai != null) { ((PoliceCarAI)ai).m_crimeCapacity = value; return; }
            }
        }
        #endregion

        public static VehicleInfo prefabUpdateUnits = null;
        public static VehicleInfo prefabUpdateEngine = null;
        private static FieldInfo m_transferVehiclesDirty = typeof(VehicleManager).GetField("m_transferVehiclesDirty", BindingFlags.Instance | BindingFlags.NonPublic);

        private VehicleInfo m_prefab = null;
        private VehicleAI m_vehicleAI = null;
        private Category m_category = Category.None;
        private ItemClass.Placement m_placementStyle;
        private string m_localizedName;
        private bool m_isTrailer = false;
        private bool m_hasCapacity = false;

        public VehicleInfo prefab
        {
            get { return m_prefab; }
        }

        public ItemClass.Placement placementStyle
        {
            get { return m_placementStyle; }
        }

        public bool hasCapacity
        {
            get { return m_hasCapacity; }
        }

        public bool hasTrailer
        {
            get { return m_prefab.m_trailers != null && m_prefab.m_trailers.Length > 0; }
        }

        public bool isTrailer
        {
            get { return m_isTrailer; }
        }

        public string localizedName
        {
            get { return m_localizedName; }
        }

        public Category category
        {
            get
            {
                if (m_category == Category.None)
                    m_category = GetCategory(m_prefab);
                return m_category;
            }
        }

        private static int GetUnitsCapacity(VehicleAI vehicleAI)
        {
            VehicleAI ai;

            ai = vehicleAI as AmbulanceAI;
            if (ai != null) return ((AmbulanceAI)ai).m_patientCapacity + ((AmbulanceAI)ai).m_paramedicCount;

            ai = vehicleAI as BusAI;
            if (ai != null) return ((BusAI)ai).m_passengerCapacity;

            /*ai = prefab.m_vehicleAI as FireTruckAI;
            if (ai != null) return ((FireTruckAI)ai).m_firemanCount;*/

            ai = vehicleAI as HearseAI;
            if (ai != null) return ((HearseAI)ai).m_corpseCapacity + ((HearseAI)ai).m_driverCount;

            ai = vehicleAI as PassengerPlaneAI;
            if (ai != null) return ((PassengerPlaneAI)ai).m_passengerCapacity;

            ai = vehicleAI as PassengerShipAI;
            if (ai != null) return ((PassengerShipAI)ai).m_passengerCapacity;

            ai = vehicleAI as PassengerTrainAI;
            if (ai != null) return ((PassengerTrainAI)ai).m_passengerCapacity;

            /*ai = prefab.m_vehicleAI as PoliceCarAI;
            if (ai != null) return ((PoliceCarAI)ai).m_policeCount;*/

            return -1;
        }

        private static int GetTotalUnitGroups(uint unitID)
        {
            int count = 0;
            while (unitID != 0)
            {
                CitizenUnit unit = Singleton<CitizenManager>.instance.m_units.m_buffer[unitID];
                unitID = unit.m_nextUnit;
                count++;
            }
            return count;
        }

        public static IEnumerator UpdateCapacityUnits(ThreadBase t)
        {
            int count = 0;
            Array16<Vehicle> vehicles = Singleton<VehicleManager>.instance.m_vehicles;
            for (int i = 0; i < vehicles.m_size; i++)
            {
                if (prefabUpdateUnits == null || vehicles.m_buffer[i].Info == prefabUpdateUnits)
                {
                    int capacity = GetUnitsCapacity(vehicles.m_buffer[i].Info.m_vehicleAI);

                    if (capacity != -1)
                    {
                        CitizenUnit[] units = Singleton<CitizenManager>.instance.m_units.m_buffer;
                        uint unit = vehicles.m_buffer[i].m_citizenUnits;

                        int currentUnitCount = GetTotalUnitGroups(unit);
                        int newUnitCount = Mathf.CeilToInt(capacity / 5f);

                        // Capacity reduced
                        if (newUnitCount < currentUnitCount)
                        {
                            // Get the first unit to remove
                            uint n = unit;
                            for (int j = 1; j < newUnitCount; j++)
                                n = units[n].m_nextUnit;
                            // Releasing units excess
                            Singleton<CitizenManager>.instance.ReleaseUnits(units[n].m_nextUnit);
                            units[n].m_nextUnit = 0;

                            count++;
                        }
                        // Capacity increased
                        else if (newUnitCount > currentUnitCount)
                        {
                            // Get the last unit
                            uint n = unit;
                            while (units[n].m_nextUnit != 0)
                                n = units[n].m_nextUnit;

                            // Creating missing units
                            int newCapacity = capacity - currentUnitCount * 5;
                            Singleton<CitizenManager>.instance.CreateUnits(out units[n].m_nextUnit, ref Singleton<SimulationManager>.instance.m_randomizer, 0, (ushort)i, 0, 0, 0, newCapacity, 0);

                            count++;
                        }
                    }
                }
                if (i % 256 == 255) yield return null;
            }
            prefabUpdateUnits = null;

            DebugUtils.Log("Modified capacity of " + count + " vehicle(s)");
        }

        public static IEnumerator UpdateBackEngines(ThreadBase t)
        {
            Array16<Vehicle> vehicles = Singleton<VehicleManager>.instance.m_vehicles;
            for (ushort i = 0; i < vehicles.m_size; i++)
            {
                VehicleInfo prefab = vehicles.m_buffer[i].Info;
                bool isTrain = prefab.m_vehicleType == VehicleInfo.VehicleType.Train;
                bool isLeading = vehicles.m_buffer[i].m_leadingVehicle == 0 && prefab.m_trailers != null && prefab.m_trailers.Length > 0;
                if ((prefabUpdateEngine == null || prefab == prefabUpdateEngine) && isTrain && isLeading)
                {
                    ushort last = vehicles.m_buffer[i].GetLastVehicle((ushort)i);
                    ushort oldPrefabID = vehicles.m_buffer[last].m_infoIndex;
                    ushort newPrefabID = (ushort)prefab.m_trailers[prefab.m_trailers.Length - 1].m_info.m_prefabDataIndex;
                    if (oldPrefabID != newPrefabID)
                    {
                        vehicles.m_buffer[last].m_infoIndex = newPrefabID;
                        vehicles.m_buffer[last].m_flags = vehicles.m_buffer[vehicles.m_buffer[last].m_leadingVehicle].m_flags;

                        if (prefab.m_trailers[prefab.m_trailers.Length - 1].m_info == prefab)
                            vehicles.m_buffer[last].m_flags |= Vehicle.Flags.Inverted;
                    }
                }
                if (i % 256 == 255) yield return null;
            }
            prefabUpdateEngine = null;
        }

        public void SetPrefab(VehicleInfo prefab)
        {
            if (prefab == null) return;

            m_prefab = prefab;
            m_vehicleAI = prefab.m_vehicleAI;
            m_placementStyle = prefab.m_placementStyle;

            VehicleInfo engine = GetEngine();
            if (engine != null)
            {
                m_localizedName = Locale.GetUnchecked("VEHICLE_TITLE", engine.name) + " (Trailer)";
                m_isTrailer = true;
                m_category = GetCategory(engine);
            }
            else
            {
                m_localizedName = Locale.GetUnchecked("VEHICLE_TITLE", prefab.name);
                if (m_localizedName.StartsWith("VEHICLE_TITLE"))
                {
                    m_localizedName = prefab.name;
                    // Removes the steam ID and trailing _Data from the name
                    m_localizedName = m_localizedName.Substring(m_localizedName.IndexOf('.') + 1).Replace("_Data", "");
                }
            }

            m_hasCapacity = capacity != -1;
        }

        public int CompareTo(object o)
        {
            if (o == null) return 1;

            VehicleOptions options = (VehicleOptions)o;

            int delta = category - options.category;
            if (delta == 0) return localizedName.CompareTo(options.localizedName);

            return delta;
        }

        private Category GetCategory(VehicleInfo prefab)
        {
            if (prefab == null) return Category.None;

            switch (prefab.m_class.m_service)
            {
                case ItemClass.Service.PoliceDepartment:
                    return Category.Police;
                case ItemClass.Service.FireDepartment:
                    return Category.FireSafety;
                case ItemClass.Service.HealthCare:
                    if (prefab.m_class.m_level == ItemClass.Level.Level1)
                        return Category.Healthcare;
                    else
                        return Category.Deathcare;
                case ItemClass.Service.Garbage:
                    return Category.Garbage;
            }

            switch (prefab.m_class.m_subService)
            {
                case ItemClass.SubService.PublicTransportBus:
                    return Category.TransportBus;
                case ItemClass.SubService.PublicTransportMetro:
                    return Category.TransportMetro;
                case ItemClass.SubService.PublicTransportTrain:
                    if (prefab.m_class.m_level == ItemClass.Level.Level1)
                        return Category.TransportTrain;
                    else
                        return Category.CargoTrain;
                case ItemClass.SubService.PublicTransportShip:
                    if (prefab.m_class.m_level == ItemClass.Level.Level1)
                        return Category.TransportShip;
                    else
                        return Category.CargoShip;
                case ItemClass.SubService.PublicTransportPlane:
                    return Category.TransportPlane;
                case ItemClass.SubService.IndustrialForestry:
                    return Category.Forestry;
                case ItemClass.SubService.IndustrialFarming:
                    return Category.Farming;
                case ItemClass.SubService.IndustrialOre:
                    return Category.Ore;
                case ItemClass.SubService.IndustrialOil:
                    return Category.Oil;
                case ItemClass.SubService.IndustrialGeneric:
                    return Category.IndustryGeneric;
            }

            return Category.Citizen;
        }

        private static Dictionary<VehicleInfo, VehicleInfo> _trailerEngines = null;
        public static void Clear()
        {
            if (_trailerEngines != null)
            {
                _trailerEngines.Clear();
                _trailerEngines = null;
            }
        }

        private VehicleInfo GetEngine()
        {
            if (_trailerEngines == null)
            {
                _trailerEngines = new Dictionary<VehicleInfo, VehicleInfo>();

                for (uint i = 0; i < PrefabCollection<VehicleInfo>.PrefabCount(); i++)
                {
                    try
                    {
                        VehicleInfo prefab = PrefabCollection<VehicleInfo>.GetPrefab(i);

                        if (prefab == null || prefab.m_trailers == null || prefab.m_trailers.Length == 0) continue;

                        for (int j = 0; j < prefab.m_trailers.Length; j++)
                        {
                            if (prefab.m_trailers[j].m_info != null && prefab.m_trailers[j].m_info != prefab && !_trailerEngines.ContainsKey(prefab.m_trailers[j].m_info))
                                _trailerEngines.Add(prefab.m_trailers[j].m_info, prefab);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
            }

            if (_trailerEngines.ContainsKey(m_prefab))
                return _trailerEngines[m_prefab];

            return null;
        }
    }

    public struct HexaColor : IXmlSerializable
    {
        private float r, g, b;

        public string Value
        {
            get
            {
                return ToString();
            }

            set
            {
                value = value.Trim().Replace("#", "");

                if (value.Length != 6) return;

                try
                {
                    r = int.Parse(value.Substring(0, 2), System.Globalization.NumberStyles.HexNumber) / 255f;
                    g = int.Parse(value.Substring(2, 2), System.Globalization.NumberStyles.HexNumber) / 255f;
                    b = int.Parse(value.Substring(4, 2), System.Globalization.NumberStyles.HexNumber) / 255f;
                }
                catch
                {
                    r = g = b = 0;
                }
            }
        }

        public HexaColor(string value)
        {
            try
            {
                r = int.Parse(value.Substring(0, 2), System.Globalization.NumberStyles.HexNumber) / 255f;
                g = int.Parse(value.Substring(2, 2), System.Globalization.NumberStyles.HexNumber) / 255f;
                b = int.Parse(value.Substring(4, 2), System.Globalization.NumberStyles.HexNumber) / 255f;
            }
            catch
            {
                r = g = b = 0;
            }
        }

        public System.Xml.Schema.XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(System.Xml.XmlReader reader)
        {
            Value = reader.ReadString();
        }

        public void WriteXml(System.Xml.XmlWriter writer)
        {
            writer.WriteString(Value);
        }

        public override string ToString()
        {
            StringBuilder s = new StringBuilder();

            s.Append(((int)(255 * r)).ToString("X2"));
            s.Append(((int)(255 * g)).ToString("X2"));
            s.Append(((int)(255 * b)).ToString("X2"));

            return s.ToString();
        }

        public static implicit operator HexaColor(Color c)
        {
            HexaColor temp = new HexaColor();

            temp.r = c.r;
            temp.g = c.g;
            temp.b = c.b;

            return temp;
        }

        public static implicit operator Color(HexaColor c)
        {
            return new Color(c.r, c.g, c.b, 1f);
        }
    }

    public class DefaultOptions
    {
        public class StoreDefault : MonoBehaviour
        {
            public void Awake()
            {
                DontDestroyOnLoad(this);
            }

            private void OnLevelWasLoaded(int level)
            {
                if (level == 6) StartCoroutine("Store");
            }

            private IEnumerator Store()
            {
                while (PrefabCollection<VehicleInfo>.GetPrefab(0) == null)
                    yield return null;

                for (uint i = 0; i < PrefabCollection<VehicleInfo>.PrefabCount(); i++)
                    DefaultOptions.Store(PrefabCollection<VehicleInfo>.GetPrefab(i));

                DebugUtils.Log("Default values stored");
                Destroy(gameObject);
            }
        }

        private static Dictionary<VehicleInfo, DefaultOptions> m_default = new Dictionary<VehicleInfo, DefaultOptions>();
        private static Dictionary<VehicleInfo, DefaultOptions> m_modded = new Dictionary<VehicleInfo, DefaultOptions>();
        private static GameObject m_gameObject;

        public static ItemClass.Placement GetPlacementStyle(VehicleInfo prefab)
        {
            if (m_default.ContainsKey(prefab))
                return m_default[prefab].m_placementStyle;
            return (ItemClass.Placement)(-1);
        }

        public static VehicleInfo GetLastTrailer(VehicleInfo prefab)
        {
            if (m_default.ContainsKey(prefab))
                return m_default[prefab].m_lastTrailer;
            return null;
        }

        public static int GetProbability(VehicleInfo prefab)
        {
            if (m_default.ContainsKey(prefab))
                return m_default[prefab].m_probability;
            return 0;
        }

        public static void Store(VehicleInfo prefab)
        {
            if (prefab != null && !m_default.ContainsKey(prefab))
            {
                m_default.Add(prefab, new DefaultOptions(prefab));
            }
        }

        public static void StoreAll()
        {
            if (m_gameObject != null) GameObject.DestroyImmediate(m_gameObject);

            m_gameObject = new GameObject("AVO-StoreDefault");
            m_gameObject.AddComponent<StoreDefault>();
        }

        public static void StoreAllModded()
        {
            if (m_modded.Count > 0) return;

            for (uint i = 0; i < PrefabCollection<VehicleInfo>.PrefabCount(); i++)
            {
                VehicleInfo prefab = PrefabCollection<VehicleInfo>.GetPrefab(i);

                if (prefab != null && !m_modded.ContainsKey(prefab))
                    m_modded.Add(prefab, new DefaultOptions(prefab));
            }
        }

        public static void CheckForConflicts()
        {
            StringBuilder conflicts = new StringBuilder();

            foreach (VehicleInfo prefab in m_default.Keys)
            {
                VehicleOptions options = new VehicleOptions();
                options.SetPrefab(prefab);

                DefaultOptions modded = m_modded[prefab];
                DefaultOptions stored = m_default[prefab];

                StringBuilder details = new StringBuilder();

                if (modded.m_enabled != stored.m_enabled && options.enabled == stored.m_enabled)
                {
                    options.enabled = modded.m_enabled;
                    details.Append("enabled, ");
                }
                if (modded.m_addBackEngine != stored.m_addBackEngine && options.addBackEngine == stored.m_addBackEngine)
                {
                    options.addBackEngine = modded.m_addBackEngine;
                    details.Append("back engine, ");
                }
                if (modded.m_maxSpeed != stored.m_maxSpeed && options.maxSpeed == stored.m_maxSpeed)
                {
                    options.maxSpeed = modded.m_maxSpeed;
                    details.Append("max speed, ");
                }
                if (modded.m_acceleration != stored.m_acceleration && options.acceleration == stored.m_acceleration)
                {
                    options.acceleration = modded.m_acceleration;
                    details.Append("acceleration, ");
                }
                if (modded.m_braking != stored.m_braking && options.braking == stored.m_braking)
                {
                    options.braking = modded.m_braking;
                    details.Append("braking, ");
                }
                if (modded.m_capacity != stored.m_capacity && options.capacity == stored.m_capacity)
                {
                    options.capacity = modded.m_capacity;
                    details.Append("capacity, ");
                }

                if (details.Length > 0)
                {
                    details.Length -= 2;
                    conflicts.AppendLine(options.name + ": " + details);
                }
            }

            if (conflicts.Length > 0)
                DebugUtils.Log("Conflicts detected (this message is harmless):" + Environment.NewLine + conflicts);
        }

        public static void Restore(VehicleInfo prefab)
        {
            if (prefab == null) return;

            VehicleOptions options = new VehicleOptions();
            options.SetPrefab(prefab);

            DefaultOptions stored = m_default[prefab];
            if (stored == null) return;

            options.enabled = stored.m_enabled;
            options.addBackEngine = stored.m_addBackEngine;
            options.maxSpeed = stored.m_maxSpeed;
            options.acceleration = stored.m_acceleration;
            options.braking = stored.m_braking;
            options.useColorVariations = stored.m_useColorVariations;
            options.color0 = stored.m_color0;
            options.color1 = stored.m_color1;
            options.color2 = stored.m_color2;
            options.color3 = stored.m_color3;
            options.capacity = stored.m_capacity;
            prefab.m_placementStyle = stored.m_placementStyle;
        }

        public static void RestoreAll()
        {
            foreach (VehicleInfo prefab in m_default.Keys)
            {
                Restore(prefab);
            }
        }

        public static void Clear()
        {
            m_default.Clear();
            m_modded.Clear();
        }

        private DefaultOptions(VehicleInfo prefab)
        {
            VehicleOptions options = new VehicleOptions();
            options.SetPrefab(prefab);

            m_enabled = options.enabled;
            m_addBackEngine = options.addBackEngine;
            m_maxSpeed = options.maxSpeed;
            m_acceleration = options.acceleration;
            m_braking = options.braking;
            m_useColorVariations = options.useColorVariations;
            m_color0 = options.color0;
            m_color1 = options.color1;
            m_color2 = options.color2;
            m_color3 = options.color3;
            m_capacity = options.capacity;
            m_placementStyle = options.placementStyle;

            if (prefab.m_trailers != null && prefab.m_trailers.Length > 0)
            {
                m_lastTrailer = prefab.m_trailers[prefab.m_trailers.Length - 1].m_info;
                m_probability = prefab.m_trailers[prefab.m_trailers.Length - 1].m_invertProbability;
            }
        }

        private bool m_enabled;
        private bool m_addBackEngine;
        private float m_maxSpeed;
        private float m_acceleration;
        private float m_braking;
        private bool m_useColorVariations;
        private HexaColor m_color0;
        private HexaColor m_color1;
        private HexaColor m_color2;
        private HexaColor m_color3;
        private int m_capacity;
        private ItemClass.Placement m_placementStyle;
        private VehicleInfo m_lastTrailer;
        private int m_probability;
    }
}
