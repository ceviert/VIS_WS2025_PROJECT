using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class UIController : Control
{
    [Export] public FleetManager FleetManagerRef; 
    [Export(PropertyHint.File, "*.csv")] public string CsvFilePath = "res://airports.csv";
    
    [Export] public LineEdit SearchBar;
    [Export] public ItemList ResultList;

    private class AirportInfo
    {
        public string Code; 
        public string Name; 
        public float Lat;
        public float Lon;
    }

    private List<AirportInfo> _allAirports = new();
    private List<AirportInfo> _filteredAirports = new();

    public override void _Ready()
    {

        LoadAirportData();


        if (SearchBar != null)
            SearchBar.TextChanged += OnSearchTextChanged;
        
        if (ResultList != null)
            ResultList.ItemSelected += OnAirportSelected;


        if (ResultList != null) ResultList.Visible = false;
    }

    private void LoadAirportData()
    {
        if (!FileAccess.FileExists(CsvFilePath))
        {
            GD.PrintErr($"[AirportUI] CSV file not found: {CsvFilePath}");
            return;
        }

        using var file = FileAccess.Open(CsvFilePath, FileAccess.ModeFlags.Read);
        

        if (!file.EofReached()) file.GetCsvLine(";"); 

        while (!file.EofReached())
        {

            string[] line = file.GetCsvLine(";");
            

            if (line.Length < 6) continue;

            string code = line[1]; 
            string name = line[3]; 
            string latRaw = line[4]; 
            string lonRaw = line[5]; 


            float lat = ParseMessyCoordinate(latRaw, true);
            float lon = ParseMessyCoordinate(lonRaw, false);


            if (lat != 0 && lon != 0)
            {
                _allAirports.Add(new AirportInfo 
                { 
                    Code = code, 
                    Name = name, 
                    Lat = lat, 
                    Lon = lon 
                });
            }
        }
        GD.Print($"[AirportUI] {_allAirports.Count} airport successfully loaded.");
    }


    private float ParseMessyCoordinate(string raw, bool isLat)
    {

        string clean = raw.Replace(".", "").Replace(",", "");
        
        if (double.TryParse(clean, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double val))
        {
            double limit = isLat ? 90.0 : 180.0;
            

            while (System.Math.Abs(val) > limit)
            {
                val /= 10.0;
            }
            return (float)val;
        }
        return 0f;
    }

    private void OnSearchTextChanged(string text)
    {
        if (ResultList == null) return;

        if (string.IsNullOrWhiteSpace(text))
        {
            ResultList.Clear();
            ResultList.Visible = false;
            return;
        }

        text = text.ToUpper();
        ResultList.Visible = true;
        ResultList.Clear();
        _filteredAirports.Clear();


        var results = _allAirports
            .Where(a => a.Code.Contains(text) || a.Name.ToUpper().Contains(text))
            .Take(20);

        foreach (var airport in results)
        {
            _filteredAirports.Add(airport);
            ResultList.AddItem($"{airport.Code} - {airport.Name}");
        }
    }

    private void OnAirportSelected(long index)
    {
        int i = (int)index;
        if (i < 0 || i >= _filteredAirports.Count) return;

        var selected = _filteredAirports[i];
        GD.Print($"[AirportUI] Selected: {selected.Code} -> {selected.Lat}, {selected.Lon}");

        if (FleetManagerRef != null)
        {
            FleetManagerRef.CenterLat = selected.Lat;
            FleetManagerRef.CenterLon = selected.Lon;
            

            SearchBar.Text = "";
            ResultList.Visible = false;
            

            SearchBar.ReleaseFocus();
        }
    }
}