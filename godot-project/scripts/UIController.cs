using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class UIController : Control
{
    // --- Referanslar ---
    [Export] public FleetManager FleetManagerRef;
    [Export(PropertyHint.File, "*.csv")] public string CsvFilePath = "res://airports.csv";
    
    // --- Mevcut UI Elementleri ---
    [Export] public LineEdit SearchBar;
    [Export] public ItemList ResultList;

    // --- YENİ UI Elementleri ---
    [Export] public HSlider RadiusSlider;
    [Export] public Label RadiusValueLabel; // "460 km" yazan yazı
    [Export] public CheckButton RadiusToggle; // Göster/Gizle butonu

    // --- Veri ---
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

        // --- Arama Sinyalleri ---
        if (SearchBar != null) SearchBar.TextChanged += OnSearchTextChanged;
        if (ResultList != null)
        {
            ResultList.ItemSelected += OnAirportSelected;
            ResultList.Visible = false;
        }

        // --- YENİ: Slider Ayarları ---
        if (RadiusSlider != null)
        {
            RadiusSlider.MinValue = 10;
            RadiusSlider.MaxValue = 463; // ~250 deniz mili
            
            // Mevcut değeri FleetManager'dan al
            if (FleetManagerRef != null)
                RadiusSlider.Value = FleetManagerRef.RadiusKM;

            RadiusSlider.ValueChanged += OnRadiusChanged;
            
            // Label'ı güncelle
            UpdateRadiusLabel((float)RadiusSlider.Value);
        }

        // --- YENİ: Toggle Ayarları ---
        if (RadiusToggle != null)
        {
            RadiusToggle.ButtonPressed = true; // Başlangıçta açık olsun
            RadiusToggle.Toggled += OnRadiusToggled;
        }
    }

    // Slider hareket ettikçe çalışır
    // HATA ÇÖZÜMÜ: Parametre 'float' değil 'double' olmalı
    private void OnRadiusChanged(double value)
    {
        int km = (int)value;
        
        // Label'ı güncelle (UpdateRadiusLabel float beklediği için cast ediyoruz)
        UpdateRadiusLabel((float)value);

        // FleetManager'ı güncelle
        if (FleetManagerRef != null)
        {
            FleetManagerRef.RadiusKM = km;
        }
    }

    // Toggle'a basıldıkça çalışır
    private void OnRadiusToggled(bool pressed)
    {
        if (FleetManagerRef != null)
        {
            FleetManagerRef.SetRadiusRingVisibility(pressed);
        }
    }

    private void UpdateRadiusLabel(float value)
    {
        if (RadiusValueLabel != null)
        {
            RadiusValueLabel.Text = $"{value:0} km";
        }
    }

    // ... LoadAirportData, FixEncoding ve diğer mevcut fonksiyonlar aynen kalacak ...
    // (Aşağıya önceki cevaptaki LoadAirportData, FixEncoding vb. kodlarını yapıştırın)
    
    private void LoadAirportData()
    {
        if (!FileAccess.FileExists(CsvFilePath)) { GD.PrintErr($"[UI] CSV Yok: {CsvFilePath}"); return; }
        using var file = FileAccess.Open(CsvFilePath, FileAccess.ModeFlags.Read);
        if (!file.EofReached()) file.GetCsvLine(";"); 

        while (!file.EofReached())
        {
            string[] line = file.GetCsvLine(";");
            if (line.Length < 6) continue;
            
            // Helper fonksiyonları kullandığını varsayıyorum
            float lat = ParseMessyCoordinate(line[4], true);
            float lon = ParseMessyCoordinate(line[5], false);

            if (lat != 0 && lon != 0)
            {
                _allAirports.Add(new AirportInfo 
                { 
                    Code = line[1], 
                    Name = FixEncoding(line[3]), 
                    Lat = lat, 
                    Lon = lon 
                });
            }
        }
    }

    private void OnSearchTextChanged(string text)
    {
        if (ResultList == null) return;
        if (string.IsNullOrWhiteSpace(text)) { ResultList.Visible = false; return; }

        text = text.ToUpper();
        ResultList.Visible = true;
        ResultList.Clear();
        _filteredAirports.Clear();

        var results = _allAirports.Where(a => a.Code.Contains(text) || a.Name.ToUpper().Contains(text)).Take(20);
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
        
        if (FleetManagerRef != null)
        {
            FleetManagerRef.CenterLat = selected.Lat;
            FleetManagerRef.CenterLon = selected.Lon;
            SearchBar.ReleaseFocus();
            ResultList.Visible = false;
        }
    }

    private float ParseMessyCoordinate(string raw, bool isLat)
    {
        string clean = raw.Replace(".", "").Replace(",", "");
        if (double.TryParse(clean, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double val))
        {
            double limit = isLat ? 90.0 : 180.0;
            while (System.Math.Abs(val) > limit) val /= 10.0;
            return (float)val;
        }
        return 0f;
    }

    private string FixEncoding(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        return text.Replace("Ã§", "ç").Replace("Ã¶", "ö").Replace("Ã¼", "ü").Replace("Ä±", "ı").Replace("Ä°", "İ").Replace("ÅŸ", "ş").Replace("ÄŸ", "ğ").Replace("Ã‡", "Ç").Replace("Ã–", "Ö").Replace("Ãœ", "Ü").Replace("Åž", "Ş").Replace("Äž", "Ğ").Replace("Ã©", "é").Replace("Ã", "à"); 
    }
}