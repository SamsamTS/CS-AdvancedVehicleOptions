using ColossalFramework;
using ColossalFramework.UI;
using UnityEngine;

namespace AdvancedVehicleOptions.GUI
{
    public class UIWarningModal : UIPanel
    {
        private UITitleBar m_title;
        private UISprite m_warningIcon;
        private UILabel m_messageLabel;
        private UIButton m_ok;
        private UIButton m_cancel;

        private string m_message;

        private bool m_simulationState;

        private static UIWarningModal _instance;

        public static UIWarningModal instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = UIView.GetAView().AddUIComponent(typeof(UIWarningModal)) as UIWarningModal;
                }
                return _instance;
            }
        }

        public string message
        {
            get { return m_message; }
            set
            {
                m_message = value;
                if (m_messageLabel != null)
                {
                    m_messageLabel.text = m_message;
                    m_messageLabel.autoHeight = true;
                    m_messageLabel.width = width - m_warningIcon.width - 15;

                    height = m_title.height + 100 + Mathf.Max(90, m_messageLabel.height);

                    m_warningIcon.relativePosition = new Vector3(5, m_title.height + (height - m_title.height - 40 - m_warningIcon.height) / 2);
                    m_messageLabel.relativePosition = new Vector3(m_warningIcon.width + 10, m_title.height + (height - m_title.height - 40 - m_messageLabel.height) / 2);

                    m_ok.relativePosition = new Vector3(5, height - m_ok.height - 5);
                    m_cancel.relativePosition = new Vector3(width - m_cancel.width - 5, height - m_cancel.height - 5);
                }
            }
        }

        public override void Start()
        {
            base.Start();

            backgroundSprite = "UnlockingPanel2";
            isVisible = false;
            canFocus = true;
            isInteractive = true;
            clipChildren = true;
            width = 550;
            height = 300;
            relativePosition = new Vector3(Mathf.Floor((GetUIView().fixedWidth - width) / 2), Mathf.Floor((GetUIView().fixedHeight - height) / 2));

            // Title Bar
            m_title = AddUIComponent<UITitleBar>();
            m_title.title = "Advanced Vehicle Options";
            m_title.iconSprite = "IconCitizenVehicle";
            m_title.isModal = true;

            // Icon
            m_warningIcon = AddUIComponent<UISprite>();
            m_warningIcon.size = new Vector2(90, 90);
            m_warningIcon.spriteName = "IconWarning";
            m_warningIcon.relativePosition = new Vector3(5, m_title.height + (height - m_title.height - 40 - m_warningIcon.height) / 2);

            // Message
            m_messageLabel = AddUIComponent<UILabel>();
            m_messageLabel.wordWrap = true;

            // Ok
            m_ok = UIUtils.CreateButton(this);
            m_ok.text = "Yes";
            m_ok.relativePosition = new Vector3(5, height - m_ok.height - 5);

            m_ok.eventClick += (c, p) =>
            {
                Detour.RandomSpeed.enabled = true;
                Detour.RandomSpeed.highwaySpeed = true;

                AdvancedVehicleOptions.SaveConfig();

                UIView.PopModal();
                Hide();
            };

            // Cancel
            m_cancel = UIUtils.CreateButton(this);
            m_cancel.text = "No";
            m_cancel.relativePosition = new Vector3(width - m_cancel.width - 5, height - m_cancel.height - 5);

            m_cancel.eventClick += (c, p) =>
            {
                UIView.PopModal();
                Hide();
            };

            message = m_message;
            isVisible = true;
        }

        protected override void OnVisibilityChanged()
        {
            base.OnVisibilityChanged();

            UIComponent modalEffect = GetUIView().panelsLibraryModalEffect;

            if (isVisible)
            {
                m_simulationState = SimulationManager.instance.SimulationPaused;
                SimulationManager.instance.SimulationPaused = true;

                Focus();
                modalEffect.Show(false);
                ValueAnimator.Animate("NewThemeModalEffect", delegate(float val)
                {
                    modalEffect.opacity = val;
                }, new AnimatedFloat(0f, 1f, 0.7f, EasingType.CubicEaseOut));
            }
            else
            {
                SimulationManager.instance.SimulationPaused = m_simulationState;

                ValueAnimator.Animate("NewThemeModalEffect", delegate(float val)
                {
                    modalEffect.opacity = val;
                }, new AnimatedFloat(1f, 0f, 0.7f, EasingType.CubicEaseOut), delegate
                {
                    modalEffect.Hide();
                });
            }
        }

        protected override void OnKeyDown(UIKeyEventParameter p)
        {
            if (Input.GetKey(KeyCode.Escape) || Input.GetKey(KeyCode.Return))
            {
                p.Use();
                UIView.PopModal();
                Hide();
            }

            base.OnKeyDown(p);
        }
    }
}
