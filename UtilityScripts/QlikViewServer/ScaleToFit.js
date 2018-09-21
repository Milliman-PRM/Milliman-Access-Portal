let boundaryElementClassName;
const possibleBoundaryElements = [
    "Document_Boundary",
    "Document_imgQlikViewLogo"
]

const observer = new MutationObserver(function(mutations) {
    let boundaryElement;
    possibleBoundaryElements.forEach(function(element) {
        if (!boundaryElementClassName 
            && document.getElementsByClassName(element)[0] 
            && document.getElementsByClassName(element)[0].offsetLeft) {
            boundaryElementClassName = element;
            boundaryElement = document.getElementsByClassName(boundaryElementClassName)[0];
        }
    });
    if (boundaryElement && boundaryElement.offsetLeft && boundaryElement.offsetWidth) {
        scaleDocument();
        observer.disconnect();
    }
});

observer.observe(document.body, {
    childList: true,
    subtree: false,
    attributes: false,
    characterData: false
});

setTimeout(function() {
    if (observer && !boundaryElementClassName) {
        observer.disconnect();
        console.log("Scaling failed");
    }
}, 10000);

function scaleDocument() {
    const logo = document.getElementsByClassName(boundaryElementClassName)[0];
    if (logo) {
        const contentWrapper = document.getElementById('PageContainer');
        const doc = document.getElementsByTagName('html')[0];
        const body = document.getElementsByTagName('body')[0];
        const contentWidth = logo.offsetWidth + logo.offsetLeft;
        const windowWidth = window.innerWidth - 25;
        const scaleFactor = windowWidth / contentWidth;
        
        doc.style.overflowX = "hidden";
        doc.style.overflowY = "auto";
        body.style.minHeight = "100vh";
        body.style.maxWidth = "100vw";
        contentWrapper.style.transform = "scale(" + scaleFactor.toString() + ")";
        contentWrapper.style.transformOrigin = "top left";
        console.log('Document scaled.');
    }
}
