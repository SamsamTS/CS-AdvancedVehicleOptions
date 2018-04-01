using System.IO;
using System.Linq;
using ColossalFramework.IO;
using ICities;

namespace AdvancedVehicleOptions
{
    public class SerializableDataExtension : SerializableDataExtensionBase
    {
        private const string ID = "AVO";
        private const int VERSION = 1;

        public override void OnLoadData()
        {
            if (ToolManager.instance.m_properties.m_mode != ItemClass.Availability.Game)
            {
                return;
            }

            // Storing default values ASAP (before any mods have the time to change values)
            DefaultOptions.StoreAll();

            if (!serializableDataManager.EnumerateData().Contains(ID))
            {
                return;
            }
            var data = serializableDataManager.LoadData(ID);
            using (var ms = new MemoryStream(data))
            {
                AdvancedVehicleOptions.config.data = DataSerializer.Deserialize<Configuration>(ms, DataSerializer.Mode.Memory).data;
            }
        }

        public override void OnSaveData()
        {
            if (ToolManager.instance.m_properties.m_mode != ItemClass.Availability.Game ||
                AdvancedVehicleOptions.config == null)
            {
                return;
            }
            using (var ms = new MemoryStream())
            {
                DataSerializer.Serialize(ms, DataSerializer.Mode.Memory, VERSION, AdvancedVehicleOptions.config);
                var data = ms.ToArray();
                serializableDataManager.SaveData(ID, data);
            }
        }
    }
}