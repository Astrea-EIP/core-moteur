---
title: Routing API
sidebar_position: 4
---

# Routing API Specification

This document details the inputs and outputs of the Astrea routing engine (`core-moteur`).

---

## 1. Method Signatures

The primary routing entry points live in `AstreaEngine`:

```csharp
// Asynchronous (recommended)
Task<RouteResponse> AstreaEngine.AstreaRouteAsync(
    string host,
    List<PointResponse> points,
    string userJson,
    string locale = "fr"
);

// Synchronous wrapper (blocking)
RouteResponse AstreaEngine.AstreaRoute(
    string host,
    List<PointResponse> points,
    string userJson,
    string locale = "fr"
);
```

Low-level coordinate strings are supported directly via `Routes`:

```csharp
Task<RouteResponse> Routes.RouteAsync(
    string host,
    List<string> points,
    string userJson,
    string locale = "fr"
);
```

---

## 2. Inputs

| Parameter | Type | Required | Default | Description |
| :--- | :--- | :---: | :---: | :--- |
| `host` | `string` | **Yes** | — | Base URL of the GraphHopper backend (e.g. `"http://localhost:8989"`). |
| `points` | `List<PointResponse>` or `List<string>` | **Yes** | — | Ordered waypoints along the journey (minimum 2 points: origin and destination). |
| `userJson` | `string` | No | `""` or `"{}""` | JSON payload describing user constraints, disability profile, or fine-grained navigation preferences. |
| `locale` | `string` | No | `"fr"` | Language code for turn-by-turn navigation instructions (e.g. `"fr"`, `"en"`, `"de"`, `"es"`). |

### Waypoints (`points`)

Each waypoint can be supplied as a `PointResponse` object:

```csharp
public class PointResponse
{
    public double Lat { get; set; } // Latitude (WGS84, e.g. 47.2184)
    public double Lng { get; set; } // Longitude (WGS84, e.g. -1.5536)
}
```

Or as a comma-separated `"lat,lng"` string in `Routes.RouteAsync`:

```csharp
var points = new List<string>
{
    "47.2184,-1.5536", // Origin (Nantes center)
    "47.2140,-1.5580"  // Destination
};
```

---

### User Profile (`userJson`)

The `userJson` string can define a high-level preset, customized parameters, or a combination of both.

#### Standard Presets (`"profile"` or `"preset"`)

| Preset Name | Description | Default Rules Applied |
| :--- | :--- | :--- |
| `"wheelchair"` | Manual / electric wheelchair | Avoids stairs (`can_take_stairs: false`), avoids slopes > 8%, requires min width 0.9m, max speed 3.5 km/h. |
| `"blind"` | Visually impaired / blind | Avoids unpaved surfaces, penalizes steps/stairs, prioritizes marked pedestrian crossings, max speed 3.5 km/h. |
| `"crutches"` | Person using crutches | Strongly penalizes stairs (`stairs_penalty: 0.4`), avoids slopes > 10%, max speed 3.0 km/h. |
| `"walking"` / `"foot"` | Standard pedestrian | Standard pedestrian network, allows stairs, max speed 5.0 km/h. |

#### Custom Accessibility Fields

Any preset can be overridden or configured manually:

| Field | Type | Unit / Range | Description |
| :--- | :--- | :--- | :--- |
| `avoid_stairs` | `bool` | `true` / `false` | When `true`, completely forbids stairs/steps (`road_class == STEPS`). |
| `stairs_penalty` | `double` | `0.0` to `1.0` | Multiplier penalty for stairs (e.g. `0.2` = heavy penalty, `1.0` = normal). |
| `avoid_slopes` | `bool` | `true` / `false` | When `true`, avoids roads with slopes exceeding `max_slope`. |
| `max_slope` | `double` | `0` to `100` (%) | Maximum slope percentage before penalties or exclusions apply. |
| `has_wheels` | `bool` | `true` / `false` | Applies surface smoothness rules and drops kerb preferences. |
| `blindness` | `bool` | `true` / `false` | Avoids complex environments and prioritizes pedestrian crossings. |
| `width` | `double` | `0` to `10` (m) | Minimum corridor / door / sidewalk clearance required. |
| `length` | `double` | `0` to `10` (m) | Maximum vehicle / mobility aid length. |
| `max_speed` | `double` | `> 0` to `50` (km/h) | Walking or rolling speed used for travel time estimation. |
| `distance_influence` | `int` | `0` to `200` | Trade-off between shortest path and most comfortable path. |

#### Example `userJson` Payloads

**1. Preset only:**
```json
{
  "profile": "wheelchair"
}
```

**2. Preset with custom overrides:**
```json
{
  "profile": "wheelchair",
  "max_speed": 4.5,
  "max_slope": 6.0,
  "width": 0.8
}
```

**3. Fine-grained custom constraints:**
```json
{
  "avoid_stairs": true,
  "avoid_slopes": true,
  "max_slope": 5.0,
  "has_wheels": true,
  "max_speed": 3.0
}
```

---

## 3. Outputs (`RouteResponse`)

The method returns a structured `RouteResponse` object:

```csharp
public class RouteResponse
{
    public double Distance { get; set; }
    public long Time { get; set; }
    public double Ascend { get; set; }
    public double Descend { get; set; }
    public double[]? BBox { get; set; }
    public List<PointResponse> Points { get; set; } = new();
    public List<InstructionResponse> Instructions { get; set; } = new();
}
```

### Response Fields

| Property | JSON Key | Type | Description |
| :--- | :--- | :--- | :--- |
| `Distance` | `distance` | `double` | Total route distance in **meters** (e.g. `850.4`). |
| `Time` | `time` | `long` | Total estimated travel duration in **milliseconds** (e.g. `680000` = 11.3 min). Computed using the user profile speed. |
| `Ascend` | `ascend` | `double` | Total elevation gain in **meters** (via NASA SRTM). |
| `Descend` | `descend` | `double` | Total elevation loss in **meters** (via NASA SRTM). |
| `BBox` | `bbox` | `double[]?` | Bounding box `[minLng, minLat, maxLng, maxLat]` for one-call map centering (`map.fitBounds(bbox)`). |
| `Points` | `points` | `List<PointResponse>` | Ordered geographic coordinates tracing the full polyline of the path. |
| `Instructions` | `instructions` | `List<InstructionResponse>` | Turn-by-turn navigation instructions for voice guidance and UI navigation. |

---

### Coordinate Object (`PointResponse`)

```csharp
public class PointResponse
{
    public double Lat { get; set; }
    public double Lng { get; set; }
    public double? Elevation { get; set; }
}
```

| Property | JSON Key | Type | Description |
| :--- | :--- | :--- | :--- |
| `Lat` | `lat` | `double` | Latitude in decimal degrees (e.g. `47.2172`). |
| `Lng` | `lng` | `double` | Longitude in decimal degrees (e.g. `-1.5536`). |
| `Elevation` | `elevation` | `double?` | Elevation in meters above sea level (e.g. `24.5`), omitted if elevation is not in graph. |

---

### Step Instruction Object (`InstructionResponse`)

```csharp
public class InstructionResponse
{
    public string Text { get; set; } = string.Empty;
    public string StreetName { get; set; } = string.Empty;
    public double Distance { get; set; }
    public long Time { get; set; }
    public int Sign { get; set; }
    public int[] Interval { get; set; } = Array.Empty<int>();
    public double? Heading { get; set; }
    public double? LastHeading { get; set; }
}
```

| Property | JSON Key | Type | Description |
| :--- | :--- | :--- | :--- |
| `Text` | `text` | `string` | Localized navigation text (e.g. `"Tournez à droite sur Rue de la Paix"`). |
| `StreetName` | `street_name` | `string` | Name of the street or pathway. |
| `Distance` | `distance` | `double` | Distance of this step in meters. |
| `Time` | `time` | `long` | Duration of this step in milliseconds. |
| `Sign` | `sign` | `int` | Turn direction indicator code (see table below). |
| `Interval` | `interval` | `int[]` | Start and end index `[from, to]` pointing to the corresponding slice in the `points` array. Used to highlight the current segment on the map. |
| `Heading` | `heading` | `double?` | Initial orientation angle in degrees (0 = North, 90 = East, 180 = South, 270 = West). |
| `LastHeading` | `last_heading` | `double?` | Heading angle at the end of the maneuver. |

#### Direction Codes (`sign`)

| Code | Maneuver | Description |
| :---: | :--- | :--- |
| `-3` | Turn Sharp Left | Sharp left turn |
| `-2` | Turn Left | Normal left turn |
| `-1` | Turn Slight Left | Slight left / keep left |
| `0` | Continue Straight | Continue on current street |
| `1` | Turn Slight Right | Slight right / keep right |
| `2` | Turn Right | Normal right turn |
| `3` | Turn Sharp Right | Sharp right turn |
| `4` | Destination Reached | Reached final waypoint |
| `5` | Waypoint Reached | Reached intermediate waypoint |
| `6` | Roundabout | Enter roundabout |

---

## 4. Full JSON Response Example

```json
{
  "distance": 850.4,
  "time": 680000,
  "ascend": 14.2,
  "descend": 6.8,
  "bbox": [-1.5580, 47.2140, -1.5510, 47.2190],
  "points": [
    {
      "lat": 47.2172,
      "lng": -1.5536,
      "elevation": 24.5
    },
    {
      "lat": 47.2175,
      "lng": -1.5539,
      "elevation": 25.1
    },
    {
      "lat": 47.2180,
      "lng": -1.5545,
      "elevation": 26.0
    }
  ],
  "instructions": [
    {
      "text": "Continuer sur Rue de Strasbourg",
      "street_name": "Rue de Strasbourg",
      "distance": 120.5,
      "time": 96400,
      "sign": 0,
      "interval": [0, 1],
      "heading": 84.5,
      "last_heading": 85.0
    },
    {
      "text": "Tournez à droite sur Cours des 50 Otages",
      "street_name": "Cours des 50 Otages",
      "distance": 729.9,
      "time": 583600,
      "sign": 2,
      "interval": [1, 2],
      "heading": 174.2,
      "last_heading": 174.5
    },
    {
      "text": "Arrivée à destination",
      "street_name": "",
      "distance": 0.0,
      "time": 0,
      "sign": 4,
      "interval": [2, 2]
    }
  ]
}
```

---

## 5. Error Handling

| Exception | Occurs When |
| :--- | :--- |
| `ValidationException` | Fewer than 2 points are provided, invalid coordinate format, invalid `userJson` syntax, or profile constraints out of bounds. |
| `InvalidDataException` | GraphHopper returns an error (e.g. points outside map boundaries, impossible route, or malformed engine response). |
| `HttpRequestException` | GraphHopper backend cannot be reached (host down, wrong port, network issue). |
