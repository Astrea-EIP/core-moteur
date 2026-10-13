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

const PROFILE_PRESETS = {
  wheelchair: {
    profile: "wheelchair",
    allow_stairs: false,
    avoid_slopes: true,
    max_slope: 8.0,
    has_wheels: true,
    blindness: false,
    width: 0.9,
    distance_influence: 70,
    speed: 3.5
  },
  blind: {
    profile: "blind",
    allow_stairs: true,
    avoid_slopes: false,
    has_wheels: false,
    blindness: true,
    width: 0.7,
    distance_influence: 80,
    speed: 3.5
  },
  crutches: {
    profile: "crutches",
    allow_stairs: true,
    stairs_penalty: 0.4,
    avoid_slopes: true,
    max_slope: 10.0,
    has_wheels: false,
    blindness: false,
    width: 0.75,
    distance_influence: 70,
    speed: 3.0
  },
  walking: {
    profile: "walking",
    allow_stairs: true,
    avoid_slopes: false,
    has_wheels: false,
    blindness: false,
    width: 0.6,
    distance_influence: 60,
    speed: 5.0
  }
};

const PROFILE_COLORS = {
  wheelchair: '#3b82f6', // blue
  blind: '#a855f7',      // purple
  crutches: '#ec4899',   // pink
  walking: '#10b981',    // emerald
  custom: '#f59e0b'      // amber
};

let currentPreset = 'wheelchair';
let isSyncing = false;
let debounceTimer = null;

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
    triggerAutoCalculate();
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
    triggerAutoCalculate();
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
    triggerAutoCalculate();
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
  document.getElementById('btn-preset-nantes')?.classList.toggle('active', key === 'nantes');
  document.getElementById('btn-preset-lille')?.classList.toggle('active', key === 'lille');
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

// -------------------------------------------------------------
// Form Controls & JSON Bi-directional Synchronization
// -------------------------------------------------------------

function getUiState() {
  const allowStairs = document.getElementById('ctrl-allow-stairs').checked;
  const stairsPenalty = parseFloat(document.getElementById('ctrl-stairs-penalty').value);
  const avoidSlopes = document.getElementById('ctrl-avoid-slopes').checked;
  const maxSlope = parseFloat(document.getElementById('ctrl-max-slope').value);
  const hasWheels = document.getElementById('ctrl-has-wheels').checked;
  const blindness = document.getElementById('ctrl-blindness').checked;
  const width = parseFloat(document.getElementById('ctrl-width').value);
  const distanceInfluence = parseInt(document.getElementById('ctrl-distance-influence').value, 10);

  const state = {
    profile: currentPreset
  };

  if (!allowStairs) {
    state.allow_stairs = false;
  } else {
    state.allow_stairs = true;
    if (stairsPenalty < 1.0) {
      state.stairs_penalty = stairsPenalty;
    }
  }

  if (avoidSlopes) {
    state.avoid_slopes = true;
    state.max_slope = maxSlope;
  } else {
    state.avoid_slopes = false;
  }

  if (hasWheels) state.has_wheels = true;
  if (blindness) state.blindness = true;
  if (!isNaN(width)) state.width = width;
  if (!isNaN(distanceInfluence)) state.distance_influence = distanceInfluence;

  return state;
}

function updateUiFromState(obj) {
  if (isSyncing) return;
  isSyncing = true;

  try {
    const rawPreset = (obj.profile || '').toLowerCase();
    const presetConfig = PROFILE_PRESETS[rawPreset];
    const data = presetConfig ? { ...presetConfig, ...obj } : obj;

    currentPreset = ['wheelchair', 'blind', 'crutches', 'walking'].includes(rawPreset) ? rawPreset : 'custom';
    document.querySelectorAll('.preset-chips .chip').forEach(c => {
      c.classList.toggle('active', c.dataset.preset === currentPreset);
    });

    const allowStairs = data.allow_stairs !== undefined ? Boolean(data.allow_stairs) : (data.avoid_stairs !== undefined ? !data.avoid_stairs : true);
    document.getElementById('ctrl-allow-stairs').checked = allowStairs;

    const penaltyVal = data.stairs_penalty !== undefined ? parseFloat(data.stairs_penalty) : 0.3;
    document.getElementById('ctrl-stairs-penalty').value = penaltyVal;
    document.getElementById('val-stairs-penalty').textContent = penaltyVal.toFixed(2);
    document.getElementById('row-stairs-penalty').style.display = allowStairs ? 'flex' : 'none';

    const avoidSlopes = data.avoid_slopes !== undefined ? Boolean(data.avoid_slopes) : (data.slope !== undefined ? !data.slope : false);
    document.getElementById('ctrl-avoid-slopes').checked = avoidSlopes;

    const maxSlopeVal = data.max_slope !== undefined ? parseFloat(data.max_slope) : 8.0;
    document.getElementById('ctrl-max-slope').value = maxSlopeVal;
    document.getElementById('val-max-slope').textContent = `${maxSlopeVal}%`;
    document.getElementById('row-max-slope').style.display = avoidSlopes ? 'flex' : 'none';

    const hasWheels = Boolean(data.has_wheels || data.wheels);
    document.getElementById('ctrl-has-wheels').checked = hasWheels;

    const blindness = Boolean(data.blindness || data.blind);
    document.getElementById('ctrl-blindness').checked = blindness;

    const widthVal = data.width !== undefined ? parseFloat(data.width) : 0.8;
    document.getElementById('ctrl-width').value = widthVal;
    document.getElementById('val-width').textContent = `${widthVal}m`;

    const distVal = data.distance_influence !== undefined ? parseInt(data.distance_influence, 10) : 70;
    document.getElementById('ctrl-distance-influence').value = distVal;
    document.getElementById('val-distance-influence').textContent = distVal;

  } finally {
    isSyncing = false;
  }
}

function syncUiToJson() {
  if (isSyncing) return;
  isSyncing = true;
  try {
    const state = getUiState();
    const jsonStr = JSON.stringify(state, null, 2);
    document.getElementById('user-json-input').value = jsonStr;
  } finally {
    isSyncing = false;
  }
  triggerAutoCalculate();
}

function setPreset(presetName, shouldSync = true) {
  currentPreset = presetName;

  document.querySelectorAll('.preset-chips .chip').forEach(c => {
    c.classList.toggle('active', c.dataset.preset === presetName);
  });

  if (PROFILE_PRESETS[presetName]) {
    const config = PROFILE_PRESETS[presetName];
    updateUiFromState(config);
    if (shouldSync) {
      document.getElementById('user-json-input').value = JSON.stringify(config, null, 2);
      triggerAutoCalculate();
    }
  }
}

function triggerAutoCalculate() {
  const auto = document.getElementById('chk-auto-calculate')?.checked ?? true;
  if (!auto) return;

  const indicator = document.getElementById('calc-indicator');
  if (indicator) indicator.textContent = 'Updating...';

  clearTimeout(debounceTimer);
  debounceTimer = setTimeout(() => {
    if (startMarker && endMarker) {
      calculateRoute();
    } else if (indicator) {
      indicator.textContent = '';
    }
  }, 300);
}

// -------------------------------------------------------------
// Health and Routing APIs
// -------------------------------------------------------------

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
  const indicator = document.getElementById('calc-indicator');

  if (!startMarker || !endMarker) {
    resultContainer.innerHTML = `<div class="alert alert-error">Please set both Start (A) and Destination (B) points on the map.</div>`;
    if (indicator) indicator.textContent = '';
    return;
  }

  const host = document.getElementById('host-input').value.trim();
  const userJson = document.getElementById('user-json-input').value.trim();

  try {
    JSON.parse(userJson);
  } catch (e) {
    resultContainer.innerHTML = `<div class="alert alert-error">Invalid userJson: ${e.message}</div>`;
    if (indicator) indicator.textContent = '';
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
  if (indicator) indicator.textContent = 'Calculating...';

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
      if (indicator) indicator.textContent = 'Error';
      return;
    }

    const points = data.points;
    if (!points || points.length === 0) {
      resultContainer.innerHTML = `<div class="alert alert-error">No route found between coordinates.</div>`;
      if (indicator) indicator.textContent = 'No route';
      return;
    }

    clearRoute();
    const latLngs = points.map(p => [p.lat, p.lng]);
    const routeColor = PROFILE_COLORS[currentPreset] || '#3b82f6';

    routeLayer = L.polyline(latLngs, {
      color: routeColor,
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

    let speedKmH = 4.0;
    try {
      const parsed = JSON.parse(userJson);
      if (parsed.speed) speedKmH = parseFloat(parsed.speed);
      else if (parsed.max_speed) speedKmH = parseFloat(parsed.max_speed);
      else if (PROFILE_PRESETS[currentPreset]?.speed) speedKmH = PROFILE_PRESETS[currentPreset].speed;
      else speedKmH = currentPreset === 'crutches' ? 3.0 : (currentPreset === 'blind' || currentPreset === 'wheelchair' ? 3.5 : 4.0);
    } catch {
      speedKmH = PROFILE_PRESETS[currentPreset]?.speed || 4.0;
    }
    let estMinutes = Math.round((totalMeters / 1000) / speedKmH * 60);
    if (estMinutes < 1) estMinutes = 1;

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
          <div class="stat-label">Est. Time (@${speedKmH} km/h)</div>
          <div class="stat-value">${estMinutes} min</div>
        </div>
        <div class="stat-card">
          <div class="stat-label">Compute Time</div>
          <div class="stat-value">${duration} ms</div>
        </div>
      </div>
      <details>
        <summary>View Raw Coordinates (${points.length} pts)</summary>
        <pre>${JSON.stringify(points, null, 2)}</pre>
      </details>
    `;
    if (indicator) indicator.textContent = 'Done';
  } catch (err) {
    resultContainer.innerHTML = `<div class="alert alert-error">Network request failed: ${err.message}</div>`;
    if (indicator) indicator.textContent = 'Failed';
  } finally {
    btn.disabled = false;
    btn.textContent = '▶ Recalculate Route';
  }
}

// -------------------------------------------------------------
// DOM Event Listeners
// -------------------------------------------------------------

document.addEventListener('DOMContentLoaded', () => {
  initMap();
  checkHealth();
  setPresetPoints('nantes');

  // Preset chips
  document.querySelectorAll('.preset-chips .chip').forEach(chip => {
    chip.addEventListener('click', () => {
      const preset = chip.dataset.preset;
      setPreset(preset, true);
    });
  });

  // Toggle switch listeners
  document.getElementById('ctrl-allow-stairs').addEventListener('change', (e) => {
    document.getElementById('row-stairs-penalty').style.display = e.target.checked ? 'flex' : 'none';
    currentPreset = 'custom';
    document.querySelectorAll('.preset-chips .chip').forEach(c => c.classList.toggle('active', c.dataset.preset === 'custom'));
    syncUiToJson();
  });

  document.getElementById('ctrl-avoid-slopes').addEventListener('change', (e) => {
    document.getElementById('row-max-slope').style.display = e.target.checked ? 'flex' : 'none';
    currentPreset = 'custom';
    document.querySelectorAll('.preset-chips .chip').forEach(c => c.classList.toggle('active', c.dataset.preset === 'custom'));
    syncUiToJson();
  });

  document.getElementById('ctrl-has-wheels').addEventListener('change', () => {
    currentPreset = 'custom';
    document.querySelectorAll('.preset-chips .chip').forEach(c => c.classList.toggle('active', c.dataset.preset === 'custom'));
    syncUiToJson();
  });

  document.getElementById('ctrl-blindness').addEventListener('change', () => {
    currentPreset = 'custom';
    document.querySelectorAll('.preset-chips .chip').forEach(c => c.classList.toggle('active', c.dataset.preset === 'custom'));
    syncUiToJson();
  });

  // Slider listeners
  const stairsSlider = document.getElementById('ctrl-stairs-penalty');
  stairsSlider.addEventListener('input', () => {
    document.getElementById('val-stairs-penalty').textContent = parseFloat(stairsSlider.value).toFixed(2);
    currentPreset = 'custom';
    document.querySelectorAll('.preset-chips .chip').forEach(c => c.classList.toggle('active', c.dataset.preset === 'custom'));
    syncUiToJson();
  });

  const slopeSlider = document.getElementById('ctrl-max-slope');
  slopeSlider.addEventListener('input', () => {
    document.getElementById('val-max-slope').textContent = `${slopeSlider.value}%`;
    currentPreset = 'custom';
    document.querySelectorAll('.preset-chips .chip').forEach(c => c.classList.toggle('active', c.dataset.preset === 'custom'));
    syncUiToJson();
  });

  const widthSlider = document.getElementById('ctrl-width');
  widthSlider.addEventListener('input', () => {
    document.getElementById('val-width').textContent = `${widthSlider.value}m`;
    currentPreset = 'custom';
    document.querySelectorAll('.preset-chips .chip').forEach(c => c.classList.toggle('active', c.dataset.preset === 'custom'));
    syncUiToJson();
  });

  const distSlider = document.getElementById('ctrl-distance-influence');
  distSlider.addEventListener('input', () => {
    document.getElementById('val-distance-influence').textContent = distSlider.value;
    currentPreset = 'custom';
    document.querySelectorAll('.preset-chips .chip').forEach(c => c.classList.toggle('active', c.dataset.preset === 'custom'));
    syncUiToJson();
  });

  // Raw JSON input listener
  const jsonInput = document.getElementById('user-json-input');
  jsonInput.addEventListener('input', () => {
    try {
      const parsed = JSON.parse(jsonInput.value);
      updateUiFromState(parsed);
      triggerAutoCalculate();
    } catch {
      // Ignore invalid JSON while user is typing
    }
  });

  // Action buttons
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
    triggerAutoCalculate();
  });

  document.getElementById('btn-preset-nantes')?.addEventListener('click', () => {
    jumpLocation('nantes');
    triggerAutoCalculate();
  });
  document.getElementById('btn-preset-lille')?.addEventListener('click', () => {
    jumpLocation('lille');
    triggerAutoCalculate();
  });

  document.getElementById('btn-format-json')?.addEventListener('click', () => {
    try {
      const parsed = JSON.parse(jsonInput.value);
      jsonInput.value = JSON.stringify(parsed, null, 2);
    } catch (e) {
      alert('Invalid JSON: ' + e.message);
    }
  });

  document.getElementById('btn-copy-json')?.addEventListener('click', () => {
    navigator.clipboard?.writeText(jsonInput.value);
    const btn = document.getElementById('btn-copy-json');
    btn.textContent = 'Copied!';
    setTimeout(() => { btn.textContent = 'Copy'; }, 1500);
  });

  // Initialize with wheelchair preset
  setPreset('wheelchair', true);

  // Trigger initial calculation
  setTimeout(() => {
    calculateRoute();
  }, 400);
});
