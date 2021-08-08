using System.Collections.Generic;
using System.Threading.Tasks;

public class GameData {
    public string RoomCode;
    public string PlayerName;
    public string Team;
    public double[] Center;
    public int Size;
    public int Kills;
    public int Deaths;
    public int LeaderKills;
    public int KillsToWin;
    public int Health;
    public int StartTime;
    public int TimeLimit;
    public int DeathTime;
    public List<Dictionary<string, object>> FloorWeapons;
    public Dictionary<string, object> PrimaryWeapon;
    public Dictionary<string, object> SecondaryWeapon;
    public Dictionary<string, object> PlayerInfo;

    public void SetupGame(string code, string nm, string team, Dictionary<string, string> center) {
        RoomCode = code;
        PlayerName = nm;
        Team = team;
        Center = new[] {double.Parse(center["lat"]), double.Parse(center["long"])};
        Health = 100;
        Kills = 0;
        Deaths = 0;
        DeathTime = 0;
        PrimaryWeapon = Weapons.CreateEquipped("Pistol");
        SecondaryWeapon = Weapons.CreateEquipped("Fist");
    }

    public async Task SetupDeathmatch() {
        await ServerHandler.GetRoomAttribute(RoomCode, "maxKills").ContinueWith(result => {
            KillsToWin = int.Parse(result.Result.Value.ToString());
        });
        await ServerHandler.GetRoomAttribute(RoomCode, "leaderKills").ContinueWith(result => {
            LeaderKills = int.Parse(result.Result.Value.ToString());
        });
        await ServerHandler.GetRoomAttribute(RoomCode, "startTime").ContinueWith(result => {
            StartTime = int.Parse(result.Result.Value.ToString());
        });
        await ServerHandler.GetRoomAttribute(RoomCode, "timeLimit").ContinueWith(result => {
            TimeLimit = int.Parse(result.Result.Value.ToString());
        });
        await ServerHandler.GetRoomAttribute(RoomCode, "size").ContinueWith(result => {
            Size = int.Parse(result.Result.Value.ToString());
        });
        await PrepWeapons();
        await GetPlayerInfo();

    }

    public async Task PrepWeapons() {
        await ServerHandler.GetRoomAttribute(RoomCode, "weapons").ContinueWith(async result => {
            var list = (List<object>)result.Result.Value;
            if (list == null) {
                await PrepWeapons();
                return;
            }
            FloorWeapons = new List<Dictionary<string, object>>();
            for (var i = 0; i < list.Count; i++) {
                FloorWeapons.Add((Dictionary<string, object>) list[i]);
            }
        });
    }

    public async void PushPlayerInfo() {
        if (GPS.CurrentTime() - ServerHandler.LastLocationUpdate < ServerHandler.LocationBuffer) return;
        await ServerHandler.UpdateField("Rooms/" + RoomCode + "/players/" + PlayerName,
            new Dictionary<string, object> {
                ["kills"] = Kills,
                ["deaths"] = Deaths,
                ["lat"] = GPS.Instance.latitude + "",
                ["long"] = GPS.Instance.longitude + ""
            });
        ServerHandler.LastLocationUpdate = GPS.CurrentTime();
    }

    public async Task GetPlayerInfo() {
        if (GPS.CurrentTime() - ServerHandler.LastInformationPull < ServerHandler.LocationBuffer) return;
        await ServerHandler.GetRoomAttribute(RoomCode, "players").ContinueWith(result => {
            PlayerInfo = new Dictionary<string, object>();
            var list = (Dictionary<string, object>)result.Result.Value;
            foreach (var prop in list) {
                if (!PlayerInfo.ContainsKey(prop.Key)) PlayerInfo.Add(prop.Key, prop.Value);
            }
        });
        ServerHandler.LastInformationPull = GPS.CurrentTime();
    }

    public bool IsDead() {
        return DeathTime > GPS.CurrentTime();
    }
}