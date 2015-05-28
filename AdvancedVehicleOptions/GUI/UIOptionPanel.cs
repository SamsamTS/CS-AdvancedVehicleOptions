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

        public override void Start()
        {
            base.Start();
            backgroundSprite = "UnlockingPanel2";
            isVisible = true;
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
            m_options = options;

            m_title.title = options.localizedName;
            m_maxSpeed.text = options.maxSpeed.ToString();
            m_color0.selectedColor = options.color0;
            m_color1.selectedColor = options.color1;
            m_color2.selectedColor = options.color2;
            m_color3.selectedColor = options.color3;
            m_color0_hex.text = options.color0.ToString();
            m_color1_hex.text = options.color1.ToString();
            m_color2_hex.text = options.color2.ToString();
            m_color3_hex.text = options.color3.ToString();
            m_enabled.isChecked = options.enabled;
            m_addBackEngine.isChecked = options.addBackEngine;
            m_addBackEngine.isVisible = (options.vehicleType == VehicleInfo.VehicleType.Train);

            m_title.iconSprite = options.icon;

            Show();
        }

        public event PropertyChangedEventHandler<bool> eventEnableStateChanged;

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
            UILabel maxSpeedLabel = this.AddUIComponent<UILabel>();
            maxSpeedLabel.text = "Maximum Speed:";
            maxSpeedLabel.textScale = 0.9f;
            maxSpeedLabel.relativePosition = new Vector3(20, offset + 15);

            m_maxSpeed = UIUtils.CreateTextField(this);
            m_maxSpeed.allowFloats = true;
            m_maxSpeed.numericalOnly = true;
            m_maxSpeed.width = 130;
            m_maxSpeed.relativePosition = new Vector3(20, offset + 35);

            // Colors
            UILabel colorsLabel = this.AddUIComponent<UILabel>();
            colorsLabel.text = "Colors:";
            colorsLabel.textScale = 0.9f;
            colorsLabel.relativePosition = new Vector3(20, offset + 70);

            m_color0 = UIUtils.CreateColorField(this);
            m_color0.relativePosition = new Vector3(18 , offset + 90 - 2);
            m_color0_hex = UIUtils.CreateTextField(this);
            m_color0_hex.maxLength = 6;
            m_color0_hex.relativePosition = new Vector3(60, offset + 90);

            m_color1 = UIUtils.CreateColorField(this);
            m_color1.relativePosition = new Vector3(18, offset + 115 - 2);
            m_color1_hex = UIUtils.CreateTextField(this);
            m_color1_hex.maxLength = 6;
            m_color1_hex.relativePosition = new Vector3(60, offset + 115);

            m_color2 = UIUtils.CreateColorField(this);
            m_color2.relativePosition = new Vector3(163, offset + 90 - 2);
            m_color2_hex = UIUtils.CreateTextField(this);
            m_color2_hex.maxLength = 6;
            m_color2_hex.relativePosition = new Vector3(205, offset + 90);

            m_color3 = UIUtils.CreateColorField(this);
            m_color3.relativePosition = new Vector3(163, offset + 115 - 2);
            m_color3_hex = UIUtils.CreateTextField(this);
            m_color3_hex.maxLength = 6;
            m_color3_hex.relativePosition = new Vector3(205, offset + 115);

            // Enable & BackEngine
            m_enabled = UIUtils.CreateCheckBox(this);
            m_enabled.text = "Allow this vehicle to spawn";
            m_enabled.isChecked = true;
            m_enabled.width = width - 40;
            m_enabled.relativePosition = new Vector3(20, offset + 155); ;

            m_addBackEngine = UIUtils.CreateCheckBox(this);
            m_addBackEngine.text = "Replace last car with engine";
            m_addBackEngine.isChecked = false;
            m_addBackEngine.width = width - 40;
            m_addBackEngine.relativePosition = new Vector3(20, offset + 180);

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

            // Event handlers
            m_maxSpeed.eventTextSubmitted += new PropertyChangedEventHandler<string>(OnMaxSpeedSubmitted);

            m_color0.eventSelectedColorChanged += new PropertyChangedEventHandler<Color>(OnColorChanged);
            m_color1.eventSelectedColorChanged += new PropertyChangedEventHandler<Color>(OnColorChanged);
            m_color2.eventSelectedColorChanged += new PropertyChangedEventHandler<Color>(OnColorChanged);
            m_color3.eventSelectedColorChanged += new PropertyChangedEventHandler<Color>(OnColorChanged);

            m_color0_hex.eventTextSubmitted += new PropertyChangedEventHandler<string>(OnColorHexSubmitted);
            m_color1_hex.eventTextSubmitted += new PropertyChangedEventHandler<string>(OnColorHexSubmitted);
            m_color2_hex.eventTextSubmitted += new PropertyChangedEventHandler<string>(OnColorHexSubmitted);
            m_color3_hex.eventTextSubmitted += new PropertyChangedEventHandler<string>(OnColorHexSubmitted);

            m_enabled.eventCheckChanged += new PropertyChangedEventHandler<bool>(OnCheckChanged);
            m_addBackEngine.eventCheckChanged += new PropertyChangedEventHandler<bool>(OnCheckChanged);

            m_clearVehicles.eventClick += new MouseEventHandler(OnClearVehicleClicked);
            m_clearParked.eventClick += new MouseEventHandler(OnClearVehicleClicked);
        }

        protected void OnCheckChanged(UIComponent component, bool state)
        {
            if (m_options.enabled != m_enabled.isChecked)
            {
                m_options.enabled = m_enabled.isChecked;
                eventEnableStateChanged(this, state);
            }
            else
                m_options.addBackEngine = m_addBackEngine.isChecked;

            AdvancedVehicleOptions.ApplyOptions(m_options);
        }

        protected void OnMaxSpeedSubmitted(UIComponent component, string text)
        {
            m_options.maxSpeed = float.Parse(text);

            AdvancedVehicleOptions.ApplyOptions(m_options);
        }

        protected void OnColorChanged(UIComponent component, Color color)
        {
            if (m_options == null) return;

            m_options.color0 = m_color0.selectedColor;
            m_options.color1 = m_color1.selectedColor;
            m_options.color2 = m_color2.selectedColor;
            m_options.color3 = m_color3.selectedColor;

            m_color0_hex.text = m_options.color0.ToString();
            m_color1_hex.text = m_options.color1.ToString();
            m_color2_hex.text = m_options.color2.ToString();
            m_color3_hex.text = m_options.color3.ToString();

            AdvancedVehicleOptions.ApplyOptions(m_options);
        }

        protected void OnColorHexSubmitted(UIComponent component, string text)
        {
            if (m_options == null) return;

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

            AdvancedVehicleOptions.ApplyOptions(m_options);
        }

        protected void OnClearVehicleClicked(UIComponent component, UIMouseEventParameter p)
        {
            if (m_options == null) return;

            AdvancedVehicleOptions.ClearVehicles(m_options, component == m_clearParked);
        }


    }

}
