"""Tests for the astrea_engine shared library — GraphHopper HTTP wrapper."""

import ctypes
import json
import os
import pathlib
import pytest

_REPO_ROOT = pathlib.Path(__file__).resolve().parents[2]
_LIB_PATH  = _REPO_ROOT / "build" / "libastrea_engine.dylib"
_GH_HOST   = os.environ.get("GRAPHHOPPER_URL", "http://localhost:8989")

_POINT_A = "47.2184,-1.5536"
_POINT_B = "47.2200,-1.5500"


@pytest.fixture(scope="module")
def lib():
    if not _LIB_PATH.exists():
        pytest.skip(f"Shared library not found at {_LIB_PATH} — run `just build-lib` first.")

    handle = ctypes.CDLL(str(_LIB_PATH))

    # const char *astrea_health(const char *host)
    handle.astrea_health.restype  = ctypes.c_char_p
    handle.astrea_health.argtypes = [ctypes.c_char_p]

    # const char *astrea_route(const char *host, const char *points_csv,
    #                           const char *user_json)
    handle.astrea_route.restype  = ctypes.c_char_p
    handle.astrea_route.argtypes = [ctypes.c_char_p, ctypes.c_char_p,
                                    ctypes.c_char_p]
    return handle


def _gh_available(lib) -> bool:
    """Return True if GraphHopper answers /health successfully."""
    raw = lib.astrea_health(_GH_HOST.encode())
    try:
        return "error" not in json.loads(raw.decode())
    except Exception:
        return False


# ---- /health ----------------------------------------------------------------

def test_health_returns_json(lib):
    raw = lib.astrea_health(_GH_HOST.encode())
    data = json.loads(raw.decode())
    assert isinstance(data, dict)


def test_health_live(lib):
    if not _gh_available(lib):
        pytest.skip("GraphHopper not reachable — skipping live test")
    raw = lib.astrea_health(_GH_HOST.encode())
    data = json.loads(raw.decode())
    assert "error" not in data
    assert data.get("status") == "OK"


# ---- /route -----------------------------------------------------------------

def test_route_not_enough_points(lib):
    """Fewer than 2 points must return an error JSON, not crash."""
    raw = lib.astrea_route(
        _GH_HOST.encode(),
        _POINT_A.encode(),   # only one point — no pipe
        b'{"profile": "foot"}',
    )
    data = json.loads(raw.decode())
    assert "error" in data


def test_route_live(lib):
    if not _gh_available(lib):
        pytest.skip("GraphHopper not reachable — skipping live test")

    points_csv = f"{_POINT_A}|{_POINT_B}".encode()
    user_json  = b'{"profile": "foot", "locale": "en"}'
    raw = lib.astrea_route(_GH_HOST.encode(), points_csv, user_json)
    data = json.loads(raw.decode())

    assert "error" not in data, f"Unexpected error: {data}"
    assert "paths" in data
    assert len(data["paths"]) > 0
    path = data["paths"][0]
    assert path["distance"] > 0
    assert path["time"] > 0


def test_route_disabled_profile_live(lib):
    if not _gh_available(lib):
        pytest.skip("GraphHopper not reachable — skipping live test")

    points_csv = f"{_POINT_A}|{_POINT_B}".encode()
    user_json  = b'{"profile": "disabled"}'
    raw = lib.astrea_route(_GH_HOST.encode(), points_csv, user_json)
    data = json.loads(raw.decode())

    assert "error" not in data, f"Unexpected error: {data}"
    assert "paths" in data
