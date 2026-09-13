window.transportIcon = transportIcon;
window.travelMap = {

    initialize: function (routeLegs) {

        const map = L.map('travelMap');

        map.setView([-30, 24], 6);

        L.tileLayer(
            'https://tile.openstreetmap.org/{z}/{x}/{y}.png',
            {
                attribution: '&copy; OpenStreetMap'
            }
        ).addTo(map);

        var bounds = [];

        routeLegs.forEach(leg => {

            const from = [
                leg.from.latitude,
                leg.from.longitude
            ];

            const to = [
                leg.to.latitude,
                leg.to.longitude
            ];

            bounds.push(from);
            bounds.push(to);

            var color = "blue";
            var dashArray = null;

            switch (leg.transport) {

                case 0: // Car
                    color = "green";
                    break;

                case 1: // Plane
                    color = "red";
                    dashArray = "10 10";
                    break;

                case 2: // Boat
                    color = "navy";
                    dashArray = "5 10";
                    break;

                case 3: // Walking
                    color = "orange";
                    break;

                case 4: // Bicycle
                    color = "purple";
                    break;
            }

            L.marker(from)
                .addTo(map)
                .bindPopup(leg.from.place);

            L.marker(to)
                .addTo(map)
                .bindPopup(leg.to.place);

            L.polyline(
                [from, to],
                {
                    color: color,
                    weight: 5,
                    dashArray: dashArray
                })
                .addTo(map);
        });

        map.fitBounds(bounds);
    }
}
function transportIcon(type) {

    let icon = "🚗";

    switch (type) {

        case 0:
            icon = "🚗";
            break;

        case 1:
            icon = "✈️";
            break;

        case 2:
            icon = "⛵";
            break;

        case 3:
            icon = "🚶";
            break;

        case 4:
            icon = "🚴";
            break;
    }

    return L.divIcon({
        html: icon,
        className: "",
        iconSize: [30, 30]
    });
}