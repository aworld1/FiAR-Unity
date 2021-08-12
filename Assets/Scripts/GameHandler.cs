using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#pragma warning disable 108,114

public class GameHandler : MonoBehaviour {
    public static readonly GameData Data = new GameData();

    public AudioSource audio;
    public static AudioSource StaticAudio;
    public TMP_Text roomText;
    public Image primaryImage;
    public TMP_Text primaryAmmo;
    public Image secondaryImage;
    public TMP_Text secondaryAmmo;
    public Image useImage;
    public Image useOutline;
    public TMP_Text leaderText;
    public TMP_Text killsText;
    public TMP_Text timeText;
    public Slider healthBar;
    public TMP_Text healthPoints;
    public GameObject scopeButton;
    private void Start() {
        StaticAudio = audio;
        if (SceneManager.GetActiveScene() == SceneManager.GetSceneByName("Map Page")) return;
        roomText.text = Data.RoomCode ?? "N/A";
        ServerHandler.SubscribeToEvents(Data.RoomCode);
        if (Data.PrimaryWeapon != null) {
            ShowUI();
        }
    }

    private void Update() {
        if (Data.PrimaryWeapon != null && SceneManager.GetActiveScene() != SceneManager.GetSceneByName("Map Page")) {
            ShowUI();
        }
    }

    private void ShowUI() {
        UIHandler.ShowInventory(primaryImage, primaryAmmo, 
            secondaryImage, secondaryAmmo);
        UIHandler.ShowUseButton(useImage, useOutline);
        UIHandler.ShowTopBar(leaderText, killsText, timeText);
        UIHandler.ShowHealth(healthBar, healthPoints);
        UIHandler.HandleScopedWeapons(scopeButton);
    }

    public async void FireResponse() {
        if (Data.IsDead()) return;
        if ((bool) Data.PrimaryWeapon["reloading"]) {
            Data.PrimaryWeapon["reloading"] = false;
            UIHandler.StopAudio(audio);
            return;
        }
        if (GPS.CurrentTime() - (int)Data.PrimaryWeapon["lastFired"] < (int)Data.PrimaryWeapon["delay"]) {
            return;
        }
        if (FireWeapon()) {
            UIHandler.PlayAudio(audio, "Fire/" + (string)Data.PrimaryWeapon["name"]);
            Data.PrimaryWeapon["lastFired"] = GPS.CurrentTime();
            await Data.GetPlayerInfo();
            var hit = ServerHandler.FireWeapon(Data.PrimaryWeapon);
            if (hit) {
                await Task.Delay(100);
                UIHandler.PlayAudio(audio, "Noise/hit");
            }
            return;
        }
        UIHandler.PlayAudio(audio, "Noise/empty");
    }

    public void ReloadResponse() {
        if (Data.IsDead() || !RangedEquipped() || FullAmmo() || (bool)Data.PrimaryWeapon["reloading"]) {
            return;
        }
        if (EmptyReserve()) {
            UIHandler.PlayAudio(audio, "Noise/empty");
            return;
        }
        Data.PrimaryWeapon["reloadStart"] = GPS.CurrentTime();
        Data.PrimaryWeapon["reloading"] = true;
        UIHandler.PlayAudio(audio, "Reload/" + (string)Data.PrimaryWeapon["name"]);
    }

    public void SwitchResponse() {
        if (!Data.IsDead() && !(bool)Data.PrimaryWeapon["reloading"]) {
            SwitchWeapon();
        }
    }

    public void ScopedResponse() {
        Data.Scoped = true;
    }

    public void UnscopedResponse() {
        Data.Scoped = false;
    }
    
    public void MapResponse() {
        if (!Data.IsDead() && !(bool)Data.PrimaryWeapon["reloading"]) {
            SceneHandler.SwitchScene("Map Page");
        }
    }

    private static bool FireWeapon() {
        if (!RangedEquipped()) {
            return true;
        }
        if ((int) Data.PrimaryWeapon["ammo"] <= 0) {
            return false;
        }
        Data.PrimaryWeapon["ammo"] = (int) Data.PrimaryWeapon["ammo"] - 1;
        return true;
    }
    
    public static void ReloadWeapon() {
        if (FullAmmo() || EmptyReserve()) {
            return;
        }
        var reloadAmount = (int) Data.PrimaryWeapon["mag"] - (int) Data.PrimaryWeapon["ammo"];
        if ((string) Data.PrimaryWeapon["name"] == "Shotgun") {
            reloadAmount = 1;
        }
        if ((int) Data.PrimaryWeapon["reserve"] >= reloadAmount) {
            Data.PrimaryWeapon["ammo"] = (int) Data.PrimaryWeapon["ammo"] + reloadAmount;
            Data.PrimaryWeapon["reserve"] = (int) Data.PrimaryWeapon["reserve"] - reloadAmount;
            return;
        }
        Data.PrimaryWeapon["ammo"] = (int) Data.PrimaryWeapon["ammo"] + (int) Data.PrimaryWeapon["reserve"];
        Data.PrimaryWeapon["reserve"] = 0;
    }

    private static bool RangedEquipped() {
        return (int) Data.PrimaryWeapon["mag"] != -1;
    }

    public static bool FullAmmo() {
        return (int) Data.PrimaryWeapon["ammo"] == (int) Data.PrimaryWeapon["mag"];
    }

    public static bool EmptyReserve() {
        return (int) Data.PrimaryWeapon["reserve"] == 0;
    }

    private static void SwitchWeapon() {
        var temp = Data.PrimaryWeapon;
        Data.PrimaryWeapon = Data.SecondaryWeapon;
        Data.SecondaryWeapon = temp;
    }

    public static async Task PickupWeapon(int index, int slot) {
        Data.FloorWeapons[index]["lat"] = Convert.ToDouble(Data.FloorWeapons[index]["lat"]);
        Data.FloorWeapons[index]["long"] = Convert.ToDouble(Data.FloorWeapons[index]["long"]);
        var weapons = new[] { Data.PrimaryWeapon, Data.SecondaryWeapon };
        var droppedWeapon = Weapons.CreateWeapon(
            weapons[slot], (double)Data.FloorWeapons[index]["lat"], (double)Data.FloorWeapons[index]["long"]);
        var pickup = Data.FloorWeapons[index];
        await ServerHandler.PickupWeapon(Data.RoomCode, pickup,
            droppedWeapon).ContinueWith(
            result => {
                if (!result.Result) {
                    return;
                }
                if (slot == 0) {
                    Data.PrimaryWeapon = Weapons.CreateEquipped(pickup);
                }
                else {
                    Data.SecondaryWeapon = Weapons.CreateEquipped(pickup);
                }
            });
    }

    public static void PlayerDied() {
        Data.PrimaryWeapon["reloading"] = false;
        Data.SecondaryWeapon["reloading"] = false;
        UIHandler.StopAudio(StaticAudio);
        Data.Deaths++;
        Data.Health = 0;
        Data.DeathTime = GPS.CurrentTime() + ServerHandler.TimeToRespawn;
        SceneHandler.SwitchScene("Deathmatch Page");
    }

    public static double ClosestAngle() {
        var gyro = Input.compass.trueHeading;
        var closestAngle = 180d;
        foreach (var t in Data.PlayerInfo) {
            var pl = (Dictionary<string, object>) t.Value;
            if (t.Key == Data.PlayerName || pl["team"].ToString() == Data.Team) continue;
            var a = GPS.AngleBetweenPoints(GPS.Instance.latitude, GPS.Instance.longitude,
                Convert.ToDouble(pl["lat"]), Convert.ToDouble(pl["long"]));
            var d = GPS.DistanceBetweenPoints(GPS.Instance.latitude, GPS.Instance.longitude,
                Convert.ToDouble(pl["lat"]), Convert.ToDouble(pl["long"]));
            var floorAngle = Math.Min(Math.Abs(gyro - a), 360 - Math.Abs(gyro - a));
            if (floorAngle < closestAngle && d <= (int) Data.PrimaryWeapon["range"]) {
                closestAngle = floorAngle;
            }
        }
        return closestAngle;
    }
}
