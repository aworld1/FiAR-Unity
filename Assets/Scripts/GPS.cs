using System;
using System.Collections;
using UnityEngine;

// ReSharper disable once InconsistentNaming
public class GPS : MonoBehaviour {
    public static GPS Instance { private set; get; }
    public double latitude;
    public double longitude;
    public void Start() {
        Instance = this;
        Input.compass.enabled = true;
        StartCoroutine(LocationCoroutine());
    }

    private void Update() {
        const double tolerance = 0.0000000001;
        if (Input.location.status != LocationServiceStatus.Running || 
            !(Math.Abs(latitude - Input.location.lastData.latitude) > tolerance) &&
            !(Math.Abs(longitude - Input.location.lastData.longitude) > tolerance)) return;
        latitude = Input.location.lastData.latitude;
        longitude = Input.location.lastData.longitude;
    }

    IEnumerator LocationCoroutine() {
        #if UNITY_EDITOR
        // No permission handling needed in Editor
        #elif UNITY_ANDROID
        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.FineLocation)) {
            UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.FineLocation);
        }

        if (!UnityEngine.Input.location.isEnabledByUser) {
            Debug.Log("Android and Location not enabled");
            yield break;
        }

        #elif UNITY_IOS
        if (!UnityEngine.Input.location.isEnabledByUser) {
            Debug.Log("IOS and Location not enabled");
            yield break;
        }
        #endif
        Input.location.Start(1f, 1f);
                
        var maxWait = 15;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0) {
            yield return new WaitForSecondsRealtime(1);
            maxWait--;
        }

        #if UNITY_EDITOR
        var editorMaxWait = 15;
        while (Input.location.status == LocationServiceStatus.Stopped && editorMaxWait > 0) {
            yield return new WaitForSecondsRealtime(1);
            editorMaxWait--;
        }
        #endif

        if (maxWait < 1) {
            Debug.Log("Timed out");
            yield break;
        }

        if (Input.location.status != LocationServiceStatus.Running) {
            Debug.LogFormat("Unable to determine device location. Failed with status {0}", Input.location.status);
            yield break;
        }
        /*Debug.LogFormat("Location service live. status {0}", Input.location.status);
        Debug.LogFormat("Location: " 
                        + Input.location.lastData.latitude + " " 
                        + Input.location.lastData.longitude + " " 
                        + Input.location.lastData.altitude + " " 
                        + Input.location.lastData.horizontalAccuracy + " " 
                        + Input.location.lastData.timestamp);*/
        latitude = Input.location.lastData.latitude;
        longitude = Input.location.lastData.longitude;
        GameHandler.Data.PushPlayerInfo();
    }
    
    private static double LatToMeters(double lat) {
        return lat*111320;
    }

    public static double MetersToLat(double meters) {
        return meters/111320;
    }

    private static double LongToMeters(double lon) {
        return lon*40075000*Math.Cos(Instance.longitude)/360;
    }

    public static double MetersToLong(double meters) {
        return 360*meters/(40075000*Math.Cos(Instance.latitude));
    }

    public static int CurrentTime() {
        return (int)(Time.time * 1000);
    }

    public static int RealTime() {
        var epochStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        return (int)(DateTime.UtcNow - epochStart).TotalSeconds;
    }

    public static double[] GetRelativeMapPosition(double[] pos, double[] center, double size) {
        return new[] {LatToMeters(pos[0] - center[0]) / size, LongToMeters(pos[1] - center[1]) / size};
    }
    
    public static double AngleBetweenPoints(double lat1, double long1, double lat2, double long2) {
        var dLon = long2 - long1;

        var y = Math.Sin(dLon) * Math.Cos(lat2);
        var x = Math.Cos(lat1) * Math.Sin(lat2) - Math.Sin(lat1)
            * Math.Cos(lat2) * Math.Cos(dLon);

        var bearing = Math.Atan2(y, x);

        bearing = Mathf.Rad2Deg * bearing;
        bearing = (bearing + 360) % 360;
        bearing = 360 - bearing;

        return bearing;
    }

    public static double DistanceBetweenPoints(double lat1, double lon1, double lat2, double lon2) {
        var R = 6378.137;
        var dLat = lat2 * Math.PI / 180 - lat1 * Math.PI / 180;
        var dLon = lon2 * Math.PI / 180 - lon1 * Math.PI / 180;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
            Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
            Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        var d = R * c;
        return d * 1000;
    }
}
