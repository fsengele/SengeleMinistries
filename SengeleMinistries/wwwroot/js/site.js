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

// Gallery lightbox functionality
(function () {
    const grid = document.getElementById('masonryGrid');
    if (!grid) return;

    const imgs = Array.from(grid.querySelectorAll('.gallery-image'));
    const lightbox = document.getElementById('lightbox');
    const lbImage = document.getElementById('lbImage');
    const btnClose = document.querySelector('.lb-close');
    const btnPrev = document.querySelector('.lb-prev');
    const btnNext = document.querySelector('.lb-next');
    let current = 0;

    function openAt(index) {
        current = index;
        lbImage.src = imgs[current].src;
        lightbox.setAttribute('aria-hidden', 'false');
        document.body.style.overflow = 'hidden';
    }

    function close() {
        lightbox.setAttribute('aria-hidden', 'true');
        lbImage.src = '';
        document.body.style.overflow = '';
    }

    function prev() {
        current = (current - 1 + imgs.length) % imgs.length;
        lbImage.src = imgs[current].src;
    }

    function next() {
        current = (current + 1) % imgs.length;
        lbImage.src = imgs[current].src;
    }

    imgs.forEach((img, idx) => {
        img.addEventListener('click', () => openAt(idx));
    });

    btnClose?.addEventListener('click', close);
    btnPrev?.addEventListener('click', prev);
    btnNext?.addEventListener('click', next);

    document.addEventListener('keydown', (e) => {
        if (lightbox.getAttribute('aria-hidden') === 'false') {
            if (e.key === 'Escape') close();
            if (e.key === 'ArrowLeft') prev();
            if (e.key === 'ArrowRight') next();
        }
    });
})();
