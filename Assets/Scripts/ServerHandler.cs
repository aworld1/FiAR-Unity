using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase.Database;
using UnityEngine;

public static class ServerHandler {
    private static readonly DatabaseReference DataRef = FirebaseDatabase.DefaultInstance.RootReference;
    public const int DeathmatchMaxKills = 5;
    public const int DeathmatchTimeLimit = 300;
    public const int PickupRange = 10;
    public const double RevealWeaponRange = 0.7;

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
            await UpdateField("Rooms/" + room + "/Events", new Dictionary<string, object> {
                [GPS.RealTime() + "" + new System.Random().Next(0, 99999)] = "WeaponUpdate"
            });
            return true;
        });
    }

    public static void SubscribeToEvents(string room) {
        FirebaseDatabase.DefaultInstance
            .GetReference("Rooms/" + room + "/Events")
            .ChildAdded += EventDetected;
    }

    private static async void EventDetected(object sender, ChildChangedEventArgs args) {
        if ((string)args.Snapshot.Value == "WeaponUpdate") {
            await GameHandler.Data.PrepWeapons();
            MapHandler.PickupUpdate = true;
        }
    }
}