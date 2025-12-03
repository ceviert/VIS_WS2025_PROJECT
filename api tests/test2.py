import json
import requests
import time

CLIENT_ID = "ertugrulcevik04@gmail.com-api-client"
CLIENT_SECRET = "VJqt1wTyXuPDr1RVbhDPrl6bxIqMSTfP"

def get_token(client_id, client_secret):
    url = "https://auth.opensky-network.org/auth/realms/opensky-network/protocol/openid-connect/token"
    data = {
        "grant_type": "client_credentials",
        "client_id": client_id,
        "client_secret": client_secret
    }
    r = requests.post(url, data=data)
    r.raise_for_status()
    return r.json()["access_token"]

def get_flights(token, begin, end):
    url = f"https://opensky-network.org/api/flights/all?begin={begin}&end={end}"
    headers = {"Authorization": f"Bearer {token}"}
    r = requests.get(url, headers=headers)
    if r.status_code != 200:
        print("Error:", r.status_code, r.text)
        return None
    return r.json()

def get_track(token, icao24):
    now = int(time.time())
    url = f"https://opensky-network.org/api/tracks/all?icao24={icao24}&time={now}"
    headers = {"Authorization": f"Bearer {token}"}
    r = requests.get(url, headers=headers)
    if r.status_code != 200:
        return None
    return r.json()

# ---------------------

token = get_token(CLIENT_ID, CLIENT_SECRET)

end = int(time.time())
begin = end - 900

flights = get_flights(token, begin, end)

if flights is None:
    print("No flights found.")
    quit()

icao24_list = []

for flight in flights:
    icao24_list.append(flight['icao24'])

results = []
for icao in icao24_list:
    track = get_track(token, icao)
    results.append({
        "icao24": icao,
        "path": track['path']
    })

with open("data.json", "w") as f:
    json.dump(results, f, indent=2)