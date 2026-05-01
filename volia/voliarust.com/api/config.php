<?php
// ============================================================
// Volia Rust — API Config
// SECRETS LOAD FROM ENV VARS (set via docker-compose / .env)
// Hardcoded fallbacks are random per-deploy unless overridden.
// ============================================================

// ── Auto-load .env file (key=value lines) ───────────────────
// Store in $GLOBALS instead of putenv() — works even if putenv is disabled
$GLOBALS['_volia_env'] = [];
(function() {
    $candidates = [__DIR__ . '/../.env', __DIR__ . '/.env'];
    foreach ($candidates as $envFile) {
        if (!is_file($envFile) || !is_readable($envFile)) continue;
        foreach (file($envFile, FILE_IGNORE_NEW_LINES | FILE_SKIP_EMPTY_LINES) as $line) {
            $line = trim($line);
            if ($line === '' || $line[0] === '#' || !str_contains($line, '=')) continue;
            [$k, $v] = explode('=', $line, 2);
            $k = trim($k); $v = trim($v, " \t\n\r\0\x0B\"'");
            if ($k !== '') $GLOBALS['_volia_env'][$k] = $v;
        }
        return;
    }
})();

function _env(string $k, string $default = ''): string {
    if (isset($GLOBALS['_volia_env'][$k]) && $GLOBALS['_volia_env'][$k] !== '') {
        return $GLOBALS['_volia_env'][$k];
    }
    $v = getenv($k);
    if ($v !== false && $v !== '') return $v;
    return $default;
}

function _envOrRand(string $k, int $bytes = 32): string {
    $v = _env($k);
    if ($v !== '') return $v;
    return bin2hex(random_bytes($bytes));
}

define('ADMIN_TOKEN',         _envOrRand('ADMIN_TOKEN'));
define('JWT_SECRET',          _envOrRand('JWT_SECRET'));
define('MONO_WEBHOOK_SECRET', _envOrRand('MONO_WEBHOOK_SECRET'));
define('AZLINK_SITE_KEY',     _envOrRand('AZLINK_SITE_KEY'));
define('STATS_TOKEN',         _envOrRand('STATS_TOKEN'));

define('SITE_URL',            _env('SITE_URL', 'https://voliarust.com'));
define('MONO_TOKEN',          _env('MONO_TOKEN', ''));
define('MONO_JAR_LINK',       _env('MONO_JAR_LINK', 'https://send.monobank.ua/jar/5nRwVbceA'));
define('STEAM_API_KEY',       _env('STEAM_API_KEY'));
define('RUSTMAPS_API_KEY',    _env('RUSTMAPS_API_KEY'));

// CORS — comma-separated allowed origins
define('ALLOWED_ORIGINS',     _env('ALLOWED_ORIGINS', 'https://voliarust.com,https://www.voliarust.com'));

// Authoritative product prices — never trust client-supplied price
const PRODUCT_PRICES = [
    1 => 50,   // Gold VIP
    2 => 100,  // Titan VIP
    3 => 150,  // Imperial VIP
    4 => 100,  // Тех-набір
    5 => 125,  // Рес-набір
    6 => 150,  // Фарм-набір
    7 => 35,   // 10 Монет
    8 => 65,   // 20 Монет
    9 => 275,  // 85 Монет
];

const PRODUCT_NAMES = [
    1 => 'Gold VIP',    2 => 'Titan VIP',   3 => 'Imperial VIP',
    4 => 'Тех-набір',   5 => 'Рес-набір',   6 => 'Фарм-набір',
    7 => '10 Монет',    8 => '20 Монет',    9 => '85 Монет',
];

// Verified seeds that are already rendered in RustMaps (used for instant thumbnails on wipe)
const RUSTMAPS_SEEDS_4000 = [12345, 55555, 77777, 100000, 1234567, 7654321];
const RUSTMAPS_SEEDS_3500 = [12345, 55555, 77777, 100000, 1234567];

// RCON — Rust WebRCON
define('RCON_SERVERS', [
    'main'   => ['host' => _env('RCON_MAIN_HOST', '57.128.211.126'), 'port' => (int)_env('RCON_MAIN_PORT', '28017'), 'pass' => _env('RCON_MAIN_PASS', '')],
    'monday' => ['host' => _env('RCON_MONDAY_HOST', '57.128.211.126'), 'port' => (int)_env('RCON_MONDAY_PORT', '28020'), 'pass' => _env('RCON_MONDAY_PASS', '')],
]);

// MySQL
define('DB_HOST', _env('DB_HOST', 'localhost'));
define('DB_USER', _env('DB_USER', 'volia'));
define('DB_PASS', _env('DB_PASS', ''));
define('DB_NAME', _env('DB_NAME', 'volia_rust'));

const PRODUCT_COMMANDS = [
    1 => 'addgroup {steam_id} vip1 7d',
    2 => 'addgroup {steam_id} vip2 7d',
    3 => 'addgroup {steam_id} vip3 7d',
    4 => [
        'mbbasket.add {steam_id} sewingkit 40',
        'mbbasket.add {steam_id} techparts 50',
        'mbbasket.add {steam_id} gears 35',
        'mbbasket.add {steam_id} roadsigns 40',
        'mbbasket.add {steam_id} fuse 20',
        'mbbasket.add {steam_id} smgbody 20',
        'mbbasket.add {steam_id} metalspring 40',
        'mbbasket.add {steam_id} metalpipe 40',
        'mbbasket.add {steam_id} metalblade 30',
        'mbbasket.add {steam_id} tarp 40',
        'mbbasket.add {steam_id} targeting.computer 10',
        'mbbasket.add {steam_id} cctv.camera 10',
        'mbbasket.add {steam_id} riflebody 20',
        'mbbasket.add {steam_id} propanetank 40',
        'mbbasket.add {steam_id} semibody 20',
        'mbbasket.add {steam_id} rope 50',
        'mbbasket.add {steam_id} sheetmetal 40',
    ],
    5 => [
        'mbbasket.add {steam_id} cloth 4000',
        'mbbasket.add {steam_id} leather 2000',
        'mbbasket.add {steam_id} scrap 500',
        'mbbasket.add {steam_id} crude.oil 300',
        'mbbasket.add {steam_id} fat.animal 1000',
        'mbbasket.add {steam_id} wood 40000',
        'mbbasket.add {steam_id} metal.fragments 15000',
        'mbbasket.add {steam_id} stones 30000',
        'mbbasket.add {steam_id} hq.metal.ore 500',
        'mbbasket.add {steam_id} metal.refined 200',
    ],
    6 => [
        'mbbasket.add {steam_id} chainsaw 3',
        'mbbasket.add {steam_id} jackhammer 3',
        'mbbasket.add {steam_id} lowgradefuel 300',
        'mbbasket.add {steam_id} axe.salvaged 3',
        'mbbasket.add {steam_id} icepick.salvaged 3',
        'mbbasket.add {steam_id} scraptea.pure 10',
        'mbbasket.add {steam_id} oretea.pure 10',
        'mbbasket.add {steam_id} woodtea.pure 10',
    ],
    7 => 'mb.give {steam_id} 10',
    8 => 'mb.give {steam_id} 20',
    9 => 'mb.give {steam_id} 85',
];
