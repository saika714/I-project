using System.IO;
using UnityEditor;

namespace SoftballConnect.Editor
{
    // Unity 6000.6 can index an empty database list when restoring Play Mode
    // before creating its per-user default index. Seed it before delayCall runs.
    internal static class SearchIndexSetup
    {
#if UNITY_6000_6
        const string IndexPath = "UserSettings/Search.index";

        [InitializeOnLoadMethod]
        static void EnsureDefaultIndex()
        {
            if (AssetDatabase.IsAssetImportWorkerProcess() || File.Exists(IndexPath)) return;

            Directory.CreateDirectory("UserSettings");
            File.WriteAllText(IndexPath, @"{
    ""name"": ""Assets"",
    ""roots"": [""Assets"", ""Packages""],
    ""includes"": [],
    ""excludes"": [""Assets/Temp/"", ""Assets/External/""],
    ""options"": {
        ""types"": true,
        ""properties"": true,
        ""extended"": false,
        ""dependencies"": true
    },
    ""baseScore"": 999
}");
        }
#endif
    }
}
