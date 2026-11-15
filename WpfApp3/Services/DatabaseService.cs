using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp3.Services
{
    public class DatabaseContext
    {
        private const string ConnectionString =
            @"Server=localhost;Database=ShoeSalesAnis;Integrated Security=True;TrustServerCertificate=True;";

        public SqlConnection GetConnection()
        {
            return new SqlConnection(ConnectionString);
        }
    }
    public static class Db
    {
        private static readonly DatabaseContext _context = new DatabaseContext();

        public static SqlConnection GetContext()
        {
            return _context.GetConnection();
        }
    }
}