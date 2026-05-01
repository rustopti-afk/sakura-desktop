<?php
require_once __DIR__ . '/config.php';
require_once __DIR__ . '/db.php';

// ── CORS & headers ────────────────────────────────────────────
$origin  = $_SERVER['HTTP_ORIGIN'] ?? '';
$allowed = array_filter(array_map('trim', explode(',', ALLOWED_ORIGINS)));
if ($origin && in_array($origin, $allowed, true)) {
    header("Access-Control-Allow-Origin: $origin");
    header('Vary: Origin');
    header('Access-Control-Allow-Credentials: true');
}
header('Access-Control-Allow-Headers: Content-Type, Authorization, X-Admin-Token, X-Webhook-Secret, Azuriom-Link-Token');
header('Access-Control-Allow-Methods: GET, POST, PUT, DELETE, OPTIONS');
if ($_SERVER['REQUEST_METHOD'] === 'OPTIONS') { http_response_code(204); exit; }
header('Content-Type: application/json; charset=utf-8');

// ── Helpers ───────────────────────────────────────────────────
function out($data, int $code = 200) {
    http_response_code($code);
    echo json_encode($data, JSON_UNESCAPED_UNICODE | JSON_UNESCAPED_SLASHES);
    exit;
}

function b64u(string $s): string {
    return rtrim(strtr(base64_encode($s), '+/', '-_'), '=');
}

function b64ud(string $s): string {
    return base64_decode(strtr($s, '-_', '+/'));
}

function jwt_sign(array $payload): string {
    $h = b64u(json_encode(['alg' => 'HS256', 'typ' => 'JWT']));
    $p = b64u(json_encode($payload));
    return "$h.$p." . b64u(hash_hmac('sha256', "$h.$p", JWT_SECRET, true));
}

function jwt_verify(string $token): ?array {
    $parts = explode('.', $token);
    if (count($parts) !== 3) return null;
    [$h, $p, $sig] = $parts;
    if (!hash_equals(b64u(hash_hmac('sha256', "$h.$p", JWT_SECRET, true)), $sig)) return null;
    $data = json_decode(b64ud($p), true);
    if (!is_array($data)) return null;
    if (isset($data['exp']) && $data['exp'] < time()) return null;
    return $data;
}

function bearer(): ?array {
    $auth = $_SERVER['HTTP_AUTHORIZATION'] ?? '';
    if (!str_starts_with($auth, 'Bearer ')) return null;
    return jwt_verify(substr($auth, 7));
}

function admin_guard(): void {
    if (($_SERVER['HTTP_X_ADMIN_TOKEN'] ?? '') !== ADMIN_TOKEN) out(['error' => 'Unauthorized'], 401);
}

function body(): array {
    static $d = null;
    if ($d === null) $d = json_decode(file_get_contents('php://input'), true) ?? [];
    return $d;
}

function uid(): int {
    return intval(microtime(true) * 1000) * 1000 + random_int(0, 999);
}

// ── WebRCON (Rust) ────────────────────────────────────────────
function rcon_send(string $server, string $command): array {
    $cfg = RCON_SERVERS[$server] ?? null;
    if (!$cfg || !$cfg['pass']) return ['ok' => false, 'error' => 'RCON not configured'];

    $host = $cfg['host'];
    $port = $cfg['port'];
    $pass = $cfg['pass'];

    $sock = @fsockopen($host, $port, $errno, $errstr, 5);
    if (!$sock) return ['ok' => false, 'error' => "Connect failed: $errstr ($errno)"];

    stream_set_timeout($sock, 5);

    // WebSocket handshake
    $key = base64_encode(random_bytes(16));
    fwrite($sock,
        "GET /$pass HTTP/1.1\r\n" .
        "Host: $host:$port\r\n" .
        "Upgrade: websocket\r\n" .
        "Connection: Upgrade\r\n" .
        "Sec-WebSocket-Key: $key\r\n" .
        "Sec-WebSocket-Version: 13\r\n\r\n"
    );

    // Read HTTP response headers
    $resp = '';
    while (!feof($sock)) {
        $line = fgets($sock, 1024);
        $resp .= $line;
        if ($line === "\r\n") break;
    }
    if (!str_contains($resp, '101')) {
        fclose($sock);
        return ['ok' => false, 'error' => 'WebSocket handshake failed'];
    }

    // Send command as WebSocket text frame (with masking)
    $id      = rand(1, 65535);
    $payload = json_encode(['Identifier' => $id, 'Message' => $command, 'Type' => 'Request']);
    $len     = strlen($payload);
    $mask    = random_bytes(4);
    $masked  = '';
    for ($i = 0; $i < $len; $i++) $masked .= $payload[$i] ^ $mask[$i % 4];

    $frame = "\x81"; // FIN + text opcode
    if ($len < 126) {
        $frame .= chr(0x80 | $len);
    } else {
        $frame .= chr(0x80 | 126) . pack('n', $len);
    }
    $frame .= $mask . $masked;
    fwrite($sock, $frame);

    // Read response frame
    $result = '';
    $raw = fread($sock, 4096);
    fclose($sock);

    if ($raw === false || strlen($raw) < 2) return ['ok' => true, 'response' => ''];

    // Parse WebSocket frame
    $opcode = ord($raw[0]) & 0x0F;
    $plen   = ord($raw[1]) & 0x7F;
    $offset = 2;
    if ($plen === 126) { $plen = unpack('n', substr($raw, 2, 2))[1]; $offset = 4; }
    $result = substr($raw, $offset, $plen);

    $decoded = json_decode($result, true);
    return ['ok' => true, 'response' => $decoded['Message'] ?? $result];
}

function execute_product(int $productId, string $steamId, string $playerName, string $server, $orderId): bool {
    $tpl = PRODUCT_COMMANDS[$productId] ?? null;
    if (!$tpl) return false;
    $cmds = is_array($tpl) ? $tpl : [$tpl];
    $allOk = true;
    foreach ($cmds as $cmdTpl) {
        $cmd  = str_replace('{steam_id}', $steamId, $cmdTpl);
        $rcon = rcon_send($server, $cmd);
        if (!$rcon['ok']) {
            $allOk = false;
            DB::run(
                "INSERT INTO pending_commands (id,steamId,playerName,command,orderId,server) VALUES (?,?,?,?,?,?)",
                [uid(), $steamId, $playerName, $cmd, $orderId, $server]
            );
        }
    }
    return $allOk;
}

function http_get(string $url, array $headers = []): string|false {
    $opts = ['timeout' => 8, 'ignore_errors' => true];
    if ($headers) $opts['header'] = implode("\r\n", $headers);
    $ctx = stream_context_create(['http' => $opts]);
    return @file_get_contents($url, false, $ctx);
}

// Generates 3 random maps from pre-verified RustMaps seeds (images available instantly)
function generate_wipe_maps(string $server, int $count = 3): void {
    $size   = $server === 'monday' ? 3500 : 4000;
    $names  = ['Карта Alpha', 'Карта Beta', 'Карта Gamma'];
    $apiKey = RUSTMAPS_API_KEY;
    $pool   = $size === 3500 ? RUSTMAPS_SEEDS_3500 : RUSTMAPS_SEEDS_4000;

    // Pick $count unique random seeds from verified pool
    shuffle($pool);
    $seeds = array_slice($pool, 0, $count);

    foreach ($seeds as $i => $seed) {
        $name   = $names[$i] ?? "Карта " . ($i + 1);
        $imgUrl = '';
        $mons   = 0;
        $mapUrl = "https://rustmaps.com/map/{$size}_{$seed}";

        if ($apiKey) {
            $raw = http_get(
                "https://api.rustmaps.com/legacy/api/v2/maps/{$seed}/{$size}",
                ["X-API-Key: {$apiKey}"]
            );
            if ($raw) {
                $data   = json_decode($raw, true);
                $imgUrl = $data['thumbnailUrl'] ?? ($data['imageUrl'] ?? '');
                $mons   = count($data['monuments'] ?? []);
                if (!empty($data['url'])) $mapUrl = $data['url']; // use official URL from API
            }
        }

        DB::run(
            "INSERT INTO maps (id,name,seed,size,type,imgUrl,mapUrl,monuments,`desc`,active,server)
             VALUES (?,?,?,?,?,?,?,?,?,1,?)",
            [uid(), $name, (string)$seed, $size, 'Procedural_Map', $imgUrl, $mapUrl, $mons, '', $server]
        );
    }
}

// Fetches rendered images for maps that were generated (imgUrl is empty)
function refresh_map_images(string $server): int {
    $apiKey = RUSTMAPS_API_KEY;
    if (!$apiKey) return 0;
    $maps = DB::q("SELECT id,seed,size FROM maps WHERE active=1 AND server=? AND imgUrl=''", [$server]);
    $updated = 0;
    foreach ($maps as $map) {
        $raw = http_get(
            "https://api.rustmaps.com/legacy/api/v2/maps/{$map['seed']}/{$map['size']}",
            ["X-API-Key: {$apiKey}"]
        );
        if (!$raw) continue;
        $data = json_decode($raw, true);
        $thumb = $data['thumbnailUrl'] ?? ($data['imageUrl'] ?? '');
        $mons  = count($data['monuments'] ?? []);
        if ($thumb) {
            DB::run("UPDATE maps SET imgUrl=?,monuments=? WHERE id=?", [$thumb, $mons, $map['id']]);
            $updated++;
        }
    }
    return $updated;
}

function http_post(string $url, array $params): string|false {
    $body = http_build_query($params);
    if (function_exists('curl_init')) {
        $ch = curl_init($url);
        curl_setopt_array($ch, [
            CURLOPT_POST           => true,
            CURLOPT_POSTFIELDS     => $body,
            CURLOPT_RETURNTRANSFER => true,
            CURLOPT_TIMEOUT        => 10,
            CURLOPT_HTTPHEADER     => ['Content-Type: application/x-www-form-urlencoded'],
            CURLOPT_FOLLOWLOCATION => false,
        ]);
        $resp = curl_exec($ch);
        curl_close($ch);
        return $resp;
    }
    $ctx = stream_context_create(['http' => [
        'method'          => 'POST',
        'header'          => 'Content-Type: application/x-www-form-urlencoded',
        'content'         => $body,
        'timeout'         => 10,
        'ignore_errors'   => true,
        'follow_location' => 0,
    ]]);
    return @file_get_contents($url, false, $ctx);
}

function http_post_json(string $url, array $data, array $headers = []): string|false {
    $body    = json_encode($data);
    $hdrs    = array_merge(['Content-Type: application/json'], $headers);
    $ctx = stream_context_create(['http' => [
        'method'  => 'POST',
        'header'  => implode("\r\n", $hdrs),
        'content' => $body,
        'timeout' => 10,
        'ignore_errors' => true,
    ]]);
    return @file_get_contents($url, false, $ctx);
}

function route(string $pattern, string $uri): array|false {
    $re = '#^' . preg_replace('#:(\w+)#', '(?P<$1>[^/]+)', $pattern) . '$#';
    if (!preg_match($re, $uri, $m)) return false;
    return array_filter($m, 'is_string', ARRAY_FILTER_USE_KEY);
}

// Calculates next wipe date (Thu for main, Mon for monday)
function next_wipe_date(string $server): string {
    $target = $server === 'monday' ? 1 : 4; // ISO day: 1=Mon, 4=Thu
    $now    = new DateTime('now', new DateTimeZone('UTC'));
    $dow    = (int)$now->format('N');
    $diff   = ($target - $dow + 7) % 7 ?: 7;
    $wipe   = clone $now;
    $wipe->modify("+{$diff} days")->setTime(12, 0, 0);
    return $wipe->format('c');
}

// Builds full maps response: maps with votes+percent, totalVotes, myVoteMapId, wipeDate
function maps_response(string $server, string $wipeId, ?string $steamId): array {
    $maps = DB::q("SELECT * FROM maps WHERE active=1 AND server=? ORDER BY id DESC", [$server]);

    if ($maps) {
        // Single aggregated query instead of N individual COUNTs
        $mapIds       = array_column($maps, 'id');
        $placeholders = implode(',', array_fill(0, count($mapIds), '?'));
        $voteRows     = DB::q(
            "SELECT mapId, COUNT(*) c FROM votes WHERE wipeId=? AND mapId IN ($placeholders) GROUP BY mapId",
            array_merge([$wipeId], $mapIds)
        );
        $voteCounts = [];
        foreach ($voteRows as $r) $voteCounts[$r['mapId']] = (int)$r['c'];

        $totalVotes = 0;
        foreach ($maps as &$map) {
            $map['votes'] = $voteCounts[$map['id']] ?? 0;
            $totalVotes  += $map['votes'];
        }
        foreach ($maps as &$map) {
            $map['percent'] = $totalVotes > 0 ? round($map['votes'] / $totalVotes * 100) : 0;
        }
        unset($map);
    }

    $totalVotes  = array_sum(array_column($maps, 'votes'));
    $myVoteMapId = null;
    if ($steamId) {
        $v = DB::q("SELECT mapId FROM votes WHERE steamId=? AND wipeId=?", [$steamId, $wipeId]);
        $myVoteMapId = isset($v[0]['mapId']) ? (int)$v[0]['mapId'] : null;
    }

    return [
        'maps'        => $maps,
        'totalVotes'  => $totalVotes,
        'myVoteMapId' => $myVoteMapId,
        'wipeDate'    => next_wipe_date($server),
    ];
}

// ── Init DB ───────────────────────────────────────────────────
try {
    DB::init();
} catch (PDOException $e) {
    error_log('[volia-rust] DB init: ' . $e->getMessage());
    out(['error' => 'Database unavailable'], 503);
}

// ── Request ───────────────────────────────────────────────────
$method = $_SERVER['REQUEST_METHOD'];
$uri    = rtrim(preg_replace('#^/api#', '', parse_url($_SERVER['REQUEST_URI'], PHP_URL_PATH)), '/') ?: '/';

// ═════════════════════════════════════════════════════════════
// AUTH — ME  (validate token, return user info)
// ═════════════════════════════════════════════════════════════

if ($method === 'GET' && $uri === '/auth/me') {
    $user = bearer();
    if (!$user) out(['error' => 'Unauthorized'], 401);
    out(['ok' => true, 'steamId' => $user['steamId'], 'name' => $user['name'], 'avatar' => $user['avatar'] ?? '']);
}

if ($method === 'GET' && $uri === '/auth/steam/profile') {
    $steamId = preg_replace('/\D/', '', $_GET['steamId'] ?? '');
    if (!$steamId) out(['error' => 'Bad request'], 400);
    $name = ''; $avatar = '';
    if (STEAM_API_KEY) {
        $raw = http_get("https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v2/?key=" . STEAM_API_KEY . "&steamids=$steamId");
        if ($raw) {
            $pl = json_decode($raw, true)['response']['players'][0] ?? null;
            if ($pl) { $name = $pl['personaname']; $avatar = $pl['avatarfull']; }
        }
    }
    if (!$name) {
        $raw = http_get("https://steamcommunity.com/profiles/$steamId/?xml=1");
        if ($raw) {
            if (preg_match('#<steamID><!\[CDATA\[(.+?)\]\]>#s', $raw, $m)) $name = trim($m[1]);
            if (preg_match('#<avatarFull><!\[CDATA\[(https?://[^\]]+)\]\]>#s', $raw, $m)) $avatar = trim($m[1]);
        }
    }
    out(['steamId' => $steamId, 'name' => $name ?: "Player_{$steamId}", 'avatar' => $avatar]);
}

// ═════════════════════════════════════════════════════════════
// STEAM AUTH — REDIRECT
// ═════════════════════════════════════════════════════════════


if ($method === 'GET' && $uri === '/auth/steam') {
    $from = preg_replace('/[^a-z0-9_-]/i', '', $_GET['from'] ?? 'home');
    // Use actual request scheme+host so realm always matches the browser URL
    $scheme   = (!empty($_SERVER['HTTPS']) && $_SERVER['HTTPS'] !== 'off') ? 'https' : 'http';
    $scheme   = ($_SERVER['HTTP_X_FORWARDED_PROTO'] ?? $scheme) === 'https' ? 'https' : $scheme;
    $host     = $_SERVER['HTTP_HOST'];
    $baseUrl  = $scheme . '://' . $host;
    $returnTo = $baseUrl . '/api/auth/steam/callback?from=' . $from;
    $qs = http_build_query([
        'openid.ns'         => 'http://specs.openid.net/auth/2.0',
        'openid.mode'       => 'checkid_setup',
        'openid.return_to'  => $returnTo,
        'openid.realm'      => $baseUrl . '/',
        'openid.identity'   => 'http://specs.openid.net/auth/2.0/identifier_select',
        'openid.claimed_id' => 'http://specs.openid.net/auth/2.0/identifier_select',
    ]);
    header('Content-Type: text/html');
    header('Location: https://steamcommunity.com/openid/login?' . $qs);
    exit;
}

// ═════════════════════════════════════════════════════════════
// STEAM AUTH — CALLBACK
// ═════════════════════════════════════════════════════════════

if ($method === 'GET' && $uri === '/auth/steam/callback') {
    // Parse query string manually — $_GET converts dots to underscores and '+' to space
    $qs = [];
    foreach (explode('&', $_SERVER['QUERY_STRING']) as $pair) {
        if (!str_contains($pair, '=')) continue;
        [$k, $v] = explode('=', $pair, 2);
        $qs[rawurldecode($k)] = rawurldecode($v);
    }

    $from = preg_replace('/[^a-z0-9_-]/i', '', $qs['from'] ?? 'home');

    $params = array_filter($qs, fn($k) => str_starts_with($k, 'openid.'), ARRAY_FILTER_USE_KEY);
    $params['openid.mode'] = 'check_authentication';
    $resp = http_post('https://steamcommunity.com/openid/login', $params);

    if (!$resp || strpos($resp, 'is_valid:true') === false) {
        $reason = !$resp ? 'http_post_failed' : urlencode(substr($resp, 0, 200));
        header('Content-Type: text/html');
        header('Location: ' . SITE_URL . '/?error=' . $reason);
        exit;
    }

    if (!preg_match('#steamcommunity\.com/openid/id/(\d+)$#', $qs['openid.claimed_id'] ?? '', $m)) {
        header('Content-Type: text/html');
        header('Location: ' . SITE_URL . '/?error=auth_failed');
        exit;
    }
    $steamId = $m[1];

    // Fetch Steam profile
    $name = 'Player'; $avatar = '';
    if (STEAM_API_KEY) {
        $raw = http_get("https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v2/?key=" . STEAM_API_KEY . "&steamids=$steamId");
        if ($raw) {
            $pl = json_decode($raw, true)['response']['players'][0] ?? null;
            if ($pl) { $name = $pl['personaname']; $avatar = $pl['avatarfull']; }
        }
    } else {
        $raw = http_get("https://steamcommunity.com/profiles/$steamId/?xml=1");
        if ($raw && preg_match('#<steamID><!\[CDATA\[(.+?)\]\]>#', $raw, $nm)) $name = $nm[1];
        if ($raw && preg_match('#<avatarFull><!\[CDATA\[(https?://[^\]]+)\]\]>#', $raw, $av)) $avatar = $av[1];
    }

    DB::run(
        "INSERT INTO players (steamId,name,avatar) VALUES (?,?,?)
         ON DUPLICATE KEY UPDATE name=VALUES(name), avatar=VALUES(avatar)",
        [$steamId, $name, $avatar]
    );

    $token = jwt_sign([
        'steamId' => $steamId,
        'name'    => $name,
        'avatar'  => $avatar,
        'exp'     => time() + 86400 * 30,
    ]);

    // Redirect back to the page the user came from
    $redirectMap = [
        'shop'  => '/pages/shop.html',
        'vote'  => '/pages/vote.html',
        'stats' => '/pages/statistics.html',
    ];
    $redirect = $redirectMap[$from] ?? '/';
    header('Content-Type: text/html');
    header('Location: ' . SITE_URL . $redirect . '?token=' . $token);
    exit;
}

// ═════════════════════════════════════════════════════════════
// AZLINK  (Rust plugin polls every 15s)
// ═════════════════════════════════════════════════════════════

if ($uri === '/azlink') {
    if (($_SERVER['HTTP_AZURIOM_LINK_TOKEN'] ?? '') !== AZLINK_SITE_KEY) out(['error' => 'Unauthorized'], 401);

    if ($method === 'GET') out(['status' => 'ok', 'version' => '1.0.0']);

    if ($method === 'POST') {
        $server = body()['server'] ?? 'main';
        $cmds   = DB::q("SELECT * FROM pending_commands WHERE server=? ORDER BY createdAt ASC LIMIT 50", [$server]);

        $grouped = [];
        foreach ($cmds as $c) {
            $sid = $c['steamId'];
            if (!isset($grouped[$sid])) $grouped[$sid] = ['uid' => $sid, 'name' => $c['playerName'], 'values' => []];
            $grouped[$sid]['values'][] = $c['command'];
            DB::run("DELETE FROM pending_commands WHERE id=?", [$c['id']]);
        }
        out(['commands' => array_values($grouped)]);
    }

    out(['error' => 'Method not allowed'], 405);
}

// ═════════════════════════════════════════════════════════════
// SHOP — CHECKOUT
// ═════════════════════════════════════════════════════════════

if ($method === 'POST' && $uri === '/shop/checkout') {
    $user = bearer();
    if (!$user) out(['error' => 'Unauthorized — please log in via Steam'], 401);

    $b         = body();
    $productId = (int)($b['productId'] ?? 0);
    $server    = in_array($b['server'] ?? '', ['main', 'monday']) ? $b['server'] : 'main';

    $price       = PRODUCT_PRICES[$productId] ?? null;
    $productName = PRODUCT_NAMES[$productId]  ?? null;
    if (!$price || !$productName) out(['error' => 'Invalid product'], 400);

    $steamId    = $user['steamId'];
    $playerName = $user['name'] ?? 'Гравець';

    $code = 'PAY-' . strtoupper(bin2hex(random_bytes(3)));
    $id   = uid();
    DB::run(
        "INSERT INTO orders (id,confirmCode,steamId,playerName,productId,productName,price,server,status)
         VALUES (?,?,?,?,?,?,?,?,'pending')",
        [$id, $code, $steamId, $playerName, $productId, $productName, $price, $server]
    );
    out(['ok' => true, 'orderId' => $id, 'code' => $code, 'jarLink' => MONO_JAR_LINK, 'price' => $price]);
}

// ═════════════════════════════════════════════════════════════
// SHOP — CHECK STATUS
// ═════════════════════════════════════════════════════════════

if ($method === 'GET' && $uri === '/shop/check') {
    $orderId = $_GET['orderId'] ?? '';
    if (!$orderId) out(['error' => 'Bad request'], 400);
    $rows = DB::q("SELECT status, confirmCode FROM orders WHERE id=?", [$orderId]);
    if (!$rows) out(['error' => 'Not found'], 404);
    out(['status' => $rows[0]['status'], 'code' => $rows[0]['confirmCode']]);
}

// ═════════════════════════════════════════════════════════════
// SHOP — MONOBANK WEBHOOK
// ═════════════════════════════════════════════════════════════

if ($method === 'POST' && $uri === '/shop/webhook') {
    // Monobank sends webhook to URL with ?secret= param
    $secret = $_GET['secret'] ?? ($_SERVER['HTTP_X_WEBHOOK_SECRET'] ?? '');
    if (MONO_WEBHOOK_SECRET && !hash_equals(MONO_WEBHOOK_SECRET, $secret)) out(['error' => 'Unauthorized'], 401);

    $data    = body();
    $comment = $data['data']['statementItem']['comment'] ?? '';
    $amount  = ($data['data']['statementItem']['amount'] ?? 0) / 100;

    if (!preg_match('/PAY-[A-F0-9]{6}/i', strtoupper($comment), $m)) out(['ok' => true]);
    $code = strtoupper($m[0]);

    $rows = DB::q("SELECT * FROM orders WHERE confirmCode=?", [$code]);
    if (!$rows || $rows[0]['status'] !== 'pending') out(['ok' => true]);
    $order = $rows[0];

    if ($amount < round((float)$order['price'] * 0.99, 2)) out(['ok' => true]);

    DB::run("UPDATE orders SET status='completed', completedAt=NOW() WHERE id=?", [$order['id']]);

    execute_product((int)$order['productId'], $order['steamId'], $order['playerName'], $order['server'], $order['id']);
    out(['ok' => true]);
}

// ═════════════════════════════════════════════════════════════
// MAPS — public
// ═════════════════════════════════════════════════════════════

if ($method === 'GET' && $uri === '/maps') {
    $server    = in_array($_GET['server'] ?? '', ['main', 'monday']) ? $_GET['server'] : 'main';
    $wipeId    = date('Y-W');
    $browserId = trim($_GET['browserId'] ?? '');
    $voterId   = ($browserId && strlen($browserId) <= 64) ? $browserId : null;
    // Auto-generate maps if none exist for this server yet this wipe
    $existing = DB::q("SELECT COUNT(*) c FROM maps WHERE active=1 AND server=?", [$server]);
    if ((int)$existing[0]['c'] === 0) generate_wipe_maps($server);
    out(maps_response($server, $wipeId, $voterId));
}

// ═════════════════════════════════════════════════════════════
// VOTE
// ═════════════════════════════════════════════════════════════

if ($method === 'POST' && $uri === '/vote') {
    $b         = body();
    $mapId     = $b['mapId'] ?? null;
    $server    = in_array($b['server'] ?? '', ['main', 'monday']) ? $b['server'] : 'main';
    $wipeId    = $b['wipeId'] ?? date('Y-W');
    $browserId = trim($b['browserId'] ?? '');

    if (!$mapId || !$browserId) out(['error' => 'Bad request'], 400);
    if (strlen($browserId) > 64) out(['error' => 'Bad request'], 400);

    $affected = DB::run(
        "INSERT INTO votes (mapId,steamId,name,wipeId) VALUES (?,?,?,?)
         ON DUPLICATE KEY UPDATE mapId=VALUES(mapId)",
        [$mapId, $browserId, '', $wipeId]
    );
    $changed = $affected === 2;

    $updated = maps_response($server, $wipeId, $browserId);
    out(array_merge(['ok' => true, 'changed' => $changed], $updated));
}

// ═════════════════════════════════════════════════════════════
// STATS — public
// ═════════════════════════════════════════════════════════════

if ($method === 'GET' && $uri === '/stats') {
    $server  = in_array($_GET['server'] ?? '', ['main', 'monday']) ? $_GET['server'] : 'main';
    $limit   = min((int)($_GET['limit'] ?? 50), 200);
    $page    = max(1, (int)($_GET['page'] ?? 1));
    $offset  = ($page - 1) * $limit;
    $sortMap = ['kills'=>'kills','deaths'=>'deaths','raids'=>'raids','playtime'=>'playtime','gathered'=>'gathered'];
    $sort    = $sortMap[$_GET['sort'] ?? ''] ?? 'kills';
    $totalRegistered = (int)DB::q("SELECT COUNT(*) as c FROM players")[0]['c'];
    $totalOnServer   = (int)DB::q("SELECT COUNT(*) as c FROM players WHERE server=?", [$server])[0]['c'];
    $players = DB::q("SELECT * FROM players WHERE server=? ORDER BY `$sort` DESC LIMIT $limit OFFSET $offset", [$server]);
    // add computed kd and rank
    $rank = $offset + 1;
    foreach ($players as &$p) {
        $p['kd']   = $p['deaths'] > 0 ? round($p['kills'] / $p['deaths'], 2) : (float)$p['kills'];
        $p['rank'] = $rank++;
    }
    unset($p);
    $agg     = DB::q("SELECT COALESCE(SUM(kills),0) as totalKills, COALESCE(SUM(raids),0) as totalRaids FROM players WHERE server=?", [$server])[0];
    out([
        'players'    => $players,
        'total'      => $totalRegistered,
        'totalOnServer' => $totalOnServer,
        'pages'      => max(1, (int)ceil($totalOnServer / $limit)),
        'totalKills' => (int)$agg['totalKills'],
        'totalRaids' => (int)$agg['totalRaids'],
    ]);
}

// ═════════════════════════════════════════════════════════════
// STATS — push from Rust plugin
// ═════════════════════════════════════════════════════════════

if ($method === 'POST' && $uri === '/stats/push') {
    if (($_SERVER['HTTP_X_ADMIN_TOKEN'] ?? '') !== STATS_TOKEN) out(['error' => 'Unauthorized'], 401);
    $players = body();
    if (!is_array($players)) out(['error' => 'Bad request'], 400);
    foreach ($players as $p) {
        if (empty($p['steamId'])) continue;
        DB::run(
            "INSERT INTO players (steamId,name,avatar,server,kills,deaths,raids,playtime,gathered)
             VALUES (?,?,?,?,?,?,?,?,?)
             ON DUPLICATE KEY UPDATE
               name=VALUES(name), avatar=VALUES(avatar),
               kills=VALUES(kills), deaths=VALUES(deaths),
               raids=VALUES(raids), playtime=VALUES(playtime), gathered=VALUES(gathered)",
            [
                $p['steamId'], $p['name'] ?? '', $p['avatar'] ?? '', $p['server'] ?? 'main',
                (int)($p['kills'] ?? 0), (int)($p['deaths'] ?? 0),
                (int)($p['raids'] ?? 0), (int)($p['playtime'] ?? 0), (int)($p['gathered'] ?? 0),
            ]
        );
    }
    out(['ok' => true]);
}

// ═════════════════════════════════════════════════════════════
// ADMIN — ORDERS
// ═════════════════════════════════════════════════════════════


if ($uri === '/admin/orders' && $method === 'GET') {
    admin_guard();
    out(DB::q("SELECT * FROM orders ORDER BY createdAt DESC LIMIT 200"));
}

// ─── ADMIN — create order manually ────────────────────────────
if ($uri === '/admin/orders' && $method === 'POST') {
    admin_guard();
    $b         = body();
    $steamId   = trim($b['steamId'] ?? '');
    $name      = trim($b['playerName'] ?? 'Manual');
    $productId = (int)($b['productId'] ?? 0);
    $price     = (float)($b['price'] ?? 0);
    $server    = in_array($b['server'] ?? '', ['main', 'monday']) ? $b['server'] : 'main';
    if (!$steamId || !$productId) out(['error' => 'Bad request'], 400);

    $products = [1=>'Gold VIP',2=>'Titan VIP',3=>'Imperial VIP',4=>'Тех-набір',5=>'Рес-набір',6=>'Фарм-набір',7=>'10 Монет',8=>'20 Монет',9=>'85 Монет'];
    $productName = $products[$productId] ?? "Product $productId";

    $id = uid();
    DB::run("INSERT INTO orders (id,steamId,playerName,productId,productName,price,server,status,confirmCode,completedAt,createdAt)
             VALUES (?,?,?,?,?,?,?,'completed','MANUAL-ADM',NOW(),NOW())",
            [$id, $steamId, $name, $productId, $productName, $price, $server]);

    $delivered = execute_product($productId, $steamId, $name, $server, $id);
    out(['ok' => true, 'delivered' => $delivered]);
}

// ─── ADMIN — dashboard revenue + registered stats ─────────────
if ($uri === '/admin/dashboard' && $method === 'GET') {
    admin_guard();
    $revenue    = (float)DB::q("SELECT COALESCE(SUM(price),0) s FROM orders WHERE status='completed'")[0]['s'];
    $registered = (int)DB::q("SELECT COUNT(*) c FROM players")[0]['c'];
    $orders     = (int)DB::q("SELECT COUNT(*) c FROM orders WHERE status='completed'")[0]['c'];
    out(['revenue' => $revenue, 'registered' => $registered, 'completedOrders' => $orders]);
}

// ─── ADMIN — pending commands list ────────────────────────────
if ($uri === '/admin/pending-commands' && $method === 'GET') {
    admin_guard();
    out(DB::q("SELECT * FROM pending_commands ORDER BY createdAt DESC LIMIT 100"));
}

// ─── ADMIN — give privilege manually ──────────────────────────
// ─── ADMIN — test RCON connection ─────────────────────────────
// ─── ADMIN — Monobank webhook register/check ──────────────────
if ($uri === '/admin/mono-webhook' && $method === 'GET') {
    admin_guard();
    $webhookUrl = SITE_URL . '/api/shop/webhook?secret=' . urlencode(MONO_WEBHOOK_SECRET);
    if (!MONO_TOKEN) out(['ok' => false, 'error' => 'MONO_TOKEN not set', 'webhook_url' => $webhookUrl]);
    $res  = http_get('https://api.monobank.ua/personal/client-info', ["X-Token: " . MONO_TOKEN]);
    $info = $res ? json_decode($res, true) : null;
    out(['ok' => true, 'webhook_url' => $webhookUrl, 'client' => $info['name'] ?? null]);
}

if ($uri === '/admin/mono-webhook' && $method === 'POST') {
    admin_guard();
    if (!MONO_TOKEN) out(['ok' => false, 'error' => 'MONO_TOKEN not set in .env']);
    $webhookUrl = SITE_URL . '/api/shop/webhook?secret=' . urlencode(MONO_WEBHOOK_SECRET);
    $ctx = stream_context_create(['http' => [
        'method'  => 'POST',
        'header'  => "X-Token: " . MONO_TOKEN . "\r\nContent-Type: application/json",
        'content' => json_encode(['webHookUrl' => $webhookUrl]),
        'timeout' => 10,
        'ignore_errors' => true,
    ]]);
    $res  = @file_get_contents('https://api.monobank.ua/personal/webhook', false, $ctx);
    $code = $http_response_header[0] ?? '';
    $ok   = str_contains($code, '200');
    out(['ok' => $ok, 'webhook_url' => $webhookUrl, 'mono_response' => $res]);
}

if ($uri === '/admin/rcon-test' && $method === 'POST') {
    admin_guard();
    $server = body()['server'] ?? 'main';
    $result = rcon_send($server, 'version');
    out($result);
}

if ($uri === '/admin/give-privilege' && $method === 'POST') {
    admin_guard();
    $b       = body();
    $steamId = trim($b['steamId'] ?? '');
    $name    = trim($b['playerName'] ?? '');
    $command = trim($b['command'] ?? '');
    $server  = in_array($b['server'] ?? '', ['main', 'monday', '5x']) ? $b['server'] : 'main';
    if (!$steamId || !$command) out(['error' => 'Bad request'], 400);
    $cmd = str_replace('{steam_id}', $steamId, $command);

    // Try RCON first
    $rcon = rcon_send($server, $cmd);
    if ($rcon['ok']) {
        // Log to DB as record
        DB::run("INSERT INTO pending_commands (id,steamId,playerName,command,orderId,server) VALUES (?,?,?,?,?,?)",
                [uid(), $steamId, $name, $cmd . ' [rcon_sent]', 0, $server]);
        out(['ok' => true, 'method' => 'rcon', 'response' => $rcon['response']]);
    }

    // RCON failed — queue for plugin pickup
    DB::run("INSERT INTO pending_commands (id,steamId,playerName,command,orderId,server) VALUES (?,?,?,?,?,?)",
            [uid(), $steamId, $name, $cmd, 0, $server]);
    out(['ok' => true, 'method' => 'queued', 'rcon_error' => $rcon['error'] ?? '']);
}

if ($p = route('/admin/orders/:id/complete', $uri)) {
    admin_guard();
    if ($method !== 'POST') out(['error' => 'Method not allowed'], 405);
    $rows = DB::q("SELECT * FROM orders WHERE id=?", [$p['id']]);
    if (!$rows) out(['error' => 'Not found'], 404);
    $o = $rows[0];
    DB::run("UPDATE orders SET status='completed', completedAt=NOW() WHERE id=?", [$o['id']]);
    $delivered = execute_product((int)$o['productId'], $o['steamId'], $o['playerName'], $o['server'], $o['id']);
    out(['ok' => true, 'delivered' => $delivered]);
}

if ($p = route('/admin/orders/:id/rcon', $uri)) {
    admin_guard();
    if ($method !== 'POST') out(['error' => 'Method not allowed'], 405);
    $rows = DB::q("SELECT * FROM orders WHERE id=?", [$p['id']]);
    if (!$rows) out(['error' => 'Not found'], 404);
    $o = $rows[0];
    $delivered = execute_product((int)$o['productId'], $o['steamId'], $o['playerName'], $o['server'], $o['id']);
    out(['ok' => true, 'delivered' => $delivered]);
}

// ═════════════════════════════════════════════════════════════
// ADMIN — MAPS
// ═════════════════════════════════════════════════════════════

if ($uri === '/admin/maps/refresh-images' && $method === 'POST') {
    admin_guard();
    $server  = in_array(body()['server'] ?? '', ['main', 'monday']) ? body()['server'] : 'main';
    $updated = refresh_map_images($server);
    $maps    = DB::q("SELECT * FROM maps WHERE active=1 AND server=? ORDER BY id DESC", [$server]);
    out(['ok' => true, 'updated' => $updated, 'maps' => $maps]);
}

if ($uri === '/admin/maps/generate' && $method === 'POST') {
    admin_guard();
    $b      = body();
    $server = in_array($b['server'] ?? '', ['main', 'monday']) ? $b['server'] : 'main';
    // Deactivate old maps for this server before generating new ones
    DB::run("UPDATE maps SET active=0 WHERE server=?", [$server]);
    generate_wipe_maps($server, 3);
    out(['ok' => true, 'maps' => DB::q("SELECT * FROM maps WHERE active=1 AND server=? ORDER BY id DESC", [$server])]);
}

if ($uri === '/admin/maps') {
    admin_guard();
    if ($method === 'GET') {
        $server = in_array($_GET['server'] ?? '', ['main', 'monday']) ? $_GET['server'] : null;
        $maps = $server
            ? DB::q("SELECT * FROM maps WHERE active=1 AND server=? ORDER BY id DESC", [$server])
            : DB::q("SELECT * FROM maps ORDER BY id DESC");
        out(['maps' => $maps]);
    }
    if ($method === 'POST') {
        $b  = body();
        $id = !empty($b['id']) ? (int)$b['id'] : uid();
        DB::run(
            "INSERT INTO maps (id,name,seed,size,type,imgUrl,mapUrl,monuments,`desc`,active,server)
             VALUES (?,?,?,?,?,?,?,?,?,?,?)",
            [
                $id, $b['name'] ?? 'Map', $b['seed'] ?? '', (int)($b['size'] ?? 4000),
                $b['type'] ?? 'Procedural_Map', $b['imgUrl'] ?? '', $b['mapUrl'] ?? '',
                (int)($b['monuments'] ?? 0), $b['desc'] ?? '',
                isset($b['active']) ? (int)(bool)$b['active'] : 1,
                $b['server'] ?? 'main',
            ]
        );
        out(['ok' => true, 'id' => $id]);
    }
}

if ($p = route('/admin/maps/:id', $uri)) {
    admin_guard();
    if ($method === 'PUT') {
        $b = body();
        DB::run(
            "UPDATE maps SET name=?,seed=?,size=?,type=?,imgUrl=?,mapUrl=?,monuments=?,`desc`=?,active=?,server=? WHERE id=?",
            [
                $b['name'] ?? 'Map', $b['seed'] ?? '', (int)($b['size'] ?? 4000),
                $b['type'] ?? 'Procedural_Map', $b['imgUrl'] ?? '', $b['mapUrl'] ?? '',
                (int)($b['monuments'] ?? 0), $b['desc'] ?? '',
                isset($b['active']) ? (int)(bool)$b['active'] : 1,
                $b['server'] ?? 'main', $p['id'],
            ]
        );
        out(['ok' => true]);
    }
    if ($method === 'DELETE') {
        DB::run("DELETE FROM maps WHERE id=?", [$p['id']]);
        out(['ok' => true]);
    }
}

// ═════════════════════════════════════════════════════════════
// ADMIN — PLAYERS
// ═════════════════════════════════════════════════════════════

if ($uri === '/admin/players' && $method === 'GET') {
    admin_guard();
    $server = $_GET['server'] ?? '';
    $search = $_GET['search'] ?? '';
    $sql    = "SELECT * FROM players WHERE 1=1";
    $params = [];
    if ($server) { $sql .= " AND server=?"; $params[] = $server; }
    if ($search) { $sql .= " AND (name LIKE ? OR steamId LIKE ?)"; $params[] = "%$search%"; $params[] = "%$search%"; }
    $sql .= " ORDER BY kills DESC LIMIT 500";
    out(DB::q($sql, $params));
}

// ═════════════════════════════════════════════════════════════
// ADMIN — ADMINS MANAGEMENT
// ═════════════════════════════════════════════════════════════

if ($uri === '/admin/admins') {
    admin_guard();
    if ($method === 'GET') {
        $rows = DB::q("SELECT a.*, p.name, p.avatar FROM admins a LEFT JOIN players p ON p.steamId=a.steamId ORDER BY a.addedAt DESC");
        out($rows);
    }
    if ($method === 'POST') {
        $b = body();
        $steamId = trim($b['steamId'] ?? '');
        $role    = trim($b['role'] ?? 'Модератор');
        $addedBy = trim($b['addedBy'] ?? '');
        if (!$steamId) out(['error' => 'steamId required'], 400);
        DB::run("INSERT INTO admins (steamId,role,addedBy) VALUES (?,?,?) ON DUPLICATE KEY UPDATE role=VALUES(role)", [$steamId, $role, $addedBy]);
        out(['ok' => true]);
    }
}

if ($p = route('/admin/admins/:steamId', $uri)) {
    admin_guard();
    if ($method === 'DELETE') {
        DB::run("DELETE FROM admins WHERE steamId=?", [$p['steamId']]);
        out(['ok' => true]);
    }
}

// ═════════════════════════════════════════════════════════════
// ADMIN — WIPE
// ═════════════════════════════════════════════════════════════

if ($uri === '/admin/wipe') {
    admin_guard();
    if ($method === 'GET') {
        out([
            'votes'           => (int)DB::q("SELECT COUNT(*) c FROM votes")[0]['c'],
            'pendingCommands' => (int)DB::q("SELECT COUNT(*) c FROM pending_commands")[0]['c'],
            'orders'          => (int)DB::q("SELECT COUNT(*) c FROM orders")[0]['c'],
        ]);
    }
    if ($method === 'POST') {
        $b = body();
        if ($b['clearVotes']  ?? false) DB::run("DELETE FROM votes");
        if ($b['clearStats']  ?? false) DB::run("DELETE FROM players");
        if ($b['clearOrders'] ?? false) DB::run("DELETE FROM orders");
        // Auto-generate new maps for both servers on wipe
        foreach (['main', 'monday'] as $srv) {
            DB::run("UPDATE maps SET active=0 WHERE server=?", [$srv]);
            generate_wipe_maps($srv, 3);
        }
        out(['ok' => true]);
    }
}

// ═════════════════════════════════════════════════════════════
// 404
// ═════════════════════════════════════════════════════════════
out(['error' => 'Not found'], 404);
