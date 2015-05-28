using UnityEngine;

using System.Text;
using System.Xml.Serialization;

namespace AdvancedVehicleOptions
{
    public class VehicleOptions
    {
        [XmlAttribute("name")]
        public string name;

        public bool enabled;
        public bool addBackEngine;
        public float maxSpeed;
        public HexaColor color0;
        public HexaColor color1;
        public HexaColor color2;
        public HexaColor color3;

        [XmlIgnore]
        public VehicleInfo.VehicleType vehicleType;
        [XmlIgnore]
        public ItemClass itemClass;
        [XmlIgnore]
        public string localizedName;

        public string icon
        {
            get
            {
                string icon;
                switch (itemClass.m_service)
                {
                    case ItemClass.Service.FireDepartment:
                        icon = "InfoIconFireSafety";
                        break;
                    case ItemClass.Service.Garbage:
                        icon = "InfoIconGarbage";
                        break;
                    case ItemClass.Service.HealthCare:
                        icon = "ToolbarIconHealthcare";
                        break;
                    case ItemClass.Service.PoliceDepartment:
                        icon = "ToolbarIconPolice";
                        break;
                    default:
                        if (vehicleType == VehicleInfo.VehicleType.Ship)
                            icon = "IconCargoShip";
                        else if (vehicleType == VehicleInfo.VehicleType.Train)
                            icon = "IconServiceVehicle"; // No cargo train icon available
                        else
                            icon = "IconCitizenVehicle";
                        break;
                }

                switch (itemClass.m_subService)
                {
                    case ItemClass.SubService.PublicTransportBus:
                        icon = "SubBarPublicTransportBus";
                        break;
                    case ItemClass.SubService.PublicTransportMetro:
                        icon = "SubBarPublicTransportMetro";
                        break;
                    case ItemClass.SubService.PublicTransportPlane:
                        icon = "SubBarPublicTransportPlane";
                        break;
                    case ItemClass.SubService.PublicTransportShip:
                        icon = "SubBarPublicTransportShip";
                        break;
                    case ItemClass.SubService.PublicTransportTrain:
                        icon = "SubBarPublicTransportTrain";
                        break;
                }

                return icon;
            }
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
