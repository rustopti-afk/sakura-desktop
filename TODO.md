# Sakura Desktop — TODO / Task Tracker

## ✅ Реалізовано

- [x] Personalize page — DWM colors, taskbar, animations, fonts, quick-apply buttons
- [x] Profiles page — load/scan/apply/export/delete, drag-drop .json, search filter
- [x] Profile Editor — full form, save/export, live dirty tracking
- [x] Terminal page — color scheme + Oh My Posh deploy
- [x] Integrations page — Rainmeter, Windhawk, Lively, Icons, HackBGRT status + controls
- [x] Backup page — list, restore, undo last, open folder, delete
- [x] Settings page — language, paths, version, about
- [x] Navigation — 12 items, fade-in animation, click sound, descriptions
- [x] Toast notifications — ToastService overlay
- [x] Update checker — GitHub Releases API
- [x] Profile Preview panel — right-side live preview
- [x] ViewModel safety — ParseHex null guard, BackupViewModel sort by time, ToggleMod null check

## 🚧 В роботі / Допиши

- [x] **Theme Engine page** — SecureUxTheme, msstyles browse + apply/revert
- [x] **Wallpaper page** — Windows wallpaper (image browse + fit) + Lively toggle
- [x] **Window Manager page** — Komorebi / GlazeWM: engine select, gaps, border colors, start/stop
- [x] **Icons & Cursors page** — icon pack folder, cursor pack, restore defaults
- [x] **Boot Splash page** — HackBGRT: splash image browse, deploy, install/uninstall

## 🐛 Відомі проблеми

- [ ] QuickApply-кнопки на Personalize (`await Task.CompletedTask`) — функціонально
       правильно, але краще зробити sync методами (не критично)
- [ ] BackupPage: `DeleteSelected` не запитує підтвердження перед видаленням
- [ ] ProfilesPage: `RemoveSelected` не видаляє файл з диску (лише з пам'яті) — by design?

## 🔮 Майбутнє

- [ ] Live DWM color preview (color picker → instant update в реальному часі)
- [ ] Multi-monitor wallpaper UI (per-monitor drag-drop)
- [ ] Profile sharing / import з URL
- [ ] Auto-backup перед кожним Apply
- [ ] Komorebi workspace editor (graphical rule builder)
- [ ] SecureUxTheme auto-install (winget)
