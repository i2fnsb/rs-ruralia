// Road Location Map Editor using Leaflet
window.roadLocationMap = {
    maps: new Map(),

    initialize: function (mapId, lat, lon, zoom, readOnly, existingWkt, dotNetRef) {
        // Initialize Leaflet map
        const map = L.map(mapId).setView([lat, lon], zoom);

        // Add OpenStreetMap base layer
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
            maxZoom: 19
        }).addTo(map);

        // Create feature group for drawn items
        const drawnItems = new L.FeatureGroup();
        map.addLayer(drawnItems);

        // Store map data
        const mapData = {
            map: map,
            drawnItems: drawnItems,
            dotNetRef: dotNetRef,
            readOnly: readOnly,
            currentLine: null
        };

        this.maps.set(mapId, mapData);

        // Add drawing control if not read-only
        if (!readOnly) {
            const drawControl = new L.Control.Draw({
                draw: {
                    polyline: {
                        shapeOptions: {
                            color: '#2196F3',
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
                dotNetRef.invokeMethodAsync('OnLineCreated', wkt);
            }.bind(this));

            map.on(L.Draw.Event.EDITED, function (e) {
                const layers = e.layers;
                layers.eachLayer(function (layer) {
                    mapData.currentLine = layer;
                    const wkt = this.layerToWkt(layer);
                    dotNetRef.invokeMethodAsync('OnLineEdited', wkt);
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
                        color: mapData.readOnly ? '#666' : '#2196F3',
                        weight: 4
                    }
                });

                // Get the actual Leaflet layer (polyline)
                layer.eachLayer(function (l) {
                    mapData.drawnItems.addLayer(l);
                    mapData.currentLine = l;
                });

                // Fit bounds to show the line
                if (mapData.drawnItems.getBounds().isValid()) {
                    mapData.map.fitBounds(mapData.drawnItems.getBounds(), { padding: [50, 50] });
                }
            }
        } catch (error) {
            console.error('Error loading WKT geometry:', error);
        }
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
