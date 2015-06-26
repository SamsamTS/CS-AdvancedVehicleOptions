using UnityEngine;
using ColossalFramework.Globalization;

using System;
using System.Text;
using System.Linq;
using System.Xml.Serialization;

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

        [XmlAttribute("name")]
        public string name;

        public bool enabled;
        public bool addBackEngine;
        public float maxSpeed;
        public float acceleration = -1f;
        public HexaColor color0;
        public HexaColor color1;
        public HexaColor color2;
        public HexaColor color3;
        public int capacity = -1;

        private VehicleInfo m_prefab = null;
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

        public bool SetPrefab(VehicleInfo prefab)
        {
            if (prefab == null) return false;

            m_prefab = prefab;
            m_placementStyle = prefab.m_placementStyle;

            m_localizedName = Locale.GetUnchecked("VEHICLE_TITLE", prefab.name);
            if (m_localizedName.StartsWith("VEHICLE_TITLE"))
            {
                VehicleInfo engine = GetEngine();
                if (engine != null)
                {
                    m_localizedName = Locale.GetUnchecked("VEHICLE_TITLE", engine.name) + " (Trailer)";
                    m_isTrailer = true;
                    m_category = GetCategory(engine);
                }
                else
                {
                    m_localizedName = prefab.name;
                    // Removes the steam ID and trailing _Data from the name
                    m_localizedName = m_localizedName.Substring(m_localizedName.IndexOf('.') + 1).Replace("_Data", "");
                }
            }

            if (acceleration == -1f)
                acceleration = prefab.m_acceleration;

            if(capacity == -1) capacity = getCapacity();
            m_hasCapacity = (capacity != -1);

            return true;
        }

        private int getCapacity()
        {
            VehicleAI ai;

            ai = m_prefab.m_vehicleAI as AmbulanceAI;
            if (ai != null) return ((AmbulanceAI)ai).m_patientCapacity;

            ai = m_prefab.m_vehicleAI as BusAI;
            if (ai != null) return ((BusAI)ai).m_passengerCapacity;

            ai = m_prefab.m_vehicleAI as CargoShipAI;
            if (ai != null) return ((CargoShipAI)ai).m_cargoCapacity;

            ai = m_prefab.m_vehicleAI as CargoTrainAI;
            if (ai != null) return ((CargoTrainAI)ai).m_cargoCapacity;

            ai = m_prefab.m_vehicleAI as CargoTruckAI;
            if (ai != null) return ((CargoTruckAI)ai).m_cargoCapacity;

            ai = m_prefab.m_vehicleAI as GarbageTruckAI;
            if (ai != null) return ((GarbageTruckAI)ai).m_cargoCapacity;

            ai = m_prefab.m_vehicleAI as FireTruckAI;
            if (ai != null) return ((FireTruckAI)ai).m_fireFightingRate;

            ai = m_prefab.m_vehicleAI as HearseAI;
            if (ai != null) return ((HearseAI)ai).m_corpseCapacity;

            ai = m_prefab.m_vehicleAI as PassengerPlaneAI;
            if (ai != null) return ((PassengerPlaneAI)ai).m_passengerCapacity;

            ai = m_prefab.m_vehicleAI as PassengerShipAI;
            if (ai != null) return ((PassengerShipAI)ai).m_passengerCapacity;

            ai = m_prefab.m_vehicleAI as PassengerTrainAI;
            if (ai != null) return ((PassengerTrainAI)ai).m_passengerCapacity;

            ai = m_prefab.m_vehicleAI as PoliceCarAI;
            if (ai != null) return ((PoliceCarAI)ai).m_crimeCapacity;
            
            return -1;
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

        private VehicleInfo GetEngine()
        {
            for (uint i = 0; i < PrefabCollection<VehicleInfo>.PrefabCount(); i++)
            {
                VehicleInfo prefab = PrefabCollection<VehicleInfo>.GetPrefab(i);
                if (prefab == null) continue;

                try
                {
                    if (prefab.m_trailers != null && prefab.m_trailers.Length > 0 && prefab.m_trailers[0].m_info == m_prefab)
                        return prefab;
                }
                catch(Exception e)
                {
                    Debug.LogException(e);
                }
            }

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
}
