// AstreaEngine Playground JavaScript Application

let map = null;
let startMarker = null;
let endMarker = null;
let routeLayer = null;

const PRESETS = {
  nantes: {
    center: [47.2184, -1.5536],
    zoom: 14,
    start: [47.2100, -1.5500],
    end: [47.2200, -1.5400]
  },
  lille: {
    center: [50.6292, 3.0573],
    zoom: 14,
    start: [50.6300, 3.0550],
    end: [50.6380, 3.0700]
  }
};

const PROFILES = {
  wheelchair: { profile: "wheelchair" },
  blind: { profile: "blind" },
  crutches: { profile: "crutches" },
  walking: { profile: "walking" },
  foot: { profile: "foot" }
};

function initMap() {
  try {
    if (map) {
      map.remove();
      map = null;
    }

    map = L.map('map', {
      zoomControl: true
    }).setView(PRESETS.nantes.center, PRESETS.nantes.zoom);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      maxZoom: 19,
      attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
    }).addTo(map);

    map.on('click', onMapClick);

    setTimeout(() => {
      if (map) {
        map.invalidateSize();
      }
    }, 200);
  } catch (err) {
    console.error("Map initialization failed:", err);
  }
}

function createMarkerIcon(color, text) {
  return L.divIcon({
    className: 'custom-pin',
    html: `<div style="
      background-color: ${color};
      width: 24px;
      height: 24px;
      border-radius: 50%;
      border: 2px solid white;
      box-shadow: 0 2px 6px rgba(0,0,0,0.4);
      color: white;
      font-weight: 700;
      font-size: 11px;
      display: flex;
      align-items: center;
      justify-content: center;
    ">${text}</div>`,
    iconSize: [24, 24],
    iconAnchor: [12, 12]
  });
}

function onMapClick(e) {
  const lat = parseFloat(e.latlng.lat.toFixed(6));
  const lng = parseFloat(e.latlng.lng.toFixed(6));

  if (!startMarker) {
    setStart(lat, lng);
  } else if (!endMarker) {
    setEnd(lat, lng);
  } else {
    setStart(lat, lng);
    clearEnd();
    clearRoute();
  }
}

function setStart(lat, lng) {
  if (!map) return;
  if (startMarker) map.removeLayer(startMarker);
  startMarker = L.marker([lat, lng], {
    draggable: true,
    icon: createMarkerIcon('#10b981', 'A')
  }).addTo(map);

  startMarker.on('dragend', () => {
    updateLabels();
  });

  updateLabels();
}

function setEnd(lat, lng) {
  if (!map) return;
  if (endMarker) map.removeLayer(endMarker);
  endMarker = L.marker([lat, lng], {
    draggable: true,
    icon: createMarkerIcon('#ef4444', 'B')
  }).addTo(map);

  endMarker.on('dragend', () => {
    updateLabels();
  });

  updateLabels();
}

function clearEnd() {
  if (endMarker && map) {
    map.removeLayer(endMarker);
    endMarker = null;
  }
  updateLabels();
}

function clearRoute() {
  if (routeLayer && map) {
    map.removeLayer(routeLayer);
    routeLayer = null;
  }
}

function updateLabels() {
  const startLbl = document.getElementById('label-start');
  const endLbl = document.getElementById('label-end');

  if (startMarker) {
    const p = startMarker.getLatLng();
    startLbl.textContent = `${p.lat.toFixed(5)}, ${p.lng.toFixed(5)}`;
  } else {
    startLbl.textContent = 'Not set';
  }

  if (endMarker) {
    const p = endMarker.getLatLng();
    endLbl.textContent = `${p.lat.toFixed(5)}, ${p.lng.toFixed(5)}`;
  } else {
    endLbl.textContent = 'Not set';
  }
}

function jumpLocation(key) {
  const p = PRESETS[key];
  if (!p) return;
  document.getElementById('chip-nantes')?.classList.toggle('active', key === 'nantes');
  document.getElementById('chip-lille')?.classList.toggle('active', key === 'lille');
  setPresetPoints(key);
}

function setPresetPoints(key) {
  const p = PRESETS[key];
  if (!p || !map) return;
  setStart(p.start[0], p.start[1]);
  setEnd(p.end[0], p.end[1]);
  clearRoute();
  map.flyToBounds([p.start, p.end], { padding: [50, 50] });
}

function applyProfile(profileName) {
  document.querySelectorAll('.preset-chips .chip').forEach(c => {
    const txt = c.textContent.trim().toLowerCase();
    if (txt === profileName.toLowerCase()) {
      c.classList.add('active');
    } else if (['wheelchair', 'blind', 'crutches', 'walking', 'foot'].includes(txt)) {
      c.classList.remove('active');
    }
  });

  const profile = PROFILES[profileName] || { profile: profileName };
  document.getElementById('user-json-input').value = JSON.stringify(profile, null, 2);
}

async function checkHealth() {
  const host = document.getElementById('host-input').value.trim();
  const badge = document.getElementById('health-badge');
  badge.className = 'badge badge-pending';
  badge.textContent = 'Checking...';

  try {
    const res = await fetch(`/api/health?host=${encodeURIComponent(host)}`);
    const data = await res.json();
    if (data.success && (data.health?.includes('"OK"') || data.health?.includes('OK'))) {
      badge.className = 'badge badge-ok';
      badge.textContent = 'GraphHopper OK';
    } else {
      badge.className = 'badge badge-error';
      badge.textContent = 'Host Offline';
    }
  } catch (err) {
    badge.className = 'badge badge-error';
    badge.textContent = 'Unreachable';
  }
}

async function calculateRoute() {
  const resultContainer = document.getElementById('result-container');
  const btn = document.getElementById('btn-calculate');

  if (!startMarker || !endMarker) {
    resultContainer.innerHTML = `<div class="alert alert-error">Please set both Start (A) and Destination (B) points on the map.</div>`;
    return;
  }

  const host = document.getElementById('host-input').value.trim();
  const userJson = document.getElementById('user-json-input').value.trim();

  try {
    JSON.parse(userJson);
  } catch (e) {
    resultContainer.innerHTML = `<div class="alert alert-error">Invalid userJson: ${e.message}</div>`;
    return;
  }

  const p1 = startMarker.getLatLng();
  const p2 = endMarker.getLatLng();

  const payload = {
    host: host,
    points: [
      { lat: p1.lat, lng: p1.lng },
      { lat: p2.lat, lng: p2.lng }
    ],
    userJson: userJson
  };

  btn.disabled = true;
  btn.textContent = 'Computing Route...';
  resultContainer.innerHTML = `<p class="help-hint">Calling AstreaEngine.AstreaRouteAsync...</p>`;

  const startTime = performance.now();

  try {
    const res = await fetch('/api/route', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload)
    });

    const data = await res.json();
    const duration = Math.round(performance.now() - startTime);

    if (!data.success) {
      resultContainer.innerHTML = `
        <div class="alert alert-error">
          <strong>Engine Error:</strong><br/>
          ${data.error || 'Failed to compute route'}
        </div>
      `;
      return;
    }

    const points = data.points;
    if (!points || points.length === 0) {
      resultContainer.innerHTML = `<div class="alert alert-error">No route found between coordinates.</div>`;
      return;
    }

    clearRoute();
    const latLngs = points.map(p => [p.lat, p.lng]);
    routeLayer = L.polyline(latLngs, {
      color: '#3b82f6',
      weight: 5,
      opacity: 0.9,
      lineJoin: 'round'
    }).addTo(map);

    map.fitBounds(routeLayer.getBounds(), { padding: [40, 40] });

    let totalMeters = 0;
    for (let i = 1; i < latLngs.length; i++) {
      totalMeters += L.latLng(latLngs[i - 1]).distanceTo(L.latLng(latLngs[i]));
    }
    const distanceStr = totalMeters > 1000
      ? `${(totalMeters / 1000).toFixed(2)} km`
      : `${Math.round(totalMeters)} m`;

    let profileName = 'Custom';
    try {
      profileName = JSON.parse(userJson).profile || 'Custom';
    } catch {}

    resultContainer.innerHTML = `
      <div class="alert alert-success">Route computed successfully!</div>
      <div class="stats-grid">
        <div class="stat-card">
          <div class="stat-label">Waypoints</div>
          <div class="stat-value">${points.length}</div>
        </div>
        <div class="stat-card">
          <div class="stat-label">Estimated Distance</div>
          <div class="stat-value">${distanceStr}</div>
        </div>
        <div class="stat-card">
          <div class="stat-label">Compute Time</div>
          <div class="stat-value">${duration} ms</div>
        </div>
        <div class="stat-card">
          <div class="stat-label">Profile</div>
          <div class="stat-value" style="font-size: 0.85rem; text-transform: capitalize;">
            ${profileName}
          </div>
        </div>
      </div>
      <details>
        <summary>View Raw Coordinates (${points.length} pts)</summary>
        <pre>${JSON.stringify(points, null, 2)}</pre>
      </details>
    `;
  } catch (err) {
    resultContainer.innerHTML = `<div class="alert alert-error">Network request failed: ${err.message}</div>`;
  } finally {
    btn.disabled = false;
    btn.textContent = '▶ Calculate Route via AstreaEngine';
  }
}

document.addEventListener('DOMContentLoaded', () => {
  initMap();
  checkHealth();
  setPresetPoints('nantes');

  document.getElementById('btn-health')?.addEventListener('click', checkHealth);
  document.getElementById('btn-calculate')?.addEventListener('click', calculateRoute);

  document.getElementById('btn-clear-points')?.addEventListener('click', () => {
    if (startMarker && map) map.removeLayer(startMarker);
    if (endMarker && map) map.removeLayer(endMarker);
    startMarker = null;
    endMarker = null;
    clearRoute();
    updateLabels();
    document.getElementById('result-container').innerHTML = `<p class="help-hint">Points cleared.</p>`;
  });

  document.getElementById('btn-reverse')?.addEventListener('click', () => {
    if (!startMarker || !endMarker) return;
    const p1 = startMarker.getLatLng();
    const p2 = endMarker.getLatLng();
    setStart(p2.lat, p2.lng);
    setEnd(p1.lat, p1.lng);
    if (routeLayer) calculateRoute();
  });

  document.getElementById('btn-preset-nantes')?.addEventListener('click', () => jumpLocation('nantes'));
  document.getElementById('btn-preset-lille')?.addEventListener('click', () => jumpLocation('lille'));

  document.getElementById('btn-format-json')?.addEventListener('click', () => {
    const input = document.getElementById('user-json-input');
    try {
      const parsed = JSON.parse(input.value);
      input.value = JSON.stringify(parsed, null, 2);
    } catch (e) {
      alert('Invalid JSON: ' + e.message);
    }
  });
});

