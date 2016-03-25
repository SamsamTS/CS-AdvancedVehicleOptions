using UnityEngine;
using ColossalFramework.UI;
using ColossalFramework.Threading;

namespace AdvancedVehicleOptions.GUI
{
    public class UIOptionPanel : UIPanel
    {
        private UITextField m_maxSpeed;
        private UITextField m_acceleration;
        private UITextField m_braking;
        private UICheckBox m_useColors;
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
        private UITextField m_capacity;
        private UIButton m_restore;
        private UILabel m_removeLabel;
        private UIButton m_clearVehicles;
        private UIButton m_clearParked;

        public VehicleOptions m_options = null;

        private bool m_initialized = false;

        public event PropertyChangedEventHandler<bool> eventEnableCheckChanged;

        public override void Start()
        {
            base.Start();
            canFocus = true;
            isInteractive = true;
            width = 315;
            height = 330;

            SetupControls();

            m_options = new VehicleOptions();
        }

        public void Show(VehicleOptions options)
        {
            m_initialized = false;

            m_options = options;

            if (m_color0 == null) return;

            m_maxSpeed.text = Mathf.RoundToInt(options.maxSpeed * 5).ToString();
            m_acceleration.text = options.acceleration.ToString();
            m_braking.text = options.braking.ToString();
            m_useColors.isChecked = options.useColorVariations;
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
            m_addBackEngine.isVisible = (options.prefab.m_vehicleType == VehicleInfo.VehicleType.Train || options.prefab.m_vehicleType == VehicleInfo.VehicleType.Tram) && options.hasTrailer;

            m_capacity.text = options.capacity.ToString();
            m_capacity.parent.isVisible = options.hasCapacity;

            string name = options.localizedName;
            if (name.Length > 16) name = name.Substring(0, 16) + "...";
            m_removeLabel.text = "Remove vehicles (" + name + "):";

            (parent as UIMainPanel).ChangePreviewColor(m_color0.selectedColor);

            m_initialized = true;
        }

        private void SetupControls()
        {
            UIPanel panel = AddUIComponent<UIPanel>();
            panel.gameObject.AddComponent<UICustomControl>();

            panel.backgroundSprite = "UnlockingPanel";
            panel.width = width - 10;
            panel.height = height - 75;
            panel.relativePosition = new Vector3(5, 0);

            // Max Speed
            UILabel maxSpeedLabel = panel.AddUIComponent<UILabel>();
            maxSpeedLabel.text = "Maximum speed:";
            maxSpeedLabel.textScale = 0.9f;
            maxSpeedLabel.relativePosition = new Vector3(15, 15);

            m_maxSpeed = UIUtils.CreateTextField(panel);
            m_maxSpeed.numericalOnly = true;
            m_maxSpeed.width = 75;
            m_maxSpeed.tooltip = "Change the maximum speed of the vehicle\nPlease note that vehicles do not go beyond speed limits";
            m_maxSpeed.relativePosition = new Vector3(15, 35);

            UILabel kmh = panel.AddUIComponent<UILabel>();
            kmh.text = "km/h";
            kmh.textScale = 0.9f;
            kmh.relativePosition = new Vector3(95, 40);

            // Acceleration
            UILabel accelerationLabel = panel.AddUIComponent<UILabel>();
            accelerationLabel.text = "Acceleration/Brake:";
            accelerationLabel.textScale = 0.9f;
            accelerationLabel.relativePosition = new Vector3(160, 15);

            m_acceleration = UIUtils.CreateTextField(panel);
            m_acceleration.numericalOnly = true;
            m_acceleration.allowFloats = true;
            m_acceleration.width = 60;
            m_acceleration.tooltip = "Change the vehicle acceleration factor";
            m_acceleration.relativePosition = new Vector3(160, 35);

            // Braking
            m_braking = UIUtils.CreateTextField(panel);
            m_braking.numericalOnly = true;
            m_braking.allowFloats = true;
            m_braking.width = 60;
            m_braking.tooltip = "Change the vehicle braking factor";
            m_braking.relativePosition = new Vector3(230, 35);

            // Colors
            m_useColors = UIUtils.CreateCheckBox(panel);
            m_useColors.text = "Color variations:";
            m_useColors.isChecked = true;
            m_useColors.width = width - 40;
            m_useColors.tooltip = "Enable color variations\nA random color is chosen between the four following colors";
            m_useColors.relativePosition = new Vector3(15, 70);

            m_color0 = UIUtils.CreateColorField(panel);
            m_color0.name = "AVO-color0";
            m_color0.popupTopmostRoot = false;
            m_color0.relativePosition = new Vector3(13 , 95 - 2);
            m_color0_hex = UIUtils.CreateTextField(panel);
            m_color0_hex.maxLength = 6;
            m_color0_hex.relativePosition = new Vector3(55, 95);

            m_color1 = UIUtils.CreateColorField(panel);
            m_color1.name = "AVO-color1";
            m_color1.popupTopmostRoot = false;
            m_color1.relativePosition = new Vector3(13, 120 - 2);
            m_color1_hex = UIUtils.CreateTextField(panel);
            m_color1_hex.maxLength = 6;
            m_color1_hex.relativePosition = new Vector3(55, 120);

            m_color2 = UIUtils.CreateColorField(panel);
            m_color2.name = "AVO-color2";
            m_color2.popupTopmostRoot = false;
            m_color2.relativePosition = new Vector3(158, 95 - 2);
            m_color2_hex = UIUtils.CreateTextField(panel);
            m_color2_hex.maxLength = 6;
            m_color2_hex.relativePosition = new Vector3(200, 95);

            m_color3 = UIUtils.CreateColorField(panel);
            m_color3.name = "AVO-color3";
            m_color3.popupTopmostRoot = false;
            m_color3.relativePosition = new Vector3(158, 120 - 2);
            m_color3_hex = UIUtils.CreateTextField(panel);
            m_color3_hex.maxLength = 6;
            m_color3_hex.relativePosition = new Vector3(200, 120);

            // Enable & BackEngine
            m_enabled = UIUtils.CreateCheckBox(panel);
            m_enabled.text = "Allow this vehicle to spawn";
            m_enabled.isChecked = true;
            m_enabled.width = width - 40;
            m_enabled.tooltip = "Make sure you have at least one vehicle allowed to spawn for that category";
            m_enabled.relativePosition = new Vector3(15, 155); ;

            m_addBackEngine = UIUtils.CreateCheckBox(panel);
            m_addBackEngine.text = "Replace last car with engine";
            m_addBackEngine.isChecked = false;
            m_addBackEngine.width = width - 40;
            m_addBackEngine.tooltip = "Make the last car of this train be an engine";
            m_addBackEngine.relativePosition = new Vector3(15, 175);

            // Capacity
            UIPanel capacityPanel = panel.AddUIComponent<UIPanel>();
            capacityPanel.size = Vector2.zero;
            capacityPanel.relativePosition = new Vector3(15, 200);

            UILabel capacityLabel = capacityPanel.AddUIComponent<UILabel>();
            capacityLabel.text = "Capacity:";
            capacityLabel.textScale = 0.9f;
            capacityLabel.relativePosition = Vector3.zero;

            m_capacity = UIUtils.CreateTextField(capacityPanel);
            m_capacity.numericalOnly = true;
            m_capacity.width = 110;
            m_capacity.tooltip = "Change the capacity of the vehicle";
            m_capacity.relativePosition = new Vector3(0, 20);

            // Restore default
            m_restore = UIUtils.CreateButton(panel);
            m_restore.text = "Restore default";
            m_restore.width = 130;
            m_restore.tooltip = "Restore all values to default";
            m_restore.relativePosition = new Vector3(160, 215);

            // Remove Vehicles
            m_removeLabel = this.AddUIComponent<UILabel>();
            m_removeLabel.text = "Remove vehicles:";
            m_removeLabel.textScale = 0.9f;
            m_removeLabel.relativePosition = new Vector3(10, height - 60);

            m_clearVehicles = UIUtils.CreateButton(this);
            m_clearVehicles.text = "Driving";
            m_clearVehicles.width = 90f;
            m_clearVehicles.tooltip = "Remove all driving vehicles of that type\nHold the SHIFT key to remove all types";
            m_clearVehicles.relativePosition = new Vector3(10, height - 40);

            m_clearParked = UIUtils.CreateButton(this);
            m_clearParked.text = "Parked";
            m_clearParked.width = 90f;
            m_clearParked.tooltip = "Remove all parked vehicles of that type\nHold the SHIFT key to remove all types";
            m_clearParked.relativePosition = new Vector3(105, height - 40);

            panel.BringToFront();

            // Event handlers
            m_maxSpeed.eventTextSubmitted += OnMaxSpeedSubmitted;
            m_acceleration.eventTextSubmitted += OnAccelerationSubmitted;
            m_braking.eventTextSubmitted += OnBrakingSubmitted;

            m_useColors.eventCheckChanged += OnCheckChanged;

            MouseEventHandler mousehandler = (c, p) => { if (m_initialized) (parent as UIMainPanel).ChangePreviewColor((c as UIColorField).selectedColor); };

            m_color0.eventMouseEnter += mousehandler;
            m_color1.eventMouseEnter += mousehandler;
            m_color2.eventMouseEnter += mousehandler;
            m_color3.eventMouseEnter += mousehandler;

            m_color0_hex.eventMouseEnter += (c, p) => { if (m_initialized) (parent as UIMainPanel).ChangePreviewColor(m_color0.selectedColor); };
            m_color1_hex.eventMouseEnter += (c, p) => { if (m_initialized) (parent as UIMainPanel).ChangePreviewColor(m_color1.selectedColor); };
            m_color2_hex.eventMouseEnter += (c, p) => { if (m_initialized) (parent as UIMainPanel).ChangePreviewColor(m_color2.selectedColor); };
            m_color3_hex.eventMouseEnter += (c, p) => { if (m_initialized) (parent as UIMainPanel).ChangePreviewColor(m_color3.selectedColor); };

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

            m_capacity.eventTextSubmitted += OnCapacitySubmitted;

            m_restore.eventClick += (c, p) =>
            {
                m_initialized = false;
                bool isEnabled = m_options.enabled;
                DefaultOptions.Restore(m_options.prefab);
                AdvancedVehicleOptions.SaveConfig();
                VehicleOptions.UpdateTransfertVehicles();

                VehicleOptions.prefabUpdateEngine = m_options.prefab;
                VehicleOptions.prefabUpdateUnits = m_options.prefab;
                new EnumerableActionThread(VehicleOptions.UpdateBackEngines);
                new EnumerableActionThread(VehicleOptions.UpdateCapacityUnits);

                Show(m_options);

                if (m_options.enabled != isEnabled)
                    eventEnableCheckChanged(this, m_options.enabled);
            };

            m_clearVehicles.eventClick += OnClearVehicleClicked;
            m_clearParked.eventClick += OnClearVehicleClicked;
        }

        protected void OnCheckChanged(UIComponent component, bool state)
        {
            if (!m_initialized || m_options == null) return;
            m_initialized = false;

            if (component == m_enabled)
            {
                if (m_options.isTrailer)
                {
                    VehicleOptions engine = m_options.engine;

                    if (engine.enabled != state)
                    {
                        engine.enabled = state;
                        VehicleOptions.UpdateTransfertVehicles();
                        eventEnableCheckChanged(this, state);
                    }
                }
                else
                {
                    if (m_options.enabled != state)
                    {
                        m_options.enabled = state;
                        VehicleOptions.UpdateTransfertVehicles();
                        eventEnableCheckChanged(this, state);
                    }
                }

                if (!state && !AdvancedVehicleOptions.CheckServiceValidity(m_options.category))
                {
                    GUI.UIWarningModal.instance.message = UIMainPanel.categoryList[(int)m_options.category + 1] + " may not work correctly because no vehicles are allowed to spawn.";
                    UIView.PushModal(GUI.UIWarningModal.instance);
                    GUI.UIWarningModal.instance.Show(true);
                }
            }
            else if (component == m_addBackEngine && m_options.addBackEngine != state)
            {
                m_options.addBackEngine = m_addBackEngine.isChecked;
                VehicleOptions.prefabUpdateEngine = m_options.prefab;
                new EnumerableActionThread(VehicleOptions.UpdateBackEngines);
            }
            else if (component == m_useColors && m_options.useColorVariations != state)
            {
                m_options.useColorVariations = state;
                (parent as UIMainPanel).ChangePreviewColor(m_color0.selectedColor);
            }

            AdvancedVehicleOptions.SaveConfig();
            m_initialized = true;
        }

        protected void OnMaxSpeedSubmitted(UIComponent component, string text)
        {
            if (!m_initialized || m_options == null) return;
            m_initialized = false;

            m_options.maxSpeed = float.Parse(text) / 5f;

            AdvancedVehicleOptions.SaveConfig();
            m_initialized = true;
        }

        protected void OnAccelerationSubmitted(UIComponent component, string text)
        {
            if (!m_initialized || m_options == null) return;
            m_initialized = false;

            m_options.acceleration = float.Parse(text);

            AdvancedVehicleOptions.SaveConfig();
            m_initialized = true;
        }

        protected void OnBrakingSubmitted(UIComponent component, string text)
        {
            if (!m_initialized || m_options == null) return;
            m_initialized = false;

            m_options.braking = float.Parse(text);

            AdvancedVehicleOptions.SaveConfig();
            m_initialized = true;
        }

        protected void OnCapacitySubmitted(UIComponent component, string text)
        {
            if (!m_initialized || m_options == null) return;
            m_initialized = false;

            m_options.capacity = int.Parse(text);
            VehicleOptions.prefabUpdateUnits = m_options.prefab;
            new EnumerableActionThread(VehicleOptions.UpdateCapacityUnits);

            AdvancedVehicleOptions.SaveConfig();
            m_initialized = true;
        }

        protected void OnColorChanged(UIComponent component, Color color)
        {
            if (!m_initialized || m_options == null) return;
            m_initialized = false;

            (parent as UIMainPanel).ChangePreviewColor(color);

            m_options.color0 = m_color0.selectedColor;
            m_options.color1 = m_color1.selectedColor;
            m_options.color2 = m_color2.selectedColor;
            m_options.color3 = m_color3.selectedColor;

            m_color0_hex.text = m_options.color0.ToString();
            m_color1_hex.text = m_options.color1.ToString();
            m_color2_hex.text = m_options.color2.ToString();
            m_color3_hex.text = m_options.color3.ToString();

            AdvancedVehicleOptions.SaveConfig();
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

                m_initialized = true;
                return;
            }

            m_options.color0 = new HexaColor(m_color0_hex.text);
            m_options.color1 = new HexaColor(m_color1_hex.text);
            m_options.color2 = new HexaColor(m_color2_hex.text);
            m_options.color3 = new HexaColor(m_color3_hex.text);

            m_color0_hex.text = m_options.color0.ToString();
            m_color1_hex.text = m_options.color1.ToString();
            m_color2_hex.text = m_options.color2.ToString();
            m_color3_hex.text = m_options.color3.ToString();

            m_color0.selectedColor = m_options.color0;
            m_color1.selectedColor = m_options.color1;
            m_color2.selectedColor = m_options.color2;
            m_color3.selectedColor = m_options.color3;

            (parent as UIMainPanel).ChangePreviewColor(color);

            m_initialized = true;
        }

        protected void OnClearVehicleClicked(UIComponent component, UIMouseEventParameter p)
        {
            if (m_options == null) return;

            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                AdvancedVehicleOptions.ClearVehicles(null, component == m_clearParked);
            else
                AdvancedVehicleOptions.ClearVehicles(m_options, component == m_clearParked);
        }
    }

}
