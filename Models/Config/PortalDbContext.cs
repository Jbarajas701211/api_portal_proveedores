using Microsoft.EntityFrameworkCore;
using ApiProveedores.Models;
using ApiProveedores.Dto.Auth;
using static Grpc.Core.Metadata;
using ApiProveedores.Models.Factura;

public class PortalDbContext : DbContext
{
    public PortalDbContext(DbContextOptions<PortalDbContext> options) : base(options) { }
    public DbSet<Proveedor> Proveedores { get; set; }
    public DbSet<Usuario> Usuarios { get; set; }
    public DbSet<UsuarioRol> UsuarioRol { get; set; }
    public DbSet<Rol> Rol { get; set; }
    public DbSet<UsuarioEmpresa> UsuarioEmpresa { get; set; }
    public DbSet<TraceUsuario> TraceUsuarios { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<Notificacion> Notificaciones { get; set; }
    public DbSet<NotificacionUsuario> NotificacionesUsuarios { get; set; }
    public DbSet<DiaNoLaborable> DiasNoLaborables { get; set; }
    public DbSet<ParametroSistema> ParametrosSistema { get; set; }
    public DbSet<ProveedorEmpresa> ProveedorEmpresa { get; set; }        // <-- navegación
    public DbSet<Documento> Documento { get; set; }
    public DbSet<ProveedorDocumento> ProveedorDocumento { get; set; }
    public DbSet<Empresa> Empresa { get; set; }
    public DbSet<OrdenCompra> OrdenesCompras => Set<OrdenCompra>();
    public DbSet<Recepcion> Recepciones => Set<Recepcion>();
    public DbSet<RecepcionDetalle> RecepcionDetalles => Set<RecepcionDetalle>();
    public DbSet<FacturaRecepcion> FacturasRecepcion => Set<FacturaRecepcion>();
    public DbSet<Factura> Facturas => Set<Factura>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("portal_proveedores");

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
            entity.Property(e => e.Faltante).HasColumnName("faltante");
            entity.Property(e => e.PorcentajeFaltante).HasColumnName("porcentaje_faltante");
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
            entity.Property(e => e.Descripcion).HasColumnName("description");
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

            entity.HasMany(e => e.UsuarioEmpresas)
                .WithOne(p => p.Empresa)
                .HasForeignKey(e => e.IdEmpresa);

            entity.HasMany(entity => entity.ProveedorEmpresa)
                .WithOne(pe => pe.Empresa)
                .HasForeignKey(pe => pe.IdEmpresa)
                .OnDelete(DeleteBehavior.Cascade);
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
                .WithMany(r => r.UsuarioRoles)
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

            entity.HasOne(e => e.Usuario)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(e => e.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);
        });


        modelBuilder.Entity<ParametroSistema>(entity =>
        {
            entity.ToTable("parametros", "portal_proveedores");

            entity.HasKey(e => e.IdParametro);

            entity.Property(e => e.IdParametro)
                .HasColumnName("id")
                .IsRequired();

            entity.Property(e => e.Valor)
                .HasColumnName("valor")
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(e => e.Descripcion)
                .HasColumnName("descripcion")
                .HasColumnType("text");

            entity.Property(e => e.UnidadMedida)
                .HasColumnName("unidad_medida")
                .HasColumnType("text");

            entity.Property(e => e.Notificacion)
                .HasColumnName("notificacion");

            entity.Property(e => e.Modificacion)
                .HasColumnName("modificacion")
                .HasColumnType("timestamp with time zone");

            // Relación con Usuario
            entity.Property(e => e.IdUsuario)
                .HasColumnName("id_usuario");

            entity.HasOne(e => e.Usuario)
                .WithMany()
                .HasForeignKey(e => e.IdUsuario)
                .OnDelete(DeleteBehavior.Restrict);


        });

        modelBuilder.Entity<Documento>(entity =>
        {
            entity.ToTable("documentos", "portal_proveedores");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id_documento");
            entity.Property(e => e.Tipo).HasColumnName("tipo").HasMaxLength(50);
            entity.Property(e => e.Descripcion).HasColumnName("descripcion").HasColumnType("text");
        });

        modelBuilder.Entity<ProveedorDocumento>(entity =>
        {
            entity.ToTable("proveedor_documento", "portal_proveedores");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id_relacion_pd");
            entity.Property(e => e.IdProveedor).HasColumnName("id_proveedor");
            entity.Property(e => e.DocumentoId).HasColumnName("id_documento");
            entity.Property(e => e.Opcional).HasColumnName("opcional");
            entity.HasOne(e => e.Proveedor)
                  .WithMany(d => d.ProveedorDocumento)
                  .HasForeignKey(e => e.IdProveedor)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Documento)
                  .WithMany()
                  .HasForeignKey(e => e.DocumentoId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrdenCompra>(entity =>
        {
            entity.ToTable("ordenes_compra", "portal_proveedores");
            entity.HasKey(e => e.IdOrdenCompra);
            entity.Property(e => e.IdOrdenCompra).HasColumnName("id_orden_compra");
            entity.Property(e => e.ErpOrigen).HasColumnName("erp_origen");
            entity.Property(e => e.IdExterno).HasColumnName("id_externo");
            entity.Property(e => e.Folio).HasColumnName("folio");
            entity.Property(e => e.FechaOc).HasColumnName("fecha_oc");
            entity.Property(e => e.Moneda).HasColumnName("moneda");
            entity.Property(e => e.Total).HasColumnName("total").HasColumnType("decimal(18,2)");
            entity.Property(e => e.ProveedorId).HasColumnName("proveedor_id");
            entity.Property(e => e.ProveedorNombre).HasColumnName("proveedor_nombre");
            entity.Property(e => e.ProveedorRfc).HasColumnName("proveedor_rfc");
            entity.Property(e => e.Sociedad).HasColumnName("sociedad");
            entity. Property(e => e.Subsidiaria).HasColumnName("subsidiaria");

            entity.HasIndex(e => new { e.ErpOrigen, e.IdExterno })
            .IsUnique()
            .HasDatabaseName("uq_oc");

            entity.HasMany(e => e.Recepciones)
                  .WithOne(r => r.OrdenCompra)
                  .HasForeignKey(r => r.IdOrdenCompra)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Recepcion>(entity =>
        {
            entity.ToTable("recepciones", "portal_proveedores");
            entity.HasKey(e => e.IdRecepcion);
            entity.Property(e => e.IdRecepcion).HasColumnName("id_recepcion");
            entity.Property(e => e.IdOrdenCompra).HasColumnName("id_orden_compra");
            entity.Property(e => e.ErpOrigen).HasColumnName("erp_origen");
            entity.Property(e => e.IdExterno).HasColumnName("id_externo");
            entity.Property(e => e.Folio).HasColumnName("folio");
            entity.Property(e => e.FechaRecepcion).HasColumnName("fecha_recepcion");
            entity.Property(e => e.FechaContabilizacion).HasColumnName("fecha_contabilizacion");
            entity.Property(e => e.Moneda).HasColumnName("moneda");
            entity.Property(e => e.Subtotal).HasColumnName("subtotal").HasColumnType("decimal(18,2)");
            entity.Property(e => e.Total).HasColumnName("total").HasColumnType("decimal(18,2)");
            entity.Property(e => e.Estado).HasColumnName("estado");
            entity.Property(e => e.UsuarioCreacion).HasColumnName("usuario_creacion");
            entity.Property(e => e.ProveedorId).HasColumnName("proveedor_id");
            entity.Property(e => e.ProveedorNombre).HasColumnName("proveedor_nombre");
            entity.Property(e => e.ProveedorRfc).HasColumnName("proveedor_rfc");
            entity.Property(e => e.Sociedad).HasColumnName("sociedad");
            entity.Property(e => e.Centro).HasColumnName("centro");
            entity.Property(e => e.Cantidad).HasColumnName("cantidad").HasColumnType("decimal(18,2)");

            entity.HasIndex(e => new { e.ErpOrigen, e.IdExterno })
                .IsUnique()
                .HasDatabaseName("uq_recepcion");

            entity.HasMany(e => e.Detalles)
                  .WithOne(d => d.Recepcion)
                  .HasForeignKey(d => d.IdRecepcion)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RecepcionDetalle>(entity =>
        {
            entity.ToTable("recepcion_detalle", "portal_proveedores");
            entity.HasKey(e => e.IdDetalle);
            entity.Property(e => e.IdDetalle).HasColumnName("id_detalle");
            entity.Property(e => e.IdRecepcion).HasColumnName("id_recepcion");
            entity.Property(e => e.Linea).HasColumnName("linea");
            entity.Property(e => e.Cantidad).HasColumnName("cantidad").HasColumnType("decimal(18,2)");
            entity.Property(e => e.Total).HasColumnName("total").HasColumnType("decimal(18,2)");

            entity.HasIndex(e => new { e.IdRecepcion, e.Linea })
                .IsUnique()
                .HasDatabaseName("uq_detalle");

        });

        modelBuilder.Entity<FacturaRecepcion>(entity =>
        {
            entity.ToTable("factura_recepcion", "portal_proveedores");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.FacturaId).HasColumnName("id_factura");
            entity.Property(e => e.RecepcionId).HasColumnName("id_recepcion");
            entity.HasOne(e => e.Factura)
                  .WithMany(d => d.FacturaRecepcion)
                  .HasForeignKey(e => e.FacturaId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Recepcion)
                  .WithMany(r => r.FacturaRecepcion)
                  .HasForeignKey(e => e.RecepcionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Factura>(entity =>
        {
            entity.ToTable("facturas", "portal_proveedores");
            entity.HasKey(e => e.IdFactura);
            entity.Property(e => e.IdFactura).HasColumnName("id_factura");
            entity.Property(e => e.IdProveedor).HasColumnName("id_proveedor");
            entity.Property(e => e.IdEmpresa).HasColumnName("id_empresa");
            entity.Property(e => e.TipoDeComprobante).HasColumnName("tipo_de_comprobante");
            entity.Property(e => e.EstatusFactura).HasColumnName("estatus_factura");
            entity.Property(e => e.FolioOrigen).HasColumnName("folio_origen");
            entity.Property(e => e.Folio).HasColumnName("folio");
            entity.Property(e => e.Serie).HasColumnName("serie");
            entity.Property(e => e.Uuid).HasColumnName("uuid");
            entity.Property(e => e.Motivo).HasColumnName("motivo");
            entity.Property(e => e.HayEvidencia).HasColumnName("hay_evidencia");
            entity.Property(e => e.FechaAlta).HasColumnName("fecha_alta");
            entity.Property(e => e.FechaFactura).HasColumnName("fecha_factura");
            entity.Property(e => e.Subtotal).HasColumnName("subtotal");
            entity.Property(e => e.CdTotal).HasColumnName("cd_total");
            entity.Property(e => e.Total).HasColumnName("total");
            entity.Property(e => e.MontoDeRecepcion).HasColumnName("monto_de_recepcion");
            entity.Property(e => e.CorreoElectronico).HasColumnName("correo_electronico");
            entity.Property(e => e.Xml).HasColumnName("xml").HasColumnType("text");
            entity.Property(e => e.RepresentacionGrafica).HasColumnName("representacion_grafica").HasColumnType("text");
            entity.Property(e => e.UnidadNegocio).HasColumnName("unidad_negocio");
            entity.Property(e => e.NoOrdenCompra).HasColumnName("no_orden_compra");
            entity.Property(e => e.NoRecepcion).HasColumnName("no_recepcion");
            entity.Property(e => e.VersionCfdi).HasColumnName("version_cfdi");
            entity.Property(e => e.Ieps).HasColumnName("ieps").HasColumnType("decimal(18,2)");
            entity.Property(e => e.FechaRegistro).HasColumnName("fecha_registro");
            entity.Property(e => e.Iva).HasColumnName("iva").HasColumnType("decimal(18,2)");
            entity.Property(e => e.FolioErp).HasColumnName("folio_erp");
            entity.Property(e => e.FechaContabilizacion).HasColumnName("fecha_contabilizacion");
            entity.Property(e => e.FechaCreacion).HasColumnName("fecha_creacion");
            entity.Property(e => e.FechaModificacion).HasColumnName("fecha_modificacion");

            // Relación Factura ↔ FacturaRecepcion definida en modelBuilder.Entity<FacturaRecepcion>
        });

    }

}
