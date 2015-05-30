using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace AdvancedVehicleOptions.OldMods
{
    public struct Vehicle
    {
        [XmlAttribute("name")]
        public string name;
        public bool enabled;
    }

    public class VehicleRemoverMod
    {
        public static List<Vehicle> LoadConfig()
        {
            if (!File.Exists("VehicleRemover.xml")) return null;

            DebugUtils.Log("Loading Vehicle Remover configuration");

            XmlSerializer xmlSerializer = new XmlSerializer(typeof(Vehicle[]));
            Vehicle[] vehicles = null;

            try
            {
                // Trying to deserialize the configuration file
                using (FileStream stream = new FileStream("VehicleRemover.xml", FileMode.Open))
                {
                    vehicles = xmlSerializer.Deserialize(stream) as Vehicle[];
                }
            }
            catch { return null; }

            return new List<Vehicle>(vehicles);
        }
    }

    public class VehicleColorInfo
    {
        [XmlAttribute("name")]
        public string name;
        public HexaColor color0;
        public HexaColor color1;
        public HexaColor color2;
        public HexaColor color3;
    }

    public class VehicleColorChangerMod
    {
        public static List<VehicleColorInfo> LoadConfig()
        {
            if (!File.Exists("VehicleColorChanger.xml")) return null;

            DebugUtils.Log("Loading Vehicle Changer configuration");

            XmlSerializer xmlSerializer = new XmlSerializer(typeof(VehicleColorInfo[]));
            VehicleColorInfo[] vehicles = null;

            try
            {
                // Trying to deserialize the configuration file
                using (FileStream stream = new FileStream("VehicleColorChanger.xml", FileMode.Open))
                {
                    vehicles = xmlSerializer.Deserialize(stream) as VehicleColorInfo[];
                }
            }
            catch { return null; }

            return new List<VehicleColorInfo>(vehicles);
        }
    }

}
    
