using ColossalFramework.IO;

using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.ComponentModel;
using System.Collections.Generic;

namespace AdvancedVehicleOptions
{
    [XmlType("ArrayOfVehicleOptions")]
    [Serializable]
    public class Configuration : IDataContainer
    {
        public class VehicleData
        {
            #region serialized
            [XmlAttribute("name")]
            public string name;
            [DefaultValue(true)]
            public bool enabled = true;
            [DefaultValue(false)]
            public bool addBackEngine = false;
            public float maxSpeed;
            public float acceleration;
            public float braking;
            [DefaultValue(true)]
            public bool useColorVariations = true;
            public HexaColor color0;
            public HexaColor color1;
            public HexaColor color2;
            public HexaColor color3;
            [DefaultValue(-1)]
            public int capacity = -1;
            #endregion

            public bool isCustomAsset
            {
                get
                {
                    return name.Contains(".");
                }
            }
        }

        [XmlElement("VehicleOptions")]
        public VehicleData[] data;

        [XmlIgnore]
        public VehicleOptions[] options;

        private List<VehicleData> m_defaultVehicles = new List<VehicleData>();

        // Serialize to save
        public void Serialize(DataSerializer s)
        {
            try
            {
                int count = options.Length;
                s.WriteInt32(count);

                for (int i = 0; i < count; i++)
                {
                    s.WriteUniqueString(options[i].name);
                    DebugUtils.Log(options[i].name);
                    s.WriteBool(options[i].enabled);
                    s.WriteBool(options[i].addBackEngine);
                    s.WriteFloat(options[i].maxSpeed);
                    s.WriteFloat(options[i].acceleration);
                    s.WriteFloat(options[i].braking);
                    s.WriteBool(options[i].useColorVariations);
                    s.WriteUniqueString(options[i].color0.Value);
                    s.WriteUniqueString(options[i].color1.Value);
                    s.WriteUniqueString(options[i].color2.Value);
                    s.WriteUniqueString(options[i].color3.Value);
                    s.WriteInt32(options[i].capacity);
                }
            }
            catch (Exception e)
            {
                DebugUtils.LogException(e);
            }
        }

        public void Deserialize(DataSerializer s)
        {
            try
            {
                options = null;
                data = null;

                int count = s.ReadInt32();
                data = new VehicleData[count];

                for (int i = 0; i < count; i++)
                {
                    data[i] = new VehicleData();
                    data[i].name = s.ReadUniqueString();
                    data[i].enabled = s.ReadBool();
                    data[i].addBackEngine = s.ReadBool();
                    data[i].maxSpeed = s.ReadFloat();
                    data[i].acceleration = s.ReadFloat();
                    data[i].braking = s.ReadFloat();
                    data[i].useColorVariations = s.ReadBool();
                    data[i].color0 = new HexaColor(s.ReadUniqueString());
                    data[i].color1 = new HexaColor(s.ReadUniqueString());
                    data[i].color2 = new HexaColor(s.ReadUniqueString());
                    data[i].color3 = new HexaColor(s.ReadUniqueString());
                    data[i].capacity = s.ReadInt32();
                }
            }
            catch (Exception e)
            {
                // Couldn't Deserialize
                DebugUtils.Warning("Couldn't deserialize");
                DebugUtils.LogException(e);
            }
        }

        public void AfterDeserialize(DataSerializer s)
        {
        }

        // Serialize to file
        public void Serialize(string filename)
        {
            try
            {
                if (AdvancedVehicleOptions.isGameLoaded) OptionsToData();

                // Add back default vehicle options that might not exist on the map
                // I.E. Snowplow on non-snowy maps
                if (m_defaultVehicles.Count > 0)
                {
                    List<VehicleData> new_data = new List<VehicleData>(data);

                    for (int i = 0; i < m_defaultVehicles.Count; i++)
                    {
                        bool found = false;
                        for (int j = 0; j < data.Length; j++)
                        {
                            if (m_defaultVehicles[i].name == data[j].name)
                            {
                                found = true;
                                break;
                            }
                        }
                        if (!found)
                        {
                            new_data.Add(m_defaultVehicles[i]);
                        }
                    }

                    data = new_data.ToArray();
                }

                using (FileStream stream = new FileStream(filename, FileMode.OpenOrCreate))
                {
                    stream.SetLength(0); // Emptying the file !!!
                    XmlSerializer xmlSerializer = new XmlSerializer(typeof(Configuration));
                    xmlSerializer.Serialize(stream, this);
                    DebugUtils.Log("Configuration saved");
                }
            }
            catch (Exception e)
            {
                DebugUtils.Warning("Couldn't save configuration at \"" + Directory.GetCurrentDirectory() + "\"");
                DebugUtils.LogException(e);
            }
        }

        public void Deserialize(string filename)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(Configuration));
            Configuration config = null;

            options = null;
            data = null;

            try
            {
                // Trying to Deserialize the configuration file
                using (FileStream stream = new FileStream(filename, FileMode.Open))
                {
                    config = xmlSerializer.Deserialize(stream) as Configuration;
                }
            }
            catch (Exception e)
            {
                // Couldn't Deserialize (XML malformed?)
                DebugUtils.Warning("Couldn't load configuration (XML malformed?)");
                DebugUtils.LogException(e);

                config = null;
            }

            if(config != null)
            {
                data = config.data;

                if(data != null)
                {
                    // Saves all default vehicle options that might not exist on the map
                    // I.E. Snowplow on non-snowy maps
                    m_defaultVehicles.Clear();
                    for (int i = 0; i < data.Length; i++)
                    {
                        if (data[i] != null && !data[i].isCustomAsset)
                            m_defaultVehicles.Add(data[i]);
                    }
                }


                if (AdvancedVehicleOptions.isGameLoaded) DataToOptions();
            }
        }

        public void OptionsToData()
        {
            if (options == null) return;

            data = new VehicleData[options.Length];

            for (int i = 0; i < options.Length; i++)
            {
                data[i] = new VehicleData();
                data[i].name = options[i].name;
                data[i].enabled = options[i].enabled;
                data[i].addBackEngine = options[i].addBackEngine;
                data[i].maxSpeed = options[i].maxSpeed;
                data[i].acceleration = options[i].acceleration;
                data[i].braking = options[i].braking;
                data[i].useColorVariations = options[i].useColorVariations;
                data[i].color0 = options[i].color0;
                data[i].color1 = options[i].color1;
                data[i].color2 = options[i].color2;
                data[i].color3 = options[i].color3;
                data[i].capacity = options[i].capacity;
            }
        }

        public void DataToOptions()
        {
            if (data == null) return;

            options = new VehicleOptions[data.Length];

            for (int i = 0; i < data.Length; i++)
            {
                options[i] = new VehicleOptions();
                options[i].name = data[i].name;
                options[i].enabled = data[i].enabled;
                options[i].addBackEngine = data[i].addBackEngine;
                options[i].maxSpeed = data[i].maxSpeed;
                options[i].acceleration = data[i].acceleration;
                options[i].braking = data[i].braking;
                options[i].useColorVariations = data[i].useColorVariations;
                options[i].color0 = data[i].color0;
                options[i].color1 = data[i].color1;
                options[i].color2 = data[i].color2;
                options[i].color3 = data[i].color3;
                options[i].capacity = data[i].capacity;
            }

            VehicleOptions.UpdateTransfertVehicles();
        }
    }
}
