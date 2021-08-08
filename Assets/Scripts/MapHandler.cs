using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public class MapHandler : MonoBehaviour {

    public GameObject map;
    private RectTransform mapTransform;
    public GameObject player;
    private RectTransform playerTransform;
    public GameObject weaponSample;
    public GameObject weaponsContainer;
    private List<WeaponObject> weaponObjs;
    public GameObject primaryWeapon;
    public GameObject secondaryWeapon;
    public Image primaryImage;
    public Image secondaryImage;
    public GameObject nearbySample;
    public GameObject nearbyContainer;
    private int weaponSelected;
    private static bool updatedNearby;
    private double[] prevLocs;
    public static bool PickupUpdate;
    private bool pickingUp;
    private bool firstAwake;
    private bool resetting;

    private void Start() {
        prevLocs = new[] { GPS.Instance.latitude, GPS.Instance.longitude };
        weaponSelected = 1;
        resetting = false;
        updatedNearby = false;
        pickingUp = false;
        PickupUpdate = false;
        mapTransform = map.GetComponent<RectTransform>();
        playerTransform = player.GetComponent<RectTransform>();
        ResetWeaponObjs();
    }

    private void Awake() {
        firstAwake = true;
    }

    private void Update() {
        ClearWeapons();
        ShowPlayer();
        HandleWeapons();
        ShowOwnedWeapons();
        ShowNearbyWeapons();
        ShowEnemies();
    }

    private void ClearWeapons() {
        foreach(Transform child in weaponsContainer.transform) {
            Destroy(child.gameObject);
        }
    }

    private void ShowPlayer() {
        player.transform.SetParent(map.transform);
        playerTransform.anchoredPosition = new Vector2(0, 0);
        playerTransform.transform.eulerAngles = Vector3.forward * -Input.compass.trueHeading;
    }

    public void SetSelectedWeapon(int num) {
        weaponSelected = num;
    }

    private void ShowEnemies() {
        foreach (var t in GameHandler.Data.PlayerInfo) {
            if (t.Key == GameHandler.Data.PlayerName) continue;
            var val = (Dictionary<string, object>) t.Value;
            var lat = double.Parse(val["lat"].ToString(), System.Globalization.NumberStyles.Float);
            var lon = double.Parse(val["long"].ToString(), System.Globalization.NumberStyles.Float);
            var pos = GPS.GetRelativeMapPosition(new[] {GPS.Instance.latitude, GPS.Instance.longitude},
                new[] {lat, lon}, ServerHandler.MapSize);
            if (math.abs(pos[0]) > 1 || math.abs(pos[1]) > 1) {
                continue;
            }
            var enemy = Instantiate(weaponSample, weaponsContainer.transform, true);
            enemy.transform.localScale = new Vector3(0.7f, 0.7f, 1);
            var rect = mapTransform.rect;
            var scaledPos = new[] { pos[0] * rect.height / -2.2f, pos[1] * rect.width / 2.2f };
            var comp = enemy.GetComponent<RectTransform>();
            comp.anchoredPosition = new Vector2((float)scaledPos[1],(float)scaledPos[0]);
            enemy.GetComponent<Image>().sprite = Resources.Load<Sprite>("Images/dot");
            enemy.GetComponent<Image>().color = Color.red;
        }
    }

    private void ShowOwnedWeapons() {
        var primaryObj = primaryWeapon;
        var secondObj = secondaryWeapon;
        if (weaponSelected != 1) {
            primaryObj = secondaryWeapon;
            secondObj = primaryWeapon;
        }
        primaryObj.GetComponent<Outline>().effectColor = new Color32(10, 87, 0, 128);
        primaryObj.GetComponent<Image>().color = new Color32(167, 255, 148, 255);
        secondObj.GetComponent<Outline>().effectColor = new Color32(0, 0, 0, 128);
        secondObj.GetComponent<Image>().color = new Color32(255, 255, 255, 255);
        primaryImage.sprite = Resources.Load<Sprite>("Images/" +
                                                     (string)GameHandler.Data.PrimaryWeapon["name"]);
        secondaryImage.sprite = Resources.Load<Sprite>("Images/" +
                                                       (string)GameHandler.Data.SecondaryWeapon["name"]);
    }

    private void HandleWeapons() {
        if (GPS.Instance.latitude == 0 && GPS.Instance.longitude == 0) {
            return;
        }
        const double tolerance = 1E-10;
        if (PickupUpdate) {
            ResetWeaponObjs();
            SetWeaponObjTargets();
            updatedNearby = false;
            PickupUpdate = false;
        }
        else if (firstAwake || Math.Abs(prevLocs[0] - GPS.Instance.latitude) > tolerance ||
            Math.Abs(prevLocs[1] - GPS.Instance.longitude) > tolerance) {
            if (prevLocs[0] == 0 && prevLocs[1] == 0) {
                ResetWeaponObjs();
            }
            else {
                SetWeaponObjTargets();
            }
            prevLocs = new[] { GPS.Instance.latitude, GPS.Instance.longitude };
            updatedNearby = false;
            firstAwake = false;
        }
        else {
            MoveWeaponObjs();
        }
        ShowFloorWeapons();
    }

    private void ResetWeaponObjs() {
        if (resetting) return;
        resetting = true;
        weaponObjs = new List<WeaponObject>();
        foreach (var t in GameHandler.Data.FloorWeapons
            .Where(t => t != null && t.ContainsKey("lat"))) {
            t["lat"] = double.Parse(t["lat"].ToString(), System.Globalization.NumberStyles.Float);
            t["long"] = double.Parse(t["long"].ToString(), System.Globalization.NumberStyles.Float);
            var pos = GPS.GetRelativeMapPosition(new[] {GPS.Instance.latitude, GPS.Instance.longitude},
                new[] {(double)t["lat"], (double)t["long"]}, ServerHandler.MapSize);
            weaponObjs.Add(new WeaponObject((string)t["name"], pos[0], pos[1],
                (double)t["lat"], (double)t["long"]));
        }
        resetting = false;
    }

    private void SetWeaponObjTargets() {
        foreach (var t in weaponObjs) {
            var pos = GPS.GetRelativeMapPosition(new[] {GPS.Instance.latitude, GPS.Instance.longitude},
                new[] {t.RealPos[0], t.RealPos[1]}, ServerHandler.MapSize);
            t.TargetPos = new[] { pos[0], pos[1] };
        }
    }

    private void MoveWeaponObjs() {
        foreach (var t in weaponObjs) {
            t.CurrentPos[0] += (t.TargetPos[0] - t.CurrentPos[0]) * WeaponObject.GlideRate;
            t.CurrentPos[1] += (t.TargetPos[1] - t.CurrentPos[1]) * WeaponObject.GlideRate;
        }
    }

    private void ShowFloorWeapons() {
        foreach (var t in weaponObjs) {
            if (math.abs(t.CurrentPos[0]) > 1 || math.abs(t.CurrentPos[1]) > 1) {
                continue;
            }
            var weapon = Instantiate(weaponSample, weaponsContainer.transform, true);
            var img = "dot";
            if (math.sqrt(math.pow(t.CurrentPos[0], 2) + math.pow(t.CurrentPos[1], 2))
                < ServerHandler.RevealWeaponRange) {
                img = t.Name;
            }
            else {
                weapon.transform.localScale = new Vector3(0.3f, 0.3f, 1);
            }
            var rect = mapTransform.rect;
            var scaledPos = new[] { t.CurrentPos[0] * rect.height / -2.2f, t.CurrentPos[1] * rect.width / 2.2f };
            var comp = weapon.GetComponent<RectTransform>();
            comp.anchoredPosition = new Vector2((float)scaledPos[1], (float)scaledPos[0]);
            weapon.GetComponent<Image>().sprite = Resources.Load<Sprite>("Images/" + img);
        }
    }

    private void ShowNearbyWeapons() {
        if (updatedNearby) {
            return;
        }
        foreach(Transform child in nearbyContainer.transform) {
            Destroy(child.gameObject);
        }
        var numWeapons = 0;
        for (var i = 0; i < weaponObjs.Count; i++) {
            if (!(GPS.DistanceBetweenPoints(GPS.Instance.latitude, GPS.Instance.longitude,
                weaponObjs[i].RealPos[0], weaponObjs[i].RealPos[1]) <= ServerHandler.PickupRange)
                || numWeapons >= 6) continue;
            var weapon = Instantiate(nearbySample, nearbyContainer.transform, true);
            var button = weapon.GetComponent<Button>();
            var j = i;
            var comp = weapon.GetComponent<RectTransform>();
            comp.anchoredPosition = new Vector2(45 + numWeapons % 3 * 70, -30 - numWeapons / 3 * 70);
            button.onClick.AddListener(() => NearbyResponse(j));
            weapon.transform.GetChild(0).gameObject.GetComponent<Image>().sprite = Resources.Load<Sprite>("Images/" + weaponObjs[i].Name);
            numWeapons++;
        }
        updatedNearby = true;
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public async void NearbyResponse(int i) {
        if (pickingUp) return;
        pickingUp = true;
        await GameHandler.PickupWeapon(i, weaponSelected - 1);
        pickingUp = false;
    }
}

public class WeaponObject {
    public readonly double[] CurrentPos;
    public double[] TargetPos;
    public readonly double[] RealPos;
    public readonly string Name;
    public const double GlideRate = 0.3;

    public WeaponObject(string name, double cLat, double cLong, double rLat, double rLong) {
        Name = name;
        CurrentPos = new[] { cLat, cLong };
        TargetPos = new[] { cLat, cLong };
        RealPos = new[] { rLat, rLong };
    }
}