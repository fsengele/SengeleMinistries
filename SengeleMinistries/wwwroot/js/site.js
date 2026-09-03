// Site interactions: header rotation + navbar scroll behavior
const SengeleSite = (() => {
    const images = [
        '/images/TUEQ6134.JPG',
        '/images/IMG_7497.JPG',
        '/images/AZWM6413.JPG'
    ];

    let i = 0;

    function changeHero() {
        const hero = document.querySelector('.hero');
        if (!hero) return;
        hero.style.backgroundImage = "url('" + images[i] + "')";
        i = (i + 1) % images.length;
    }

    function handleNavbarScroll() {
        const nav = document.getElementById('mainNavbar');
        if (!nav) return;
        if (window.scrollY > 60) nav.classList.add('scrolled'); else nav.classList.remove('scrolled');
    }

    function init() {
        // hero rotation
        if (document.querySelector('.hero')) {
            changeHero();
            const prefersReduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
            if (!prefersReduced) setInterval(changeHero, 4500);
        }

        // navbar scroll
        handleNavbarScroll();
        window.addEventListener('scroll', handleNavbarScroll, { passive: true });
    }

    // auto-init on DOM ready
    if (document.readyState === 'complete' || document.readyState === 'interactive') {
        setTimeout(init, 0);
    } else {
        window.addEventListener('DOMContentLoaded', init);
    }

    return { init };
})();
