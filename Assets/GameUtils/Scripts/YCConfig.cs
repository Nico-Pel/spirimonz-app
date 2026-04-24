using UnityEngine;
using System;
using UnityEngine.Serialization;

namespace YsoCorp {
    namespace GameUtils {

        [CreateAssetMenu(fileName = "YCConfigData", menuName = "YsoCorp/Configuration", order = 1)]
        public sealed class YCConfig : ScriptableObject {

            public static string VERSION = "2.14.2";

            [Serializable]
            public struct DataData {
                public InfosData data;
            }

            [Serializable]
            public struct InfosData {
                public string key;
                public bool isYsocorp;
                public string name;
                public string android_key;
                public string ios_bundle_id;
                public string ios_key;
                public string facebook_app_id;
                public string facebook_client_token;
                public string admob_android_app_id;
                public string admob_ios_app_id;
                public string google_services_ios;
                public string google_services_android;
                public ApplovinData applovin;
                public MmpsData mmps;
                public APSIOSData aps_ios;
                public APSAndroidData aps_android;
            }

            // APPLOVIN
            [Serializable]
            public struct ApplovinData {
                public ApplovinAdUnitsData adunits;
            }
            [Serializable]
            public struct ApplovinAdUnitsData {
                public ApplovinAdUnitsOsData ios;
                public ApplovinAdUnitsOsData android;
            }
            [Serializable]
            public struct ApplovinAdUnitsOsData {
                public string interstitial;
                public string rewarded;
                public string banner;
            }

            [Serializable]
            public struct MmpData {
                public bool active;
            }
            [Serializable]
            public struct AdjustMmpData {
                public bool active;
                public string app_token;
            }

            [Serializable]
            public struct MmpsData {
                public AdjustMmpData adjust;
                public MmpData tenjin;
            }

            // APS
            [Serializable]
            public struct APSIOSData {
                public string app_id;
                public string interstitial;
                public string rewarded;
                public string banner;
            }

            [Serializable]
            public struct APSAndroidData {
                public string app_id;
                public string interstitial;
                public string rewarded;
                public string banner;
            }

            [Flags]
            public enum YCLogCategories {
                GUResourceLoad = 1,
                GURequests = 2,
                ApplovinMax = 4
            }

            [Header("------------------------------- CONFIG")]
            public string gameYcId;

#if IN_APP_PURCHASING
            [Header("InApp")]
            public string InAppRemoveAds;
            public bool InAppRemoveAdsCanRemoveInBanner = true;
            [HideInInspector] [Obsolete("Will be removed. Please use YCConfig.CustomInapps instead")] public string[] InAppConsumables;
            public InAppManager.CustomInapp[] CustomInapps;
            public bool InappDebug = true;
#else
            [Header("InApp (Enable & Import InApp in Service)")]
            [YcReadOnly] public string InAppRemoveAds;
            [YcReadOnly] public bool InAppRemoveAdsCanRemoveInBanner;
            [HideInInspector] [Obsolete("Will be removed. Please use YCConfig.CustomInapps instead")] public string[] InAppConsumables { get; set; } = { };
            public InAppManager.CustomInapp[] CustomInapps { get; set; } = { };
            [YcReadOnly] public bool InappDebug = true;
#endif

            [Header("A/B Tests")]
            public uint ABVersion = 1;
            public string ABForcedSample = "";
            public string[] ABSamples = { };
            public bool ABDebugLog = true;

            [Header("Ads")]
            [FormerlySerializedAs("BannerDisplayOnInit")] public bool UseBanners = true;
            [YcBoolShow("UseBanners", true)] [FormerlySerializedAs("BannerDisplayOnInitEditor")] public bool UseBannersInEditor = true;
            [YcBoolShow("UseBanners", true)] [field: SerializeField] public bool TransparentBannerBackground { get; private set; }
            [Space(10)]
            public float InterstitialInterval = 20;
            public bool rewardedResetDelay = false;
            [SerializeField] internal bool useCustomFirstInterDelay = false;
            [SerializeField, YcBoolShow("useCustomFirstInterDelay", true)] internal float firstInterstitialDelay = 300;
            [field: SerializeField] public bool PauseGameDuringAd { get; private set; } = true;


            [Header("System Logs")]
            public YCLogCategories activeLogs;

            [Header("Customization")]
            [SerializeField] public bool overrideSettingsWindow;
            [YcBoolShow("overrideSettingsWindow", true)] public SettingsWindow newSettingsWindowPrefab;

            [Header("------------------------------- AUTO IMPORT")]

            [YcReadOnly] public string Name;
            [Space(10)]
            [YcReadOnly] public string FbAppId;
            [YcReadOnly] public string FbClientToken;
            [YcReadOnly] public string appleId = "";

            [Header("Mmp")]
            [YcReadOnly] public bool MmpTenjin = true;

            [Header("Google AdMob")]
            [YcReadOnly] public string AdMobAndroidAppId = "";
            [YcReadOnly] public string AdMobIosAppId = "";

            [Header("Max AppLovin")]
            [YcReadOnly] public string IosInterstitial;
            [YcReadOnly] public string IosRewarded;
            [YcReadOnly] public string IosBanner;
            [Space(5)]
            [YcReadOnly] public string AndroidInterstitial;
            [YcReadOnly] public string AndroidRewarded;
            [YcReadOnly] public string AndroidBanner;

            [Header("Amazon")]
            [HideInInspector] public string AmazonIosAppID;
            [HideInInspector] public string AmazonIosInterstitial;
            [HideInInspector] public string AmazonIosRewarded;
            [HideInInspector] public string AmazonIosBanner;
            [Space(5)]
            [HideInInspector] public string AmazonAndroidAppID;
            [HideInInspector] public string AmazonAndroidInterstitial;
            [HideInInspector] public string AmazonAndroidRewarded;
            [HideInInspector] public string AmazonAndroidBanner;

            public string deviceKey => SystemInfo.deviceUniqueIdentifier;

            public static YCConfig Create() {
                return Resources.Load<YCConfig>("YCConfigData");
            }

            public void LogWarning(string msg) {
                Debug.LogWarning("[GameUtils][CONFIG]" + msg);
            }

            public bool HasInApps() {
#if IN_APP_PURCHASING
                if (this.InAppRemoveAds != null && this.InAppRemoveAds != "") {
                    return true;
                }
                if (this.CustomInapps.Length > 0) {
                    return true;
                }
#endif
                return false;
            }

        }

    }

}
