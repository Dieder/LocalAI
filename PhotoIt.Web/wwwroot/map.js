window.createRouteMap = (elementId, start, end) => {

    const map = new google.maps.Map(
        document.getElementById(elementId),
        {
            zoom: 7,
            center: start
        });

    const directionsService =
        new google.maps.DirectionsService();

    const directionsRenderer =
        new google.maps.DirectionsRenderer();

    directionsRenderer.setMap(map);

    directionsService.route({
        origin: start,
        destination: end,
        travelMode: 'DRIVING'
    }, (result, status) => {

        if (status === 'OK') {
            directionsRenderer.setDirections(result);
        }
    });
};
