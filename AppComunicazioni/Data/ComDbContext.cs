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

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("SERVER=10.10.20.21;DATABASE=testadc;User Id=sa;Pwd=K5jWUyAg;");

    /*    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    #warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=NA-023\\SQLEXPRESS;Database=db_comunicazioni;Trusted_Connection=True;TrustServerCertificate=True;");*/

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Comunicazioni>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_comunication");
        });

        modelBuilder.Entity<Destinatari>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_email");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
