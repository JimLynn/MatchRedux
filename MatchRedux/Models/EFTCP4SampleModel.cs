using System.Data.Entity;

namespace MatchRedux.Models {
    // Sample POCO entity class
    public class Product {
        public int ID { get; set; }
        public string Name { get; set; }
        public double Price { get; set; }
    }

    // Sample context
    public class MyProductContext : DbContext {
        public DbSet<Product> Products { get; set; }
    }
}