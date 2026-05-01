<?php
require_once __DIR__ . '/config.php';

class DB {
    private static ?PDO $pdo = null;

    public static function connect(): PDO {
        if (!self::$pdo) {
            self::$pdo = new PDO(
                sprintf('mysql:host=%s;dbname=%s;charset=utf8mb4', DB_HOST, DB_NAME),
                DB_USER, DB_PASS,
                [
                    PDO::ATTR_ERRMODE            => PDO::ERRMODE_EXCEPTION,
                    PDO::ATTR_DEFAULT_FETCH_MODE => PDO::FETCH_ASSOC,
                    PDO::ATTR_EMULATE_PREPARES   => false,
                ]
            );
        }
        return self::$pdo;
    }

    public static function q(string $sql, array $p = []): array {
        $s = self::connect()->prepare($sql);
        $s->execute($p);
        return $s->fetchAll();
    }

    public static function run(string $sql, array $p = []): int {
        $s = self::connect()->prepare($sql);
        $s->execute($p);
        return $s->rowCount();
    }

    public static function insertId(): string {
        return self::connect()->lastInsertId();
    }

    public static function init(): void {
        $db = self::connect();
        $db->exec("CREATE TABLE IF NOT EXISTS maps (
            id        BIGINT PRIMARY KEY,
            name      VARCHAR(255) NOT NULL,
            seed      VARCHAR(100) DEFAULT '',
            size      INT DEFAULT 4000,
            type      VARCHAR(50)  DEFAULT 'Procedural_Map',
            imgUrl    TEXT,
            mapUrl    TEXT,
            monuments INT DEFAULT 0,
            `desc`    TEXT,
            active    TINYINT(1)   DEFAULT 1,
            server    VARCHAR(50)  DEFAULT 'main'
        )");
        $db->exec("CREATE TABLE IF NOT EXISTS votes (
            id       INT AUTO_INCREMENT PRIMARY KEY,
            mapId    BIGINT,
            steamId  VARCHAR(50),
            name     VARCHAR(255),
            wipeId   VARCHAR(100),
            at       DATETIME DEFAULT CURRENT_TIMESTAMP,
            UNIQUE KEY uq_vote (wipeId, steamId)
        )");
        $db->exec("CREATE TABLE IF NOT EXISTS orders (
            id           BIGINT PRIMARY KEY,
            confirmCode  VARCHAR(30) UNIQUE,
            steamId      VARCHAR(50),
            playerName   VARCHAR(255),
            productId    INT DEFAULT 0,
            productName  VARCHAR(255),
            price        DECIMAL(10,2),
            server       VARCHAR(50)  DEFAULT 'main',
            status       VARCHAR(20)  DEFAULT 'pending',
            createdAt    DATETIME     DEFAULT CURRENT_TIMESTAMP,
            completedAt  DATETIME     NULL
        )");
        $db->exec("CREATE TABLE IF NOT EXISTS players (
            steamId   VARCHAR(50) PRIMARY KEY,
            name      VARCHAR(255),
            avatar    TEXT,
            server    VARCHAR(50) DEFAULT 'main',
            kills     INT DEFAULT 0,
            deaths    INT DEFAULT 0,
            raids     INT DEFAULT 0,
            playtime  INT DEFAULT 0,
            gathered  INT DEFAULT 0,
            updatedAt DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
        )");
        $db->exec("CREATE TABLE IF NOT EXISTS pending_commands (
            id          BIGINT PRIMARY KEY,
            steamId     VARCHAR(50),
            playerName  VARCHAR(255),
            command     TEXT,
            orderId     BIGINT,
            server      VARCHAR(50) DEFAULT 'main',
            createdAt   DATETIME DEFAULT CURRENT_TIMESTAMP
        )");
        $db->exec("CREATE TABLE IF NOT EXISTS admins (
            steamId   VARCHAR(50) PRIMARY KEY,
            role      VARCHAR(50)  DEFAULT 'Модератор',
            addedBy   VARCHAR(50)  DEFAULT '',
            addedAt   DATETIME     DEFAULT CURRENT_TIMESTAMP
        )");
    }
}
