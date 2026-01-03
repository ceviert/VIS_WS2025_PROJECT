using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class UIController : Control
{
    // --- Ayarlar ---
    [Export] public FleetManager FleetManagerRef; // FleetManager'a referans
    [Export(PropertyHint.File, "*.csv")] public string CsvFilePath = "res://airports.csv";
    
    // --- UI Elementleri ---
    [Export] public LineEdit SearchBar;
    [Export] public ItemList ResultList;

    // --- Veri ---
    private class AirportInfo
    {
        public string Code; // ICAO veya IATA (örn: LTFM)
        public string Name; // Örn: Istanbul Airport
        public float Lat;
        public float Lon;
    }

    private List<AirportInfo> _allAirports = new();
    private List<AirportInfo> _filteredAirports = new();

    public override void _Ready()
    {
        // 1. CSV Dosyasını Yükle
        LoadAirportData();

        // 2. Sinyalleri Bağla
        if (SearchBar != null)
            SearchBar.TextChanged += OnSearchTextChanged;
        
        if (ResultList != null)
            ResultList.ItemSelected += OnAirportSelected;

        // Başlangıçta listeyi gizle veya boşalt
        if (ResultList != null) ResultList.Visible = false;
    }

    private void LoadAirportData()
    {
        if (!FileAccess.FileExists(CsvFilePath))
        {
            GD.PrintErr($"[AirportUI] CSV dosyası bulunamadı: {CsvFilePath}");
            return;
        }

        using var file = FileAccess.Open(CsvFilePath, FileAccess.ModeFlags.Read);
        
        // Başlık satırını atla
        if (!file.EofReached()) file.GetCsvLine(";"); 

        while (!file.EofReached())
        {
            // ÖNEMLİ: Delimiter olarak ";" kullanıyoruz
            string[] line = file.GetCsvLine(";");
            
            // Satırın en az 6 sütunu olduğundan emin ol (Lon verisi index 5'te)
            if (line.Length < 6) continue;

            string code = line[1]; // ident sütunu
            string name = line[3]; // name sütunu
            string latRaw = line[4]; // latitude_deg
            string lonRaw = line[5]; // longitude_deg

            // Karmaşık koordinatları temizle ve parse et
            float lat = ParseMessyCoordinate(latRaw, true);
            float lon = ParseMessyCoordinate(lonRaw, false);

            // Koordinatlar geçerliyse (0,0 değilse) listeye ekle
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
        GD.Print($"[AirportUI] {_allAirports.Count} havalimanı başarıyla yüklendi.");
    }

    // Bozuk koordinat formatlarını (örn: 44.201.401 veya 467.911) düzelten fonksiyon
    private float ParseMessyCoordinate(string raw, bool isLat)
    {
        // Noktaları ve olası virgülleri temizle, saf sayıya çevir
        string clean = raw.Replace(".", "").Replace(",", "");
        
        if (double.TryParse(clean, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double val))
        {
            double limit = isLat ? 90.0 : 180.0;
            
            // Değer mantıklı bir aralığa (lat için 90, lon için 180) inene kadar 10'a böl
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

        // Eğer arama kutusu boşsa listeyi gizle
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

        // Basit Arama Algoritması (Kod veya İsim içinde ara)
        // Performans için sadece ilk 20 sonucu gösterelim
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
        GD.Print($"[AirportUI] Seçilen: {selected.Code} -> {selected.Lat}, {selected.Lon}");

        // FleetManager'ı güncelle
        if (FleetManagerRef != null)
        {
            FleetManagerRef.CenterLat = selected.Lat;
            FleetManagerRef.CenterLon = selected.Lon;
            
            // UI'ı temizle (isteğe bağlı)
            SearchBar.Text = "";
            ResultList.Visible = false;
            
            // Odağı oyuna geri ver (Klavye ile kamera kontrolü için önemli)
            SearchBar.ReleaseFocus();
        }
    }
}