using System;
using System.Collections.Generic;
using AppComunicazioni.Models;
using Microsoft.EntityFrameworkCore;

namespace AppComunicazioni.Data;

public partial class ComDbContext : DbContext
{
    public ComDbContext()
    {
    }

    public ComDbContext(DbContextOptions<ComDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Comunicazioni> Comunicazionis { get; set; }
    public virtual DbSet<Destinatari> Destinataris { get; set; }
    public virtual DbSet<ComunicazioniDettaglio> ComunicazioniDettagli { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("SERVER=10.10.20.21;DATABASE=testadc;User Id=sa;Pwd=K5jWUyAg;TrustServerCertificate=True;");

    /*protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
=> optionsBuilder.UseSqlServer("Server=PC-FLAVIO\\SQLEXPRESS;Database=db_comunicazioni;Trusted_Connection=True;TrustServerCertificate=True;");*/

    //UFFICIO

    /*protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
    => optionsBuilder.UseSqlServer("Server=NA-23\\SQLEXPRESS;Database=db_comunicazioni;Trusted_Connection=True;TrustServerCertificate=True;");*/

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Comunicazioni>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_comunication");

            // Configura la relazione tra Comunicazioni e ComunicazioniDettaglio (uno a molti)
            entity.HasMany(e => e.Dettagli)
                  .WithOne(d => d.Comunicazione)
                  .HasForeignKey(d => d.ComunicazioneId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Destinatari>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_email");
        });

        modelBuilder.Entity<ComunicazioniDettaglio>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_comunicazioni_dettagli");

            entity.Property(e => e.Protocollo)
                  .HasMaxLength(50)
                  .IsRequired();

            entity.Property(e => e.CodiceFiscale)
                  .HasMaxLength(50)
                  .IsRequired();

            entity.Property(e => e.ColonnaSupplementare)
                  .HasMaxLength(100);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
