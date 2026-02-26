// Leaflet Map JavaScript Utilities for Blazor

window.leafletMaps = {};

window.initializeMap = function (mapId, lat, lng, zoom) {
    try {
        // Create the map
        const map = L.map(mapId).setView([lat, lng], zoom);
        
        // Add OpenStreetMap tiles
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
            maxZoom: 19
        }).addTo(map);
        
        // Store the map instance
        window.leafletMaps[mapId] = {
            map: map,
            layers: {}
        };
        
        return true;
    } catch (error) {
        console.error('Error initializing map:', error);
        return false;
    }
};

window.addGeoJsonLayer = function (mapId, geoJsonString, title, layerName, styleOptions) {
    try {
        const mapData = window.leafletMaps[mapId];
        if (!mapData) {
            console.error('Map not found:', mapId);
            return false;
        }

        const map = mapData.map;
        layerName = layerName || 'geoJsonLayer';

        // Parse GeoJSON
        const geoJsonData = JSON.parse(geoJsonString);

        // Remove existing layer with this name if it exists
        if (mapData.layers[layerName]) {
            map.removeLayer(mapData.layers[layerName]);
        }

        // Default style options
        const defaultStyle = {
            color: '#3388ff',
            weight: 2,
            opacity: 0.8,
            fillOpacity: 0.3
        };

        // Merge custom style options
        const style = styleOptions ? { ...defaultStyle, ...styleOptions } : defaultStyle;

        // Create and add the GeoJSON layer
        const geoJsonLayer = L.geoJSON(geoJsonData, {
            onEachFeature: function (feature, layer) {
                if (feature.properties) {
                    let popupContent = '<div style="padding: 10px;">';

                    if (title) {
                        popupContent += `<h6>${title}</h6>`;
                    }

                    let count = 0;
                    for (const [key, value] of Object.entries(feature.properties)) {
                        if (count >= 10) break;
                        popupContent += `<strong>${key}:</strong> ${value}<br/>`;
                        count++;
                    }
                    popupContent += '</div>';

                    layer.bindPopup(popupContent);
                }
            },
            style: function(feature) {
                return style;
            }
        }).addTo(map);

        // Store the layer
        mapData.layers[layerName] = geoJsonLayer;

        // Only fit bounds for the primary layer (not roads)
        if (layerName === 'geoJsonLayer' || layerName === 'serviceArea') {
            const bounds = geoJsonLayer.getBounds();
            if (bounds.isValid()) {
                map.fitBounds(bounds, { padding: [50, 50] });
            }
        }

        return true;
    } catch (error) {
        console.error('Error adding GeoJSON layer:', error);
        return false;
    }
};

window.removeGeoJsonLayer = function (mapId, layerName) {
    try {
        const mapData = window.leafletMaps[mapId];
        if (!mapData) {
            console.error('Map not found:', mapId);
            return false;
        }

        layerName = layerName || 'geoJsonLayer';

        if (mapData.layers[layerName]) {
            mapData.map.removeLayer(mapData.layers[layerName]);
            delete mapData.layers[layerName];
        }

        return true;
    } catch (error) {
        console.error('Error removing GeoJSON layer:', error);
        return false;
    }
};

window.setMapCenter = function (mapId, lat, lng) {
    try {
        const mapData = window.leafletMaps[mapId];
        if (!mapData) {
            console.error('Map not found:', mapId);
            return false;
        }
        
        mapData.map.setView([lat, lng]);
        return true;
    } catch (error) {
        console.error('Error setting map center:', error);
        return false;
    }
};

window.setMapZoom = function (mapId, zoom) {
    try {
        const mapData = window.leafletMaps[mapId];
        if (!mapData) {
            console.error('Map not found:', mapId);
            return false;
        }
        
        mapData.map.setZoom(zoom);
        return true;
    } catch (error) {
        console.error('Error setting map zoom:', error);
        return false;
    }
};

window.disposeMap = function (mapId) {
    try {
        const mapData = window.leafletMaps[mapId];
        if (mapData) {
            mapData.map.remove();
            delete window.leafletMaps[mapId];
        }
        return true;
    } catch (error) {
        console.error('Error disposing map:', error);
        return false;
    }
};
