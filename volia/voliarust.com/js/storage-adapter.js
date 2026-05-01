// ═══════════════════════════════════════════════════════════════
//  Storage Adapter — js/storage-adapter.js
//  Абстракція над сховищем даних. Зараз — localStorage.
//  При міграції на PHP — замінити реалізацію методів нижче,
//  не торкаючись content.js, admin.html та інших файлів.
//
//  Підключення (ОДИН раз, перед content.js):
//    <script src="js/storage-adapter.js"></script>
//    <script src="js/content.js"></script>
//
//  Після міграції на PHP:
//    1. Розгорни api.php (шаблон нижче)
//    2. Змінити USE_PHP = true
//    3. Вказати API_URL = 'https://voliarust.com/api.php'
//    Все інше залишається без змін.
// ═══════════════════════════════════════════════════════════════

window.VoliaStorage = (function () {
  'use strict';

  // ── Конфігурація ─────────────────────────────────────────────
  const USE_PHP = false;                           // true → PHP backend
  const API_URL = 'https://voliarust.com/api.php'; // URL після міграції

  // ── localStorage implementation ──────────────────────────────
  const localImpl = {
    async get(key) {
      try { return JSON.parse(localStorage.getItem(key)); }
      catch { return null; }
    },
    async set(key, value) {
      localStorage.setItem(key, JSON.stringify(value));
    },
    async remove(key) {
      localStorage.removeItem(key);
    },
    // Upload: canvas resize в content.js / VoliaMedia
    async uploadFile(file) {
      throw new Error('uploadFile потребує VoliaMedia.upload() при localStorage режимі');
    },
    async listFiles() {
      return JSON.parse(localStorage.getItem('volia_media') || '[]');
    },
    async deleteFile(id) {
      const list = JSON.parse(localStorage.getItem('volia_media') || '[]');
      localStorage.setItem('volia_media', JSON.stringify(list.filter(m => m.id !== id)));
    },
  };

  // ── PHP implementation ───────────────────────────────────────
  // Готова до підключення — тільки вмикай USE_PHP = true
  const phpImpl = {
    async get(key) {
      const r = await fetch(`${API_URL}?action=get&key=${encodeURIComponent(key)}`);
      const j = await r.json();
      return j.ok ? j.value : null;
    },
    async set(key, value) {
      await fetch(API_URL, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ action: 'set', key, value }),
      });
    },
    async remove(key) {
      await fetch(API_URL, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ action: 'remove', key }),
      });
    },
    async uploadFile(file, key) {
      const fd = new FormData();
      fd.append('action', 'upload');
      fd.append('file', file);
      fd.append('key', key || '');
      const r = await fetch(API_URL, { method: 'POST', body: fd });
      return await r.json(); // { ok, url, id, name, size }
    },
    async listFiles() {
      const r = await fetch(`${API_URL}?action=listFiles`);
      const j = await r.json();
      return j.ok ? j.files : [];
    },
    async deleteFile(id) {
      await fetch(API_URL, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ action: 'deleteFile', id }),
      });
    },
  };

  return USE_PHP ? phpImpl : localImpl;
})();

// ═══════════════════════════════════════════════════════════════
//  PHP API шаблон — api.php
//  Скопіюй цей файл на сервер поруч з index.html.
//  Вимоги: PHP 7.4+, права на запис у /data/ та /uploads/
//
// ── Структура файлів на сервері ──────────────────────────────
//  voliarust.com/
//    api.php           ← цей файл
//    data/             ← JSON дані (створити вручну, chmod 755)
//      volia_news.json
//      volia_site_settings.json
//      ...
//    uploads/          ← завантажені зображення (chmod 755)
//
// ── Захист: перед публікацією додай перевірку токену ─────────
//  $token = $_SERVER['HTTP_X_ADMIN_TOKEN'] ?? '';
//  if ($token !== getenv('ADMIN_SECRET')) { http_response_code(403); die('Forbidden'); }
// ═══════════════════════════════════════════════════════════════
/*
<?php
header('Content-Type: application/json');
header('Access-Control-Allow-Origin: *');
header('Access-Control-Allow-Methods: GET, POST, OPTIONS');
header('Access-Control-Allow-Headers: Content-Type');
if ($_SERVER['REQUEST_METHOD'] === 'OPTIONS') exit;

define('DATA_DIR',    __DIR__ . '/data/');
define('UPLOADS_DIR', __DIR__ . '/uploads/');
define('UPLOADS_URL', '/uploads/');
define('MAX_UPLOAD',  5 * 1024 * 1024); // 5 MB

@mkdir(DATA_DIR, 0755, true);
@mkdir(UPLOADS_DIR, 0755, true);

function ok($data = [])  { echo json_encode(['ok' => true]  + $data); exit; }
function err($msg)       { http_response_code(400); echo json_encode(['ok'=>false,'error'=>$msg]); exit; }
function keyFile($key)   { return DATA_DIR . preg_replace('/[^a-z0-9_\-]/i','_',$key) . '.json'; }

$action = $_GET['action'] ?? (json_decode(file_get_contents('php://input'),true)['action'] ?? '');
$body   = $action !== 'upload' ? (json_decode(file_get_contents('php://input'),true) ?? []) : [];

switch ($action) {

  case 'get':
    $key  = $_GET['key'] ?? '';
    $file = keyFile($key);
    $val  = file_exists($file) ? json_decode(file_get_contents($file), true) : null;
    ok(['value' => $val]);

  case 'set':
    $key = $body['key'] ?? ''; $val = $body['value'] ?? null;
    if (!$key) err('key required');
    file_put_contents(keyFile($key), json_encode($val, JSON_UNESCAPED_UNICODE | JSON_PRETTY_PRINT));
    ok();

  case 'remove':
    @unlink(keyFile($body['key'] ?? ''));
    ok();

  case 'upload':
    if (!isset($_FILES['file'])) err('no file');
    $file = $_FILES['file'];
    if ($file['error'] !== UPLOAD_ERR_OK) err('upload error ' . $file['error']);
    if ($file['size'] > MAX_UPLOAD) err('file too large (max 5 MB)');
    $allowed = ['image/jpeg','image/png','image/webp','image/gif'];
    if (!in_array($file['type'], $allowed)) err('not an image');

    $ext      = ['image/jpeg'=>'jpg','image/png'=>'png','image/webp'=>'webp','image/gif'=>'gif'][$file['type']];
    $name     = time() . '_' . preg_replace('/[^a-z0-9_\-]/i','_', pathinfo($file['name'],PATHINFO_FILENAME)) . '.' . $ext;
    $dest     = UPLOADS_DIR . $name;
    if (!move_uploaded_file($file['tmp_name'], $dest)) err('save failed');

    $url = UPLOADS_URL . $name;
    ok(['url'=>$url, 'name'=>$name, 'size'=>$file['size']]);

  case 'listFiles':
    $files = [];
    foreach (glob(UPLOADS_DIR . '*') as $f) {
      $files[] = ['name'=>basename($f), 'url'=>UPLOADS_URL.basename($f), 'size'=>filesize($f)];
    }
    ok(['files' => $files]);

  case 'deleteFile':
    $name = preg_replace('/[^a-z0-9_\-\.]/i','', $body['id'] ?? '');
    @unlink(UPLOADS_DIR . $name);
    ok();

  default:
    err('unknown action');
}
*/
