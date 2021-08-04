using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Database;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Random = System.Random;
#pragma warning disable 4014

public static class ServerHandler {
    private static readonly DatabaseReference DataRef = FirebaseDatabase.DefaultInstance.RootReference;
    public const int DeathmatchMaxKills = 5;
    public const int DeathmatchTimeLimit = 300;
    public const int PickupRange = 10;
    public const double RevealWeaponRange = 0.7;
    public const int LocationBuffer = 3000;
    private const int MaxMissMargin = 45;
    public static double LastLocationUpdate = 0;
    public static double LastInformationPull = 0;
    private static bool EventSubscribed = false;

    private static async Task<DataSnapshot> GetRoom(string room) {
        return await FirebaseDatabase.DefaultInstance
            .GetReference("Rooms/" + room)
            .GetValueAsync().ContinueWith(result => result.Result);
    }

    public static async Task<DataSnapshot> GetRoomAttribute(string room, string attr) {
        DataSnapshot snapshot = null;
        await GetRoom(room + "/" + attr).ContinueWith(task => {
            if (task.IsFaulted) {
                Debug.Log("Error");
            }
            else if (task.IsCompleted) {
                snapshot = task.Result;
            }
        });
        return snapshot;
    }

    public static async Task<bool> DoesRoomExist(string room) {
        var roomExists = false;
        await FirebaseDatabase.DefaultInstance
            .GetReference("Rooms/" + room)
            .GetValueAsync().ContinueWith(task => {
                if (task.IsFaulted) {
                    Debug.Log("Error");
                }
                else if (task.IsCompleted) {
                    var snapshot = task.Result;
                    roomExists = snapshot.Exists;
                }
            });
        return roomExists;
    }

    public static async Task<bool> DoesNameExistInRoom(string room, string nm) {
        var nameExists = false;
        await FirebaseDatabase.DefaultInstance
            .GetReference("Rooms/" + room + "/locations/" + nm)
            .GetValueAsync().ContinueWith(task => {
                if (task.IsFaulted) {
                    Debug.Log("Error");
                }
                else if (task.IsCompleted) {
                    var snapshot = task.Result;
                    nameExists = snapshot.Exists;
                }
            });
        return nameExists;
    }

    public static async Task UpdateField(string path, Dictionary<string, object> content) {
        await DataRef.Child(path).UpdateChildrenAsync(content);
    }

    public static bool FireWeapon(Dictionary<string, object> weapon) {
        var hitPlayer = "";
        var anyHit = false;
        if ((int) weapon["reserve"] != -1) {
            for (var i = 0; i < (int) weapon["bullets"]; i++) {
                hitPlayer = "";
                var gyro = (Input.compass.trueHeading + new Random().Next(0, 1000) / 500d
                    * (int) weapon["inaccuracy"] -
                    (int) weapon["inaccuracy"] + 540) % 360;
                var smallestDistance = (int) weapon["range"] + 1;
                foreach (var t in GameHandler.Data.PlayerInfo) {
                    var pl = (Dictionary<string, object>) t.Value;
                    if (t.Key == GameHandler.Data.PlayerName || pl["team"].ToString() == GameHandler.Data.Team) continue;
                    var a = (GPS.AngleBetweenPoints(GPS.Instance.latitude, GPS.Instance.longitude,
                        Convert.ToDouble(pl["lat"]), Convert.ToDouble(pl["long"])) + 360) % 360;
                    var d = GPS.DistanceBetweenPoints(GPS.Instance.latitude, GPS.Instance.longitude,
                        Convert.ToDouble(pl["lat"]), Convert.ToDouble(pl["long"]));
                    var acceptableMiss = MaxMissMargin - Math.Pow(d / (int) weapon["range"], 2) * MaxMissMargin;
                    if (Math.Abs(a - gyro) < acceptableMiss ||
                        Math.Abs(a - gyro) > 360 - acceptableMiss && d < smallestDistance) {
                        hitPlayer = t.Key;
                    }
                }
                if (hitPlayer != "") {
                    CreateEvent("Hit$" + hitPlayer + "$" + GameHandler.Data.PlayerName + "$" + (int) weapon["damage"]);
                    anyHit = true;
                }
            }
            return anyHit;
        }
        foreach (var t in GameHandler.Data.PlayerInfo) {
            var pl = (Dictionary<string, object>) t.Value;
            if (t.Key == GameHandler.Data.PlayerName || pl["team"].ToString() == GameHandler.Data.Team) continue;
            var d = GPS.DistanceBetweenPoints(GPS.Instance.latitude, GPS.Instance.longitude,
                Convert.ToDouble(pl["lat"]), Convert.ToDouble(pl["long"]));
            if (d <= (int) weapon["range"]) {
                hitPlayer = t.Key;
            }
        }
        if (hitPlayer == "") return false;
        CreateEvent("Hit$" + hitPlayer + "$" + GameHandler.Data.PlayerName + "$" + (int) weapon["damage"]);
        return true;
    }

    public static async Task<bool> PickupWeapon(string room, Dictionary<string, object> pickup, Dictionary<string, object> drop) {
        return await await GetRoomAttribute(room, "weapons").ContinueWith(async result => {
            var snapshot = (List<object>)result.Result.Value;
            var weapons = new List<Dictionary<string, object>>();
            var found = false;
            for (var i = 0; i < snapshot.Count; i++) {
                var w = (Dictionary<string, object>) snapshot[i];
                var props = new []{"name", "ammo", "reserve", "lat", "long"};
                var allMatch = true;
                foreach (var t in props) {
                    if (t == "lat" || t == "long") {
                        if (Convert.ToDouble(w[t]).CompareTo(Convert.ToDouble(pickup[t])) < 1E-10) continue;
                    }
                    else if (w[t].Equals(pickup[t])) continue;
                    weapons.Add(w);
                    allMatch = false;
                    break;
                }
                if (allMatch) {
                    found = true;
                }
            }
            if (!found) return false;
            if ((string) drop["name"] != "Fist") weapons.Add(drop);
            await UpdateField("Rooms/" + room, new Dictionary<string, object> {
                ["weapons"] = weapons
            });
            CreateEvent("WeaponUpdate");
            return true;
        });
    }

    private static async Task CreateEvent(string message) {
        var sig = GPS.RealTime() + "" + new Random().Next(0, 99999);
        await UpdateField("Rooms/" + GameHandler.Data.RoomCode + "/Events",
            new Dictionary<string, object> {
                [sig] = message
            });
        await Task.Delay(3000);
        await UpdateField("Rooms/" + GameHandler.Data.RoomCode + "/Events",
            new Dictionary<string, object> {
                [sig] = null
            });
    }

    public static void SubscribeToEvents(string room) {
        if (EventSubscribed) return;
        EventSubscribed = true;
        FirebaseDatabase.DefaultInstance
            .GetReference("Rooms/" + room + "/Events")
            .ChildAdded += EventDetected;
    }

    private static async void EventDetected(object sender, ChildChangedEventArgs args) {
        var ev = (string) args.Snapshot.Value;
        if (ev == "WeaponUpdate") {
            await GameHandler.Data.PrepWeapons();
            MapHandler.PickupUpdate = true;
        }
        else if (ev.Substring(0, 3) == "Hit" && GameHandler.Data.Health > 0) {
            var info = ev.Split('$');
            if (info[1] != GameHandler.Data.PlayerName) return;
            UIHandler.PlayAudio(GameHandler.StaticAudio, "Noise/hit" + new Random().Next(1,3));
            GameHandler.Data.Health -= Convert.ToInt32(info[3]);
            if (GameHandler.Data.Health > 0) return;
            CreateEvent("Kill$" + info[2]);
            GameHandler.Data.Health = 0;
        }
        else if (ev.Substring(0, 4) == "Hit") {
            var info = ev.Split('$');
            if (info[1] != GameHandler.Data.PlayerName) return;
            GameHandler.Data.Kills++;
        }
    }
}