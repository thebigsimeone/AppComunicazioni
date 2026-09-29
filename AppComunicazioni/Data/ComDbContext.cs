using System;
using System.Collections.Generic;
using AppComunicazioni.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AppComunicazioni.Data;

public partial class ComDbContext : DbContext
{
    private readonly IConfiguration _configuration;

    public ComDbContext(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public ComDbContext(DbContextOptions<ComDbContext> options, IConfiguration configuration)
        : base(options)
    {
        _configuration = configuration;
    }

    public virtual DbSet<Comunicazioni> Comunicazionis { get; set; }
    public virtual DbSet<Destinatari> Destinataris { get; set; }
    public virtual DbSet<ComunicazioniDettaglio> ComunicazioniDettagli { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            // Usa la stringa di connessione dal file di configurazione
            var connectionString = _configuration.GetConnectionString("ComDbContext");
            optionsBuilder.UseSqlServer(connectionString);
        }
    }

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
