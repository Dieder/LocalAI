window.travelMap = {

    map: null,

    TRANSPORT: {
        CAR: 0,
        PLANE: 1,
        BOAT: 2,
        WALKING: 3,
        CYCLE: 4
    },

    markers: [],
    polylines: [],
    routeLayers: [],

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

        this.markers.forEach(function (m) {
            self.map.removeLayer(m);
        });

        this.polylines.forEach(function (p) {
            self.map.removeLayer(p);
        });

        this.routeLayers.forEach(function (r) {
            self.map.removeLayer(r);
        });

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

            if (!from || !to) {
                return;
            }

            if (from.latitude == null ||
                from.longitude == null ||
                to.latitude == null ||
                to.longitude == null) {
                return;
            }

            var fromLatLng = [
                from.latitude,
                from.longitude
            ];

            var toLatLng = [
                to.latitude,
                to.longitude
            ];

            bounds.extend(fromLatLng);
            bounds.extend(toLatLng);

            var fromIcon = L.divIcon({
                html: "📍",
                className: "travel-marker",
                iconSize: [30, 30]
            });

            var toIcon = transportIcon(leg.transport);

            var fromMarker = L.marker(fromLatLng, {
                icon: fromIcon
            })
                .addTo(self.map)
                .bindPopup("<strong>" + from.place + "</strong>");

            self.markers.push(fromMarker);

            var toMarker = L.marker(toLatLng, {
                icon: toIcon
            })
                .addTo(self.map)
                .bindPopup("<strong>" + to.place + "</strong>");

            self.markers.push(toMarker);

            if (leg.routeGeoJson) {

                try {

                    var geoJson = JSON.parse(leg.routeGeoJson);

                    var routeLayer = L.geoJSON(geoJson, {
                        style: {
                            color: getTransportColor(leg.transport),
                            weight: 5,
                            opacity: 0.8
                        }
                    }).addTo(self.map);

                    self.routeLayers.push(routeLayer);

                }
                catch (e) {

                    console.error("GeoJSON parse failed", e);

                    drawTransportLine(
                        self.map,
                        fromLatLng,
                        toLatLng,
                        leg.transport,
                        self.polylines
                    );
                }

            } else {

                drawTransportLine(
                    self.map,
                    fromLatLng,
                    toLatLng,
                    leg.transport,
                    self.polylines
                );
            }

            var midpoint = [
                (fromLatLng[0] + toLatLng[0]) / 2,
                (fromLatLng[1] + toLatLng[1]) / 2
            ];

            var middleIcon = L.marker(midpoint, {
                icon: transportIcon(leg.transport)
            }).addTo(self.map);

            self.markers.push(middleIcon);
        });

        if (bounds.isValid()) {
            fitMapToBounds(this.map, bounds);
        }
    },

    reloadRoutes: function (tripGraph) {
        this.refresh(tripGraph);
    }
};

function fitMapToBounds(map, bounds) {

    var southWest = bounds.getSouthWest();
    var northEast = bounds.getNorthEast();

    var marginLat = 10 / 111;

    var centerLat = bounds.getCenter().lat;

    var marginLng =
        10 / (111 * Math.cos(centerLat * Math.PI / 180));

    var expandedBounds = L.latLngBounds(
        [
            southWest.lat - marginLat,
            southWest.lng - marginLng
        ],
        [
            northEast.lat + marginLat,
            northEast.lng + marginLng
        ]
    );

    map.fitBounds(expandedBounds);
}

function getTransportColor(transport) {

    switch (transport) {

        case window.travelMap.TRANSPORT.CAR:
            return "green";

        case window.travelMap.TRANSPORT.PLANE:
            return "red";

        case window.travelMap.TRANSPORT.BOAT:
            return "navy";

        case window.travelMap.TRANSPORT.WALKING:
            return "orange";

        case window.travelMap.TRANSPORT.CYCLE:
            return "purple";

        default:
            return "blue";
    }
}

function drawTransportLine(
    map,
    fromLatLng,
    toLatLng,
    transport,
    polylinesArray) {

    if (transport === window.travelMap.TRANSPORT.PLANE) {

        drawCurvedPlaneLine(
            map,
            fromLatLng,
            toLatLng,
            polylinesArray
        );

        return;
    }

    drawStraightLine(
        map,
        fromLatLng,
        toLatLng,
        transport,
        polylinesArray
    );
}

function drawCurvedPlaneLine(
    map,
    fromLatLng,
    toLatLng,
    polylinesArray) {

    var midLat =
        (fromLatLng[0] + toLatLng[0]) / 2;

    var midLng =
        (fromLatLng[1] + toLatLng[1]) / 2;

    var distance =
        Math.abs(toLatLng[1] - fromLatLng[1]);

    var curveOffset =
        Math.max(distance * 0.2, 1);

    var curvedMidPoint = [
        midLat + curveOffset,
        midLng
    ];

    var polyline = L.polyline(
        [
            fromLatLng,
            curvedMidPoint,
            toLatLng
        ],
        {
            color: "red",
            weight: 3,
            opacity: 0.8,
            dashArray: "10,10"
        }
    ).addTo(map);

    polylinesArray.push(polyline);
}

function drawStraightLine(
    map,
    fromLatLng,
    toLatLng,
    transport,
    polylinesArray) {

    var color = getTransportColor(transport);

    var dashArray = null;

    if (transport === window.travelMap.TRANSPORT.WALKING) {
        dashArray = "10,10";
    }

    if (transport === window.travelMap.TRANSPORT.CYCLE) {
        dashArray = "5,10";
    }

    var polyline = L.polyline(
        [
            fromLatLng,
            toLatLng
        ],
        {
            color: color,
            weight: 5,
            opacity: 0.8,
            dashArray: dashArray
        }
    ).addTo(map);

    polylinesArray.push(polyline);
}

function transportIcon(type) {

    var icon = "🚗";

    switch (type) {

        case window.travelMap.TRANSPORT.CAR:
            icon = "🚗";
            break;

        case window.travelMap.TRANSPORT.PLANE:
            icon = "✈️";
            break;

        case window.travelMap.TRANSPORT.BOAT:
            icon = "⛵";
            break;

        case window.travelMap.TRANSPORT.WALKING:
            icon = "🚶";
            break;

        case window.travelMap.TRANSPORT.CYCLE:
            icon = "🚴";
            break;
    }

    return L.divIcon({
        html: icon,
        className: "transport-icon",
        iconSize: [30, 30]
    });
}
