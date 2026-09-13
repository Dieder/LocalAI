window.travelMap = {
    map: null,

    initialize: function () {
        if (this.map) {
            this.map.remove();
        }

        this.map = L.map('travelMap');
        this.map.setView([-30, 24], 4);

        L.tileLayer(
            'https://tile.openstreetmap.org/{z}/{x}/{y}.png',
            {
                attribution: '&copy; OpenStreetMap'
            }
        ).addTo(this.map);

        this.markers = [];
        this.polylines = [];
        this.routeLayers = [];
    },

    refresh: function (tripGraph) {
        var self = this;

        // Clear previous markers and polylines
        this.markers.forEach(m => this.map.removeLayer(m));
        this.polylines.forEach(p => this.map.removeLayer(p));
        this.routeLayers.forEach(r => this.map.removeLayer(r));
        this.markers = [];
        this.polylines = [];
        this.routeLayers = [];

        var bounds = L.latLngBounds();

        if (!tripGraph || !tripGraph.legs || tripGraph.legs.length === 0) {
            return;
        }

        tripGraph.legs.forEach(function (leg) {
            var from = leg.from;
            var to = leg.to;

            if (!from || !to) return;

            var fromLatLng = [from.latitude, from.longitude];
            var toLatLng = [to.latitude, to.longitude];

            bounds.extend(fromLatLng);
            bounds.extend(toLatLng);

            // Markers
            var fromIcon = L.divIcon({
                html: '📍',
                className: 'travel-marker',
                iconSize: [30, 30]
            });
            var toIcon = window.transportIcon(leg.transport);

            var fromMarker = L.marker(fromLatLng, { icon: fromIcon }).addTo(self.map)
                .bindPopup('<strong>' + from.place + '</strong>' + (from.warning ? '<br/><span style="color:red">⚠ ' + from.warning + '</span>' : ''));
            this.markers.push(fromMarker);

            var toMarker = L.marker(toLatLng, { icon: toIcon }).addTo(self.map)
                .bindPopup('<strong>' + to.place + '</strong>' + (to.warning ? '<br/><span style="color:red">⚠ ' + to.warning + '</span>' : ''));
            this.markers.push(toMarker);

            // If we have cached route geometry (GeoJSON), use it; otherwise draw straight line
            if (leg.routeGeoJson) {
                try {
                    var geoJson = JSON.parse(leg.routeGeoJson);
                    var routeLayer = L.geoJSON(geoJson, {
                        style: {
                            color: getTransportColor(leg.transport),
                            weight: leg.transport === 1 ? 3 : 5,
                            opacity: 0.8,
                            dashArray: leg.transport === 1 ? '10,10' : (leg.transport === 2 ? '5,10' : null)
                        }
                    }).addTo(self.map);
                    self.routeLayers.push(routeLayer);
                } catch (e) {
                    console.error('Failed to parse route GeoJSON', e);
                    drawStraightLine(fromLatLng, toLatLng, leg.transport, self.map, self.polylines);
                }
            } else {
                drawStraightLine(fromLatLng, toLatLng, leg.transport, self.map, self.polylines);
            }
        });

        if (bounds.isValid()) {
            this.map.fitBounds(bounds, { padding: [50, 50] });
        }
    },

    reloadRoutes: function (tripGraph) {
        this.refresh(tripGraph);
    }
};

function getTransportColor(transport) {
    switch (transport) {
        case 0: return 'green';   // Car
        case 1: return 'red';     // Plane
        case 2: return 'navy';    // Boat
        case 3: return 'orange';  // Walking
        case 4: return 'purple';  // Bicycle
        default: return 'blue';
    }
}

function drawStraightLine(fromLatLng, toLatLng, transport, map, polylinesArray) {
    var color = getTransportColor(transport);
    var dashArray = null;

    if (transport === 1) { // Plane
        dashArray = '10,10';
    } else if (transport === 2) { // Boat
        dashArray = '5,10';
    }

    var polyline = L.polyline([fromLatLng, toLatLng], {
        color: color,
        weight: transport === 1 ? 3 : 5,
        opacity: 0.8,
        dashArray: dashArray
    }).addTo(map);

    polylinesArray.push(polyline);
}

function transportIcon(type) {
    var icon = "🚗";

    switch (type) {
        case 0: icon = "🚗"; break; // Car
        case 1: icon = "✈️"; break;  // Plane
        case 2: icon = "⛵"; break;  // Boat
        case 3: icon = "🚶"; break;  // Walking
        case 4: icon = "🚴"; break;  // Bicycle
    }

    return L.divIcon({
        html: icon,
        className: "",
        iconSize: [30, 30]
    });
}
