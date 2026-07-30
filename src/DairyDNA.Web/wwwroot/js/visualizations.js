(function () {
    const charts = new Map();
    const maps = new Map();
    let googleMapsPromise;

    function showFallback(element, message) {
        element.replaceChildren();
        const fallback = document.createElement("div");
        fallback.className = "dd-visual-fallback";
        fallback.setAttribute("role", "status");
        fallback.textContent = message;
        element.appendChild(fallback);
    }

    window.dairyDnaCharts = {
        render(canvasId, config) {
            const canvas = document.getElementById(canvasId);
            if (!canvas) return;

            charts.get(canvasId)?.destroy();
            charts.delete(canvasId);

            const oldFallback = canvas.parentElement?.querySelector(".dd-chart-fallback");
            oldFallback?.remove();

            if (!window.Chart) {
                const fallback = document.createElement("p");
                fallback.className = "dd-visual-fallback dd-chart-fallback";
                fallback.textContent = "The chart library could not be loaded.";
                canvas.hidden = true;
                canvas.after(fallback);
                return;
            }

            canvas.hidden = false;
            config.options = config.options || {};
            config.options.responsive = true;
            config.options.maintainAspectRatio = false;
            charts.set(canvasId, new Chart(canvas.getContext("2d"), config));
        },

        destroy(canvasId) {
            charts.get(canvasId)?.destroy();
            charts.delete(canvasId);
        }
    };

    function loadGoogleMaps(apiKey) {
        if (window.google?.maps) return Promise.resolve(window.google.maps);
        if (googleMapsPromise) return googleMapsPromise;

        googleMapsPromise = new Promise((resolve, reject) => {
            const callbackName = `dairyDnaGoogleMapsReady_${Date.now()}`;
            const script = document.createElement("script");
            const timeout = window.setTimeout(
                () => reject(new Error("Google Maps took too long to load.")),
                15000);

            window[callbackName] = () => {
                window.clearTimeout(timeout);
                delete window[callbackName];
                resolve(window.google.maps);
            };

            script.async = true;
            script.onerror = () => {
                window.clearTimeout(timeout);
                delete window[callbackName];
                googleMapsPromise = undefined;
                reject(new Error("Google Maps could not be loaded."));
            };
            script.src = `https://maps.googleapis.com/maps/api/js?key=${encodeURIComponent(apiKey)}&v=weekly&loading=async&callback=${callbackName}`;
            document.head.appendChild(script);
        });

        return googleMapsPromise;
    }

    const markerColors = {
        farm: "#7a5c29",
        facility: "#1f4e79",
        customer: "#2a9d8f"
    };

    function markerLabel(name) {
        const match = /\(([A-Z]{2})\)\s*$/.exec(name);
        return match ? match[1] : "";
    }

    window.dairyDnaMaps = {
        async render(elementId, apiKey, points, flows) {
            const element = document.getElementById(elementId);
            if (!element) return;

            const previous = maps.get(elementId);
            previous?.overlays.forEach(overlay => overlay.setMap(null));
            maps.delete(elementId);

            if (!apiKey) {
                showFallback(
                    element,
                    "Google Maps is not configured. Set GoogleMaps:ApiKey to display this map.");
                return;
            }

            try {
                const googleMaps = await loadGoogleMaps(apiKey);
                element.replaceChildren();
                const map = new googleMaps.Map(element, {
                    center: { lat: 39.5, lng: -98.35 },
                    zoom: 4,
                    mapTypeControl: true,
                    streetViewControl: false,
                    fullscreenControl: true,
                    gestureHandling: "cooperative"
                });
                const bounds = new googleMaps.LatLngBounds();
                const overlays = [];
                const pointsById = new Map();
                const infoWindow = new googleMaps.InfoWindow();

                (points || []).forEach(point => {
                    const position = {
                        lat: Number(point.latitude),
                        lng: Number(point.longitude)
                    };
                    if (!Number.isFinite(position.lat) || !Number.isFinite(position.lng)) return;

                    pointsById.set(String(point.id).toLowerCase(), position);
                    bounds.extend(position);
                    const kind = String(point.kind || "customer").toLowerCase();
                    const marker = new googleMaps.Marker({
                        map,
                        position,
                        title: `${point.kind}: ${point.name}`,
                        label: {
                            text: markerLabel(point.name || ""),
                            color: "#ffffff",
                            fontSize: "10px",
                            fontWeight: "600"
                        },
                        icon: {
                            path: googleMaps.SymbolPath.CIRCLE,
                            fillColor: markerColors[kind] || markerColors.customer,
                            fillOpacity: 1,
                            strokeColor: "#ffffff",
                            strokeWeight: 1.5,
                            scale: 8
                        }
                    });
                    marker.addListener("click", () => {
                        const content = document.createElement("div");
                        const title = document.createElement("strong");
                        title.textContent = point.name;
                        const type = document.createElement("div");
                        type.textContent = point.kind;
                        content.append(title, type);
                        infoWindow.setContent(content);
                        infoWindow.open({ map, anchor: marker });
                    });
                    overlays.push(marker);
                });

                (flows || []).forEach(flow => {
                    const origin = pointsById.get(String(flow.originId).toLowerCase());
                    const destination = pointsById.get(String(flow.destinationId).toLowerCase());
                    if (!origin || !destination) return;

                    const line = new googleMaps.Polyline({
                        map,
                        path: [origin, destination],
                        geodesic: true,
                        strokeColor: "#e76f51",
                        strokeOpacity: 0.85,
                        strokeWeight: 3,
                        icons: [{
                            icon: {
                                path: googleMaps.SymbolPath.FORWARD_CLOSED_ARROW,
                                scale: 3,
                                strokeColor: "#e76f51"
                            },
                            offset: "100%"
                        }]
                    });
                    overlays.push(line);
                });

                if (!bounds.isEmpty()) {
                    map.fitBounds(bounds, 36);
                    googleMaps.event.addListenerOnce(map, "idle", () => {
                        if ((map.getZoom() ?? 4) > 7) map.setZoom(7);
                    });
                }

                maps.set(elementId, { map, overlays });
            } catch (error) {
                showFallback(element, error?.message || "Google Maps could not be displayed.");
            }
        },

        destroy(elementId) {
            const current = maps.get(elementId);
            current?.overlays.forEach(overlay => overlay.setMap(null));
            maps.delete(elementId);
        }
    };
})();
