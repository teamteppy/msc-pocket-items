using HutongGames.PlayMaker;
using MSCLoader;
using System.Collections.Generic;
using UnityEngine;

namespace MSCPocketItems
{
    public class MSCPocketItems : Mod
    {
        public override string ID => "MSCPocketItems";
        public override string Name => "MSCPocketItems";
        public override string Author => "teamteppy";
        public override string Version => "1.0";
        public override string Description => "";
        public override Game SupportedGames => Game.MySummerCar;

        private PlayMakerGlobals globals;
        private SettingsKeybind debugKey;
        private SettingsKeybind pocketKey;

        private FsmGameObject pickedObject;
        private Transform itemPivot;
        private PlayMakerFSM pickUpFsm;

        private const int MAX_POCKET_SLOTS = 3;
        private Stack<GameObject> pocket = new Stack<GameObject>();

        private Camera fpsCamera;

        private float pocketFullTimer = 0f;

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
            SetupFunction(Setup.ModSettings, Mod_Settings);
        }

        private void Mod_Settings()
        {
            debugKey = Keybind.Add("DebugKey", "Debug Game", KeyCode.Alpha9);
            pocketKey = Keybind.Add("PocketKey", "Pocket Item", KeyCode.Alpha3);
        }

        private void Mod_OnLoad()
        {
            fpsCamera = GameObject.Find("FPSCamera").GetComponent<Camera>();
            GameObject player = GameObject.Find("PLAYER");

            foreach (var fsm in player.GetComponentsInChildren<PlayMakerFSM>())
            {
                if (fsm.FsmName == "PickUp")
                {
                    pickUpFsm = fsm;
                    foreach (var v in fsm.FsmVariables.GameObjectVariables)
                    {
                        if (v.Name == "PickedObject")
                        {
                            pickedObject = v;
                            LogToFile("pickedObject reference cached successfully.");
                            break;
                        }
                    }

                    break;
                }
            }

            if (pickedObject == null)
            {
                LogToFile("ERROR: could not find PickedObject FSM variable.");
            }

            itemPivot = GameObject.Find("ItemPivot").transform;

            if (itemPivot == null)
            {
                LogToFile("ERROR: could not find ItemPivot.");
            }
            else
            {
                LogToFile("ItemPivot cached successfully.");
            }
        }

        private void Mod_OnSave()
        {
            // Called once, when save and quit
            // Serialize your save file here.
        }

        private void Mod_OnGUI()
        {
            if (pocketFullTimer > 0f)
            {
                GUIStyle style = new GUIStyle();
                style.fontSize = 60;
                style.fontStyle = FontStyle.Bold;
                style.normal.textColor = Color.white;
                style.alignment = TextAnchor.UpperCenter;

                GUI.Label(new Rect(0, 60, Screen.width, 60), "Pocket full!", style);
            }
        }

        private void Mod_Update()
        {
            if (pocket.Count > 0)
            {
                int i = 0;
                foreach (GameObject item in pocket)
                {
                    Vector3 viewportPos = fpsCamera.ViewportToWorldPoint(
                        new Vector3(0.1f, 0.5f - (i * 0.1f), 0.5f)
                    );
                    item.transform.position = viewportPos;
                    i++;
                }
            }

            if (pocketFullTimer > 0f)
            {
                pocketFullTimer -= Time.deltaTime;
            }

            if (debugKey.GetKeybindDown())
            {

                // Log envelope state if it exists in the scene
                GameObject envelope = GameObject.Find("envelope(xxxxx)");
                if (envelope != null)
                {
                    LogToFile($"--- envelope state ---");
                    LogToFile($"  Position: {envelope.transform.position}");
                    LogToFile($"  Parent: {(envelope.transform.parent != null ? envelope.transform.parent.name : "null")}");
                    LogToFile($"  Active: {envelope.activeSelf}");
                    LogToFile($"  Layer: {LayerMask.LayerToName(envelope.layer)}");
                    LogToFile($"  Tag: {envelope.tag}");
                    foreach (var c in envelope.GetComponentsInChildren<Collider>())
                    {
                        LogToFile($"  Collider: {c.name} enabled={c.enabled} isTrigger={c.isTrigger}");
                    }
                    foreach (var r in envelope.GetComponentsInChildren<Renderer>())
                    {
                        LogToFile($"  Renderer: {r.name} enabled={r.enabled}");
                    }
                    Rigidbody rb = envelope.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        LogToFile($"  RB: isKinematic={rb.isKinematic} sleeping={rb.IsSleeping()}");
                    }
                }
                else
                {
                    LogToFile("  envelope not found in scene");
                }
            }

            if (pocketKey.GetKeybindDown())
            {
                if (pocket.Count > 0 && pickedObject.Value == null)
                {
                    GameObject item = pocket.Pop();

                    foreach (var r in item.GetComponentsInChildren<Renderer>())
                    {
                        r.enabled = true;
                    }

                    foreach (var c in item.GetComponentsInChildren<Collider>())
                    {
                        c.enabled = true;
                    }

                    pickedObject.Value = null;
                    pickUpFsm.SendEvent("DROP_PART");

                    item.transform.SetParent(null);
                    item.layer = LayerMask.NameToLayer("Parts");

                    GameObject player = GameObject.Find("PLAYER");
                    item.transform.position = player.transform.position
                        + player.transform.forward * 0.8f
                        + Vector3.up * 1.6f;

                    Rigidbody rb = item.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.isKinematic = false;
                        rb.velocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                        rb.WakeUp();
                    }

                    LogToFile($"Item retrieved: {item.name} | Stack size: {pocket.Count}");
                }
                else if (pickedObject.Value != null && pocket.Count < MAX_POCKET_SLOTS)
                {
                    if (pocket.Count >= MAX_POCKET_SLOTS)
                    {
                        pocketFullTimer = 3f;
                    }
                    else
                    {
                        GameObject held = pickedObject.Value;

                        Rigidbody rb = held.GetComponent<Rigidbody>();
                        if (rb != null)
                        {
                            rb.isKinematic = true;
                            rb.velocity = Vector3.zero;
                            rb.angularVelocity = Vector3.zero;
                        }

                        held.transform.SetParent(fpsCamera.transform);
                        held.transform.localRotation = Quaternion.Euler(20f, 120f, 0f);
                        pickedObject.Value = null;
                        pickUpFsm.SendEvent("DROP_PART");
                        pocket.Push(held);

                        LogToFile($"Item pocketed: {held.name} | Stack size: {pocket.Count}");
                    }
                }
                else if (pickedObject.Value != null && pocket.Count >= MAX_POCKET_SLOTS)
                {
                    // holding something but pocket full
                    pocketFullTimer = 3f;
                }
                else
                {
                    LogToFile("Not holding anything and pocket is empty.");
                }
            }

        }

    }
}