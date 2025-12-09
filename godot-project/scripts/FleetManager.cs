using Godot;
using System;

public partial class FleetManager : Node3D
{
    private HttpRequest _httpRequest;
    private Timer _timer;

    [Export] public double UpdateInterval = 10.0; 
    
    [Export] public float CenterLat = 41.0f;
    [Export] public float CenterLon = 28.0f;
    [Export] public int RadiusKM = 50;

    public override void _Ready()
    {

        _httpRequest = new HttpRequest();
        AddChild(_httpRequest);
        _httpRequest.RequestCompleted += OnRequestCompleted;

        _timer = new Timer();
        AddChild(_timer);
        _timer.WaitTime = UpdateInterval;
        _timer.OneShot = false;
        _timer.Timeout += FetchData;
        _timer.Start();

        FetchData();
    }

    private void FetchData()
    {
        int radiusNm = (int)(RadiusKM * 0.539957);
        
        string url = $"https://api.airplanes.live/v2/point/{CenterLat}/{CenterLon}/{radiusNm}";
        
        GD.Print($"\n--- REQUEST SENT TO: {url} ---");
    }

    private void OnRequestCompleted(long result, long responseCode, string[] headers, byte[] body)
    {
        if (responseCode != 200)
        {
            GD.PrintErr($"ERROR! API response code: {responseCode}");
            return;
        }

        string jsonStr = System.Text.Encoding.UTF8.GetString(body);

        GD.Print(jsonStr);
        
        var json = new Json();
        if (json.Parse(jsonStr) != Error.Ok)
        {
            GD.PrintErr("ERROR parsing the json.");
            return;
        }
        
        var data = json.Data.AsGodotDictionary();
        var aircraftList = data["ac"].AsGodotArray();
        
        processAircraftList(aircraftList);
    }

    private void processAircraftList(Godot.Collections.AsGodotArray list)
    {
        foreach (var aircraft in list)
        {
            string aircraftHex = aircraft["hex"];
            string aircraftFlight = aircraft["flight"];
            
        }
    }
}