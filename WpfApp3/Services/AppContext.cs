using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp3.Services
{
    public static class AppContext
    {
        private static readonly DatabaseContext _context = new DatabaseContext();

        public static SqlConnection GetContext()
        {
            return _context.GetConnection();
        }
    }
}
