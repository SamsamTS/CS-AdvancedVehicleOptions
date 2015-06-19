using UnityEngine;
using ColossalFramework.UI;

namespace AdvancedVehicleOptions.GUI
{
    public class UIOptionPanel : UIPanel
    {
        private UITitleBar m_title;
        private UITextField m_maxSpeed;
        private UIColorField m_color0;
        private UIColorField m_color1;
        private UIColorField m_color2;
        private UIColorField m_color3;
        private UITextField m_color0_hex;
        private UITextField m_color1_hex;
        private UITextField m_color2_hex;
        private UITextField m_color3_hex;
        private UICheckBox m_enabled;
        private UICheckBox m_addBackEngine;
        private UIButton m_clearVehicles;
        private UIButton m_clearParked;

        private VehicleOptions m_options = null;

        private bool m_initialized = false;

        public event PropertyChangedEventHandler<bool> eventEnableCheckChanged;

        public override void Start()
        {
            base.Start();
            backgroundSprite = "UnlockingPanel2";
            isVisible = false;
            canFocus = true;
            isInteractive = true;
            width = 315;
            height = 330;
            relativePosition = new Vector3(Mathf.Floor((GetUIView().fixedWidth - width + 450) / 2), Mathf.Floor((GetUIView().fixedHeight - height) / 2));

            SetupControls();

            m_options = new VehicleOptions();
        }

        public void Show(VehicleOptions options)
        {
            m_initialized = false;

            m_options = options;

            m_title.title = options.localizedName;
            m_maxSpeed.text = Mathf.RoundToInt(options.maxSpeed * 5).ToString();
            m_color0.selectedColor = options.color0;
            m_color1.selectedColor = options.color1;
            m_color2.selectedColor = options.color2;
            m_color3.selectedColor = options.color3;
            m_color0_hex.text = options.color0.ToString();
            m_color1_hex.text = options.color1.ToString();
            m_color2_hex.text = options.color2.ToString();
            m_color3_hex.text = options.color3.ToString();
            m_enabled.isChecked = options.enabled;
            //m_enabled.isVisible = !options.isTrailer;
            m_addBackEngine.isChecked = options.addBackEngine;
            m_addBackEngine.isVisible = (options.prefab.m_vehicleType == VehicleInfo.VehicleType.Train) && options.hasTrailer;

            m_title.iconSprite = UIMainPanel.vehicleIconList[(int)options.category];

            Show();

            m_initialized = true;
        }

        private void SetupControls()
        {
            float offset = 40f;

            // Title Bar
            m_title = AddUIComponent<UITitleBar>();
            m_title.iconSprite = "IconCitizenVehicle";
            m_title.title = "Vehicle Options";

            UIPanel panel = AddUIComponent<UIPanel>();
            panel.gameObject.AddComponent<UICustomControl>();

            panel.backgroundSprite = "UnlockingPanel";
            panel.width = width - 10;
            panel.height = height - offset - 75;
            panel.relativePosition = new Vector3(5, offset);

            // Max Speed
            UILabel maxSpeedLabel = panel.AddUIComponent<UILabel>();
            maxSpeedLabel.text = "Maximum Speed:";
            maxSpeedLabel.textScale = 0.9f;
            maxSpeedLabel.relativePosition = new Vector3(15, 15);

            m_maxSpeed = UIUtils.CreateTextField(panel);
            m_maxSpeed.numericalOnly = true;
            m_maxSpeed.width = 75;
            m_maxSpeed.relativePosition = new Vector3(15, 35);

            UILabel kmh = panel.AddUIComponent<UILabel>();
            kmh.text = "km/h";
            kmh.textScale = 0.9f;
            kmh.relativePosition = new Vector3(95, 40);

            // Colors
            UILabel colorsLabel = panel.AddUIComponent<UILabel>();
            colorsLabel.text = "Colors:";
            colorsLabel.textScale = 0.9f;
            colorsLabel.relativePosition = new Vector3(15, 70);

            gameObject.AddComponent<UICustomControl>();

            m_color0 = UIUtils.CreateColorField(panel);
            m_color0.name = "AVO-color0";
            m_color0.relativePosition = new Vector3(13 , 90 - 2);
            m_color0_hex = UIUtils.CreateTextField(panel);
            m_color0_hex.maxLength = 6;
            m_color0_hex.relativePosition = new Vector3(55, 90);

            m_color1 = UIUtils.CreateColorField(panel);
            m_color1.name = "AVO-color1";
            m_color1.relativePosition = new Vector3(13, 115 - 2);
            m_color1_hex = UIUtils.CreateTextField(panel);
            m_color1_hex.maxLength = 6;
            m_color1_hex.relativePosition = new Vector3(55, 115);

            m_color2 = UIUtils.CreateColorField(panel);
            m_color2.name = "AVO-color2";
            m_color2.relativePosition = new Vector3(158, 90 - 2);
            m_color2_hex = UIUtils.CreateTextField(panel);
            m_color2_hex.maxLength = 6;
            m_color2_hex.relativePosition = new Vector3(200, 90);

            m_color3 = UIUtils.CreateColorField(panel);
            m_color3.name = "AVO-color3";
            m_color3.relativePosition = new Vector3(158, 115 - 2);
            m_color3_hex = UIUtils.CreateTextField(panel);
            m_color3_hex.maxLength = 6;
            m_color3_hex.relativePosition = new Vector3(200, 115);

            // Enable & BackEngine
            m_enabled = UIUtils.CreateCheckBox(panel);
            m_enabled.text = "Allow this vehicle to spawn";
            m_enabled.isChecked = true;
            m_enabled.width = width - 40;
            m_enabled.relativePosition = new Vector3(15, 155); ;

            m_addBackEngine = UIUtils.CreateCheckBox(panel);
            m_addBackEngine.text = "Replace last car with engine";
            m_addBackEngine.isChecked = false;
            m_addBackEngine.width = width - 40;
            m_addBackEngine.relativePosition = new Vector3(15, 180);

            // Remove Vehicles
            UILabel removeLabel = this.AddUIComponent<UILabel>();
            removeLabel.text = "Remove vehicles:";
            removeLabel.textScale = 0.9f;
            removeLabel.relativePosition = new Vector3(10, height - 60);

            m_clearVehicles = UIUtils.CreateButton(this);
            m_clearVehicles.text = "Driving";
            m_clearVehicles.width = 90f;
            m_clearVehicles.relativePosition = new Vector3(10, height - 40);

            m_clearParked = UIUtils.CreateButton(this);
            m_clearParked.text = "Parked";
            m_clearParked.width = 90f;
            m_clearParked.relativePosition = new Vector3(105, height - 40);

            panel.BringToFront();

            // Event handlers
            m_maxSpeed.eventTextSubmitted += OnMaxSpeedSubmitted;

            m_color0.eventSelectedColorChanged += OnColorChanged;
            m_color1.eventSelectedColorChanged += OnColorChanged;
            m_color2.eventSelectedColorChanged += OnColorChanged;
            m_color3.eventSelectedColorChanged += OnColorChanged;

            m_color0_hex.eventTextSubmitted += OnColorHexSubmitted;
            m_color1_hex.eventTextSubmitted += OnColorHexSubmitted;
            m_color2_hex.eventTextSubmitted += OnColorHexSubmitted;
            m_color3_hex.eventTextSubmitted += OnColorHexSubmitted;

            m_enabled.eventCheckChanged += OnCheckChanged;
            m_addBackEngine.eventCheckChanged += OnCheckChanged;

            m_clearVehicles.eventClick += OnClearVehicleClicked;
            m_clearParked.eventClick += OnClearVehicleClicked;
        }

        protected void OnCheckChanged(UIComponent component, bool state)
        {
            if (!m_initialized || m_options == null) return;
            m_initialized = false;

            if (component == m_enabled && m_options.enabled != state)
            {
                m_options.enabled = state;
                AdvancedVehicleOptions.ApplySpawning(m_options);
                eventEnableCheckChanged(this, state);
            }
            else
            {
                m_options.addBackEngine = m_addBackEngine.isChecked;
                AdvancedVehicleOptions.ApplyBackEngine(m_options);
            }
            m_initialized = true;
        }

        protected void OnMaxSpeedSubmitted(UIComponent component, string text)
        {
            if (!m_initialized || m_options == null) return;
            m_initialized = false;

            m_options.maxSpeed = float.Parse(text) / 5f;

            AdvancedVehicleOptions.ApplyMaxSpeed(m_options);
        }

        protected void OnColorChanged(UIComponent component, Color color)
        {
            if (!m_initialized || m_options == null) return;
            m_initialized = false;

            m_options.color0 = m_color0.selectedColor;
            m_options.color1 = m_color1.selectedColor;
            m_options.color2 = m_color2.selectedColor;
            m_options.color3 = m_color3.selectedColor;

            m_color0_hex.text = m_options.color0.ToString();
            m_color1_hex.text = m_options.color1.ToString();
            m_color2_hex.text = m_options.color2.ToString();
            m_color3_hex.text = m_options.color3.ToString();

            AdvancedVehicleOptions.ApplyColors(m_options);
            m_initialized = true;
        }

        protected void OnColorHexSubmitted(UIComponent component, string text)
        {
            if (!m_initialized || m_options == null) return;
            m_initialized = false;

            // Is text a valid color?
            if(text != "000000" && new HexaColor(text).ToString() == "000000")
            {
                m_color0_hex.text = m_options.color0.ToString();
                m_color1_hex.text = m_options.color1.ToString();
                m_color2_hex.text = m_options.color2.ToString();
                m_color3_hex.text = m_options.color3.ToString();

                return;
            }

            m_options.color0.Value = m_color0_hex.text;
            m_options.color1.Value = m_color1_hex.text;
            m_options.color2.Value = m_color2_hex.text;
            m_options.color3.Value = m_color3_hex.text;

            m_color0_hex.text = m_options.color0.ToString();
            m_color1_hex.text = m_options.color1.ToString();
            m_color2_hex.text = m_options.color2.ToString();
            m_color3_hex.text = m_options.color3.ToString();

            m_color0.selectedColor = m_options.color0;
            m_color1.selectedColor = m_options.color1;
            m_color2.selectedColor = m_options.color2;
            m_color3.selectedColor = m_options.color3;

            AdvancedVehicleOptions.ApplyColors(m_options);
            m_initialized = true;
        }

        protected void OnClearVehicleClicked(UIComponent component, UIMouseEventParameter p)
        {
            if (m_options == null) return;

            AdvancedVehicleOptions.ClearVehicles(m_options, component == m_clearParked);
        }


    }

}
