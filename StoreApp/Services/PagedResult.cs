using StoreApp.Models;
using StoreApp.Repository;
using StoreApp.Shared;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace StoreApp.Services
{
    public class PagedResult<T>
    {
        public int TotalItems { get; set; }
        public List<T> Items { get; set; } = new();
    }
}
