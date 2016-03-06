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

        private string m_message;

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
                if(m_messageLabel != null)
                {
                    m_messageLabel.text = m_message;
                    m_messageLabel.autoHeight = true;
                    m_messageLabel.width = width - m_warningIcon.width - 15;

                    height = 200;

                    if ((m_title.height + 40 + m_messageLabel.height) > (height - 40))
                    {
                        height = m_title.height + 40 + m_messageLabel.height;
                    }

                    m_warningIcon.relativePosition = new Vector3(5, m_title.height + (height - m_title.height - 40 - m_warningIcon.height) / 2);
                    m_ok.relativePosition = new Vector3((width - m_ok.width) / 2, height - m_ok.height - 5);
                    m_messageLabel.relativePosition = new Vector3(m_warningIcon.width + 10, m_title.height + (height - m_title.height - 40 - m_messageLabel.height) / 2);
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
            width = 600;
            height = 200;
            relativePosition = new Vector3(Mathf.Floor((GetUIView().fixedWidth - width) / 2), Mathf.Floor((GetUIView().fixedHeight - height) / 2));

            // Title Bar
            m_title = AddUIComponent<UITitleBar>();
            m_title.title = "Advanced Vehicle Options - Warning";
            m_title.iconSprite = "IconCitizenVehicle";
            m_title.isModal = true;

            // Icon
            m_warningIcon = AddUIComponent<UISprite>();
            m_warningIcon.size = new Vector2(90, 90);
            m_warningIcon.spriteName = "IconWarning";

            // Message
            m_messageLabel = AddUIComponent<UILabel>();
            m_messageLabel.wordWrap = true;

            // Ok
            m_ok = UIUtils.CreateButton(this);
            m_ok.text = "OK";

            m_ok.eventClick += (c, p) =>
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
                Focus();
                if (modalEffect != null)
                {
                    modalEffect.Show(false);
                    ValueAnimator.Animate("NewThemeModalEffect", delegate(float val)
                    {
                        modalEffect.opacity = val;
                    }, new AnimatedFloat(0f, 1f, 0.7f, EasingType.CubicEaseOut));
                }
            }
            else if (modalEffect != null)
            {
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
