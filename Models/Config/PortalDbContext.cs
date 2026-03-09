
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
            entity.Property(e => e.Unidad).HasColumnName("unid_ad");
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

    }

}
