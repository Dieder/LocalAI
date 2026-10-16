const photoFitObservers = new WeakMap();

window.layoutEditor = {
    getElementSizeAtPoint: function (x, y) {
        const element = document.elementFromPoint(x, y);
        const rect = element.getBoundingClientRect();
        return { width: rect.width, height: rect.height };
    },
    initializePhotoFit: function (root) {
        if (!root || photoFitObservers.has(root))
            return;

        const observedImages = new Map();
        const fitImage = image => {
            const slot = image.closest(".slot-photo");
            if (!slot || image.naturalWidth <= 0 || image.naturalHeight <= 0)
                return;

            const slotRect = slot.getBoundingClientRect();
            if (slotRect.width <= 0 || slotRect.height <= 0)
                return;

            const photoWidth = image.naturalWidth;
            const photoHeight = image.naturalHeight;
            const coverScale = Math.max(
                slotRect.width / photoWidth,
                slotRect.height / photoHeight);
            const displayWidth = photoWidth * coverScale;
            const displayHeight = photoHeight * coverScale;
            const cropX = displayWidth - slotRect.width;
            const cropY = displayHeight - slotRect.height;
            const containScale = Math.min(
                slotRect.width / photoWidth,
                slotRect.height / photoHeight);
            const coverFitZoom = coverScale / containScale;
            const userZoom = Math.max(1, Number.parseFloat(image.dataset.userZoom) || 1);
            const renderedZoom = coverFitZoom * userZoom;
            const parsedPositionX = Number.parseFloat(image.dataset.positionX);
            const parsedPositionY = Number.parseFloat(image.dataset.positionY);
            const positionX = Number.isFinite(parsedPositionX) ? parsedPositionX : 50;
            const positionY = Number.isFinite(parsedPositionY) ? parsedPositionY : 50;
            const offsetX = (50 - positionX) * (renderedZoom - 1);
            const offsetY = (50 - positionY) * (renderedZoom - 1);

            image.style.transform = `translate(${offsetX}%, ${offsetY}%) scale(${renderedZoom})`;
            image.dataset.cropX = String(cropX);
            image.dataset.cropY = String(cropY);
        };

        const observeImage = image => {
            if (observedImages.has(image))
                return;

            const onLoad = () => fitImage(image);
            observedImages.set(image, onLoad);
            image.addEventListener("load", onLoad);
            resizeObserver.observe(image.closest(".slot-photo"));
            fitImage(image);
        };

        const scan = node => {
            if (node instanceof Element && node.matches("img.photo-img"))
                observeImage(node);
            node.querySelectorAll?.("img.photo-img").forEach(observeImage);
        };

        const resizeObserver = new ResizeObserver(entries => {
            for (const entry of entries)
                entry.target.querySelectorAll("img.photo-img").forEach(fitImage);
        });

        const mutationObserver = new MutationObserver(records => {
            for (const record of records) {
                if (record.type === "attributes")
                    fitImage(record.target);
                else
                    record.addedNodes.forEach(scan);
            }
        });

        scan(root);
        mutationObserver.observe(root, {
            subtree: true,
            childList: true,
            attributes: true,
            attributeFilter: ["data-user-zoom", "data-position-x", "data-position-y"]
        });

        photoFitObservers.set(root, { observedImages, mutationObserver, resizeObserver });
    },
    disposePhotoFit: function (root) {
        const state = root && photoFitObservers.get(root);
        if (!state)
            return;

        state.mutationObserver.disconnect();
        state.resizeObserver.disconnect();
        for (const [image, onLoad] of state.observedImages)
            image.removeEventListener("load", onLoad);
        photoFitObservers.delete(root);
    }
};
