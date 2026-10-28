#!/usr/bin/env python3
"""
Descarga y prepara los datos geográficos abiertos del entorno de la plaza.

Genera en Assets/_Guadalupe/Data/Geo:
  - osm_plaza.json       Contorno de la plaza, recinto del monasterio y fuentes (OpenStreetMap, ODbL).
  - dem_plaza.json       Malla de elevaciones (EU-DEM v1.1 25 m, Copernicus) vía OpenTopoData.
  - catastro_plaza.json  Plantas de edificios con número de plantas (D.G. del Catastro, INSPIRE).

Uso:
    python3 Tools/GeoData/descargar_datos.py [--solo osm|dem|catastro]

Solo usa la biblioteca estándar de Python 3.
"""

import argparse
import json
import math
import os
import time
import urllib.parse
import urllib.request
import xml.etree.ElementTree as ET

# Origen local: nodo de la Plaza de Santa María de Guadalupe en OSM.
ORIGIN_LAT = 39.4519182
ORIGIN_LON = -5.3274418
HALF_SIZE_M = 200
DEM_STEP_M = 10
UTM_ZONE = 30

OUT_DIR = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "_Guadalupe", "Data", "Geo")
USER_AGENT = "PlazaGuadalupe-proyecto-academico"
OVERPASS_SERVERS = [
    "https://overpass-api.de/api/interpreter",
    "https://maps.mail.ru/osm/tools/overpass/api/interpreter",
    "https://overpass.kumi.systems/api/interpreter",
]


def http_get(url, data=None, timeout=120):
    request = urllib.request.Request(url, data=data, headers={"User-Agent": USER_AGENT})
    with urllib.request.urlopen(request, timeout=timeout) as response:
        return response.read()


def write_json(name, content):
    path = os.path.join(OUT_DIR, name)
    with open(path, "w", encoding="utf-8") as f:
        json.dump(content, f, ensure_ascii=False, separators=(",", ":"))
    print(f"  -> {os.path.normpath(path)}")


def bbox_latlon(margin=0):
    d_lat = (HALF_SIZE_M + margin) / 110540.0
    d_lon = (HALF_SIZE_M + margin) / (111320.0 * math.cos(math.radians(ORIGIN_LAT)))
    return ORIGIN_LAT - d_lat, ORIGIN_LON - d_lon, ORIGIN_LAT + d_lat, ORIGIN_LON + d_lon


# Conversión UTM (ETRS89 / GRS80) <-> geográficas. Fórmulas de Snyder (USGS, 1987).
_A = 6378137.0
_F = 1 / 298.257222101
_E2 = _F * (2 - _F)
_K0 = 0.9996


def latlon_to_utm(lat, lon, zone=UTM_ZONE):
    ep2 = _E2 / (1 - _E2)
    lon0 = math.radians((zone - 1) * 6 - 180 + 3)
    la, lo = math.radians(lat), math.radians(lon)
    n = _A / math.sqrt(1 - _E2 * math.sin(la) ** 2)
    t = math.tan(la) ** 2
    c = ep2 * math.cos(la) ** 2
    a = math.cos(la) * (lo - lon0)
    m = _A * ((1 - _E2 / 4 - 3 * _E2 ** 2 / 64 - 5 * _E2 ** 3 / 256) * la
              - (3 * _E2 / 8 + 3 * _E2 ** 2 / 32 + 45 * _E2 ** 3 / 1024) * math.sin(2 * la)
              + (15 * _E2 ** 2 / 256 + 45 * _E2 ** 3 / 1024) * math.sin(4 * la)
              - (35 * _E2 ** 3 / 3072) * math.sin(6 * la))
    x = _K0 * n * (a + (1 - t + c) * a ** 3 / 6 + (5 - 18 * t + t * t + 72 * c - 58 * ep2) * a ** 5 / 120) + 500000
    y = _K0 * (m + n * math.tan(la) * (a * a / 2 + (5 - t + 9 * c + 4 * c * c) * a ** 4 / 24
                                       + (61 - 58 * t + t * t + 600 * c - 330 * ep2) * a ** 6 / 720))
    return x, y


def utm_to_latlon(x, y, zone=UTM_ZONE):
    ep2 = _E2 / (1 - _E2)
    e1 = (1 - math.sqrt(1 - _E2)) / (1 + math.sqrt(1 - _E2))
    lon0 = math.radians((zone - 1) * 6 - 180 + 3)
    m = y / _K0
    mu = m / (_A * (1 - _E2 / 4 - 3 * _E2 ** 2 / 64 - 5 * _E2 ** 3 / 256))
    p1 = (mu + (3 * e1 / 2 - 27 * e1 ** 3 / 32) * math.sin(2 * mu)
          + (21 * e1 ** 2 / 16 - 55 * e1 ** 4 / 32) * math.sin(4 * mu)
          + (151 * e1 ** 3 / 96) * math.sin(6 * mu))
    n1 = _A / math.sqrt(1 - _E2 * math.sin(p1) ** 2)
    t1 = math.tan(p1) ** 2
    c1 = ep2 * math.cos(p1) ** 2
    r1 = _A * (1 - _E2) / (1 - _E2 * math.sin(p1) ** 2) ** 1.5
    d = (x - 500000) / (n1 * _K0)
    lat = p1 - (n1 * math.tan(p1) / r1) * (d * d / 2 - (5 + 3 * t1 + 10 * c1 - 4 * c1 * c1 - 9 * ep2) * d ** 4 / 24
                                          + (61 + 90 * t1 + 298 * c1 + 45 * t1 * t1 - 252 * ep2 - 3 * c1 * c1) * d ** 6 / 720)
    lon = lon0 + (d - (1 + 2 * t1 + c1) * d ** 3 / 6
                  + (5 - 2 * c1 + 28 * t1 - 3 * c1 * c1 + 8 * ep2 + 24 * t1 * t1) * d ** 5 / 120) / math.cos(p1)
    return math.degrees(lat), math.degrees(lon)


def download_osm():
    print("OpenStreetMap (Overpass)...")
    s, w, n, e = bbox_latlon()
    bbox = f"{s:.6f},{w:.6f},{n:.6f},{e:.6f}"
    query = (f'[out:json][timeout:60];('
             f'way["place"="square"]({bbox});'
             f'way["amenity"="monastery"]({bbox});'
             f'node["amenity"="fountain"]({bbox});'
             f');out geom;')
    data = urllib.parse.urlencode({"data": query}).encode()
    for server in OVERPASS_SERVERS:
        try:
            content = json.loads(http_get(server, data=data))
            write_json("osm_plaza.json", content)
            return
        except Exception as ex:  # noqa: BLE001 - se prueba el siguiente servidor
            print(f"  fallo en {server}: {ex}")
    raise RuntimeError("Ningún servidor Overpass respondió.")


def download_dem():
    print("Elevaciones EU-DEM (OpenTopoData)...")
    m_lat = 110540.0
    m_lon = 111320.0 * math.cos(math.radians(ORIGIN_LAT))
    size = HALF_SIZE_M * 2
    samples = size // DEM_STEP_M + 1
    points = []
    for j in range(samples):
        for i in range(samples):
            x = -HALF_SIZE_M + i * DEM_STEP_M
            z = -HALF_SIZE_M + j * DEM_STEP_M
            points.append((ORIGIN_LAT + z / m_lat, ORIGIN_LON + x / m_lon))

    heights = []
    for k in range(0, len(points), 100):
        locations = "|".join(f"{a:.7f},{b:.7f}" for a, b in points[k:k + 100])
        url = f"https://api.opentopodata.org/v1/eudem25m?locations={locations}"
        for _ in range(5):
            try:
                result = json.loads(http_get(url, timeout=60))
                if result.get("status") == "OK":
                    break
            except Exception as ex:  # noqa: BLE001
                print(f"  reintento: {ex}")
            time.sleep(2)
        heights += [round(r["elevation"], 2) for r in result["results"]]
        time.sleep(1.1)  # Límite de la API pública: 1 petición por segundo.

    write_json("dem_plaza.json", {
        "source": "EU-DEM v1.1 25 m (Copernicus) via OpenTopoData",
        "originLat": ORIGIN_LAT, "originLon": ORIGIN_LON,
        "sizeMeters": size, "stepMeters": DEM_STEP_M, "samples": samples,
        "heights": heights,
    })


def download_catastro():
    print("Catastro (INSPIRE WFS, BuildingPart)...")
    x0, y0 = latlon_to_utm(ORIGIN_LAT, ORIGIN_LON)
    bbox = f"{x0 - HALF_SIZE_M:.0f},{y0 - HALF_SIZE_M:.0f},{x0 + HALF_SIZE_M:.0f},{y0 + HALF_SIZE_M:.0f}"
    url = ("http://ovc.catastro.meh.es/INSPIRE/wfsBU.aspx?service=WFS&version=2.0.0&request=GetFeature"
           f"&typeNames=BU:BuildingPart&bbox={bbox}&SRSname=EPSG::25830")
    root = ET.fromstring(http_get(url))

    ns = {
        "gml": "http://www.opengis.net/gml/3.2",
        "bu-ext2d": "http://inspire.jrc.ec.europa.eu/schemas/bu-ext2d/2.0",
    }
    parts = []
    for part in root.iter("{http://inspire.jrc.ec.europa.eu/schemas/bu-ext2d/2.0}BuildingPart"):
        part_id = part.get("{http://www.opengis.net/gml/3.2}id", "").replace("ES.SDGC.BU.", "")
        floors_text = part.findtext("bu-ext2d:numberOfFloorsAboveGround", default="0", namespaces=ns)
        floors = int(floors_text) if floors_text.strip().lstrip("-").isdigit() else 0
        exterior = part.find(".//gml:exterior//gml:posList", ns)
        if exterior is None or floors <= 0:
            continue  # Partes sin plantas sobre rasante (patios, sótanos).

        values = [float(v) for v in exterior.text.split()]
        ring = [utm_to_latlon(values[i], values[i + 1]) for i in range(0, len(values), 2)]
        parts.append({
            "id": part_id,
            "building": part_id.split("_")[0],
            "floors": floors,
            "ring": [[round(lat, 7), round(lon, 7)] for lat, lon in ring],
        })

    write_json("catastro_plaza.json", {
        "source": "Dirección General del Catastro, servicio INSPIRE de edificios (CC BY 4.0)",
        "parts": parts,
    })
    print(f"  {len(parts)} partes de edificio")


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--solo", choices=["osm", "dem", "catastro"], help="descarga solo una fuente")
    args = parser.parse_args()

    os.makedirs(OUT_DIR, exist_ok=True)
    steps = {"osm": download_osm, "dem": download_dem, "catastro": download_catastro}
    for name, step in steps.items():
        if args.solo in (None, name):
            step()


if __name__ == "__main__":
    main()
