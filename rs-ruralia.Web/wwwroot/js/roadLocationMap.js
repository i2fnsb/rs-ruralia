// Road Location Map Editor using Leaflet
window.roadLocationMap = {
    maps: new Map(),

    // Base Map Definitions (same as leaflet-map.js)
    baseMaps: {
        streets: {
            url: 'https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png',
            attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
            maxZoom: 19
        },
        satellite: {
            url: 'https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}',
            attribution: 'Tiles &copy; Esri &mdash; Source: Esri, i-cubed, USDA, USGS, AEX, GeoEye, Getmapping, Aerogrid, IGN, IGP, UPR-EGP, and the GIS User Community',
            maxZoom: 19
        },
        topographic: {
            url: 'https://{s}.tile.opentopomap.org/{z}/{x}/{y}.png',
            attribution: 'Map data: &copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors, <a href="http://viewfinderpanoramas.org">SRTM</a> | Map style: &copy; <a href="https://opentopomap.org">OpenTopoMap</a> (<a href="https://creativecommons.org/licenses/by-sa/3.0/">CC-BY-SA</a>)',
            maxZoom: 17
        },
        terrain: {
            url: 'https://stamen-tiles-{s}.a.ssl.fastly.net/terrain/{z}/{x}/{y}.png',
            attribution: 'Map tiles by <a href="http://stamen.com">Stamen Design</a>, <a href="http://creativecommons.org/licenses/by/3.0">CC BY 3.0</a> &mdash; Map data &copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
            maxZoom: 18
        },
        dark: {
            url: 'https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png',
            attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors &copy; <a href="https://carto.com/attributions">CARTO</a>',
            maxZoom: 19
        },
        light: {
            url: 'https://{s}.basemaps.cartocdn.com/light_all/{z}/{x}/{y}{r}.png',
            attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors &copy; <a href="https://carto.com/attributions">CARTO</a>',
            maxZoom: 19
        }
    },

    initialize: function (mapId, lat, lon, zoom, readOnly, existingWkt, dotNetRef) {
        // Initialize Leaflet map
        const map = L.map(mapId).setView([lat, lon], zoom);

        // Add OpenStreetMap base layer (default)
        const baseLayer = L.tileLayer(this.baseMaps.streets.url, {
            attribution: this.baseMaps.streets.attribution,
            maxZoom: this.baseMaps.streets.maxZoom
        }).addTo(map);

        // Create feature group for drawn items
        const drawnItems = new L.FeatureGroup();
        map.addLayer(drawnItems);

        // Create feature group for service area
        const serviceAreaLayer = new L.FeatureGroup();
        map.addLayer(serviceAreaLayer);

        // Store map data
        const mapData = {
            map: map,
            baseLayer: baseLayer,
            drawnItems: drawnItems,
            serviceAreaLayer: serviceAreaLayer,
            dotNetRef: dotNetRef,
            readOnly: readOnly,
            currentLine: null,
            currentBaseMap: 'streets'
        };

        this.maps.set(mapId, mapData);

        // Add basemap selector control
        this.addBasemapSelector(mapId);

        // Add drawing control if not read-only
        if (!readOnly) {
            const drawControl = new L.Control.Draw({
                draw: {
                    polyline: {
                        shapeOptions: {
                            color: '#00BFFF',
                            weight: 4
                        }
                    },
                    polygon: false,
                    circle: false,
                    rectangle: false,
                    marker: false,
                    circlemarker: false
                },
                edit: {
                    featureGroup: drawnItems,
                    remove: false
                }
            });

            // Store draw control
            mapData.drawControl = drawControl;

            // Handle draw events
            map.on(L.Draw.Event.CREATED, function (e) {
                const layer = e.layer;

                // Remove existing line if any
                drawnItems.clearLayers();

                // Add new line
                drawnItems.addLayer(layer);
                mapData.currentLine = layer;

                // Convert to WKT and notify Blazor
                const wkt = this.layerToWkt(layer);
                const distance = this.calculateDistanceInFeet(layer);
                dotNetRef.invokeMethodAsync('OnLineCreated', wkt, distance);
            }.bind(this));

            map.on(L.Draw.Event.EDITED, function (e) {
                const layers = e.layers;
                layers.eachLayer(function (layer) {
                    mapData.currentLine = layer;
                    const wkt = this.layerToWkt(layer);
                    const distance = this.calculateDistanceInFeet(layer);
                    dotNetRef.invokeMethodAsync('OnLineEdited', wkt, distance);
                }.bind(this));
            }.bind(this));
        }

        // Load existing geometry if provided
        if (existingWkt) {
            this.loadWktGeometry(mapId, existingWkt);
        }

        // Invalidate size after a short delay to ensure proper rendering
        setTimeout(() => {
            map.invalidateSize();
        }, 100);
    },

    addBasemapSelector: function(mapId) {
        const mapData = this.maps.get(mapId);
        if (!mapData) return;

        // Create basemap selector control
        const BasemapControl = L.Control.extend({
            options: {
                position: 'topright'
            },

            onAdd: function (map) {
                const container = L.DomUtil.create('div', 'leaflet-bar leaflet-control leaflet-control-custom');
                container.style.backgroundColor = 'white';
                container.style.padding = '5px';
                container.style.cursor = 'pointer';
                container.innerHTML = '<div style="width: 150px;"><select id="basemap-selector-' + mapId + '" style="width: 100%; padding: 5px; border: none; background: white; cursor: pointer;">' +
                    '<option value="streets">Streets</option>' +
                    '<option value="satellite">Satellite</option>' +
                    '<option value="topographic">Topographic</option>' +
                    '<option value="terrain">Terrain</option>' +
                    '<option value="light">Light</option>' +
                    '<option value="dark">Dark</option>' +
                    '</select></div>';

                // Prevent click propagation
                L.DomEvent.disableClickPropagation(container);

                return container;
            }
        });

        mapData.map.addControl(new BasemapControl());

        // Add event listener for basemap change
        setTimeout(() => {
            const selector = document.getElementById('basemap-selector-' + mapId);
            if (selector) {
                selector.addEventListener('change', (e) => {
                    this.changeBaseMap(mapId, e.target.value);
                });
            }
        }, 100);
    },

    changeBaseMap: function(mapId, baseMapType) {
        const mapData = this.maps.get(mapId);
        if (!mapData) return;

        const baseMapConfig = this.baseMaps[baseMapType];
        if (!baseMapConfig) return;

        // Remove existing base layer
        if (mapData.baseLayer) {
            mapData.map.removeLayer(mapData.baseLayer);
        }

        // Add new base layer
        mapData.baseLayer = L.tileLayer(baseMapConfig.url, {
            attribution: baseMapConfig.attribution,
            maxZoom: baseMapConfig.maxZoom
        }).addTo(mapData.map);

        // Move to back
        mapData.baseLayer.bringToBack();
        mapData.currentBaseMap = baseMapType;
    },

    loadServiceArea: function(mapId, geoJsonString) {
        const mapData = this.maps.get(mapId);
        if (!mapData) return;

        try {
            const geoJsonData = JSON.parse(geoJsonString);

            // Clear existing service area
            mapData.serviceAreaLayer.clearLayers();

            // Add service area boundary
            const layer = L.geoJSON(geoJsonData, {
                style: {
                    color: '#FF5722',
                    weight: 2,
                    opacity: 0.8,
                    fillOpacity: 0.1,
                    dashArray: '5, 10'
                }
            });

            layer.addTo(mapData.serviceAreaLayer);

            // Only fit bounds if no road line exists
            if (!mapData.currentLine) {
                const bounds = layer.getBounds();
                if (bounds.isValid()) {
                    mapData.map.fitBounds(bounds, { padding: [50, 50] });
                }
            }
        } catch (error) {
            console.error('Error loading service area:', error);
        }
    },

    startDrawing: function (mapId) {
        const mapData = this.maps.get(mapId);
        if (!mapData || mapData.readOnly) return;

        // Trigger polyline drawing
        new L.Draw.Polyline(mapData.map, mapData.drawControl.options.draw.polyline).enable();
    },

    clearLine: function (mapId) {
        const mapData = this.maps.get(mapId);
        if (!mapData) return;

        mapData.drawnItems.clearLayers();
        mapData.currentLine = null;
    },

    loadWktGeometry: function (mapId, wkt) {
        const mapData = this.maps.get(mapId);
        if (!mapData) return;

        try {
            // Parse WKT to GeoJSON
            const geojson = this.wktToGeoJson(wkt);

            if (geojson) {
                // Clear existing layers
                mapData.drawnItems.clearLayers();

                // Add GeoJSON to map
                const layer = L.geoJSON(geojson, {
                    style: {
                        color: '#00BFFF',
                        weight: 4
                    }
                });

                // Get the actual Leaflet layer (polyline)
                layer.eachLayer(function (l) {
                    mapData.drawnItems.addLayer(l);
                    mapData.currentLine = l;

                    // Calculate and notify distance for existing geometry
                    if (mapData.dotNetRef && !mapData.readOnly) {
                        const distance = this.calculateDistanceInFeet(l);
                        mapData.dotNetRef.invokeMethodAsync('OnExistingLineLoaded', distance);
                    }
                }.bind(this));

                // Fit bounds to show the line
                if (mapData.drawnItems.getBounds().isValid()) {
                    mapData.map.fitBounds(mapData.drawnItems.getBounds(), { padding: [50, 50] });
                }
            }
        } catch (error) {
            console.error('Error loading WKT geometry:', error);
        }
    },

    calculateDistanceInFeet: function(layer) {
        if (!(layer instanceof L.Polyline)) return 0;

        // Get coordinates
        const latlngs = layer.getLatLngs();
        if (latlngs.length < 2) return 0;

        let totalDistanceFeet = 0;

        // Calculate distance for each segment
        for (let i = 0; i < latlngs.length - 1; i++) {
            const from = latlngs[i];
            const to = latlngs[i + 1];

            // Convert WGS84 to NAD83 Alaska State Plane Zone 3 (EPSG:26933)
            const fromProjected = this.wgs84ToAlaskaStatePlane(from.lat, from.lng);
            const toProjected = this.wgs84ToAlaskaStatePlane(to.lat, to.lng);

            // Calculate distance in meters using Pythagorean theorem
            const dx = toProjected.x - fromProjected.x;
            const dy = toProjected.y - fromProjected.y;
            const distanceMeters = Math.sqrt(dx * dx + dy * dy);

            // Convert meters to US Survey Feet
            // 1 meter = 3.28083989501312 US Survey Feet
            const distanceFeet = distanceMeters * 3.28083989501312;
            totalDistanceFeet += distanceFeet;
        }

        return Math.round(totalDistanceFeet * 100) / 100; // Round to 2 decimal places
    },

    wgs84ToAlaskaStatePlane: function(lat, lon) {
        // Define coordinate systems using proj4js
        // EPSG:4326 = WGS84 (input)
        // EPSG:26933 = NAD83 / Alaska zone 3 (output)

        // Define EPSG:26933 projection (NAD83 Alaska State Plane Zone 3)
        // Transverse Mercator projection
        proj4.defs("EPSG:26933", "+proj=tmerc +lat_0=54 +lon_0=-146 +k=0.9999 +x_0=500000 +y_0=0 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs");

        // WGS84 (this is built-in to proj4, but defining explicitly for clarity)
        proj4.defs("EPSG:4326", "+proj=longlat +datum=WGS84 +no_defs");

        // Transform from WGS84 (lon, lat) to Alaska State Plane Zone 3 (x, y in meters)
        const projected = proj4("EPSG:4326", "EPSG:26933", [lon, lat]);

        return {
            x: projected[0], // Easting in meters
            y: projected[1]  // Northing in meters
        };
    },

    layerToWkt: function (layer) {
        if (layer instanceof L.Polyline) {
            const latlngs = layer.getLatLngs();
            const coords = latlngs.map(latlng => `${latlng.lng} ${latlng.lat}`).join(', ');
            return `LINESTRING (${coords})`;
        }
        return null;
    },

    wktToGeoJson: function (wkt) {
        try {
            // Simple WKT parser for LINESTRING
            if (wkt.startsWith('LINESTRING')) {
                const coordsStr = wkt.match(/\(([^)]+)\)/)[1];
                const coords = coordsStr.split(',').map(pair => {
                    const [lon, lat] = pair.trim().split(' ').map(parseFloat);
                    return [lon, lat];
                });

                return {
                    type: 'LineString',
                    coordinates: coords
                };
            }
        } catch (error) {
            console.error('Error parsing WKT:', error);
        }
        return null;
    },

    dispose: function (mapId) {
        const mapData = this.maps.get(mapId);
        if (mapData) {
            if (mapData.map) {
                mapData.map.remove();
            }
            this.maps.delete(mapId);
        }
    }
};
