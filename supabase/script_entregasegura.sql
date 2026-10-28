-- =============================================================
-- Entrega Segura (versión de clase) - Script de base de datos v2
-- Ejecutar en Supabase > SQL Editor > New query > Run
--
-- 10 TABLAS en el esquema uis:
--   roles, vehiculos, conductores, clientes, productos, motivos_novedad   (catálogos)
--   ordenes, detalle_orden                                         (lo que se entrega)
--   entregas, novedades                                            (lo que registra el conductor)
--
-- IMPORTANTE: borra y vuelve a crear las tablas de la versión anterior
-- (ordenes, entregas, novedades) y sus datos de prueba.
-- No toca las tablas del proyecto grande (delivery_*).
-- =============================================================

create schema if not exists uis;

drop table if exists uis.novedades cascade;
drop table if exists uis.entregas cascade;
drop table if exists uis.detalle_orden cascade;
drop table if exists uis.ordenes cascade;
drop table if exists uis.motivos_novedad cascade;
drop table if exists uis.productos cascade;
drop table if exists uis.clientes cascade;
drop table if exists uis.conductores cascade;
drop table if exists uis.vehiculos cascade;
drop table if exists uis.roles cascade;
drop function if exists uis.es_supervisor() cascade;
drop function if exists uis.mi_conductor_id() cascade;

-- ---------------------------------------------------------------
-- 1. CATÁLOGOS
-- ---------------------------------------------------------------
-- Roles de usuario: CONDUCTOR (entrega) y SUPERVISOR (controla a todos)
create table uis.roles (
    id     bigint generated always as identity primary key,
    nombre text not null unique check (nombre in ('CONDUCTOR', 'SUPERVISOR'))
);

create table uis.vehiculos (
    id        bigint generated always as identity primary key,
    placa     text not null unique,
    modelo    text not null,
    capacidad_kg int not null default 0 check (capacidad_kg >= 0)
);

create table uis.conductores (
    id          bigint generated always as identity primary key,
    email       text not null unique,          -- el mismo correo del usuario en Supabase Auth
    nombres     text not null,
    cedula      text not null unique check (length(cedula) = 10),
    telefono    text,
    rol_id      bigint not null references uis.roles(id),
    vehiculo_id bigint references uis.vehiculos(id)
);

create table uis.clientes (
    id        bigint generated always as identity primary key,
    nombre    text not null,
    ruc       text unique,
    direccion text not null,
    telefono  text,
    latitud   double precision,
    longitud  double precision
);

create table uis.productos (
    id          bigint generated always as identity primary key,
    codigo      text not null unique,
    descripcion text not null,
    precio      numeric(10,2) not null check (precio >= 0)
);

create table uis.motivos_novedad (
    id          bigint generated always as identity primary key,
    descripcion text not null unique
);

-- ---------------------------------------------------------------
-- 2. ÓRDENES Y SU DETALLE
-- ---------------------------------------------------------------
create table uis.ordenes (
    id           bigint generated always as identity primary key,
    numero       text not null unique,
    cliente_id   bigint not null references uis.clientes(id),
    conductor_id bigint not null references uis.conductores(id),
    fecha        date not null default current_date,
    secuencia    int not null default 1,
    estado       text not null default 'PENDIENTE' check (estado in ('PENDIENTE', 'ENTREGADA', 'NOVEDAD')),
    actualizado  timestamptz not null default now()
);

create table uis.detalle_orden (
    id          bigint generated always as identity primary key,
    orden_id    bigint not null references uis.ordenes(id) on delete cascade,
    producto_id bigint not null references uis.productos(id),
    cantidad    int not null check (cantidad > 0)
);

-- ---------------------------------------------------------------
-- 3. LO QUE REGISTRA EL CONDUCTOR
-- ---------------------------------------------------------------
create table uis.entregas (
    id              bigint generated always as identity primary key,
    orden_id        bigint not null unique references uis.ordenes(id),   -- una entrega por orden
    recibe          text not null,
    latitud         double precision not null,
    longitud        double precision not null,
    foto            text not null,
    firma           text not null,
    conductor_email text not null default auth.email(),
    fecha           timestamptz not null default now()
);

create table uis.novedades (
    id              bigint generated always as identity primary key,
    orden_id        bigint not null references uis.ordenes(id),
    motivo_id       bigint not null references uis.motivos_novedad(id),
    descripcion     text not null,
    latitud         double precision,
    longitud        double precision,
    foto            text,
    conductor_email text not null default auth.email(),
    fecha           timestamptz not null default now(),
    revisada        boolean not null default false,   -- la marca el supervisor
    respuesta       text                              -- comentario del supervisor
);

-- Índices para las consultas más usadas
create index on uis.ordenes (conductor_id, estado);
create index on uis.detalle_orden (orden_id);

-- ---------------------------------------------------------------
-- 4. PERMISOS (solo usuarios con sesión iniciada)
-- ---------------------------------------------------------------
grant usage on schema uis to authenticated;
grant select on uis.roles, uis.vehiculos, uis.conductores, uis.clientes, uis.productos,
                uis.motivos_novedad, uis.ordenes, uis.detalle_orden to authenticated;
grant update (estado) on uis.ordenes to authenticated;
grant select, insert on uis.entregas, uis.novedades to authenticated;
grant update (revisada, respuesta) on uis.novedades to authenticated;   -- solo supervisor (ver RLS)

-- ---------------------------------------------------------------
-- 5. SEGURIDAD (RLS)
--    CONDUCTOR: ve y modifica solo lo suyo.
--    SUPERVISOR: ve todo y revisa las novedades.
-- ---------------------------------------------------------------

-- Funciones de ayuda (SECURITY DEFINER evita que las reglas se llamen a sí mismas)
create or replace function uis.mi_conductor_id()
returns bigint language sql stable security definer set search_path = '' as $$
    select c.id from uis.conductores c where c.email = auth.email();
$$;

create or replace function uis.es_supervisor()
returns boolean language sql stable security definer set search_path = '' as $$
    select exists (select 1 from uis.conductores c join uis.roles r on r.id = c.rol_id
                   where c.email = auth.email() and r.nombre = 'SUPERVISOR');
$$;

grant execute on function uis.mi_conductor_id(), uis.es_supervisor() to authenticated;

alter table uis.roles           enable row level security;
alter table uis.vehiculos       enable row level security;
alter table uis.conductores     enable row level security;
alter table uis.clientes        enable row level security;
alter table uis.productos       enable row level security;
alter table uis.motivos_novedad enable row level security;
alter table uis.ordenes         enable row level security;
alter table uis.detalle_orden   enable row level security;
alter table uis.entregas        enable row level security;
alter table uis.novedades       enable row level security;

-- Catálogos: los puede leer cualquier usuario con sesión
create policy roles_ver     on uis.roles     for select to authenticated using (true);
create policy vehiculos_ver on uis.vehiculos for select to authenticated using (true);
create policy clientes_ver  on uis.clientes  for select to authenticated using (true);
create policy productos_ver on uis.productos for select to authenticated using (true);
create policy motivos_ver   on uis.motivos_novedad for select to authenticated using (true);

-- Conductores: cada uno ve su perfil; el supervisor ve a todos
create policy conductores_ver on uis.conductores for select to authenticated
    using (email = auth.email() or uis.es_supervisor());

-- Órdenes: el conductor ve las suyas; el supervisor ve todas
create policy ordenes_ver on uis.ordenes for select to authenticated
    using (conductor_id = uis.mi_conductor_id() or uis.es_supervisor());

-- Solo el conductor asignado cambia el estado de una orden pendiente
create policy ordenes_actualizar on uis.ordenes for update to authenticated
    using (estado = 'PENDIENTE' and conductor_id = uis.mi_conductor_id())
    with check (conductor_id = uis.mi_conductor_id());

-- Detalle: el de las órdenes que el usuario puede ver
create policy detalle_ver on uis.detalle_orden for select to authenticated
    using (exists (select 1 from uis.ordenes o where o.id = orden_id));

-- Entregas y novedades: el conductor registra las suyas; el supervisor ve todas
create policy entregas_ver on uis.entregas for select to authenticated
    using (conductor_email = auth.email() or uis.es_supervisor());
create policy entregas_crear on uis.entregas for insert to authenticated
    with check (conductor_email = auth.email()
                and exists (select 1 from uis.ordenes o where o.id = orden_id and o.conductor_id = uis.mi_conductor_id()));

create policy novedades_ver on uis.novedades for select to authenticated
    using (conductor_email = auth.email() or uis.es_supervisor());
create policy novedades_crear on uis.novedades for insert to authenticated
    with check (conductor_email = auth.email()
                and exists (select 1 from uis.ordenes o where o.id = orden_id and o.conductor_id = uis.mi_conductor_id()));

-- Solo el supervisor marca una novedad como revisada
create policy novedades_revisar on uis.novedades for update to authenticated
    using (uis.es_supervisor()) with check (uis.es_supervisor());

-- Guarda la hora de cada cambio de estado
create or replace function uis.marcar_actualizado()
returns trigger language plpgsql as $$
begin
    new.actualizado := now();
    return new;
end;
$$;

create trigger trg_ordenes_actualizado before update on uis.ordenes
    for each row execute function uis.marcar_actualizado();

-- ---------------------------------------------------------------
-- 6. BUCKET PRIVADO PARA FOTOS Y FIRMAS (cada conductor en su carpeta)
-- ---------------------------------------------------------------
insert into storage.buckets (id, name, public)
values ('fotos-entregas', 'fotos-entregas', false)
on conflict (id) do nothing;

drop policy if exists fotos_subir on storage.objects;
create policy fotos_subir on storage.objects for insert to authenticated
    with check (bucket_id = 'fotos-entregas' and (storage.foldername(name))[1] = auth.uid()::text);

drop policy if exists fotos_ver on storage.objects;
create policy fotos_ver on storage.objects for select to authenticated
    using (bucket_id = 'fotos-entregas'
           and ((storage.foldername(name))[1] = auth.uid()::text or uis.es_supervisor()));

-- ---------------------------------------------------------------
-- 7. DATOS DE PRUEBA (los usuarios se crean en Authentication > Users)
-- ---------------------------------------------------------------
insert into uis.roles (nombre) values ('CONDUCTOR'), ('SUPERVISOR');

insert into uis.vehiculos (placa, modelo, capacidad_kg) values
 ('PBA-1234', 'Camión Chevrolet NPR', 3500),
 ('PCC-5678', 'Furgón Toyota Hiace', 1200);

-- rol_id: 1 = CONDUCTOR, 2 = SUPERVISOR
insert into uis.conductores (email, nombres, cedula, telefono, rol_id, vehiculo_id) values
 ('jperez@entregasegura.com',   'Jorge Pérez',   '1712345675', '0991112233', 1, 1),
 ('mlopez@entregasegura.com',   'María López',   '1709876543', '0994445566', 1, 2),
 ('psalazar@entregasegura.com', 'Paola Salazar', '1711223344', '0997778899', 2, null);

insert into uis.clientes (nombre, ruc, direccion, telefono, latitud, longitud) values
 ('Papelería La Pluma',         '1790012345001', 'Av. Amazonas N24-10 y Colón, Quito',   '0990000001', -0.2010, -78.4950),
 ('Librería El Saber',          '1790023456001', 'Av. 6 de Diciembre N34-120, Quito',    '0990000002', -0.1840, -78.4780),
 ('Colegio Nuevo Horizonte',    '1790034567001', 'Calle De los Pinos E12-45, Quito',     '0990000003', -0.1580, -78.4720),
 ('Papelería El Estudiante',    '1790045678001', 'Av. Mariana de Jesús E7-80, Quito',    '0990000014', -0.1870, -78.4870),
 ('Distribuidora Escolar Norte','1790056789001', 'Av. Galo Plaza Lasso N65-20, Quito',   '0990000015', -0.1280, -78.4880);

insert into uis.productos (codigo, descripcion, precio) values
 ('NOR-C100', 'Cuaderno Norma cosido 100 hojas', 1.85),
 ('NOR-E160', 'Cuaderno Norma espiral 160 hojas', 3.20),
 ('NOR-BLA4', 'Block de dibujo Norma A4', 1.40),
 ('NOR-AGEN', 'Agenda escolar Norma', 4.50),
 ('NOR-CARP', 'Carpeta archivadora Norma', 2.75),
 ('NOR-U100', 'Cuaderno universitario Norma 100 hojas', 2.10);

insert into uis.motivos_novedad (descripcion) values
 ('Cliente ausente'), ('Dirección incorrecta'), ('Cliente rechaza el pedido'),
 ('Producto dañado'), ('Pedido incompleto'), ('Otro motivo');

insert into uis.ordenes (numero, cliente_id, conductor_id, secuencia) values
 ('ES-001', 1, 1, 1), ('ES-002', 2, 1, 2), ('ES-003', 3, 1, 3),
 ('ES-004', 4, 2, 1), ('ES-005', 5, 2, 2);

insert into uis.detalle_orden (orden_id, producto_id, cantidad) values
 (1, 1, 48), (1, 2, 24),
 (2, 3, 30),
 (3, 1, 120), (3, 4, 40),
 (4, 5, 25), (4, 6, 50),
 (5, 3, 20), (5, 2, 10), (5, 4, 5);

notify pgrst, 'reload schema';
