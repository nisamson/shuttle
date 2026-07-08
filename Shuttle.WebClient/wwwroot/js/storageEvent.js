function registerStorageListenerEvent(listener) {
    window.addEventListener('storage', async (event) => {
        if (event.key === 'shuttle-options') {
            await listener.invokeMethodAsync('OnOptionsUpdatedOutsideTab');
        }
    });
}