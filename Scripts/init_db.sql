

CREATE DATABASE IF NOT EXISTS restogest_db CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE restogest_db;

CREATE TABLE IF NOT EXISTS CATEGORIA (
    id_categoria INT AUTO_INCREMENT PRIMARY KEY,
    nombre VARCHAR(100) NOT NULL,
    descripcion VARCHAR(255),
    activo BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE IF NOT EXISTS MESA (
    id_mesa INT AUTO_INCREMENT PRIMARY KEY,
    numero INT NOT NULL UNIQUE,
    capacidad INT NOT NULL,
    estado ENUM('Libre', 'Ocupada') NOT NULL DEFAULT 'Libre',
    activo BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE IF NOT EXISTS USUARIO (
    id_usuario INT AUTO_INCREMENT PRIMARY KEY,
    nombre VARCHAR(100) NOT NULL,
    apellido VARCHAR(100) NOT NULL,
    telefono VARCHAR(50),
    email VARCHAR(150) UNIQUE NOT NULL,
    password VARCHAR(255) NOT NULL,
    rol ENUM('Administrador', 'Mozo') NOT NULL,
    avatar_ruta VARCHAR(255),
    activo BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE IF NOT EXISTS PRODUCTO (
    id_producto INT AUTO_INCREMENT PRIMARY KEY,
    id_categoria INT NOT NULL,
    nombre VARCHAR(100) NOT NULL,
    descripcion TEXT,
    precio_vigente DECIMAL(10,2) NOT NULL,
    imagen_ruta VARCHAR(255),
    estado ENUM('Disponible', 'Sin stock') NOT NULL DEFAULT 'Disponible',
    activo BOOLEAN NOT NULL DEFAULT TRUE,
    FOREIGN KEY (id_categoria) REFERENCES CATEGORIA(id_categoria)
);

CREATE TABLE IF NOT EXISTS PEDIDO (
    id_pedido INT AUTO_INCREMENT PRIMARY KEY,
    id_mesa INT NOT NULL,
    id_usuario_mozo INT NOT NULL,
    fecha_apertura DATETIME NOT NULL,
    fecha_cierre DATETIME,
    estado ENUM('Abierto', 'Pagado', 'Anulado') NOT NULL DEFAULT 'Abierto',
    id_usuario_anulacion INT,
    fecha_anulacion DATETIME,
    FOREIGN KEY (id_mesa) REFERENCES MESA(id_mesa),
    FOREIGN KEY (id_usuario_mozo) REFERENCES USUARIO(id_usuario),
    FOREIGN KEY (id_usuario_anulacion) REFERENCES USUARIO(id_usuario)
);

CREATE TABLE IF NOT EXISTS DETALLE_PEDIDO (
    id_detalle INT AUTO_INCREMENT PRIMARY KEY,
    id_pedido INT NOT NULL,
    id_producto INT NOT NULL,
    cantidad INT NOT NULL,
    precio_historico DECIMAL(10,2) NOT NULL,
    FOREIGN KEY (id_pedido) REFERENCES PEDIDO(id_pedido),
    FOREIGN KEY (id_producto) REFERENCES PRODUCTO(id_producto)
);

CREATE TABLE IF NOT EXISTS PAGO (
    id_pago INT AUTO_INCREMENT PRIMARY KEY,
    id_pedido INT NOT NULL,
    medio_pago ENUM('Efectivo', 'Tarjeta', 'Transferencia') NOT NULL,
    importe DECIMAL(10,2) NOT NULL,
    fecha_pago DATETIME NOT NULL,
    estado ENUM('Valido', 'Anulado') NOT NULL DEFAULT 'Valido',
    id_usuario_anulacion INT,
    fecha_anulacion DATETIME,
    FOREIGN KEY (id_pedido) REFERENCES PEDIDO(id_pedido),
    FOREIGN KEY (id_usuario_anulacion) REFERENCES USUARIO(id_usuario)
);


-- DATOS DE PRUEBA 

INSERT INTO USUARIO (nombre, apellido, telefono, email, password, rol, avatar_ruta, activo) VALUES
('Martín', 'Gómez', '2664001122', 'admin@restogest.com', '123456', 'Administrador', NULL, TRUE),
('Lucía', 'Fernández', '2664334455', 'mozo@restogest.com', '123456', 'Mozo', NULL, TRUE),
('Carlos', 'Rodríguez', '2664667788', 'carlos@restogest.com', '123456', 'Mozo', NULL, TRUE);

INSERT INTO CATEGORIA (nombre, descripcion, activo) VALUES
('Entradas', 'Picadas, empanadas y entradas calientes', TRUE),
('Platos Principales', 'Carnes, pastas caseras y minutas', TRUE),
('Pizzas y Hamburguesas', 'Pizzas a la piedra y burgers gourmet', TRUE),
('Bebidas sin Alcohol', 'Gaseosas, aguas saborizadas y jugos naturales', TRUE),
('Bebidas con Alcohol', 'Cervezas artesanales y vinos seleccionados', TRUE),
('Postres', 'Postres clásicos y helados', TRUE);

INSERT INTO MESA (numero, capacidad, estado, activo) VALUES
(1, 2, 'Ocupada', TRUE),
(2, 4, 'Libre', TRUE),
(3, 4, 'Libre', TRUE),
(4, 6, 'Libre', TRUE),
(5, 8, 'Libre', TRUE),
(6, 2, 'Libre', TRUE);

INSERT INTO PRODUCTO (id_categoria, nombre, descripcion, precio_vigente, imagen_ruta, estado, activo) VALUES
(1, 'Empanada Cortada a Cuchillo', 'Empanada salteña de carne cortada a cuchillo al horno de barro', 1800.00, NULL, 'Disponible', TRUE),
(1, 'Rabas a la Provenzal', 'Porción abundante de rabas tiernas con limón y perejil', 9500.00, NULL, 'Disponible', TRUE),
(2, 'Bife de Chorizo con Papas Rusticas', 'Corte premium de 400g grillado con papas rusticas al romero', 16500.00, NULL, 'Disponible', TRUE),
(2, 'Sorrentinos de Jamón y Mozzarella', 'Pasta casera con salsa fileto, crema o mixta', 12000.00, NULL, 'Disponible', TRUE),
(2, 'Milanesa Napolitana con Fritas', 'Milanesa de ternera con salsa de tomate, jamón, muzzarella y papas', 13800.00, NULL, 'Disponible', TRUE),
(3, 'Pizza Napolitana Grande', 'Muzzarella, rodajas de tomate frsco, ajo y albahaca (8 porciones)', 11500.00, NULL, 'Disponible', TRUE),
(3, 'Burger Doble Cheddar & Bacon', 'Doble medallón smash, cheddar, panceta crocante y papas fritas', 10800.00, NULL, 'Sin stock', TRUE),
(4, 'Agua Mineral 500ml', 'Agua con o sin gas linea Eco de los Andes', 2200.00, NULL, 'Disponible', TRUE),
(4, 'Limonada con Menta y Jengibre 1L', 'Jarra de limonada recién exprimida', 4800.00, NULL, 'Disponible', TRUE),
(5, 'Pinta IPA Artesanal', 'Cerveza tirada artesanal lupulada 500ml', 4200.00, NULL, 'Disponible', TRUE),
(6, 'Flan Casero Mixto', 'Flan de huevos casero con dulce de leche y crema chantilly', 4500.00, NULL, 'Disponible', TRUE);

INSERT INTO PEDIDO (id_mesa, id_usuario_mozo, fecha_apertura, fecha_cierre, estado, id_usuario_anulacion, fecha_anulacion) VALUES
(1, 2, DATE_SUB(NOW(), INTERVAL 45 MINUTE), NULL, 'Abierto', NULL, NULL),
(2, 2, DATE_SUB(NOW(), INTERVAL 3 HOUR), DATE_SUB(NOW(), INTERVAL 2 HOUR), 'Pagado', NULL, NULL),
(3, 3, DATE_SUB(NOW(), INTERVAL 1 DAY), DATE_SUB(NOW(), INTERVAL 23 HOUR), 'Anulado', 1, DATE_SUB(NOW(), INTERVAL 22 HOUR));

INSERT INTO DETALLE_PEDIDO (id_pedido, id_producto, cantidad, precio_historico) VALUES
(1, 3, 1, 16500.00),
(1, 8, 2, 2200.00),
(2, 5, 2, 13800.00),
(2, 9, 1, 4800.00),
(3, 6, 1, 11500.00);

INSERT INTO PAGO (id_pedido, medio_pago, importe, fecha_pago, estado, id_usuario_anulacion, fecha_anulacion) VALUES
(2, 'Tarjeta', 32400.00, DATE_SUB(NOW(), INTERVAL 2 HOUR), 'Valido', NULL, NULL),
(3, 'Efectivo', 11500.00, DATE_SUB(NOW(), INTERVAL 23 HOUR), 'Anulado', 1, DATE_SUB(NOW(), INTERVAL 22 HOUR));