using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WpfApp3.Models;

namespace WpfApp3.Services
{
    public static class Session
    {
        public static User CurrentUser { get; set; }
        
    }
}