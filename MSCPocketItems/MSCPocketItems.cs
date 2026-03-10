using MSCLoader;
using UnityEngine;

namespace MSCPocketItems
{
    public class MSCPocketItems : Mod
    {
        public override string ID => "MSCPocketItems"; // Your (unique) mod ID 
        public override string Name => "MSCPocketItems"; // Your mod name
        public override string Author => "teamteppy"; // Name of the Author (your name)
        public override string Version => "1.0"; // Version
        public override string Description => ""; // Short description of your mod 
        public override Game SupportedGames => Game.MySummerCar;

        private PlayMakerGlobals globals;
        private SettingsKeybind debugKey;

        private void LogToFile(string message)
        {
            string path = Application.persistentDataPath + "/MSCMod_debug.txt";
            System.IO.File.AppendAllText(path, message + "\n");
        }
        private string GetGameObjectPath(GameObject go)
        {
            string path = go.name;
            Transform t = go.transform.parent;
            while (t != null)
            {
                path = t.name + "/" + path;
                t = t.parent;
            }
            return path;
        }

        public override void ModSetup()
        {
            SetupFunction(Setup.OnLoad, Mod_OnLoad);
            SetupFunction(Setup.OnSave, Mod_OnSave);
            SetupFunction(Setup.OnGUI, Mod_OnGUI);
            SetupFunction(Setup.Update, Mod_Update);
            SetupFunction(Setup.FixedUpdate, Mod_FixedUpdate);
            SetupFunction(Setup.ModSettings, Mod_Settings);
        }

        private void Mod_Settings()
        {
            debugKey = Keybind.Add("DebugKey", "Debug Game", KeyCode.Alpha9);
        }

        private void Mod_OnLoad()
        {
            globals = PlayMakerGlobals.Instance;
        }
        private void Mod_OnSave()
        {
            // Called once, when save and quit
            // Serialize your save file here.
        }
        private void Mod_OnGUI()
        {
            // Draw unity OnGUI() here
        }
        private void Mod_Update()
        {
            if (debugKey.GetKeybindDown())
            {
                //foreach (var v in globals.Variables.FloatVariables)
                //    LogToFile($"GLOBAL FloatVar: {v.Name.PadRight(30)} Val: {v.Value}");

                //foreach (var v in globals.Variables.IntVariables)
                //    LogToFile($"GLOBAL IntVar:   {v.Name.PadRight(30)} Val: {v.Value}");

                foreach (GameObject go in GameObject.FindObjectsOfType<GameObject>())
                {
                    string lower = go.name.ToLower();
                    if (lower.Contains("light"))
                    {
                        LogToFile($"[MATCH] {go.name} at path: {GetGameObjectPath(go)}");
                    }
                }

                //GameObject spannerSet = GameObject.Find("spanner set(itemx)");
                //if (spannerSet != null)
                //{
                //    LogToFile($"[FOUND] spanner set at: ITEMS/spanner set(itemx)");
                //    LogToFile($"Child count: {spannerSet.transform.childCount}");
                //    foreach (Transform child in spannerSet.transform)
                //    {
                //        LogToFile($"  Child: {child.name} active={child.gameObject.activeSelf}");
                //    }
                //}
                //else
                //{
                //    LogToFile("spanner set NOT found");
                //}

                //GameObject spannerSet = GameObject.Find("spanner set(itemx)");
                //if (spannerSet != null)
                //{
                //    Transform tools = spannerSet.transform.Find("Tools");
                //    if (tools != null)
                //    {
                //        LogToFile($"Tools child count: {tools.childCount}");
                //        foreach (Transform child in tools)
                //        {
                //            LogToFile($"  Child: {child.name} active={child.gameObject.activeSelf}");
                //        }
                //    }
                //}

            }

        }
        private void Mod_FixedUpdate()
        {
            // FixedUpdate is called once per fixed frame
        }
    }
}
