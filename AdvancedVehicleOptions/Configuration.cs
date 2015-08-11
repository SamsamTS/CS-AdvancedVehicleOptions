using UnityEngine;

using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.ComponentModel;

namespace AdvancedVehicleOptions
{
    [XmlType("ArrayOfVehicleOptions")]
    [Serializable]
    public class Configuration
    {
        public class VehicleData
        {
            #region serialized
            [XmlAttribute("name")]
            public string name;
            public bool enabled;
            public bool addBackEngine;
            public float maxSpeed;
            public float acceleration;
            public float braking;
            public bool useColorVariations;
            public HexaColor color0;
            public HexaColor color1;
            public HexaColor color2;
            public HexaColor color3;
            [DefaultValue(-1)]
            public int capacity;
            #endregion
        }

        [XmlAttribute]
        public string version;

        [XmlAttribute, DefaultValue(true)]
        public bool randomSpeed = true;

        [XmlElement("VehicleOptions")]
        public VehicleData[] data;

        [XmlIgnore]
        public VehicleOptions[] options;

        public void Serialize(string filename)
        {
            try
            {
                if (AdvancedVehicleOptions.isGameLoaded) ConvertOptions();

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
                Debug.LogException(e);
            }
        }

        public void Deserialize(string filename)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(Configuration));
            Configuration config = null;

            try
            {
                if (AdvancedVehicleOptions.isGameLoaded) ConvertItems();
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
                Debug.LogException(e);

                config = null;
            }

            if(config != null)
            {
                version = config.version;
                randomSpeed = config.randomSpeed;
                data = config.data;

                if (AdvancedVehicleOptions.isGameLoaded) ConvertItems();
            }
        }

        private void ConvertOptions()
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

        private void ConvertItems()
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
        }
    }
}
