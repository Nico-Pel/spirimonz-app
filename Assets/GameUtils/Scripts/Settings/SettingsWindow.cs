using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace YsoCorp {
    namespace GameUtils {

        public class SettingsWindow : MonoBehaviour {
            private static Color COLOR_ON = Color.white;
            private static Color COLOR_OFF = new Color(0.7f, 0.7f, 0.7f, 1f);

            [Header("Scene References")]
            [SerializeField] private Button bClose;
            [SerializeField] private Button bCloseBlackBG;
            [SerializeField] private Button bRestorePurchase;
            [SerializeField] private Button bDataPrivacy;
            [SerializeField] private GameObject buttonsParent;
            [SerializeField] private GameObject panelBts;
            [SerializeField] private Button bLang;
            [SerializeField] private Image iLang;
            [SerializeField] private Transform langsContent;
            [SerializeField] private List<LangSelector> langSelectors;
            [SerializeField] private Text tVersion;
            [SerializeField] private Button bVersion;

            [SerializeField] internal SettingsDebugWindow debugWindow;

            private float _versionLastClick;
            private YCManager ycManager;    

            private void Awake() {
                this.ycManager = YCManager.instance;
                this.gameObject.SetActive(false);
            }

            public virtual void Init() {
                this.UpdateCanvasScaler();
                this.InitLangs();
                this.InitButtons();
                this.InitVersion();

                this.panelBts.gameObject.SetActive(this.bLang.gameObject.activeSelf);

                this.debugWindow.Init();
            }

            private void InitButtons() {
                this.bClose.onClick.AddListener(() => {
                    this.gameObject.SetActive(false);
                });

                this.bCloseBlackBG.onClick.AddListener(() => {
                    this.gameObject.SetActive(false);
                });

#if !IN_APP_PURCHASING || UNITY_ANDROID && !UNITY_EDITOR 
                this.bRestorePurchase.gameObject.SetActive(false);
#else
                this.bRestorePurchase.gameObject.SetActive(this.ycManager.ycConfig.HasInApps() && Application.isMobilePlatform);
#endif
                this.bRestorePurchase.onClick.AddListener(() => {
                    this.ycManager.inAppManager.RestorePurchases();
                });

                this.bDataPrivacy.onClick.AddListener(() => {
                    this.ycManager.adsManager.DisplayGDPR();
                });
            }

            private void InitVersion() {
                this.tVersion.text = "v" + Application.version + "  sdk" + YCConfig.VERSION;
                if (this.ycManager.abTestingManager.GetPlayerSample() != "") {
                    this.tVersion.text += " (" + this.ycManager.abTestingManager.GetPlayerSample() + ")";
                }
                this.bVersion.onClick.AddListener(() => {
                    if (Time.unscaledTime - this._versionLastClick < 0.3f) {
                        this.debugWindow.gameObject.SetActive(true);
                    }
                    this._versionLastClick = Time.unscaledTime;
                });
            }

            private void InitLangs() {
                int langCount = this.ycManager.i18nManager.i18NResourcesManager.i18ns.Count;
                this.bLang.gameObject.SetActive(langCount > 1);
                if (langCount > 1) {
                    this.iLang.sprite = this.ycManager.i18nManager.GetCurrentSprite();
                    this.bLang.onClick.AddListener(() => {
                        this.buttonsParent.SetActive(false);
                        this.langsContent.gameObject.SetActive(true);
                    });
                    foreach (KeyValuePair<string, Dictionary<string, string>> lang in this.ycManager.i18nManager.i18NResourcesManager.i18ns) {
                        LangSelector selector = this.langSelectors[0];
                        if (lang.Key != "EN") {
                            selector = Instantiate(this.langSelectors[0], langsContent);
                        }
                        selector.Init(lang.Key, () => {
                            this.iLang.sprite = this.ycManager.i18nManager.GetCurrentSprite();
                            this.buttonsParent.SetActive(true);
                            this.langsContent.gameObject.SetActive(false);
                        });
                    }
                }
            }

            private void UpdateCanvasScaler() {
                this.GetComponentInParent<CanvasScaler>().matchWidthOrHeight = (Screen.width > Screen.height) ? 0.69f : 0f;
            }

            public void Show() {
                this.gameObject.SetActive(true);
                this.buttonsParent.SetActive(true);
                this.langsContent.gameObject.SetActive(false);
            }
        }

    }
}
