extends Node3D

@onready var plane = $Plane 
@onready var earth = $Earth

var earth_radius = 50.0

var sample_data = """
{
    "icao24": "thy123",
    "path": [
        [1678000000, 41.0082, 28.9784, 1000, 90, false], 
        [1678000010, 41.2000, 29.1000, 2000, 95, false],
        [1678000020, 41.4000, 29.3000, 3000, 100, false],
        [1678000030, 41.6000, 29.5000, 3000, 110, false]
    ]
}
"""

func _ready():
	print("Simulation...")
	
	var json = JSON.new()
	var error = json.parse(sample_data)
	
	if error == OK:
		var data = json.data
		var path_list = data["path"]
		print("path_list.size:", path_list.size())
		
		fly_the_plane(path_list)
	else:
		print("JSON Hatası: ", json.get_error_message())

func fly_the_plane(path_list):
	var tween = create_tween()
	
	for i in range(path_list.size()):
		var point = path_list[i]
		
		var lat = point[1]
		var lon = point[2]
		var alt = point[3]

		var target_position = lat_lon_to_vector3(lat, lon, alt)

		if i == 0:
			plane.position = target_position
			look_at_target(target_position)
		else:
			var interval = 2.0 
			
			tween.tween_property(plane, "position", target_position, interval)

func lat_lon_to_vector3(lat, lon, alt):
	var phi = deg_to_rad(lat)
	var theta = deg_to_rad(-lon)

	var yukseklik_scale = alt * 0.0001 
	var toplam_yaricap = earth_radius + yukseklik_scale
	
	var x = toplam_yaricap * cos(phi) * cos(theta)
	var y = toplam_yaricap * sin(phi)
	var z = toplam_yaricap * cos(phi) * sin(theta)
	
	return Vector3(x, y, z)

func look_at_target(target_pos):
	# Uçağın burnunu gittiği yere çevirmesi için basit helper
	# Vector3.UP, dünyanın "yukarısı" neresi onu belirtir.
	# Eğer uçak ters dönerse burayla oynamak gerekir.
	if plane.position.distance_to(target_pos) > 0.1:
		plane.look_at(target_pos, Vector3.UP)
