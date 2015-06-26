using UnityEngine;
using ColossalFramework.UI;

using System;

namespace AdvancedVehicleOptions.GUI
{
    public class UIMainPanel : UIPanel
    {
        private UITitleBar m_title;
        private UIDropDown m_category;
        private UITextField m_search;
        private UIFastList m_fastList;
        private UIButton m_reload;
        private UIButton m_save;
        private UITextureSprite m_preview;
        private UIOptionPanel m_optionPanel;

        public UISprite m_button;

        private VehicleOptions[] m_optionsList;
        private PreviewRenderer m_previewRenderer;

        private const int HEIGHT = 550;
        private const int WIDTHLEFT = 470;
        private const int WIDTHRIGHT = 315;

        public static readonly string[] categoryList = { "All", "Citizen",
            "Forestry", "Farming", "Ore", "Oil", "Industry",
            "Police", "FireSafety", "Healthcare", "Deathcare", "Garbage",
            "Bus", "Metro", "Cargo Train", "Passenger Train",
            "Cargo Ship", "Passenger Ship", "Plane" };

        public static readonly string[] vehicleIconList = { "IconCitizenVehicle",
              "IconPolicyForest", "IconPolicyFarming", "IconPolicyOre", "IconPolicyOil", "IconPolicyNone",
              "ToolbarIconPolice", "InfoIconFireSafety", "ToolbarIconHealthcare", "ToolbarIconHealthcareHovered", "InfoIconGarbage",
              "SubBarPublicTransportBus", "SubBarPublicTransportMetro", "IconServiceVehicle", "SubBarPublicTransportTrain",
              "IconCargoShip", "SubBarPublicTransportShip", "SubBarPublicTransportPlane" };

        public UIOptionPanel optionPanel
        {
            get { return m_optionPanel; }
        }

        public VehicleOptions[] optionList
        {
            get { return m_optionsList; }
            set
            {
                m_optionsList = value;
                Array.Sort(m_optionsList);

                PopulateList();
            }
        }

        public override void Start()
        {
            base.Start();

            UIView view = GetUIView();

            name = "AdvancedVehicleOptions";
            backgroundSprite = "UnlockingPanel2";
            isVisible = false;
            canFocus = true;
            isInteractive = true;
            width = WIDTHLEFT + WIDTHRIGHT;
            height = HEIGHT;
            relativePosition = new Vector3(Mathf.Floor((view.fixedWidth - width) / 2), Mathf.Floor((view.fixedHeight - height) / 2));

            // Setting up UI
            SetupControls();

            // Adding main button
            UITabstrip toolStrip = view.FindUIComponent<UITabstrip>("MainToolstrip");
            m_button = toolStrip.AddUIComponent<UISprite>();
            m_button.spriteName = "IconCitizenVehicle";
            m_button.size = m_button.spriteInfo.pixelSize;
            m_button.relativePosition = new Vector3(0, 5);

            view.FindUIComponent<UITabContainer>("TSContainer").AddUIComponent<UIPanel>().color = new Color32(0, 0, 0, 0);

            m_button.eventClick += new MouseEventHandler((c, p) =>
            {
                if (p != null) p.Use();
                isVisible = !isVisible;
                if (isVisible)
                {
                    m_fastList.DisplayAt(m_fastList.listPosition);
                    m_optionPanel.Show(m_fastList.rowsData[m_fastList.selectedIndex] as VehicleOptions);
                    m_preview.parent.isVisible = true;
                    ChangePreviewColor(m_optionsList[m_fastList.selectedIndex].color0);
                }
            });

            // Loading config
            AdvancedVehicleOptions.LoadConfig(this);
        }

        public override void OnDestroy()
        {
            base.OnDestroy();

            DebugUtils.Log("Destroying UIMainPanel");

            Destroy(m_button);
            UIUtils.DestroyDeeply(m_optionPanel);
        }

        private void SetupControls()
        {
            float offset = 40f;

            // Title Bar
            m_title = AddUIComponent<UITitleBar>();
            m_title.iconSprite = "IconCitizenVehicle";
            m_title.title = "Advanced Vehicle Options " + AdvancedVehicleOptions.version;

            // Category DropDown
            UILabel label = AddUIComponent<UILabel>();
            label.textScale = 0.8f;
            label.padding = new RectOffset(0, 0, 8, 0);
            label.relativePosition = new Vector3(10f, offset);
            label.text = "Category :";

            m_category = UIUtils.CreateDropDown(this);
            m_category.width = 150;

            for (int i = 0; i < categoryList.Length; i++)
                m_category.AddItem(categoryList[i]);

            m_category.selectedIndex = 0;
            m_category.relativePosition = label.relativePosition + new Vector3(70f, 0f);

            m_category.eventSelectedIndexChanged += (c, t) =>
            {
                m_category.enabled = false;
                PopulateList();
                m_category.enabled = true;
            };

            // Search
            m_search = UIUtils.CreateTextField(this);
            m_search.width = 150f;
            m_search.height = 30f;
            m_search.padding = new RectOffset(6, 6, 6, 6);
            m_search.relativePosition = new Vector3(WIDTHLEFT - m_search.width, offset);

            m_search.eventTextChanged += (c, t) => PopulateList();

            label = AddUIComponent<UILabel>();
            label.textScale = 0.8f;
            label.padding = new RectOffset(0, 0, 8, 0);
            label.relativePosition = m_search.relativePosition - new Vector3(60f, 0f);
            label.text = "Search :";

            // FastList
            m_fastList = UIFastList.Create<UIVehicleItem>(this);
            m_fastList.backgroundSprite = "UnlockingPanel";
            m_fastList.width = WIDTHLEFT - 5;
            m_fastList.height = height - offset - 110;
            m_fastList.canSelect = true;
            m_fastList.relativePosition = new Vector3(5, offset + 35);

            // Configuration file buttons
            UILabel configLabel = this.AddUIComponent<UILabel>();
            configLabel.text = "Configuration file:";
            configLabel.textScale = 0.9f;
            configLabel.relativePosition = new Vector3(10, height - 60);

            m_reload = UIUtils.CreateButton(this);
            m_reload.text = "Reload";
            m_reload.relativePosition = new Vector3(10, height - 40);

            m_save = UIUtils.CreateButton(this);
            m_save.text = "Save";
            m_save.relativePosition = new Vector3(105, height - 40);

            // Preview
            UIPanel panel = AddUIComponent<UIPanel>();
            panel.backgroundSprite = "GenericPanel";
            panel.width = WIDTHRIGHT - 10;
            panel.height = HEIGHT - 375;
            panel.relativePosition = new Vector3(WIDTHLEFT + 5, offset);

            m_preview = panel.AddUIComponent<UITextureSprite>();
            m_preview.size = panel.size;
            m_preview.relativePosition = Vector3.zero;

            m_previewRenderer = gameObject.AddComponent<PreviewRenderer>();
            m_previewRenderer.size = m_preview.size * 2; // Twice the size for anti-aliasing

            m_preview.texture = m_previewRenderer.texture;

            // Option panel
            m_optionPanel = AddUIComponent<UIOptionPanel>();
            m_optionPanel.relativePosition = new Vector3(WIDTHLEFT, height - 330);

            // Event handlers
            m_fastList.eventSelectedIndexChanged += OnSelectedItemChanged; 
            m_optionPanel.eventEnableCheckChanged += OnEnableStateChanged;
            m_reload.eventClick += (c, t) => AdvancedVehicleOptions.LoadConfig(this);
            m_save.eventClick += (c, t) => AdvancedVehicleOptions.SaveConfig();

            panel.eventMouseDown += (c, p) =>
            {
                eventMouseMove += RotateCamera;
                m_previewRenderer.Render();
            };

            panel.eventMouseUp += (c, p) =>
            {
                eventMouseMove -= RotateCamera;
                m_previewRenderer.Render();
            };

            panel.eventMouseWheel += (c, p) =>
            {
                m_previewRenderer.zoom -= Mathf.Sign(p.wheelDelta) * 0.25f;
                m_previewRenderer.Render();
            };
        }

        private void RotateCamera(UIComponent c, UIMouseEventParameter p)
        {
            m_previewRenderer.cameraRotation -= p.moveDelta.x / m_preview.width * 360f;
            m_previewRenderer.Render();
        }

        private void PopulateList()
        {
            m_fastList.rowsData.Clear();
            m_fastList.selectedIndex = -1;
            for (int i = 0; i < m_optionsList.Length; i++)
            {
                if (m_optionsList[i] != null &&
                    (m_category.selectedIndex == 0 || (int)m_optionsList[i].category == m_category.selectedIndex - 1) &&
                    (String.IsNullOrEmpty(m_search.text.Trim()) || m_optionsList[i].localizedName.ToLower().Contains(m_search.text.Trim().ToLower())))
                {
                    m_fastList.rowsData.Add(m_optionsList[i]);
                }
            }

            m_fastList.rowHeight = 40f;
            m_fastList.DisplayAt(0);
            m_fastList.selectedIndex = 0;

            m_optionPanel.isVisible = m_fastList.rowsData.m_size > 0;
            m_preview.parent.isVisible = m_optionPanel.isVisible;
        }
        protected void OnSelectedItemChanged(UIComponent component, int i)
        {
            m_previewRenderer.mesh = null;

            VehicleOptions options = m_fastList.rowsData[i] as VehicleOptions;

            m_optionPanel.Show(options);
            m_preview.parent.isVisible = true;

            m_previewRenderer.mesh = options.prefab.m_mesh;
            m_previewRenderer.material = options.prefab.m_material;
            Color color = options.color0;
            color.a = 0; // Fixes the wrong lighting on one half of the vehicle
            m_previewRenderer.material.color = color;
            m_previewRenderer.cameraRotation = 120f;
            m_previewRenderer.zoom = 3f;
            m_previewRenderer.Render();
        }

        protected void OnEnableStateChanged(UIComponent component, bool state)
        {
            m_fastList.DisplayAt(m_fastList.listPosition);
        }

        public void ChangePreviewColor(Color color)
        {
            if (m_previewRenderer.material != null && m_previewRenderer.material.color != color)
            {
                color.a = 0; // Fixes the wrong lighting on one half of the vehicle
                m_previewRenderer.material.color = color;
                m_previewRenderer.Render();
            }
        }
    }

}
