const translations = {
  uk: {
    // Nav
    'nav.home': 'Головна', 'nav.servers': 'Сервери', 'nav.shop': 'Магазин',
    'nav.support': 'Підтримка', 'nav.login': 'Увійти', 'nav.register': 'Реєстрація',
    'nav.news': 'Новини', 'nav.vote': 'Голосування', 'nav.stats': 'Статистика',
    // Hero
    'hero.badge': 'Українська Rust спільнота',
    'hero.title1': 'Воля —', 'hero.title2': 'в кожному бою',
    'hero.sub': 'Тут не виживають — тут <strong>панують</strong>.<br>Обери свій сервер і доведи, що ти — Воля.',
    'hero.cta1': 'Обрати сервер', 'hero.cta2': 'Магазин',
    // Stats
    'stats.online': 'Гравців онлайн', 'stats.servers': 'Активних серверів',
    'stats.registered': 'Зареєстровано', 'stats.goal': 'Місячна ціль', 'stats.topbuyer': 'Топ донатер',
    // Servers
    'servers.tag': 'Список серверів', 'servers.title': 'Наші сервери',
    'servers.sub': 'Обери сервер під свій стиль гри',
    'servers.online': 'Онлайн', 'servers.slots': 'слотів', 'servers.updated': 'Оновлено щойно',
    'wipe': 'Вайп', 'srv.shop': 'Магазин', 'srv.popular': 'Популярний',
    // Login
    'login.title': 'Вхід на сайт',
    'login.sub': 'Для входу використовується ваш акаунт Steam.<br>Ніяких паролів — тільки Steam.',
    'login.btn': 'Увійти через Steam',
    'login.why.title': 'Чому Steam?',
    'login.why.desc': 'Rust — гра Steam. Вхід через Steam гарантує що ти реальний гравець та захищає акаунт.',
    'login.step1': 'Натисни кнопку', 'login.step2': 'Авторизуйся в Steam', 'login.step3': 'Повернись на сайт',
    'login.terms': 'Входячи, ти погоджуєшся з <a href="#">Правилами</a> та <a href="#">Угодою</a>.',
    // News
    'news.title': 'Новини', 'news.sub': 'Оновлення, вайпи, події та анонси',
    'news.read': 'Читати далі', 'news.more': 'Детальніше',
    // Vote
    'vote.title': 'Голосування', 'vote.sub': 'Обери карту для наступного вайпу',
    'vote.ends': 'Голосування закінчується через:',
    'vote.count': 'Проголосувало:', 'vote.players': 'гравців',
    'vote.need_login': 'Потрібно увійти через Steam',
    'vote.btn': 'Голосувати', 'vote.voted': 'Ваш вибір',
    'map1.name': 'Процедурна карта', 'map1.desc': 'Стандартна карта 4000×4000. Збалансована для всіх стилів гри.',
    'map2.name': 'Острівна карта', 'map2.desc': 'Архіпелаг островів. Багато води, морські бої, унікальні POI.',
    'map3.name': 'Пустельна карта', 'map3.desc': 'Суворий клімат, мало ресурсів. Тільки для найсильніших.',
    // Footer
    'footer.desc': 'Українська Rust спільнота. Воля в кожному бою.',
    'footer.nav': 'Навігація', 'footer.legal': 'Правова інфо',
    'footer.rules': 'Правила сервера', 'footer.terms': 'Угода користувача', 'footer.privacy': 'Конфіденційність',
    'footer.copy': '© 2025–2026 Воля Rust. Всі права захищені.',
    // Shop
    'shop.title': 'Магазин', 'shop.sub': 'Привілеї, набори та Воля Койни для твого сервера',
    'shop.promo.label': 'Маєш промокод?', 'shop.promo.placeholder': 'Введи промокод', 'shop.promo.btn': 'Застосувати',
    'shop.sale': '🔥 Акція: <strong>-50% на всі VIP привілеї!</strong> Встигни поки діє знижка.',
    'shop.info': 'Після оплати привілеї активуються автоматично протягом 5–10 хвилин.',
    'shop.tab.vip': 'VIP Привілеї', 'shop.tab.bundles': 'Одноразові набори', 'shop.tab.coins': 'Воля Койни',
    'shop.coins.title': 'Покупка Воля Койнів', 'shop.coins.sub': 'Поповни баланс — використовуй у магазині на сервері',
    'shop.buy': 'Придбати', 'shop.buy.coins': 'Купити',
    'shop.modal.confirm': 'Підтвердження покупки', 'shop.modal.steamid': 'Ваш Steam ID',
    'shop.modal.product': 'Товар', 'shop.modal.server': 'Сервер', 'shop.modal.total': 'Сума',
    'shop.modal.pay': 'Оплатити', 'shop.modal.saved': 'Збережено',
    'shop.modal.hint': 'Знайди в Steam → Обліковий запис → знизу сторінки',
    'vote.login.title': 'Увійдіть через Steam', 'vote.login.btn': 'Увійти через Steam',
    'vote.login.desc': 'Щоб проголосувати, необхідно увійти через Steam.',
    'vote.login.cancel': 'Скасувати',
    'shop.login.title': 'Увійдіть через Steam', 'shop.login.btn': 'Увійти через Steam',
    'shop.login.desc': 'Щоб придбати товар, необхідно увійти через Steam. Це безпечно та займе кілька секунд.',
    'shop.login.cancel': 'Скасувати',
    'shop.pay.title': 'Переказ на банку', 'shop.pay.code.label': 'Код платежу (вкажи у коментарі переказу):',
    'shop.pay.copy': 'Скопіювати', 'shop.pay.open': 'Відкрити Monobank банку',
    'shop.pay.waiting': 'Очікуємо оплату...', 'shop.pay.after': 'Після переказу активація займає до 2 хвилин',
    // Shop product descriptions
    'shop.desc.gold': 'Базові привілеї для гравця — 7 днів',
    'shop.desc.titan': 'Розширені привілеї та доступ до команд — 7 днів',
    'shop.desc.imperial': 'Максимальні привілеї та особливі права — 7 днів',
    'shop.name.tech': 'Тех-набір', 'shop.desc.tech': 'Технічні компоненти та деталі для крафту',
    'shop.name.res': 'Рес-набір', 'shop.desc.res': 'Базові ресурси для швидкого старту',
    'shop.name.farm': 'Фарм-набір', 'shop.desc.farm': 'Інструменти та ресурси для фармінгу',
    'shop.name.coins10': '10 Воля Койнів', 'shop.desc.coins10': 'Поповнення балансу 10 монет',
    'shop.name.coins20': '20 Воля Койнів', 'shop.desc.coins20': 'Поповнення балансу 20 монет',
    'shop.name.coins85': '85 Воля Койнів', 'shop.desc.coins85': 'Поповнення балансу 85 монет',
    // Vote
    'vote.page.title': 'Вибір карти', 'vote.page.sub': 'Обери карту для наступного вайпу. Один голос з одного браузера.',
    'vote.tab.main': 'Main ×2 — Четвер', 'vote.tab.monday': 'Monday ×2 — Понеділок',
    'vote.total.label': 'всього голосів', 'vote.wipe.label': 'Вайп:', 'vote.timer.label': 'До вайпу:',
    'vote.btn.vote': 'Голосувати', 'vote.btn.voted': 'Ваш вибір', 'vote.btn.rustmaps': 'RustMaps',
    'vote.leader': 'Лідер', 'vote.your.vote': 'Ваш голос',
    // Support
    'support.title': 'Підтримка', 'support.sub': 'Створи тікет — ми відповімо якомога швидше',
    'support.discord.title': 'Discord сервер', 'support.discord.sub': 'Найшвидший спосіб отримати допомогу',
    'support.discord.btn': 'Приєднатись до Discord',
    'support.faq.title': 'Часті запитання',
    'support.new.title': 'Новий тікет',
    'support.field.name.label': 'Ваш нікнейм / Steam ID',
    'support.field.name.ph': 'Наприклад: volia_gamer або 7656119...',
    'support.field.category': 'Категорія',
    'support.field.subject.label': 'Тема',
    'support.field.subject.ph': 'Коротко опиши проблему',
    'support.field.desc.label': 'Опис',
    'support.field.desc.ph': 'Детально опиши ситуацію. Вкажи дату, сервер, нікнейм порушника (якщо є)...',
    'support.submit': 'Відправити тікет',
    'support.my.title': 'Мої тікети',
    'support.empty': 'Тут з\'apareться твої тікети',
    'support.cat.ban': 'Апеляція бану', 'support.cat.bug': 'Технічна проблема',
    'support.cat.shop': 'Магазин', 'support.cat.griefer': 'Скарга на гравця', 'support.cat.other': 'Інше',
    'support.status.open': 'Відкрито', 'support.status.answered': 'Відповідь', 'support.status.closed': 'Закрито',
    'support.report.title': 'Поскаржитись на гравця',
    'support.report.target.label': 'Нікнейм порушника *',
    'support.report.reason.label': 'Причина',
    'support.report.desc.label': 'Опис порушення + докази (посилання) *',
    'support.report.desc.ph': 'Опиши детально що сталося. Додай посилання на скріншоти/відео...',
    'support.report.submit': 'Надіслати скаргу',
    'support.reason.cheat': 'Читерство / Хак', 'support.reason.grief': 'Гріфінг',
    'support.reason.abuse': 'Образа / Токсик', 'support.reason.steal': 'Крадіжка / Обман',
    'support.toast.fill': 'Заповни всі поля!', 'support.toast.sent': 'Тікет відправлено!',
    'support.toast.fill.report': 'Заповни нікнейм та опис!',
    'support.toast.report.sent': 'Скаргу надіслано! Адмін розгляне найближчим часом.',
    // Statistics page
    'stats.page.sub': 'Рейтинг найкращих гравців цього вайпу',
    'stats.wipe.main.day': 'Вайп кожного четверга (глобальний)',
    'stats.wipe.monday.day': 'Вайп кожного понеділка',
    'stats.wipe.map.title': 'Поточна карта',
    'stats.wipe.next': 'Наступний:',
    'stats.cat.players': 'Гравці',
    'stats.cat.clans': 'Клани',
    'stats.search': 'Пошук гравця...',
    'stats.sort.kills': 'Вбивства',
    'stats.sort.deaths': 'Смерті',
    'stats.sort.playtime': 'Час гри',
    'stats.sort.raids': 'Рейди',
    'stats.sort.gathered': 'Зібрано ресурсів',
    'stats.col.player': 'Гравець',
    'stats.col.time': 'Час',
    'stats.col.resources': 'Ресурси',
    'stats.load.error': 'Не вдалося завантажити статистику',
    'stats.empty.search': 'Гравців з таким ніком не знайдено',
    'stats.empty.noplayers': 'Статистика ще збирається. Дані з\'являться після того як гравці пограють на сервері.',
    // Common
    'common.loading': 'Завантаження...', 'common.signin': 'Увійти',
    // Statistics live bar
    'stats.live.online': 'Онлайн зараз', 'stats.live.servers': 'Активних серверів',
    'stats.live.kills': 'Вбивств за вайп', 'stats.live.raids': 'Рейдів за вайп',
    // Vote
    'vote.loading': 'Завантаження...',
    // News
    'news.loading': 'Завантаження новин...', 'news.readmore': 'Читати далі',
    // Profile
    'profile.loading': 'Завантаження...', 'profile.tab.overview': 'Огляд',
    'profile.tab.tickets': 'Тікети', 'profile.logout': 'Вийти',
    'profile.stat.vip': 'Активний VIP', 'profile.stat.tickets': 'Тікетів відкрито',
    'profile.stat.days': 'Днів на сайті',
    'profile.no.vip': 'У тебе немає активних VIP привілеїв',
    'profile.no.tickets': 'Тікетів поки немає',
    // Toast
    'toast.copied': 'IP скопійовано!',
  },
  en: {
    // Nav
    'nav.home': 'Home', 'nav.servers': 'Servers', 'nav.shop': 'Shop',
    'nav.support': 'Support', 'nav.login': 'Sign In', 'nav.register': 'Register',
    'nav.news': 'News', 'nav.vote': 'Map Vote', 'nav.stats': 'Statistics',
    // Hero
    'hero.badge': 'Ukrainian Rust Community',
    'hero.title1': 'Volia —', 'hero.title2': 'in every battle',
    'hero.sub': "Don't just survive — <strong>dominate</strong>.<br>Choose your server and prove you are Volia.",
    'hero.cta1': 'Choose Server', 'hero.cta2': 'Shop',
    // Stats
    'stats.online': 'Players online', 'stats.servers': 'Active servers',
    'stats.registered': 'Registered', 'stats.goal': 'Monthly goal', 'stats.topbuyer': 'Top donor',
    // Servers
    'servers.tag': 'Server list', 'servers.title': 'Our Servers',
    'servers.sub': 'Pick a server that fits your playstyle',
    'servers.online': 'Online', 'servers.slots': 'slots', 'servers.updated': 'Just updated',
    'wipe': 'Wipe', 'srv.shop': 'Shop', 'srv.popular': 'Popular',
    // Login
    'login.title': 'Sign In',
    'login.sub': 'Use your Steam account to sign in.<br>No passwords — Steam only.',
    'login.btn': 'Sign in through Steam',
    'login.why.title': 'Why Steam?',
    'login.why.desc': 'Rust is a Steam game. Steam login ensures you are a real player and keeps your account secure.',
    'login.step1': 'Click the button', 'login.step2': 'Authorize in Steam', 'login.step3': 'Return to site',
    'login.terms': 'By signing in, you agree to our <a href="#">Rules</a> and <a href="#">Terms</a>.',
    // News
    'news.title': 'News', 'news.sub': 'Updates, wipes, events and announcements',
    'news.read': 'Read more', 'news.more': 'Details',
    // Vote
    'vote.title': 'Map Vote', 'vote.sub': 'Choose the map for the next wipe',
    'vote.ends': 'Voting ends in:',
    'vote.count': 'Voted:', 'vote.players': 'players',
    'vote.need_login': 'Sign in with Steam required',
    'vote.btn': 'Vote', 'vote.voted': 'Your choice',
    'map1.name': 'Procedural Map', 'map1.desc': 'Standard 4000×4000 map. Balanced for all playstyles.',
    'map2.name': 'Island Map', 'map2.desc': 'Archipelago of islands. Lots of water, sea battles, unique POIs.',
    'map3.name': 'Desert Map', 'map3.desc': 'Harsh climate, scarce resources. Only for the strongest.',
    // Footer
    'footer.desc': 'Ukrainian Rust community. Volia in every battle.',
    'footer.nav': 'Navigation', 'footer.legal': 'Legal',
    'footer.rules': 'Server Rules', 'footer.terms': 'Terms of Service', 'footer.privacy': 'Privacy Policy',
    'footer.copy': '© 2025–2026 Volia Rust. All rights reserved.',
    // Shop
    'shop.title': 'Shop', 'shop.sub': 'VIP privileges, kits and Volia Coins for your server',
    'shop.promo.label': 'Have a promo code?', 'shop.promo.placeholder': 'Enter promo code', 'shop.promo.btn': 'Apply',
    'shop.sale': '🔥 Sale: <strong>-50% on all VIP privileges!</strong> Limited time offer.',
    'shop.info': 'After payment, privileges are activated automatically within 5–10 minutes.',
    'shop.tab.vip': 'VIP Privileges', 'shop.tab.bundles': 'One-time Kits', 'shop.tab.coins': 'Volia Coins',
    'shop.coins.title': 'Buy Volia Coins', 'shop.coins.sub': 'Top up your balance — spend it in the in-game shop',
    'shop.buy': 'Buy', 'shop.buy.coins': 'Buy',
    'shop.modal.confirm': 'Confirm purchase', 'shop.modal.steamid': 'Your Steam ID',
    'shop.modal.product': 'Item', 'shop.modal.server': 'Server', 'shop.modal.total': 'Total',
    'shop.modal.pay': 'Pay', 'shop.modal.saved': 'Saved',
    'shop.modal.hint': 'Find it in Steam → Account Details → bottom of page',
    'vote.login.title': 'Sign in via Steam', 'vote.login.btn': 'Sign in via Steam',
    'vote.login.desc': 'To vote, please sign in via Steam.',
    'vote.login.cancel': 'Cancel',
    'shop.login.title': 'Sign in via Steam', 'shop.login.btn': 'Sign in via Steam',
    'shop.login.desc': 'To purchase, please sign in via Steam. It\'s safe and takes just a few seconds.',
    'shop.login.cancel': 'Cancel',
    'shop.pay.title': 'Transfer to jar', 'shop.pay.code.label': 'Payment code (put it in transfer comment):',
    'shop.pay.copy': 'Copy', 'shop.pay.open': 'Open Monobank jar',
    'shop.pay.waiting': 'Waiting for payment...', 'shop.pay.after': 'Activation takes up to 2 minutes after transfer',
    // Shop product descriptions
    'shop.desc.gold': 'Basic player privileges — 7 days',
    'shop.desc.titan': 'Extended privileges and command access — 7 days',
    'shop.desc.imperial': 'Maximum privileges and special rights — 7 days',
    'shop.name.tech': 'Tech Kit', 'shop.desc.tech': 'Technical components and parts for crafting',
    'shop.name.res': 'Resource Kit', 'shop.desc.res': 'Basic resources for a fast start',
    'shop.name.farm': 'Farm Kit', 'shop.desc.farm': 'Tools and resources for farming',
    'shop.name.coins10': '10 Volia Coins', 'shop.desc.coins10': 'Top up balance by 10 coins',
    'shop.name.coins20': '20 Volia Coins', 'shop.desc.coins20': 'Top up balance by 20 coins',
    'shop.name.coins85': '85 Volia Coins', 'shop.desc.coins85': 'Top up balance by 85 coins',
    // Vote
    'vote.page.title': 'Map Vote', 'vote.page.sub': 'Choose the map for the next wipe. One vote per browser.',
    'vote.tab.main': 'Main ×2 — Thursday', 'vote.tab.monday': 'Monday ×2 — Monday',
    'vote.total.label': 'total votes', 'vote.wipe.label': 'Wipe:', 'vote.timer.label': 'Until wipe:',
    'vote.btn.vote': 'Vote', 'vote.btn.voted': 'Your choice', 'vote.btn.rustmaps': 'RustMaps',
    'vote.leader': 'Leader', 'vote.your.vote': 'Your vote',
    // Support
    'support.title': 'Support', 'support.sub': 'Create a ticket — we\'ll reply as soon as possible',
    'support.discord.title': 'Discord Server', 'support.discord.sub': 'The fastest way to get help',
    'support.discord.btn': 'Join Discord',
    'support.faq.title': 'FAQ',
    'support.new.title': 'New Ticket',
    'support.field.name.label': 'Your nickname / Steam ID',
    'support.field.name.ph': 'e.g. volia_gamer or 7656119...',
    'support.field.category': 'Category',
    'support.field.subject.label': 'Subject',
    'support.field.subject.ph': 'Briefly describe the issue',
    'support.field.desc.label': 'Description',
    'support.field.desc.ph': 'Describe the situation in detail. Include date, server, offender nickname (if any)...',
    'support.submit': 'Submit Ticket',
    'support.my.title': 'My Tickets',
    'support.empty': 'Your tickets will appear here',
    'support.cat.ban': 'Ban Appeal', 'support.cat.bug': 'Technical Issue',
    'support.cat.shop': 'Shop', 'support.cat.griefer': 'Player Report', 'support.cat.other': 'Other',
    'support.status.open': 'Open', 'support.status.answered': 'Answered', 'support.status.closed': 'Closed',
    'support.report.title': 'Report a Player',
    'support.report.target.label': 'Offender\'s nickname *',
    'support.report.reason.label': 'Reason',
    'support.report.desc.label': 'Description + evidence (links) *',
    'support.report.desc.ph': 'Describe in detail what happened. Add links to screenshots/video...',
    'support.report.submit': 'Send Report',
    'support.reason.cheat': 'Cheating / Hack', 'support.reason.grief': 'Griefing',
    'support.reason.abuse': 'Abuse / Toxic', 'support.reason.steal': 'Theft / Scam',
    'support.toast.fill': 'Fill in all fields!', 'support.toast.sent': 'Ticket submitted!',
    'support.toast.fill.report': 'Fill in nickname and description!',
    'support.toast.report.sent': 'Report sent! Admin will review it soon.',
    // Statistics page
    'stats.page.sub': 'Top players leaderboard for this wipe',
    'stats.wipe.main.day': 'Wipe every Thursday (global)',
    'stats.wipe.monday.day': 'Wipe every Monday',
    'stats.wipe.map.title': 'Current Map',
    'stats.wipe.next': 'Next:',
    'stats.cat.players': 'Players',
    'stats.cat.clans': 'Clans',
    'stats.search': 'Search player...',
    'stats.sort.kills': 'Kills',
    'stats.sort.deaths': 'Deaths',
    'stats.sort.playtime': 'Playtime',
    'stats.sort.raids': 'Raids',
    'stats.sort.gathered': 'Gathered',
    'stats.col.player': 'Player',
    'stats.col.time': 'Time',
    'stats.col.resources': 'Resources',
    'stats.load.error': 'Failed to load statistics',
    'stats.empty.search': 'No players found with that name',
    'stats.empty.noplayers': 'Stats are still being collected. Data will appear after players spend time on the server.',
    // Common
    'common.loading': 'Loading...', 'common.signin': 'Sign In',
    // Statistics live bar
    'stats.live.online': 'Online now', 'stats.live.servers': 'Active servers',
    'stats.live.kills': 'Kills this wipe', 'stats.live.raids': 'Raids this wipe',
    // Vote
    'vote.loading': 'Loading...',
    // News
    'news.loading': 'Loading news...', 'news.readmore': 'Read more',
    // Profile
    'profile.loading': 'Loading...', 'profile.tab.overview': 'Overview',
    'profile.tab.tickets': 'Tickets', 'profile.logout': 'Sign Out',
    'profile.stat.vip': 'Active VIP', 'profile.stat.tickets': 'Open Tickets',
    'profile.stat.days': 'Days on site',
    'profile.no.vip': 'You have no active VIP privileges',
    'profile.no.tickets': 'No tickets yet',
    // Toast
    'toast.copied': 'IP copied!',
  },
  ru: {
    // Nav
    'nav.home': 'Главная', 'nav.servers': 'Серверы', 'nav.shop': 'Магазин',
    'nav.support': 'Поддержка', 'nav.login': 'Войти', 'nav.register': 'Регистрация',
    'nav.news': 'Новости', 'nav.vote': 'Голосование', 'nav.stats': 'Статистика',
    // Hero
    'hero.badge': 'Украинское Rust сообщество',
    'hero.title1': 'Воля —', 'hero.title2': 'в каждом бою',
    'hero.sub': 'Здесь не выживают — здесь <strong>властвуют</strong>.<br>Выбери свой сервер и докажи, что ты — Воля.',
    'hero.cta1': 'Выбрать сервер', 'hero.cta2': 'Магазин',
    // Stats
    'stats.online': 'Игроков онлайн', 'stats.servers': 'Активных серверов',
    'stats.registered': 'Зарегистрировано', 'stats.goal': 'Месячная цель', 'stats.topbuyer': 'Топ донатер',
    // Servers
    'servers.tag': 'Список серверов', 'servers.title': 'Наши серверы',
    'servers.sub': 'Выбери сервер под свой стиль игры',
    'servers.online': 'Онлайн', 'servers.slots': 'слотов', 'servers.updated': 'Обновлено только что',
    'wipe': 'Вайп', 'srv.shop': 'Магазин', 'srv.popular': 'Популярный',
    // Login
    'login.title': 'Вход на сайт',
    'login.sub': 'Для входа используется ваш аккаунт Steam.<br>Никаких паролей — только Steam.',
    'login.btn': 'Войти через Steam',
    'login.why.title': 'Почему Steam?',
    'login.why.desc': 'Rust — игра Steam. Вход через Steam гарантирует что ты реальный игрок и защищает аккаунт.',
    'login.step1': 'Нажми кнопку', 'login.step2': 'Авторизуйся в Steam', 'login.step3': 'Вернись на сайт',
    'login.terms': 'Входя, ты соглашаешься с <a href="#">Правилами</a> и <a href="#">Соглашением</a>.',
    // News
    'news.title': 'Новости', 'news.sub': 'Обновления, вайпы, события и анонсы',
    'news.read': 'Читать далее', 'news.more': 'Подробнее',
    // Vote
    'vote.title': 'Голосование', 'vote.sub': 'Выбери карту для следующего вайпа',
    'vote.ends': 'Голосование заканчивается через:',
    'vote.count': 'Проголосовало:', 'vote.players': 'игроков',
    'vote.need_login': 'Необходимо войти через Steam',
    'vote.btn': 'Голосовать', 'vote.voted': 'Ваш выбор',
    'map1.name': 'Процедурная карта', 'map1.desc': 'Стандартная карта 4000×4000. Сбалансирована для всех стилей игры.',
    'map2.name': 'Островная карта', 'map2.desc': 'Архипелаг островов. Много воды, морские бои, уникальные POI.',
    'map3.name': 'Пустынная карта', 'map3.desc': 'Суровый климат, мало ресурсов. Только для сильнейших.',
    // Footer
    'footer.desc': 'Украинское Rust сообщество. Воля в каждом бою.',
    'footer.nav': 'Навигация', 'footer.legal': 'Правовая инфо',
    'footer.rules': 'Правила сервера', 'footer.terms': 'Пользовательское соглашение', 'footer.privacy': 'Конфиденциальность',
    'footer.copy': '© 2025–2026 Воля Rust. Все права защищены.',
    // Shop
    'shop.title': 'Магазин', 'shop.sub': 'Привилегии, наборы и Воля Коины для твоего сервера',
    'shop.promo.label': 'Есть промокод?', 'shop.promo.placeholder': 'Введи промокод', 'shop.promo.btn': 'Применить',
    'shop.sale': '🔥 Акция: <strong>-50% на все VIP привилегии!</strong> Успей пока действует скидка.',
    'shop.info': 'После оплаты привилегии активируются автоматически в течение 5–10 минут.',
    'shop.tab.vip': 'VIP Привилегии', 'shop.tab.bundles': 'Разовые наборы', 'shop.tab.coins': 'Воля Коины',
    'shop.coins.title': 'Покупка Воля Коинов', 'shop.coins.sub': 'Пополни баланс — используй в магазине на сервере',
    'shop.buy': 'Купить', 'shop.buy.coins': 'Купить',
    'shop.modal.confirm': 'Подтверждение покупки', 'shop.modal.steamid': 'Ваш Steam ID',
    'shop.modal.product': 'Товар', 'shop.modal.server': 'Сервер', 'shop.modal.total': 'Сумма',
    'shop.modal.pay': 'Оплатить', 'shop.modal.saved': 'Сохранено',
    'shop.modal.hint': 'Найди в Steam → Аккаунт → внизу страницы',
    'vote.login.title': 'Войдите через Steam', 'vote.login.btn': 'Войти через Steam',
    'vote.login.desc': 'Чтобы проголосовать, необходимо войти через Steam.',
    'vote.login.cancel': 'Отмена',
    'shop.login.title': 'Войдите через Steam', 'shop.login.btn': 'Войти через Steam',
    'shop.login.desc': 'Для покупки необходимо войти через Steam. Это безопасно и займёт несколько секунд.',
    'shop.login.cancel': 'Отмена',
    'shop.pay.title': 'Перевод на банку', 'shop.pay.code.label': 'Код платежа (укажи в комментарии перевода):',
    'shop.pay.copy': 'Скопировать', 'shop.pay.open': 'Открыть Monobank банку',
    'shop.pay.waiting': 'Ожидаем оплату...', 'shop.pay.after': 'После перевода активация занимает до 2 минут',
    // Shop product descriptions
    'shop.desc.gold': 'Базовые привилегии для игрока — 7 дней',
    'shop.desc.titan': 'Расширенные привилегии и доступ к командам — 7 дней',
    'shop.desc.imperial': 'Максимальные привилегии и особые права — 7 дней',
    'shop.name.tech': 'Тех-набор', 'shop.desc.tech': 'Технические компоненты и детали для крафта',
    'shop.name.res': 'Рес-набор', 'shop.desc.res': 'Базовые ресурсы для быстрого старта',
    'shop.name.farm': 'Фарм-набор', 'shop.desc.farm': 'Инструменты и ресурсы для фарминга',
    'shop.name.coins10': '10 Воля Коинов', 'shop.desc.coins10': 'Пополнение баланса 10 монет',
    'shop.name.coins20': '20 Воля Коинов', 'shop.desc.coins20': 'Пополнение баланса 20 монет',
    'shop.name.coins85': '85 Воля Коинов', 'shop.desc.coins85': 'Пополнение баланса 85 монет',
    // Vote
    'vote.page.title': 'Выбор карты', 'vote.page.sub': 'Выбери карту для следующего вайпа. Один голос с одного браузера.',
    'vote.tab.main': 'Main ×2 — Четверг', 'vote.tab.monday': 'Monday ×2 — Понедельник',
    'vote.total.label': 'всего голосов', 'vote.wipe.label': 'Вайп:', 'vote.timer.label': 'До вайпа:',
    'vote.btn.vote': 'Голосовать', 'vote.btn.voted': 'Ваш выбор', 'vote.btn.rustmaps': 'RustMaps',
    'vote.leader': 'Лидер', 'vote.your.vote': 'Ваш голос',
    // Support
    'support.title': 'Поддержка', 'support.sub': 'Создай тикет — мы ответим как можно скорее',
    'support.discord.title': 'Discord сервер', 'support.discord.sub': 'Самый быстрый способ получить помощь',
    'support.discord.btn': 'Присоединиться к Discord',
    'support.faq.title': 'Частые вопросы',
    'support.new.title': 'Новый тикет',
    'support.field.name.label': 'Ваш никнейм / Steam ID',
    'support.field.name.ph': 'Например: volia_gamer или 7656119...',
    'support.field.category': 'Категория',
    'support.field.subject.label': 'Тема',
    'support.field.subject.ph': 'Кратко опиши проблему',
    'support.field.desc.label': 'Описание',
    'support.field.desc.ph': 'Детально опиши ситуацию. Укажи дату, сервер, никнейм нарушителя (если есть)...',
    'support.submit': 'Отправить тикет',
    'support.my.title': 'Мои тикеты',
    'support.empty': 'Здесь появятся твои тикеты',
    'support.cat.ban': 'Апелляция бана', 'support.cat.bug': 'Техническая проблема',
    'support.cat.shop': 'Магазин', 'support.cat.griefer': 'Жалоба на игрока', 'support.cat.other': 'Другое',
    'support.status.open': 'Открыто', 'support.status.answered': 'Ответ', 'support.status.closed': 'Закрыто',
    'support.report.title': 'Пожаловаться на игрока',
    'support.report.target.label': 'Никнейм нарушителя *',
    'support.report.reason.label': 'Причина',
    'support.report.desc.label': 'Описание нарушения + доказательства (ссылки) *',
    'support.report.desc.ph': 'Опиши подробно что произошло. Добавь ссылки на скриншоты/видео...',
    'support.report.submit': 'Отправить жалобу',
    'support.reason.cheat': 'Читерство / Хак', 'support.reason.grief': 'Грифинг',
    'support.reason.abuse': 'Оскорбление / Токсик', 'support.reason.steal': 'Кража / Обман',
    'support.toast.fill': 'Заполни все поля!', 'support.toast.sent': 'Тикет отправлен!',
    'support.toast.fill.report': 'Заполни никнейм и описание!',
    'support.toast.report.sent': 'Жалоба отправлена! Админ рассмотрит в ближайшее время.',
    // Statistics page
    'stats.page.sub': 'Рейтинг лучших игроков этого вайпа',
    'stats.wipe.main.day': 'Вайп каждый четверг (глобальный)',
    'stats.wipe.monday.day': 'Вайп каждый понедельник',
    'stats.wipe.map.title': 'Текущая карта',
    'stats.wipe.next': 'Следующий:',
    'stats.cat.players': 'Игроки',
    'stats.cat.clans': 'Кланы',
    'stats.search': 'Поиск игрока...',
    'stats.sort.kills': 'Убийства',
    'stats.sort.deaths': 'Смерти',
    'stats.sort.playtime': 'Время игры',
    'stats.sort.raids': 'Рейды',
    'stats.sort.gathered': 'Собрано ресурсов',
    'stats.col.player': 'Игрок',
    'stats.col.time': 'Время',
    'stats.col.resources': 'Ресурсы',
    'stats.load.error': 'Не удалось загрузить статистику',
    'stats.empty.search': 'Игроки с таким ником не найдены',
    'stats.empty.noplayers': 'Статистика ещё собирается. Данные появятся после того как игроки поиграют на сервере.',
    // Common
    'common.loading': 'Загрузка...', 'common.signin': 'Войти',
    // Statistics live bar
    'stats.live.online': 'Онлайн сейчас', 'stats.live.servers': 'Активных серверов',
    'stats.live.kills': 'Убийств за вайп', 'stats.live.raids': 'Рейдов за вайп',
    // Vote
    'vote.loading': 'Загрузка...',
    // News
    'news.loading': 'Загрузка новостей...', 'news.readmore': 'Читать далее',
    // Profile
    'profile.loading': 'Загрузка...', 'profile.tab.overview': 'Обзор',
    'profile.tab.tickets': 'Тикеты', 'profile.logout': 'Выйти',
    'profile.stat.vip': 'Активный VIP', 'profile.stat.tickets': 'Открытых тикетов',
    'profile.stat.days': 'Дней на сайте',
    'profile.no.vip': 'У тебя нет активных VIP привилегий',
    'profile.no.tickets': 'Тикетов пока нет',
    // Toast
    'toast.copied': 'IP скопирован!',
  }
};

let currentLang = localStorage.getItem('lang') || 'uk';

function applyLang(lang) {
  currentLang = lang;
  localStorage.setItem('lang', lang);
  const t = translations[lang];
  document.querySelectorAll('[data-i18n]').forEach(el => {
    const key = el.dataset.i18n;
    if (t[key] !== undefined) el.innerHTML = t[key];
  });
  document.querySelectorAll('[data-i18n-placeholder]').forEach(el => {
    const key = el.dataset.i18nPlaceholder;
    if (t[key] !== undefined) el.placeholder = t[key];
  });
  const flagEl = document.getElementById('langFlag');
  const labelEl = document.getElementById('langLabel');
  const langLabel = lang === 'en' ? 'EN' : lang === 'ru' ? 'RU' : 'UA';
  if (flagEl) flagEl.textContent = langLabel;
  if (labelEl) labelEl.textContent = langLabel;
  document.documentElement.lang = lang;
  document.querySelectorAll('.lang-option').forEach(btn => {
    btn.classList.toggle('active', btn.dataset.lang === lang);
  });
}

function toggleLangMenu() {
  const menu = document.getElementById('langMenu');
  const arrow = document.getElementById('langArrow');
  const btn = document.querySelector('.lang-switch');
  if (!menu) return;
  const isOpen = menu.classList.contains('open');
  menu.classList.toggle('open', !isOpen);
  if (arrow) arrow.classList.toggle('rotated', !isOpen);
  if (btn) btn.classList.toggle('open', !isOpen);
}

function selectLang(lang) {
  applyLang(lang);
  const menu = document.getElementById('langMenu');
  const arrow = document.getElementById('langArrow');
  const btn = document.querySelector('.lang-switch');
  if (menu) menu.classList.remove('open');
  if (arrow) arrow.classList.remove('rotated');
  if (btn) btn.classList.remove('open');
}

document.addEventListener('click', e => {
  const dropdown = document.getElementById('langDropdown');
  if (dropdown && !dropdown.contains(e.target)) {
    const menu = document.getElementById('langMenu');
    const arrow = document.getElementById('langArrow');
    const btn = document.querySelector('.lang-switch');
    if (menu) menu.classList.remove('open');
    if (arrow) arrow.classList.remove('rotated');
    if (btn) btn.classList.remove('open');
  }
});

applyLang(currentLang);
