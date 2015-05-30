using UnityEngine;

using System;
using System.Text;
using System.Xml.Serialization;

namespace AdvancedVehicleOptions
{
    public class VehicleOptions : IComparable
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
        public bool hasTrailer;
        [XmlIgnore]
        public string localizedName;
        [XmlIgnore]
        private Category m_category = Category.None;

        public enum Category
        {
            None = -1,
            Citizen,
            Industrial,
            CargoTrain,
            CargoShip,
            Police,
            FireSafety,
            Healthcare,
            Garbage,
            TransportBus,
            TransportMetro,
            TransportTrain,
            TransportShip,
            TransportPlane
        }

        public Category category
        {
            get
            {
                if (m_category > Category.None) return m_category;

                switch (itemClass.m_service)
                {
                    case ItemClass.Service.PoliceDepartment:
                        return Category.Police;
                    case ItemClass.Service.FireDepartment:
                        return Category.FireSafety;
                    case ItemClass.Service.HealthCare:
                        return Category.Healthcare;
                    case ItemClass.Service.Garbage:
                        return Category.Garbage;
                }

                switch (itemClass.m_subService)
                {
                    case ItemClass.SubService.PublicTransportBus:
                        return Category.TransportBus;
                    case ItemClass.SubService.PublicTransportMetro:
                        return Category.TransportMetro;
                    case ItemClass.SubService.PublicTransportTrain:
                        return Category.TransportTrain;
                    case ItemClass.SubService.PublicTransportShip:
                        return Category.TransportShip;
                    case ItemClass.SubService.PublicTransportPlane:
                        return Category.TransportPlane;
                }

                switch (vehicleType)
                {
                    case VehicleInfo.VehicleType.Train:
                        return Category.CargoTrain;
                    case VehicleInfo.VehicleType.Ship:
                        return Category.CargoShip;
                }

                switch (itemClass.m_service)
                {
                    case ItemClass.Service.Industrial:
                        return Category.Industrial;
                }

                return Category.Citizen;
            }
        }

        public int CompareTo(object o)
        {
            if (o == null) return 1;

            VehicleOptions options = (VehicleOptions)o;

            int delta = category - options.category;
            if (delta == 0) return localizedName.CompareTo(options.localizedName);

            return delta;
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
