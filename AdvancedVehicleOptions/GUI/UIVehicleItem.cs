using UnityEngine;
using ColossalFramework.UI;

namespace AdvancedVehicleOptions.GUI
{
    public class UIVehicleItem : UIPanel
    {
        private UISprite m_icon;
        private UILabel m_name;

        private VehicleOptions m_options;
        private UIOptionPanel m_optionPanel;

        public VehicleOptions options
        {
            get { return m_options; }
            set { m_options = value; }
        }

        public UIOptionPanel optionPanel
        {
            get { return m_optionPanel; }
            set { m_optionPanel = value; }
        }

        public override void Start()
        {
            base.Start();

            backgroundSprite = "UnlockingPanel2";
            isVisible = true;
            canFocus = true;
            isInteractive = true;
            width = parent.width;
            height = 40;

            padding = new RectOffset(10, 0, 10, 10);

            autoLayout = true;
            autoLayoutDirection = LayoutDirection.Horizontal;
            autoLayoutStart = LayoutStart.TopLeft;
            autoLayoutPadding = new RectOffset(0, 10, 0, 0);

            m_icon = AddUIComponent<UISprite>();
            m_icon.spriteName = options.icon;

            m_name = AddUIComponent<UILabel>();
            m_name.textScale = 0.9f;
            m_name.text = m_options.localizedName;
            if (m_options.enabled)
                m_name.textColor = new Color32(255, 255, 255, 255);
            else
                m_name.textColor = new Color32(255, 255, 255, 128);

            m_optionPanel.eventEnableStateChanged += new PropertyChangedEventHandler<bool>(RefreshState);
        }

        public override void OnDestroy()
        {
            base.OnDestroy();

            m_optionPanel.eventEnableStateChanged -= new PropertyChangedEventHandler<bool>(RefreshState);
        }

        protected override void OnClick(UIMouseEventParameter p)
        {
            base.OnClick(p);

            m_optionPanel.Show(options);
        }

        private void RefreshState(UIComponent component, bool state)
        {
            if (m_options.enabled)
                m_name.textColor = new Color32(255, 255, 255, 255);
            else
                m_name.textColor = new Color32(255, 255, 255, 128);
        }
    }
}
