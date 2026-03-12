using HutongGames.PlayMaker;
using MSCLoader;
using System.Collections.Generic;
using UnityEngine;

namespace MSCPocketItems
{
    public class MSCPocketItems : Mod
    {
        public override string ID => "MSCPocketItems";
        public override string Name => "Carry More Items";
        public override string Author => "teamteppy";
        public override string Version => "1.1";
        public override string Description => "Hold 3 items (Keybind: '3')";
        public override Game SupportedGames => Game.MySummerCar;

        private SettingsKeybind pocketKey;

        private FsmGameObject pickedObject;
        private FsmGameObject raycastHitObject;
        private FsmBool handEmpty;
        private FsmInt lenght;
        private Transform itemPivot;
        private PlayMakerFSM pickUpFsm;

        private const int MAX_POCKET_SLOTS = 3;
        private Stack<GameObject> pocket = new Stack<GameObject>();

        private Camera fpsCamera;

        private float pocketFullTimer = 0f;
        private float itemTooBigTimer = 0f;

        private string[] pocketWhitelist = new string[]
        {
            "envelope(xxxxx)",
            "notepad(itemx)",
            "shock absorber(Clone)",
            "tv remote control(itemx)",
            "steering column(Clone)",
            "trail arm rl(Clone)",
            "trail arm rr(Clone)",
            "drum brake(Clone)",
            "strut fr(Clone)",
            "strut fl(Clone)",
            "halfshaft(Clone)",
            "coil spring(Clone)",
            "wishbone fr(Clone)",
            "wishbone fl(Clone)",
            "steering rack(Clone)",
            "steering rod fl(Clone)",
            "steering rod fr(Clone)",
            "disc brake(Clone)",
            "spindle fr(Clone)",
            "spindle fl(Clone)",
            "dashboard meters(Clone)",
            "flashlight(itemx)",
            "main bearing1(Clone)",
            "main bearing(Clone)",
            "main bearing3(Clone)",
            "mudflap rr(Clone)",
            "mudflap rl(Clone)",
            "mudflap fr(Clone)",
            "mudflap fl(Clone)",
            "airfilter(Clone)",
            "rear light right(Clone)",
            "rear light left(Clone)",
            "spanner set(itemx)",
            "headlight left(Clone)",
            "headlight right(Clone)",
            "gasoline(itemx)",
            "clock gauge(Clone)",
            "hubcap(Clone)",
            "brake master cylinder(Clone)",
            "radio(Clone)",
            "wiring mess(itemx)",
            "brake lining(Clone)",
            "clutch lining(Clone)",
            "stock steering wheel(Clone)",
            "xmas lights(Clone)",
            "clutch master cylinder(Clone)",
            "starter(Clone)",
            "headers(Clone)",
            "electrics(Clone)",
            "fuel pump(Clone)",
            "water pump(Clone)",
            "water pump pulley(Clone)",
            "alternator(Clone)",
            "rocker shaft(Clone)",
            "main bearing2(Clone)",
            "distributor(Clone)",
            "oil filter(Clone)",
            "head gasket(Clone)",
            "flywheel(Clone)",
            "camshaft(Clone)",
            "camshaft gear(Clone)",
            "crankshaft pulley(Clone)",
            "clutch cover plate(Clone)",
            "clutch pressure plate(Clone)",
            "clutch disc(Clone)",
            "timing chain(Clone)",
            "engine plate(Clone)",
            "drive gear(Clone)",
            "piston1(Clone)",
            "piston2(Clone)",
            "piston3(Clone)",
            "piston4(Clone)",
            "timing cover(Clone)",
            "radiator hose3(Clone)",
            "radiator hose2(Clone)",
            "radiator hose1(Clone)",
            "battery(Clone)",
            "radiator(Clone)",
            "gear linkage(Clone)",
            "inspection cover(Clone)",
            "gear stick(Clone)",
            "fuel strainer(Clone)",
            "fuel tank pipe(Clone)",
            "exhaust muffler(Clone)",
            "handbrake(Clone)",
            "back panel(Clone)",
            "subwoofer panel(Clone)",
            "bumper front(Clone)",
            "bumper rear(Clone)",
            "grille(Clone)",
            "coolant(itemx)",
            "motor oil(itemx)",
            "two stroke fuel(itemx)",
            "fire extinguisher(itemx)",
            "seat cover suomi(Clone)",
            "shopping bag(itemx)",
            "ground coffee(itemx)",
            "milk(itemx)",
            "sugar(itemx)",
            "potato chips(itemx)",
            "alternator belt(Clone)",
            "car light bulb box(Clone)",
            "mosquito spray(itemx)",
            "cigarettes(itemx)",
            "brake fluid(itemx)",
            "spark plug box(Clone)",
            "juice(itemx)",
            "spray can(itemx)",
            "fuse package(Clone)",
            "yeast(itemx)",
            "r20 battery box(Clone)",
            "macaron box(itemx)",
            "sausages(itemx)",
            "pizza(itemx)",
            "empty(itemx)",
            "r20 battery(Clone)",
            "fuse(Clone)",
            "spark plug(Clone)",
            "light bulb(Clone)",
            "empty bottle(Clone)",
            "basketball(Clone)",
            "helmet(itemx)",
            "fur dices(Clone)",
        };

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
                        if (v.Name == "PickedObject") { pickedObject = v; }
                        if (v.Name == "RaycastHitObject") { raycastHitObject = v; }
                    }

                    foreach (var v in fsm.FsmVariables.BoolVariables)
                    {
                        if (v.Name == "HandEmpty") { handEmpty = v; }
                    }

                    foreach (var v in fsm.FsmVariables.IntVariables)
                    {
                        if (v.Name == "Lenght") { lenght = v; }
                    }

                    break;
                }
            }

            itemPivot = player.transform.Find("Pivot/AnimPivot/Camera/FPSCamera/1Hand_Assemble/ItemPivot");
        }

        private void Mod_OnSave()
        {
            while (pocket.Count > 0)
            {
                GameObject item = pocket.Pop();

                item.transform.SetParent(null);
                item.layer = LayerMask.NameToLayer("Parts");

                Rigidbody rb = item.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = true;
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }

                item.transform.position = new Vector3(-8.42f, 0.2f, 9.29f);
            }
        }

        private void Mod_OnGUI()
        {
            if (itemTooBigTimer > 0f)
            {
                GUIStyle style = new GUIStyle();
                style.fontSize = 30;
                style.fontStyle = FontStyle.Bold;
                style.normal.textColor = Color.yellow;
                style.alignment = TextAnchor.UpperCenter;

                GUI.Label(new Rect(0, 60, Screen.width, 60), "Liikaa tavaraa! ):", style);
            }

            if (pocketFullTimer > 0f)
            {
                GUIStyle style = new GUIStyle();
                style.fontSize = 30;
                style.fontStyle = FontStyle.Bold;
                style.normal.textColor = Color.yellow;
                style.alignment = TextAnchor.UpperCenter;

                GUI.Label(new Rect(0, 60, Screen.width, 60), "Liikaa tavaraa! (3/3)", style);
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

            if (itemTooBigTimer > 0f)
            {
                itemTooBigTimer -= Time.deltaTime;
            }

            if (pocketFullTimer > 0f)
            {
                pocketFullTimer -= Time.deltaTime;
            }

            if (pocketKey.GetKeybindDown())
            {
                if (pocket.Count > 0 && pickedObject.Value == null)
                {
                    GameObject item = pocket.Pop();

                    item.transform.SetParent(itemPivot, true);
                    item.transform.localPosition = Vector3.zero;

                    pickedObject.Value = item;
                    raycastHitObject.Value = item;
                    handEmpty.Value = false;
                    lenght.Value = item.name.Length;

                    Rigidbody rb = item.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.isKinematic = false;
                        rb.velocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                    }

                    pickUpFsm.SendEvent("FINISHED");
                }
                else if (pickedObject.Value != null && pocket.Count < MAX_POCKET_SLOTS)
                {
                    GameObject held = pickedObject.Value;

                    if (!System.Array.Exists(pocketWhitelist, n => n == held.name))
                    {
                        itemTooBigTimer = 1f;
                        return;
                    }

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
                }
                else if (pickedObject.Value != null && pocket.Count >= MAX_POCKET_SLOTS)
                {
                    pocketFullTimer = 1f;
                }
            }
        }
    }
}