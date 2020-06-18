window.addEventListener('DOMContentLoaded', () => {
    const anchors = document.getElementsByTagName('a');
    for (let i = 0; i < anchors.length; i++){
        anchors[i].setAttribute('target', '_blank');
    }
});
