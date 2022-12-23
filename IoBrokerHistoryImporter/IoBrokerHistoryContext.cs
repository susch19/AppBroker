using Microsoft.EntityFrameworkCore;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading.Tasks;

namespace IoBrokerHistoryImporter;
internal class IoBrokerHistoryContext : DbContext
{
    private readonly string path;

    internal DbSet<Datapoint> Datapoints { get; set; }
    internal DbSet<BoolValue> Bools { get; set; }
    internal DbSet<StringValue> Strings { get; set; }
    internal DbSet<DoubleValue> Doubles { get; set; }

    public IoBrokerHistoryContext()
    {

    }

    public IoBrokerHistoryContext(string path)
    {
        this.path = path;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        _ = optionsBuilder
            .UseSqlite("Data Source=" + path)
            .UseLazyLoadingProxies();
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BoolValue>()
            .HasKey(c => new { c.Id, c.Ts });
        modelBuilder.Entity<StringValue>()
            .HasKey(c => new { c.Id, c.Ts });
        modelBuilder.Entity<DoubleValue>()
            .HasKey(c => new { c.Id, c.Ts });
    }
}

[Table("datapoints")]
public class Datapoint
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public int Id { get; set; }
    [Column("name")]
    public string Name { get; set; }
    [Column("type")]
    public int Type { get; set; }

}

public interface ValueBase
{
    long Ts { get; set; }
    int Id { get; set; }
}

[Table("ts_bool")]
public class BoolValue : ValueBase
{
    [Column("val")]
    public bool Val { get; set; }
    [Key]
    [Column("id")]
    public int Id { get; set; }
    [Column("ts")]
    public long Ts { get; set; }

}

[Table("ts_number")]
public class DoubleValue : ValueBase
{
    [Column("val")]
    public double Val { get; set; }
    [Column("id")]
    public int Id { get; set; }
    [Column("ts")]
    public long Ts { get; set; }

}

[Table("ts_string")]
public class StringValue : ValueBase
{
    [Column("val")]
    public string Val { get; set; }
    [Key]
    [Column("id")]
    public int Id { get; set; }
    [Column("ts")]
    public long Ts { get; set; }

}
