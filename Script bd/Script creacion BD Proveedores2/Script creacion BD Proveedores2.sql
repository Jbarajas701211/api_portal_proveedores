BEGIN;

CREATE SCHEMA IF NOT EXISTS portal_proveedores;
-- =========================
-- CATÁLOGOS
-- =========================

CREATE TABLE IF NOT EXISTS portal_proveedores.moneda (
    id_moneda BIGSERIAL PRIMARY KEY,
    name VARCHAR(50) NOT NULL,
    symbol VARCHAR(10) NOT NULL
);

CREATE TABLE IF NOT EXISTS portal_proveedores.rol (
    id_rol BIGSERIAL PRIMARY KEY,
    description VARCHAR(100) NOT NULL
);

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint 
        WHERE conname = 'uq_rol_description'
    ) THEN
        ALTER TABLE portal_proveedores.rol
        ADD CONSTRAINT uq_rol_description UNIQUE (description);
    END IF;
END $$;

CREATE TABLE IF NOT EXISTS portal_proveedores.tipos_impuestos (
    id_impuesto_t VARCHAR(5) PRIMARY KEY,
    descripcion VARCHAR(100) NOT NULL
);

CREATE TABLE IF NOT EXISTS portal_proveedores.codigos_impuestos (
    id_impuesto_c VARCHAR(10) PRIMARY KEY,
    descripcion VARCHAR(100) NOT NULL
);

CREATE TABLE IF NOT EXISTS portal_proveedores.clasificacion_items (
    id_clasificacion BIGSERIAL PRIMARY KEY,
    descripcion VARCHAR(100) NOT NULL
);

CREATE TABLE IF NOT EXISTS portal_proveedores.unidades (
    id_unidad BIGSERIAL PRIMARY KEY,
    descripcion VARCHAR(50) NOT NULL
);

CREATE TABLE IF NOT EXISTS portal_proveedores.categorias (
    id_categoria BIGSERIAL PRIMARY KEY,
    clave_categoria VARCHAR(50) NOT NULL,
    categoria VARCHAR(100) NOT NULL,
    acreedor_sin_xml BOOLEAN NOT NULL,
    aplicar_tolerancia_categoria BOOLEAN NOT NULL
);

-- =========================
-- USUARIOS
-- =========================

CREATE TABLE IF NOT EXISTS portal_proveedores.usuario (
    id_usuario BIGSERIAL PRIMARY KEY,
    usuario VARCHAR(50) NOT NULL,
    password VARCHAR(255) NOT NULL,
    nombre VARCHAR(100) NOT NULL,
    apellido_paterno VARCHAR(100) NOT NULL,
    apellido_materno VARCHAR(100),
    correo_electronico VARCHAR(150) NOT NULL,
	rfc_proveedor VARCHAR(20),
    estatus BOOLEAN NOT NULL,
	codigo_activacion VARCHAR(50)
);

CREATE TABLE IF NOT EXISTS portal_proveedores.usuario_rol (
    id_relacion_ur BIGSERIAL PRIMARY KEY,
    id_usuario BIGINT NOT NULL REFERENCES portal_proveedores.usuario(id_usuario),
    id_rol BIGINT NOT NULL REFERENCES portal_proveedores.rol(id_rol),
    UNIQUE (id_usuario, id_rol)
);

-- =========================
-- EMPRESAS
-- =========================

CREATE TABLE IF NOT EXISTS portal_proveedores.empresas (
    id_empresa BIGSERIAL PRIMARY KEY,
    nombre VARCHAR(150) NOT NULL,
    rfc VARCHAR(20) NOT NULL,
    estatus BOOLEAN NOT NULL,
    unidad_de_negocio VARCHAR(50)
);

CREATE TABLE IF NOT EXISTS portal_proveedores.usuario_empresa (
    id_relacion_ue BIGSERIAL PRIMARY KEY,
    id_usuario BIGINT NOT NULL REFERENCES portal_proveedores.usuario(id_usuario),
    id_empresa BIGINT NOT NULL REFERENCES portal_proveedores.empresas(id_empresa),
    UNIQUE (id_usuario, id_empresa)
);

-- =========================
-- PROVEEDORES
-- =========================

CREATE TABLE IF NOT EXISTS portal_proveedores.proveedores (
    id_proveedor BIGSERIAL PRIMARY KEY,
    nombre VARCHAR(150) NOT NULL,
    rfc VARCHAR(20) NOT NULL,
    vendor_id VARCHAR(50),
    estatus BOOLEAN NOT NULL,
    sobrante NUMERIC(18,2),
    porcentaje_sobrante NUMERIC(5,2),
	faltante NUMERIC(18,2),
	porcentaje_faltante NUMERIC(5,2),
    aplicar_tolerancia BOOLEAN NOT NULL,
    id_categoria BIGINT REFERENCES portal_proveedores.categorias(id_categoria),
    acreedor_sin_xml BOOLEAN NOT NULL,
    aplicar_tolerancia_categoria BOOLEAN NOT NULL,
    email_proveedor VARCHAR(150),
    doc_fiscal VARCHAR(25),
    factura BOOLEAN NOT NULL,
    recepcion BOOLEAN NOT NULL,
    origen VARCHAR(50),
    razon_social VARCHAR(150),
    entity_id VARCHAR(50)
);

-- proveedores
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint 
        WHERE conname = 'uq_proveedor_rfc'
    ) THEN
        ALTER TABLE portal_proveedores.proveedores
        ADD CONSTRAINT uq_proveedor_rfc UNIQUE (rfc);
    END IF;
END $$;

CREATE TABLE IF NOT EXISTS portal_proveedores.proveedor_empresa (
    id_relacion_pe BIGSERIAL PRIMARY KEY,
    id_proveedor BIGINT NOT NULL REFERENCES portal_proveedores.proveedores(id_proveedor),
    id_empresa BIGINT NOT NULL REFERENCES portal_proveedores.empresas(id_empresa),
    UNIQUE (id_proveedor, id_empresa)
);

-- =========================
-- DOCUMENTOS
-- =========================

CREATE TABLE IF NOT EXISTS portal_proveedores.documentos (
    id_documento BIGSERIAL PRIMARY KEY,
    tipo VARCHAR(50) NOT NULL,
    descripcion VARCHAR(150)
);

CREATE TABLE IF NOT EXISTS portal_proveedores.proveedor_documento (
    id_relacion_pd BIGSERIAL PRIMARY KEY,
    id_proveedor BIGINT NOT NULL REFERENCES portal_proveedores.proveedores(id_proveedor),
    id_documento BIGINT NOT NULL REFERENCES portal_proveedores.documentos(id_documento),
    opcional BOOLEAN NOT NULL,
    UNIQUE (id_proveedor, id_documento)
);


-- =========================
-- FACTURAS
-- =========================

CREATE TABLE IF NOT EXISTS portal_proveedores.facturas (
    id_factura BIGSERIAL PRIMARY KEY,
    id_proveedor BIGINT REFERENCES portal_proveedores.proveedores(id_proveedor),
    id_empresa BIGINT REFERENCES portal_proveedores.empresas(id_empresa),
    tipo_de_comprobante VARCHAR(50),
    estatus_factura VARCHAR(50),
    folio_origen VARCHAR(50),
    folio VARCHAR(50),
    serie VARCHAR(50),
    uuid VARCHAR(50) NOT NULL,
    motivo VARCHAR(150),
    hay_evidencia BOOLEAN,
    fecha_alta TIMESTAMP,
    fecha_factura TIMESTAMP,
    subtotal NUMERIC(18,2),
    cd_total NUMERIC(18,2),
    total NUMERIC(18,2),
    monto_de_recepcion NUMERIC(18,2),
    correo_electronico VARCHAR(150),
    xml TEXT,
    representacion_grafica TEXT,
    unidad_negocio VARCHAR(100),
    no_orden_compra VARCHAR(50),
    no_recepcion VARCHAR(50),
    version_cfdi VARCHAR(10),
    ieps NUMERIC(18,2),
    fecha_registro TIMESTAMP,
    iva NUMERIC(18,2),
    folio_erp VARCHAR(50),
    fecha_contabilizacion TIMESTAMP,
    fecha_creacion TIMESTAMP,
    fecha_modificacion TIMESTAMP,
	rfc_proveedor VARCHAR(20),
	numero_factura_relacionada VARCHAR(50)
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_facturas_uuid
ON portal_proveedores.facturas(uuid);



-- =========================
-- ÍNDICES
-- =========================

CREATE INDEX IF NOT EXISTS idx_usuario_usuario ON portal_proveedores.usuario(usuario);
CREATE INDEX IF NOT EXISTS idx_usuario_correo ON portal_proveedores.usuario(correo_electronico);
CREATE INDEX IF NOT EXISTS idx_empresas_rfc ON portal_proveedores.empresas(rfc);
CREATE INDEX IF NOT EXISTS idx_proveedores_vendor ON portal_proveedores.proveedores(vendor_id);
CREATE INDEX IF NOT EXISTS idx_facturas_empresa_fecha ON portal_proveedores.facturas(id_empresa, fecha_factura);
CREATE INDEX IF NOT EXISTS idx_facturas_proveedor_estatus ON portal_proveedores.facturas(id_proveedor, estatus_factura);


-- =========================
-- TRACE DE Usuario
-- =========================
CREATE TABLE IF NOT EXISTS portal_proveedores.trace_usuarios
(
    id BIGSERIAL PRIMARY KEY,
    id_usuario bigint NOT NULL,
    evento varchar(50) NOT NULL,
    descripcion text,
    registrado_en timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT trace_usuarios_usuario_id_fkey
        FOREIGN KEY (id_usuario)
        REFERENCES portal_proveedores.usuario (id_usuario)
        ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS portal_proveedores.refresh_tokens
(
    id BIGSERIAL,
    id_usuario bigint NOT NULL,
    token text COLLATE pg_catalog."default" NOT NULL,
    creado_en timestamp without time zone NOT NULL DEFAULT now(),
    expira_en timestamp without time zone NOT NULL,
    revocado_en timestamp without time zone,
    reemplazado_por text COLLATE pg_catalog."default",
    CONSTRAINT refresh_tokens_pkey PRIMARY KEY (id),
    CONSTRAINT fk_refresh_tokens_usuario FOREIGN KEY (id_usuario)
        REFERENCES portal_proveedores.usuario (id_usuario) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS portal_proveedores.parametros(
	id BIGSERIAL,
	codigo varchar(50) NOT NULL,
	descripcion VARCHAR(50) NOT NULL,
	valor VARCHAR(50) NOT NULL,
	unidad_medida VARCHAR(50) NOT NULL,
	notificacion BOOLEAN NOT NULL,
	modificado TIMESTAMP,
	id_usuario bigint NOT NULL,
	estatus BOOLEAN NOT NULL,
	CONSTRAINT pk_parametros PRIMARY KEY (id),
	CONSTRAINT fk_parametros_usuario FOREIGN KEY (id_usuario)
		REFERENCES portal_proveedores.usuario (id_usuario) MATCH SIMPLE
		
	);
	
	Insert into portal_proveedores.rol (description) values('PROVEEDOR') ON CONFLICT (description) DO NOTHING;
	Insert into portal_proveedores.rol (description) values('ADMIN') ON CONFLICT (description) DO NOTHING;
	INSERT INTO portal_proveedores.usuario_rol (id_usuario, id_rol)
	SELECT 1, 1
	WHERE EXISTS (SELECT 1 FROM portal_proveedores.usuario WHERE id_usuario = 1)
	ON CONFLICT (id_usuario, id_rol) DO NOTHING;


-- ============================================
-- 1. TABLA: PAGOS CFDI (ENCABEZADO)
-- Representa el XML completo del complemento de pago
-- ============================================
CREATE TABLE IF NOT EXISTS portal_proveedores.pagos_cfdi (
    id BIGSERIAL PRIMARY KEY,

    uuid VARCHAR(50) UNIQUE NOT NULL,
    serie VARCHAR(10),
    folio VARCHAR(20),
    fecha TIMESTAMP,

    rfc_emisor VARCHAR(13),
    nombre_emisor VARCHAR(255),

    rfc_receptor VARCHAR(13),
    nombre_receptor VARCHAR(255),

    total NUMERIC(18,2),

    xml_original TEXT,

    fecha_alta TIMESTAMP DEFAULT NOW()
);



-- ============================================
-- 2. TABLA: DETALLE DE PAGOS
-- Cada registro = un pago dentro del CFDI
-- ============================================
CREATE TABLE IF NOT EXISTS portal_proveedores.pagos_detalle (
    id BIGSERIAL PRIMARY KEY,

    pago_cfdi_id BIGINT NOT NULL
    REFERENCES portal_proveedores.pagos_cfdi(id) ON DELETE CASCADE,

    fecha_pago TIMESTAMP,
    forma_pago VARCHAR(10),
    moneda VARCHAR(10),
    tipo_cambio NUMERIC(18,6),
    monto NUMERIC(18,2),

    num_operacion VARCHAR(100),
    banco_ordenante VARCHAR(255),
    cuenta_ordenante VARCHAR(50),
    cuenta_beneficiario VARCHAR(50)
);

-- ============================================
-- 3. TABLA: FACTURAS RELACIONADAS (DR)
-- Cada pago puede tener múltiples facturas
-- ============================================
CREATE TABLE IF NOT EXISTS portal_proveedores.pagos_facturas_relacionadas (
    id BIGSERIAL PRIMARY KEY,

    pago_id BIGINT NOT NULL
    REFERENCES portal_proveedores.pagos_detalle(id) ON DELETE CASCADE,

    uuid_factura VARCHAR(50),
    serie VARCHAR(20),
    folio VARCHAR(20),

    num_parcialidad INT,

    imp_saldo_anterior NUMERIC(18,2),
    imp_pagado NUMERIC(18,2),
    imp_saldo_insoluto NUMERIC(18,2)
);


-- ============================================
-- 4. VISTA TIPO REPORTE (PORTAL / EXCEL / BI)
-- ============================================
CREATE OR REPLACE VIEW portal_proveedores.vw_reporte_pagos AS
SELECT
    c.uuid AS uuid_pago,
    c.fecha,
    c.nombre_emisor,
    c.nombre_receptor,

    p.fecha_pago,
    p.monto,
    p.forma_pago,

    f.uuid_factura,
    f.folio,
    f.num_parcialidad,
    f.imp_pagado,
    f.imp_saldo_insoluto

FROM portal_proveedores.pagos_cfdi c
JOIN portal_proveedores.pagos_detalle p 
    ON p.pago_cfdi_id = c.id
LEFT JOIN portal_proveedores.pagos_facturas_relacionadas f 
    ON f.pago_id = p.id;


-- ============================================
-- ORDENES DE COMPRA
-- ============================================

CREATE TABLE IF NOT EXISTS portal_proveedores.ordenes_compra (
    id_orden_compra BIGSERIAL PRIMARY KEY,

	-- Identificadores
    erp_origen VARCHAR(20) NOT NULL, -- SAP | NETSUITE
    id_externo VARCHAR(50) NOT NULL, -- ebeln (SAP) | internalid (NS)
    folio VARCHAR(50),				 -- tranid OC

	-- Datos generales
    fecha_oc TIMESTAMP,
    moneda VARCHAR(10),
    total NUMERIC(18,2),

	-- Proveedor
    proveedor_id VARCHAR(50),
    proveedor_nombre VARCHAR(150),
    proveedor_rfc VARCHAR(20),

	-- Empresa / sociedad
    sociedad VARCHAR(10),
    subsidiaria VARCHAR(150),

    CONSTRAINT uq_oc UNIQUE (erp_origen, id_externo)
);

-- ============================================
-- RECEPCIONES (CABECERA)
-- ============================================

CREATE TABLE IF NOT EXISTS portal_proveedores.recepciones (
    id_recepcion BIGSERIAL PRIMARY KEY,

	-- Relación
    id_orden_compra BIGINT NOT NULL,

	-- Identificadores
    erp_origen VARCHAR(20) NOT NULL,
    id_externo VARCHAR(50) NOT NULL, -- internalid NS | mblnr SAP
    folio VARCHAR(50),				 -- tranid NS | mblnr SAP

	-- Fechas
    fecha_recepcion TIMESTAMP,
    fecha_contabilizacion TIMESTAMP,

	-- Datos generales
    moneda VARCHAR(10),
    subtotal NUMERIC(18,2),
    total NUMERIC(18,2),
	cantidad NUMERIC(18,2),

	-- Estado
    estado VARCHAR(10),
	
	-- Usuario
    usuario_creacion VARCHAR(100),

	-- Proveedor
    proveedor_id VARCHAR(50),
    proveedor_nombre VARCHAR(150),
    proveedor_rfc VARCHAR(20),

	-- Empresa
    sociedad VARCHAR(10),
    centro VARCHAR(10),

    CONSTRAINT fk_recepcion_oc FOREIGN KEY (id_orden_compra)
        REFERENCES portal_proveedores.ordenes_compra(id_orden_compra),

    CONSTRAINT uq_recepcion UNIQUE (erp_origen, id_externo)
);

-- ============================================
-- RECEPCION DETALLE
-- ============================================

CREATE TABLE IF NOT EXISTS portal_proveedores.recepcion_detalle (
    id_detalle BIGSERIAL PRIMARY KEY,

    id_recepcion BIGINT NOT NULL,

	-- Línea
    linea VARCHAR(10),

	-- Material
    item_id VARCHAR(50),
    item_nombre VARCHAR(150),
    descripcion TEXT,

	-- Material
    unidad_id VARCHAR(10),
    unidad_nombre VARCHAR(50),

	-- Cantidades
    cantidad NUMERIC(18,4),
    precio_unitario NUMERIC(18,4),

    subtotal NUMERIC(18,2),
    total NUMERIC(18,2),

    CONSTRAINT fk_detalle_recepcion FOREIGN KEY (id_recepcion)
        REFERENCES portal_proveedores.recepciones(id_recepcion) ON DELETE CASCADE
);

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint 
        WHERE conname = 'uq_detalle'
    ) THEN
        ALTER TABLE portal_proveedores.recepcion_detalle
        ADD CONSTRAINT uq_detalle UNIQUE (id_recepcion, linea);
    END IF;
END $$;

-- ============================================
-- IMPUESTOS DE RECEPCION (taxDetails NETSUITE)
-- ============================================

CREATE TABLE IF NOT EXISTS portal_proveedores.recepcion_impuestos (
    id_impuesto BIGSERIAL PRIMARY KEY,

    id_recepcion BIGINT NOT NULL,

	-- Tipo impuesto
    tax_type_id VARCHAR(10),
    tax_type_nombre VARCHAR(100),

    tax_code_id VARCHAR(10),
    tax_code_nombre VARCHAR(100),

	-- Valores
    base NUMERIC(18,2),
    importe NUMERIC(18,2),
    tasa NUMERIC(10,6),

	-- SAT
    tipo_impuesto VARCHAR(10),
    clase_impuesto VARCHAR(20),

    CONSTRAINT fk_impuesto_recepcion FOREIGN KEY (id_recepcion)
        REFERENCES portal_proveedores.recepciones(id_recepcion) ON DELETE CASCADE
);

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint 
        WHERE conname = 'uq_impuesto_recepcion'
    ) THEN
        ALTER TABLE portal_proveedores.recepcion_impuestos
        ADD CONSTRAINT uq_impuesto_recepcion 
        UNIQUE (id_recepcion, tax_code_id, tax_type_id);
    END IF;
END $$;
-- ============================================
-- LOG DE INTEGRACIÓN (JSON SAP / NETSUITE)
-- ============================================

CREATE TABLE IF NOT EXISTS portal_proveedores.integracion_logs (
    id_log BIGSERIAL PRIMARY KEY,

    erp_origen VARCHAR(20) NOT NULL, -- SAP | NETSUITE
    tipo_documento VARCHAR(30) NOT NULL, -- RECEPCION | OC

    id_externo VARCHAR(50), -- para trazabilidad

    json_payload JSONB NOT NULL,

    fecha_registro TIMESTAMP DEFAULT CURRENT_TIMESTAMP,

    procesado BOOLEAN DEFAULT FALSE,
    error TEXT
);

-- =========================
-- SINCRONIZACIÓN
-- =========================

CREATE TABLE IF NOT EXISTS portal_proveedores.sincronizacion_facturas (
    id_proceso BIGSERIAL PRIMARY KEY,
    id_factura BIGINT REFERENCES portal_proveedores.facturas(id_factura),
    id_recepcion BIGINT REFERENCES portal_proveedores.recepciones(id_recepcion),
    internal_id_oc VARCHAR(50),
    solicitud TEXT,
    respuesta TEXT,
    estatus VARCHAR(50),
    fecha_solicitud TIMESTAMP,
    intento INT
);
-- ============================================
-- ÍNDICES
-- ============================================

CREATE INDEX IF NOT EXISTS idx_oc_externo ON portal_proveedores.ordenes_compra(id_externo);
CREATE INDEX IF NOT EXISTS idx_rec_externo ON portal_proveedores.recepciones(id_externo);
CREATE INDEX IF NOT EXISTS idx_rec_oc ON portal_proveedores.recepciones(id_orden_compra);
CREATE INDEX IF NOT EXISTS idx_detalle_rec ON portal_proveedores.recepcion_detalle(id_recepcion);

CREATE INDEX IF NOT EXISTS idx_log_erp ON portal_proveedores.integracion_logs(erp_origen);
CREATE INDEX IF NOT EXISTS idx_log_externo ON portal_proveedores.integracion_logs(id_externo);
CREATE INDEX IF NOT EXISTS idx_log_procesado ON portal_proveedores.integracion_logs(procesado);
CREATE INDEX IF NOT EXISTS idx_sync_factura ON portal_proveedores.sincronizacion_facturas(id_factura);
CREATE INDEX IF NOT EXISTS idx_sync_recepcion ON portal_proveedores.sincronizacion_facturas(id_recepcion);
CREATE INDEX IF NOT EXISTS idx_log_json ON portal_proveedores.integracion_logs USING GIN (json_payload);
CREATE INDEX IF NOT EXISTS idx_log_tipo_doc ON portal_proveedores.integracion_logs(tipo_documento);

-- =========================================
-- RELACIÓN N:M FACTURA - RECEPCIÓN
-- =========================================
CREATE TABLE IF NOT EXISTS portal_proveedores.factura_recepcion (
    id BIGSERIAL PRIMARY KEY,

    id_factura BIGINT NOT NULL,
    id_recepcion BIGINT NOT NULL,

    UNIQUE (id_factura, id_recepcion),

    FOREIGN KEY (id_factura)
        REFERENCES portal_proveedores.facturas(id_factura)
        ON DELETE CASCADE,

    FOREIGN KEY (id_recepcion)
        REFERENCES portal_proveedores.recepciones(id_recepcion)
        ON DELETE CASCADE
);

-- =========================================
-- TABLA AVISOS PORTAL
-- =========================================
CREATE TABLE IF NOT EXISTS portal_proveedores.avisos (
    id_aviso SERIAL PRIMARY KEY,
    categoria VARCHAR(50) NOT NULL,
    mensaje TEXT NOT NULL,
    estatus BOOLEAN default FALSE,
    fecha_inicio_aviso TIMESTAMP WITHOUT TIME ZONE NOT NULL,
    fecha_final_aviso TIMESTAMP WITHOUT TIME ZONE,
    fecha_creacion TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);


-- =========================================
-- TABLA NOTIFICACIONES
-- =========================================

CREATE TABLE IF NOT EXISTS portal_proveedores.notificaciones
(
    id BIGSERIAL PRIMARY KEY,
    fecha date NOT NULL,
    hora time without time zone NOT NULL,
    titulo character varying(255) COLLATE pg_catalog."default" NOT NULL,
    tag character varying(255) COLLATE pg_catalog."default" NOT NULL,
    detalle text COLLATE pg_catalog."default",
    creado_en TIMESTAMP without time zone DEFAULT CURRENT_TIMESTAMP,
    meta_data json NOT NULL DEFAULT '{}'::json
)


CREATE INDEX IF NOT EXISTS ix_notif_creado
    ON portal_proveedores.notificaciones USING btree
    (creado_en DESC NULLS FIRST, id ASC NULLS LAST)
    TABLESPACE pg_default;
	

-- =========================================
-- TABLA NOTIFICACIONES_USUARIOS
-- =========================================
	CREATE TABLE IF NOT EXISTS portal_proveedores.notificaciones_usuarios
(
    id BIGSERIAL PRIMARY KEY,
    notificacion_id bigint NOT NULL,
    usuario_id bigint NOT NULL,
    leida boolean DEFAULT false,
    leida_en timestamp without time zone,
    CONSTRAINT notificaciones_usuarios_notificacion_id_usuario_id_key UNIQUE (notificacion_id, usuario_id),
    CONSTRAINT notificaciones_usuarios_notificacion_id_fkey FOREIGN KEY (notificacion_id)
        REFERENCES portal_proveedores.notificaciones (id) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE CASCADE,
    CONSTRAINT notificaciones_usuarios_usuario_id_fkey FOREIGN KEY (usuario_id)
        REFERENCES portal_proveedores.usuario (id_usuario) MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS ix_notif_user
    ON portal_proveedores.notificaciones_usuarios USING btree
    (usuario_id ASC NULLS LAST, notificacion_id ASC NULLS LAST)
    TABLESPACE pg_default;
	
CREATE INDEX IF NOT EXISTS ix_notif_user_leida
    ON portal_proveedores.notificaciones_usuarios USING btree
    (usuario_id ASC NULLS LAST, leida ASC NULLS LAST, notificacion_id ASC NULLS LAST)
    TABLESPACE pg_default;
	
CREATE INDEX IF NOT EXISTS ix_notif_user_leidaen
    ON portal_proveedores.notificaciones_usuarios USING btree
    (usuario_id ASC NULLS LAST, leida_en ASC NULLS LAST)
    TABLESPACE pg_default;	
COMMIT;
