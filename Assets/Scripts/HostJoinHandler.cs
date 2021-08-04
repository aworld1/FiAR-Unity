using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;
using Random = UnityEngine.Random;

public class HostJoinHandler : MonoBehaviour {
    [Header("Host Page")]
    public TMP_InputField roomSize;
    public TMP_Dropdown gamemode;
    
    [Header("Join Page")]
    public TMP_InputField playerName;
    public TMP_InputField roomCode;
    public async void HostGame() {
        var sizeString = roomSize.text.Trim();
        var nm = playerName.text.Trim();
        var mode = gamemode.options[gamemode.value].text.Trim();
        if (sizeString == "" || nm == "" || mode == "") {
            Debug.Log("Empty fields!");
            return;
        }
        if (mode != "Deathmatch") {
            Debug.Log("Mode not supported yet!");
            return;
        }
        var code = "";
        await GetUniqueCode().ContinueWith(result => {
            code = result.Result;
        });
        var size = int.Parse(sizeString);
        var loc = new Dictionary<string, string> {
            ["lat"] = GPS.Instance.latitude + "",
            ["long"] = GPS.Instance.longitude + ""
        };
        var weapons = new Dictionary<string, object>();
        var weaponsArr = GetWeapons(GPS.Instance.latitude, GPS.Instance.longitude, size);
        for (var i = 0; i < weaponsArr.Count; i++) {
            weapons[i + ""] = weaponsArr[i];
        }
        var dict = new Dictionary<string, object> {
            ["players/" + nm] = loc,
            ["center"] = loc,
            ["gamemode"] = mode,
            ["size"] = size,
            ["weapons"] = weapons,
            ["startTime"] = GPS.RealTime()
        };
        await ServerHandler.UpdateField("Rooms/" + code, dict);
        switch(mode) {
            case "Deathmatch":
                GameHandler.Data.SetupGame(code, nm, nm, loc);
                PrepDeathmatch();
                break;
        }
    }

    private void PrepDeathmatch() {
        JoinDeathmatch(new Dictionary<string, object> {
            ["timeLimit"] = ServerHandler.DeathmatchTimeLimit,
            ["maxKills"] = ServerHandler.DeathmatchMaxKills,
            ["leaderKills"] = 0
        });
    }

    private async void JoinDeathmatch(Dictionary<string, object> dict) {
        dict ??= new Dictionary<string, object>();
        dict["players/" + GameHandler.Data.PlayerName + "/kills"] = 0;
        dict["players/" + GameHandler.Data.PlayerName + "/deaths"] = 0;
        dict["players/" + GameHandler.Data.PlayerName + "/team"] = GameHandler.Data.PlayerName;
        await ServerHandler.UpdateField("Rooms/" + GameHandler.Data.RoomCode, dict);
        await GameHandler.Data.SetupDeathmatch();
        SceneHandler.SwitchScene("Deathmatch Page");
    }

    public async void JoinGame() {
        var nm = playerName.text.Trim();
        var code = roomCode.text.Trim();
        if (nm == "" || code == "") {
            Debug.Log("Empty fields!");
            return;
        }
        var roomExists = false;
        await ServerHandler.DoesRoomExist(code).ContinueWith(result => {
            roomExists = result.Result;
        });
        if (!roomExists) {
            Debug.Log("Room doesn't exist!");
            return;
        }
        var nameExists = false;
        await ServerHandler.DoesNameExistInRoom(code, nm).ContinueWith(result => {
            nameExists = result.Result;
        });
        if (nameExists) {
            Debug.Log("Name is taken!");
            return;
        }
        var loc = new Dictionary<string, object> {
            ["lat"] = GPS.Instance.latitude + "", ["long"] = GPS.Instance.longitude + ""
        };
        var stringLoc = new Dictionary<string, string> {
            ["lat"] = GPS.Instance.latitude + "", ["long"] = GPS.Instance.longitude + ""
        };
        await ServerHandler.UpdateField("Rooms/" + code + "/players/" + nm, loc);
        SetInfo(code, nm);
        var mode = "";
        await ServerHandler.GetRoomAttribute(code, "gamemode").ContinueWith(result => {
            mode = result.Result.Value.ToString();
        });
        switch(mode) {
            case "Deathmatch":
                GameHandler.Data.SetupGame(code, nm, nm, stringLoc);
                JoinDeathmatch(null);
                break;
        }
    }

    private void SetInfo(string code, string nm) {
        GameHandler.Data.RoomCode = code;
        GameHandler.Data.PlayerName = nm;
    }

    private async Task<string> GetUniqueCode() {
        var code = "";
        var roomExists = true;
        while (roomExists) {
            code = GenerateRandomCode();
            await ServerHandler.DoesRoomExist(code).ContinueWith(result => {
                roomExists = result.Result;
            });
            roomExists = await ServerHandler.DoesRoomExist(code);
        }
        return code;
    }

    private static string GenerateRandomCode() {
        var randomCode = "";
        const string alphanum = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        for (var i = 0; i < 6; i++) {
            randomCode += alphanum[Random.Range(0, alphanum.Length - 1)];
        }
        return randomCode;
    }

    private static ArrayList GetWeapons(double centerLat, double centerLong, double radius) {
        var weapons = new ArrayList();
        for (var i = 0; i < 10; i++) {
            weapons.Add(Weapons.CreateWeapon("Pistol", 
                centerLat + GPS.MetersToLat(2 * radius * Random.value - radius),
                centerLong + GPS.MetersToLong(2 * radius * Random.value - radius)));
        }
        for (var i = 0; i < 5; i++) {
            weapons.Add(Weapons.CreateWeapon("Shotgun", 
                centerLat + GPS.MetersToLat(2 * radius * Random.value - radius),
                centerLong + GPS.MetersToLong(2 * radius * Random.value - radius)));
        }
        for (var i = 0; i < 7; i++) {
            weapons.Add(Weapons.CreateWeapon("Submachine", 
                centerLat + GPS.MetersToLat(2 * radius * Random.value - radius),
                centerLong + GPS.MetersToLong(2 * radius * Random.value - radius)));
        }
        for (var i = 0; i < 7; i++) {
            weapons.Add(Weapons.CreateWeapon("Rifle", 
                centerLat + GPS.MetersToLat(2 * radius * Random.value - radius),
                centerLong + GPS.MetersToLong(2 * radius * Random.value - radius)));
        }
        for (var i = 0; i < 3; i++) {
            weapons.Add(Weapons.CreateWeapon("Sniper", 
                centerLat + GPS.MetersToLat(2 * radius * Random.value - radius),
                centerLong + GPS.MetersToLong(2 * radius * Random.value - radius)));
        }
        return weapons;
    }
}
