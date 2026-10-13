using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Descarboniza.Areas.Identity.Data;

// IDataProtectionKeyContext: guarda as chaves de proteção (login, formulários)
// no banco. No Render o disco é apagado a cada reinício, e sem isso todo mundo
// seria deslogado.
public class DescarbonizaContext : IdentityDbContext<IdentityUser>, IDataProtectionKeyContext
{
    public DescarbonizaContext(DbContextOptions<DescarbonizaContext> options)
        : base(options)
    {
    }

    public DbSet<Maquina> Maquinas { get; set; }
    public DbSet<Consumo> Consumos { get; set; }
    public DbSet<EmissaoCarbono> EmissoesCarbono { get; set; }

    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;

    public class ConfigUser : IEntityTypeConfiguration<Usuario>
    {
        public void Configure(EntityTypeBuilder<Usuario> builder)
        {
            builder.Property(x => x.Nome).HasMaxLength(150);
            builder.Property(x => x.Sobrenome).HasMaxLength(150);
        }
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfiguration(new ConfigUser());
    }
}
