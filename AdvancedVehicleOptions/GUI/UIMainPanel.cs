using UnityEngine;
using ColossalFramework.UI;

namespace AdvancedVehicleOptions.GUI
{
    public class UIMainPanel : UIPanel
    {
        private UITitleBar m_title;
        private UIScrollablePanel m_scrollablePanel;
        private UIPanel m_panelForScrollPanel;
        private UIButton m_cancel;
        private UIButton m_apply;

        private UIOptionPanel m_optionPanel;
        private VehicleOptions[] m_optionsList;

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
                PopulateList();
            }
        }

        public override void Start()
        {
            base.Start();

            backgroundSprite = "UnlockingPanel2";
            isVisible = true;
            canFocus = true;
            isInteractive = true;
            width = 450;
            height = 400;
            relativePosition = new Vector3(Mathf.Floor((GetUIView().fixedWidth - width - 315) / 2), Mathf.Floor((GetUIView().fixedHeight - height) / 2));

            m_optionPanel = (UIOptionPanel)GetUIView().AddUIComponent(typeof(UIOptionPanel));

            SetupControls();

            PopulateList();
        }

        public override void OnDestroy()
        {
            base.OnDestroy();

            Destroy(m_optionPanel);
        }

        private void SetupControls()
        {
            float offset = 40f;

            m_title = AddUIComponent<UITitleBar>();
            m_title.iconSprite = "IconCitizenVehicle";
            m_title.title = "Advanced Vehicle Options";

            m_panelForScrollPanel = AddUIComponent<UIPanel>();
            m_panelForScrollPanel.gameObject.AddComponent<UICustomControl>();

            m_panelForScrollPanel.backgroundSprite = "UnlockingPanel";
            m_panelForScrollPanel.width = width - 10;
            m_panelForScrollPanel.height = height - offset - 75;
            m_panelForScrollPanel.relativePosition = new Vector3(5, offset);

            m_scrollablePanel = m_panelForScrollPanel.AddUIComponent<UIScrollablePanel>();
            m_scrollablePanel.width = m_scrollablePanel.parent.width - 20f;
            m_scrollablePanel.height = m_scrollablePanel.parent.height;

            m_scrollablePanel.autoLayout = true;
            m_scrollablePanel.autoLayoutDirection = LayoutDirection.Vertical;
            m_scrollablePanel.autoLayoutStart = LayoutStart.TopLeft;
            m_scrollablePanel.autoLayoutPadding = new RectOffset(0, 0, 1, 1);
            m_scrollablePanel.clipChildren = true;

            m_scrollablePanel.pivot = UIPivotPoint.TopLeft;
            m_scrollablePanel.AlignTo(m_scrollablePanel.parent, UIAlignAnchor.TopLeft);

            UIScrollbar scrollbar = m_panelForScrollPanel.AddUIComponent<UIScrollbar>();
            scrollbar.width = scrollbar.parent.width - m_scrollablePanel.width;
            scrollbar.height = scrollbar.parent.height;
            scrollbar.orientation = UIOrientation.Vertical;
            scrollbar.pivot = UIPivotPoint.BottomLeft;
            scrollbar.AlignTo(scrollbar.parent, UIAlignAnchor.TopRight);
            scrollbar.minValue = 0;
            scrollbar.value = 0;
            scrollbar.incrementAmount = 50;

            UISlicedSprite tracSprite = scrollbar.AddUIComponent<UISlicedSprite>();
            tracSprite.relativePosition = Vector2.zero;
            tracSprite.autoSize = true;
            tracSprite.size = tracSprite.parent.size;
            tracSprite.fillDirection = UIFillDirection.Vertical;
            tracSprite.spriteName = "ScrollbarTrack";

            scrollbar.trackObject = tracSprite;

            UISlicedSprite thumbSprite = tracSprite.AddUIComponent<UISlicedSprite>();
            thumbSprite.relativePosition = Vector2.zero;
            thumbSprite.fillDirection = UIFillDirection.Vertical;
            thumbSprite.autoSize = true;
            thumbSprite.width = thumbSprite.parent.width;
            thumbSprite.spriteName = "ScrollbarThumb";

            scrollbar.thumbObject = thumbSprite;

            m_scrollablePanel.verticalScrollbar = scrollbar;
            m_scrollablePanel.eventMouseWheel += (component, param) =>
            {
                var sign = Mathf.Sign(param.wheelDelta);
                m_scrollablePanel.scrollPosition += new Vector2(0, sign * (-1) * 20);
            };

            UILabel configLabel = this.AddUIComponent<UILabel>();
            configLabel.text = "Configuration file:";
            configLabel.textScale = 0.9f;
            configLabel.relativePosition = new Vector3(10, height - 60);

            m_cancel = UIUtils.CreateButton(this);
            m_cancel.text = "Reload";
            m_cancel.relativePosition = new Vector3(10, height - 40);

            m_apply = UIUtils.CreateButton(this);
            m_apply.text = "Save";
            m_apply.relativePosition = new Vector3(105, height - 40);
        }

        private UIVehicleItem[] m_itemList;

        private void PopulateList()
        {
            ClearList();

            if (m_optionsList == null) return;

            m_itemList = new UIVehicleItem[m_optionsList.Length];
            for (int i = 0; i < m_optionsList.Length; i++)
            {
                m_itemList[i] = m_scrollablePanel.AddUIComponent<UIVehicleItem>();

                if ((i % 2) == 1)
                    m_itemList[i].backgroundSprite = "UnlockingItemBackground";

                m_itemList[i].eventClick += new MouseEventHandler(OnSelectedItemChanged);
                m_itemList[i].options = m_optionsList[i];
                m_itemList[i].optionPanel = m_optionPanel;
            }
        }

        private void ClearList()
        {
            if (m_itemList == null) return;

            for (int i = 0; i < m_itemList.Length; i++)
                Destroy(m_itemList[i]);
        }

        public void OnSelectedItemChanged(UIComponent component, UIMouseEventParameter p)
        {
            for (int i = 0; i < m_itemList.Length; i++)
            {
                if ((i % 2) == 1)
                    m_itemList[i].backgroundSprite = "UnlockingItemBackground";
                else
                    m_itemList[i].backgroundSprite = "";
            }

            ((UIVehicleItem)component).backgroundSprite = "UnlockingItemBackgroundPressed";
        }
    }

}
