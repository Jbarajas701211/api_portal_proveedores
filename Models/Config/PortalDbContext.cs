
using Microsoft.EntityFrameworkCore;
using ApiProveedores.Models;
using ApiProveedores.Dto.Auth;
using static Grpc.Core.Metadata;

public class PortalDbContext : DbContext
{
    public PortalDbContext(DbContextOptions<PortalDbContext> options) : base(options) { }
    public DbSet<Proveedor> Proveedores { get; set; }
    public DbSet<Usuario> Usuarios { get; set; }
    public DbSet<UsuarioRol> UsuarioRol { get; set; }
    public DbSet<UsuarioEmpresa> UsuarioEmpresa { get; set; }
    public DbSet<TraceUsuario> TraceUsuarios { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<Notificacion> Notificaciones { get; set; }
    public DbSet<NotificacionUsuario> NotificacionesUsuarios { get; set; }
    public DbSet<CapacidadCdOrigen> CapacidadCdOrigen { get; set; }
    public DbSet<CapacidadResumenCd> CapacidadResumenCd { get; set; }
    public DbSet<CapacidadUso> CapacidadUso { get; set; }
    public DbSet<Cita> Citas { get; set; }
    public DbSet<Orden> Ordenes { get; set; }
    public DbSet<DiaNoLaborable> DiasNoLaborables { get; set; }
    public DbSet<Tienda> Tiendas { get; set; }
    public DbSet<ParametroSistema> ParametrosSistema { get; set; }
    public DbSet<CentroDistribucion> CentrosDistribucion { get; set; }
    public DbSet<CitaDetalle> CitasDetalle { get; set; }
    public DbSet<OrdenCantidadTeorica> OrdenCantidadTeorica { get; set; }
    public DbSet<CatalogoOrigenCapacidad> CatalogoOrigenCapacidad => Set<CatalogoOrigenCapacidad>();
    public DbSet<CitaEntrega> CitasEntregas => Set<CitaEntrega>();
    public DbSet<CitaIncidencia> CitasIncidencias { get; set; }
    public DbSet<CatalogoIncidencia> CatalogoIncidencias { get; set; }
    public DbSet<DetalleOrden> DetalleOrdenes { get; set; }
    public DbSet<CitaLote> CitasLotes { get; set; }
    public DbSet<Devolucion> Devoluciones { get; set; }
    public DbSet<CitaSeguimiento> CitasSeguimiento => Set<CitaSeguimiento>();
    public DbSet<KpiProveedorResult> KpiProveedores => Set<KpiProveedorResult>();
    public DbSet<OrdenSeguimiento> OrdenesSeguimiento => Set<OrdenSeguimiento>();
    public DbSet<ProveedorEmpresa> ProveedorEmpresa { get; set; }        // <-- navegación

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("portal_proveedores");


        modelBuilder.Entity<CapacidadCdOrigen>(entity =>
        {
            entity.ToTable("capacidad_cd_origen");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Cd).HasColumnName("cd").HasMaxLength(1).IsRequired();
            entity.Property(e => e.Origen).HasColumnName("origen").HasMaxLength(10).IsRequired();
            entity.Property(e => e.CapacidadMaxima).HasColumnName("capacidad_maxima");
            entity.Property(e => e.RegistradoEn).HasColumnName("registrado_en");

            entity.HasIndex(e => new { e.Cd, e.Origen }).IsUnique();
        });

        modelBuilder.Entity<CapacidadResumenCd>(entity =>
        {
            entity.ToTable("capacidad_resumen_cd");

            entity.HasKey(e => new { e.Cd, e.Origen, e.Fecha });

            entity.Property(e => e.Cd).HasColumnName("cd").HasMaxLength(1);
            entity.Property(e => e.Origen).HasColumnName("origen").HasMaxLength(10);
            entity.Property(e => e.Fecha).HasColumnName("fecha");
            entity.Property(e => e.CapacidadMaxima).HasColumnName("capacidad_maxima");
            entity.Property(e => e.CapacidadUtilizada).HasColumnName("capacidad_utilizada");
            entity.Property(e => e.CapacidadDisponible).HasColumnName("capacidad_disponible");
            entity.Property(e => e.ActualizadoEn).HasColumnName("actualizado_en");
        });

        modelBuilder.Entity<CapacidadUso>(entity =>
        {
            entity.ToTable("capacidad_uso");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Cd).HasColumnName("cd").HasMaxLength(1);
            entity.Property(e => e.Origen).HasColumnName("origen").HasMaxLength(10);
            entity.Property(e => e.Fecha).HasColumnName("fecha");
            entity.Property(e => e.CantidadAsignada).HasColumnName("cantidad_asignada");

            entity.Property(e => e.Tipo)
                .HasColumnName("tipo")
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(e => e.RegistradoEn).HasColumnName("registrado_en").HasColumnType("timestamp without time zone");
            entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");

            entity.HasOne(e => e.Usuario)
                  .WithMany()
                  .HasForeignKey(e => e.UsuarioId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne<CapacidadCdOrigen>()
                  .WithMany(c => c.Usos)
                  .HasForeignKey(e => new { e.Cd, e.Origen })
                  .HasPrincipalKey(c => new { c.Cd, c.Origen })
                  .OnDelete(DeleteBehavior.NoAction)
                  .HasConstraintName("fk_capacidad_uso_cd_origen");
        });




        modelBuilder.Entity<Proveedor>(entity =>
        {
            entity.ToTable("proveedores", "portal_proveedores");

            entity.HasKey(e => e.Id_proveedor);

            entity.Property(e => e.Id_proveedor).HasColumnName("id_proveedor");
            entity.Property(e => e.Nombre).HasColumnName("nombre");
            entity.Property(e => e.Rfc).HasColumnName("rfc");
            entity.Property(e => e.VendorId).HasColumnName("vendor_id");
            entity.Property(e => e.Estatus).HasColumnName("estatus");
            entity.Property(e => e.Sobrante).HasColumnName("sobrante");
            entity.Property(e => e.PorcentajeSobrante).HasColumnName("porcentaje_sobrante");
            entity.Property(e => e.AplicarTolerancia).HasColumnName("aplicar_tolerancia");
            entity.Property(e => e.IdCategoria).HasColumnName("id_categoria");
            entity.Property(e => e.AcreedorSinXml).HasColumnName("acreedor_sin_xml");
            entity.Property(e => e.AplicarToleranciaCategoria).HasColumnName("aplicar_tolerancia_categoria");
            entity.Property(e => e.EmailProveedor).HasColumnName("email_proveedor");
            entity.Property(e => e.DocFiscal).HasColumnName("doc_fiscal");
            entity.Property(e => e.Factura).HasColumnName("factura");
            entity.Property(e => e.Recepcion).HasColumnName("recepcion");
            entity.Property(e => e.Origen).HasColumnName("origen");
            entity.Property(e => e.RazonSocial).HasColumnName("razon_social");
            entity.Property(e => e.EntityId).HasColumnName("entity_id");

            entity.HasMany(e => e.ProveedorEmpresa)
                .WithOne(pe => pe.Proveedor)
                .HasForeignKey(pe => pe.IdProveedor)
                .OnDelete(DeleteBehavior.Cascade);

        });

        modelBuilder.Entity<ProveedorEmpresa>(b =>
        {
            b.ToTable("proveedor_empresa", "portal_proveedores");
            b.HasKey(x => x.IdRelacionPE);
            b.Property(x => x.IdRelacionPE).HasColumnName("id_relacion_pe");
            b.Property(x => x.IdProveedor).HasColumnName("id_proveedor");
            b.Property(x => x.IdEmpresa).HasColumnName("id_empresa");

            b.HasOne(x => x.Proveedor).WithMany(p => p.ProveedorEmpresa).HasForeignKey(x => x.IdProveedor);
            b.HasOne(x => x.Empresa).WithMany(e => e.ProveedorEmpresa).HasForeignKey(x => x.IdEmpresa);
        });


        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.ToTable("usuario", "portal_proveedores");

            entity.HasKey(e => e.IdUsuario);

            entity.Property(e => e.IdUsuario).HasColumnName("id_usuario");
            entity.Property(e => e.usuario).HasColumnName("usuario");
            entity.Property(e => e.Password).HasColumnName("password");
            entity.Property(e => e.Nombre).HasColumnName("nombre");
            entity.Property(e => e.ApellidoPaterno).HasColumnName("apellido_paterno");
            entity.Property(e => e.ApellidoMaterno).HasColumnName("apellido_materno");
            entity.Property(e => e.CorreoElectronico).HasColumnName("correo_electronico");
            entity.Property(e => e.Estatus).HasColumnName("estatus");


            // Relación con rol
            entity.HasMany(e => e.UsuarioRoles)
                .WithOne(p => p.Usuario)
                .HasForeignKey(e => e.IdUsuario);

            entity.HasMany(e => e.UsuarioEmpresas)
                .WithOne()
                .HasForeignKey(e => e.IdUsuario);

        });

        modelBuilder.Entity<Rol>(entity =>
        {
            entity.ToTable("rol", "portal_proveedores");
            entity.HasKey(e => e.IdRol);
            entity.Property(e => e.IdRol).HasColumnName("id_rol");
            entity.Property(e => e.Descripcion).HasColumnName("descripcion");

            entity.HasMany(e => e.UsuarioRoles)
                .WithOne(p => p.Rol)
                .HasForeignKey(e => e.IdRol);
        });

        modelBuilder.Entity<Empresa>(entity =>
        {
            entity.ToTable("empresas", "portal_proveedores");
            entity.HasKey(e => e.IdEmpresa);
            entity.Property(e => e.IdEmpresa).HasColumnName("id_empresa");
            entity.Property(e => e.Nombre).HasColumnName("nombre");
            entity.Property(e => e.Rfc).HasColumnName("rfc");
            entity.Property(e => e.Estatus).HasColumnName("estatus");
            entity.Property(e => e.Unidad).HasColumnName("unidad");
            entity.Property(e => e.Sobrante).HasColumnName("sobrante");
            entity.Property(e => e.PorcentajeSobrante).HasColumnName("porcentaje_sobrante");
            entity.Property(e => e.Faltante).HasColumnName("faltante");
            entity.Property(e => e.PorcentajeFaltante).HasColumnName("porcentaje_faltante");
            entity.Property(e => e.AplicarTolerancia).HasColumnName("aplicar_tolerancia");

            entity.HasMany(e => e.UsuarioEmpresas)
                .WithOne(p => p.Empresa)
                .HasForeignKey(e => e.IdEmpresa);
        });

        modelBuilder.Entity<UsuarioEmpresa>(entity =>
        {
            entity.ToTable("usuario_empresa", "portal_proveedores");
            entity.HasKey(e => e.IdRelacionUE);
            entity.Property(e => e.IdRelacionUE).HasColumnName("id_relacion_ue");
            entity.Property(e => e.IdUsuario).HasColumnName("id_usuario");
            entity.Property(e => e.IdEmpresa).HasColumnName("id_empresa");
            entity.HasOne(e => e.Usuario)
                .WithMany(u => u.UsuarioEmpresas)
                .HasForeignKey(e => e.IdUsuario)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Empresa)
                .WithMany()
                .HasForeignKey(e => e.IdEmpresa)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UsuarioRol>(entity =>
        {
            entity.ToTable("usuario_rol", "portal_proveedores");
            entity.HasKey(e => e.IdRelacionUr);
            entity.Property(e => e.IdRelacionUr).HasColumnName("id_relacion_ur");
            entity.Property(e => e.IdUsuario).HasColumnName("id_usuario");
            entity.Property(e => e.IdRol).HasColumnName("id_rol");
            entity.HasOne(e => e.Usuario)
                .WithMany(u => u.UsuarioRoles)
                .HasForeignKey(e => e.IdUsuario)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Rol)
                .WithMany()
                .HasForeignKey(e => e.IdRol)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TraceUsuario>(entity =>
        {
            entity.ToTable("trace_usuarios", "portal_proveedores");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.IdUsuario).HasColumnName("id_usuario");
            entity.Property(e => e.Evento)
                  .HasColumnName("evento")
                  .HasMaxLength(50)
                  .HasConversion<string>();
            entity.Property(e => e.Descripcion).HasColumnName("descripcion");
            entity.Property(e => e.RegistradoEn)
                  .HasColumnName("registrado_en")
                  .HasColumnType("timestamp with time zone");

            entity.HasOne(e => e.Usuario)
                  .WithMany(u => u.TraceUsuarios)
                  .HasForeignKey(e => e.IdUsuario)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("refresh_tokens", "portal_proveedores");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UsuarioId).HasColumnName("id_usuario");
            entity.Property(e => e.Token).HasColumnName("token").IsRequired();
            entity.Property(e => e.CreadoEn).HasColumnName("creado_en");
            entity.Property(e => e.ExpiraEn).HasColumnName("expira_en");
            entity.Property(e => e.RevocadoEn).HasColumnName("revocado_en");
            entity.Property(e => e.ReemplazadoPor).HasColumnName("reemplazado_por");

            //entity.HasOne(e => e.Usuario)
            //    .WithMany(u => u.RefreshTokens)
            //    .HasForeignKey(e => e.UsuarioId)
            //    .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Notificacion>(entity =>
        {
            entity.ToTable("notificaciones", "portal");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Fecha).HasColumnName("fecha").IsRequired();
            entity.Property(e => e.Hora).HasColumnName("hora").IsRequired();
            entity.Property(e => e.Titulo).HasColumnName("titulo").HasMaxLength(255).IsRequired();
            entity.Property(e => e.Tag).HasColumnName("tag").HasMaxLength(255).IsRequired();
            entity.Property(e => e.Detalle).HasColumnName("detalle");
            entity.Property(e => e.CreadoEn).HasColumnName("creado_en").HasColumnType("timestamp without time zone");
            entity.Property(x => x.MetaData).HasColumnName("meta_data").HasColumnType("json");
            entity.HasMany(e => e.NotificacionesUsuarios)
                  .WithOne(nu => nu.Notificacion)
                  .HasForeignKey(nu => nu.NotificacionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<NotificacionUsuario>(entity =>
        {
            entity.ToTable("notificaciones_usuarios", "portal");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.NotificacionId).HasColumnName("notificacion_id");
            entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");
            entity.Property(e => e.Leida).HasColumnName("leida").HasDefaultValue(false);
            entity.Property(e => e.LeidaEn).HasColumnName("leida_en");

            entity.HasIndex(e => new { e.NotificacionId, e.UsuarioId }).IsUnique();

            entity.HasOne(e => e.Notificacion)
                  .WithMany(n => n.NotificacionesUsuarios)
                  .HasForeignKey(e => e.NotificacionId)
                  .OnDelete(DeleteBehavior.Cascade);

            //entity.HasOne(e => e.Usuario)
            //      .WithMany(u => u.NotificacionesUsuarios)
            //      .HasForeignKey(e => e.UsuarioId)
            //      .OnDelete(DeleteBehavior.Cascade);
        });




        modelBuilder.Entity<Cita>(entity =>
        {
            entity.ToTable("citas");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");

            entity.Property(e => e.Lote).HasColumnName("lote").HasMaxLength(20);
            entity.Property(e => e.Folio).HasColumnName("folio").HasMaxLength(30);
            entity.Property(e => e.FechaCita).HasColumnName("fecha_cita").IsRequired();
            entity.Property(e => e.Cd).HasColumnName("cd").HasMaxLength(1).IsRequired();
            entity.Property(e => e.ProveedorId).HasColumnName("proveedor_id").IsRequired();
            entity.Property(e => e.MarcadaParaSolicitarAutorizacion).HasColumnName("para_solicitar_autorizacion");

            entity.Property(e => e.NombreSolicitante).HasColumnName("nombre_solicitante").HasMaxLength(100).IsRequired();
            entity.Property(e => e.HoraCita).HasColumnName("hora_cita").IsRequired();
            entity.Property(e => e.FechaSolicitud).HasColumnName("fecha_solicitud").IsRequired();
            entity.Property(e => e.Estado).HasColumnName("estado").HasMaxLength(30).HasDefaultValue("CREADA");

            entity.Property(e => e.NombreChofer).HasColumnName("nombre_chofer").HasMaxLength(100);
            entity.Property(e => e.NombreAyudante).HasColumnName("nombre_ayudante").HasMaxLength(100);
            entity.Property(e => e.TipoUnidad).HasColumnName("tipo_unidad").HasMaxLength(50);
            entity.Property(e => e.Placas).HasColumnName("placas").HasMaxLength(20);
            entity.Property(e => e.LineaTransportista).HasColumnName("linea_transportista").HasMaxLength(100);
            entity.Property(e => e.Observaciones).HasColumnName("observaciones");
            entity.Property(e => e.CreadoEn)
                .HasColumnName("creado_en")
                .HasColumnType("timestamp without time zone");

            entity.Property(e => e.PublicId)
                  .HasColumnName("public_id")
                  .HasDefaultValueSql("gen_random_uuid()");

            entity.HasOne<Proveedor>(e => e.Proveedor)
                  .WithMany()
                  .HasForeignKey(e => e.ProveedorId)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(c => c.Entrega).WithOne().HasForeignKey<CitaEntrega>(x => x.CitaId);

            entity.Property(e => e.RegistradoPorId).HasColumnName("registrado_por_id");

            entity.HasOne(e => e.RegistradoPor)
                  .WithMany()
                  .HasForeignKey(e => e.RegistradoPorId)
                  .OnDelete(DeleteBehavior.Restrict)
                  .HasConstraintName("fk_citas_registrado_por");

            entity.HasIndex(e => e.RegistradoPorId)
                .HasDatabaseName("ix_citas_registrado_por_id");

        });


        modelBuilder.Entity<Orden>(entity =>
        {
            entity.HasNoKey();
            entity.ToView("vw_pc_tran_po_cantidades", "portal");

            entity.HasKey(e => e.Nopedido);

            entity.Property(e => e.Nopedido)
                .HasColumnName("nopedido")
                .HasMaxLength(12)
                .IsRequired();

            entity.Property(e => e.Fechapedido)
                .HasColumnName("fechapedido")
                .IsRequired();

            entity.Property(e => e.Fechavenci)
                .HasColumnName("fechavenci")
                .IsRequired();

            entity.Property(e => e.Origen)
                .HasColumnName("origen")
                .HasMaxLength(3)
                .IsRequired();

            entity.Property(e => e.Cveprov)
                .HasColumnName("cveprov")
                .HasMaxLength(10)
                .IsRequired();

            entity.Property(e => e.Status)
                .HasColumnName("status")
                .IsRequired();

            entity.Property(e => e.Cd)
                .HasColumnName("cd")
                .HasMaxLength(1)
                .IsRequired();

            entity.Property(e => e.CdDesc)
                .HasColumnName("cd_desc")
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(e => e.Importe)
                .HasColumnName("importe")
                .HasPrecision(20, 4);

            entity.Property(e => e.Comprador)
                .HasColumnName("comprador")
                .HasPrecision(4, 0)
                .IsRequired();

            entity.Property(e => e.Cantitotal)
                .HasColumnName("cantitotal")
                .HasPrecision(12, 4);

            entity.Property(e => e.Unnego)
                .HasColumnName("unnego")
                .HasMaxLength(1)
                .IsRequired();

            entity.Property(e => e.Cvecateg)
                .HasColumnName("cvecateg")
                .HasPrecision(4, 0)
                .IsRequired();

            entity.Property(e => e.Notacanc)
                .HasColumnName("notacanc");

            entity.Property(e => e.Asn)
                .HasColumnName("asn")
                .HasMaxLength(10);

            entity.Property(e => e.Basico)
                .HasColumnName("basico")
                .HasMaxLength(4);

            entity.Property(e => e.TipoOrden)
                .HasColumnName("tipo_orden")
                .HasMaxLength(4)
                .IsRequired();

            entity.Property(e => e.CitaInd)
                .HasColumnName("cita_ind")
                .HasMaxLength(1)
                .IsRequired();

            entity.Property(e => e.OFlag)
                .HasColumnName("o_flag")
                .HasMaxLength(2);

            entity.Property(e => e.OExMsg)
                .HasColumnName("o_ex_msg")
                .HasMaxLength(255);

            entity.Property(e => e.ODttm)
                .HasColumnName("o_dttm");

            entity.Property(e => e.Bloqueado)
                .HasColumnName("bloqueado");

            entity.Property(e => e.CveAlmacen)
                .HasColumnName("cvealmacen");

            entity.Property(e => e.CantidadTeorica)
                .HasColumnName("cantidad_teorica");

            entity.Property(e => e.CantidadEntregada)
                .HasColumnName("cantidad_entregada");

            entity.Property(e => e.CantidadFaltante)
                .HasColumnName("cantidad_faltante");
        });

        modelBuilder.Entity<DiaNoLaborable>(entity =>
        {
            entity.ToTable("catalogo_dias_no_laborables", "portal");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id");

            entity.Property(e => e.Fecha)
                .HasColumnName("fecha")
                .HasColumnType("date")
                .IsRequired();

            entity.Property(e => e.Descripcion)
                .HasColumnName("descripcion")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.CreadoEn)
                .HasColumnName("creado_en")
                .HasColumnType("timestamp without time zone")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<Tienda>(entity =>
        {
            entity.ToTable("tiendas", "portal");

            entity.HasKey(e => e.CcCntrCsto);

            entity.Property(e => e.CcCntrCsto)
                .HasColumnName("cc_cntr_csto")
                .HasColumnType("numeric(10,0)");

            entity.Property(e => e.CcScrs)
                .HasColumnName("cc_scrs")
                .HasMaxLength(30);

            entity.Property(e => e.CreadoEn)
                .HasColumnName("creado_en")
                .HasColumnType("timestamp without time zone")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.OFlag)
                .HasColumnName("o_flag")
                .HasMaxLength(2);

            entity.Property(e => e.OExMsg)
                .HasColumnName("o_ex_msg")
                .HasMaxLength(255);

            entity.Property(e => e.ODttm)
                .HasColumnName("o_dttm");
        });


        modelBuilder.Entity<ParametroSistema>(entity =>
        {
            entity.ToTable("parametros_sistema", "portal");

            entity.HasKey(e => e.Clave);

            entity.Property(e => e.Clave)
                .HasColumnName("clave")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.Valor)
                .HasColumnName("valor")
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(e => e.Descripcion)
                .HasColumnName("descripcion")
                .HasColumnType("text");

            entity.Property(e => e.ActualizadoEn)
                .HasColumnName("actualizado_en")
                .HasColumnType("timestamp without time zone")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });


        modelBuilder.Entity<CentroDistribucion>(entity =>
        {
            entity.ToTable("centros_distribucion");
            entity.HasKey(e => e.Clave).HasName("centros_distribucion_pkey");

            entity.Property(e => e.Clave)
                  .HasColumnName("clave")
                  .HasColumnType("character(1)")
                  .IsRequired();

            entity.Property(e => e.Nombre)
                  .HasColumnName("nombre")
                  .HasMaxLength(100)
                  .IsRequired();

            entity.Property(e => e.Activo)
                  .HasColumnName("activo")
                  .HasDefaultValue(true);
        });


        modelBuilder.Entity<CitaDetalle>(entity =>
        {
            entity.ToTable("citas_detalle");

            entity.HasKey(e => new { e.CitaId, e.Oc })
                  .HasName("citas_detalle_pkey");

            entity.Property(e => e.CitaId)
                  .HasColumnName("cita_id");

            entity.Property(e => e.Oc)
                  .HasColumnName("oc")
                  .HasMaxLength(20)
                  .IsRequired();

            entity.Property(e => e.Origen)
                  .HasColumnName("origen")
                  .HasMaxLength(10)
                  .IsRequired();

            entity.Property(e => e.ClaveAlmacen)
                  .HasColumnName("cve_almacen")
                  .HasMaxLength(10)
                  .IsRequired();

            entity.Property(e => e.CantidadPorCita)
                  .HasColumnName("cantidad_por_cita")
                  .IsRequired();

            entity.Property(e => e.RegistradoEn)
                  .HasColumnName("registrado_en")
                  .HasColumnType("timestamp without time zone")
                  .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.FechaVencimiento)
                  .HasColumnName("fecha_vencimiento")
                  .HasColumnType("date");

            entity.Property(e => e.CantidadTotal)
                  .HasColumnName("cantidad_total");

            entity.HasOne(d => d.Cita)
                  .WithMany(p => p.Detalles)
                  .HasForeignKey(d => d.CitaId)
                  .OnDelete(DeleteBehavior.Cascade)
                  .HasConstraintName("citas_detalle_cita_id_fkey");
        });



        modelBuilder.Entity<OrdenCantidadTeorica>(entity =>
        {
            entity.ToTable("orden_cantidad_teorica");

            entity.HasKey(e => e.Oc)
                  .HasName("orden_cantidad_teorica_pkey");

            entity.Property(e => e.Oc)
                  .HasColumnName("oc")
                  .HasMaxLength(20)
                  .IsRequired();

            entity.Property(e => e.CantidadTeorica)
                  .HasColumnName("cantidad_teorica")
                  .IsRequired();

            entity.Property(e => e.CantidadTotal)
                  .HasColumnName("cantidad_total")
                  .IsRequired();

            entity.Property(e => e.CantidadEntregada)
                  .HasColumnName("cantidad_entregada")
                  .IsRequired();

            entity.Property(e => e.RegistradoEn)
                  .HasColumnName("registrado_en")
                  .HasColumnType("timestamp without time zone")
                  .HasDefaultValueSql("CURRENT_TIMESTAMP")
                  .IsRequired();

            entity.Property(e => e.ActualizadoEn)
                  .HasColumnName("actualizado_en")
                  .HasColumnType("timestamp without time zone")
                  .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });



        modelBuilder.Entity<CatalogoOrigenCapacidad>(entity =>
        {
            entity.ToTable("catalogo_origen_capacidad", "portal");

            entity.HasKey(e => e.Clave)
             .HasName("catalogo_origen_capacidad_pkey");

            entity.Property(e => e.Clave)
                .HasColumnName("clave")
                .HasMaxLength(10)
                .IsRequired();

            entity.Property(e => e.Descripcion)
                .HasColumnName("descripcion")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.Activo)
                .HasColumnName("activo")
                .HasDefaultValue(true);

            entity.Property(e => e.ClaveOrigen)
                .HasColumnName("clave_origen")
                .HasMaxLength(5);
        });


        modelBuilder.HasPostgresEnum<EntregaEstatus>("portal", "cita_entrega_estatus");
        modelBuilder.Entity<CitaEntrega>(entity =>
        {
            entity.ToTable("citas_entregas", "portal");
            entity.HasKey(x => x.CitaId);
            entity.Property(x => x.CitaId).HasColumnName("cita_id");
            entity.Property(x => x.FechaEntrega).HasColumnName("fecha_entrega");
            entity.Property(x => x.HoraRecepcion).HasColumnName("hora_recepcion");
            entity.Property(x => x.CantidadEntregada).HasColumnName("cantidad_entregada");

            entity.Property(x => x.Estatus).HasColumnName("estatus").HasMaxLength(10)
                .HasConversion<string>();


            entity.Property(x => x.Anden).HasColumnName("anden");
            entity.Property(x => x.Acuse).HasColumnName("acuse");
            entity.Property(x => x.Notas).HasColumnName("notas");

            entity.Property(e => e.RegistradoEn)
                  .HasColumnName("registrado_en")
                  .HasColumnType("timestamp without time zone")
                  .HasDefaultValueSql("CURRENT_TIMESTAMP")
                  .IsRequired();
        });


        modelBuilder.Entity<CatalogoIncidencia>(entity =>
        {
            entity.ToTable("catalogo_incidencias", "portal");
            entity.HasKey(e => e.Clave).HasName("catalogo_incidencias_pkey");

            entity.Property(e => e.Clave).HasColumnName("clave");
            entity.Property(e => e.Descripcion).HasColumnName("descripcion");
        });


        modelBuilder.Entity<CitaIncidencia>(entity =>
        {
            entity.ToTable("citas_incidencias", "portal");

            entity.HasKey(e => e.Id)
                  .HasName("citas_incidencias_pkey");

            entity.Property(e => e.Id)
                  .HasColumnName("id")
                  .ValueGeneratedOnAdd()
                  .UseIdentityByDefaultColumn();

            entity.Property(e => e.CitaId)
                  .HasColumnName("cita_id");

            entity.Property(e => e.ArchivoCargado)
                  .HasColumnName("archivo_cargado");

            entity.Property(e => e.HashMasivo)
                  .HasColumnName("hash_masivo")
                  .HasMaxLength(100);

            entity.Property(e => e.Observacion)
                  .HasColumnName("observacion")
                  .HasColumnType("text");

            entity.Property(e => e.RegistradoEn)
                  .HasColumnName("registrado_en")
                  .HasColumnType("timestamp without time zone")
                  .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.RutaArchivo)
                  .HasColumnName("ruta_archivo")
                  .HasMaxLength(2048);

            entity.HasOne(e => e.Cita)
                  .WithMany(c => c.Incidencias)
                  .HasForeignKey(e => e.CitaId)
                  .OnDelete(DeleteBehavior.Cascade)
                  .HasConstraintName("citas_incidencias_cita_id_fkey");
        });

        modelBuilder.Entity<CitaIncidenciaClave>(entity =>
        {
            entity.ToTable("citas_incidencias_claves", "portal");

            entity.HasKey(e => e.Id)
                  .HasName("citas_incidencias_claves_pkey");

            entity.Property(e => e.Id)
                  .HasColumnName("id")
                  .ValueGeneratedOnAdd()
                  .UseIdentityByDefaultColumn();

            entity.Property(e => e.CitaIncidenciaId)
                  .HasColumnName("cita_incidencia_id");

            entity.Property(e => e.ClaveInc)
                  .HasColumnName("clave_inc");

            entity.HasOne(e => e.CitaIncidencia)
                  .WithMany(ci => ci.Claves)
                  .HasForeignKey(e => e.CitaIncidenciaId)
                  .OnDelete(DeleteBehavior.Cascade)
                  .HasConstraintName("citas_incidencias_claves_cita_incidencia_id_fkey");

            entity.HasOne(e => e.CatalogoIncidencia)
                  .WithMany()
                  .HasForeignKey(e => e.ClaveInc)
                  .OnDelete(DeleteBehavior.Restrict)
                  .HasConstraintName("citas_incidencias_claves_clave_inc_fkey");
        });




        modelBuilder.Entity<DetalleOrden>(entity =>
        {
            entity.ToTable("pc_tran_po_dtl", "sap");
            entity.HasKey(e => new { e.Nopedido, e.Idarticulo, e.Iddetapedi })
                  .HasName("pc_tran_po_dtl_pkey");

            entity.Property(e => e.Nopedido)
                  .HasColumnName("nopedido")
                  .HasMaxLength(12)
                  .IsRequired();

            entity.Property(e => e.Idarticulo)
                  .HasColumnName("idarticulo")
                  .HasMaxLength(25)
                  .IsRequired();

            entity.Property(e => e.Iddetapedi)
                  .HasColumnName("iddetapedi")
                  .IsRequired();

            entity.Property(e => e.Upc)
                  .HasColumnName("upc")
                  .HasMaxLength(25)
                  .IsRequired();

            entity.Property(e => e.Cantidad)
                  .HasColumnName("cantidad")
                  .HasPrecision(12, 4)
                  .IsRequired();

            entity.Property(e => e.Cvetienda)
                  .HasColumnName("cvetienda")
                  .HasMaxLength(25)
                  .IsRequired();

            entity.Property(e => e.Costo)
                  .HasColumnName("costo")
                  .HasPrecision(20, 4)
                  .IsRequired();

            entity.Property(e => e.Descrip)
                  .HasColumnName("descrip")
                  .HasMaxLength(250)
                  .IsRequired();

            entity.Property(e => e.Modelo)
                  .HasColumnName("modelo")
                  .HasMaxLength(250)
                  .IsRequired();

            entity.Property(e => e.Color)
                  .HasColumnName("color")
                  .HasMaxLength(10)
                  .IsRequired();

            entity.Property(e => e.Talla)
                  .HasColumnName("talla")
                  .HasMaxLength(10)
                  .IsRequired();

            entity.Property(e => e.Marca)
                  .HasColumnName("marca")
                  .HasMaxLength(10)
                  .IsRequired();

            entity.Property(e => e.Purchaseord)
                  .HasColumnName("purchaseord");

        });


        modelBuilder.Entity<CitaLote>(entity =>
        {
            entity.ToTable("citas_lotes", "portal");
            entity.HasNoKey();

            entity.HasKey(e => new { e.Lote, e.ProveedorId })
                  .HasName("citas_lotes_pk");

            entity.Property(e => e.Lote)
                  .HasColumnName("lote")
                  .HasMaxLength(20);

            entity.Property(e => e.ProveedorId)
                  .HasColumnName("proveedor_id");

            entity.Property(e => e.FechaCreacion)
                  .HasColumnName("fecha_creacion")
                  .HasColumnType("date");

            entity.HasIndex(e => e.ProveedorId)
                  .HasDatabaseName("ix_citas_lotes_proveedor_id");
        });

        modelBuilder.Entity<Devolucion>(entity =>
        {
            entity.ToTable("devolucion", "portal");

            entity.HasKey(e => e.Id)
                  .HasName("devolucion_pkey");

            entity.Property(e => e.Id)
                  .HasColumnName("id")
                  .ValueGeneratedOnAdd()
                  .UseIdentityByDefaultColumn();

            entity.Property(e => e.ProveedorId)
                  .HasColumnName("proveedor_id")
                  .IsRequired();

            entity.Property(e => e.Cantidad)
                  .HasColumnName("cantidad");

            entity.Property(e => e.NumeroRtv)
                  .HasColumnName("numero_rtv")
                  .HasMaxLength(20);

            entity.Property(e => e.FechaRecoleccion)
                  .HasColumnName("fecha_recoleccion")
                  .HasColumnType("date");

            entity.Property(e => e.CreadoPorId)
                  .HasColumnName("creado_por_id")
                  .IsRequired();

            entity.Property(e => e.CreadoEn)
                  .HasColumnName("creado_en")
                  .HasColumnType("timestamp without time zone")
                  .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.ActualizadoEn)
                  .HasColumnName("actualizado_en")
                  .HasColumnType("timestamp without time zone")
                  .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.CreadoPor)
                  .WithMany()
                  .HasForeignKey(d => d.CreadoPorId)
                  .OnDelete(DeleteBehavior.Restrict)
                  .HasConstraintName("devolucion_creado_por_id_fkey");

            entity.HasOne(d => d.Proveedor)
                  .WithMany()
                  .HasForeignKey(d => d.ProveedorId)
                  .OnDelete(DeleteBehavior.Restrict)
                  .HasConstraintName("devolucion_proveedor_id_fkey");

            entity.HasIndex(e => e.ProveedorId).HasDatabaseName("idx_devolucion_proveedor");
            entity.HasIndex(e => e.CreadoPorId).HasDatabaseName("idx_devolucion_creado_por");
        });


        modelBuilder.Entity<KpiProveedorResult>(entity =>
        {
            entity.HasNoKey();
            entity.ToView(null);

            entity.Property(e => e.ProveedorId).HasColumnName("proveedor_id");

            // citas
            entity.Property(e => e.TotalCitas).HasColumnName("total_citas");
            entity.Property(e => e.CitasEntregadas).HasColumnName("citas_entregadas");
            entity.Property(e => e.CitasCanceladas).HasColumnName("citas_canceladas");
            entity.Property(e => e.CitasReagendadas).HasColumnName("citas_reagendadas");
            entity.Property(e => e.CitasProgramadas).HasColumnName("citas_programadas");
            entity.Property(e => e.EntregasConIncidencias).HasColumnName("entregas_con_incidencias");
            entity.Property(e => e.EntregasSinIncidencias).HasColumnName("entregas_sin_incidencias");
            entity.Property(e => e.CitasFallo).HasColumnName("citas_fallo");

            // ordenes
            entity.Property(e => e.TotalOrdenes).HasColumnName("total_ordenes");
            entity.Property(e => e.OrdenesCompletadas).HasColumnName("ordenes_completadas");
            entity.Property(e => e.OrdenesIncompletas).HasColumnName("ordenes_incompletas");
            entity.Property(e => e.OrdenesCanceladas).HasColumnName("ordenes_canceladas");
            entity.Property(e => e.OrdenesNuevas).HasColumnName("ordenes_nuevas");
            

            // % citas
            entity.Property(e => e.PctEntregadas).HasColumnName("pct_entregadas");
            entity.Property(e => e.PctCanceladas).HasColumnName("pct_canceladas");
            entity.Property(e => e.PctReagendadas).HasColumnName("pct_reagendadas");
            entity.Property(e => e.PctProgramadas).HasColumnName("pct_programadas");
            entity.Property(e => e.PctEntregasConIncidencias).HasColumnName("pct_entregas_con_incidencias");
            entity.Property(e => e.PctOtifSimple).HasColumnName("pct_otif_simple");
            entity.Property(e => e.PctFallo).HasColumnName("pct_fallo");

            // % ordenes
            entity.Property(e => e.PctOrdenesCompletadas).HasColumnName("pct_ordenes_completadas");
            entity.Property(e => e.PctOrdenesIncompletas).HasColumnName("pct_ordenes_incompletas");
            entity.Property(e => e.PctOrdenesCanceladas).HasColumnName("pct_ordenes_canceladas");

            // score / rating
            entity.Property(e => e.ScoreGlobal).HasColumnName("score_global");
            entity.Property(e => e.RatingEstrellas).HasColumnName("rating_estrellas");
        });


        modelBuilder.Entity<CitaSeguimiento>(entity =>
        {
            entity.ToTable("citas_seguimiento", "portal");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CitaId).HasColumnName("cita_id");
            entity.Property(e => e.Evento).HasColumnName("evento").HasMaxLength(50);
            entity.Property(e => e.Notificado).HasColumnName("notificado");
            entity.Property(e => e.FechaNotificacion).HasColumnName("fecha_notificacion");
            entity.Property(e => e.UsuarioModifico).HasColumnName("usuario_modifico").HasMaxLength(255);
            entity.Property(e => e.RegistradoEn).HasColumnName("registrado_en")
                .HasColumnType("timestamp without time zone");
            entity.Property(e => e.EstadoActivo).HasColumnName("estado_activo");
            entity.Property(e => e.ConIncidencias).HasColumnName("con_incidencias");
        });


        modelBuilder.Entity<OrdenSeguimiento>(entity =>
        {
            entity.ToTable("ordenes_seguimiento", "portal");
            entity.HasKey(e => e.Id).HasName("ordenes_seguimiento_pkey");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Nopedido).HasColumnName("nopedido").HasMaxLength(12);
            entity.Property(e => e.Evento).HasColumnName("evento").HasMaxLength(50);
            entity.Property(e => e.Descripcion).HasColumnName("descripcion").HasColumnType("text");
            entity.Property(e => e.RegistradoEn).HasColumnName("registrado_en")
                .HasColumnType("timestamp without time zone");
            entity.Property(e => e.UsuarioModifico).HasColumnName("usuario_modifico");
            entity.Property(e => e.EstadoActivo).HasColumnName("estado_activo");
        });

    }

}
