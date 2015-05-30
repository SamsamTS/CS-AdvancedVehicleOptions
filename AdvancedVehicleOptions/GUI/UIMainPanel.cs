using UnityEngine;
using ColossalFramework.UI;

using System;

namespace AdvancedVehicleOptions.GUI
{
    public class UIMainPanel : UIPanel
    {
        private UITitleBar m_title;
        private UIScrollablePanel m_scrollablePanel;
        private UIPanel m_panelForScrollPanel;
        private UIButton m_reload;
        private UIButton m_save;

        private UISprite m_button;

        private UIOptionPanel m_optionPanel;
        private VehicleOptions[] m_optionsList;

        public static readonly string[] vehicleIconList =
            { "IconCitizenVehicle", "IconServiceVehicle", "IconServiceVehicle", "IconCargoShip",
              "ToolbarIconPolice", "InfoIconFireSafety", "ToolbarIconHealthcare", "InfoIconGarbage",
              "SubBarPublicTransportBus", "SubBarPublicTransportMetro", "SubBarPublicTransportTrain",
              "SubBarPublicTransportShip", "SubBarPublicTransportPlane" };

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

            backgroundSprite = "UnlockingPanel2";
            isVisible = false;
            canFocus = true;
            isInteractive = true;
            width = 450;
            height = 395;
            relativePosition = new Vector3(Mathf.Floor((GetUIView().fixedWidth - width - 315) / 2), Mathf.Floor((GetUIView().fixedHeight - height) / 2));

            m_optionPanel = (UIOptionPanel)GetUIView().AddUIComponent(typeof(UIOptionPanel));

            SetupControls();

            PopulateList();

            UITabstrip toolStrip = GetUIView().FindUIComponent<UITabstrip>("MainToolstrip");
            m_button = toolStrip.AddUIComponent<UISprite>();
            m_button.spriteName = "IconCitizenVehicle";
            m_button.size = m_button.spriteInfo.pixelSize;
            m_button.relativePosition = new Vector3(0, 5);

            GetUIView().FindUIComponent<UITabContainer>("TSContainer").AddUIComponent<UIPanel>().color = new Color32(0, 0, 0, 0);

            m_button.eventClick += new MouseEventHandler((c, p) =>
            {
                if (p != null) p.Use();
                isVisible = !isVisible;
                
                m_optionPanel.isVisible = false;
                OnSelectedItemChanged(null, null);
            });
        }

        public override void OnDestroy()
        {
            base.OnDestroy();

            m_button.parent.RemoveUIComponent(m_button);
            Destroy(m_button);
            Destroy(m_optionPanel);
        }

        private void SetupControls()
        {
            float offset = 40f;

            // Title Bar
            m_title = AddUIComponent<UITitleBar>();
            m_title.iconSprite = "IconCitizenVehicle";
            m_title.title = "Advanced Vehicle Options";

            // Scroll Panel (from Extended Public Transport UI)
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
            m_scrollablePanel.autoLayoutPadding = new RectOffset(0, 0, 0, 0);
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
            thumbSprite.width = thumbSprite.parent.width - 8;
            thumbSprite.spriteName = "ScrollbarThumb";

            scrollbar.thumbObject = thumbSprite;

            m_scrollablePanel.verticalScrollbar = scrollbar;
            m_scrollablePanel.eventMouseWheel += (component, param) =>
            {
                var sign = Mathf.Sign(param.wheelDelta);
                m_scrollablePanel.scrollPosition += new Vector2(0, sign * (-1) * 40);
            };

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

            // Event handlers
            m_title.closeButton.eventClick += new MouseEventHandler((c, t) => { m_optionPanel.isVisible = false; });
            m_optionPanel.eventEnableCheckChanged += new PropertyChangedEventHandler<bool>(OnEnableStateChanged);
            m_reload.eventClick += new MouseEventHandler((c, t) => AdvancedVehicleOptions.LoadConfig());
            m_save.eventClick += new MouseEventHandler((c, t) => AdvancedVehicleOptions.SaveConfig());
        }

        private UIVehicleItem[] m_itemList;

        private void PopulateList()
        {
            ClearList();

            if (m_optionsList == null || m_scrollablePanel == null) return;

            m_itemList = new UIVehicleItem[m_optionsList.Length];
            for (int i = 0; i < m_optionsList.Length; i++)
            {
                m_itemList[i] = m_scrollablePanel.AddUIComponent<UIVehicleItem>();

                if ((i % 2) == 1)
                {
                    m_itemList[i].background.backgroundSprite = "UnlockingItemBackground";
                    m_itemList[i].background.color = new Color32(0, 0, 0, 128);
                }

                m_itemList[i].eventClick += new MouseEventHandler(OnSelectedItemChanged);
                m_itemList[i].options = m_optionsList[i];
            }
        }

        private void ClearList()
        {
            if (m_itemList == null) return;

            for (int i = 0; i < m_itemList.Length; i++)
            {
                m_itemList[i].isVisible = false;
                RemoveUIComponent(m_itemList[i]);
            }

            m_itemList = null;
        }

        public void OnSelectedItemChanged(UIComponent component, UIMouseEventParameter p)
        {
            for (int i = 0; i < m_itemList.Length; i++)
            {
                if ((i % 2) == 1)
                {
                    m_itemList[i].background.backgroundSprite = "UnlockingItemBackground";
                    m_itemList[i].background.color = new Color32(0, 0, 0, 128);
                }
                else
                {
                    m_itemList[i].background.backgroundSprite = null;
                }

                m_itemList[i].Refresh();
            }

            if (component == null) return;
            UIVehicleItem item = (UIVehicleItem)component;
            item.background.backgroundSprite = "ListItemHighlight";
            item.background.color = new Color32(255, 255, 255, 255);

            m_optionPanel.Show(item.options);
            m_optionPanel.relativePosition = new Vector3(relativePosition.x + width + 5, relativePosition.y);
        }

        public void OnEnableStateChanged(UIComponent component, bool state)
        {
            for (int i = 0; i < m_itemList.Length; i++)
                m_itemList[i].Refresh();
        }
    }

}
