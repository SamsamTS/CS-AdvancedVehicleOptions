using UnityEngine;
using ColossalFramework.UI;
using ColossalFramework.Steamworks;

using UIUtils = SamsamTS.UIUtils;

namespace AdvancedVehicleOptions.GUI
{
    public class UIVehicleItem : UIPanel, IUIFastListRow
    {
        private UISprite m_icon;
        private UISprite m_disabled;
        private UILabel m_name;
        private UIPanel m_background;
        private UISprite m_steamIcon;

        private VehicleOptions m_options;

        public VehicleOptions options
        {
            get { return m_options; }
            set { m_options = value; }
        }

        public UIPanel background
        {
            get
            {
                if (m_background == null)
                {
                    m_background = AddUIComponent<UIPanel>();
                    m_background.width = width;
                    m_background.height = 40;
                    m_background.relativePosition = Vector2.zero;

                    m_background.zOrder = 0;
                }

                return m_background;
            }
        }
        
        public override void Start()
        {
            base.Start();

            isVisible = true;
            canFocus = true;
            isInteractive = true;
            width = parent.width;
            height = 40;

            m_icon = AddUIComponent<UISprite>();

            m_disabled = AddUIComponent<UISprite>();
            m_disabled.spriteName = "Niet";
            m_disabled.size = m_disabled.spriteInfo.pixelSize;
            UIUtils.ResizeIcon(m_disabled, new Vector2(32, 32));
            m_disabled.relativePosition = new Vector3(10, Mathf.Floor((height - m_disabled.height) / 2));

            m_name = AddUIComponent<UILabel>();
            m_name.textScale = 0.9f;
            m_name.relativePosition = new Vector3(55, 13);

            m_steamIcon = AddUIComponent<UISprite>();
            m_steamIcon.spriteName = "SteamWorkshop";
            m_steamIcon.isVisible = false;
            m_steamIcon.relativePosition = new Vector3(width - 45, 12.5f);

            UIUtils.ResizeIcon(m_steamIcon, new Vector2(25, 25));

            if (Steam.IsOverlayEnabled())
            {
                m_steamIcon.eventClick += (c, p) =>
                {
                    p.Use();
                    Steam.ActivateGameOverlayToWorkshopItem(new PublishedFileId(ulong.Parse(m_options.steamID)));
                };
            }
        }

        protected override void OnClick(UIMouseEventParameter p)
        {
            base.OnClick(p);

            m_name.textColor = new Color32(255, 255, 255, 255);
        }

        public void Display(object data, bool isRowOdd)
        {
            m_options = data as VehicleOptions;

            if (m_icon == null || m_options == null) return;

            m_icon.spriteName = UIMainPanel.vehicleIconList[(int)m_options.category];
            m_icon.size = m_icon.spriteInfo.pixelSize;
            UIUtils.ResizeIcon(m_icon, new Vector2(32, 32));
            m_icon.relativePosition = new Vector3(10, Mathf.Floor((height - m_icon.height) / 2));

            m_name.text = m_options.localizedName;

            m_disabled.isVisible = !options.enabled;
            m_name.textColor = options.enabled ? new Color32(255, 255, 255, 255) : new Color32(128, 128, 128, 255);

            m_steamIcon.tooltip = m_options.steamID;
            m_steamIcon.isVisible = m_options.steamID != null;

            if (isRowOdd)
            {
                background.backgroundSprite = "UnlockingItemBackground";
                background.color = new Color32(0, 0, 0, 128);
            }
            else
            {
                background.backgroundSprite = null;
            }
        }

        public void Select(bool isRowOdd)
        {
            if (m_icon == null || m_options == null) return;

            m_name.textColor = new Color32(255, 255, 255, 255);

            background.backgroundSprite = "ListItemHighlight";
            background.color = new Color32(255, 255, 255, 255);
        }

        public void Deselect(bool isRowOdd)
        {
            if (m_icon == null || m_options == null) return;

            m_name.textColor = options.enabled ? new Color32(255, 255, 255, 255) : new Color32(128, 128, 128, 255);

            if (isRowOdd)
            {
                background.backgroundSprite = "UnlockingItemBackground";
                background.color = new Color32(0, 0, 0, 128);
            }
            else
            {
                background.backgroundSprite = null;
            }
        }

    }
}
