-- =====================================================================
-- ИС «Магазин электроники»: структура и тестовые данные
-- Перед запуском создайте пустую базу electronics_store
-- (Collation — Cyrillic_General_CI_AS) и выберите её в SSMS.
-- =====================================================================
USE electronics_store;
GO

DROP TABLE IF EXISTS order_items;
DROP TABLE IF EXISTS orders;
DROP TABLE IF EXISTS product_variants;
DROP TABLE IF EXISTS products;
DROP TABLE IF EXISTS departments;
DROP TABLE IF EXISTS categories;
DROP TABLE IF EXISTS users;
DROP TABLE IF EXISTS roles;
GO

-- ---------------------------------------------------------------------
-- В.1 Создание таблиц
-- ---------------------------------------------------------------------
CREATE TABLE roles (
    role_id   int IDENTITY(1, 1),
    role_name nvarchar(50) NOT NULL,
    CONSTRAINT pk_roles PRIMARY KEY (role_id),
    CONSTRAINT uq_roles_name UNIQUE (role_name)
);

CREATE TABLE users (
    user_id     int IDENTITY(1, 1),
    login       nvarchar(50) NOT NULL,
    password    nvarchar(255) NOT NULL,
    last_name   nvarchar(60) NOT NULL,
    first_name  nvarchar(60) NOT NULL,
    middle_name nvarchar(60) NULL,
    role_id     int NOT NULL,
    CONSTRAINT pk_users PRIMARY KEY (user_id),
    CONSTRAINT uq_users_login UNIQUE (login),
    CONSTRAINT fk_users_role FOREIGN KEY (role_id)
        REFERENCES roles (role_id)
);

CREATE TABLE categories (
    category_id   int IDENTITY(1, 1),
    category_name nvarchar(100) NOT NULL,
    CONSTRAINT pk_categories PRIMARY KEY (category_id),
    CONSTRAINT uq_categories_name UNIQUE (category_name)
);

CREATE TABLE departments (
    department_id   int IDENTITY(1, 1),
    department_name nvarchar(120) NOT NULL,
    CONSTRAINT pk_departments PRIMARY KEY (department_id),
    CONSTRAINT uq_departments_name UNIQUE (department_name)
);

CREATE TABLE products (
    product_id      int IDENTITY(1, 1),
    product_code    nvarchar(20)  NOT NULL,
    category_id     int NOT NULL,
    department_id   int NOT NULL,
    product_name    nvarchar(150) NOT NULL,
    product_content nvarchar(300) NULL,
    description     nvarchar(max) NULL,
    image_path      nvarchar(150) NULL,
    base_price      decimal(10, 2) NOT NULL,
    CONSTRAINT pk_products PRIMARY KEY (product_id),
    CONSTRAINT uq_products_code UNIQUE (product_code),
    CONSTRAINT chk_products_price CHECK (base_price >= 0),
    CONSTRAINT fk_products_category FOREIGN KEY (category_id)
        REFERENCES categories (category_id),
    CONSTRAINT fk_products_department FOREIGN KEY (department_id)
        REFERENCES departments (department_id)
);

CREATE TABLE product_variants (
    batch_id        int IDENTITY(1, 1),
    product_id      int NOT NULL,
    variant_name    nvarchar(30) NOT NULL,
    available_stock int NOT NULL CONSTRAINT df_variants_stock DEFAULT (0),
    CONSTRAINT pk_product_variants PRIMARY KEY (batch_id),
    CONSTRAINT uq_product_variant UNIQUE (product_id, variant_name),
    CONSTRAINT chk_variants_stock CHECK (available_stock >= 0),
    CONSTRAINT fk_variants_product FOREIGN KEY (product_id)
        REFERENCES products (product_id) ON DELETE CASCADE
);

CREATE TABLE orders (
    order_id   int IDENTITY(1, 1),
    order_date date NOT NULL CONSTRAINT df_orders_date
               DEFAULT (CAST(GETDATE() AS date)),
    client_id  int NOT NULL,
    CONSTRAINT pk_orders PRIMARY KEY (order_id),
    CONSTRAINT fk_orders_client FOREIGN KEY (client_id)
        REFERENCES users (user_id)
);

CREATE TABLE order_items (
    order_item_id int IDENTITY(1, 1),
    order_id      int NOT NULL,
    batch_id      int NOT NULL,
    quantity      int NOT NULL,
    unit_price    decimal(10, 2) NOT NULL,
    CONSTRAINT pk_order_items PRIMARY KEY (order_item_id),
    CONSTRAINT uq_order_item UNIQUE (order_id, batch_id),
    CONSTRAINT chk_items_quantity CHECK (quantity > 0),
    CONSTRAINT chk_items_price CHECK (unit_price >= 0),
    CONSTRAINT fk_items_order FOREIGN KEY (order_id)
        REFERENCES orders (order_id) ON DELETE CASCADE,
    CONSTRAINT fk_items_variant FOREIGN KEY (batch_id)
        REFERENCES product_variants (batch_id)
);
GO

CREATE INDEX ix_products_category ON products (category_id);
CREATE INDEX ix_variants_product  ON product_variants (product_id);
CREATE INDEX ix_orders_date       ON orders (order_date);
CREATE INDEX ix_items_variant     ON order_items (batch_id);
GO

-- ---------------------------------------------------------------------
-- В.2 Наполнение таблиц данными
-- ---------------------------------------------------------------------
INSERT INTO roles (role_name) VALUES
    (N'Администратор'),
    (N'Менеджер'),
    (N'Клиент');

INSERT INTO users (login, password, last_name, first_name, middle_name, role_id) VALUES
    (N'admin',     N'1234', N'Звягинцев', N'Игорь',     N'Дмитриевич', 1),
    (N'manager',   N'1234', N'Орлова',    N'Марина',    N'Сергеевна',  2),
    (N'kuznetsov', N'1234', N'Кузнецов',  N'Павел',     N'Андреевич',  2),
    (N'ivanov',    N'1234', N'Иванов',    N'Алексей',   N'Петрович',   3),
    (N'smirnova',  N'1234', N'Смирнова',  N'Екатерина', N'Олеговна',   3),
    (N'petrov',    N'1234', N'Петров',    N'Денис',     NULL,          3),
    (N'volkova',   N'1234', N'Волкова',   N'Анна',      N'Викторовна', 3),
    (N'sokolov',   N'1234', N'Соколов',   N'Михаил',    N'Ильич',      3);

INSERT INTO categories (category_name) VALUES
    (N'Смартфоны'),
    (N'Ноутбуки'),
    (N'Планшеты'),
    (N'Наушники'),
    (N'Умные часы'),
    (N'Телевизоры');

INSERT INTO departments (department_name) VALUES
    (N'Мобильная электроника'),
    (N'Компьютерная техника'),
    (N'Аудио и ТВ');

INSERT INTO products (product_code, category_id, department_id, product_name,
                      product_content, description, image_path, base_price) VALUES
    (N'SM-001', 1, 1, N'Смартфон Nova X5',
     N'6,5" AMOLED, 8/256 ГБ, камера 50 Мп, 5000 мА·ч',
     N'Смартфон среднего класса с ярким AMOLED-экраном 120 Гц, двойной камерой и быстрой зарядкой 67 Вт.',
     N'SM-001.png', 32990.00),
    (N'SM-002', 1, 1, N'Смартфон Pulse Lite 12',
     N'6,1" IPS, 4/128 ГБ, камера 48 Мп, 4500 мА·ч',
     N'Компактный недорогой смартфон для повседневных задач. Поддержка двух SIM-карт и NFC.',
     N'SM-002.png', 15490.00),
    (N'SM-003', 1, 1, N'Смартфон Orion Pro Max',
     N'6,8" OLED, 12/512 ГБ, тройная камера 200 Мп',
     N'Флагманский смартфон с перископическим зумом, защитой IP68 и беспроводной зарядкой.',
     N'SM-003.png', 89990.00),
    (N'NB-001', 2, 2, N'Ноутбук AeroBook 14',
     N'14" IPS 2,2K, Core i5, 16 ГБ, SSD 512 ГБ',
     N'Лёгкий ноутбук в алюминиевом корпусе для учёбы и работы, вес 1,3 кг, автономность до 12 часов.',
     N'NB-001.png', 64990.00),
    (N'NB-002', 2, 2, N'Ноутбук Titan G15',
     N'15,6" 165 Гц, Ryzen 7, RTX 4060, 16 ГБ, SSD 1 ТБ',
     N'Игровой ноутбук с производительной видеокартой и эффективной системой охлаждения.',
     N'NB-002.png', 119990.00),
    (N'NB-003', 2, 2, N'Ноутбук OfficeLine 15',
     N'15,6" Full HD, Core i3, 8 ГБ, SSD 256 ГБ',
     N'Доступный ноутбук для офисных задач и интернета, полноразмерная клавиатура с цифровым блоком.',
     NULL, 38990.00),
    (N'TB-001', 3, 1, N'Планшет Slate 11',
     N'11" 2K, 8/128 ГБ, 4 динамика, 8000 мА·ч',
     N'Планшет для фильмов и учёбы с поддержкой стилуса и клавиатуры-чехла.',
     N'TB-001.png', 27990.00),
    (N'TB-002', 3, 1, N'Планшет Kids Tab 8',
     N'8" HD, 3/32 ГБ, защитный чехол',
     N'Детский планшет с родительским контролем и ударопрочным чехлом.',
     N'TB-002.png', 9990.00),
    (N'HP-001', 4, 3, N'Наушники SoundAir Pro',
     N'TWS, активное шумоподавление, до 30 ч с кейсом',
     N'Беспроводные внутриканальные наушники с адаптивным шумоподавлением и прозрачным режимом.',
     N'HP-001.png', 12990.00),
    (N'HP-002', 4, 3, N'Наушники BassLine 700',
     N'Накладные, Bluetooth 5.3, до 60 ч работы',
     N'Полноразмерные беспроводные наушники с глубоким басом и складной конструкцией.',
     N'HP-002.png', 7490.00),
    (N'WT-001', 5, 1, N'Умные часы Fit Watch 3',
     N'1,43" AMOLED, пульс, SpO2, GPS, 14 дней',
     N'Спортивные часы со 100+ режимами тренировок, мониторингом сна и защитой 5 ATM.',
     N'WT-001.png', 8990.00),
    (N'TV-001', 6, 3, N'Телевизор Vision 55 QLED',
     N'55" 4K QLED, Smart TV, HDR10+, 120 Гц',
     N'Телевизор с квантовыми точками, голосовым управлением и поддержкой игровых режимов.',
     N'TV-001.png', 54990.00);

INSERT INTO product_variants (product_id, variant_name, available_stock) VALUES
    (1,  N'Партия 01 · чёрный',    6),
    (1,  N'Партия 02 · синий',     4),
    (2,  N'Партия 01 · белый',     2),
    (2,  N'Партия 02 · чёрный',    1),
    (3,  N'Партия 01 · титан',     3),
    (3,  N'Партия 02 · графит',    0),
    (4,  N'Партия 01 · серебро',   7),
    (4,  N'Партия 02 · серый',     5),
    (5,  N'Партия 01 · чёрный',    2),
    (6,  N'Партия 01 · серый',     8),
    (7,  N'Партия 01 · серый',     4),
    (7,  N'Партия 02 · голубой',   3),
    (8,  N'Партия 01 · зелёный',   9),
    (9,  N'Партия 01 · белый',    12),
    (9,  N'Партия 02 · чёрный',    6),
    (10, N'Партия 01 · чёрный',    5),
    (11, N'Партия 01 · чёрный',    3),
    (11, N'Партия 02 · розовый',   1),
    (12, N'Партия 01 · чёрный',    4);

-- Заказы прошлого месяца (влияют на скидку) и текущего месяца
INSERT INTO orders (order_date, client_id) VALUES
    ('2026-10-03', 4),
    ('2026-10-11', 5),
    ('2026-10-18', 4),
    ('2026-10-25', 6),
    ('2026-10-29', 7),
    ('2026-11-02', 5),
    ('2026-11-04', 8),
    ('2026-11-05', 4);

INSERT INTO order_items (order_id, batch_id, quantity, unit_price) VALUES
    (1, 1,  1, 24742.50),
    (1, 14, 1,  9742.50),
    (2, 7,  1, 48742.50),
    (3, 16, 2,  5617.50),
    (4, 3,  1, 11617.50),
    (4, 17, 1,  6742.50),
    (5, 11, 1, 20992.50),
    (6, 19, 1, 41242.50),
    (7, 13, 1,  7492.50),
    (7, 15, 1,  9742.50),
    (8, 9,  1, 89992.50);
GO

-- Проверка числа строк в каждой из восьми таблиц
SELECT 'roles' AS table_name, COUNT(*) AS rows_count FROM dbo.roles
UNION ALL SELECT 'users', COUNT(*) FROM dbo.users
UNION ALL SELECT 'categories', COUNT(*) FROM dbo.categories
UNION ALL SELECT 'departments', COUNT(*) FROM dbo.departments
UNION ALL SELECT 'products', COUNT(*) FROM dbo.products
UNION ALL SELECT 'product_variants', COUNT(*) FROM dbo.product_variants
UNION ALL SELECT 'orders', COUNT(*) FROM dbo.orders
UNION ALL SELECT 'order_items', COUNT(*) FROM dbo.order_items;
GO
