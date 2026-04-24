using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace YsoCorp {
    namespace GameUtils {
        public static class YCBootstrapper {

            [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
            public static void ExecuteYCManager() {
                if (YCManager.instance == null) {
                    Object.DontDestroyOnLoad(Object.Instantiate(Resources.Load("YCManager")));
                }
            }
        }
    }
}