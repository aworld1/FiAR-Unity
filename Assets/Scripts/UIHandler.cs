using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class UIHandler {

    public static void ShowInventory(Image primary, TMP_Text primaryText,
        Image secondary, TMP_Text secondaryText) {
        primary.sprite = Resources.Load<Sprite>("Images/" + (string)GameHandler.Data.PrimaryWeapon["name"]);
        secondary.sprite = Resources.Load<Sprite>("Images/" + (string)GameHandler.Data.SecondaryWeapon["name"]);
        if ((int) GameHandler.Data.PrimaryWeapon["reserve"] != -1) {
            primaryText.text = GameHandler.Data.PrimaryWeapon["ammo"] + " / " + GameHandler.Data.PrimaryWeapon["reserve"];
        }
        else {
            primaryText.text = "∞";
        }
        if ((int) GameHandler.Data.SecondaryWeapon["reserve"] != -1) {
            secondaryText.text = GameHandler.Data.SecondaryWeapon["ammo"] +" / " + GameHandler.Data.SecondaryWeapon["reserve"];
        }
        else {
            secondaryText.text = "∞";
        }
    }

    public static void ShowUseButton(Image use, Image outline) {
        var img = "Images/" + ((int) GameHandler.Data.PrimaryWeapon["mag"] < 0 ? "Fist" : "fire-gun");
        var fill = 1f;
        var color = Color.black;
        color.a = 0.5f;
        if ((bool) GameHandler.Data.PrimaryWeapon["reloading"]) {
            var time = GPS.CurrentTime() - (int) GameHandler.Data.PrimaryWeapon["reloadStart"];
            if (time >= (int)GameHandler.Data.PrimaryWeapon["reload"]) {
                GameHandler.ReloadWeapon();
                if ((string) GameHandler.Data.PrimaryWeapon["name"] == "Shotgun" &&
                    !GameHandler.FullAmmo() && !GameHandler.EmptyReserve()) {
                    GameHandler.Data.PrimaryWeapon["reloadStart"] = GPS.CurrentTime();
                    PlayAudio(GameHandler.StaticAudio, "Reload/Shotgun");
                }
                else {
                    if ((string) GameHandler.Data.PrimaryWeapon["name"] == "Shotgun") {
                        PlayAudio(GameHandler.StaticAudio, "Reload/ShotgunPump");
                    }
                    GameHandler.Data.PrimaryWeapon["reloading"] = false;
                }
            }
            img = "Images/cancel";
            color = Color.red;
            fill = (float) time / (int) GameHandler.Data.PrimaryWeapon["reload"];
        }
        outline.fillAmount = fill;
        outline.color = color;
        use.sprite = Resources.Load<Sprite>(img);
    }

    public static void ShowTopBar(TMP_Text leader, TMP_Text kills, TMP_Text time) {
        leader.text = GameHandler.Data.LeaderKills + " / " + GameHandler.Data.KillsToWin;
        kills.text = GameHandler.Data.Kills + " / " + GameHandler.Data.KillsToWin;
        var timeLeft = GameHandler.Data.TimeLimit - (GPS.RealTime() - GameHandler.Data.StartTime);
        var min = timeLeft % 60 + "";
        if (timeLeft % 60 < 10) {
            min = "0" + min;
        }
        time.text = timeLeft / 60 + " : " + min;
    }

    public static void ShowHealth(Slider bar, TMP_Text hp) {
        bar.value = GameHandler.Data.Health;
        hp.text = GameHandler.Data.Health + "";
    }

    public static void PlayAudio(AudioSource audio, string path) {
        audio.PlayOneShot((AudioClip)Resources.Load("Audio/" + path));
    }

    public static void StopAudio(AudioSource audio) {
        audio.Stop();
    }
}