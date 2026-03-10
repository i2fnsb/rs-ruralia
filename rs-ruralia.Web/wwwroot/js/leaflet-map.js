// Leaflet Map JavaScript Utilities for Blazor

window.leafletMaps = {};

window.initializeMap = function (mapId, lat, lng, zoom) {
    try {
        // Create the map
        const map = L.map(mapId).setView([lat, lng], zoom);

        // Add OpenStreetMap tiles as default base layer
        const baseLayer = L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
            maxZoom: 19
        }).addTo(map);

        // Store the map instance and base layer
        window.leafletMaps[mapId] = {
            map: map,
            baseLayer: baseLayer,
            layers: {}
        };

        // Force map to recalculate its size after a short delay
        // This fixes tile loading issues when the container size isn't immediately available
        setTimeout(function() {
            map.invalidateSize();
        }, 100);

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

        // Invalidate size to ensure tiles load correctly
        setTimeout(function() {
            map.invalidateSize();
        }, 50);

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

window.invalidateMapSize = function (mapId) {
    try {
        const mapData = window.leafletMaps[mapId];
        if (!mapData) {
            console.error('Map not found:', mapId);
            return false;
        }

        mapData.map.invalidateSize();
        return true;
    } catch (error) {
        console.error('Error invalidating map size:', error);
        return false;
    }
};

// Base Map Definitions
window.baseMaps = {
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
};

window.changeBaseMap = function (mapId, baseMapType) {
    try {
        const mapData = window.leafletMaps[mapId];
        if (!mapData) {
            console.error('Map not found:', mapId);
            return false;
        }

        const baseMapConfig = window.baseMaps[baseMapType];
        if (!baseMapConfig) {
            console.error('Base map type not found:', baseMapType);
            return false;
        }

        // Remove existing base layer
        if (mapData.baseLayer) {
            mapData.map.removeLayer(mapData.baseLayer);
        }

        // Add new base layer
        mapData.baseLayer = L.tileLayer(baseMapConfig.url, {
            attribution: baseMapConfig.attribution,
            maxZoom: baseMapConfig.maxZoom
        }).addTo(mapData.map);

        return true;
    } catch (error) {
        console.error('Error changing base map:', error);
        return false;
    }
};

window.showLayer = function (mapId, layerName) {
    try {
        const mapData = window.leafletMaps[mapId];
        if (!mapData) {
            console.error('Map not found:', mapId);
            return false;
        }

        const layer = mapData.layers[layerName];
        if (layer && !mapData.map.hasLayer(layer)) {
            mapData.map.addLayer(layer);
        }

        return true;
    } catch (error) {
        console.error('Error showing layer:', error);
        return false;
    }
};

window.hideLayer = function (mapId, layerName) {
    try {
        const mapData = window.leafletMaps[mapId];
        if (!mapData) {
            console.error('Map not found:', mapId);
            return false;
        }

        const layer = mapData.layers[layerName];
        if (layer && mapData.map.hasLayer(layer)) {
            mapData.map.removeLayer(layer);
        }

        return true;
    } catch (error) {
        console.error('Error hiding layer:', error);
        return false;
    }
};

window.setLayerOpacity = function (mapId, layerName, opacity) {
    try {
        const mapData = window.leafletMaps[mapId];
        if (!mapData) {
            console.error('Map not found:', mapId);
            return false;
        }

        const layer = mapData.layers[layerName];
        if (layer) {
            layer.setStyle({
                opacity: opacity,
                fillOpacity: opacity * 0.5 // Fill is half the line opacity
            });
        }

        return true;
    } catch (error) {
        console.error('Error setting layer opacity:', error);
        return false;
    }
};
