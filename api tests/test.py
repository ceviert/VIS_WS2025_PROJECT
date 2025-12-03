import time
from opensky_api import OpenSkyApi

api = OpenSkyApi(username="ertugrulcevik04@gmail.com-api-client", password="KfbqGsl6jwIueyT2N93QGKBjAz8Mm3dZ")

end = int(time.time())
begin = end - 3599  # 1 hour interval

flights = api.get_flights_from_interval(begin, end)
icaos = []

if flights is None:
    print("No data")
else:
    for flight in flights:
        icaos.append(flight.icao24)

print(icaos)

icao_track_pair = []
for icao in icaos:
    track = api.get_track_by_aircraft(icao24=icao)
    pair = {
        'icao24': icao,
        'path': track
    }
    icao_track_pair.append(pair)

print(icao_track_pair)