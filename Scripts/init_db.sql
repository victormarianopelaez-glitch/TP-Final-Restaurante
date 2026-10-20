-- 1. Crear la base de datos y seleccionarla
CREATE DATABASE IF NOT EXISTS restogest_db;
USE restogest_db;

-- 2. Crear tabla CATEGORIA
CREATE TABLE CATEGORIA (
    id_categoria INT AUTO_INCREMENT PRIMARY KEY,
    nombre VARCHAR(100) NOT NULL,
    descripcion VARCHAR(255),
    activo BOOLEAN NOT NULL DEFAULT TRUE
);

-- 3. Crear tabla MESA
CREATE TABLE MESA (
    id_mesa INT AUTO_INCREMENT PRIMARY KEY,
    numero INT NOT NULL,
    capacidad INT NOT NULL,
    estado ENUM('Libre', 'Ocupada') NOT NULL DEFAULT 'Libre',
    activo BOOLEAN NOT NULL DEFAULT TRUE
);

-- 4. Crear tabla USUARIO
CREATE TABLE USUARIO (
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

-- 5. Crear tabla PRODUCTO (depende de CATEGORIA)
CREATE TABLE PRODUCTO (
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

-- 6. Crear tabla PEDIDO (depende de MESA y USUARIO)
CREATE TABLE PEDIDO (
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

-- 7. Crear tabla DETALLE_PEDIDO (depende de PEDIDO y PRODUCTO)
CREATE TABLE DETALLE_PEDIDO (
    id_detalle INT AUTO_INCREMENT PRIMARY KEY,
    id_pedido INT NOT NULL,
    id_producto INT NOT NULL,
    cantidad INT NOT NULL,
    precio_historico DECIMAL(10,2) NOT NULL,
    FOREIGN KEY (id_pedido) REFERENCES PEDIDO(id_pedido),
    FOREIGN KEY (id_producto) REFERENCES PRODUCTO(id_producto)