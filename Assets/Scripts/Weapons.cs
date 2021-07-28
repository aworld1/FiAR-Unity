using System.Collections.Generic;
using UnityEngine;

public class Weapons {
    public static Dictionary<string, int> Pistol = new Dictionary<string, int> {
        ["damage"] = 24,
        ["range"] = 15,
        ["inaccuracy"] = 30,
        ["mag"] = 10,
        ["reserve"] = 30,
        ["delay"] = 250,
        ["reload"] = 1250,
        ["bullets"] = 1
    };
    
    public static Dictionary<string, int> Shotgun = new Dictionary<string, int> {
        ["damage"] = 9,
        ["range"] = 12,
        ["inaccuracy"] = 35,
        ["mag"] = 5,
        ["reserve"] = 15,
        ["delay"] = 700,
        ["reload"] = 650,
        ["bullets"] = 10
    };
    
    public static Dictionary<string, int> Submachine = new Dictionary<string, int> {
        ["damage"] = 19,
        ["range"] = 25,
        ["inaccuracy"] = 25,
        ["mag"] = 30,
        ["reserve"] = 90,
        ["delay"] = 100,
        ["reload"] = 1500,
        ["bullets"] = 1
    };
    
    public static Dictionary<string, int> Rifle = new Dictionary<string, int> {
        ["damage"] = 33,
        ["range"] = 35,
        ["inaccuracy"] = 7,
        ["mag"] = 25,
        ["reserve"] = 75,
        ["delay"] = 200,
        ["reload"] = 1750,
        ["bullets"] = 1
    };
    
    public static Dictionary<string, int> Sniper = new Dictionary<string, int> {
        ["damage"] = 99,
        ["range"] = 100,
        ["inaccuracy"] = 0,
        ["mag"] = 3,
        ["reserve"] = 9,
        ["delay"] = 1500,
        ["reload"] = 3000,
        ["bullets"] = 1
    };
    
    public static Dictionary<string, int> Fist = new Dictionary<string, int> {
        ["damage"] = 99,
        ["range"] = 2,
        ["inaccuracy"] = 0,
        ["mag"] = -1,
        ["reserve"] = -1,
        ["delay"] = 200,
        ["reload"] = -1,
        ["bullets"] = -1
    };

    public static Dictionary<string, object> CreateWeapon(Dictionary<string, object> equippedWeapon, double lat, double lon) {
        var weapon = CreateWeapon((string)equippedWeapon["name"], lat, lon);
        weapon["ammo"] = equippedWeapon["ammo"];
        weapon["reserve"] = equippedWeapon["reserve"];
        return weapon;
    }
    
    public static Dictionary<string, object> CreateWeapon(string nm, double lat, double lon) {
        var obj = new Weapons();
        var weapon = (Dictionary<string, int>)obj.GetType().GetField(nm).GetValue(obj);
        return new Dictionary<string, object> {
            ["name"] = nm,
            ["ammo"] = weapon["mag"],
            ["reserve"] = weapon["reserve"],
            ["lat"] = lat + "",
            ["long"] = lon + ""
        };
    }

    public static Dictionary<string, object> CreateEquipped(Dictionary<string, object> floorWeapon) {
        return CreateEquipped((string)floorWeapon["name"]);
    }

    public static Dictionary<string, object> CreateEquipped(string nm) {
        var obj = new Weapons();
        var weapon = (Dictionary<string, int>)obj.GetType().GetField(nm).GetValue(obj);
        return new Dictionary<string, object> {
            ["name"] = nm,
            ["damage"] = weapon["damage"],
            ["range"] = weapon["range"],
            ["inaccuracy"] = weapon["inaccuracy"],
            ["ammo"] = weapon["mag"],
            ["mag"] = weapon["mag"],
            ["reserve"] = weapon["reserve"],
            ["delay"] = weapon["delay"],
            ["reload"] = weapon["reload"],
            ["bullets"] = weapon["bullets"],
            ["lastFired"] = 0,
            ["reloadStart"] = 0,
            ["reloading"] = false
        };
    }
}