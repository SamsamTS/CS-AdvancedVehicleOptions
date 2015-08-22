using UnityEngine;
using ColossalFramework.UI;

namespace AdvancedVehicleOptions.GUI
{
    public class UITitleBar : UIPanel
    {
        private UISprite m_icon;
        private UILabel m_title;
        private UIButton m_close;
        private UIDragHandle m_drag;

        public bool isModal = false;

        public string iconSprite
        {
            get { return m_icon.spriteName; }
            set {
                if (m_icon == null) return;
                m_icon.spriteName = value;

                if (m_icon.spriteInfo != null)
                {
                    m_icon.size = m_icon.spriteInfo.pixelSize;
                    UIUtils.ResizeIcon(m_icon, new Vector2(32, 32));
                    m_icon.relativePosition = new Vector3(10, Mathf.Floor((height - m_icon.height) / 2));
                }
            }
        }

        public UIButton closeButton
        {
            get { return m_close; }
        }

        public string title
        {
            get { return m_title.text; }
            set { m_title.text = value; }
        }

        public override void Awake()
        {
            base.Awake();

            m_icon = AddUIComponent<UISprite>();
            m_title = AddUIComponent<UILabel>();
            m_close = AddUIComponent<UIButton>();
            m_drag = AddUIComponent<UIDragHandle>();

            height = 40;
            width = 450;
            title = "(None)";
            iconSprite = "";
        }

        public override void Start()
        {
            base.Start();
            
            width = parent.width;
            relativePosition = Vector3.zero;
            isVisible = true;
            canFocus = true;
            isInteractive = true;

            m_drag.width = width - 50;
            m_drag.height = height;
            m_drag.relativePosition = Vector3.zero;
            m_drag.target = parent;

            m_icon.spriteName = iconSprite;
            m_icon.relativePosition = new Vector3(10, 0);

            m_title.relativePosition = new Vector3(50, 13);
            m_title.text = title;

            m_close.relativePosition = new Vector3(width - 35, 2);
            m_close.normalBgSprite = "buttonclose";
            m_close.hoveredBgSprite = "buttonclosehover";
            m_close.pressedBgSprite = "buttonclosepressed";
            m_close.eventClick += (component, param) =>
            {
                if (isModal)
                    UIView.PopModal();
                parent.Hide();
            };
        }
    }
}