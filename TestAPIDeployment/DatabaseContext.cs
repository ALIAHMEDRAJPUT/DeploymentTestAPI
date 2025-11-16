using Microsoft.AspNetCore.Builder.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Options;

namespace TestAPIDeployment
{
    public sealed class DatabaseContext(DbContextOptions<DatabaseContext> dbOptions) : DbContext(dbOptions)
    {
        private readonly string _collation;
        public DatabaseContext(
            IOptions<ConnectionStrings> options,
            DbContextOptions<DatabaseContext> dbOptions
            )
            : this(dbOptions)
        {
            _collation = options.Value.Collation;
        }

        public override ChangeTracker ChangeTracker
        {
            get
            {
                base.ChangeTracker.LazyLoadingEnabled = false;
                base.ChangeTracker.CascadeDeleteTiming = CascadeTiming.OnSaveChanges;
                base.ChangeTracker.CascadeDeleteTiming = CascadeTiming.OnSaveChanges;
                return base.ChangeTracker;
            }
        }

        public DbSet<User> Users => Set<User>();
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
        }
    }

    public class User
    {
        public string Name { get; set; }
    }
    public sealed class ConnectionStrings : IAppOptions
    {
        public static string ConfigSectionPath => "ConnectionStrings";

        /// <summary>
        /// Connection string to the relational database.
        /// </summary>
        public string Database { get; private init; }

        /// <summary>
        /// (Optional) Definition of the Collation for the relational database.
        /// REF: https://learn.microsoft.com/en-us/ef/core/miscellaneous/collations-and-case-sensitivity
        /// </summary>
        public string Collation { get; private init; }

        /// <summary>
        /// Connection string to the Cache server.
        /// </summary>
        public string Cache { get; private init; }
    }
    public interface IAppOptions
    {
        /// <summary>
        /// The configuration section path.
        /// </summary>
        static abstract string ConfigSectionPath { get; }
    }
    public sealed class InMemoryOptions : IAppOptions
    {
        public static string ConfigSectionPath => "InMemoryOptions";

        public bool Database { get; private init; }
        public bool Cache { get; private init; }
    }
}
