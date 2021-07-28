using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Database;
using UnityEngine;

public static class ServerHandler {
    private static readonly DatabaseReference DataRef = FirebaseDatabase.DefaultInstance.RootReference;
    public const int DeathmatchMaxKills = 5;
    public const int DeathmatchTimeLimit = 300;
    public const int PickupRange = 10;

    private static async Task<DataSnapshot> GetRoom(string room) {
        return await GetRoomAttribute(room, "").ContinueWith(result => result.Result);
    }

    public static async Task<DataSnapshot> GetRoomAttribute(string room, string attr) {
        DataSnapshot snapshot = null;
        await FirebaseDatabase.DefaultInstance
            .GetReference("Rooms/" + room + "/" + attr)
            .GetValueAsync().ContinueWith(task => {
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

    public static async Task PickupWeapon(string room, Dictionary<string, object> pickup, Dictionary<string, object> drop) {
        await FirebaseDatabase.DefaultInstance
            .GetReference("Rooms/" + room + "/weapons/")
            .GetValueAsync().ContinueWith(result => {
                var snapshot = result.Result.Value;
                Debug.Log(snapshot);
            });
    }
}