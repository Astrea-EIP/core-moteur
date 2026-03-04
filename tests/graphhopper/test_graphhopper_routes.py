import os
import requests
from urllib.parse import urlencode

def base_url():
    return os.environ.get("GRAPHHOPPER_URL", "http://localhost:8989")


def test_route_basic():
    """Query a simple route between two nearby points.

    Requires a GraphHopper server running with a loaded map on port 8989
    (the default configuration used in the docker-compose file).
    """
    url = f"{base_url()}/route"
    points = ["47.2184,-1.5536", "47.2200,-1.5500"]
    qs = "&".join(f"point={p}" for p in points)
    qs += "&" + urlencode({"profile": "foot", "locale": "en"})
    resp = requests.get(f"{url}?{qs}", timeout=10)
    assert resp.status_code == 200, f"unexpected status: {resp.status_code}"
    data = resp.json()

    assert "paths" in data, "missing 'paths' in response"
    assert isinstance(data["paths"], list)
    assert len(data["paths"]) > 0

    path = data["paths"][0]

    assert "distance" in path
    assert "time" in path
    assert path["distance"] > 0
    assert path["time"] > 0

def test_route_invalid_point():
    """Ensure GraphHopper returns a 400 when missing points."""
    url = f"{base_url()}/route"

    resp = requests.get(url, params={"vehicle": "car"}, timeout=5)
    assert resp.status_code == 400


# --- Nominatim tests -------------------------------------------------------

def nominatim_base_url():
    return os.environ.get("NOMINATIM_URL", "http://localhost:8991")


def test_nominatim_search():
    """Simple forward geocoding request."""
    url = f"{nominatim_base_url()}/search"

    params = {"q": "Nantes", "format": "json", "limit": 1}

    resp = requests.get(url, params=params, timeout=10)
    assert resp.status_code == 200, f"unexpected status: {resp.status_code}"

    data = resp.json()
    assert isinstance(data, list), "expected a JSON array"
    assert len(data) > 0, "no results returned"

    item = data[0]
    assert "lat" in item and "lon" in item, "missing coordinates in result"


def test_nominatim_reverse():
    """Reverse geocoding around a known point (Pays‑de‑la‑Loire)."""
    url = f"{nominatim_base_url()}/reverse"

    params = {"lat": 47.2184, "lon": -1.5536, "format": "json"}

    resp = requests.get(url, params=params, timeout=10)
    assert resp.status_code == 200, f"unexpected status: {resp.status_code}"
    data = resp.json()
    assert "address" in data, "missing address object"
    assert "lat" in data and "lon" in data, "missing lat/lon in response"


def test_nominatim_search_invalid():
    """Searching without a query should return an error status."""
    url = f"{nominatim_base_url()}/search"
    resp = requests.get(url, timeout=5)
    assert resp.status_code >= 400, "expected HTTP error for missing query"
