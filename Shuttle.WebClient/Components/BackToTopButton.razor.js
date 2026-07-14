function findScrollParent(el) {
    let node = el?.parentElement;
    while (node && node !== document.body && node !== document.documentElement) {
        const overflowY = getComputedStyle(node).overflowY;
        if (overflowY === 'auto' || overflowY === 'scroll' || overflowY === 'overlay') {
            return node;
        }
        node = node.parentElement;
    }
    return document.scrollingElement || document.documentElement;
}

export function initialize(container, threshold) {
    const scroller = findScrollParent(container);
    const documentScroller = document.scrollingElement || document.documentElement;
    const eventTarget = scroller === documentScroller ? window : scroller;

    const update = () => {
        container.classList.toggle('back-to-top--visible', scroller.scrollTop > threshold);
    };

    eventTarget.addEventListener('scroll', update, { passive: true });
    window.addEventListener('resize', update, { passive: true });
    update();

    return {
        scrollToTop: () => scroller.scrollTo({ top: 0, behavior: 'smooth' }),
        dispose: () => {
            eventTarget.removeEventListener('scroll', update);
            window.removeEventListener('resize', update);
        }
    };
}
