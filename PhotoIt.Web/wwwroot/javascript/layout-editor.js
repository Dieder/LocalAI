window.layoutEditor = {
    getElementSizeAtPoint: function (x, y) {
        const element = document.elementFromPoint(x, y);
        const rect = element.getBoundingClientRect();
        return { width: rect.width, height: rect.height };
    }
};
