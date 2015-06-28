using ColossalFramework;
using UnityEngine;

namespace AdvancedVehicleOptions
{
    public class PreviewRenderer : MonoBehaviour
    {
        private Camera m_camera;
        private Mesh m_mesh;
        private Bounds m_bounds;
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

        public Mesh mesh
        {
            get { return m_mesh; }
            set
            {
                if(m_mesh != value)
                {
                    m_mesh = value;

                    if (value != null)
                    {
                        m_bounds = new Bounds(Vector3.zero, Vector3.zero);
                        Vector3[] vertices = mesh.vertices;
                        for (int i = 0; i < vertices.Length; i++)
                            m_bounds.Encapsulate(vertices[i]);
                    }
                }
            }
        }

        public Material material
        {
            get;
            set;
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

        public void Render()
        {
            if (m_mesh == null) return;
            
            float magnitude = m_bounds.extents.magnitude;
            float num = magnitude + 16f;
            float num2 = magnitude * m_zoom;

            m_camera.transform.position = -Vector3.forward * num2;
            m_camera.transform.rotation = Quaternion.identity;
            m_camera.nearClipPlane = Mathf.Max(num2 - num * 1.5f, 0.01f);
            m_camera.farClipPlane = num2 + num * 1.5f;

            Quaternion quaternion = Quaternion.Euler(-20f, 0f, 0f) * Quaternion.Euler(0f, m_rotation, 0f);
            Vector3 pos = quaternion * -m_bounds.center;
            Matrix4x4 matrix = Matrix4x4.TRS(pos, quaternion, Vector3.one);

            InfoManager infoManager = Singleton<InfoManager>.instance;
            InfoManager.InfoMode currentMod = infoManager.CurrentMode;
            InfoManager.SubInfoMode currentSubMod = infoManager.CurrentSubMode; ;
            infoManager.SetCurrentMode(InfoManager.InfoMode.None, InfoManager.SubInfoMode.Default);

            Graphics.DrawMesh(m_mesh, matrix, material, 0, m_camera, 0, null, true, true);
            m_camera.RenderWithShader(material.shader, "");

            infoManager.SetCurrentMode(currentMod, currentSubMod);

        }
    }
}