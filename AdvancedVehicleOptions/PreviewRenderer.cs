using ColossalFramework;
using UnityEngine;

namespace AdvancedVehicleOptions
{
    public class PreviewRenderer : MonoBehaviour
    {
        private Camera m_camera;
        private float m_rotation = 120f;
        private float m_zoom = 3f;

        public PreviewRenderer()
        {
            m_camera = new GameObject("Camera").AddComponent<Camera>();
            m_camera.transform.SetParent(transform);
            m_camera.backgroundColor = new Color(0, 0, 0, 0);
            m_camera.fieldOfView = 30f;
            m_camera.nearClipPlane = 1f;
            m_camera.farClipPlane = 1000f;
            m_camera.hdr = true;
            m_camera.enabled = false;
            m_camera.targetTexture = new RenderTexture(512, 512, 24, RenderTextureFormat.ARGB32);
            m_camera.pixelRect = new Rect(0f, 0f, 512, 512);
            m_camera.clearFlags = CameraClearFlags.Color;
        }

        public Vector2 size
        {
            get { return new Vector2(m_camera.targetTexture.width, m_camera.targetTexture.height); }
            set
            {
                if (size != value)
                {
                    m_camera.targetTexture = new RenderTexture((int)value.x, (int)value.y, 24, RenderTextureFormat.ARGB32);
                    m_camera.pixelRect = new Rect(0f, 0f, value.x, value.y);
                }
            }
        }

        public RenderTexture texture
        {
            get { return m_camera.targetTexture; }
        }

        public float cameraRotation
        {
            get { return m_rotation; }
            set { m_rotation = value % 360f; }
        }

        public float zoom
        {
            get { return m_zoom; }
            set
            {
                m_zoom = Mathf.Clamp(value, 0.5f, 5f);
            }
        }

        public void RenderVehicle(VehicleInfo info)
        {
            RenderVehicle(info, info.m_color0, false);
        }

        public void RenderVehicle(VehicleInfo info, Color color, bool useColor = true)
        {
            InfoManager infoManager = Singleton<InfoManager>.instance;
            InfoManager.InfoMode currentMod = infoManager.CurrentMode;
            InfoManager.SubInfoMode currentSubMod = infoManager.CurrentSubMode; ;
            infoManager.SetCurrentMode(InfoManager.InfoMode.None, InfoManager.SubInfoMode.Default);
            infoManager.UpdateInfoMode();

            Light sunLight = DayNightProperties.instance.sunLightSource;
            float lightIntensity = sunLight.intensity;
            Color lightColor = sunLight.color;
            Vector3 lightAngles = sunLight.transform.eulerAngles;

            sunLight.intensity = 2f;
            sunLight.color = Color.white;
            sunLight.transform.eulerAngles = new Vector3(50, 180, 70);

            Light mainLight = RenderManager.instance.MainLight;
            RenderManager.instance.MainLight = sunLight;

            if(mainLight == DayNightProperties.instance.moonLightSource)
            {
                DayNightProperties.instance.sunLightSource.enabled = true;
                DayNightProperties.instance.moonLightSource.enabled = false;
            }

            Vector3 one = Vector3.one;

            float magnitude = info.m_mesh.bounds.extents.magnitude;
            float num = magnitude + 16f;
            float num2 = magnitude * m_zoom;

            m_camera.transform.position = Vector3.forward * num2;
            m_camera.transform.rotation = Quaternion.AngleAxis(180, Vector3.up);
            m_camera.nearClipPlane = Mathf.Max(num2 - num * 1.5f, 0.01f);
            m_camera.farClipPlane = num2 + num * 1.5f;

            Quaternion rotation = Quaternion.Euler(20f, 0f, 0f) * Quaternion.Euler(0f, m_rotation, 0f);
            Vector3 position = rotation * -info.m_mesh.bounds.center;

            Vector3 swayPosition = Vector3.zero;

            VehicleManager instance = Singleton<VehicleManager>.instance;
            Matrix4x4 matrixBody = Matrix4x4.TRS(position, rotation, Vector3.one);
            Matrix4x4 matrixTyre = info.m_vehicleAI.CalculateTyreMatrix(Vehicle.Flags.Created, ref position, ref rotation, ref one, ref matrixBody);

            MaterialPropertyBlock materialBlock = instance.m_materialBlock;
            materialBlock.Clear();
            materialBlock.SetMatrix(instance.ID_TyreMatrix, matrixTyre);
            materialBlock.SetVector(instance.ID_TyrePosition, Vector3.zero);
            materialBlock.SetVector(instance.ID_LightState, Vector3.zero);
            if(useColor) materialBlock.SetColor(instance.ID_Color, color);

            instance.m_drawCallData.m_defaultCalls = instance.m_drawCallData.m_defaultCalls + 1;

            info.m_material.SetVectorArray(instance.ID_TyreLocation, info.m_generatedInfo.m_tyres);
            Graphics.DrawMesh(info.m_mesh, matrixBody, info.m_material, 0, m_camera, 0, materialBlock, true, true);

            m_camera.RenderWithShader(info.m_material.shader, "");

            sunLight.intensity = lightIntensity;
            sunLight.color = lightColor;
            sunLight.transform.eulerAngles = lightAngles;

            RenderManager.instance.MainLight = mainLight;

            if (mainLight == DayNightProperties.instance.moonLightSource)
            {
                DayNightProperties.instance.sunLightSource.enabled = false;
                DayNightProperties.instance.moonLightSource.enabled = true;
            }

            infoManager.SetCurrentMode(currentMod, currentSubMod);
            infoManager.UpdateInfoMode();
        }
    }
}