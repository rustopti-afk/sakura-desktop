'use strict';

// ── Token capture (after Steam redirect) ─────────────────────
(function captureToken() {
  const p = new URLSearchParams(location.search);
  const token = p.get('token');
  if (!token) return;
  localStorage.setItem('volia_steam_token', token);
  // Decode JWT payload (no verification — display only)
  try {
    const payload = JSON.parse(atob(token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/')));
    localStorage.setItem('volia_user', JSON.stringify({
      steamId:  payload.steamId,
      nickname: payload.name,
      avatar:   payload.avatar || '',
      loginAt:  Date.now(),
    }));
  } catch { /* malformed token — ignore */ }
  // Strip ?token= from URL without reload
  p.delete('token');
  const clean = location.pathname + (p.toString() ? '?' + p.toString() : '');
  history.replaceState(null, '', clean);
})();

// ── Auth state → navbar ───────────────────────────────────────
function checkAuth() {
  const navAuth = document.getElementById('navAuth');
  if (!navAuth) return;
  try {
    const token = localStorage.getItem('volia_steam_token');
    const user  = JSON.parse(localStorage.getItem('volia_user') || 'null');
    if (!token || !user?.steamId) return;
    // Validate expiry
    const p = JSON.parse(atob(token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/')));
    if (p.exp && p.exp < Math.floor(Date.now() / 1000)) {
      localStorage.removeItem('volia_steam_token');
      localStorage.removeItem('volia_user');
      return;
    }
    const avatar = user.avatar
      ? `<img src="${escHtml(user.avatar)}" alt="" style="width:28px;height:28px;border-radius:50%;border:2px solid var(--purple);object-fit:cover">`
      : `<i class="fab fa-steam" style="font-size:1.1rem"></i>`;
    navAuth.innerHTML = `
      <a href="/pages/profile.html" class="nav-user-btn" style="display:flex;align-items:center;gap:8px;text-decoration:none;color:var(--text);font-size:0.85rem;font-weight:500">
        ${avatar}
        <span style="max-width:120px;overflow:hidden;text-overflow:ellipsis;white-space:nowrap">${escHtml(user.nickname || user.steamId)}</span>
      </a>`;
  } catch { /* ignore */ }
}

function escHtml(s) {
  return String(s || '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
}

// ── Server stats via RCON (replaces BattleMetrics) ───────────
async function fetchServerStats() {
  const set = (id, val) => { const el = document.getElementById(id); if (el) el.textContent = val; };

  try {
    const r = await fetch('/api/server-status');
    if (!r.ok) throw new Error(r.status);
    const d = await r.json();

    const main   = d.servers?.main   || { online: false, players: 0, maxPlayers: 100 };
    const monday = d.servers?.monday || { online: false, players: 0, maxPlayers: 100 };

    // Server 1 (Main)
    updateServerCard(1, main);
    // Server 2 (Monday)
    updateServerCard(2, monday);

    // Totals
    const totalEl = document.getElementById('totalOnline');
    if (totalEl) {
      totalEl.innerHTML = `<strong>${d.totalPlayers}</strong> / 200 <span data-i18n="servers.slots">слотів</span>`;
    }

    // Stats strip counters
    const nums = document.querySelectorAll('.stat-num[data-target]');
    if (nums[0]) animateCounter(nums[0], d.totalPlayers);
    if (nums[1]) animateCounter(nums[1], d.activeServers);

    // Last updated
    const updEl = document.getElementById('lastUpdated');
    if (updEl) {
      const now = new Date();
      updEl.innerHTML = `<i class="fas fa-check-circle" style="color:#4ade80"></i> ${now.getHours().toString().padStart(2,'0')}:${now.getMinutes().toString().padStart(2,'0')}:${now.getSeconds().toString().padStart(2,'0')}`;
    }
  } catch {
    [1, 2].forEach(i => {
      const dot = document.getElementById(`status-${i}`);
      if (dot) { dot.textContent = '● Offline'; dot.style.color = '#ef4444'; }
      const pl = document.getElementById(`players-${i}`);
      if (pl) pl.textContent = '—';
    });
    const updEl = document.getElementById('lastUpdated');
    if (updEl) updEl.innerHTML = `<i class="fas fa-exclamation-circle" style="color:#ef4444"></i> Помилка`;
  }
}

function updateServerCard(idx, srv) {
  const dot = document.getElementById(`status-${idx}`);
  const pl  = document.getElementById(`players-${idx}`);
  const pct = document.getElementById(`pct-${idx}`);
  const bar = document.getElementById(`bar-${idx}`);

  if (dot) {
    dot.textContent = srv.online ? '● Online' : '● Offline';
    dot.style.color = srv.online ? '#4ade80' : '#ef4444';
  }
  if (pl)  pl.textContent = srv.players;
  const percent = srv.maxPlayers > 0 ? Math.round(srv.players / srv.maxPlayers * 100) : 0;
  if (pct) pct.textContent = percent + '%';
  if (bar) bar.style.width = percent + '%';
}

function animateCounter(el, target) {
  const start    = parseInt(el.textContent, 10) || 0;
  const duration = 600;
  const startTs  = performance.now();
  function step(ts) {
    const p = Math.min((ts - startTs) / duration, 1);
    el.textContent = Math.round(start + (target - start) * p);
    if (p < 1) requestAnimationFrame(step);
  }
  requestAnimationFrame(step);
}

// ── Wipe countdown timers ─────────────────────────────────────
function updateTimers() {
  document.querySelectorAll('.wipe-timer[data-wipe]').forEach(el => {
    const target = new Date(el.dataset.wipe).getTime();
    const diff   = target - Date.now();
    if (diff <= 0) { el.textContent = 'Вайп!'; return; }
    const d = Math.floor(diff / 86400000);
    const h = Math.floor((diff % 86400000) / 3600000);
    const m = Math.floor((diff % 3600000) / 60000);
    const s = Math.floor((diff % 60000) / 1000);
    el.textContent = d > 0
      ? `${d}д ${h.toString().padStart(2,'0')}:${m.toString().padStart(2,'0')}:${s.toString().padStart(2,'0')}`
      : `${h.toString().padStart(2,'0')}:${m.toString().padStart(2,'0')}:${s.toString().padStart(2,'0')}`;
  });
}

// ── Copy IP to clipboard ──────────────────────────────────────
function copyIP(id) {
  const el = document.getElementById(id);
  if (!el) return;
  navigator.clipboard.writeText(el.textContent.trim()).then(() => {
    const toast = document.getElementById('toast');
    if (!toast) return;
    toast.textContent = 'IP скопійовано!';
    toast.classList.add('show');
    setTimeout(() => toast.classList.remove('show'), 2000);
  }).catch(() => {
    // fallback for older browsers
    const ta = document.createElement('textarea');
    ta.value = el.textContent.trim();
    ta.style.position = 'fixed'; ta.style.opacity = '0';
    document.body.appendChild(ta);
    ta.select(); document.execCommand('copy');
    ta.remove();
  });
}

// ── Scroll animations ─────────────────────────────────────────
function initScrollAnimations() {
  if (!('IntersectionObserver' in window)) return;
  const obs = new IntersectionObserver((entries) => {
    entries.forEach(e => { if (e.isIntersecting) { e.target.classList.add('visible'); obs.unobserve(e.target); } });
  }, { threshold: 0.1 });
  document.querySelectorAll('.anim-fade-up, .anim-scale-in').forEach(el => obs.observe(el));
}

// ── Site settings (accent color, hero, etc.) ─────────────────
function applySiteSettings() {
  try {
    const s = JSON.parse(localStorage.getItem('volia_site_settings') || 'null');
    if (!s) return;
    if (s.accentColor) {
      document.documentElement.style.setProperty('--purple', s.accentColor);
      // derive hover shade (slightly darker)
      document.documentElement.style.setProperty('--purple-h', s.accentColor);
    }
    if (s.siteName) {
      document.querySelectorAll('.nav-logo-name').forEach(el => { el.innerHTML = s.siteName; });
    }
  } catch { /* ignore */ }
}

// ── Announcements banners ─────────────────────────────────────
function showAnnouncements() {
  try {
    const announces = JSON.parse(localStorage.getItem('volia_announces') || '[]');
    const now = Date.now();
    const active = announces.filter(a =>
      a.active && (!a.expires || new Date(a.expires).getTime() > now)
    );
    if (!active.length) return;
    const colors = { info: '#60a5fa', success: '#4ade80', warning: '#fbbf24', error: '#ef4444' };
    const container = document.createElement('div');
    container.style.cssText = 'position:fixed;top:70px;right:16px;z-index:900;display:flex;flex-direction:column;gap:8px;max-width:360px';
    active.forEach(a => {
      const el = document.createElement('div');
      el.style.cssText = `background:var(--bg-card);border:1px solid ${colors[a.type]||colors.info};border-radius:10px;padding:12px 16px;font-size:0.85rem;display:flex;gap:10px;align-items:flex-start;animation:fadeInRight .3s ease`;
      el.innerHTML = `<span style="color:${colors[a.type]||colors.info};margin-top:1px"><i class="fas fa-info-circle"></i></span><div><strong>${escHtml(a.title)}</strong><br><span style="color:var(--text-sub)">${escHtml(a.body||'')}</span></div><button onclick="this.parentElement.remove()" style="background:none;border:none;color:var(--text-sub);cursor:pointer;margin-left:auto;padding:0;line-height:1;font-size:1rem">×</button>`;
      container.appendChild(el);
    });
    document.body.appendChild(container);
  } catch { /* ignore */ }
}

// ── Navbar burger ─────────────────────────────────────────────
(function initBurger() {
  const burger   = document.getElementById('burger');
  const navLinks = document.getElementById('navLinks');
  if (!burger || !navLinks) return;
  burger.addEventListener('click', () => {
    const open = navLinks.classList.toggle('open');
    burger.classList.toggle('active', open);
    burger.setAttribute('aria-expanded', open);
  });
  document.addEventListener('click', e => {
    if (!burger.contains(e.target) && !navLinks.contains(e.target)) {
      navLinks.classList.remove('open');
      burger.classList.remove('active');
    }
  });
})();

// ── Navbar scroll shadow ──────────────────────────────────────
(function initNavScroll() {
  const nav = document.getElementById('navbar');
  if (!nav) return;
  window.addEventListener('scroll', () => {
    nav.classList.toggle('scrolled', window.scrollY > 20);
  }, { passive: true });
})();

// ── Language switcher ─────────────────────────────────────────
function toggleLangMenu() {
  const menu  = document.getElementById('langMenu');
  const arrow = document.getElementById('langArrow');
  if (!menu) return;
  const open = menu.classList.toggle('open');
  if (arrow) arrow.style.transform = open ? 'rotate(180deg)' : '';
}

function selectLang(lang) {
  localStorage.setItem('volia_lang', lang);
  const flag = document.getElementById('langFlag');
  if (flag) flag.textContent = lang.toUpperCase();
  // close menu
  const menu  = document.getElementById('langMenu');
  const arrow = document.getElementById('langArrow');
  if (menu)  menu.classList.remove('open');
  if (arrow) arrow.style.transform = '';
  // update active option
  document.querySelectorAll('.lang-option').forEach(b => {
    b.classList.toggle('active', b.dataset.lang === lang);
  });
  // trigger i18n re-render if available
  if (typeof window.I18n?.setLang === 'function') window.I18n.setLang(lang);
  else location.reload();
}

// close lang menu on outside click
document.addEventListener('click', e => {
  const dd = document.getElementById('langDropdown');
  if (dd && !dd.contains(e.target)) {
    const menu = document.getElementById('langMenu');
    const arrow = document.getElementById('langArrow');
    if (menu)  menu.classList.remove('open');
    if (arrow) arrow.style.transform = '';
  }
});

// ── Init ──────────────────────────────────────────────────────
document.addEventListener('DOMContentLoaded', () => {
  applySiteSettings();
  checkAuth();
  showAnnouncements();
  initScrollAnimations();
  fetchServerStats();
  setInterval(fetchServerStats, 60000);
  setInterval(updateTimers, 1000);
  updateTimers();

  // Restore saved language flag
  const savedLang = localStorage.getItem('volia_lang') || 'uk';
  const flag = document.getElementById('langFlag');
  if (flag) flag.textContent = savedLang.toUpperCase();
  document.querySelectorAll('.lang-option').forEach(b => {
    b.classList.toggle('active', b.dataset.lang === savedLang);
  });
});
