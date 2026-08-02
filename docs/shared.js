(function () {
        var params = new URLSearchParams(window.location.search);
        if (params.get("r") !== "1") {
            var locale = (navigator.language || "en").toLowerCase();
            var langTag = locale.split("-")[0];
            var redirect = null;
            if (locale.indexOf("pt-br") === 0) {
                redirect = "index-pt-br.html";
            } else if (langTag === "fr") {
                redirect = "index-fr.html";
            } else if (langTag === "es") {
                redirect = "index-es.html";
            } else if (langTag === "de") {
                redirect = "index-de.html";
            } else if (langTag === "pt") {
                redirect = "index-pt.html";
            } else if (langTag === "ar") {
                redirect = "index-ar.html";
            }
            if (redirect && window.location.pathname.indexOf(redirect) === -1) {
                window.location.href = redirect + "?r=1";
            }
        }
        var reduced = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
        var reveals = document.querySelectorAll(".reveal");

        if (reduced || !("IntersectionObserver" in window)) {
            for (var i = 0; i < reveals.length; i++) {
                reveals[i].classList.add("visible");
            }
        } else {
            var observer = new IntersectionObserver(function (entries) {
                for (var i = 0; i < entries.length; i++) {
                    if (entries[i].isIntersecting) {
                        entries[i].target.classList.add("visible");
                        observer.unobserve(entries[i].target);
                    }
                }
            }, { threshold: 0.12 });
            for (var j = 0; j < reveals.length; j++) {
                observer.observe(reveals[j]);
            }
        }

        var langSelect = document.getElementById("lang-select");
        if (langSelect) {
            langSelect.addEventListener("change", function () {
                window.location.href = langSelect.value + "?r=1";
            });
        }

        var lite = document.querySelector(".video-lite");
        if (lite) {
            function play() {
                var frame = document.createElement("iframe");
                frame.src = "https://www.youtube.com/embed/2V63N7nyiWE?autoplay=1&rel=0";
                frame.title = "BlinkTalk demonstration video";
                frame.allow = "accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture";
                frame.setAttribute("allowfullscreen", "");
                lite.innerHTML = "";
                lite.appendChild(frame);
            }
            lite.addEventListener("click", play);
            lite.addEventListener("keydown", function (event) {
                if (event.key === "Enter" || event.key === " ") {
                    event.preventDefault();
                    play();
                }
            });
        }
    })();
