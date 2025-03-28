

    using global::SignalGenerator.Core.Models;
    using Microsoft.EntityFrameworkCore;
    using SignalGenerator.Core.Models;
    using System.Collections.Generic;

    namespace SignalGenerator.Core.Data
{
        public class AppDbContext : DbContext
        {
            public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

            public DbSet<SignalData> Signals { get; set; }
        }
    }


