// ═══════════════════════════════════════════════════════════════
//  Volia Content System — js/content.js
//  Reads volia_content from localStorage and patches data-editable
//  elements on every page. Enables inline edit mode when admin sets
//  the volia_edit_mode flag.
//
//  Usage in HTML:
//    <h1 data-editable="hero.title">Default text</h1>
//    <p  data-editable="hero.sub" data-html>Default <b>html</b></p>
//
//  The data-html attribute allows innerHTML replacement (rich text).
//  Without it only textContent is used (safer, prevents XSS).
// ═══════════════════════════════════════════════════════════════

(function VoliaContentSystem() {
  'use strict';

  // ── Storage adapter (swap this block for PHP migration) ──────
  const Storage = {
    get(key) {
      try { return JSON.parse(localStorage.getItem(key) || 'null'); }
      catch { return null; }
    },
    set(key, value) {
      localStorage.setItem(key, JSON.stringify(value));
    },
  };

  const CONTENT_KEY = 'volia_content';
  const EDIT_FLAG   = 'volia_edit_mode';
  const MEDIA_KEY   = 'volia_media';

  // ── Read/write helpers ───────────────────────────────────────
  function getContent() { return Storage.get(CONTENT_KEY) || {}; }
  function saveContent(data) { Storage.set(CONTENT_KEY, data); }
  function isEditMode() { return localStorage.getItem(EDIT_FLAG) === '1'; }

  // ── Apply stored content to the DOM ─────────────────────────
  // Called on every page load. Replaces default text with saved values.
  function applyContent() {
    const data = getContent();
    document.querySelectorAll('[data-editable]').forEach(el => {
      const key = el.dataset.editable;
      if (!key || data[key] === undefined) return;
      if (el.hasAttribute('data-html')) {
        el.innerHTML = data[key];
      } else {
        el.textContent = data[key];
      }
    });

    // Apply saved background images to sections
    const media = Storage.get(MEDIA_KEY) || [];
    document.querySelectorAll('[data-bg-key]').forEach(el => {
      const key = el.dataset.bgKey;
      const item = media.find(m => m.key === key);
      if (item) el.style.backgroundImage = `url(${item.base64})`;
    });
  }

  // ── Edit mode toolbar ────────────────────────────────────────
  function buildToolbar() {
    const bar = document.createElement('div');
    bar.id = 'vc-toolbar';
    bar.style.cssText = [
      'position:fixed', 'top:0', 'left:0', 'right:0', 'z-index:99999',
      'background:rgba(12,8,30,0.97)', 'border-bottom:2px solid #7C5CFC',
      'padding:8px 20px', 'display:flex', 'align-items:center', 'gap:14px',
      'font-family:Inter,sans-serif', 'font-size:0.82rem', 'color:#e2e8f0',
      'backdrop-filter:blur(12px)',
    ].join(';');

    bar.innerHTML = `
      <span style="color:#7C5CFC;font-weight:700;letter-spacing:.05em">✏️ РЕЖИМ РЕДАГУВАННЯ</span>
      <span style="color:#64748b">|</span>
      <span id="vc-status" style="color:#94a3b8">Клацни на будь-який текст щоб редагувати</span>
      <div style="margin-left:auto;display:flex;gap:8px">
        <button id="vc-save-btn"  onclick="window.__VC.saveAll()"  style="${btnStyle('#7C5CFC')}">💾 Зберегти все</button>
        <button id="vc-undo-btn"  onclick="window.__VC.undo()"     style="${btnStyle('#334155')}">↩ Скасувати</button>
        <button id="vc-exit-btn"  onclick="window.__VC.exitEdit()" style="${btnStyle('#ef4444')}">✕ Вийти</button>
      </div>
    `;
    document.body.prepend(bar);
    // Push page content down so toolbar doesn't cover it
    document.body.style.paddingTop = '44px';
  }

  function btnStyle(bg) {
    return [
      `background:${bg}`, 'border:none', 'color:#fff',
      'padding:5px 14px', 'border-radius:6px', 'cursor:pointer',
      'font-size:0.8rem', 'font-family:Inter,sans-serif', 'font-weight:500',
    ].join(';');
  }

  // ── Highlight editable elements ──────────────────────────────
  function highlightEditables() {
    document.querySelectorAll('[data-editable]').forEach(el => {
      el.setAttribute('contenteditable', 'plaintext-only');
      el.dataset.original = el.hasAttribute('data-html') ? el.innerHTML : el.textContent;
      el.style.outline        = '2px dashed rgba(124,92,252,0.45)';
      el.style.outlineOffset  = '3px';
      el.style.borderRadius   = '3px';
      el.style.cursor         = 'text';
      el.style.transition     = 'outline-color .15s';

      el.addEventListener('focus', () => {
        el.style.outlineColor = 'rgba(124,92,252,0.9)';
        el.style.background   = 'rgba(124,92,252,0.06)';
      });
      el.addEventListener('blur',  () => {
        el.style.outlineColor = 'rgba(124,92,252,0.45)';
        el.style.background   = '';
        autoSave(el);
      });
    });
  }

  // ── Auto-save on blur ────────────────────────────────────────
  let changeCount = 0;

  function autoSave(el) {
    const key = el.dataset.editable;
    const val = el.hasAttribute('data-html') ? el.innerHTML : el.textContent;
    if (val === el.dataset.original) return; // nothing changed
    const data = getContent();
    data[key] = val;
    saveContent(data);
    el.dataset.original = val;
    changeCount++;
    const status = document.getElementById('vc-status');
    if (status) status.innerHTML = `<span style="color:#4ade80">✓ Збережено ${changeCount} змін</span>`;
  }

  // ── Undo: restore all elements to their original values ──────
  function undoAll() {
    if (!confirm('Скасувати всі зміни на цій сторінці?')) return;
    document.querySelectorAll('[data-editable]').forEach(el => {
      const original = el.dataset.original;
      if (original !== undefined) {
        if (el.hasAttribute('data-html')) el.innerHTML = original;
        else el.textContent = original;
      }
    });
    const status = document.getElementById('vc-status');
    if (status) status.innerHTML = '<span style="color:#f59e0b">Зміни скасовано</span>';
  }

  // ── Manually save all (button) ───────────────────────────────
  function saveAll() {
    const data = getContent();
    document.querySelectorAll('[data-editable]').forEach(el => {
      const key = el.dataset.editable;
      if (!key) return;
      data[key] = el.hasAttribute('data-html') ? el.innerHTML : el.textContent;
    });
    saveContent(data);
    const status = document.getElementById('vc-status');
    if (status) status.innerHTML = '<span style="color:#4ade80">✓ Все збережено!</span>';
    setTimeout(() => {
      if (status) status.textContent = 'Клацни на будь-який текст щоб редагувати';
    }, 2000);
  }

  // ── Exit edit mode ───────────────────────────────────────────
  function exitEdit() {
    localStorage.removeItem(EDIT_FLAG);
    const bar = document.getElementById('vc-toolbar');
    if (bar) bar.remove();
    document.body.style.paddingTop = '';
    document.querySelectorAll('[data-editable]').forEach(el => {
      el.removeAttribute('contenteditable');
      el.style.outline = el.style.outlineOffset = el.style.cursor = el.style.background = '';
    });
  }

  // ── Public API (used by admin panel buttons) ─────────────────
  window.__VC = { saveAll, undo: undoAll, exitEdit };

  // ── Init ─────────────────────────────────────────────────────
  function init() {
    applyContent();
    if (isEditMode()) {
      buildToolbar();
      highlightEditables();
    }
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }

})();

// ═══════════════════════════════════════════════════════════════
//  Volia Media System — частина content.js
//  Надає API для завантаження зображень з resize через Canvas
//  та збереження в volia_media localStorage.
//
//  PHP migration: замінити MediaStorage.upload() на fetch('api.php')
// ═══════════════════════════════════════════════════════════════

window.VoliaMedia = (function () {
  'use strict';

  const MEDIA_KEY  = 'volia_media';
  const MAX_BYTES  = 900 * 1024; // 900KB — залишаємо буфер до 1MB
  const MAX_DIM    = 1920;       // максимальна ширина/висота після resize

  function getMedia() {
    try { return JSON.parse(localStorage.getItem(MEDIA_KEY) || '[]'); }
    catch { return []; }
  }
  function saveMedia(arr) { localStorage.setItem(MEDIA_KEY, JSON.stringify(arr)); }

  // ── Canvas resize ────────────────────────────────────────────
  // Повертає Promise<string> — base64 JPEG що не перевищує MAX_BYTES
  function resizeToBase64(file) {
    return new Promise((resolve, reject) => {
      const img = new Image();
      const url = URL.createObjectURL(file);

      img.onload = () => {
        URL.revokeObjectURL(url);

        let { width, height } = img;
        if (width > MAX_DIM || height > MAX_DIM) {
          const ratio = Math.min(MAX_DIM / width, MAX_DIM / height);
          width  = Math.round(width  * ratio);
          height = Math.round(height * ratio);
        }

        const canvas = document.createElement('canvas');
        canvas.width  = width;
        canvas.height = height;
        const ctx = canvas.getContext('2d');
        ctx.drawImage(img, 0, 0, width, height);

        // Поступово знижуємо якість поки файл не вліз у ліміт
        let quality = 0.92;
        let base64  = canvas.toDataURL('image/jpeg', quality);

        while (base64.length * 0.75 > MAX_BYTES && quality > 0.3) {
          quality -= 0.08;
          base64   = canvas.toDataURL('image/jpeg', quality);
        }

        if (base64.length * 0.75 > MAX_BYTES) {
          reject(new Error('Зображення занадто велике навіть після стиснення. Спробуй менший файл.'));
          return;
        }

        resolve(base64);
      };

      img.onerror = () => reject(new Error('Не вдалось прочитати зображення'));
      img.src = url;
    });
  }

  // ── Upload: resize → save → return media item ────────────────
  async function upload(file, key) {
    if (!file.type.startsWith('image/')) throw new Error('Тільки зображення (JPEG, PNG, WebP, GIF)');

    const base64 = await resizeToBase64(file);
    const item = {
      id:     Date.now(),
      key:    key || '',           // прив'язка до секції (напр. 'hero-bg', 'shop-banner')
      name:   file.name,
      base64,
      size:   Math.round(base64.length * 0.75), // приблизний розмір байт
      date:   new Date().toISOString().split('T')[0],
    };

    const media = getMedia();
    media.push(item);
    saveMedia(media);
    return item;
  }

  // ── Delete by id ─────────────────────────────────────────────
  function remove(id) {
    saveMedia(getMedia().filter(m => m.id !== id));
  }

  // ── Apply image to a section element ─────────────────────────
  // selector: CSS selector of the element (e.g. '.hero', '.section-banner')
  function applyToElement(selector, base64) {
    const el = document.querySelector(selector);
    if (!el) return;
    el.style.backgroundImage = `url(${base64})`;
    el.style.backgroundSize  = 'cover';
    el.style.backgroundPosition = 'center';
  }

  // ── Format bytes for display ─────────────────────────────────
  function formatSize(bytes) {
    if (bytes < 1024) return bytes + ' B';
    if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB';
    return (bytes / 1024 / 1024).toFixed(2) + ' MB';
  }

  return { upload, remove, getMedia, applyToElement, formatSize };
})();
