// NOVA Workspace — site behaviour

// Download buttons: give instant feedback (the /download route streams platform zips).
const status = document.getElementById('downloadStatus');
const downloadLinks = document.querySelectorAll('a[href^="/download"]');

if (status && downloadLinks.length) {
  downloadLinks.forEach((btn) => {
    btn.addEventListener('click', () => {
      const platform = btn.getAttribute('data-platform') || 'win-x64';
      const label = (btn.textContent || '').replace(/\s+/g, ' ').trim();
      status.textContent = `Starting download: ${label}. If this platform package is not yet published, the download page will explain the current status.`;
      btn.setAttribute('aria-busy', 'true');
    });
  });
}

// Reveal animations on scroll (progressive enhancement).
if ('IntersectionObserver' in window) {
  const targets = document.querySelectorAll(
    '.editorial-row, .stat, .stepper li, .app-card, .desk-strip, .versus-table-wrap, ' +
    '.price-card, .how, .price-strip, .download-card, .faq-item'
  );
  targets.forEach((el) => el.classList.add('reveal'));

  // Stagger: siblings inside one block fade in one after another, so a group
  // reads as one movement instead of a pile. The delay is written up front,
  // the way the reference design bakes it into the markup, so the order is
  // fixed and never depends on which observer fires first.
  const groups = document.querySelectorAll('.editorial, .price-cards, .stat-grid, .stepper');
  groups.forEach((group) => {
    Array.from(group.children).forEach((kid, i) => {
      kid.style.transitionDelay = `${Math.min(i * 70, 420)}ms`;
    });
  });

  const io = new IntersectionObserver(
    (entries) => {
      for (const entry of entries) {
        if (entry.isIntersecting) {
          entry.target.classList.add('is-in');
          io.unobserve(entry.target);
        }
      }
    },
    { threshold: 0.12, rootMargin: '0px 0px -40px 0px' }
  );
  targets.forEach((el) => io.observe(el));
}
