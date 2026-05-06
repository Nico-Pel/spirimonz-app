using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif
using UnityEditor.Android;
using System.Collections.Generic;
using System;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using System.Linq;
using UnityEngine;
using System.Xml.Linq;

namespace YsoCorp {

    namespace GameUtils {
        public class GameUtilsProcessor : IPreprocessBuildWithReport, IPostGenerateGradleAndroidProject {

            public int callbackOrder {
                get { return int.MaxValue; }
            }

            private static Dictionary<string, string> attLocalizations = new Dictionary<string, string>() {
                {"ar", "يستخدم هذا فقط معلومات الجهاز لمزيد من الإعلانات الشيقة وذات الصلة"},
                {"da", "Dette bruger kun enhedsoplysninger til mere interessante og relevante annoncer"},
                {"de", "\\\"Erlauben\\\" drücken benutzt Gerätinformationen für relevantere Werbeinhalte"},
                {"en", "This only uses device info for more interesting and relevant ads"},
                {"es", "Presionando \\\"Permitir\\\", se usa la información del dispositivo para obtener contenido publicitario más relevante"},
                {"fr", "\\\"Autoriser\\\" permet d'utiliser les infos du téléphone pour afficher des contenus publicitaires plus pertinents"},
                {"it", "Questo utilizza solo le informazioni sul dispositivo per annunci più interessanti e pertinenti"},
                {"ja", "\\\"許可\\\"をクリックすることで、デバイス情報を元により最適な広告を表示することができます"},
                {"ko", "\\\"허용\\\"을 누르면 더 관련성 높은 광고 콘텐츠를 제공하기 위해 기기 정보가 사용됩니다"},
                {"pt", "Isso usa apenas informações do dispositivo para anúncios mais interessantes e relevantes"},
                {"ru", "Это использует только информацию об устройстве для более интересной и релевантной рекламы."},
                {"vi", "Điều này chỉ sử dụng thông tin thiết bị cho các quảng cáo thú vị và phù hợp hơn."},
                {"zh-Hans", "点击\\\"允许\\\"以使用设备信息获得更加相关的广告内容"},
                {"zh-Hant", "點擊\\\"允許\\\"以使用設備信息獲得更加相關的廣告內容"}
            };

            public void OnPreprocessBuild(BuildReport report) {

                YCConfig ycConfig = YCConfig.Create();
                if (ycConfig.gameYcId == "") {
                    string error = "[GameUtils] Empty Game Yc Id";
                    if (this.IsBuildAndRun(report)) {
                        Debug.LogWarning(error);
                    } else {
                        throw new Exception(error);
                    }
                }
                bool hasFacebookConfig = ycConfig.FbAppId != "" && ycConfig.FbClientToken != "";
                if (ycConfig.FbAppId == "" || ycConfig.FbClientToken == "") {
                    Debug.LogWarning("[GameUtils] Facebook config is incomplete. Facebook initialization will be skipped for this build.");
                }
#if UNITY_IOS
                if (this.IsAnyGoogleInstalled() && ycConfig.AdMobAndroidAppId == "") {
                    throw new BuildFailedException("[GameUtils] Empty AdMob IOS Id");
                } else if (this.IsAnyGoogleInstalled() == false && ycConfig.AdMobAndroidAppId != "") {
                    throw new BuildFailedException("[GameUtils] AdMob IOS Id found but the network is not installed");
                }
                if (ycConfig.IosInterstitial == "" || ycConfig.IosRewarded == "" || ycConfig.IosBanner == "") {
                    string error = "[GameUtils] Empty iOS Ad Units";
                    if (this.IsBuildAndRun(report)) {
                        Debug.LogWarning(error);
                    } else {
                        throw new Exception(error);
                    }
                }
#elif UNITY_ANDROID
                if (this.IsAnyGoogleInstalled() && ycConfig.AdMobAndroidAppId == "") {
                    throw new BuildFailedException("[GameUtils] Empty AdMob Android Id");
                } else if (this.IsAnyGoogleInstalled() == false && ycConfig.AdMobAndroidAppId != "") {
                    throw new BuildFailedException("[GameUtils] AdMob Android Id found but the network is not installed");
                }
                if (ycConfig.AndroidInterstitial == "" || ycConfig.AndroidRewarded == "" || ycConfig.AndroidBanner == "") {
                    string error = "[GameUtils] Empty Android Ad Units";
                    if (this.IsBuildAndRun(report)) {
                        Debug.LogWarning(error);
                    } else {
                        throw new Exception(error);
                    }
                }
#endif
                if (hasFacebookConfig) {
                    YCConfigEditor.InitFacebook(ycConfig);
                }
                YCConfigEditor.InitMax(ycConfig);
            }

            private bool IsAnyGoogleInstalled() {
                return File.Exists("Assets/MaxSdk/Mediation/Google/Editor/Dependencies.xml") || File.Exists("Assets/MaxSdk/Mediation/GoogleAdManager/Editor/Dependencies.xml");
            }

            private bool IsBuildAndRun(BuildReport report) {
                return report.summary.options.HasFlag(BuildOptions.AutoRunPlayer);
            }

            private void GradleReplaces(string path, string file, List<KeyValuePair<string, string>> replaces) {
                try {
                    string gradleBuildPath = Path.Combine(path, file);
                    string content = File.ReadAllText(gradleBuildPath);
                    foreach (KeyValuePair<string, string> r in replaces) {
                        content = content.Replace(r.Key, r.Value);
                    }
                    File.WriteAllText(gradleBuildPath, content);
                } catch { }
            }

            private void RemoveLinesContaining(string filePath, params string[] markers)
            {
                try
                {
                    if (!File.Exists(filePath))
                        return;

                    string[] lines = File.ReadAllLines(filePath);
                    string[] filtered = lines
                        .Where(line => markers.All(marker => line.Contains(marker) == false))
                        .ToArray();

                    if (filtered.Length != lines.Length)
                        File.WriteAllLines(filePath, filtered);
                }
                catch { }
            }

            private void RemoveMavenRepositoryBlocksContaining(string filePath, params string[] markers)
            {
                try
                {
                    if (!File.Exists(filePath))
                        return;

                    string content = File.ReadAllText(filePath);
                    foreach (string marker in markers)
                    {
                        if (string.IsNullOrEmpty(marker))
                            continue;

                        while (true)
                        {
                            int markerIndex = content.IndexOf(marker, StringComparison.Ordinal);
                            if (markerIndex < 0)
                                break;

                            int blockStart = content.LastIndexOf("maven {", markerIndex, StringComparison.Ordinal);
                            if (blockStart < 0)
                                break;

                            int blockEnd = content.IndexOf("\n        }", markerIndex, StringComparison.Ordinal);
                            if (blockEnd < 0)
                                break;

                            blockEnd += "\n        }".Length;
                            content = content.Remove(blockStart, blockEnd - blockStart);
                        }
                    }

                    File.WriteAllText(filePath, content);
                }
                catch { }
            }

            private void RemoveFacebookManifestEntries(string manifestPath)
            {
                try
                {
                    if (!File.Exists(manifestPath))
                        return;

                    XDocument document = XDocument.Load(manifestPath);
                    XNamespace androidNs = "http://schemas.android.com/apk/res/android";
                    XElement application = document.Root?.Element("application");
                    if (application == null)
                        return;

                    string[] facebookActivities =
                    {
                        "com.facebook.unity.FBUnityLoginActivity",
                        "com.facebook.unity.FBUnityDialogsActivity",
                        "com.facebook.unity.FBUnityGamingServicesFriendFinderActivity",
                        "com.facebook.unity.FBUnityAppLinkActivity",
                        "com.facebook.unity.FBUnityDeepLinkingActivity",
                        "com.facebook.unity.FBUnityGameRequestActivity",
                        "com.facebook.unity.FBUnityCreateGameGroupActivity",
                        "com.facebook.unity.FBUnityJoinGameGroupActivity"
                    };

                    string[] facebookMetaNames =
                    {
                        "com.facebook.sdk.ApplicationId",
                        "com.facebook.sdk.AutoLogAppEventsEnabled",
                        "com.facebook.sdk.AdvertiserIDCollectionEnabled",
                        "com.facebook.sdk.ClientToken"
                    };

                    var toRemove = application.Elements()
                        .Where(element =>
                        {
                            string androidName = element.Attribute(androidNs + "name")?.Value;
                            string authorities = element.Attribute(androidNs + "authorities")?.Value;

                            if (element.Name.LocalName == "activity" && facebookActivities.Contains(androidName))
                                return true;

                            if (element.Name.LocalName == "meta-data" && facebookMetaNames.Contains(androidName))
                                return true;

                            if (element.Name.LocalName == "provider" &&
                                element.Attribute(androidNs + "name")?.Value == "com.facebook.FacebookContentProvider")
                                return true;

                            if (!string.IsNullOrEmpty(authorities) &&
                                authorities.StartsWith("com.facebook.app.FacebookContentProvider", StringComparison.Ordinal))
                                return true;

                            return false;
                        })
                        .ToList();

                    if (toRemove.Count == 0)
                        return;

                    foreach (XElement element in toRemove)
                        element.Remove();

                    document.Save(manifestPath);
                }
                catch { }
            }

            public void OnPostGenerateGradleAndroidProject(string path) {
#if UNITY_ANDROID
                YCConfig ycConfig = YCConfig.Create();
                bool hasFacebookConfig = ycConfig.FbAppId != "" && ycConfig.FbClientToken != "";

                this.GradleReplaces(path, "../build.gradle", new List<KeyValuePair<string, string>> {
                    new KeyValuePair<string, string>("com.android.tools.build:gradle:3.4.0", "com.android.tools.build:gradle:3.4.+")
                });
                this.GradleReplaces(path, "../unityLibrary/Tenjin/build.gradle", new List<KeyValuePair<string, string>> {
                    new KeyValuePair<string, string>("implementation fileTree(dir: 'libs', include: ['*.jar'])", "implementation fileTree(dir: 'libs', include: ['*.jar', '*.aar'])")
                });
                this.GradleReplaces(path, "../gradle.properties", new List<KeyValuePair<string, string>> {
                    new KeyValuePair<string, string>("android.enableJetifier=true", "android.enableJetifier=true\nandroid.suppressUnsupportedCompileSdk=35")
                });

                string rootPath = Path.GetFullPath(Path.Combine(path, ".."));
                string settingsGradlePath = Path.Combine(rootPath, "settings.gradle");
                string unityLibraryGradlePath = Path.Combine(rootPath, "unityLibrary", "build.gradle");
                string manifestPath = Path.Combine(rootPath, "unityLibrary", "src", "main", "AndroidManifest.xml");

                // These adapters are known to introduce sync or dexing failures on some Unity/Gradle exports.
                RemoveLinesContaining(
                    unityLibraryGradlePath,
                    "com.applovin.mediation:adjoeads-adapter",
                    "com.applovin.mediation:bidmachine-adapter",
                    "com.applovin.mediation:bigoads-adapter",
                    "com.applovin.mediation:bytedance-adapter",
                    "com.applovin.mediation:fyber-adapter",
                    "com.applovin.mediation:inmobi-adapter",
                    "com.applovin.mediation:line-adapter",
                    "com.applovin.mediation:mobilefuse-adapter",
                    "com.applovin.mediation:ogury-presage-adapter",
                    "com.applovin.mediation:verve-adapter",
                    "com.applovin.mediation:vungle-adapter",
                    "com.applovin.mediation:yandex-adapter",
                    "com.applovin.mediation:yso-network-adapter",
                    "io.adn:adn-applovin-adapter",
                    "com.facebook.android:facebook-",
                    "com.google.android.gms:play-services-ads-identifier",
                    "com.parse.bolts:bolts-android",
                    "com.squareup.picasso:picasso",
                    "com.tenjin:android-sdk"
                );
                RemoveMavenRepositoryBlocksContaining(
                    settingsGradlePath,
                    "https://releases.adjoe.io/maven",
                    "https://artifactory.bidmachine.io/bidmachine",
                    "https://artifact.bytedance.com/repository/pangle",
                    "https://maven.ogury.co",
                    "https://ysonetwork.s3.eu-west-3.amazonaws.com/sdk/android",
                    "https://framework.voodoo-adn.com/android/release/gaming",
                    "https://verve.jfrog.io/artifactory/verve-gradle-release"
                );
                RemoveLinesContaining(
                    unityLibraryGradlePath,
                    "com.applovin.mediation:facebook-adapter",
                    "facebook-android-wrapper"
                );
                RemoveFacebookManifestEntries(manifestPath);
#endif
            }

            [PostProcessBuild(int.MaxValue)]
            public static void ChangeXcodePlist(BuildTarget buildTarget, string path) {
                if (buildTarget == BuildTarget.iOS) {
#if UNITY_IOS
                    YCConfig ycConfig = YCConfig.Create();
                    string plistPath = path + "/Info.plist";
                    PlistDocument plist = new PlistDocument();
                    plist.ReadFromFile(plistPath);
                    PlistElementDict rootDict = plist.root;

                    PlistElementArray rootCapacities = (PlistElementArray)rootDict.values["UIRequiredDeviceCapabilities"];
                    rootCapacities.values.RemoveAll((PlistElement elem) => {
                        return elem.AsString() == "metal";
                    });

                    rootDict.SetString("NSCalendarsUsageDescription", "Used to deliver better advertising experience");
                    rootDict.SetString("NSLocationWhenInUseUsageDescription", "Used to deliver better advertising experience");
                    rootDict.SetString("NSPhotoLibraryUsageDescription", "Used to deliver better advertising experience");
                    rootDict.SetString("NSAdvertisingAttributionReportEndpoint", "https://tenjin-skan.com");
                    rootDict.SetString("NSUserTrackingUsageDescription", attLocalizations["en"]);
                    rootDict.SetBoolean("ITSAppUsesNonExemptEncryption", false);
                    rootDict.values.Remove("UIApplicationExitsOnSuspend");

                    //SKAdNetworks
                    plist.root.values.TryGetValue("SKAdNetworkItems", out PlistElement SKAdNetworkItems);
                    if (SKAdNetworkItems == null || SKAdNetworkItems.GetType() != typeof(PlistElementArray)) { // if the array does not exist, create it
                        SKAdNetworkItems = plist.root.CreateArray("SKAdNetworkItems");
                    }

                    bool hasYsoNetworkSKAd = false;
                    IEnumerable<PlistElement> SKAdNetworks = SKAdNetworkItems.AsArray().values.Where(plistElement => plistElement.GetType() == typeof(PlistElementDict));
                    foreach (PlistElement SKAdNetwork in SKAdNetworks) { // Check if the SKAdNetwork already exists
                        PlistElement current;
                        SKAdNetwork.AsDict().values.TryGetValue("SKAdNetworkIdentifier", out current);
                        if (current != null && current.GetType() == typeof(PlistElementString)) {
                            if (current.AsString() == "pt89h2hlb7.skadnetwork") {
                                hasYsoNetworkSKAd = true;
                                continue;
                            }
                        }

                        if (hasYsoNetworkSKAd) {
                            break;
                        }
                    }

                    if (hasYsoNetworkSKAd == false) {
                        PlistElementDict ysonetworkSKAd = SKAdNetworkItems.AsArray().AddDict();
                        ysonetworkSKAd.SetString("SKAdNetworkIdentifier", "pt89h2hlb7.skadnetwork");
                    }

                    File.WriteAllText(plistPath, plist.WriteToString());
#endif
                }
            }

            [PostProcessBuildAttribute(int.MaxValue - 1)]
            public static void ChangeXcodePBXProject(BuildTarget buildTarget, string path) {
#if UNITY_IOS
                string projectPath = PBXProject.GetPBXProjectPath(path);
                PBXProject project = new PBXProject();
                project.ReadFromFile(projectPath);

#if UNITY_2019_3_OR_NEWER
                string unityMainTargetGuid = project.GetUnityMainTargetGuid();
#else
                string unityMainTargetGuid = project.TargetGuidByName(UnityMainTargetName);
#endif
                foreach (KeyValuePair<string, string> attLocalization in attLocalizations) {
                    AddATTLocalization(attLocalization.Value, attLocalization.Key, path, project, unityMainTargetGuid);
                }

                string unityFrameworkTargetGuid = project.GetUnityFrameworkTargetGuid();
                project.SetBuildProperty(unityFrameworkTargetGuid, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "NO");


                project.WriteToFile(projectPath);
#endif
            }

#if UNITY_IOS
            private static void AddATTLocalization(string localizedATTDescription, string localeCode, string buildPath, PBXProject project, string targetGuid) {
                // Use the legacy resources directory name if the build is being appended (the "Resources" directory already exists if it is an incremental build).
                string resourcesDirectoryName = Directory.Exists(Path.Combine(buildPath, "Resources")) ? "Resources" : "GameUtilsResources";
                string resourcesDirectoryPath = Path.Combine(buildPath, resourcesDirectoryName);
                string localeSpecificDirectoryName = localeCode + ".lproj";
                string localeSpecificDirectoryPath = Path.Combine(resourcesDirectoryPath, localeSpecificDirectoryName);
                string infoPlistStringsFilePath = Path.Combine(localeSpecificDirectoryPath, "InfoPlist.strings");

                if (Directory.Exists(resourcesDirectoryPath) == false) {
                    Directory.CreateDirectory(resourcesDirectoryPath);
                }
                if (Directory.Exists(localeSpecificDirectoryPath) == false) {
                    Directory.CreateDirectory(localeSpecificDirectoryPath);
                }

                string localizedDescriptionLine = "\"NSUserTrackingUsageDescription\" = \"" + localizedATTDescription + "\";\n";

                if (File.Exists(infoPlistStringsFilePath)) {
                    List<string> output = new List<string>();
                    string[] lines = File.ReadAllLines(infoPlistStringsFilePath);
                    bool keyUpdated = false;
                    foreach (var line in lines) {
                        if (line.Contains("NSUserTrackingUsageDescription")) {
                            output.Add(localizedDescriptionLine);
                            keyUpdated = true;
                        } else {
                            output.Add(line);
                        }
                    }

                    if (!keyUpdated) {
                        output.Add(localizedDescriptionLine);
                    }

                    File.WriteAllText(infoPlistStringsFilePath, string.Join("\n", output.ToArray()) + "\n");
                } else {
                    File.WriteAllText(infoPlistStringsFilePath, "/* Localized versions of Info.plist keys - Generated by GameUtils */\n" + localizedDescriptionLine);
                }

                string localeSpecificDirectoryRelativePath = Path.Combine(resourcesDirectoryName, localeSpecificDirectoryName);
                string guid = project.AddFolderReference(localeSpecificDirectoryRelativePath, localeSpecificDirectoryRelativePath);
                project.AddFileToBuild(targetGuid, guid);
            }
#endif

            [PostProcessBuild(89)] //89 because Applovin runs their embedding script at 90
            public static void RemoveEmptyIphoneTarget(BuildTarget buildTarget, string path) {
#if UNITY_IOS
                string podfilePath = path + "/Podfile";
                string emptyIphoneTarget = "target 'Unity-iPhone' do\nend";
                string podFileContent = File.ReadAllText(podfilePath);
                if (podFileContent.Contains(emptyIphoneTarget)) {
                    podFileContent = podFileContent.Replace(emptyIphoneTarget, "");
                    File.WriteAllText(podfilePath, podFileContent);
                }
#endif
            }
        }

    }

}
