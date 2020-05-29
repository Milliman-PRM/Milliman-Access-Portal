
var acc = document.getElementsByClassName("accordion");
var icon = document.getElementsByClassName("icon");
var content = document.getElementsByClassName("content");

for (var i = 0; i < acc.length; i++) {
    acc[i].onclick = function() {
        var setClasses = !this.classList.contains('active');
        setClass(acc, 'active', 'remove');
        setClass(icon, 'rotate', 'remove')
        setClass(content, 'show', 'remove');

        if (setClasses) {
            this.classList.toggle("active");
            this.lastElementChild.classList.toggle("rotate")
            this.nextElementSibling.classList.toggle("show");
        }
    }
}

function setClass(els, className, fnName) {
    for (var i = 0; i < els.length; i++) {
        els[i].classList[fnName](className);
    }
}
